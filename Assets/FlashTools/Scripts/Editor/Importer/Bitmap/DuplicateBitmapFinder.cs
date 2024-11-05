using System.Collections.Generic;
using System.Linq;
using FTRuntime;
using FTSwfTools;
using UnityEngine;

namespace FTEditor.Importer
{
    static class DuplicateBitmapFinder
    {
        public static BitmapRedirect[] Analyze(Dictionary<BitmapId, TextureData> textures, SwfInstanceData[] instances)
        {
            var duplicates = new List<BitmapRedirect>();
            FindDuplicateColorTextures(textures, duplicates);
            FindDuplicateMaskTextures(textures, instances, duplicates);
            BitmapRedirector.FlattenChain(duplicates);
            return duplicates.ToArray();
        }

        static void FindDuplicateColorTextures(Dictionary<BitmapId, TextureData> textures, List<BitmapRedirect> yield)
        {
            var textureList = textures.ToList();
            for (var i = 0; i < textureList.Count; i++)
            for (var j = i + 1; j < textureList.Count; j++)
            {
                var a = textureList[i];
                var b = textureList[j];
                if (Utils.ColorEquals(a.Value.Data, b.Value.Data))
                    yield.Add(new BitmapRedirect(a.Key, b.Key));
            }
        }

        // (BitmapId MaskBitmap, BitmapId ColorBitmap)
        static void FindDuplicateMaskTextures(
            Dictionary<BitmapId, TextureData> textures, SwfInstanceData[] instances, List<BitmapRedirect> yield)
        {
            var colorBitmaps = instances
                .Where(x => x.Type is SwfInstanceData.Types.Simple or SwfInstanceData.Types.Masked).Select(x => x.Bitmap)
                .ToHashSet();
            var maskBitmaps = instances
                .Where(x => x.Type is SwfInstanceData.Types.MaskIn or SwfInstanceData.Types.MaskOut)
                .Select(x => x.Bitmap)
                .Where(x => colorBitmaps.Contains(x) is false)
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
                var found = colorBitmapAlphaDict.FirstOrDefault(x => AlphaEquals(x.Value, maskOnlyAlpha));
                if (found.Key is not 0)
                {
                    L.I($"Found duplicate mask texture: {maskBitmap.ToName()} -> {found.Key.ToName()}");
                    yield.Add(new BitmapRedirect(maskBitmap, found.Key));
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