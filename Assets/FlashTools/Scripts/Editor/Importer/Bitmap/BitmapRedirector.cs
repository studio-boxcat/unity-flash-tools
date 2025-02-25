using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace FTEditor.Importer
{
    [Serializable, InlineProperty]
    internal struct BitmapRedirect
    {
        [HideLabel, HorizontalGroup, DisplayAsString]
        public BitmapId From;
        [HideLabel, HorizontalGroup, DisplayAsString]
        public BitmapId To;

        public BitmapRedirect(BitmapId from, BitmapId to)
        {
            From = from;
            To = to;
        }

        public void Deconstruct(out BitmapId a, out BitmapId b)
        {
            a = From;
            b = To;
        }

        public override string ToString() => $"[{From}, {To}]";
    }

    internal static class BitmapRedirector
    {
        public static void Process(SwfFrameData[] frames, BitmapRedirect[] map)
        {
            foreach (var (from, to) in map) // Replace A with To
            foreach (var data in frames)
            {
                var instances = data.Instances;
                for (var index = 0; index < instances.Length; index++)
                {
                    var instData = instances[index];
                    if (instData.Bitmap == from)
                        instances[index] = instData.WithBitmap(to);
                }
            }
        }

        // Iterate until no chain found. (1 -> 2, 2 -> 3 should be replaced with 1 -> 3)
        public static void FlattenChain(List<BitmapRedirect> pairs)
        {
            while (true)
            {
                var replaced = false;

                for (var i = 0; i < pairs.Count; i++)
                {
                    var (a, b1) = pairs[i];
                    for (var j = 0; j < pairs.Count; j++)
                    {
                        if (i == j) continue;
                        var (b2, c) = pairs[j];
                        if (b1 != b2) continue;
                        pairs[i] = new BitmapRedirect(a, c);
                        replaced = true;
                        break;
                    }
                }

                if (replaced is false)
                    break;
            }
        }
    }
}