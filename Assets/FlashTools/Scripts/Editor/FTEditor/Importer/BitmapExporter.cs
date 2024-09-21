using System.Collections.Generic;
using System.IO;
using FTSwfTools.SwfTags;
using UnityEngine;

namespace FTEditor.Importer
{
    static class BitmapExporter
    {
        public static string GetSpriteName(ushort bitmapId) => $"{bitmapId:D4}.png";

        public static TextureData CreateData(IBitmapData data)
        {
            var size = data.Size;
            var colors = ToColor32(data.ToARGB32());
            return new TextureData(size.x, size.y, colors);

            static Color32[] ToColor32(byte[] argb)
            {
                var len = argb.Length;
                var count = len / 4;
                var colors = new Color32[count];

                for (var i = 0; i < len; i += 4)
                {
                    colors[i / 4] = new Color32(
                        argb[i + 1],
                        argb[i + 2],
                        argb[i + 3],
                        argb[i + 0]);
                }

                return colors;
            }
        }

        public static Texture2D CreateFlippedTexture(TextureData data)
        {
            var (width, height, orgColors) = data;

            var len = orgColors.Length;
            var colors = new Color[len];

            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var i = y * width + x;
                var j = (height - y - 1) * width + x;
                colors[j] = RevertPremultipliedAlpha(orgColors[i]);
            }

            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.SetPixels(colors);
            return tex;

            static Color RevertPremultipliedAlpha(Color32 c)
            {
                // finalR = (r / 255f) / (a / 255f) = r / a
                var a = c.a;
                var div = (a is 0 or 255) ? 255f : a;
                return new Color(c.r / div, c.g / div, c.b / div, a / 255f);
            }
        }

        public static void SaveAsPng(Dictionary<ushort, Texture2D> bitmaps, string dir)
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);

            foreach (var (bitmapId, tex) in bitmaps)
                File.WriteAllBytes($"{dir}/{GetSpriteName(bitmapId)}", tex.EncodeToPNG());
        }
    }
}