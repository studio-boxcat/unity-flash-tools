using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using FTRuntime;
using FTSwfTools;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace FTEditor.Importer
{
    internal static class AtlasBuilder
    {
        public static Texture2D PackAtlas(
            string outputPath, string spriteFolder, int maxSize, int shapePadding)
        {
            var dataPath = outputPath.Replace(".png", ".tpsheet");
            var result = ExecutePack(outputPath, dataPath, spriteFolder, maxSize, shapePadding);
            if (result is false) return null;
            AssetDatabase.ImportAsset(outputPath);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(outputPath);
        }

        [MustUseReturnValue]
        public static bool ExecutePack(string sheet, string data, string spriteFolder, int maxSize, int shapePadding)
        {
            // https://www.codeandweb.com/texturepacker/documentation/texture-settings
            const string texturePacker = "/Applications/TexturePacker.app/Contents/MacOS/TexturePacker";

            var arguments =
                $"--format unity-texture2d --sheet {sheet} --data {data} " +
                "--alpha-handling ReduceBorderArtifacts " +
                $"--max-size {maxSize} " +
                "--size-constraints AnySize " +
                "--algorithm Polygon " +
                "--trim-mode Polygon " +
                "--trim-margin 0 " +
                "--tracer-tolerance 200 " +
                "--extrude 0 " +
                $"--shape-padding {shapePadding} " +
                "--pack-mode Best " +
                "--enable-rotation " +
                spriteFolder;

            L.I($"Running TexturePacker: {texturePacker} {arguments}");

            var procInfo = new ProcessStartInfo(texturePacker, arguments)
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            var proc = Process.Start(procInfo);
            proc!.WaitForExit();

            if (proc.ExitCode != 0)
            {
                var err = proc.StandardError.ReadToEnd();
                L.E($"TexturePacker failed with exit code {proc.ExitCode}\n{err}");
                return false;
            }

            return true;
        }

        public static void MigrateSpriteToMesh(string atlasPath, Mesh mesh, out MeshId[] bitmapToMesh)
        {
            // assign mesh id
            var sprites = AssetDatabase.LoadAllAssetsAtPath(atlasPath)
                .OfType<Sprite>()
                .Select(x => (Bitmap: (BitmapId) int.Parse(x.name), Sprite: x))
                .OrderBy(x => (int) x.Bitmap)
                .ToArray();

            // collect vertices & indices
            var meshVertices = new List<VertexData>();
            var meshIndices = new List<ushort>();
            var subMeshIndices = new List<SubMeshDescriptor>();
            foreach (var (_, sprite) in sprites)
            {
                var last = meshIndices.Count;
                var count = Append(sprite, meshVertices, meshIndices);
                Assert.AreNotEqual(0, count);
                subMeshIndices.Add(new SubMeshDescriptor(last, count));
            }

            // build mesh
            MeshBuilder.SetVertexLayout(mesh, meshVertices.Count);
            mesh.SetVertexBufferData(meshVertices, 0, 0, meshVertices.Count, stream: 0);
            mesh.SetIndices(meshIndices.ToArray(), MeshTopology.Triangles, 0, calculateBounds: false);
            mesh.SetSubMeshes(subMeshIndices);
            EditorUtility.SetDirty(mesh);

            // delete .tpsheet & sub-asset sprites
            File.Delete(atlasPath.Replace(".png", ".tpsheet"));
            foreach (var (_, sprite) in sprites)
                AssetDatabase.RemoveObjectFromAsset(sprite);

            // build bitmap to mesh map
            var maxBitmapId = (int) sprites[^1].Bitmap;
            bitmapToMesh = new MeshId[maxBitmapId + 1];
            for (var i = 0; i < sprites.Length; i++)
            {
                var bitmap = (int) sprites[i].Bitmap;
                bitmapToMesh[bitmap] = (MeshId) i;
            }

            return;

            static int Append(
                Sprite sprite,
                List<VertexData> outVertices,
                List<ushort> outIndices)
            {
                var inIndices = sprite.triangles;
                var offset = outVertices.Count;
                for (var i = 0; i < inIndices.Length; i++)
                    outIndices.Add((ushort) (inIndices[i] + offset));
                VertexData.Append(sprite, outVertices);
                return inIndices.Length;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct VertexData
        {
            // XXX: Mesh.CombineMeshes() requires position to be Vector3.
            public Vector3 Position;
            public ushort U; // Float16
            public ushort V; // Float16

            private VertexData(Vector3 position, Vector2 uv)
            {
                Position = position;
                U = f16(uv.x);
                V = f16(uv.y);
                return;

                static unsafe ushort f16(float x)
                {
                    const int infinity_32 = 255 << 23;
                    const uint msk = 0x7FFFF000u;

                    uint ux = asuint(x);
                    uint uux = ux & msk;
                    uint h = asuint(min(asfloat(uux) * 1.92592994e-34f, 260042752.0f)) + 0x1000 >> 13; // Clamp to signed infinity if overflowed
                    h = select(h, select(0x7c00u, 0x7e00u, (int) uux > infinity_32), (int) uux >= infinity_32); // NaN->qNaN and Inf->Inf
                    return (ushort) (h | (ux & ~msk) >> 16);

                    static float min(float x, float y) => float.IsNaN(y) || x < y ? x : y;
                    static uint asuint(float x) => *(uint*) &x;
                    static uint select(uint falseValue, uint trueValue, bool test) => test ? trueValue : falseValue;
                    static float asfloat(uint x) => *(float*) &x;
                }
            }

            public static void Append(Sprite sprite, List<VertexData> verts)
            {
                var poses = sprite.vertices;
                var uvs = sprite.uv;
                var count = poses.Length;
                for (var i = 0; i < count; ++i)
                {
                    // Sometimes the UVs are out of range.
                    Assert.IsTrue((uvs[i].x is >= -0.00001f and <= 1.00001f) && (uvs[i].y is >= -0.00001f and <= 1.00001f),
                        $"Invalid UV: {uvs[i]}");
                    var p = poses[i] * ImportConfig.PixelsPerUnit; // Scale to Unity units
                    verts.Add(new VertexData(p, uvs[i]));
                }
            }
        }
    }
}