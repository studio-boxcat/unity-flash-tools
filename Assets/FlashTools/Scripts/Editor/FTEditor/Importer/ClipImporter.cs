using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FTRuntime;
using FTRuntime.Internal;
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
            var t = new TimeLogger("SwfParser.Parse");
            var fileData = SwfParser.Parse(AssetDatabase.GetAssetPath(SwfFile));
            var symbols = SwfParser.LoadSymbols(fileData.Tags, out var library);
            var frames = symbols.Single(x => x.Name is not SwfParser.stage_symbol).Frames;
            var instances = frames.SelectMany(x => x.Instances).ToArray();
            t.Dispose();

            // Find used bitmaps
            var usedBitmaps = instances.Select(x => x.Bitmap).ToHashSet();
            var bitmaps = library.GetBitmaps()
                .Where(x => usedBitmaps.Contains(x.Key))
                .ToDictionary(x => x.Key, x => BitmapExporter.CreateData(x.Value));

            // Remove duplicate textures
            t = new TimeLogger("Removing duplicate textures");
            var duplicateTextures = AnalyzeDuplicateTextures(bitmaps, instances);
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
            var sheetPath = swfPath.Replace(".swf", ".png");
            var spriteFolder = swfPath.Replace(".swf", "_Sprites~");

            var oldMaxSize = AtlasMaxSize;
            var maxSize = AtlasMaxSize;
            var triedSizes = new Dictionary<int, bool>();
            byte[] granularitySeries = { 64, 32, 16, 8, 4, 2, 1 };
            foreach (var granularity in granularitySeries)
            {
                var testSize = maxSize;
                var failedCount = 0;
                while (true)
                {
                    testSize -= granularity;
                    if (testSize <= 0) break;

                    if (triedSizes.TryGetValue(testSize, out var prevResult))
                    {
                        if (prevResult is false) failedCount++;
                        continue;
                    }

                    L.I($"Trying to pack atlas with size {testSize}...");

                    var atlas = PackAtlas(sheetPath, spriteFolder, testSize, AtlasShapePadding);
                    if (atlas is not null)
                    {
                        Atlas = atlas;
                        maxSize = testSize;
                        failedCount = 0;
                        continue;
                    }

                    failedCount++;
                    if (failedCount >= 10)
                        break;
                }
            }

            if (maxSize != oldMaxSize)
            {
                L.I($"Atlas size has been optimized: {oldMaxSize} → {maxSize}");
                AtlasMaxSize = maxSize;
                EditorUtility.SetDirty(this);
            }
            else
            {
                L.I("Atlas size is already optimized.");
            }
        }

        [Button(ButtonSizes.Medium), EnableIf("Atlas")]
        void BakeClip()
        {
            // load swf and atlas
            var fileData = SwfParser.Parse(AssetDatabase.GetAssetPath(SwfFile));
            var symbols = SwfParser.LoadSymbols(fileData.Tags, out var library);

            var symbol = symbols.Single(x => x.Name is not SwfParser.stage_symbol);
            var frames = symbol.Frames;
            FlipYAndAdjustPivotToCenter(frames, library.GetBitmaps().ToDictionary(x => x.Key, x => x.Value.Size));

            // bake
            var atlasDef = AtlasDef.FromTexture(Atlas);
            var sequences = ClipBaker.Bake(symbol, atlasDef, out var meshes, out var materialGroups);

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

        [Button(ButtonSizes.Medium), EnableIf("Atlas")]
        void SpawnGameObject()
        {
            var fileData = SwfParser.Parse(AssetDatabase.GetAssetPath(SwfFile));
            var symbols = SwfParser.LoadSymbols(fileData.Tags, out _);
            var symbol = symbols.Single(x => x.Name is not SwfParser.stage_symbol);
            var atlasDef = AtlasDef.FromTexture(Atlas);

            var propBlock = new MaterialPropertyBlock();
            propBlock.SetTexture(SwfUtils.MainTexShaderProp, Atlas);
            propBlock.SetColor(SwfUtils.TintShaderProp, Color.white);

            var go = new GameObject(name);

            for (var index = 0; index < symbol.Frames.Length; index++)
            {
                var frameGO = new GameObject(index.ToString("D3"), typeof(SortingGroup));
                frameGO.transform.SetParent(go.transform, false);

                var frame = symbol.Frames[index];
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

            static Mesh CreateMesh(SpriteData spriteData, Matrix4x4 matrix, float a)
            {
                matrix = InstanceBatcher.GetVertexMatrix(matrix);
                var mesh = new Mesh();
                mesh.vertices = spriteData.Poses.Select(pos => matrix.MultiplyPoint3x4(pos)).ToArray();
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
                // both atlas data should be same. (texture size, file size)
                var atlas1 = Atlas;
                var atlas2 = ClipAsset.Atlas;

                // compare texture size
                var w1 = atlas1.width;
                var h1 = atlas1.height;
                var w2 = atlas2.width;
                var h2 = atlas2.height;
                if (w1 != w2 || h1 != h2)
                    result.AddError($"Atlas size mismatch: {w1}x{h1} vs {w2}x{h2}");

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

        static List<(ushort, ushort)> AnalyzeDuplicateTextures(Dictionary<ushort, TextureData> textures, SwfInstanceData[] instances)
        {
            var duplicateTextures = FindDuplicateTextures(textures)
                .Concat(FindDuplicateMaskTextures(textures, instances))
                .ToList();

            // Iterate until no chain of duplicates found. (1 -> 2, 2 -> 3 should be replaced with 1 -> 3)
            while (true)
            {
                var replaced = false;

                for (var i = 0; i < duplicateTextures.Count; i++)
                {
                    var (bitmapA, bitmapB1) = duplicateTextures[i];
                    for (var j = 0; j < duplicateTextures.Count; j++)
                    {
                        if (i == j) continue;
                        var (bitmapB2, bitmapC) = duplicateTextures[j];
                        if (bitmapB1 != bitmapB2) continue;
                        duplicateTextures[i] = (bitmapA, bitmapC);
                        replaced = true;
                        break;
                    }
                }

                if (replaced is false)
                    break;
            }

            return duplicateTextures;
        }

        static IEnumerable<(ushort BitmapA, ushort BitmapB)> FindDuplicateTextures(Dictionary<ushort, TextureData> textures)
        {
            var textureList = textures.ToList();
            for (var i = 0; i < textureList.Count; i++)
            for (var j = i + 1; j < textureList.Count; j++)
            {
                var a = textureList[i];
                var b = textureList[j];
                if (ColorEquals(a.Value.Data, b.Value.Data))
                    yield return (a.Key, b.Key);
            }
            yield break;

            static bool ColorEquals(Color32[] a, Color32[] b)
            {
                var len = a.Length;
                for (var i = 0; i < len; i++)
                {
                    if (a[i].r != b[i].r) return false;
                    if (a[i].g != b[i].g) return false;
                    if (a[i].b != b[i].b) return false;
                    if (a[i].a != b[i].a) return false;
                }

                return true;
            }
        }

        static IEnumerable<(ushort MaskBitmap, ushort RenderBitmap)> FindDuplicateMaskTextures(
            Dictionary<ushort, TextureData> textures, SwfInstanceData[] instances)
        {
            var renderBitmaps = instances.Where(x => x.Type is SwfInstanceData.Types.Simple or SwfInstanceData.Types.Masked).Select(x => x.Bitmap).ToHashSet();
            var maskBitmaps = instances
                .Where(x => x.Type is SwfInstanceData.Types.MaskIn or SwfInstanceData.Types.MaskOut)
                .Select(x => x.Bitmap)
                .Where(x => renderBitmaps.Contains(x) is false)
                .ToHashSet();
            L.I("Render bitmaps: " + string.Join(", ", renderBitmaps.Select(x => x.ToString("D4"))));
            L.I("Mask bitmaps: " + string.Join(", ", maskBitmaps.Select(x => x.ToString("D4"))));

            if (maskBitmaps.Count is 0)
            {
                L.I("No mask bitmaps found.");
                yield break;
            }

            // find any texture exists with same alpha.
            var renderBitmapAlphaDict = renderBitmaps.ToDictionary(x => x, x => textures[x].Data.Select(y => y.a).ToArray());
            foreach (var maskBitmap in maskBitmaps)
            {
                var maskOnlyTexture = textures[maskBitmap];
                var maskOnlyAlpha = maskOnlyTexture.Data.Select(x => x.a).ToArray();
                var found = renderBitmapAlphaDict.FirstOrDefault(x => AlphaEquals(x.Value, maskOnlyAlpha));
                if (found.Key is not 0)
                {
                    L.I($"Found duplicate mask texture: {maskBitmap:D4} -> {found.Key:D4}");
                    yield return (maskBitmap, found.Key);
                }
            }
            yield break;

            static bool AlphaEquals(byte[] a, byte[] b)
            {
                const byte deltaThreshold = 100;
                const byte avgThreshold = 3;

                if (a.Length != b.Length)
                    return false;

                float deltaSum = 0;
                for (var i = 0; i < a.Length; i++)
                {
                    var delta = Mathf.Abs(a[i] - b[i]);
                    if (delta > deltaThreshold) return false;
                    deltaSum += delta;
                }

                var avgDelta = deltaSum / a.Length;
                return avgDelta < avgThreshold;
            }
        }

        static void FlipYAndAdjustPivotToCenter(SwfFrameData[] frames, Dictionary<ushort, Vector2Int> bitmapSizes)
        {
            foreach (var swfFrameData in frames)
            foreach (var inst in swfFrameData.Instances)
            {
                var size = bitmapSizes[inst.Bitmap];
                inst.Matrix =
                    inst.Matrix
                    * Matrix4x4.Scale(new Vector3(1, -1, 1))
                    * Matrix4x4.Translate(new Vector3(size.x / 2f, -size.y / 2f, 0));
            }
        }

        readonly struct TimeLogger
        {
            readonly Stopwatch _sw;
            readonly string _subject;

            public TimeLogger(string subject)
            {
                _sw = Stopwatch.StartNew();
                _subject = subject;
            }

            public void Dispose()
            {
                _sw.Stop();
                L.I($"{_subject}: {_sw.ElapsedMilliseconds}ms");
            }
        }
    }
}