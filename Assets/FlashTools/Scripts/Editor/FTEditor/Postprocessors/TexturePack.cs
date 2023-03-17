using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public class TexturePack
{
    public static (Texture2D, RectInt[]) PackTextures(Texture2D[] textures, int atlasPadding)
    {
        var srcRects = textures.Select(
            x => new RectInt(0, 0, x.width + atlasPadding * 2, x.height + atlasPadding * 2)).ToList();
        var (size, dstRects) = BinPack(srcRects);

        for (var i = 0; i < dstRects.Length; i++)
        {
            var rect = dstRects[i];
            var offset = new Vector2Int(atlasPadding, atlasPadding);
            dstRects[i] = new RectInt(rect.min + offset, rect.size - 2 * offset);
        }

        var atlas = new Texture2D(size, size, TextureFormat.ARGB32, false);
        var colors = new Color32[size * size];
        Array.Clear(colors, 0, colors.Length);
        atlas.SetPixels32(colors);
        atlas.Apply();

        for (var i = 0; i < dstRects.Length; i++)
        {
            Graphics.CopyTexture(
                textures[i], 0, 0, 0, 0, textures[i].width, textures[i].height,
                atlas, 0, 0, dstRects[i].x, dstRects[i].y);
        }

        return (atlas, dstRects);
    }

    static (int, RectInt[]) BinPack(List<RectInt> srcRects)
    {
        var algorithms = new[]
        {
            MaxRectsBinPack.FreeRectChoiceHeuristic.RectBestShortSideFit, ///< -BSSF: Positions the rectangle against the short side of a free rectangle into which it fits the best.
            MaxRectsBinPack.FreeRectChoiceHeuristic.RectBestLongSideFit, ///< -BLSF: Positions the rectangle against the long side of a free rectangle into which it fits the best.
            MaxRectsBinPack.FreeRectChoiceHeuristic.RectBestAreaFit, ///< -BAF: Positions the rectangle into the smallest free rect into which it fits.
            MaxRectsBinPack.FreeRectChoiceHeuristic.RectBottomLeftRule, ///< -BL: Does the Tetris placement.
            MaxRectsBinPack.FreeRectChoiceHeuristic.RectContactPointRule ///< -CP: Choosest the placement where the rectangle touches other rects as much as possible.
        };

        var minSize = int.MaxValue;
        var dstRects = new RectInt[srcRects.Count];

        foreach (var algorithm in algorithms)
        {
            var tmpSize = 300;
            var tempDstRects = new RectInt[srcRects.Count];

            while (true)
            {
                var pack = new MaxRectsBinPack(tmpSize, tmpSize, false);
                var success = pack.Insert(srcRects, tempDstRects, algorithm);
                if (success) break;
                tmpSize += 4;
            }

            if (tmpSize < minSize)
            {
                minSize = tmpSize;
                dstRects = tempDstRects;
            }
        }

        Assert.AreNotEqual(int.MaxValue, minSize);
        return (minSize, dstRects);
    }
}