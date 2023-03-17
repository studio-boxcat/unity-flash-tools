using UnityEngine;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace FTEditor.Postprocessors
{
    struct SubMeshData
    {
        public int StartVertex;
        public int IndexCount;
    }

    class MeshData
    {
        public SubMeshData[] SubMeshes;
        public Vector2[] Vertices;
        public SwfRectIntData[] Rects;
        public SwfVec4Data[] AddColors;
        public SwfVec4Data[] MulColors;

        public override int GetHashCode()
        {
            var hash = new Hash128();
            hash.Append(SubMeshes);
            hash.Append(Vertices);
            hash.Append(Rects);
            hash.Append(AddColors);
            hash.Append(MulColors);
            return hash.GetHashCode();
        }
    }

    static class MeshBuilder
    {
        public static Mesh Build(MeshData mesh_data)
        {
            var mesh = new Mesh();
            FillGeneratedMesh(mesh, mesh_data);
            MeshUtility.Optimize(mesh);
            mesh.UploadMeshData(true);
            return mesh;
        }

        //
        // FillGeneratedMesh
        //

        static void FillGeneratedMesh(Mesh mesh, MeshData mesh_data)
        {
            Assert.AreNotEqual(0, mesh_data.SubMeshes.Length);
            Assert.AreEqual(mesh_data.Vertices.Length, mesh_data.Rects.Length * 4);
            Assert.AreEqual(mesh_data.Rects.Length, mesh_data.AddColors.Length);
            Assert.AreEqual(mesh_data.Rects.Length, mesh_data.MulColors.Length);
            Assert.IsTrue(mesh_data.AddColors.All(x => x.x == 0 && x.y == 0 && x.z == 0 && x.w == 0));
            Assert.IsTrue(mesh_data.MulColors.All(x => x.x == 1 && x.y == 1 && x.z == 1));

            mesh.subMeshCount = mesh_data.SubMeshes.Length;

            var vertexCount = mesh_data.Vertices.Length;

            mesh.SetVertexBufferParams(
                vertexCount,
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float16, 2),
                // X: 12 bits, Y: 12 bits, A: 8 bits.
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.UInt32, 1));

            var verts = new NativeArray<VertexData>(vertexCount, Allocator.Temp);
            SetVertices(mesh_data.Vertices, verts);
            SetUVs(mesh_data.Rects, mesh_data.MulColors, verts);
            mesh.SetVertexBufferData(verts, 0, 0, vertexCount);
            verts.Dispose();

            for (var i = 0; i < mesh_data.SubMeshes.Length; ++i)
            {
                var indices = AllocateTriangles(
                    mesh_data.SubMeshes[i].StartVertex,
                    mesh_data.SubMeshes[i].IndexCount);
                mesh.SetTriangles(indices.ToArray(), i);
                indices.Dispose();
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        struct VertexData
        {
            public half2 Position;
            // X: 12 bits, Y: 12 bits, A: 8 bits.
            public uint UVA;
        }

        static void SetVertices(Vector2[] vertices, NativeArray<VertexData> verts)
        {
            for (var i = 0; i < vertices.Length; ++i)
            {
                var vert = verts[i];
                vert.Position = new half2(vertices[i].x, vertices[i].y);
                verts[i] = vert;
            }
        }

        static void SetUVs(SwfRectIntData[] rects, SwfVec4Data[] mulcolors, NativeArray<VertexData> verts)
        {
            for (var i = 0; i < rects.Length; ++i)
            {
                var rect = rects[i];
                Assert.IsTrue(mulcolors[i].w is >= 0 and <= 1);
                var a = (byte) Mathf.RoundToInt(mulcolors[i].w * byte.MaxValue);

                SetUV(i * 4, rect.xMin, rect.yMin, a);
                SetUV(i * 4 + 1, rect.xMax, rect.yMin, a);
                SetUV(i * 4 + 2, rect.xMax, rect.yMax, a);
                SetUV(i * 4 + 3, rect.xMin, rect.yMax, a);
            }

            void SetUV(int i, int x, int y, byte a)
            {
                Assert.IsTrue(x is >= 0 and <= 4095); // 12 bits.
                Assert.IsTrue(y is >= 0 and <= 4095); // 12 bits.

                var vert = verts[i];
                vert.UVA = (uint) (x | (y << 12) | (a << 24));
                verts[i] = vert;
            }
        }

        static NativeArray<ushort> AllocateTriangles(int start_vertex, int index_count)
        {
            Assert.AreEqual(0, index_count % 6);

            var arr = new NativeArray<ushort>(index_count, Allocator.Temp);

            for (var i = 0; i < index_count / 6; i++)
            {
                arr[i * 6] = (ushort) (start_vertex + 2);
                arr[i * 6 + 1] = (ushort) (start_vertex + 1);
                arr[i * 6 + 2] = (ushort) (start_vertex + 0);
                arr[i * 6 + 3] = (ushort) (start_vertex + 0);
                arr[i * 6 + 4] = (ushort) (start_vertex + 3);
                arr[i * 6 + 5] = (ushort) (start_vertex + 2);
                start_vertex += 4;
            }

            return arr;
        }
    }
}