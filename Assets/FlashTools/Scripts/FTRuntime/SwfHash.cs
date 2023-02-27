using JetBrains.Annotations;

namespace FTRuntime
{
    public static class SwfHash
    {
        [Pure]
        public static uint Hash([NotNull] string str)
        {
            ulong hash = 5381;
            var i = 0;
            for (i = 0; i < str.Length; i++)
                hash = ((hash << 5) + hash) + ((byte) str[i]);
            return unchecked((uint) hash.GetHashCode());
        }
    }
}