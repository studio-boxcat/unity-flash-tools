using System;
using UnityEngine.Assertions;

namespace FTRuntime
{
    public enum MeshId : ushort { }

    static class MeshIdUtils
    {
        internal static string ToName(this MeshId id)
        {
            var index = (ushort) id;
            Assert.IsTrue(index < 26 * 26, "Too many objects");
            Span<char> c = stackalloc char[2];
            c[0] = (char) ('A' + index / 26);
            c[1] = (char) ('A' + index % 26);
            return new string(c);
        }

#if UNITY_EDITOR
        public static ushort ToPrimitive(this MeshId id) => (ushort) id;

        public static MeshId ToIndex(string name)
        {
            Assert.IsTrue(name.Length == 2, "Invalid name: " + name);
            return (MeshId) ((name[0] - 'A') * 26 + (name[1] - 'A'));
        }
#endif
    }
}