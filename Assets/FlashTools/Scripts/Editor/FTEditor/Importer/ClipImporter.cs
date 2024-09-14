using System.IO;
using System.Linq;
using FTRuntime;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace FTEditor.Importer
{
    class ClipImporter : ScriptableObject, ISelfValidator
    {
        [BoxGroup("Objects")]
        [SerializeField, Required, AssetsOnly]
        public Object SwfFile;
        [BoxGroup("Objects")]
        [SerializeField, Required, ChildGameObjectsOnly]
        public Texture2D Atlas;
        [BoxGroup("Objects")]
        [SerializeField, Required, AssetsOnly]
        public SwfClipAsset ClipAsset;

        [BoxGroup("Pack Options"), SerializeField]
        public int AtlasMaxSize = 1024;
        [BoxGroup("Pack Options"), SerializeField]
        public int AtlasExtrude = 2;


        [ButtonGroup, Button(ButtonSizes.Medium)]
        void BuildAtlas()
        {
            L.I($"Building atlas for {SwfFile.name}...");

            // Parse swf
            var fileData = SwfParser.Parse(AssetDatabase.GetAssetPath(SwfFile));
            var symbols = SwfParser.LoadSymbols(fileData.Tags, out var library);

            // Find used bitmaps
            var symbol = symbols.Single(x => x.Name is not SwfParser.stage_symbol);
            var usedBitmaps = symbol.Frames.SelectMany(x => x.Instances).Select(x => x.Bitmap).Distinct().ToHashSet();
            var bitmaps = library.GetBitmaps()
                .Where(x => usedBitmaps.Contains(x.Key))
                .ToDictionary(x => x.Key, x => x.Value);

            // Export bitmaps
            var swfPath = AssetDatabase.GetAssetPath(SwfFile);
            Assert.IsTrue(swfPath.EndsWith(".swf"));
            var exportDir = swfPath.Replace(".swf", "_Sprites~");
            BitmapExporter.ExportBitmaps(bitmaps, exportDir);

            // Pack atlas
            var sheetPath = swfPath.Replace(".swf", ".png");
            var dataPath = swfPath.Replace(".swf", ".tpsheet");
            TexturePackerUtils.Pack(sheetPath, dataPath, exportDir, AtlasMaxSize, AtlasExtrude);
            AssetDatabase.ImportAsset(sheetPath);
            Atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(sheetPath);

            L.I($"Atlas has been successfully built: {sheetPath}", Atlas);
        }

        [ButtonGroup, Button(ButtonSizes.Medium), EnableIf("Atlas")]
        void BakeClip()
        {
            // load swf and atlas
            var fileData = SwfParser.Parse(AssetDatabase.GetAssetPath(SwfFile));
            var symbols = SwfParser.LoadSymbols(fileData.Tags, out _);

            // bake
            var symbol = symbols.Single(x => x.Name is not SwfParser.stage_symbol);
            var atlasDef = AtlasDef.FromTexture(Atlas);
            ClipBaker.Bake(symbol, atlasDef, out var sequences, out var meshes, out var materialGroups);

            // configure
            var asset = ClipAsset;
            asset.FrameRate = fileData.FrameRate;
            asset.Atlas = PublishTexture(Atlas);
            asset.Sequences = sequences;
            asset.MaterialGroups = materialGroups;

            // recreate mesh assets.
            SwfEditorUtils.DestroySubAssetsOfType(asset, typeof(Mesh));
            foreach (var mesh in meshes) AssetDatabase.AddObjectToAsset(mesh, asset);

            // save
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            L.I($"SwfAsset has been successfully converted:\nPath: {AssetDatabase.GetAssetPath(asset)}", asset);

            // update scene
            SwfEditorUtils.UpdateSceneSwfClips(asset);
            return;

            static Texture2D PublishTexture(Texture2D src)
            {
                // Copy texture
                var srcPath = AssetDatabase.GetAssetPath(src);
                var dstPath = srcPath.Replace(".png", " (Authored).png");
                File.Copy(srcPath, dstPath, true);
                AssetDatabase.ImportAsset(dstPath);

                // Configure texture
                var ti = (TextureImporter) AssetImporter.GetAtPath(dstPath);
                ti.textureType = TextureImporterType.Default;
                ti.filterMode = FilterMode.Bilinear;
                ti.alphaIsTransparency = true;
                ti.mipmapEnabled = false;
                AssetDatabase.ImportAsset(dstPath);

                // Load published texture
                return AssetDatabase.LoadAssetAtPath<Texture2D>(dstPath);
            }
        }

        void ISelfValidator.Validate(SelfValidationResult result)
        {
            if (SwfFile != null)
            {
                var path = AssetDatabase.GetAssetPath(SwfFile);
                if (path.EndsWith(".swf") is false)
                    result.AddError("SwfFile must be a .swf file");
            }
        }
    }
}