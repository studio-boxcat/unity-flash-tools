using System;
using UnityEngine;
using UnityEditor;
using System.Linq;
using Object = UnityEngine.Object;

namespace FT {
	internal static class SwfEditorUtils {

		// ---------------------------------------------------------------------
		//
		// Inspector
		//
		// ---------------------------------------------------------------------

		public static void DoWithMixedValue(bool mixed, Action act) {
			var last_show_mixed_value = EditorGUI.showMixedValue;
			EditorGUI.showMixedValue = mixed;
			try {
				act();
			} finally {
				EditorGUI.showMixedValue = last_show_mixed_value;
			}
		}

		public static void DoRightHorizontalGUI(Action act) {
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			try {
				act();
			} finally {
				GUILayout.EndHorizontal();
			}
		}

		public static void DoCenterHorizontalGUI(Action act) {
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			try {
				act();
			} finally {
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
			}
		}

		public static SerializedProperty GetPropertyByName(SerializedObject obj, string name) {
			var prop = obj.FindProperty(name);
			if ( prop == null ) {
				throw new UnityException(string.Format(
					"SwfEditorUtils. Not found property: {0}",
					name));
			}
			return prop;
		}

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