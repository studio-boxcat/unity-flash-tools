using System.Collections.Generic;
using System.IO;
using FTSwfTools.SwfTags;
using UnityEngine;

namespace FTEditor.Importer
{
    static class BitmapExporter
    {
        static string GetSpriteName(ushort bitmapId) => $"{bitmapId:D4}.png";

        public static TextureData LoadTextureFromData(IBitmapData data)
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

        public static void ExportBitmaps(Dictionary<ushort, TextureData> bitmaps, string dir)
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);

            foreach (var (bitmapId, data) in bitmaps)
            {
                var (width, height, orgColors) = data;
                var colors = RevertPremultipliedAlpha(orgColors);
                var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                tex.SetPixels(colors);
                File.WriteAllBytes($"{dir}/{GetSpriteName(bitmapId)}", tex.EncodeToPNG());
            }
            return;

            static Color[] RevertPremultipliedAlpha(Color32[] colors)
            {
                var len = colors.Length;
                var result = new Color[len];

                for (var i = 0; i < len; i++)
                {
                    // finalR = (r / 255f) / (a / 255f) = r / a
                    var c = colors[i];
                    var a = c.a;
                    var div = (a is 0 or 255) ? 255f : a;
                    result[i] = new Color(c.r / div, c.g / div, c.b / div, a / 255f);
                }

                return result;
            }
        }
    }
}