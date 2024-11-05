using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using FTRuntime;
using FTSwfTools;
using JetBrains.Annotations;
using UnityEngine;

namespace FTEditor.Importer
{
    static class OcclusionProcessor
    {
        public static void RemoveOccludedPixels(SwfFrameData[] frames, Dictionary<BitmapId, TextureData> bitmaps)
        {
            var framebuffers = new Dictionary<Vector2Int, FramebufferPixel>[frames.Length];
            var maskBitmaps = new ConcurrentBag<BitmapId>();

            // Process each frame
            var t = new TimeLogger("Processing frames");
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
                            maskBitmaps.Add(instance.Bitmap);
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
            t.Dispose();


            // Create visibility maps for each bitmap
            t = new TimeLogger("Building visibility maps");
            var visibilityMaps = BuildVisibilityMap(
                framebuffers, bitmaps, maskBitmaps.ToHashSet());
            t.Dispose();


            // Now, modify the bitmaps based on visibility maps
            t = new TimeLogger("Modifying bitmaps");
            Parallel.ForEach(bitmaps, d =>
            {
                var (bitmapId, bitmap) = d;
                var (width, _, pixels) = bitmap;

                var visibilityMap = visibilityMaps[bitmapId];

                for (var i = 0; i < pixels.Length; i++)
                {
                    var x = i % width;
                    var y = i / width;
                    if (!visibilityMap[x, y])
                        pixels[i].a = 0;
                }
            });
            t.Dispose();
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

                var center = matrix.MultiplyPoint(x, y); // Bitmap space -> Framebuffer space
                var replace = noAlphaTint && pixelColor.a is 255; // Instances with alpha cannot occlude pixels behind
                const int range = kernelSize / 2;
                for (var dy = -range; dy <= range; dy++)
                for (var dx = -range; dx <= range; dx++)
                {
                    // Transform pixel position using instance's Matrix
                    var framebufferPos = new Vector2Int(
                        Mathf.RoundToInt(center.x + dx * 0.5f),
                        Mathf.RoundToInt(center.y + dy * 0.5f));

                    // Pixel is masked out
                    if (!mask.IsPixelInMask(framebufferPos))
                        continue;

                    // Check if pixel has same color as background.
                    var p = new FramebufferPixel.Pixel(instance.Bitmap, (ushort) x, (ushort) y);
                    framebuffer[framebufferPos] = replace
                        ? new FramebufferPixel(p)
                        : framebuffer.TryGetValue(framebufferPos, out var existing)
                            ? existing.Add(p)
                            : new FramebufferPixel(p);
                }
            }
        }

        static SwfMatrix SwfToUnitMatrix(SwfMatrix m)
        {
            // Convert Swf matrix to Unity matrix
            var scale = 1f / ImportConfig.CustomScaleFactor;
            return SwfMatrix.Scale(scale, -scale) * m;
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

                var center = matrix.MultiplyPoint(x, y); // Bitmap space -> Framebuffer space
                const int range = kernelSize / 2;
                for (var dy = -range; dy <= range; dy++)
                for (var dx = -range; dx <= range; dx++)
                {
                    // Add pixel to mask if it's not clipped by parent mask
                    var maskPos = new Vector2Int(
                        Mathf.RoundToInt(center.x + dx * 0.5f),
                        Mathf.RoundToInt(center.y + dy * 0.5f));
                    if (parentMask.IsPixelInMask(maskPos))
                        pixels.Add(maskPos);
                }
            }

            return new Mask(new HashSet<Vector2Int>(pixels));
        }

        static Dictionary<BitmapId, bool[,]> BuildVisibilityMap(Dictionary<Vector2Int, FramebufferPixel>[] framebuffers, Dictionary<BitmapId, TextureData> bitmaps, HashSet<BitmapId> maskBitmaps)
        {
            var visibilityMaps = new Dictionary<BitmapId, bool[,]>();
            foreach (var (bitmapId, bitmap) in bitmaps)
            {
                if (maskBitmaps.Contains(bitmapId)) continue;
                visibilityMaps[bitmapId] = new bool[bitmap.Width, bitmap.Height];
            }

            foreach (var framebuffer in framebuffers)
            foreach (var pixel in framebuffer.Values)
            {
                if (pixel.IsSingle)
                {
                    var p = pixel.Single;
                    if (maskBitmaps.Contains(p.Bitmap)) continue;
                    visibilityMaps[p.Bitmap][p.X, p.Y] = true;
                }
                else
                {
                    foreach (var p in pixel.Multiple)
                    {
                        if (maskBitmaps.Contains(p.Bitmap)) continue;
                        visibilityMaps[p.Bitmap][p.X, p.Y] = true;
                    }
                }
            }

            // Expand visibility maps to avoid occlusion artifacts
            visibilityMaps = visibilityMaps.ToDictionary(
                x => x.Key,
                x => ReduceOcclusionArtifacts(x.Value));

            // Add mask bitmaps to visibility maps
            // Mask bitmaps are always fully visible.
            foreach (var maskBitmap in maskBitmaps)
            {
                var (width, height, _) = bitmaps[maskBitmap];
                var visibilityMap = visibilityMaps[maskBitmap] = new bool[width, height];
                for (var x = 0; x < width; x++)
                for (var y = 0; y < height; y++)
                    visibilityMap[x, y] = true;
            }

            return visibilityMaps;
        }

        static bool[,] ReduceOcclusionArtifacts(bool[,] visibilityMap)
        {
            const int kernelSize = 12;

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
                        ox = range + 1; // Exit outer loop.
                        break;
                    }
                }
            }

            return newVisibilityMap;
        }

        // Classes to represent the framebuffer pixel and masks
        readonly struct FramebufferPixel
        {
            [StructLayout(LayoutKind.Explicit)]
            public struct Pixel
            {
                [FieldOffset(0)]
                public readonly ulong Data;
                [FieldOffset(0)]
                public readonly BitmapId Bitmap;
                [FieldOffset(2)]
                public readonly ushort X;
                [FieldOffset(4)]
                public readonly ushort Y;

                public Pixel(BitmapId bitmap, ushort x, ushort y) : this()
                {
                    Bitmap = bitmap;
                    X = x;
                    Y = y;
                }
            }

            public readonly Pixel Single;
            public readonly Pixel[] Multiple;

            public FramebufferPixel(Pixel single) : this() => Single = single;
            FramebufferPixel(Pixel[] multiple) : this() => Multiple = multiple;

            public bool IsSingle => Multiple is null;

            public FramebufferPixel Add(Pixel p)
            {
                if (Multiple is null)
                {
                    return Single.Data == p.Data
                        ? this // If the new pixel is same as the existing one, return self
                        : new FramebufferPixel(new[] { Single, p });
                }

                foreach (var existing in Multiple)
                {
                    // If the new pixel is same as an existing one, return self
                    if (existing.Data == p.Data)
                        return this;
                }

                var oldLen = Multiple.Length;
                var bitmaps = new Pixel[oldLen + 1];
                Multiple.CopyTo(bitmaps, 0);
                bitmaps[oldLen] = p;
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