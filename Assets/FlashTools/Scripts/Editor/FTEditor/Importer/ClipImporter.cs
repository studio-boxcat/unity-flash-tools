using System.Collections.Generic;
using System.IO;
using System.Linq;
using FTRuntime;
using FTRuntime.Internal;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
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
            var symbol = symbols.Single(x => x.Name is not SwfParser.stage_symbol);
            var instances = symbol.Frames.SelectMany(x => x.Instances).ToArray();

            // Find used bitmaps
            var usedBitmaps = instances.Select(x => x.Bitmap).ToHashSet();
            var bitmaps = library.GetBitmaps()
                .Where(x => usedBitmaps.Contains(x.Key))
                .ToDictionary(x => x.Key, x => x.Value);

            // Export bitmaps
            var swfPath = AssetDatabase.GetAssetPath(SwfFile);
            Assert.IsTrue(swfPath.EndsWith(".swf"));
            var exportDir = swfPath.Replace(".swf", "_Sprites~");
            BitmapExporter.ExportBitmaps(bitmaps, exportDir, out var textures);

            // Find mask only textures.
            foreach (var (maskBitmap, renderBitmap) in FindDuplicateMaskTextures(instances, textures))
            {
                var maskPath = Path.Combine(exportDir, BitmapExporter.GetSpriteName(maskBitmap));
                var renderPath = Path.Combine(exportDir, BitmapExporter.GetSpriteName(renderBitmap));
                File.Delete(maskPath);
                File.Copy(renderPath, maskPath);
                L.I($"Mask only bitmap {maskBitmap} has been replaced with {renderBitmap}");
            }

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
                    mr.sharedMaterial = SwfMaterialCache.Query(
                        inst.Type, inst.BlendMode.type, inst.ClipDepth);
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

        static IEnumerable<(ushort MaskBitmap, ushort RenderBitmap)> FindDuplicateMaskTextures(
            SwfInstanceData[] instances, Dictionary<ushort, Texture2D> textures)
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
            var renderBitmapAlphaDict = renderBitmaps.ToDictionary(x => x, x => textures[x].GetPixels().Select(y => y.a).ToArray());
            foreach (var maskBitmap in maskBitmaps)
            {
                var maskOnlyTexture = textures[maskBitmap];
                var maskOnlyAlpha = maskOnlyTexture.GetPixels().Select(x => x.a).ToArray();
                var found = renderBitmapAlphaDict.FirstOrDefault(x => AlphaEquals(x.Value, maskOnlyAlpha));
                if (found.Key is not 0)
                {
                    L.I($"Found duplicate mask texture: {maskBitmap:D4} -> {found.Key:D4}");
                    yield return (maskBitmap, found.Key);
                }
            }
            yield break;

            static bool AlphaEquals(float[] a, float[] b)
            {
                const float threshold = 0.4f;
                if (a.Length != b.Length) return false;
                for (var i = 0; i < a.Length; i++)
                    if (Mathf.Abs(a[i] - b[i]) > threshold)
                        return false;
                return true;
            }
        }
    }
}