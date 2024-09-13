using System;
using UnityEngine;
using UnityEditor;
using System.Linq;
using FTRuntime;
using Object = UnityEngine.Object;

namespace FTEditor {
	static class SwfEditorUtils {

		// ---------------------------------------------------------------------
		//
		// Inspector
		//
		// ---------------------------------------------------------------------

		public static void DoWithMixedValue(bool mixed, System.Action act) {
			var last_show_mixed_value = EditorGUI.showMixedValue;
			EditorGUI.showMixedValue = mixed;
			try {
				act();
			} finally {
				EditorGUI.showMixedValue = last_show_mixed_value;
			}
		}

		public static void DoWithEnabledGUI(bool enabled, System.Action act) {
			EditorGUI.BeginDisabledGroup(!enabled);
			try {
				act();
			} finally {
				EditorGUI.EndDisabledGroup();
			}
		}

		public static void DoRightHorizontalGUI(System.Action act) {
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			try {
				act();
			} finally {
				GUILayout.EndHorizontal();
			}
		}

		public static void DoCenterHorizontalGUI(System.Action act) {
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

		public static void UpdateSceneSwfClips(SwfClipAsset asset)
		{
			var clips = Object.FindObjectsByType<SwfClip>(FindObjectsInactive.Include, FindObjectsSortMode.None)
				.Where (p => p && p.clip && p.clip == asset)
				.ToList();
			foreach (var clip in clips)
				clip.Internal_UpdateAllProperties();
		}
	}
}