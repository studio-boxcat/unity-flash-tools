using System.Collections.Generic;
using FTSwfTools.SwfTypes;
using UnityEngine;
using UnityEngine.Assertions;

namespace FTEditor.Importer
{
    readonly struct RenderUnit
    {
        public readonly MeshData Mesh;
        public readonly MaterialKey Material;

        public RenderUnit(MeshData mesh, MaterialKey material)
        {
            Mesh = mesh;
            Material = material;
        }
    }

    class InstanceBatcher
    {
        MaterialKey _curMaterial = _invalidMaterial; // intentionally set to invalid value.
        readonly List<Vector2> _curPoses = new();
        readonly List<Vector3> _curUVAs = new();
        readonly List<ushort> _curIndices = new();
        readonly List<RenderUnit> _batches = new();

        static readonly MaterialKey _invalidMaterial = new((SwfInstanceData.Types) byte.MaxValue, default, default);


        public void Feed(SwfInstanceData inst, SpriteData spriteData)
        {
            Assert.IsNotNull(inst);

            // Start new batch if necessary.
            var instMaterial = inst.GetMaterialKey();
            if (_curMaterial.Equals(instMaterial) is false)
            {
                if (_curPoses.Count is not 0)
                    SettleIntoNewBatch();
                _curMaterial = instMaterial;
            }

            // Store vertex offset.
            var vertexOffset = (ushort) _curPoses.Count;

            // Pos
            var matrix = GetVertexMatrix(inst.Matrix);
            foreach (var pos in spriteData.Poses)
                _curPoses.Add(matrix.MultiplyPoint(pos));

            // UVA
            var mul = inst.ColorTrans.CalculateMul();
            var add = inst.ColorTrans.CalculateAdd();
            Assert.IsTrue(mul is { r: 1, g: 1, b: 1 }, "Tint is not supported");
            Assert.IsTrue(add is { r: 0, g: 0, b: 0, a: 0 }, "Add is not supported");
            var a = mul.a;
            foreach (var uva in spriteData.UVs)
                _curUVAs.Add(new Vector3(uva.x, uva.y, a));

            // Indices
            foreach (var index in spriteData.Indices)
            {
                Assert.IsTrue((vertexOffset + index) < ushort.MaxValue);
                _curIndices.Add((ushort) (vertexOffset + index));
            }
        }

        // Swf space -> view space.
        public static SwfMatrix GetVertexMatrix(SwfMatrix m)
        {
            const float scale = 1f / ImportConfig.PixelsPerUnit / ImportConfig.CustomScaleFactor;
            return SwfMatrix.Scale(scale, -scale) * m;
        }

        public RenderUnit[] Flush()
        {
            if (_curPoses.Count is not 0)
                SettleIntoNewBatch();

            // If there's a single mask-out batch, remove it.
            if (SingleMaskOut(_batches, out var index))
                _batches.RemoveAt(index);

            var batches = _batches.ToArray();
            _batches.Clear();
            return batches;

            static bool SingleMaskOut(List<RenderUnit> batches, out int index)
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
        }

        void SettleIntoNewBatch()
        {
            Assert.IsTrue(_curPoses.Count is not 0,
                "No poses to settle into new batch");

            _batches.Add(new RenderUnit(
                new MeshData(_curPoses.ToArray(), _curUVAs.ToArray(), _curIndices.ToArray()),
                _curMaterial));

            _curMaterial = _invalidMaterial;
            _curPoses.Clear();
            _curUVAs.Clear();
            _curIndices.Clear();
        }
    }
}