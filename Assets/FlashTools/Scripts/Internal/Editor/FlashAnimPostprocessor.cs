﻿using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Reflection;
using System.Globalization;
using System.Collections.Generic;

namespace FlashTools.Internal {
	public class FlashAnimPostprocessor : AssetPostprocessor {
		static void OnPostprocessAllAssets(
			string[] imported_assets, string[] deleted_assets,
			string[] moved_assets, string[] moved_from_asset_paths)
		{
			var fta_assets = imported_assets
				.Where(p => Path.GetExtension(p).ToLower().Equals(".fta"));
			foreach ( var fta_asset in fta_assets ) {
				FtaAssetProcess(fta_asset);
			}
		}

		static void FtaAssetProcess(string fta_asset) {
			var flash_anim_data = LoadFlashAnimFromFtaFile(fta_asset);
			if ( flash_anim_data != null ) {
				var new_asset_path = Path.ChangeExtension(fta_asset, ".asset");
				var new_asset = AssetDatabase.LoadAssetAtPath<FlashAnimAsset>(new_asset_path);
				if ( !new_asset ) {
					new_asset = ScriptableObject.CreateInstance<FlashAnimAsset>();
					AssetDatabase.CreateAsset(new_asset, new_asset_path);
				}
				new_asset.Data = flash_anim_data;
				EditorUtility.SetDirty(new_asset);
				AssetDatabase.SaveAssets();
			}
		}

		static FlashAnimData LoadFlashAnimFromFtaFile(string fta_path) {
			try {
				var fta_root_elem   = XDocument.Load(fta_path).Document.Root;
				var flash_anim_data = new FlashAnimData();
				LoadFlashAnimStageFromFtaRootElem  (fta_root_elem, flash_anim_data);
				LoadFlashAnimLibraryFromFtaRootElem(fta_root_elem, flash_anim_data);
				LoadFlashAnimStringsFromFtaRootElem(fta_root_elem, flash_anim_data);
				PrepareBitmapTextures(fta_path, flash_anim_data);
				PackBitmapTextures(fta_path, flash_anim_data);
				return flash_anim_data;
			} catch ( Exception e ) {
				Debug.LogErrorFormat("Parsing FTA file error: {0}", e.Message);
				return null;
			}
		}

		// -----------------------------
		// Stage
		// -----------------------------

		static void LoadFlashAnimStageFromFtaRootElem(XElement root_elem, FlashAnimData data) {
			var stage_elem = root_elem.Element("stage");
			LoadFlashAnimLayersFromFtaSymbolElem(stage_elem, data.Stage);
		}

		// -----------------------------
		// Library
		// -----------------------------

		static void LoadFlashAnimLibraryFromFtaRootElem(XElement root_elem, FlashAnimData data) {
			var library_elem = root_elem.Element("library");
			LoadFlashAnimBitmapsFromFtaLibraryElem(library_elem, data);
			LoadFlashAnimSymbolsFromFtaLibraryElem(library_elem, data);
		}

		static void LoadFlashAnimBitmapsFromFtaLibraryElem(XElement library_elem, FlashAnimData data) {
			foreach ( var bitmap_elem in library_elem.Elements("bitmap") ) {
				var bitmap = new FlashAnimBitmapData();
				bitmap.Id  = SafeLoadStrFromElemAttr(bitmap_elem, "id", bitmap.Id);
				bitmap.ImageSource = bitmap.Id + ".png";
				data.Library.Bitmaps.Add(bitmap);
			}
		}

		static void LoadFlashAnimSymbolsFromFtaLibraryElem(XElement library_elem, FlashAnimData data) {
			foreach ( var symbol_elem in library_elem.Elements("symbol") ) {
				var symbol = new FlashAnimSymbolData();
				symbol.Id  = SafeLoadStrFromElemAttr(symbol_elem, "id", symbol.Id);
				LoadFlashAnimLayersFromFtaSymbolElem(symbol_elem, symbol);
				data.Library.Symbols.Add(symbol);
			}
		}

		static void LoadFlashAnimLayersFromFtaSymbolElem(XElement symbol_elem, FlashAnimSymbolData data) {
			foreach ( var layer_elem in symbol_elem.Elements("layer") ) {
				var layer       = new FlashAnimLayerData();
				layer.Id        = SafeLoadStrFromElemAttr (layer_elem, "id"        , layer.Id);
				layer.LayerType = SafeLoadEnumFromElemAttr(layer_elem, "layer_type", FlashAnimLayerType.Normal);
				LoadFlashAnimFramesFromFtaLayerElem(layer_elem, layer);
				data.Layers.Add(layer);
			}
		}

