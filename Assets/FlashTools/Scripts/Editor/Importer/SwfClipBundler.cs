using System.IO;
using System.Linq;
using FTRuntime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using AssetBundleBuild = UnityEditor.AssetBundleBuild;

namespace FTEditor.Importer
{
    public static class SwfClipBundler
    {
        public static void BundleAll()
        {
            BundleAllPlatforms(AssetDatabase.FindAssets($"t:{nameof(SwfClipImporter)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<SwfClipImporter>)
                .ToArray());
        }

        internal static void Bundle(SwfClipImporter ci)
        {
            BundleAllPlatforms(new[] { ci });
        }

        static void BundleAllPlatforms(SwfClipImporter[] target)
        {
            foreach (var ci in target)
                ci.hideFlags = HideFlags.DontUnloadUnusedAsset; // prevent unloading during build.

            Bundle(target, BuildTarget.iOS);
            Bundle(target, BuildTarget.Android);
        }

        static void Bundle(SwfClipImporter[] target, BuildTarget buildTarget)
        {
            // check eligibility.
            if (target.Length is 0)
            {
                L.W("No SwfClipImporter found.");
                return;
            }

            // prepare output directory.
            var outDir = SwfClipLoader.GetBuildDir(buildTarget);
            Assert.IsTrue(outDir.EndsWith('/'), "outDir must end with /");
            if (Directory.Exists(outDir) is false)
                Directory.CreateDirectory(outDir);

            // build asset bundles.
            L.I($"Building {target.Length} bundles to {outDir}...\n" +
                $"builds={string.Join(", ", target.Select(x => x.name))}, target={buildTarget}");
            var builds = target.Select(ToBuildScheme).ToArray();
            var manifest = BuildPipeline.BuildAssetBundles(outDir, builds,
                BuildAssetBundleOptions.DisableLoadAssetByFileNameWithExtension,
                buildTarget);
            L.I($"Built {target.Length} bundles.");

            // verify manifest.
            var allDeps = builds.Select(x => x.assetBundleName).SelectMany(manifest.GetAllDependencies).ToArray();
            L.I($"manifest: dep={string.Join(", ", allDeps)}");
            Assert.AreEqual(0, allDeps.Length);

            // delete unnecessary files.
            File.Delete(outDir + Path.GetFileName(outDir[..^1])); // delete the file same name with the directory.
            foreach (var file in Directory.GetFiles(outDir, "*.manifest")) File.Delete(file); // delete all manifest files.

            return;

            static AssetBundleBuild ToBuildScheme(SwfClipImporter ci)
            {
                Assert.IsFalse(string.IsNullOrEmpty(ci.BundleName), "BundleName is empty");

                return new AssetBundleBuild
                {
                    assetBundleName = ci.BundleName,
                    assetNames = new[] { AssetDatabase.GetAssetPath(ci.Clip) },
                    addressableNames = new[] { ci.BundleName } // override clip name to bundle name
                };
            }
        }
    }
}