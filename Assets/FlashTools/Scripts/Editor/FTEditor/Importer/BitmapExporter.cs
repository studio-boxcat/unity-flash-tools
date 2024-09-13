using System.Collections.Generic;
using System.IO;
using FTSwfTools.SwfTags;
using UnityEngine;

namespace FTEditor.Importer
{
    static class BitmapExporter
    {
        public static void ExportBitmaps(Dictionary<ushort, IBitmapData> bitmaps, string dir)
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
                Directory.CreateDirectory(dir);
            }

            foreach (var (bitmapId, bitmapData) in bitmaps)
            {
                var tex = LoadTextureFromData(bitmapData);
                File.WriteAllBytes($"{dir}/{bitmapId:D4}.png", tex.EncodeToPNG());
            }
            return;

            static Texture2D LoadTextureFromData(IBitmapData data)
            {
                var size = data.Size;
                var texture = new Texture2D(size.x, size.y, TextureFormat.ARGB32, false);
                texture.LoadRawTextureData(data.ToARGB32());
                RevertTexturePremultipliedAlpha(texture);
                return texture;
            }

            static void RevertTexturePremultipliedAlpha(Texture2D texture)
            {
                var pixels = texture.GetPixels();
                for (var i = 0; i < pixels.Length; ++i)
                {
                    var c = pixels[i];
                    if (c.a > 0)
                    {
                        c.r /= c.a;
                        c.g /= c.a;
                        c.b /= c.a;
                    }
                    pixels[i] = c;
                }
                texture.SetPixels(pixels);
                texture.Apply();
            }
        }
    }
}