using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using FTRuntime;
using UnityEngine.Assertions;

namespace FTEditor.Postprocessors {
	class SwfAssetPostprocessor : AssetPostprocessor {
		static SwfEditorUtils.ProgressBar _progressBar = new SwfEditorUtils.ProgressBar();

		static void OnPostprocessAllAssets(
			string[] imported_assets,
			string[] deleted_assets,
			string[] moved_assets,
			string[] moved_from_asset_paths)
		{
			var assets = imported_assets
				.Where(p => Path.GetExtension(p).ToLower().Equals(".asset"))
				.Select(p => AssetDatabase.LoadAssetAtPath<SwfAsset>(p))
				.Where(p => p && !p.Atlas);
			if ( assets.Any() ) {
				EditorApplication.delayCall += () => {
					foreach ( var asset in assets ) {
						SwfAssetProcess(asset);
					}
					AssetDatabase.SaveAssets();
				};
			}
		}

		static void SwfAssetProcess(SwfAsset asset) {
			try {
				EditorUtility.SetDirty(asset);
				var asset_data = SwfEditorUtils.DecompressAsset<SwfAssetData>(asset.Data, progress => {
					_progressBar.UpdateProgress("decompress swf asset", progress);
				});
				asset.Atlas = LoadAssetAtlas(asset);
				if ( asset.Atlas ) {
					ConfigureAtlas(asset);
					ConfigureClips(asset, asset_data);
					Debug.LogFormat(
						asset,
						"<b>[FlashTools]</b> SwfAsset has been successfully converted:\nPath: {0}",
						AssetDatabase.GetAssetPath(asset));
				} else {
					_progressBar.UpdateTitle(asset.name);
					var new_data = ConfigureBitmaps(asset, asset_data);
					asset.Data = SwfEditorUtils.CompressAsset(new_data, progress => {
						_progressBar.UpdateProgress("compress swf asset", progress);
					});
				}
			} catch ( Exception e ) {
				Debug.LogException(e);
				Debug.LogErrorFormat(
					asset,
					"<b>[FlashTools]</b> Postprocess swf asset error: {0}\nPath: {1}",
					e.Message, AssetDatabase.GetAssetPath(asset));
				AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(asset));
			} finally {
				if ( asset ) {
					UpdateAssetClips(asset);
				}
				_progressBar.HideProgress();
			}
		}

		static Texture2D LoadAssetAtlas(SwfAsset asset) {
			return AssetDatabase.LoadAssetAtPath<Texture2D>(
				GetAtlasPath(asset));
		}

		static string GetAtlasPath(SwfAsset asset) {
			if ( asset.Atlas ) {
				return AssetDatabase.GetAssetPath(asset.Atlas);
			} else {
				var asset_path = AssetDatabase.GetAssetPath(asset);
				return Path.ChangeExtension(asset_path, "._Atlas_.png");
			}
		}

		// ---------------------------------------------------------------------
		//
		// ConfigureBitmaps
		//
		// ---------------------------------------------------------------------

		static SwfAssetData ConfigureBitmaps(SwfAsset asset, SwfAssetData data) {
			var textures = new List<KeyValuePair<ushort, Texture2D>>(data.Bitmaps.Count);
			for ( var i = 0; i < data.Bitmaps.Count; ++i ) {
				_progressBar.UpdateProgress(
					"configure bitmaps",
					(float)(i + 1) / data.Bitmaps.Count);
				var bitmap = data.Bitmaps[i];
				if ( bitmap.Redirect == 0 ) {
					textures.Add(new KeyValuePair<ushort, Texture2D>(
						bitmap.Id,
						LoadTextureFromData(bitmap, asset.Settings)));
				}
			}
			var rects = PackAndSaveBitmapsAtlas(
				GetAtlasPath(asset),
				textures.Select(p => p.Value).ToArray(),
				asset.Settings);
			for ( var i = 0; i < data.Bitmaps.Count; ++i ) {
				var bitmap        = data.Bitmaps[i];
				var texture_key   = bitmap.Redirect > 0 ? bitmap.Redirect : bitmap.Id;
				bitmap.SourceRect = SwfRectIntData.FromURect(
					rects[textures.FindIndex(p => p.Key == texture_key)]);
			}
			return data;
		}

		static Texture2D LoadTextureFromData(SwfBitmapData bitmap, SwfSettingsData settings) {
			var argb32 = settings.BitmapTrimming
				? TrimBitmapByRect(bitmap, bitmap.TrimmedRect)
				: bitmap.ARGB32;
			var widht = settings.BitmapTrimming
				? bitmap.TrimmedRect.width
				: bitmap.RealWidth;
			var height = settings.BitmapTrimming
				? bitmap.TrimmedRect.height
				: bitmap.RealHeight;
			var texture = new Texture2D(
				widht, height,
				TextureFormat.ARGB32, false);
			texture.LoadRawTextureData(argb32);
			return texture;
		}

