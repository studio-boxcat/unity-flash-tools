using System.Linq;
using UnityEngine;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace FTEditor.Importer
{
    readonly struct MeshData
    {
        public readonly Vector2[] Poses;
        public readonly Vector3[] UVAs; // z = alpha
        public readonly ushort[] Indices;
        public readonly int Hash;

        public MeshData(Vector2[] poses, Vector3[] uvAs, ushort[] indices)
        {
            Poses = poses;
            UVAs = uvAs;
            Indices = indices;

            Hash = default;
            foreach (var x in Poses) Hash ^= x.GetHashCode();
            foreach (var x in UVAs) Hash ^= x.GetHashCode();
            foreach (var x in Indices) Hash ^= x;
        }

        public override int GetHashCode() => Hash;
    }

    static class MeshBuilder
    {
        public static Mesh Build(MeshData[] mesh_data)
        {
            var mesh = new Mesh();
            SetupMesh(mesh, mesh_data);
            // XXX: Somehow this breaks the mesh.
            // MeshUtility.Optimize(mesh);
            mesh.UploadMeshData(true);
            return mesh;
        }

        static void SetupMesh(Mesh mesh, MeshData[] meshData)
        {
            Assert.AreNotEqual(0, meshData.Length);

            mesh.subMeshCount = meshData.Length;

            var totalVertCount = meshData.Sum(x => x.Poses.Length);
            var bounds = new Bounds();

            mesh.SetVertexBufferParams(
                totalVertCount,
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float16, 2),
                // Use 4 half to assign 4 bytes.
                // X: U, Y: V, Z: A, W: Unused.
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, 4));
            using var verts = new NativeArray<VertexData>(totalVertCount, Allocator.Temp);
            var vertOffset = 0;
            for (var index = 0; index < meshData.Length; index++)
            {
                var subMeshData = meshData[index];
                SetVertices(subMeshData.Poses, subMeshData.UVAs, verts, vertOffset);
                mesh.SetTriangles(subMeshData.Indices, submesh: index, calculateBounds: false, baseVertex: vertOffset);
                Encapsulate(subMeshData.Poses, ref bounds);
                vertOffset += subMeshData.Poses.Length;
            }

            mesh.SetVertexBufferData(verts, 0, 0, totalVertCount);
            mesh.bounds = bounds;
            return;

            static void SetVertices(Vector2[] poses, Vector3[] uvas, NativeArray<VertexData> verts, int offset)
            {
                var count = poses.Length;
                for (var i = 0; i < count; ++i)
                {
                    Assert.AreEqual(poses.Length, uvas.Length);
                    Assert.IsTrue((uvas[i].x is >= 0 and <= 1) && (uvas[i].y is >= 0 and <= 1),
                        $"Invalid UV: uva={uvas[i]}, index={i}");
                    verts[i + offset] = new VertexData(
                        (half2) poses[i],
                        (half4) uvas[i]);
                }
            }

            static void Encapsulate(Vector2[] vertices, ref Bounds bounds)
            {
                foreach (var vertex in vertices)
                    bounds.Encapsulate(vertex);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        struct VertexData
        {
            public half2 Position;
            public half4 UVA;

            public VertexData(half2 position, half4 uva)
            {
                Position = position;
                UVA = uva;
            }
        }
    }
}