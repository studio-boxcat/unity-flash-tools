using System.Collections.Generic;
using System.Linq;
using FTRuntime;
using FTSwfTools;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace FTEditor.Importer
{
    class SwfClipImporter : ScriptableObject, ISelfValidator
    {
        [SerializeField, Required, AssetsOnly]
        public Object SwfFile;

        [BoxGroup("Bundle")]
        [SerializeField, Required, AssetsOnly]
        public Texture2D Atlas;
        [BoxGroup("Bundle")]
        [SerializeField, Required, AssetsOnly]
        public SwfClip Clip;
        [BoxGroup("Bundle")]
        [SerializeField, Required]
        public string BundleName; // lower case only.

        [BoxGroup("Pack Options"), SerializeField]
        public int AtlasMaxSize = 2048;
        [FormerlySerializedAs("AtlasExtrude")]
        [BoxGroup("Pack Options"), SerializeField]
        public int AtlasShapePadding = 2;

        [BoxGroup("Intermediates")]
        [SerializeField, Required, AssetsOnly, ListDrawerSettings(IsReadOnly = true)]
        public Mesh[] Meshes;
        [BoxGroup("Intermediates"), ListDrawerSettings(IsReadOnly = true)]
        public BitmapRedirect[] BitmapRedirects; // From -> To
        [BoxGroup("Intermediates"), ListDrawerSettings(IsReadOnly = true, ShowIndexLabels = true)]
        public MeshId[] BitmapToMesh; // BitmapId -> MeshId


        static string ResolveOutDir(Object refObj) => AssetDatabase.GetAssetPath(refObj)[..^6]; // .asset
        string ResolveOutDir() => ResolveOutDir(this);


        [Button(ButtonSizes.Medium), PropertySpace(8, 0)]
        void BuildAtlas()
        {
            L.I($"Building atlas for {SwfFile.name}...");

            // Parse swf
            var data = ParseSwfFile(SwfFile, out _, out var library);
            var orgInstances = data.SelectMany(x => x.Instances).ToArray();

            // Find used bitmaps
            var usedBitmaps = orgInstances.Select(x => x.Bitmap).ToHashSet();
            var bitmaps = library.GetBitmaps()
                .Where(x => usedBitmaps.Contains(x.Key))
                .ToDictionary(x => x.Key, x => BitmapExporter.CreateData(x.Value));

            // Remove duplicate textures
            var t = new TimeLogger("Removing duplicate textures");
            BitmapRedirects = DuplicateBitmapFinder.Analyze(bitmaps, orgInstances);
            BitmapRedirector.Process(data, BitmapRedirects);
            bitmaps.RemoveAll(BitmapRedirects.Select(x => x.From)); // Remove duplicates.
            t.Dispose();

            // Remove occluded pixels
            t = new TimeLogger("OcclusionProcessor.RemoveOccludedPixels");
            OcclusionProcessor.RemoveOccludedPixels(data, bitmaps);
            t.Dispose();

            // Flip & adjust pivot center
            t = new TimeLogger("FlipYAndAdjustPivotToCenter");
            var textures = bitmaps.ToDictionary(x => x.Key,
                x => BitmapExporter.CreateFlippedTexture(x.Value));
            t.Dispose();

            // Export bitmaps
            t = new TimeLogger("BitmapExporter.SaveAsPng");
            var outDir = ResolveOutDir();
            var spriteDir = outDir + "/Sprites~";
            BitmapExporter.SaveAsPng(textures, spriteDir);
            t.Dispose();

            // Pack atlas
            t = new TimeLogger("Build");
            var atlasPath = outDir + "/01.png";
            Atlas = AtlasBuilder.PackAtlas(atlasPath, spriteDir, AtlasMaxSize, AtlasShapePadding);
            if (Atlas == null)
            {
                L.W("Atlas packing failed. Trying to pack with larger size...");
                const int newMaxSize = 2048;
                var newAtlas = AtlasBuilder.PackAtlas(atlasPath, spriteDir, newMaxSize, AtlasShapePadding);
                if (newAtlas is not null)
                {
                    Atlas = newAtlas;
                    AtlasMaxSize = newMaxSize;
                }
            }
            t.Dispose();

            // Migrate sprite to mesh
            t = new TimeLogger("MigrateSpriteToMesh");
            Meshes = AtlasBuilder.MigrateSpriteToMesh(atlasPath, outDir, out BitmapToMesh);
            t.Dispose();

            L.I($"Atlas has been successfully built: {atlasPath}", Atlas);
        }

        [Button(ButtonSizes.Medium), EnableIf("Atlas")]
        void OptimizeAtlasSize()
        {
            var outDir = ResolveOutDir();
            var spriteDir = outDir + "/Sprites~";
            var atlasPath = outDir + "/01.png";

            var result = AtlasOptimizer.Optimize(AtlasMaxSize, AtlasShapePadding, spriteDir, atlasPath, out var newMaxSize);
            if (result is false) return;

            Atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
            AtlasMaxSize = newMaxSize;
            Meshes = AtlasBuilder.MigrateSpriteToMesh(atlasPath, outDir, out BitmapToMesh);
        }

        [Button(ButtonSizes.Medium), EnableIf("Atlas")]
        void BakeClip()
        {
            // load swf & bake
            var data = ParseSwfFile(SwfFile, frameRate: out var frameRate, out var library);
            BitmapRedirector.Process(data, BitmapRedirects);
            FlipYAndAdjustPivotToCenter(data, library.GetBitmaps().ToDictionary(x => x.Key, x => x.Value.Size));
            SwfClipBaker.Bake(data, Meshes, BitmapToMesh, out var frames, out var sequences);

            // configure
            CreateClipAssetIfNotExists(ref Clip, this);
            var asset = Clip;
            asset.FrameRate = frameRate;
            asset.Atlas = Atlas;
            asset.Frames = frames;
            asset.Sequences = sequences;
            asset.MeshCount = (ushort) Meshes.Length;
            Assert.IsTrue(asset.MeshCount < byte.MaxValue, "Too many meshes");
            SwfEditorUtils.DestroySubAssetsOfType(asset, typeof(Mesh)); // Legacy

            // save
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            L.I($"SwfAsset has been successfully converted:\nPath: {AssetDatabase.GetAssetPath(asset)}", asset);

            // update scene
            SwfEditorUtils.UpdateSceneSwfClips(asset);
            return;

            static void CreateClipAssetIfNotExists(ref SwfClip clipAsset, Object refObject)
            {
                if (clipAsset != null) return;

                clipAsset = CreateInstance<SwfClip>();
                AssetDatabase.CreateAsset(clipAsset, $"{ResolveOutDir(refObject)}/00.asset");
            }
        }

        [Button(ButtonSizes.Medium), EnableIf("Clip")]
        void Bundle()
        {
            Assert.IsFalse(string.IsNullOrEmpty(BundleName), "BundleName is empty");

            var assets = new List<Object> { Clip, Atlas };
            assets.AddRange(Meshes);

            var addressableNames = assets.Select(x => x.name).ToArray();
            addressableNames[0] = BundleName; // override clip name to bundle name

            var build = new AssetBundleBuild
            {
                assetBundleName = BundleName,
                assetNames = assets.Select(AssetDatabase.GetAssetPath).ToArray(),
                addressableNames = addressableNames,
            };

            var manifest = BuildPipeline.BuildAssetBundles(
                "Assets/StreamingAssets/SwfClips",
                new[] { build },
                BuildAssetBundleOptions.DisableLoadAssetByFileNameWithExtension,
                BuildTarget.iOS);

            L.I($"manifest: dep={string.Join(", ", manifest.GetAllDependencies(BundleName))}");
            Assert.AreEqual(0, manifest.GetAllDependencies(BundleName).Length);
        }

        void ISelfValidator.Validate(SelfValidationResult result)
        {
            if (SwfFile != null)
            {
                var path = AssetDatabase.GetAssetPath(SwfFile);
                if (path.EndsWith(".swf") is false)
                    result.AddError("SwfFile must be a .swf file");
            }

            foreach (var c in BundleName)
            {
                if (c is >= 'a' and <= 'z' or >= '0' and <= '9') continue;
                result.AddError("BundleName must be lower case");
                break;
            }

            if (AtlasMaxSize == 2048)
                result.AddError("Atlas has never been optimized.");
        }

        static SwfFrameData[] ParseSwfFile(Object swfFile, out byte frameRate, out SwfLibrary library)
            => SwfParser.Load(AssetDatabase.GetAssetPath(swfFile), out frameRate, out library);

        static void FlipYAndAdjustPivotToCenter(SwfFrameData[] frames, Dictionary<BitmapId, Vector2Int> bitmapSizes)
        {
            foreach (var frame in frames)
            {
                var insts = frame.Instances;
                for (var index = 0; index < insts.Length; index++)
                {
                    var inst = insts[index];
                    var size = bitmapSizes[inst.Bitmap];
                    insts[index] = inst.WithMatrix(
                        inst.Matrix
                        * SwfMatrix.Scale(1, -1)
                        * SwfMatrix.Translate(size.x / 2f, -size.y / 2f));
                }
            }
        }
    }
}