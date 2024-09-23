using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FTRuntime;
using FTRuntime.Internal;
using FTSwfTools;
using FTSwfTools.SwfTypes;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace FTEditor.Importer
{
    class ClipImporter : ScriptableObject, ISelfValidator
    {
        [BoxGroup("Objects")]
        [SerializeField, Required, AssetsOnly]
        public Object SwfFile;
        [BoxGroup("Objects"), InfoBox("$AtlasSizeHint", visibleIfMemberName: "Atlas")]
        [SerializeField, Required, ChildGameObjectsOnly]
        public Texture2D Atlas;
        [BoxGroup("Objects")]
        [SerializeField, Required, AssetsOnly]
        public SwfClipAsset ClipAsset;

        [BoxGroup("Pack Options"), SerializeField]
        public int AtlasMaxSize = 2048;
        [FormerlySerializedAs("AtlasExtrude")]
        [BoxGroup("Pack Options"), SerializeField]
        public int AtlasShapePadding = 2;


        string GetSwfPath()
        {
            var path = AssetDatabase.GetAssetPath(SwfFile);
            Assert.IsTrue(path.EndsWith(".swf"));
            return path;
        }

        [Button(ButtonSizes.Medium), PropertySpace(8, 0)]
        void BuildAtlas()
        {
            L.I($"Building atlas for {SwfFile.name}...");

            // Parse swf
            var frames = ParseSwfFile(SwfFile, out var library);
            var instances = frames.SelectMany(x => x.Instances).ToArray();

            // Find used bitmaps
            var usedBitmaps = instances.Select(x => x.Bitmap).ToHashSet();
            var bitmaps = library.GetBitmaps()
                .Where(x => usedBitmaps.Contains(x.Key))
                .ToDictionary(x => x.Key, x => BitmapExporter.CreateData(x.Value));

            // Remove duplicate textures
            var t = new TimeLogger("Removing duplicate textures");
            var duplicateTextures = DuplicateTextureFinder.Analyze(bitmaps, instances);
            foreach (var (bitmapA, bitmapB) in duplicateTextures) // Replace A with B
            {
                foreach (var instData in instances)
                {
                    if (instData.Bitmap == bitmapA)
                        instData.Bitmap = bitmapB;
                }
                bitmaps.Remove(bitmapA);
                L.I($"Duplicate bitmap {bitmapA} has been replaced with {bitmapB}");
            }
            t.Dispose();

            // Remove occluded pixels
            t = new TimeLogger("OcclusionProcessor.RemoveOccludedPixels");
            OcclusionProcessor.RemoveOccludedPixels(frames, bitmaps);
            t.Dispose();

            // Flip & adjust pivot center
            t = new TimeLogger("FlipYAndAdjustPivotToCenter");
            var textures = bitmaps.ToDictionary(x => x.Key,
                x => BitmapExporter.CreateFlippedTexture(x.Value));
            t.Dispose();

            // Export bitmaps
            t = new TimeLogger("BitmapExporter.SaveAsPng");
            var swfPath = GetSwfPath();
            var exportDir = swfPath.Replace(".swf", "_Sprites~");
            BitmapExporter.SaveAsPng(textures, exportDir);
            t.Dispose();

            // Redirect duplicated textures
            foreach (var (bitmapA, bitmapB) in duplicateTextures)
            {
                var srcPath = Path.Combine(exportDir, BitmapExporter.GetSpriteName(bitmapB));
                var dstPath = Path.Combine(exportDir, BitmapExporter.GetSpriteName(bitmapA));
                File.Copy(srcPath, dstPath, true);
            }

            // Pack atlas
            t = new TimeLogger("PackAtlas");
            var sheetPath = swfPath.Replace(".swf", ".png");
            Atlas = PackAtlas(sheetPath, exportDir, AtlasMaxSize, AtlasShapePadding);
            if (Atlas == null)
            {
                L.W("Atlas packing failed. Trying to pack with larger size...");
                const int newMaxSize = 2048;
                var newAtlas = PackAtlas(sheetPath, exportDir, newMaxSize, AtlasShapePadding);
                if (newAtlas is not null)
                {
                    Atlas = newAtlas;
                    AtlasMaxSize = newMaxSize;
                }
            }
            t.Dispose();

            L.I($"Atlas has been successfully built: {sheetPath}", Atlas);
        }

        [Button(ButtonSizes.Medium), EnableIf("Atlas")]
        void OptimizeAtlasSize()
        {
            var swfPath = GetSwfPath();
            var spriteFolder = swfPath.Replace(".swf", "_Sprites~");

            var sheetPath = swfPath.Replace(".swf", ".png");
            var dataPath = swfPath.Replace(".swf", ".tpsheet");
            if (AtlasOptimizer.Optimize(AtlasMaxSize, AtlasShapePadding, spriteFolder, sheetPath, dataPath, out var newMaxSize))
            {
                Atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(sheetPath);
                AtlasMaxSize = newMaxSize;
            }
        }

        [Button(ButtonSizes.Medium), EnableIf("Atlas")]
        void BakeClip()
        {
            // load swf and atlas
            var frames = ParseSwfFile(SwfFile, frameRate: out var frameRate, out var library);
            FlipYAndAdjustPivotToCenter(frames, library.GetBitmaps().ToDictionary(x => x.Key, x => x.Value.Size));

            // bake
            var atlasDef = AtlasDef.FromTexture(Atlas);
            var sequences = ClipBaker.Bake(frames, atlasDef, out var meshes, out var materialGroups);

            // configure
            var asset = ClipAsset;
            asset.FrameRate = frameRate;
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

        [Button(ButtonSizes.Medium), EnableIf("Atlas")]
        void SpawnGameObject()
        {
            var frames = ParseSwfFile(SwfFile, out var library);
            FlipYAndAdjustPivotToCenter(frames, library.GetBitmaps().ToDictionary(x => x.Key, x => x.Value.Size));

            var propBlock = new MaterialPropertyBlock();
            propBlock.SetTexture(SwfUtils.MainTexShaderProp, Atlas);
            propBlock.SetColor(SwfUtils.TintShaderProp, Color.white);

            var atlasDef = AtlasDef.FromTexture(Atlas);

            var go = new GameObject(name);

            for (var index = 0; index < frames.Length; index++)
            {
                var frameGO = new GameObject(index.ToString("D3"), typeof(SortingGroup));
                frameGO.transform.SetParent(go.transform, false);

                var frame = frames[index];
                for (var i = 0; i < frame.Instances.Length; i++)
                {
                    var inst = frame.Instances[i];
                    var instGO = new GameObject($"{i:D3}:B{inst.Bitmap:D3}:T{inst.Type}",
                        typeof(MeshFilter), typeof(MeshRenderer));
                    instGO.transform.SetParent(frameGO.transform, false);

                    var spriteData = atlasDef[inst.Bitmap];
                    instGO.GetComponent<MeshFilter>().sharedMesh = CreateMesh(spriteData,
                        inst.Matrix, inst.ColorTrans.CalculateMul().a);

                    var mr = instGO.GetComponent<MeshRenderer>();
                    mr.sharedMaterial = SwfMaterialCache.Query(inst.GetMaterialKey());
                    mr.SetPropertyBlock(propBlock);
                }
            }
            return;

            static Mesh CreateMesh(SpriteData spriteData, SwfMatrix matrix, float a)
            {
                matrix = InstanceBatcher.GetVertexMatrix(matrix);
                var mesh = new Mesh();
                mesh.vertices = spriteData.Poses.Select(pos => (Vector3) matrix.MultiplyPoint(pos)).ToArray();
                mesh.SetUVs(0, spriteData.UVs.Select(x => new Vector3(x.x, x.y, a)).ToArray());
                mesh.SetTriangles(spriteData.Indices, 0);
                mesh.UploadMeshData(false);
                return mesh;
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

            if (Atlas != null && ClipAsset != null && ClipAsset.Atlas != Atlas)
            {
                // both atlas data should be same. (aspect ratio, file size)
                var atlas1 = Atlas;
                var atlas2 = ClipAsset.Atlas;

                // compare texture size
                var a1 = atlas1.width / atlas1.height;
                var a2 = atlas2.width / atlas2.height;
                if (a1 != a2)
                    result.AddError($"Atlas aspect ratio mismatch: {a1:F2} vs {a2:F2}");

                // compare file size
                var p1 = AssetDatabase.GetAssetPath(atlas1);
                var p2 = AssetDatabase.GetAssetPath(atlas2);
                var f1 = new FileInfo(p1).Length;
                var f2 = new FileInfo(p2).Length;
                if (f1 != f2)
                    result.AddError($"Atlas file size mismatch: {f1} vs {f2}");
            }

            if (AtlasMaxSize == 2048)
                result.AddError("Atlas has never been optimized.");
        }

        static string AtlasSizeHint(Texture2D tex)
        {
            var size = $"{tex.width}x{tex.height}";
            var sqrt = Mathf.Sqrt(tex.width * tex.height);
            return $"Size: {size}, Sqrt: {sqrt:F1}";
        }

        static Texture2D PackAtlas(string sheetPath, string spriteFolder, int maxSize, int shapePadding)
        {
            var dataPath = sheetPath.Replace(".png", ".tpsheet");

            try
            {
                TexturePackerUtils.Pack(sheetPath, dataPath, spriteFolder, maxSize, shapePadding);
                AssetDatabase.ImportAsset(sheetPath);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(sheetPath);
            }
            catch (Exception e)
            {
                L.E("Failed to pack atlas: " + Path.GetFileName(sheetPath));
                L.E(e);
                return null;
            }
        }

        static SwfFrameData[] ParseSwfFile(Object swfFile, out float frameRate, out SwfLibrary library)
        {
            var t = new TimeLogger("SwfParser.Parse");
            var fileData = SwfParser.Parse(AssetDatabase.GetAssetPath(swfFile));
            frameRate = fileData.FrameRate;
            var symbols = SwfParser.LoadSymbols(fileData.Tags, out library);
            var symbol = symbols.Single(x => x.Name is not SwfParser.stage_symbol);
            var frames = symbol.Frames;
            t.Dispose();
            return frames;
        }

        static SwfFrameData[] ParseSwfFile(Object swfFile, out SwfLibrary library)
            => ParseSwfFile(swfFile, out _, out library);

        static void FlipYAndAdjustPivotToCenter(SwfFrameData[] frames, Dictionary<BitmapId, Vector2Int> bitmapSizes)
        {
            foreach (var swfFrameData in frames)
            foreach (var inst in swfFrameData.Instances)
            {
                var size = bitmapSizes[inst.Bitmap];
                inst.Matrix =
                    inst.Matrix
                    * SwfMatrix.Scale(1, -1)
                    * SwfMatrix.Translate(size.x / 2f, -size.y / 2f);
            }
        }
    }
}