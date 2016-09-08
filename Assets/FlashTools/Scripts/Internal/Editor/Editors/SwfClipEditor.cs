﻿using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;

namespace FlashTools.Internal {
	[CustomEditor(typeof(SwfClip)), CanEditMultipleObjects]
	public class SwfClipEditor : Editor {
		List<SwfClip> _clips = new List<SwfClip>();

		void AllClipsForeachWithUndo(Action<SwfClip> act) {
			Undo.RecordObjects(
				_clips.ToArray(),
				"Inspector");
			foreach ( var clip in _clips ) {
				act(clip);
				EditorUtility.SetDirty(clip);
			}
		}

		int GetMinClipsFrameCount() {
			return _clips.Count > 0
				? _clips.Min(clip => clip.frameCount)
				: 0;
		}

		string GetClipsFrameCountStr() {
			return _clips.Aggregate(string.Empty, (acc, clip) => {
				var frame_count     = clip.frameCount > 0 ? clip.frameCount - 1 : 0;
				var frame_count_str = frame_count.ToString();
				return string.IsNullOrEmpty(acc)
					? frame_count_str
					: (acc != frame_count_str ? "--" : acc);
			});
		}

		string GetClipsCurrentFrameStr() {
			return _clips.Aggregate(string.Empty, (acc, clip) => {
				var current_frame     = clip.currentFrame;
				var current_frame_str = current_frame.ToString();
				return string.IsNullOrEmpty(acc)
					? current_frame_str
					: (acc != current_frame_str ? "--" : acc);
			});
		}

		List<string> GetAllSequences(bool include_empty) {
			var result       = new List<string>();
			var result_clips = _clips
				.Where (p => p.clip && p.clip.Sequences.Count > 0)
				.Select(p => p.clip.Sequences)
				.Where (p => p.All(s => !string.IsNullOrEmpty(s.Name)))
				.ToList();
			if ( result_clips.Count > 0 ) {
				result = result_clips.First()
					.Select(p => p.Name)
					.ToList();
				var sequences_enum = result_clips
					.Select(p => p.Select(s => s.Name));
				foreach ( var sequences in sequences_enum ) {
					result = result
						.Where(p => sequences.Contains(p))
						.ToList();
				}
				if ( include_empty ) {
					result.Add(string.Empty);
				}
			}
			return result;
		}

		void DrawSequence() {
			var all_sequences = GetAllSequences(true);
			if ( all_sequences.Count > 0 ) {
				var sequence_prop = SwfEditorUtils.GetPropertyByName(serializedObject, "_sequence");
				SwfEditorUtils.DoWithMixedValue(
					sequence_prop.hasMultipleDifferentValues, () => {
						var sequence_index = EditorGUILayout.Popup(
							"Sequence",
							sequence_prop.hasMultipleDifferentValues
								? all_sequences.FindIndex(p => string.IsNullOrEmpty(p))
								: all_sequences.FindIndex(p => p == sequence_prop.stringValue),
							all_sequences.ToArray());
						if ( sequence_index >= 0 && sequence_index < all_sequences.Count ) {
							var new_sequence = all_sequences[sequence_index];
							if ( !string.IsNullOrEmpty(new_sequence) ) {
								if ( sequence_prop.hasMultipleDifferentValues ) {
									sequence_prop.stringValue = string.Empty;
								}
								sequence_prop.stringValue = new_sequence;
								sequence_prop.serializedObject.ApplyModifiedProperties();
							}
						}
					});
			}
		}

		void DrawCurrentFrame() {
			var min_frame_count = GetMinClipsFrameCount();
			if ( min_frame_count > 1 ) {
				EditorGUILayout.IntSlider(
					SwfEditorUtils.GetPropertyByName(serializedObject, "_currentFrame"),
					0,
					min_frame_count - 1,
					"Current frame");
				DrawClipControls();
			}
		}

		void DrawClipControls() {
			EditorGUILayout.Space();
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			{
				if ( GUILayout.Button(new GUIContent("<<", "to begin frame")) ) {
					AllClipsForeachWithUndo(p => p.ToBeginFrame());
				}
				if ( GUILayout.Button(new GUIContent("<", "to prev frame")) ) {
					AllClipsForeachWithUndo(p => p.ToPrevFrame());
				}
				GUILayout.Label(string.Format(
					"{0}/{1}",
					GetClipsCurrentFrameStr(), GetClipsFrameCountStr()));
				if ( GUILayout.Button(new GUIContent(">", "to next frame")) ) {
					AllClipsForeachWithUndo(p => p.ToNextFrame());
				}
				if ( GUILayout.Button(new GUIContent(">>", "to end frame")) ) {
					AllClipsForeachWithUndo(p => p.ToEndFrame());
				}
			}
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}

		// ---------------------------------------------------------------------
		//
		// Messages
		//
		// ---------------------------------------------------------------------

		void OnEnable() {
			_clips = targets
				.OfType<SwfClip>()
				.ToList();
		}

		public override void OnInspectorGUI() {
			serializedObject.Update();
			DrawDefaultInspector();
			DrawSequence();
			DrawCurrentFrame();
			if ( GUI.changed ) {
				serializedObject.ApplyModifiedProperties();
			}
		}
	}
}