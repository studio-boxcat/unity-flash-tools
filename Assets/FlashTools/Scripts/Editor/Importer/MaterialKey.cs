using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace FT.Importer
{
    [StructLayout(LayoutKind.Explicit)] // total size 4 bytes
    internal readonly struct MaterialKey : IEquatable<MaterialKey>
    {
        [FieldOffset(0)] public readonly SwfInstanceData.Types Type; // 1 byte
        [FieldOffset(1)] public readonly SwfBlendMode BlendMode; // 1 byte
        [FieldOffset(2)] public readonly Depth ClipDepth; // 2 bytes
        [FieldOffset(0)] public readonly int Hash;

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

        public bool Equals(MaterialKey other) => Hash == other.Hash;
        public override bool Equals(object obj) => obj is MaterialKey other && Equals(other);
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