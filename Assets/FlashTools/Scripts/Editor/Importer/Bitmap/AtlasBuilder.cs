using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TexturePacker;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace FT.Importer
{
    internal static class AtlasBuilder
    {
        public static SheetInfo PackAtlas(string outputPath, string spriteFolder, int maxSize, int shapePadding)
        {
            var dataPath = $"Temp/{Guid.NewGuid().ToString().Replace("-", "")}.tpsheet";
            var result = TexturePackerCLI.Pack(outputPath, dataPath, spriteFolder, maxSize, shapePadding);
            return result ? SheetLoader.Load(dataPath) : null;
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
                U = new half(uv.x).value;
                V = new half(uv.y).value;
            }
        }
    }
}