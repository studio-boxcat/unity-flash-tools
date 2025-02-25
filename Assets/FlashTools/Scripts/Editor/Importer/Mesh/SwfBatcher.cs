using System;
using System.Collections.Generic;
using System.Linq;
using FTRuntime;
using UnityEngine;
using UnityEngine.Assertions;

namespace FTEditor.Importer
{
    internal class SwfBatcher
    {
        private MaterialKey _curMaterial = _invalidMaterial; // intentionally set to invalid value.
        private readonly List<SwfObject> _curObjects = new();
        private readonly List<SubMeshData> _subMeshes = new();

        private static readonly MaterialKey _invalidMaterial = new((SwfInstanceData.Types) byte.MaxValue, default, default);


        public void Feed(SwfInstanceData inst, MeshId[] bitmapToMesh)
        {
            // Start new batch if necessary.
            var instMaterial = inst.GetMaterialKey();
            if (_curMaterial != instMaterial)
            {
                if (_curObjects.Count is not 0)
                    SettleIntoNewBatch();
                _curMaterial = instMaterial;
            }

            // Matrix
            var matrix = GetVertexMatrix(inst.Matrix);

            // Alpha
            var mul = inst.ColorTrans.CalculateMul();
            var add = inst.ColorTrans.CalculateAdd();
            Assert.IsTrue(mul is { r: 1, g: 1, b: 1 }, "Tint is not supported");
            Assert.IsTrue(add is { r: 0, g: 0, b: 0, a: 0 }, "Add is not supported");
            Assert.IsTrue(mul.a is >= 0 and <= 1, "Alpha must be in range [0, 1]");
            var a = (byte) Mathf.RoundToInt(mul.a * 0xFF);

            // Add object
            var meshId = (ushort) bitmapToMesh[(int) inst.Bitmap];
            _curObjects.Add(new SwfObject(meshId, matrix, a));
        }

        // Bitmap space -> view space.
        private static SwfMatrix GetVertexMatrix(SwfMatrix m)
        {
            const float scale = 1f / ImportConfig.PixelsPerUnit / ImportConfig.CustomScaleFactor;
            return SwfMatrix.Scale(scale, -scale) * m;
        }

        public SwfFrame Flush(ushort[] indexCounts, out MaterialKey[] materials)
        {
            if (_curObjects.Count is not 0)
                SettleIntoNewBatch();

            // If there's a single mask-out batch, remove it.
            // MaskOut will decrease the stencil value but the stencil will never be used after the mask-out.
            if (SingleMaskOut(_subMeshes, out var index))
                _subMeshes.RemoveAt(index);

            // Build frame
            ushort objCount = 0;
            ushort lastIndex = 0;
            var meshCount = _subMeshes.Count;
            var objs = new SwfObject[_subMeshes.Sum(x => x.Objects.Length)];
            var subMeshIndices = new ushort[meshCount]; // submesh end index
            materials = new MaterialKey[meshCount];
            for (var i = 0; i < meshCount; i++)
            {
                var data = _subMeshes[i];

                // flatten the objects
                var curObjs = data.Objects;
                Array.Copy(curObjs, 0, objs, objCount, curObjs.Length);
                objCount += (ushort) curObjs.Length;

                // write submesh index
                lastIndex += CountIndices(curObjs, indexCounts);
                subMeshIndices[i] = lastIndex;

                // set material
                materials[i] = data.Material;
            }
            _subMeshes.Clear();

            if (subMeshIndices.Length <= 1)
                subMeshIndices = Array.Empty<ushort>();

            return new SwfFrame(objs, subMeshIndices);


            static bool SingleMaskOut(List<SubMeshData> batches, out int index)
            {
                for (var i = 0; i < batches.Count; i++)
                {
                    if (batches[i].Material.Type is SwfInstanceData.Types.MaskOut)
                    {
                        index = i;
                        return true;
                    }
                }

                index = default;
                return false;
            }

            static ushort CountIndices(SwfObject[] objs, ushort[] indexCounts)
            {
                var count = 0u;
                foreach (var obj in objs)
                    count += indexCounts[obj.MeshIndex];
                Assert.IsTrue(count <= ushort.MaxValue, "Index count is too large");
                return (ushort) count;
            }
        }

        private void SettleIntoNewBatch()
        {
            Assert.IsTrue(_curObjects.Count is not 0,
                "No poses to settle into new batch");

            _subMeshes.Add(new SubMeshData(
                _curObjects.ToArray(),
                _curMaterial));

            _curMaterial = _invalidMaterial;
            _curObjects.Clear();
        }

        private readonly struct SubMeshData
        {
            public readonly SwfObject[] Objects;
            public readonly MaterialKey Material;

            public SubMeshData(SwfObject[] objects, MaterialKey material)
            {
                Objects = objects;
                Material = material;
            }
        }
    }
}