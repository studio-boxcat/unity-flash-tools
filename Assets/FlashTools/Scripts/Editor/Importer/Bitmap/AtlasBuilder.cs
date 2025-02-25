using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using FTRuntime;
using JetBrains.Annotations;
using TexturePackerImporter;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace FTEditor.Importer
{
    internal static class AtlasBuilder
    {
        public static SheetInfo PackAtlas(string outputPath, string spriteFolder, int maxSize, int shapePadding)
        {
            var dataPath = $"Temp/{Guid.NewGuid().ToString().Replace("-", "")}.tpsheet";
            var result = ExecutePack(outputPath, dataPath, spriteFolder, maxSize, shapePadding);
            return result ? SheetLoader.Load(dataPath) : null;
        }

        [MustUseReturnValue]
        public static bool ExecutePack(string sheetPath, string dataPath, string spriteFolder, int maxSize, int shapePadding)
        {
            // https://www.codeandweb.com/texturepacker/documentation/texture-settings
            const string texturePacker = "/Applications/TexturePacker.app/Contents/MacOS/TexturePacker";

            var arguments =
                $"--format unity-texture2d --sheet {sheetPath} --data {dataPath} " +
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

        public static void BuildSpriteMesh(SheetInfo sheetInfo, Mesh mesh, out MeshId[] bitmapToMesh)
        {
            bitmapToMesh = Array.Empty<MeshId>();

            // collect vertices & indices
            var meshVertices = new List<VertexData>();
            var meshIndices = new List<ushort>();
            var subMeshIndices = new List<SubMeshDescriptor>();

            var sprites = sheetInfo.geometries;
            for (var i = 0; i < sprites.Length; i++)
            {
                // add submesh
                var last = meshIndices.Count;
                var count = Append(sheetInfo, i, meshVertices, meshIndices);
                Assert.AreNotEqual(0, count);
                subMeshIndices.Add(new SubMeshDescriptor(last, count));

                // add bitmapToMesh
                var bitmapId = int.Parse(sheetInfo.metadata[i].name);
                if (bitmapToMesh.Length <= bitmapId)
                    Array.Resize(ref bitmapToMesh, bitmapId + 1);
                bitmapToMesh[bitmapId] = (MeshId) i;
            }

            // build mesh
            MeshBuilder.SetVertexLayout(mesh, meshVertices.Count);
            mesh.SetVertexBufferData(meshVertices, 0, 0, meshVertices.Count, stream: 0);
            mesh.SetIndices(meshIndices.ToArray(), MeshTopology.Triangles, 0, calculateBounds: false);
            mesh.SetSubMeshes(subMeshIndices);
            EditorUtility.SetDirty(mesh);
        }

        private static int Append(SheetInfo sheetInfo, int spriteIndex, List<VertexData> outVerts, List<ushort> outIndices)
        {
            var md = sheetInfo.metadata[spriteIndex];
            var geo = sheetInfo.geometries[spriteIndex];


            // append indices (must be done first to calculate vertex offset)
            var indices = geo.Triangles;
            var indexOffset = outVerts.Count;
            foreach (var index in indices)
                outIndices.Add((ushort) (index + indexOffset));


            // append vertices
            var vert = geo.Vertices;
            var origin = md.rect.min;
            var offset = md.rect.size * md.pivot;
            foreach (var v in vert)
            {
                var uv = origin + v;
                uv.x /= sheetInfo.width;
                uv.y /= sheetInfo.height;
                // Sometimes the UVs are out of range.
                Assert.IsTrue((uv.x is >= -0.00001f and <= 1.00001f) && (uv.y is >= -0.00001f and <= 1.00001f), $"Invalid UV: {uv}");
                outVerts.Add(new VertexData(v - offset, uv));
            }


            return indices.Length;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct VertexData
        {
            // XXX: Mesh.CombineMeshes() requires position to be Vector3.
            public Vector3 Position;
            public ushort U; // Float16
            public ushort V; // Float16

            public VertexData(Vector3 position, Vector2 uv)
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
        }
    }
}