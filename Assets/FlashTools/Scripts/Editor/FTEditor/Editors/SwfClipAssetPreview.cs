using UnityEngine;
using UnityEditor;
using FTRuntime;

namespace FTEditor.Editors {
	class SwfClipAssetPreview : ObjectPreview {
		int                   _sequence     = 0;
		MaterialPropertyBlock _matPropBlock = null;
		PreviewRenderUtility  _previewUtils = null;

		Texture2D targetAtlas {
			get {
				var clip = target as SwfClipAsset;
				return clip.Atlas;
			}
		}

		SwfClipAsset.Frame targetFrame {
			get {
				var clip = target as SwfClipAsset;
				return GetFrameForClip(clip, _sequence);
			}
		}

		SwfClipAsset.Sequence targetSequence {
			get {
				var clip = target as SwfClipAsset;
				return clip.Sequences[_sequence];
			}
		}

		static SwfClipAsset.Frame GetFrameForClip(SwfClipAsset clip, int sequence_index) {
			var sequence = clip.Sequences[sequence_index];
			var frames = sequence.Frames;
			var frame_time = (float)(EditorApplication.timeSinceStartup * clip.FrameRate);
			var frame_index = Mathf.FloorToInt(frame_time) % frames.Length;
			return frames[frame_index];
		}

		static Bounds CalculateBoundsForSequence(SwfClipAsset.Sequence sequence)
		{
			var frames = sequence.Frames;
			if (frames.Length == 0)
				return new Bounds();

			var result = frames[0].Mesh.bounds;
			for (var i = 1; i < frames.Length; i++)
				result.Encapsulate(frames[i].Mesh.bounds);

			return result;
		}

		static void ConfigureCameraForSequence(Camera camera, SwfClipAsset.Sequence sequence) {
			var bounds              = CalculateBoundsForSequence(sequence);
			camera.orthographic     = true;
			camera.orthographicSize = Mathf.Max(
				Mathf.Abs(bounds.extents.x),
				Mathf.Abs(bounds.extents.y));
			camera.transform.position = new Vector3(
				bounds.center.x,
				bounds.center.y,
				-10.0f);
		}

		static Camera GetCameraFromPreviewUtils(PreviewRenderUtility previewUtils) {
			var cameraField = previewUtils.GetType().GetField("m_Camera");
			var cameraFieldValue = cameraField != null
				? cameraField.GetValue(previewUtils) as Camera
				: null;
			if ( cameraFieldValue ) {
				return cameraFieldValue;
			}
			var cameraProperty = previewUtils.GetType().GetProperty("camera");
			var cameraPropertyValue = cameraProperty != null
				? cameraProperty.GetValue(previewUtils, null) as Camera
				: null;
			if ( cameraPropertyValue ) {
				return cameraPropertyValue;
			}
			return null;
		}

		// ---------------------------------------------------------------------
		//
		// Functions
		//
		// ---------------------------------------------------------------------

		public void SetSequence(string sequence_name) {
			_sequence = -1;

			var clip = target as SwfClipAsset;
			var sequences = clip.Sequences;
			for (var i = 0; i < sequences.Length; i++)
			{
				if (sequences[i].Name == sequence_name)
					_sequence = i;
			}

			if (_sequence == -1)
				_sequence = 0;
		}

		public void Shutdown() {
			_matPropBlock.Clear();
			_previewUtils.Cleanup();
		#if UNITY_2021_1_OR_NEWER
			Cleanup();
		#endif
		}

		// ---------------------------------------------------------------------
		//
		// Messages
		//
		// ---------------------------------------------------------------------

		public override void Initialize(Object[] targets) {
			base.Initialize(targets);
			if ( _matPropBlock == null ) {
				_matPropBlock = new MaterialPropertyBlock();
			}
			if ( _previewUtils == null ) {
				_previewUtils = new PreviewRenderUtility();
			}
		}

		public override bool HasPreviewGUI() {
			return true;
		}

		public override void OnPreviewSettings() {
			var clip = m_Targets.Length == 1
				? m_Targets[0] as SwfClipAsset
				: null;

			if ( !clip || clip.Sequences == null ) {
				return;
			}

			if ( clip.Sequences.Length > 1 ) {
				if ( GUILayout.Button("<", EditorStyles.miniButton) ) {
					--_sequence;
					if ( _sequence < 0 ) {
						_sequence = clip.Sequences.Length - 1;
					}
				}
			}

			var sequence = clip.Sequences[_sequence];
			if ( !string.IsNullOrEmpty(sequence.Name) ) {
				GUILayout.Label(sequence.Name, EditorStyles.whiteLabel);
			}

			if ( clip.Sequences.Length > 1 ) {
				if ( GUILayout.Button(">", EditorStyles.miniButton) ) {
					++_sequence;
					if ( _sequence >= clip.Sequences.Length ) {
						_sequence = 0;
					}
				}
			}
		}

		public override void OnPreviewGUI(Rect r, GUIStyle background) {
			if ( Event.current.type == EventType.Repaint ) {
				_previewUtils.BeginPreview(r, background);
				{
					_matPropBlock.SetTexture(
						"_MainTex",
						targetAtlas ? targetAtlas : Texture2D.whiteTexture);
					var camera = GetCameraFromPreviewUtils(_previewUtils);
					if ( camera ) {
						ConfigureCameraForSequence(camera, targetSequence);

						var clip = target as SwfClipAsset;
						var frame = targetFrame;
						var materials = clip.MaterialGroups[frame.MaterialGroupIndex].Materials;
						for ( var i = 0; i < materials.Length; ++i ) {
							_previewUtils.DrawMesh(
								frame.Mesh,
								Matrix4x4.identity,
								materials[i],
								i,
								_matPropBlock);
						}
						camera.Render();
					}
				}
				_previewUtils.EndAndDrawPreview(r);
			}
		}
	}
}