		static void LoadFlashAnimFramesFromFtaLayerElem(XElement layer_elem, FlashAnimLayerData data) {
			foreach ( var frame_elem in layer_elem.Elements("frame") ) {
				var frame = new FlashAnimFrameData();
				frame.Id  = SafeLoadStrFromElemAttr(frame_elem, "id", frame.Id);
				LoadFlashAnimElemsFromFtaFrameElem(frame_elem, frame);
				data.Frames.Add(frame);
			}
		}

		static void LoadFlashAnimElemsFromFtaFrameElem(XElement frame_elem, FlashAnimFrameData data) {
			foreach ( var elem_elem in frame_elem.Elements("element") ) {
				var elem    = new FlashAnimElemData();
				elem.Id     = SafeLoadStrFromElemAttr(elem_elem, "id"    , elem.Id);
				elem.Matrix = SafeLoadMatFromElemAttr(elem_elem, "matrix", elem.Matrix);
				LoadFlashAnimInstFromFtaElemElem(elem_elem, elem);
				data.Elems.Add(elem);
			}
		}

		static void LoadFlashAnimInstFromFtaElemElem(XElement elem_elem, FlashAnimElemData data) {
			var inst_elem       = elem_elem.Element("instance");
			var instance        = new FlashAnimInstData();
			instance.Type       = SafeLoadEnumFromElemAttr(inst_elem, "type"       , instance.Type);
			instance.SymbolType = SafeLoadEnumFromElemAttr(inst_elem, "symbol_type", instance.SymbolType);
			instance.BlendMode  = SafeLoadEnumFromElemAttr(inst_elem, "blend_mode" , instance.BlendMode);
			instance.Asset      = SafeLoadStrFromElemAttr (inst_elem, "asset"      , instance.Asset);
			instance.Visible    = SafeLoadBoolFromElemAttr(inst_elem, "visible"    , instance.Visible);
			var looping_elem = inst_elem.Element("looping");
			if ( looping_elem != null ) {
				instance.LoopingType       = SafeLoadEnumFromElemAttr(looping_elem, "type"        , instance.LoopingType);
				instance.LoopingFirstFrame = SafeLoadIntFromElemAttr (looping_elem, "first_frame" , instance.LoopingFirstFrame);
			}
			data.Instance = instance;
		}

		// -----------------------------
		// Strings
		// -----------------------------

		static void LoadFlashAnimStringsFromFtaRootElem(XElement root_elem, FlashAnimData data) {
			var strings_elem = root_elem.Element("strings");
			foreach ( var string_elem in strings_elem.Elements("string") ) {
				var string_id  = SafeLoadStrFromElemAttr(string_elem, "id" , null);
				var string_str = SafeLoadStrFromElemAttr(string_elem, "str", null);
				if ( !string.IsNullOrEmpty(string_id) && string_str != null ) {
					data.Strings.Add(string_id);
					data.Strings.Add(string_str);
				}
			}
		}

		// -----------------------------
		// Textures
		// -----------------------------

		static void PrepareBitmapTextures(string fta_path, FlashAnimData data) {
		}

		static void PackBitmapTextures(string fta_path, FlashAnimData data) {
			var base_path = Path.GetDirectoryName(fta_path);
			var textures  = new List<Texture2D>();
			var texturen  = new List<string>();
			foreach ( var bitmap in data.Library.Bitmaps ) {
				var texture_path = Path.Combine(base_path, bitmap.ImageSource);
				var importer = AssetImporter.GetAtPath(texture_path) as TextureImporter;
				if ( !importer ) {
					throw new UnityException(string.Format(
						"bitmap ({0}) texture importer not found ({1})",
						bitmap.Id, texture_path));
				}
				if ( !importer.isReadable ) {
					importer.isReadable = true;
					AssetDatabase.ImportAsset(texture_path, ImportAssetOptions.ForceUpdate);
					AssetDatabase.ImportAsset(fta_path, ImportAssetOptions.ForceUpdate);
					return;
				}
				var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texture_path);
				if ( !texture ) {
					throw new UnityException(string.Format(
						"bitmap ({0}) texture not found ({1})",
						bitmap.Id, texture_path));
				}
				textures.Add(texture);
				texturen.Add(bitmap.ImageSource);
			}

			var atlas = new Texture2D(0, 0);
			var atlas_rects = atlas.PackTextures(textures.ToArray(), 1, 1024);
			var atlas_asset_path = Path.Combine(
				Path.GetDirectoryName(Application.dataPath),
				Path.Combine(base_path, "atlas.png"));
			File.WriteAllBytes(atlas_asset_path, atlas.EncodeToPNG());
			GameObject.DestroyImmediate(atlas);
			AssetDatabase.Refresh();

