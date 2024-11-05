using System.Runtime.InteropServices;
using FTRuntime;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace FTEditor.Importer
{
    static class SpriteMeshFactory
    {
        public static void Set(Sprite sprite, Mesh mesh)
        {
            var poses = sprite.vertices;
            var uvs = sprite.uv;
            var indices = sprite.triangles;
            Assert.AreEqual(poses.Length, uvs.Length);

            var vertCount = poses.Length;
            MeshBuilder.SetVertexLayout(mesh, vertCount);
            using var verts = new NativeArray<VertexData>(vertCount, Allocator.Temp);
            SetVertices(poses, uvs, verts);
            mesh.SetVertexBufferData(verts, 0, 0, vertCount);
            mesh.SetTriangles(indices, calculateBounds: true, submesh: 0);

            EditorUtility.SetDirty(mesh);
            return;

            static void SetVertices(Vector2[] poses, Vector2[] uvs, NativeArray<VertexData> verts)
            {
                var count = poses.Length;
                for (var i = 0; i < count; ++i)
                {
                    // Sometimes the UVs are out of range.
                    Assert.IsTrue((uvs[i].x is >= -0.00001f and <= 1) && (uvs[i].y is >= -0.00001f and <= 1),
                        $"Invalid UV: uva=(x={uvs[i].x}, y={uvs[i].y})");
                    var p = (Vector3) (poses[i] * ImportConfig.PixelsPerUnit); // Scale to Unity units
                    var u = f16(uvs[i].x);
                    var v = f16(uvs[i].y);
                    verts[i] = new VertexData(p, u, v);
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        struct VertexData
        {
            // XXX: Mesh.CombineMeshes() requires position to be Vector3.
            public Vector3 Position;
            public ushort U; // Float16
            public ushort V; // Float16

            public VertexData(Vector3 position, ushort u, ushort v)
            {
                Position = position;
                U = u;
                V = v;
            }
        }

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