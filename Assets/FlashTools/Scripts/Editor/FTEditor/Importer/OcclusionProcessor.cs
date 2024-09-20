using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace FTEditor.Importer
{
    static class OcclusionProcessor
    {
        public static Dictionary<ushort, Texture2D> RemoveOccludedPixels(SwfFrameData[] frames, Dictionary<ushort, Texture2D> bitmaps)
        {
            var framebuffers = new Dictionary<Vector2Int, FramebufferPixel>[frames.Length];

            // Process each frame
            for (var i = 0; i < frames.Length; i++)
            {
                // Initialize framebuffer & mask stack
                var framebuffer = framebuffers[i] = new Dictionary<Vector2Int, FramebufferPixel>();
                var maskStack = new Stack<Mask>();

                foreach (var instance in frames[i].Instances)
                {
                    // Get the bitmap for this instance
                    var bitmap = bitmaps[instance.Bitmap];

                    // Process according to type
                    switch (instance.Type)
                    {
                        case SwfInstanceData.Types.Simple:
                        case SwfInstanceData.Types.Masked:
                        {
                            ProcessInstance(instance, bitmap, framebuffer, Mask.Composite(maskStack));
                            break;
                        }

                        case SwfInstanceData.Types.MaskIn:
                        {
                            var mask = CreateMask(instance, bitmap, Mask.Composite(maskStack));
                            maskStack.Push(mask);
                            break;
                        }

                        case SwfInstanceData.Types.MaskOut:
                            maskStack.Pop();
                            break;
                    }
                }
            }

            // Create visibility maps for each bitmap
            var visibilityMaps = new Dictionary<ushort, bool[,]>();
            foreach (var (bitmapId, bitmap) in bitmaps)
                visibilityMaps[bitmapId] = new bool[bitmap.width, bitmap.height];

            foreach (var framebuffer in framebuffers)
            foreach (var pixel in framebuffer.Values)
            {
                if (pixel.IsSingle)
                {
                    var p = pixel.SinglePosition;
                    visibilityMaps[pixel.SingleBitmap][p.x, p.y] = true;
                }
                else
                {
                    foreach (var (bitmap, p) in pixel.BlendingBitmaps)
                        visibilityMaps[bitmap][p.x, p.y] = true;
                }
            }

            // Now, modify the bitmaps based on visibility maps
            var resultBitmaps = new Dictionary<ushort, Texture2D>();
            foreach (var (bitmapId, bitmap) in bitmaps)
            {
                var visibilityMap = visibilityMaps[bitmapId];
                var newBitmap = new Texture2D(bitmap.width, bitmap.height, TextureFormat.RGBA32, false);

                var pixels = bitmap.GetPixels();
                var width = bitmap.width;
                var height = bitmap.height;
                for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                {
                    var index = y * width + x;
                    var pixel = pixels[index];
                    if (!visibilityMap[x, y]) pixel.a = 0;
                    newBitmap.SetPixel(x, y, pixel);
                }

                newBitmap.Apply();
                resultBitmaps[bitmapId] = newBitmap;
            }

            return resultBitmaps;
        }

        // Helper method to process an instance
        static void ProcessInstance(SwfInstanceData instance, Texture2D bitmap, Dictionary<Vector2Int, FramebufferPixel> framebuffer, Mask mask)
        {
            var tintAlpha = instance.TintAlpha;
            if (tintAlpha is 0) return; // Fully transparent instance, skip

            var noAlphaTint = tintAlpha is 1;
            var matrix = SwfToUnitMatrix(instance.Matrix);

            var bitmapPixels = bitmap.GetPixels();
            var width = bitmap.width;
            var height = bitmap.height;

            // For each pixel in the bitmap
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var index = y * width + x;
                var pixelColor = bitmapPixels[index];

                // Fully transparent pixel, skip
                if (pixelColor.a is 0)
                    continue;

                // Transform pixel position using instance's Matrix
                var framebufferPos = TransformPoint(x, y, matrix);

                // Pixel is masked out
                if (!mask.IsPixelInMask(framebufferPos))
                    continue;

                // Instances with TintAlpha != 1 cannot occlude pixels behind
                var replace = noAlphaTint && pixelColor.a == 1f;

                // Kernel size is 5x5
                for (var ox = -2; ox <= 2; ox++)
                for (var oy = -2; oy <= 2; oy++)
                {
                    // Check if pixel has same color as background.
                    var hasBg = framebuffer.TryGetValue(framebufferPos, out var fbPixel);
                    if (replace
                        && hasBg
                        && fbPixel.IsSingle
                        && fbPixel.SingleColor == pixelColor) // Only if both colors are same.
                    {
                        continue;
                    }

                    var single = !hasBg || replace;
                    var pos = framebufferPos + new Vector2Int(ox, oy);
                    framebuffer[framebufferPos] = single
                        ? FramebufferPixel.Single(instance.Bitmap, pos, pixelColor)
                        : fbPixel.Add(instance.Bitmap, pos);
                }
            }
        }

        static Vector2Int TransformPoint(int x, int y, Matrix4x4 m)
        {
            // Vector2Int version of Matrix4x4.MultiplyPoint
            Vector2 v;
            v.x = m.m00 * x + m.m01 * y + m.m03;
            v.y = m.m10 * x + m.m11 * y + m.m13;
            var num = 1f / (m.m30 * x + m.m31 * y + m.m33);
            v.x *= num;
            v.y *= num;
            return Vector2Int.RoundToInt(v);
        }

        static Matrix4x4 SwfToUnitMatrix(Matrix4x4 m)
        {
            // Convert Swf matrix to Unity matrix
            var scale = new Vector3(1f / ImportConfig.CustomScaleFactor, -1f / ImportConfig.CustomScaleFactor, 1f);
            return Matrix4x4.Scale(scale) * m;
        }

        // Helper method to create a mask from an instance
        static Mask CreateMask(SwfInstanceData instance, Texture2D bitmap, Mask parentMask)
        {
            var bitmapPixels = bitmap.GetPixels();
            var width = bitmap.width;
            var height = bitmap.height;

            var pixels = new List<Vector2Int>(width * height);
            var matrix = SwfToUnitMatrix(instance.Matrix);

            // For each pixel in the bitmap
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var index = y * width + x;
                var pixelColor = bitmapPixels[index];

                // Transparent pixel, not part of the mask
                if (pixelColor.a is 0)
                    continue;

                // Transform pixel position using instance's Matrix
                var maskPos = TransformPoint(x, y, matrix);

                // Pixel is not part of the parent mask
                if (!parentMask.IsPixelInMask(maskPos))
                    continue;

                pixels.Add(maskPos);
            }

            return new Mask(new HashSet<Vector2Int>(pixels));
        }

        // Classes to represent the framebuffer pixel and masks
        readonly struct FramebufferPixel
        {
            public readonly ushort SingleBitmap;
            public readonly Vector2Int SinglePosition;
            public readonly Color SingleColor;
            public readonly (ushort, Vector2Int)[] BlendingBitmaps;

            FramebufferPixel(ushort bitmap, Vector2Int position, Color color) : this()
            {
                SingleBitmap = bitmap;
                SinglePosition = position;
                SingleColor = color;
            }

            FramebufferPixel((ushort, Vector2Int)[] blendingBitmaps) : this() => BlendingBitmaps = blendingBitmaps;

            public static FramebufferPixel Single(ushort bitmap, Vector2Int position, Color color) => new(bitmap, position, color);

            public bool IsSingle => BlendingBitmaps is null;

            public FramebufferPixel Add(ushort bitmap, Vector2Int position)
            {
                if (BlendingBitmaps is null)
                    return new FramebufferPixel(new[] { (SingleBitmap, SinglePosition), (bitmap, position) });

                var oldLen = BlendingBitmaps.Length;
                var bitmaps = new (ushort, Vector2Int)[oldLen + 1];
                BlendingBitmaps.CopyTo(bitmaps, 0);
                bitmaps[oldLen] = (bitmap, position);
                return new FramebufferPixel(bitmaps);
            }
        }

        readonly struct Mask
        {
            [CanBeNull] readonly HashSet<Vector2Int> _pixels;

            public Mask(HashSet<Vector2Int> pixels) => _pixels = pixels;

            public bool IsPixelInMask(Vector2Int position)
                => _pixels is null || _pixels.Contains(position);

            public static Mask Composite(Stack<Mask> maskStack)
            {
                if (maskStack.Count is 0)
                    return default;

                var result = new HashSet<Vector2Int>();
                foreach (var mask in maskStack)
                {
                    if (mask._pixels is not null)
                        result.UnionWith(mask._pixels);
                }

                return result.Count is not 0
                    ? new Mask(result)
                    : default;
            }
        }
    }
}