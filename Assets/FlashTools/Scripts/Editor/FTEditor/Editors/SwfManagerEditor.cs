using UnityEngine;
using UnityEditor;

using System.Linq;
using System.Collections.Generic;

using FTRuntime;

namespace FTEditor.Editors {
	[CustomEditor(typeof(SwfManager))]
	class SwfManagerEditor : Editor {
		SwfManager              _manager       = null;
		List<SwfClipController> _controllers   = new List<SwfClipController>();
		bool                    _groupsFoldout = true;

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
				if ( _manager.useUnscaledDt && GUILayout.Button("Use Scaled Dt") ) {
					_manager.useUnscaledDt = false;
				}
				if ( !_manager.useUnscaledDt && GUILayout.Button("Use Unscaled Dt") ) {
					_manager.useUnscaledDt = true;
				}
				if ( _manager.isPaused && GUILayout.Button("Resume") ) {
					_manager.Resume();
				}
				if ( _manager.isPlaying && GUILayout.Button("Pause") ) {
					_manager.Pause();
				}
			});
		}

		// ---------------------------------------------------------------------
		//
		// Messages
		//
		// ---------------------------------------------------------------------

		void OnEnable() {
			_manager     = target as SwfManager;
			_controllers = FindObjectsOfType<SwfClipController>().ToList();
		}

		public override void OnInspectorGUI() {
			serializedObject.Update();
			DrawDefaultInspector();
			DrawCounts();
			if ( Application.isPlaying ) {
				DrawControls();
			}
			if ( GUI.changed ) {
				serializedObject.ApplyModifiedProperties();
			}
		}
	}
}