using System.Collections.Generic;
using System.Linq;
using FTSwfTools;
using UnityEngine;
using UnityEngine.Assertions;

namespace FTEditor.Importer
{
    class InstanceBatcher
    {
        public readonly struct BatchProperties
        {
            public readonly SwfInstanceData.Types Type;
            public readonly SwfBlendModeData.Types BlendMode;
            public readonly Depth ClipDepth;

            BatchProperties(SwfInstanceData.Types type, SwfBlendModeData.Types blendMode, Depth clipDepth)
            {
                Type = type;
                BlendMode = blendMode;
                ClipDepth = clipDepth;
            }

            public static BatchProperties FromInstanceData(SwfInstanceData inst) => new(inst.Type, inst.BlendMode.type, inst.ClipDepth);

            public static BatchProperties Invalid => new((SwfInstanceData.Types) byte.MaxValue, default, default);

            public bool Equals(BatchProperties other) => Type == other.Type && BlendMode == other.BlendMode && ClipDepth == other.ClipDepth;
        }

        public readonly struct BatchInfo
        {
            public readonly BatchProperties Property;
            public readonly Vector2[] Poses;
            public readonly Vector3[] UVAs;
            public readonly ushort[] Indices;

            public BatchInfo(BatchProperties property, Vector2[] poses, Vector3[] uvAs, ushort[] indices)
            {
                Property = property;
                Poses = poses;
                UVAs = uvAs;
                Indices = indices;
            }
        }

        BatchProperties _curBatchProps = BatchProperties.Invalid; // intentionally set to invalid value.
        readonly List<Vector2> _curPoses = new();
        readonly List<Vector3> _curUVAs = new();
        readonly List<ushort> _curIndices = new();
        readonly List<BatchInfo> _batches = new();


        public void Feed(SwfInstanceData inst, SpriteData spriteData)
        {
            Assert.IsNotNull(inst);

            // Start new batch if necessary.
            var instBatchProps = BatchProperties.FromInstanceData(inst);
            if (_curBatchProps.Equals(instBatchProps) is false)
            {
                if (_curPoses.Count is not 0)
                    SettleIntoNewBatch();
                _curBatchProps = instBatchProps;
            }

            // Store vertex offset.
            var vertexOffset = (ushort) _curPoses.Count;

            // Pos
            var matrix = GetVertexMatrix(inst.Matrix);
            foreach (var pos in spriteData.Poses)
                _curPoses.Add(matrix.MultiplyPoint3x4(pos));

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

        public static Matrix4x4 GetVertexMatrix(Matrix4x4 m) =>
            Matrix4x4.Scale(new Vector3(1.0f, -1.0f, 1.0f) / ImportConfig.PixelsPerUnit / ImportConfig.CustomScaleFactor) * m;

        public BatchInfo[] Flush()
        {
            if (_curPoses.Count is not 0)
                SettleIntoNewBatch();

            // If there's a single mask-out batch, remove it.
            if (SingleMaskOut(_batches, out var index))
                _batches.RemoveAt(index);

            var batches = _batches.ToArray();
            _batches.Clear();
            return batches;

            static bool SingleMaskOut(List<BatchInfo> batches, out int index)
            {
                for (var i = 0; i < batches.Count; i++)
                {
                    if (batches[i].Property.Type is SwfInstanceData.Types.MaskOut)
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

            _batches.Add(new BatchInfo(
                _curBatchProps,
                _curPoses.ToArray(),
                _curUVAs.ToArray(),
                _curIndices.ToArray()
            ));

            _curBatchProps = BatchProperties.Invalid;
            _curPoses.Clear();
            _curUVAs.Clear();
            _curIndices.Clear();
        }
    }
}