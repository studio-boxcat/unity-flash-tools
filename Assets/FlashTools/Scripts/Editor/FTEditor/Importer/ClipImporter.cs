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
                .ToDictionary(x => x.Key, x => BitmapExporter.LoadTextureFromData(x.Value));

            // Remove duplicate mask textures
            t = new TimeLogger("Removing duplicate mask textures");
            foreach (var (maskBitmap, renderBitmap) in FindDuplicateMaskTextures(instances, bitmaps))
            {
                foreach (var instData in instances)
                {
                    if (instData.Bitmap == maskBitmap)
                        instData.Bitmap = renderBitmap;
                }
                bitmaps.Remove(maskBitmap);
                L.I($"Mask only bitmap {maskBitmap} has been replaced with {renderBitmap}");
            }
            t.Dispose();

            // Remove occluded pixels
            t = new TimeLogger("OcclusionProcessor.RemoveOccludedPixels");
            OcclusionProcessor.RemoveOccludedPixels(frames, bitmaps);
            t.Dispose();

            // Export bitmaps
            t = new TimeLogger("BitmapExporter.ExportBitmaps");
            var swfPath = GetSwfPath();
            var exportDir = swfPath.Replace(".swf", "_Sprites~");
            BitmapExporter.ExportBitmaps(bitmaps, exportDir);
            t.Dispose();

            // Pack atlas
            t = new TimeLogger("PackAtlas");
            var sheetPath = swfPath.Replace(".swf", ".png");
            Atlas = PackAtlas(sheetPath, exportDir, AtlasMaxSize, AtlasShapePadding);
            t.Dispose();

            L.I($"Atlas has been successfully built: {sheetPath}", Atlas);
        }

        [Button(ButtonSizes.Medium), EnableIf("Atlas")]
        void BakeClip()
        {
            // load swf and atlas
            var fileData = SwfParser.Parse(AssetDatabase.GetAssetPath(SwfFile));
            var symbols = SwfParser.LoadSymbols(fileData.Tags, out _);

            // bake
            var symbol = symbols.Single(x => x.Name is not SwfParser.stage_symbol);
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
        void OptimizeAtlasSize()
        {
            var swfPath = GetSwfPath();
            var sheetPath = swfPath.Replace(".swf", ".png");
            var spriteFolder = swfPath.Replace(".swf", "_Sprites~");

            var oldMaxSize = AtlasMaxSize;
            var maxSize = AtlasMaxSize;
            byte[] granularitySeries = { 64, 32, 16, 8, 4, 2, 1 };
            foreach (var granularity in granularitySeries)
            {
                var testSize = maxSize;
                var failedCount = 0;
                while (true)
                {
                    testSize -= granularity;
                    if (testSize <= 0) break;
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
                var frameGO = new GameObject(index.ToString("D3"));
                frameGO.transform.SetParent(go.transform, false);

                var frame = symbol.Frames[index];
                for (var i = 0; i < frame.Instances.Length; i++)
                {
                    var inst = frame.Instances[i];
                    var instGO = new GameObject($"{i:D3}:B{inst.Bitmap:D3}:T{inst.Type}",
                        typeof(MeshFilter), typeof(MeshRenderer), typeof(SortingGroup));
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

        static IEnumerable<(ushort MaskBitmap, ushort RenderBitmap)> FindDuplicateMaskTextures(
            SwfInstanceData[] instances, Dictionary<ushort, TextureData> textures)
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