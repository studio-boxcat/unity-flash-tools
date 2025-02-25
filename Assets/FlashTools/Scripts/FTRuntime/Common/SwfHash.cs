using JetBrains.Annotations;
using UnityEngine.Assertions;

namespace FT
{
    public static class SwfHash
    {
        [Pure]
        public static uint Hash([NotNull] string str)
        {
            // For C# Interactive mode.
            // System.Func<string, uint> hash = x => System.Linq.Enumerable.Aggregate(x, 5381u, (hash, c) => (hash << 5) + hash + (byte) c);

            uint hash = 5381;
            var i = 0;
            for (i = 0; i < str.Length; i++)
            {
                var c = str[i];
                Assert.IsTrue(c < 128, "Only ASCII characters are supported");
                hash = (hash << 5) + hash + (byte) c;
            }
            return hash;
        }

        public static SwfSequenceId SequenceId([NotNull] string str)
            => (SwfSequenceId) Hash(str);
    }
}