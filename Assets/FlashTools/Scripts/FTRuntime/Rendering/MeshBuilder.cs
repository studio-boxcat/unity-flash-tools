using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace FT
{
    public static class MeshBuilder
    {
        private static VertexAttributeDescriptor[] _vertexLayout;

        public static Mesh CreateEmpty(bool dynamic = true)
        {
            var mesh = new Mesh { name = "", hideFlags = HideFlags.DontUnloadUnusedAsset, indexFormat = IndexFormat.UInt16 };
            if (dynamic) mesh.MarkDynamic();
            SetVertexLayout(mesh, 0);
            return mesh;
        }

        public static void SetVertexLayout(Mesh mesh, int vertexCount)
        {
            mesh.SetVertexBufferParams(vertexCount, _vertexLayout ??= new[]
            {
                // XXX: CombineMesh requires Float32 x 3 for position
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, 2, 0),
                // XXX: Use UNorm8 x 4 to avoid the following error:
                // Invalid vertex attribute format+dimension value (UNorm8 x 1, data size must be multiple of 4)
                // XXX: Combine mesh will reset the vertex layout, so we keep the UNorm8 x 4 for sprite mesh even if it's not used.
                new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.UNorm8, 4, 1)
            });
        }

        internal static void Build(SwfFrame frame, Mesh catalogMesh, Mesh mesh)
        {
            Assert.AreEqual(VertexAttributeFormat.Float32, mesh.GetVertexAttributeFormat(VertexAttribute.Position), "Position format mismatch");
            Assert.AreEqual(3, mesh.GetVertexAttributeDimension(VertexAttribute.Position), "Position dimension mismatch");
            Assert.AreEqual(VertexAttributeFormat.Float16, mesh.GetVertexAttributeFormat(VertexAttribute.TexCoord0), "UV format mismatch");
            Assert.AreEqual(2, mesh.GetVertexAttributeDimension(VertexAttribute.TexCoord0), "UV dimension mismatch");
            Assert.AreEqual(VertexAttributeFormat.UNorm8, mesh.GetVertexAttributeFormat(VertexAttribute.TexCoord1), "Alpha format mismatch");
            Assert.AreEqual(4, mesh.GetVertexAttributeDimension(VertexAttribute.TexCoord1), "Alpha dimension mismatch");

            AlphaBuffer.Clear();

            var objs = frame.Objects;
            var objCount = objs.Length;
            var cis = CombineInstancePool.Get(objCount);

            // combine meshes
            for (var i = 0; i < objCount; i++)
            {
                var obj = objs[i];
                var subMeshIndex = (int) obj.MeshIndex;
                cis[i] = new CombineInstance
                {
                    mesh = catalogMesh,
                    transform = (Matrix4x4) obj.Matrix,
                    subMeshIndex = subMeshIndex,
                };

                var desc = catalogMesh.GetSubMesh(subMeshIndex);
                AlphaBuffer.Append(obj.Alpha, desc.vertexCount);
            }
            mesh.CombineMeshes(cis, true, true);

            // set alpha
            AlphaBuffer.Flush(mesh, stream: 1);

            // set submeshes
            var subMeshIndices = frame.SubMeshIndices; // end index of each submesh
            var subMeshCount = subMeshIndices.Length;
            if (subMeshCount > 1)
            {
                var subMeshDesc = GetSubMeshDesc(subMeshCount);
                var lastIndex = 0;
                for (var i = 0; i < subMeshCount; i++)
                {
                    var nextIndex = subMeshIndices[i];
                    subMeshDesc[i] = new SubMeshDescriptor(lastIndex, nextIndex - lastIndex);
                    lastIndex = nextIndex;
                }
                mesh.SetSubMeshes(subMeshDesc, 0, subMeshCount);
            }
            else
            {
                Assert.AreEqual(1, mesh.subMeshCount, "SubMesh count mismatch");
            }
        }

        private static SubMeshDescriptor[] _subMeshDescCache;

        private static SubMeshDescriptor[] GetSubMeshDesc(int count)
        {
            if (_subMeshDescCache is null || _subMeshDescCache.Length < count)
            {
                var capacity = count > 8 ? count : 8; // minimum capacity
                _subMeshDescCache = new SubMeshDescriptor[capacity];
            }
            return _subMeshDescCache;
        }

        private static class AlphaBuffer
        {
            private static int _ptr;
            private static NativeArray<uint> _buffer;

            public static unsafe void Append(byte alpha, int count)
            {
                var last = _ptr + count;
                SecureSpace(ref _buffer, last);
                UnsafeUtility.MemSet((byte*) _buffer.GetUnsafePtr() + _ptr * 4, alpha, count * 4); // UNorm8 x 4
                _ptr = last;
                return;

                static void SecureSpace(ref NativeArray<uint> buffer, int capacity)
                {
                    if (buffer.IsCreated is false)
                    {
                        buffer = new NativeArray<uint>(capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                        return;
                    }

                    if (buffer.Length < capacity)
                    {
                        var newBuffer = new NativeArray<uint>(capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                        UnsafeUtility.MemCpy(newBuffer.GetUnsafePtr(), buffer.GetUnsafePtr(), buffer.Length * sizeof(uint));
                        buffer.Dispose();
                        buffer = newBuffer;
                    }
                }
            }

            public static void Clear()
            {
                _ptr = 0;
            }

            public static void Flush(Mesh mesh, int stream)
            {
                mesh.SetVertexBufferData(_buffer, 0, 0, _ptr, stream,
                    MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds);
                _ptr = 0;
            }
        }
    }
}