﻿using UnityEngine;
using UnityEditor;

using System.Collections.Generic;

using FTRuntime;
using FTRuntime.Internal;

namespace FTEditor.Editors {
	[CustomEditor(typeof(SwfManager))]
	public class SwfManagerEditor : Editor {
		SwfManager                 _manager       = null;
		SwfList<SwfClipController> _controllers   = new SwfList<SwfClipController>();
		bool                       _groupsFoldout = true;

		void DrawCounts() {
			SwfEditorUtils.DoWithEnabledGUI(false, () => {
				EditorGUILayout.IntField(
					"Clip count",
					_manager.clipCount);
				EditorGUILayout.IntField(
					"Controller count",
					_manager.controllerCount);
			});
		}

		void DrawControls() {
			SwfEditorUtils.DoRightHorizontalGUI(() => {
				if ( _manager.isPaused && GUILayout.Button("Resume") ) {
					_manager.Resume();
				}
				if ( _manager.isPlaying && GUILayout.Button("Pause") ) {
					_manager.Pause();
				}
			});
		}

		void DrawGroupControls() {
			var group_names = GetAllGroupNames();
			if ( group_names.Count > 0 ) {
				_groupsFoldout = EditorGUILayout.Foldout(_groupsFoldout, "Groups");
				if ( _groupsFoldout ) {
					foreach ( var group_name in group_names ) {
						SwfEditorUtils.DoWithEnabledGUI(false, () => {
							EditorGUILayout.TextField("Name", group_name);
						});
						EditorGUI.BeginChangeCheck();
						var new_rate_scale = EditorGUILayout.FloatField(
							"Rate Scale", _manager.GetGroupRateScale(group_name));
						if ( EditorGUI.EndChangeCheck() ) {
							_manager.SetGroupRateScale(group_name, new_rate_scale);
						}
						SwfEditorUtils.DoRightHorizontalGUI(() => {
							if ( _manager.IsGroupPaused(group_name) && GUILayout.Button("Resume") ) {
								_manager.ResumeGroup(group_name);
							}
							if ( _manager.IsGroupPlaying(group_name) && GUILayout.Button("Pause") ) {
								_manager.PauseGroup(group_name);
							}
						});
					}
				}
			}
		}

		HashSet<string> GetAllGroupNames() {
			var result = new HashSet<string>();
			for ( int i = 0, e = _controllers.Count; i < e; ++i ) {
				var ctrl = _controllers[i];
				if ( !string.IsNullOrEmpty(ctrl.groupName) ) {
					result.Add(ctrl.groupName);
				}
			}
			return result;
		}

		// ---------------------------------------------------------------------
		//
		// Messages
		//
		// ---------------------------------------------------------------------

		void OnEnable() {
			_manager = target as SwfManager;
			_manager.GetAllControllers(_controllers);
		}

		public override void OnInspectorGUI() {
			serializedObject.Update();
			DrawDefaultInspector();
			DrawCounts();
			if ( Application.isPlaying ) {
				DrawControls();
				DrawGroupControls();
			}
			if ( GUI.changed ) {
				serializedObject.ApplyModifiedProperties();
			}
		}
	}
}