		static byte[] TrimBitmapByRect(SwfBitmapData bitmap, SwfRectIntData rect) {
			var argb32 = new byte[rect.area * 4];
			for ( var i = 0; i < rect.height; ++i ) {
				var src_index = rect.xMin + (rect.yMin + i) * bitmap.RealWidth;
				var dst_index = i * rect.width;
				Array.Copy(
					bitmap.ARGB32, src_index * 4,
					argb32, dst_index * 4,
					rect.width * 4);
			}
			return argb32;
		}

		struct BitmapsAtlasInfo {
			public Texture2D Atlas;
			public RectInt[]    Rects;
		}

		static RectInt[] PackAndSaveBitmapsAtlas(
			string atlas_path, Texture2D[] textures, SwfSettingsData settings)
		{
			_progressBar.UpdateProgress("pack bitmaps", 0.25f);
			var atlas_info = PackBitmapsAtlas(textures, settings);
			RevertTexturePremultipliedAlpha(atlas_info.Atlas);
			_progressBar.UpdateProgress("save atlas", 0.5f);
			File.WriteAllBytes(atlas_path, atlas_info.Atlas.EncodeToPNG());
			GameObject.DestroyImmediate(atlas_info.Atlas, true);
			_progressBar.UpdateProgress("import atlas", 0.75f);
			AssetDatabase.ImportAsset(atlas_path);
			return atlas_info.Rects;
		}

		static BitmapsAtlasInfo PackBitmapsAtlas(
			Texture2D[] textures, SwfSettingsData settings)
		{
			var atlas_padding  = Mathf.Max(0,  settings.AtlasPadding);
			var (atlas, rects) = TexturePack.PackTextures(textures, atlas_padding);
			return new BitmapsAtlasInfo{Atlas = atlas, Rects = rects};
		}

		static void RevertTexturePremultipliedAlpha(Texture2D texture) {
			var pixels = texture.GetPixels();
			for ( var i = 0; i < pixels.Length; ++i ) {
				var c = pixels[i];
				if ( c.a > 0 ) {
					c.r /= c.a;
					c.g /= c.a;
					c.b /= c.a;
				}
				pixels[i] = c;
			}
			texture.SetPixels(pixels);
			texture.Apply();
		}

		// ---------------------------------------------------------------------
		//
		// ConfigureAtlas
		//
		// ---------------------------------------------------------------------

		static void ConfigureAtlas(SwfAsset asset) {
			var atlas_importer                 = GetBitmapsAtlasImporter(asset);
			atlas_importer.textureType         = TextureImporterType.Sprite;
			atlas_importer.spriteImportMode    = SpriteImportMode.Single;
			atlas_importer.spritePixelsPerUnit = asset.Settings.PixelsPerUnit;
			atlas_importer.filterMode          = FilterMode.Bilinear;

			var atlas_settings = new TextureImporterSettings();
			atlas_importer.ReadTextureSettings(atlas_settings);
			atlas_settings.spriteMeshType = SpriteMeshType.FullRect;
			atlas_importer.SetTextureSettings(atlas_settings);

			atlas_importer.SaveAndReimport();
		}

		static TextureImporter GetBitmapsAtlasImporter(SwfAsset asset) {
			var atlas_path     = AssetDatabase.GetAssetPath(asset.Atlas);
			var atlas_importer = AssetImporter.GetAtPath(atlas_path) as TextureImporter;
			if ( !atlas_importer ) {
				throw new UnityException(string.Format(
					"atlas texture importer not found ({0})",
					atlas_path));
			}
			return atlas_importer;
		}

		// ---------------------------------------------------------------------
		//
		// ConfigureClips
		//
		// ---------------------------------------------------------------------

		static SwfAssetData ConfigureClips(SwfAsset asset, SwfAssetData data) {
			for ( var i = 0; i < data.Symbols.Count; ++i )
			{
				_progressBar.UpdateProgress(
					"configure clips",
					(float)(i + 1) / data.Symbols.Count);

				// XXX: 잘 모르겠지만 swf 안에 _Stage_ 라는 이름의 심볼이 자동으로 포함되는 것 같음.
				if (data.Symbols[i].Name == "_Stage_")
					continue;

				ConfigureClip(asset, data, data.Symbols[i]);
			}
			return data;
		}

