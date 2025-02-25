using System.Collections.Generic;
using System.Linq;
using FTRuntime;
using UnityEngine;

namespace FTEditor.Importer
{
    internal static class DuplicateBitmapFinder
    {
        public static BitmapRedirect[] Analyze(Dictionary<BitmapId, TextureData> textures, SwfInstanceData[] instances)
        {
            var duplicates = new List<BitmapRedirect>();
            FindDuplicateColorTextures(textures, instances, duplicates);
            FindDuplicateMaskTextures(textures, instances, duplicates);
            BitmapRedirector.FlattenChain(duplicates);
            return duplicates.ToArray();
        }

        private static void FindDuplicateColorTextures(Dictionary<BitmapId, TextureData> textures, SwfInstanceData[] instances, List<BitmapRedirect> yield)
        {
            var bitmaps = instances
                .Where(x => !x.Type.IsMask())
                .Select(x => x.Bitmap)
                .Distinct()
                .ToArray();

            for (var i = 0; i < bitmaps.Length; i++)
            for (var j = i + 1; j < bitmaps.Length; j++)
            {
                var ba = bitmaps[i];
                var bb = bitmaps[j];
                var ta = textures[ba];
                var tb = textures[bb];
                if (Utils.ColorEquals(ta.Data, tb.Data))
                {
                    L.I($"Found duplicate color textures: {ba.ToName()} -> {bb.ToName()}");
                    yield.Add(new BitmapRedirect(ba, bb));
                }
            }
        }

        // (BitmapId MaskBitmap, BitmapId ColorBitmap)
        private static void FindDuplicateMaskTextures(
            Dictionary<BitmapId, TextureData> textures, SwfInstanceData[] instances, List<BitmapRedirect> yield)
        {
            var colorBitmaps = instances
                .Where(x => !x.Type.IsMask())
                .Select(x => x.Bitmap)
                .ToHashSet();
            var maskBitmaps = instances
                .Where(x => x.Type.IsMask())
                .Select(x => x.Bitmap)
                .Where(x => colorBitmaps.Contains(x) is false) // if the mask is used as color for at least once, consider it as color.
                .ToHashSet();
            L.I("Render bitmaps: " + string.Join(", ", colorBitmaps.Select(x => x.ToName())));
            L.I("Mask bitmaps: " + string.Join(", ", maskBitmaps.Select(x => x.ToName())));

            if (maskBitmaps.Count is 0)
            {
                L.I("No mask bitmaps found.");
                return;
            }

            // find any texture exists with same alpha.
            var colorBitmapAlphaDict = colorBitmaps.ToDictionary(x => x, x => textures[x].Data.Select(y => y.a).ToArray());
            foreach (var maskBitmap in maskBitmaps)
            {
                var maskOnlyTexture = textures[maskBitmap];
                var maskOnlyAlpha = maskOnlyTexture.Data.Select(x => x.a).ToArray();
                var found = colorBitmapAlphaDict.FirstOrDefault(x => AlphaEquals(x.Value, maskOnlyAlpha)).Key;
                if (found is not 0)
                {
                    L.I($"Found duplicate mask texture: {maskBitmap.ToName()} -> {found.ToName()}");
                    yield.Add(new BitmapRedirect(maskBitmap, found));
                }
            }
            return;

            static bool AlphaEquals(byte[] a, byte[] b)
            {
                const byte deltaThreshold = 100;
                const byte avgThreshold = 3;

                if (a.Length != b.Length)
                    return false;

                float deltaSum = 0;
                for (var i = 0; i < a.Length; i++)
                {
                    var delta = Mathf.Abs(a[i] - b[i]);
                    if (delta > deltaThreshold) return false;
                    deltaSum += delta;
                }

                var avgDelta = deltaSum / a.Length;
                return avgDelta < avgThreshold;
            }
        }
    }
}