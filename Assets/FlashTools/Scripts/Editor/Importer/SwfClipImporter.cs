using System.Collections.Generic;
using System.Linq;
using Boxcat.Bundler;
using Boxcat.Bundler.Editor;
using FTRuntime;
using FTSwfTools;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace FTEditor.Importer
{
    internal class SwfClipImporter : ScriptableObject, ISelfValidator, IBundleTarget
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
        public Mesh Mesh;
        [BoxGroup("Intermediates"), ListDrawerSettings(IsReadOnly = true)]
        public BitmapRedirect[] BitmapRedirects; // From -> To
        [BoxGroup("Intermediates"), ListDrawerSettings(IsReadOnly = true, ShowIndexLabels = true)]
        public MeshId[] BitmapToMesh; // BitmapId -> MeshId


        private static string ResolveOutDir(Object refObj) => AssetDatabase.GetAssetPath(refObj)[..^6]; // .asset
        private string ResolveOutDir() => ResolveOutDir(this);


        [Button(ButtonSizes.Medium), PropertySpace(8, 0)]
        private void BuildAtlas()
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
            var (atlasPath, spriteDir) = ResolveAtlasDirs();
            BitmapExporter.SaveAsPng(textures, spriteDir);
            t.Dispose();

            // Pack atlas
            t = new TimeLogger("Build");
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
            Utils.GetOrCreateAsset(ref Mesh, Atlas, "02.asset");
            AtlasBuilder.MigrateSpriteToMesh(atlasPath, Mesh, out BitmapToMesh);
            t.Dispose();

            L.I($"Atlas has been successfully built: {atlasPath}", Atlas);
        }

        [Button(ButtonSizes.Medium), EnableIf("Atlas")]
        private void OptimizeAtlasSize()
        {
            var (atlasPath, spriteDir) = ResolveAtlasDirs();
            var result = AtlasOptimizer.Optimize(AtlasMaxSize, AtlasShapePadding, spriteDir, atlasPath, out var newMaxSize);
            if (result is false) return;

            Atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
            AtlasMaxSize = newMaxSize;
            Utils.GetOrCreateAsset(ref Mesh, Atlas, "02.asset");
            AtlasBuilder.MigrateSpriteToMesh(atlasPath, Mesh, out BitmapToMesh);
        }

        private (string AtlasPath, string SpriteDir) ResolveAtlasDirs()
        {
            var outDir = ResolveOutDir();
            var spriteDir = outDir + "/Sprites~";
            var atlasPath = outDir + "/01.png";
            return (atlasPath, spriteDir);
        }

        [Button(ButtonSizes.Medium), EnableIf("Atlas")]
        private void BakeClip()
        {
            // load swf & bake
            var data = ParseSwfFile(SwfFile, frameRate: out var frameRate, out var library);
            BitmapRedirector.Process(data, BitmapRedirects);
            FlipYAndAdjustPivotToCenter(data, library.GetBitmaps().ToDictionary(x => x.Key, x => x.Value.Size));
            SwfClipBaker.Bake(data, Mesh, BitmapToMesh, out var frames, out var sequences);

            // configure
            Utils.GetOrCreateAsset(ref Clip, Atlas, BundleName + ".asset");
            var asset = Clip;
            asset.FrameRate = frameRate;
            asset.Atlas = Atlas;
            asset.Frames = frames;
            asset.Sequences = sequences;
            asset.Mesh = Mesh;
            SwfEditorUtils.DestroySubAssetsOfType(asset, typeof(Mesh)); // Legacy

            // save
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            L.I($"SwfAsset has been successfully converted:\nPath: {AssetDatabase.GetAssetPath(asset)}", asset);

            // update scene
            SwfEditorUtils.UpdateSceneSwfClips(asset);
        }

        [Button(ButtonSizes.Medium), EnableIf("Clip")]
        private void Bundle() => AssetBundleBuilder.BundleAsSingleAssetBundle(this);

        void ISelfValidator.Validate(SelfValidationResult result)
        {
            // swf file
            if (SwfFile != null)
            {
                var path = AssetDatabase.GetAssetPath(SwfFile);
                if (path.EndsWith(".swf") is false)
                    result.AddError("SwfFile must be a .swf file");
            }

            // atlas
            {
                var ti = (TextureImporter) AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(Atlas));
                if (ti.textureType != TextureImporterType.Default)
                    result.AddError("Atlas must be a default texture type");
                if (AtlasMaxSize == 2048)
                    result.AddError("Atlas has never been optimized.");
            }

            // bundle name
            if (BundlerUtils.VerifyBundleKey(BundleName) is false)
                result.AddError("Invalid bundle name: " + BundleName);
        }

        private static SwfFrameData[] ParseSwfFile(Object swfFile, out byte frameRate, out SwfLibrary library)
            => SwfParser.Load(AssetDatabase.GetAssetPath(swfFile), out frameRate, out library);

        private static void FlipYAndAdjustPivotToCenter(SwfFrameData[] frames, Dictionary<BitmapId, Vector2Int> bitmapSizes)
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

        AssetBundleKey IBundleTarget.BundleKey => BundlerUtils.ParseBundleName(BundleName);
        IEnumerable<(Object Asset, AssetIndex AssetIndex)> IBundleTarget.GetAssets()
            => new (Object, AssetIndex)[] { (Clip, SwfClip.AssetIndex) };
    }
}