		static void ConfigureClip(SwfAsset asset, SwfAssetData data, SwfSymbolData symbol) {
			var asset_guid  = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
			var clip_assets = SwfEditorUtils.LoadAllAssetsDBByFilter<SwfClipAsset>("t:SwfClipAsset")
				.Where(p => p.AssetGUID == asset_guid && p.Name == symbol.Name);
			if ( clip_assets.Any() ) {
				foreach ( var clip_asset in clip_assets ) {
					ConfigureClipAsset(clip_asset, asset, data, symbol);
				}
			} else {
				var asset_path      = AssetDatabase.GetAssetPath(asset);
				var clip_asset_path = Path.ChangeExtension(asset_path, symbol.Name + ".asset");
				SwfEditorUtils.LoadOrCreateAsset<SwfClipAsset>(clip_asset_path, (new_clip_asset, created) => {
					ConfigureClipAsset(new_clip_asset, asset, data, symbol);
					return true;
				});
			}
		}

		static void ConfigureClipAsset(
			SwfClipAsset clip_asset, SwfAsset asset, SwfAssetData data, SwfSymbolData symbol)
		{
			var context = new ConvertContext();

			var asset_guid     = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
			var asset_atlas      = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GetAssetPath(asset.Atlas));
			clip_asset.Name      = symbol.Name;
			clip_asset.Atlas     = asset_atlas;
			clip_asset.FrameRate = data.FrameRate;
			clip_asset.AssetGUID = asset_guid;
			clip_asset.Sequences = LoadClipSequences(asset, data, symbol, context).ToArray();
			clip_asset.MaterialGroups = context.MaterialMemory.Bake();
			EditorUtility.SetDirty(clip_asset);

