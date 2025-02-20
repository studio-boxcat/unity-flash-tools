using System.Collections.Generic;
using System.Runtime.InteropServices;
using FTSwfTools;

namespace FTEditor.Importer
{
    [StructLayout(LayoutKind.Explicit)]
    internal readonly struct MaterialKey
    {
        [FieldOffset(0)]
        public readonly SwfInstanceData.Types Type; // 1 byte
        [FieldOffset(1)]
        public readonly SwfBlendMode BlendMode; // 1 byte
        [FieldOffset(2)]
        public readonly Depth ClipDepth; // 2 bytes
        [FieldOffset(0)]
        public readonly int Hash;

        public MaterialKey(SwfInstanceData.Types type, SwfBlendMode blendMode, Depth clipDepth) : this()
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

        public bool Equals(MaterialKey other)
            => Type == other.Type && BlendMode == other.BlendMode && ClipDepth == other.ClipDepth;
        public override int GetHashCode() => Hash;

        public static bool operator ==(MaterialKey a, MaterialKey b) => a.Equals(b);
        public static bool operator !=(MaterialKey a, MaterialKey b) => !a.Equals(b);

        public static bool Equals(MaterialKey[] a, MaterialKey[] b)
        {
            if (a.Length != b.Length) return false;
            for (var i = 0; i < a.Length; i++)
                if (a[i] != b[i])
                    return false;
            return true;
        }

        public class Comparer : IEqualityComparer<MaterialKey>
        {
            public static readonly Comparer Instance = new();
            public bool Equals(MaterialKey x, MaterialKey y) => x.Equals(y);
            public int GetHashCode(MaterialKey obj) => obj.GetHashCode();
        }
    }
}