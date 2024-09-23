using System;
using FTSwfTools.SwfTypes;

namespace FTEditor
{
    readonly struct MaterialKey
    {
        public readonly SwfInstanceData.Types Type;
        public readonly SwfBlendMode BlendMode;
        public readonly Depth ClipDepth;

        public MaterialKey(SwfInstanceData.Types type, SwfBlendMode blendMode, Depth clipDepth)
        {
            Type = type;
            BlendMode = blendMode;
            ClipDepth = clipDepth;
        }

        public void Deconstruct(out SwfInstanceData.Types type, out SwfBlendMode blendMode, out Depth clipDepth)
        {
            type = Type;
            blendMode = BlendMode;
            clipDepth = ClipDepth;
        }

        public bool Equals(MaterialKey other) => Type == other.Type && BlendMode == other.BlendMode && ClipDepth == other.ClipDepth;
        public override bool Equals(object obj) => obj is MaterialKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine((int) Type, (int) BlendMode, (int) ClipDepth);
    }
}