			EditorApplication.delayCall += () =>
			{
				if (clip_asset == null)
					return;

				AssetDatabase.SaveAssets();

				var clip_asset_path = AssetDatabase.GetAssetPath(clip_asset);
				foreach (var sub_asset in AssetDatabase.LoadAllAssetsAtPath(clip_asset_path).Where(x => x is Mesh))
					UnityEngine.Object.DestroyImmediate(sub_asset, true);

				foreach (var mesh in context.MeshMemory)
					AssetDatabase.AddObjectToAsset(mesh, clip_asset);

				AssetDatabase.ImportAsset(clip_asset_path);
			};
		}

		static List<SwfClipAsset.Sequence> LoadClipSequences(
			SwfAsset asset, SwfAssetData data, SwfSymbolData symbol, ConvertContext context)
		{
			var sequences = new List<SwfClipAsset.Sequence>();

			string curSequenceName = null;
			List<SwfClipAsset.Frame> curFrames = null;
			List<SwfClipAsset.Label> curLabels = null;

			foreach ( var frame in symbol.Frames ) {
				if (!string.IsNullOrEmpty(frame.Anchor) && curSequenceName != frame.Anchor)
				{
					// 시퀀스가 변경되었다면 시퀀스를 추가해줌.
					if (curSequenceName != null)
						sequences.Add(new SwfClipAsset.Sequence(
							curSequenceName, curFrames.ToArray(), curLabels.ToArray()));

					curSequenceName = frame.Anchor;
					curFrames = new List<SwfClipAsset.Frame>();
					curLabels = new List<SwfClipAsset.Label>();
				}

				// 프레임 삽입.
				var frameIndex = (ushort) curFrames.Count;
				curFrames.Add(BakeClipFrame(asset, data, frame, context));

				// 레이블 삽입.
				foreach (var label in frame.Labels)
				{
					var hash = SwfHash.Hash(label);
					curLabels.Add(new SwfClipAsset.Label(hash, frameIndex));
				}
			}

			if (curFrames is {Count: > 0})
			{
				sequences.Add(new SwfClipAsset.Sequence(
					curSequenceName, curFrames.ToArray(), curLabels.ToArray()));
			}

			return sequences;
		}

		class BakedGroup {
			public SwfInstanceData.Types  Type;
			public SwfBlendModeData.Types BlendMode;
			public int                    ClipDepth;
			public int                    StartVertex;
			public int                    TriangleCount;
		}

		static SwfClipAsset.Frame BakeClipFrame(
			SwfAsset asset, SwfAssetData data, SwfFrameData frame, ConvertContext context)
		{
			List<SwfRectIntData> baked_rects  = new List<SwfRectIntData>();
			List<SwfVec4Data> baked_mulcolors = new List<SwfVec4Data>();
			List<SwfVec4Data> baked_addcolors = new List<SwfVec4Data>();
			List<Vector2>    baked_vertices  = new List<Vector2>();
			List<BakedGroup> baked_groups    = new List<BakedGroup>();

			foreach ( var inst in frame.Instances ) {
				var bitmap = inst != null
					? FindBitmapFromAssetData(data, inst.Bitmap)
					: null;
				while ( bitmap != null && bitmap.Redirect > 0 ) {
					bitmap = FindBitmapFromAssetData(data, bitmap.Redirect);
				}
				if ( bitmap != null ) {
					var br = asset.Settings.BitmapTrimming
						? bitmap.TrimmedRect
						: new SwfRectIntData(bitmap.RealWidth, bitmap.RealHeight);

					var v0 = new Vector2(br.xMin, br.yMin);
					var v1 = new Vector2(br.xMax, br.yMin);
					var v2 = new Vector2(br.xMax, br.yMax);
					var v3 = new Vector2(br.xMin, br.yMax);

					var matrix =
						Matrix4x4.Scale(new Vector3(1.0f, -1.0f, 1.0f) / asset.Settings.PixelsPerUnit) *
						inst.Matrix.ToUMatrix() *
						Matrix4x4.Scale(new Vector3(1.0f / 20.0f, 1.0f / 20.0f, 1.0f));

					baked_vertices.Add(matrix.MultiplyPoint3x4(v0));
					baked_vertices.Add(matrix.MultiplyPoint3x4(v1));
					baked_vertices.Add(matrix.MultiplyPoint3x4(v2));
					baked_vertices.Add(matrix.MultiplyPoint3x4(v3));

					baked_rects.Add(bitmap.SourceRect);

					baked_mulcolors.Add(inst.ColorTrans.mulColor);

					baked_addcolors.Add(inst.ColorTrans.addColor);

					if ( baked_groups.Count == 0 ||
						baked_groups[^1].Type      != inst.Type           ||
						baked_groups[^1].BlendMode != inst.BlendMode.type ||
						baked_groups[^1].ClipDepth != inst.ClipDepth )
					{
						baked_groups.Add(new BakedGroup{
							Type          = inst.Type,
							BlendMode     = inst.BlendMode.type,
							ClipDepth     = inst.ClipDepth,
							StartVertex   = baked_vertices.Count - 4,
							TriangleCount = 0,
						});
					}

					baked_groups.Last().TriangleCount += 6;
				}
			}

			var materials = new Material[baked_groups.Count];
			for (var index = 0; index < baked_groups.Count; index++)
			{
				var group = baked_groups[index];
				var material = group.Type switch
				{
					SwfInstanceData.Types.Mask => SwfMaterialCache.GetIncrMaskMaterial(),
					SwfInstanceData.Types.Group => SwfMaterialCache.GetSimpleMaterial(group.BlendMode),
					SwfInstanceData.Types.Masked => SwfMaterialCache.GetMaskedMaterial(group.BlendMode, group.ClipDepth),
					SwfInstanceData.Types.MaskReset => SwfMaterialCache.GetDecrMaskMaterial(),
					_ => throw new UnityException($"SwfAssetPostprocessor. Incorrect instance type: {group.Type}")
				};

				Assert.IsNotNull(material);
				materials[index] = material;
			}

			var mesh_data = new MeshData{
				SubMeshes = baked_groups
					.Select(p => new SubMeshData{
						StartVertex = p.StartVertex,
						IndexCount  = p.TriangleCount})
					.ToArray(),
				Vertices  = baked_vertices .ToArray(),
				Rects     = baked_rects    .ToArray(),
				AddColors = baked_addcolors.ToArray(),
				MulColors = baked_mulcolors.ToArray()};

			return new SwfClipAsset.Frame(
				context.MeshMemory.GetOrAdd(mesh_data.GetHashCode(), () => MeshBuilder.Build(mesh_data)),
				context.MaterialMemory.ResolveMaterialGroupIndex(materials));
		}

		static SwfBitmapData FindBitmapFromAssetData(SwfAssetData data, int bitmap_id) {
			for ( var i = 0; i < data.Bitmaps.Count; ++i ) {
				var bitmap = data.Bitmaps[i];
				if ( bitmap.Id == bitmap_id ) {
					return bitmap;
				}
			}
			return null;
		}

		// ---------------------------------------------------------------------
		//
		// UpdateAssetClips
		//
		// ---------------------------------------------------------------------

		static void UpdateAssetClips(SwfAsset asset) {
			var asset_guid  = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
			var scene_clips = GameObject.FindObjectsOfType<SwfClip>()
				.Where (p => p && p.clip && p.clip.AssetGUID == asset_guid)
				.ToList();
			for ( var i = 0; i < scene_clips.Count; ++i ) {
				_progressBar.UpdateProgress(
					"update scene clips",
					(float)(i + 1) / scene_clips.Count);
				scene_clips[i].Internal_UpdateAllProperties();
			}
		}
	}
}