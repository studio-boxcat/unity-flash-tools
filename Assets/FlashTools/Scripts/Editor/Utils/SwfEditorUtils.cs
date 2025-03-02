using System;
using UnityEngine;
using UnityEditor;
using System.Linq;
using Object = UnityEngine.Object;

namespace FT {
	internal static class SwfEditorUtils {

		// ---------------------------------------------------------------------
		//
		// Assets
		//
		// ---------------------------------------------------------------------

		public static void DestroySubAssetsOfType(Object obj, Type subAssetType) {
			var asset_path = AssetDatabase.GetAssetPath(obj);
			foreach (var sub_asset in AssetDatabase.LoadAllAssetsAtPath(asset_path))
			{
				if (sub_asset.GetType() == subAssetType)
					Object.DestroyImmediate(sub_asset, true);
			}
		}

		public static void UpdateSceneSwfClips(SwfClip clip)
		{
			var views = Object.FindObjectsByType<SwfView>(FindObjectsInactive.Include, FindObjectsSortMode.None)
				.Where (p => p && p.clip && p.clip == clip)
				.ToList();
			foreach (var view in views)
				view.Editor_OnClipChanges();
		}
	}
}