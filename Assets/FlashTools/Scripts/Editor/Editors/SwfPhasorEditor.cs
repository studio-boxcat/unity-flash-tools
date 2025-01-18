using UnityEngine;
using UnityEditor;

using System.Linq;
using System.Collections.Generic;

using FTRuntime;

namespace FTEditor.Editors {
	[CustomEditor(typeof(SwfPhasor)), CanEditMultipleObjects]
	internal class SwfPhasorEditor : UnityEditor.Editor {
		private List<SwfPhasor> _phasors = new();

		private void AllControllersForeach(System.Action<SwfPhasor> act) {
			foreach ( var phasor in _phasors ) {
				act(phasor);
			}
		}

		private void DrawClipControls() {
			SwfEditorUtils.DoRightHorizontalGUI(() => {
				if ( GUILayout.Button("Stop") ) {
					AllControllersForeach(phasor => phasor.Stop(phasor.isStopped));
				}
				if ( GUILayout.Button("Play") ) {
					AllControllersForeach(phasor => {
						var rewind =
							phasor.isPlaying ||
							(phasor.View && (
								phasor.View.currentFrame == 0 ||
								phasor.View.currentFrame == phasor.View.frameCount - 1));
						phasor.Play(rewind);
					});
				}
			});
		}

		// ---------------------------------------------------------------------
		//
		// Messages
		//
		// ---------------------------------------------------------------------

		private void OnEnable() {
			_phasors = targets.OfType<SwfPhasor>().ToList();
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