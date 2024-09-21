using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

namespace FTEditor.Importer
{
    static class OcclusionProcessor
    {
        public static void RemoveOccludedPixels(SwfFrameData[] frames, Dictionary<ushort, TextureData> bitmaps)
        {
            var framebuffers = new Dictionary<Vector2Int, FramebufferPixel>[frames.Length];

            // Process each frame
            Parallel.For(0, frames.Length, i =>
            {
                // Initialize framebuffer & mask stack
                var framebuffer = framebuffers[i] = new Dictionary<Vector2Int, FramebufferPixel>();
                var maskStack = new Stack<Mask>();
                var mask = Mask.CreateEmpty();

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
                            ProcessInstance(instance, bitmap, framebuffer, mask);
                            break;
                        }

                        case SwfInstanceData.Types.MaskIn:
                        {
                            mask = CreateMask(instance, bitmap, parentMask: mask);
                            maskStack.Push(mask);
                            break;
                        }

                        case SwfInstanceData.Types.MaskOut:
                            maskStack.Pop();
                            mask = maskStack.Count is not 0
                                ? maskStack.Peek()
                                : Mask.CreateEmpty();
                            break;
                    }
                }
            });

            // Create visibility maps for each bitmap
            var visibilityMaps = new Dictionary<ushort, bool[,]>();
            foreach (var (bitmapId, bitmap) in bitmaps)
                visibilityMaps[bitmapId] = new bool[bitmap.Width, bitmap.Height];

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
            Parallel.ForEach(bitmaps, d =>
            {
                var (bitmapId, bitmap) = d;
                var (width, _, pixels) = bitmap;

                var visibilityMap = visibilityMaps[bitmapId];
                visibilityMap = ReduceOcclusionArtifacts(visibilityMap); // Expand visibility maps to avoid occlusion artifacts

                for (var i = 0; i < pixels.Length; i++)
                {
                    var x = i % width;
                    var y = i / width;
                    if (!visibilityMap[x, y])
                        pixels[i].a = 0;
                }
            });
        }

        // Helper method to process an instance
        static void ProcessInstance(SwfInstanceData instance, TextureData bitmap, Dictionary<Vector2Int, FramebufferPixel> framebuffer, Mask mask)
        {
            const int kernelSize = 3; // 3x3 kernel

            var tintAlpha = instance.TintAlpha;
            if (tintAlpha is 0) return; // Fully transparent instance, skip

            var noAlphaTint = tintAlpha is 1;
            var matrix = SwfToUnitMatrix(instance.Matrix);

            // For each pixel in the bitmap
            var (width, height, bitmapPixels) = bitmap;
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var index = y * width + x;
                var pixelColor = bitmapPixels[index];

                // Fully transparent pixel, skip
                if (pixelColor.a is 0)
                    continue;

                // Instances with alpha cannot occlude pixels behind
                var replace = noAlphaTint && pixelColor.a is 255;

                const int range = kernelSize / 2;
                for (var dy = -range; dy <= range; dy++)
                for (var dx = -range; dx <= range; dx++)
                {
                    // Transform pixel position using instance's Matrix
                    var framebufferPos = TransformPoint(x, y, dx * 0.5f, dy * 0.5f, matrix);

                    // Pixel is masked out
                    if (!mask.IsPixelInMask(framebufferPos))
                        continue;

                    // Check if pixel has same color as background.
                    var hasBg = framebuffer.TryGetValue(framebufferPos, out var fbPixel);
                    var single = !hasBg || replace;
                    var pixelPos = new Vector2Int(x, y);
                    framebuffer[framebufferPos] = single
                        ? FramebufferPixel.Single(instance.Bitmap, pixelPos)
                        : fbPixel.Add(instance.Bitmap, pixelPos);
                }
            }
        }

        // Bitmap space -> Framebuffer space
        static Vector2Int TransformPoint(float x, float y, float ox, float oy, Matrix4x4 m)
        {
            // Vector2Int version of Matrix4x4.MultiplyPoint
            Vector2 v;
            v.x = m.m00 * x + m.m01 * y + m.m03;
            v.y = m.m10 * x + m.m11 * y + m.m13;
            var num = 1f / (m.m30 * x + m.m31 * y + m.m33);
            v.x *= num;
            v.y *= num;
            v.x += ox;
            v.y += oy;
            return Vector2Int.RoundToInt(v);
        }

        static Matrix4x4 SwfToUnitMatrix(Matrix4x4 m)
        {
            // Convert Swf matrix to Unity matrix
            var scale = new Vector3(1f / ImportConfig.CustomScaleFactor, -1f / ImportConfig.CustomScaleFactor, 1f);
            return Matrix4x4.Scale(scale) * m;
        }

        // Helper method to create a mask from an instance
        static Mask CreateMask(SwfInstanceData instance, TextureData bitmap, Mask parentMask)
        {
            const int kernelSize = 3; // 3x3 kernel

            var (width, height, bitmapPixels) = bitmap;
            var pixels = new List<Vector2Int>(width * height * kernelSize * kernelSize);
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

                const int range = kernelSize / 2;
                for (var dy = -range; dy <= range; dy++)
                for (var dx = -range; dx <= range; dx++)
                {
                    // Add pixel to mask if it's not clipped by parent mask
                    var maskPos = TransformPoint(x, y, dx * 0.5f, dy * 0.5f, matrix);
                    if (parentMask.IsPixelInMask(maskPos))
                        pixels.Add(maskPos);
                }
            }

            return new Mask(new HashSet<Vector2Int>(pixels));
        }

        static bool[,] ReduceOcclusionArtifacts(bool[,] visibilityMap)
        {
            const int kernelSize = 7; // 7x7 kernel

            var width = visibilityMap.GetLength(0);
            var height = visibilityMap.GetLength(1);
            var newVisibilityMap = new bool[width, height];

            for (var x = 0; x < width; x++)
            for (var y = 0; y < height; y++)
            {
                var center = visibilityMap[x, y];
                if (center)
                {
                    newVisibilityMap[x, y] = true;
                    continue;
                }

                const int range = kernelSize / 2;
                for (var ox = -range; ox <= range; ox++)
                for (var oy = -range; oy <= range; oy++)
                {
                    if (ox is 0 && oy is 0)
                        continue;

                    var nx = x + ox;
                    var ny = y + oy;
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                        continue;

                    if (visibilityMap[nx, ny])
                    {
                        newVisibilityMap[x, y] = true;
                        break;
                    }
                }
            }

            return newVisibilityMap;
        }

        // Classes to represent the framebuffer pixel and masks
        readonly struct FramebufferPixel
        {
            public readonly ushort SingleBitmap;
            public readonly Vector2Int SinglePosition;
            public readonly (ushort, Vector2Int)[] BlendingBitmaps;

            FramebufferPixel(ushort bitmap, Vector2Int position) : this()
            {
                SingleBitmap = bitmap;
                SinglePosition = position;
            }

            FramebufferPixel((ushort, Vector2Int)[] blendingBitmaps) : this() => BlendingBitmaps = blendingBitmaps;

            public static FramebufferPixel Single(ushort bitmap, Vector2Int position) => new(bitmap, position);

            public bool IsSingle => BlendingBitmaps is null;

            public FramebufferPixel Add(ushort bitmap, Vector2Int position)
            {
                if (BlendingBitmaps is null)
                {
                    // If the new pixel is same as the existing one, return self
                    if (SingleBitmap == bitmap && SinglePosition == position)
                        return this;
                    return new FramebufferPixel(new[] { (SingleBitmap, SinglePosition), (bitmap, position) });
                }

                foreach (var (existingBitmap, existingPosition) in BlendingBitmaps)
                {
                    // If the new pixel is same as an existing one, return self
                    if (existingBitmap == bitmap && existingPosition == position)
                        return this;
                }

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

            public static Mask CreateEmpty() => default;
        }
    }
}