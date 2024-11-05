using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FTRuntime;
using UnityEditor;

namespace FTEditor.Importer
{
    static class AtlasOptimizer
    {
        public static bool Optimize(int initialMaxSize, int shapePadding, string spriteFolder, string outputSheetPath, out int newMaxSize)
        {
            var timestamp = System.DateTime.Now.ToString("yyyyMMddHHmmss");
            var guid = System.Guid.NewGuid().ToString().Replace("-", "");
            var sheetFormat = $"Temp/Atlas_{timestamp}_{guid}_";

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
                    var (sheetPath, dataPath) = FormatPath(sheetFormat, size);
                    var result = AtlasBuilder.ExecutePack(sheetPath, dataPath, spriteFolder, size, shapePadding);
                    tests[j] = (size, result);
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


            if (maxSize >= initialMaxSize)
            {
                L.I("Atlas size is already optimized.");
                newMaxSize = initialMaxSize;
                return false;
            }

            L.I($"Atlas size has been optimized: {initialMaxSize} → {maxSize}");
            var (finalSheetPath, finalDataPath) = FormatPath(sheetFormat, maxSize);

            // Copy texture as it is.
            File.Copy(finalSheetPath, outputSheetPath, true);

            // Replace the png name in the data file.
            var data = File.ReadAllText(finalDataPath);
            data = data.Replace(Path.GetFileName(finalSheetPath), Path.GetFileName(outputSheetPath));
            var outputDataPath = outputSheetPath.Replace(".png", ".tpsheet");
            File.WriteAllText(outputDataPath, data);

            AssetDatabase.ImportAsset(outputSheetPath);
            AssetDatabase.ImportAsset(outputDataPath);
            newMaxSize = maxSize;
            return true;

            static (string, string) FormatPath(string format, int size)
            {
                var sheetPath = format + size + ".png";
                var dataPath = format + size + ".tpsheet";
                return (sheetPath, dataPath);
            }
        }
    }
}