			var atlas_path = Path.Combine(base_path, "atlas.png");
			data.Atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(atlas_path);
			if ( !data.Atlas ) {
				AssetDatabase.ImportAsset(fta_path, ImportAssetOptions.ForceUpdate);
				return;
			}

			var atlas_importer = AssetImporter.GetAtPath(atlas_path) as TextureImporter;
			if ( !atlas_importer ) {
				throw new UnityException(string.Format(
					"atlas importer not found ({0})",
					atlas_path));
			}

			var method_args = new object[2]{0,0};
			typeof(TextureImporter)
				.GetMethod("GetWidthAndHeight", BindingFlags.NonPublic | BindingFlags.Instance)
				.Invoke(atlas_importer, method_args);
			var atlas_width  = (int)method_args[0];
			var atlas_height = (int)method_args[1];

			var meta_data = new List<SpriteMetaData>();
			for ( var i = 0; i < atlas_rects.Length; ++i ) {
				var meta_elem  = new SpriteMetaData();
				meta_elem.name = texturen[i];
				data.Library.Bitmaps[i].RealSize = new Vector2(textures[i].width, textures[i].height);
				data.Library.Bitmaps[i].SourceRect = atlas_rects[i];
				meta_elem.rect = new Rect(
					atlas_rects[i].xMin   * atlas_width,
					atlas_rects[i].yMin   * atlas_height,
					atlas_rects[i].width  * atlas_width,
					atlas_rects[i].height * atlas_height);
				meta_data.Add(meta_elem);
			}
			atlas_importer.spritesheet      = meta_data.ToArray();
			atlas_importer.textureType      = TextureImporterType.Sprite;
			atlas_importer.spriteImportMode = SpriteImportMode.Multiple;
			atlas_importer.textureFormat    = TextureImporterFormat.AutomaticTruecolor;
			AssetDatabase.ImportAsset(atlas_path, ImportAssetOptions.ForceUpdate);
		}

		// -----------------------------
		// Common
		// -----------------------------

		static string SafeLoadStrFromElemAttr(XElement elem, string attr_name, string def_value) {
			if ( elem != null && elem.Attribute(attr_name) != null ) {
				return elem.Attribute(attr_name).Value;
			}
			return def_value;
		}

		static int SafeLoadIntFromElemAttr(XElement elem, string attr_name, int def_value) {
			int value;
			if ( elem != null && int.TryParse(SafeLoadStrFromElemAttr(elem, attr_name, string.Empty), out value) ) {
				return value;
			}
			return def_value;
		}

		static bool SafeLoadBoolFromElemAttr(XElement elem, string attr_name, bool def_value) {
			bool value;
			if ( elem != null && bool.TryParse(SafeLoadStrFromElemAttr(elem, attr_name, string.Empty), out value) ) {
				return value;
			}
			return def_value;
		}

		static Matrix4x4 SafeLoadMatFromElemAttr(XElement elem, string attr_name, Matrix4x4 def_value) {
			var mat_str = SafeLoadStrFromElemAttr(elem, attr_name, string.Empty);
			var mat_strs = mat_str.Split(';');
			if ( mat_strs.Length == 6 ) {
				float a, b, c, d, tx, ty;
				if (
					float.TryParse(mat_strs[0], NumberStyles.Any, CultureInfo.InvariantCulture, out a ) &&
					float.TryParse(mat_strs[1], NumberStyles.Any, CultureInfo.InvariantCulture, out b ) &&
					float.TryParse(mat_strs[2], NumberStyles.Any, CultureInfo.InvariantCulture, out c ) &&
					float.TryParse(mat_strs[3], NumberStyles.Any, CultureInfo.InvariantCulture, out d ) &&
					float.TryParse(mat_strs[4], NumberStyles.Any, CultureInfo.InvariantCulture, out tx) &&
					float.TryParse(mat_strs[5], NumberStyles.Any, CultureInfo.InvariantCulture, out ty) )
				{
					var mat = Matrix4x4.identity;
					mat.m00 = a;
					mat.m10 = b;
					mat.m01 = c;
					mat.m11 = d;
					mat.m03 = tx;
					mat.m13 = ty;
					return mat;
				}
			}
			return def_value;
		}

		static T SafeLoadEnumFromElemAttr<T>(XElement elem, string attr_name, T def_value) {
			try {
				return (T)Enum.Parse(typeof(T), SafeLoadStrFromElemAttr(elem, attr_name, string.Empty), true);
			} catch ( Exception ) {
				return def_value;
			}
		}
	}
}
