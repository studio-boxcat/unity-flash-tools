using UnityEngine;
using UnityEditor;

using System.Linq;
using System.Collections.Generic;

using FTRuntime;

namespace FTEditor.Editors {
	[CustomEditor(typeof(SwfClipPhasor)), CanEditMultipleObjects]
	class SwfClipControllerEditor : Editor {
		List<SwfClipPhasor> _controllers = new();

		void AllControllersForeach(System.Action<SwfClipPhasor> act) {
			foreach ( var controller in _controllers ) {
				act(controller);
			}
		}

		void DrawClipControls() {
			SwfEditorUtils.DoRightHorizontalGUI(() => {
				if ( GUILayout.Button("Stop") ) {
					AllControllersForeach(ctrl => ctrl.Stop(ctrl.isStopped));
				}
				if ( GUILayout.Button("Play") ) {
					AllControllersForeach(ctrl => {
						var rewind =
							ctrl.isPlaying ||
							(ctrl.clip && (
								ctrl.clip.currentFrame == 0 ||
								ctrl.clip.currentFrame == ctrl.clip.frameCount - 1));
						ctrl.Play(rewind);
					});
				}
			});
		}

		// ---------------------------------------------------------------------
		//
		// Messages
		//
		// ---------------------------------------------------------------------

		void OnEnable() {
			_controllers = targets.OfType<SwfClipPhasor>().ToList();
		}

		public override void OnInspectorGUI() {
			serializedObject.Update();
			DrawDefaultInspector();
			if ( Application.isPlaying ) {
				DrawClipControls();
			}
			if ( GUI.changed ) {
				serializedObject.ApplyModifiedProperties();
			}
		}
	}
}