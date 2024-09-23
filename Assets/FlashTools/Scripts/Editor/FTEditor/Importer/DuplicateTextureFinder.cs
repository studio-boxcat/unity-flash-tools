using System.Collections.Generic;
using System.Linq;
using FTSwfTools.SwfTypes;
using UnityEngine;

namespace FTEditor.Importer
{
    static class DuplicateTextureFinder
    {
        public static List<(BitmapId, BitmapId)> Analyze(Dictionary<BitmapId, TextureData> textures, SwfInstanceData[] instances)
        {
            var duplicateTextures = FindDuplicateTextures(textures)
                .Concat(FindDuplicateMaskTextures(textures, instances))
                .ToList();

            // Iterate until no chain of duplicates found. (1 -> 2, 2 -> 3 should be replaced with 1 -> 3)
            while (true)
            {
                var replaced = false;

                for (var i = 0; i < duplicateTextures.Count; i++)
                {
                    var (bitmapA, bitmapB1) = duplicateTextures[i];
                    for (var j = 0; j < duplicateTextures.Count; j++)
                    {
                        if (i == j) continue;
                        var (bitmapB2, bitmapC) = duplicateTextures[j];
                        if (bitmapB1 != bitmapB2) continue;
                        duplicateTextures[i] = (bitmapA, bitmapC);
                        replaced = true;
                        break;
                    }
                }

                if (replaced is false)
                    break;
            }

            return duplicateTextures;
        }

        static IEnumerable<(BitmapId BitmapA, BitmapId BitmapB)> FindDuplicateTextures(Dictionary<BitmapId, TextureData> textures)
        {
            var textureList = textures.ToList();
            for (var i = 0; i < textureList.Count; i++)
            for (var j = i + 1; j < textureList.Count; j++)
            {
                var a = textureList[i];
                var b = textureList[j];
                if (ColorEquals(a.Value.Data, b.Value.Data))
                    yield return (a.Key, b.Key);
            }
            yield break;

            static bool ColorEquals(Color32[] a, Color32[] b)
            {
                var len = a.Length;
                for (var i = 0; i < len; i++)
                {
                    if (a[i].r != b[i].r) return false;
                    if (a[i].g != b[i].g) return false;
                    if (a[i].b != b[i].b) return false;
                    if (a[i].a != b[i].a) return false;
                }

                return true;
            }
        }

        static IEnumerable<(BitmapId MaskBitmap, BitmapId RenderBitmap)> FindDuplicateMaskTextures(
            Dictionary<BitmapId, TextureData> textures, SwfInstanceData[] instances)
        {
            var renderBitmaps = instances.Where(x => x.Type is SwfInstanceData.Types.Simple or SwfInstanceData.Types.Masked).Select(x => x.Bitmap).ToHashSet();
            var maskBitmaps = instances
                .Where(x => x.Type is SwfInstanceData.Types.MaskIn or SwfInstanceData.Types.MaskOut)
                .Select(x => x.Bitmap)
                .Where(x => renderBitmaps.Contains(x) is false)
                .ToHashSet();
            L.I("Render bitmaps: " + string.Join(", ", renderBitmaps.Select(x => x.ToName())));
            L.I("Mask bitmaps: " + string.Join(", ", maskBitmaps.Select(x => x.ToName())));

            if (maskBitmaps.Count is 0)
            {
                L.I("No mask bitmaps found.");
                yield break;
            }

            // find any texture exists with same alpha.
            var renderBitmapAlphaDict = renderBitmaps.ToDictionary(x => x, x => textures[x].Data.Select(y => y.a).ToArray());
            foreach (var maskBitmap in maskBitmaps)
            {
                var maskOnlyTexture = textures[maskBitmap];
                var maskOnlyAlpha = maskOnlyTexture.Data.Select(x => x.a).ToArray();
                var found = renderBitmapAlphaDict.FirstOrDefault(x => AlphaEquals(x.Value, maskOnlyAlpha));
                if (found.Key is not 0)
                {
                    L.I($"Found duplicate mask texture: {maskBitmap.ToName()} -> {found.Key.ToName()}");
                    yield return (maskBitmap, found.Key);
                }
            }
            yield break;

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