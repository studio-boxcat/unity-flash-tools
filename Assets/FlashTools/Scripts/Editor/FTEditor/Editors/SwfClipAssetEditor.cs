using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

using FTRuntime;

namespace FTEditor.Editors {
	[CustomEditor(typeof(SwfClipAsset)), CanEditMultipleObjects]
	class SwfClipAssetEditor : Editor {
		List<SwfClipAsset>  _clips    = new();
		SwfClipAssetPreview _preview  = null;

		static int GetFrameCount(SwfClipAsset clip) {
			return clip != null ? clip.Sequences.Aggregate(0, (acc, seq) => {
				return seq.Frames.Length + acc;
			}) : 0;
		}

		//
		//
		//

		void DrawGUIFrameCount() {
			var counts      = _clips.Select(p => GetFrameCount(p));
			var mixed_value = counts.GroupBy(p => p).Count() > 1;
			SwfEditorUtils.DoWithEnabledGUI(false, () => {
				SwfEditorUtils.DoWithMixedValue(
					mixed_value, () => {
						EditorGUILayout.IntField("Frame count", counts.First());
					});
			});
		}

		void DrawGUISequences() {
			SwfEditorUtils.DoWithEnabledGUI(false, () => {
				var sequences_prop = SwfEditorUtils.GetPropertyByName(
					serializedObject, "Sequences");
				if ( sequences_prop.isArray ) {
					SwfEditorUtils.DoWithMixedValue(
						sequences_prop.hasMultipleDifferentValues, () => {
							EditorGUILayout.IntField("Sequence count", sequences_prop.arraySize);
						});
				}
			});
		}

		//
		//
		//

		void SetupPreviews() {
			ShutdownPreviews();
			_preview = new SwfClipAssetPreview();
			_preview.Initialize(targets
				.OfType<SwfClipAsset>()
				.Where(p => p)
				.ToArray());
		}

		void ShutdownPreviews() {
			if ( _preview != null ) {
				_preview.Shutdown();
				_preview = null;
			}
		}

		// ---------------------------------------------------------------------
		//
		// Messages
		//
		// ---------------------------------------------------------------------

		void OnEnable() {
			_clips = targets.OfType<SwfClipAsset>().ToList();
			SetupPreviews();
		}

		void OnDisable() {
			ShutdownPreviews();
			_clips.Clear();
		}

		public override void OnInspectorGUI() {
			serializedObject.Update();
			DrawDefaultInspector();
			DrawGUIFrameCount();
			DrawGUISequences();
			if ( GUI.changed ) {
				serializedObject.ApplyModifiedProperties();
			}
		}

		public override bool RequiresConstantRepaint() {
			return _clips.Count > 0;
		}

		public override bool HasPreviewGUI() {
			return _clips.Count > 0;
		}

		public override void OnPreviewSettings() {
			if ( _preview != null ) {
				_preview.OnPreviewSettings();
			}
		}

		public override void OnPreviewGUI(Rect r, GUIStyle background) {
			if ( _preview != null ) {
				_preview.OnPreviewGUI(r, background);
			}
		}
	}
}