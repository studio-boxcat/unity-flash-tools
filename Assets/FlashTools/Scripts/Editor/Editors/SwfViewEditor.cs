using System;
using UnityEngine;
using UnityEditor;

using System.Linq;
using System.Collections.Generic;

using FTRuntime;
using Object = UnityEngine.Object;

namespace FTEditor.Editors {
	[CustomEditor(typeof(SwfView)), CanEditMultipleObjects]
	internal class SwfViewEditor : Editor {
		private List<SwfView>                            _clips    = new();
		private Dictionary<SwfView, SwfClipPreview> _previews = new();

		private void AllClipsForeachWithUndo(System.Action<SwfView> act) {
			Undo.RecordObjects(_clips.ToArray(), "Inspector");
			foreach ( var clip in _clips ) {
				act(clip);
				EditorUtility.SetDirty(clip);
			}
		}

		private string GetClipsFrameCountForView() {
			return _clips.Aggregate(string.Empty, (acc, clip) => {
				var frame_count     = clip.frameCount;
				var frame_count_str = frame_count.ToString();
				return string.IsNullOrEmpty(acc)
					? frame_count_str
					: (acc != frame_count_str ? "--" : acc);
			});
		}

		private string GetClipsCurrentFrameForView() {
			return _clips.Aggregate(string.Empty, (acc, clip) => {
				var current_frame     = clip.currentFrame + 1;
				var current_frame_str = current_frame.ToString();
				return string.IsNullOrEmpty(acc)
					? current_frame_str
					: (acc != current_frame_str ? "--" : acc);
			});
		}

		private SwfSequenceId[] GetAllSequences() {
			var seqs = _clips
				.Where (p => p.clip)
				.Select(p => p.clip.Sequences.Select(x => x.Id).ToArray())
				.ToList();
			if (seqs.Count is 0)
				return Array.Empty<SwfSequenceId>();

			var intersection = new HashSet<SwfSequenceId>(seqs[0]);
			foreach (var clip in seqs.Skip(0))
				intersection.IntersectWith(clip);

			return seqs[0].Where(p => intersection.Contains(p)).ToArray();
		}

		private void DrawSequence() {
			var seqs = GetAllSequences();
			if (seqs.Length is 0) return;

			var prop = SwfEditorUtils.GetPropertyByName(serializedObject, "_sequence");
			var index = prop.hasMultipleDifferentValues ? -1 : Array.IndexOf(seqs, (SwfSequenceId) prop.intValue);
			var opts = seqs.Select(x => ((int) x).ToString()).ToArray();

			SwfEditorUtils.DoWithMixedValue(prop.hasMultipleDifferentValues, () => {
				var selected = EditorGUILayout.Popup("Sequence", index, opts);
				if (selected < 0 || selected >= seqs.Length) return; // Out of range
				if (selected == index) return;
				prop.intValue = (int) seqs[selected];
				prop.serializedObject.ApplyModifiedProperties();
			});
		}

		private void DrawCurrentFrame()
		{
			if (_clips.Count is 0) return;
			var minFrameCount = _clips.Min(clip => clip.frameCount);
			if (minFrameCount is 0) return;

			EditorGUILayout.IntSlider(
				SwfEditorUtils.GetPropertyByName(serializedObject, "_currentFrame"),
				0, minFrameCount - 1,
				"Current frame");
			DrawClipControls();
		}

		private void DrawClipControls() {
			EditorGUILayout.Space();
			SwfEditorUtils.DoCenterHorizontalGUI(() => {
				if ( GUILayout.Button(new GUIContent("<<", "to begin frame")) ) {
					AllClipsForeachWithUndo(p => p.ToBeginFrame());
				}
				if ( GUILayout.Button(new GUIContent("<", "to prev frame")) ) {
					AllClipsForeachWithUndo(p => p.ToPrevFrame());
				}
				GUILayout.Label(string.Format(
					"{0}/{1}",
					GetClipsCurrentFrameForView(), GetClipsFrameCountForView()));
				if ( GUILayout.Button(new GUIContent(">", "to next frame")) ) {
					AllClipsForeachWithUndo(p => p.ToNextFrame());
				}
				if ( GUILayout.Button(new GUIContent(">>", "to end frame")) ) {
					AllClipsForeachWithUndo(p => p.ToEndFrame());
				}
			});
		}

		private void SetupPreviews() {
			ShutdownPreviews();
			_previews = targets
				.OfType<SwfView>()
				.Where(p => p.clip)
				.ToDictionary(p => p, p => {
					var preview = new SwfClipPreview();
					preview.Initialize(new Object[] { p.clip });
					return preview;
				});
		}

		private void ShutdownPreviews() {
			foreach ( var p in _previews ) {
		        p.Value.Shutdown();
			}
			_previews.Clear();
		}

		// ---------------------------------------------------------------------
		//
		// Messages
		//
		// ---------------------------------------------------------------------

		private void OnEnable() {
			_clips = targets.OfType<SwfView>().ToList();
			SetupPreviews();
		}

		private void OnDisable() {
			ShutdownPreviews();
			_clips.Clear();
		}

		public override void OnInspectorGUI() {
			serializedObject.Update();
			DrawDefaultInspector();
			DrawSequence();
			DrawCurrentFrame();
			if ( GUI.changed ) {
				serializedObject.ApplyModifiedProperties();
				SetupPreviews();
			}
		}

		public override bool RequiresConstantRepaint() => _clips.Count > 0;

		public override bool HasPreviewGUI() => _clips.Count > 0;

		public override void OnPreviewGUI(Rect r, GUIStyle background)
		{
			if (Event.current.type != EventType.Repaint)
				return;

			var clip = target as SwfView;
			if ( _previews.TryGetValue(clip, out var preview) ) {
				preview.SetSequence(clip.sequence);
				preview.DrawPreview(r);
			}
		}
	}
}