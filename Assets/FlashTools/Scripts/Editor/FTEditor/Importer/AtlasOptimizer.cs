using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace FTEditor.Importer
{
    static class AtlasOptimizer
    {
        public static bool Optimize(int initialMaxSize, int shapePadding, string spriteFolder, [CanBeNull] out string newAtlasPath, out int newMaxSize)
        {
            var timestamp = System.DateTime.Now.ToString("yyyyMMddHHmmss");
            var guid = System.Guid.NewGuid().ToString().Replace("-", "");
            var sheetFormat = $"Temp/Atlas_{timestamp}_{guid}_{{0}}.png";

            var maxSize = initialMaxSize;
            var triedSizes = new Dictionary<int, bool>();
            var granularity = 64;
            const int batchSize = 32;

            while (true)
            {
                // create tests.
                var tests = new (int, bool)[batchSize]; // Parallelize this
                var testCount = 0;
                for (var i = 0; i < batchSize; i++)
                {
                    var size = maxSize - granularity * (i + 1);
                    if (size <= 0) break;
                    if (triedSizes.ContainsKey(size)) continue;
                    tests[testCount++] = (size, false);
                }
                tests = tests[..testCount];

                // run tests.
                Parallel.For(0, testCount, j =>
                {
                    var (size, _) = tests[j];
                    var sheetPath = string.Format(sheetFormat, size);
                    var success = PackAtlas(sheetPath, spriteFolder, size, shapePadding);
                    tests[j] = (size, success);
                });

                // check results.
                var found = false;
                for (var i = testCount - 1; i >= 0; i--) // find the smallest successful size.
                {
                    var (size, success) = tests[i];
                    if (success)
                    {
                        maxSize = size;
                        found = true;
                        break;
                    }
                    triedSizes[size] = true;
                }

                // if not found, reduce granularity by half.
                if (!found)
                {
                    granularity /= 2;
                    if (granularity == 0) break;
                }
            }

            if (maxSize < initialMaxSize)
            {
                L.I($"Atlas size has been optimized: {initialMaxSize} → {maxSize}");
                newAtlasPath = string.Format(sheetFormat, maxSize);
                newMaxSize = maxSize;
                return true;
            }
            else
            {
                L.I("Atlas size is already optimized.");
                newAtlasPath = null;
                newMaxSize = initialMaxSize;
                return false;
            }
        }

        static bool PackAtlas(string sheetPath, string spriteFolder, int maxSize, int shapePadding)
        {
            var dataPath = sheetPath.Replace(".png", ".tpsheet");

            try
            {
                TexturePackerUtils.Pack(sheetPath, dataPath, spriteFolder, maxSize, shapePadding);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}