using UnityEngine;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine.Assertions;

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
        public SwfRectData[] Rects;
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

            var verts = GeneratedMeshCache.AllocateVertices(mesh_data.Vertices);
            mesh.SetVertices(verts);
            verts.Dispose();

            for (var i = 0; i < mesh_data.SubMeshes.Length; ++i)
            {
                var indices = GeneratedMeshCache.AllocateTriangles(
                    mesh_data.SubMeshes[i].StartVertex,
                    mesh_data.SubMeshes[i].IndexCount);
                mesh.SetTriangles(indices.ToArray(), i);
                indices.Dispose();
            }

            var uvs = GeneratedMeshCache.AllocateUVs(mesh_data.Rects, mesh_data.MulColors);
            mesh.SetUVs(0, uvs);
            uvs.Dispose();
        }

        static class GeneratedMeshCache
        {
            public static NativeArray<Vector3> AllocateVertices(Vector2[] vertices)
            {
                var arr = new NativeArray<Vector3>(vertices.Length, Allocator.Temp);
                for (var i = 0; i < vertices.Length; ++i)
                    arr[i] = vertices[i];
                return arr;
            }

            public static NativeArray<int> AllocateTriangles(int start_vertex, int index_count)
            {
                Assert.AreEqual(0, index_count % 6);

                var arr = new NativeArray<int>(index_count, Allocator.Temp);

                for (var i = 0; i < index_count / 6; i++)
                {
                    arr[i * 6] = start_vertex + 2;
                    arr[i * 6 + 1] = start_vertex + 1;
                    arr[i * 6 + 2] = start_vertex + 0;
                    arr[i * 6 + 3] = start_vertex + 0;
                    arr[i * 6 + 4] = start_vertex + 3;
                    arr[i * 6 + 5] = start_vertex + 2;
                    start_vertex += 4;
                }

                return arr;
            }

            public static NativeArray<Vector3> AllocateUVs(SwfRectData[] rects, SwfVec4Data[] mulcolors)
            {
                var arr = new NativeArray<Vector3>(rects.Length * 4, Allocator.Temp);

                for (var i = 0; i < rects.Length; ++i)
                {
                    var rect = rects[i];
                    var a = mulcolors[i].w;

                    arr[i * 4] = new Vector3(rect.xMin, rect.yMin, a);
                    arr[i * 4 + 1] = new Vector3(rect.xMax, rect.yMin, a);
                    arr[i * 4 + 2] = new Vector3(rect.xMax, rect.yMax, a);
                    arr[i * 4 + 3] = new Vector3(rect.xMin, rect.yMax, a);
                }

                return arr;
            }
        }
    }
}