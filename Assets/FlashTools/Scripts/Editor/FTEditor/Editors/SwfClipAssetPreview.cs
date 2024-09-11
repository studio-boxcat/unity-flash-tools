using UnityEngine;
using UnityEditor;
using FTRuntime;
using UnityEngine.Rendering;

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

		public override void OnPreviewGUI(Rect r, GUIStyle background)
		{
			if (Event.current.type is not EventType.Repaint) return;

			if (targetAtlas == null) return;
			var camera = _previewUtils.camera;
			if (camera == null) return;

			_previewUtils.BeginPreview(r, background);

			var cmd = new CommandBuffer();
			cmd.ClearRenderTarget(true, false, default);

			var bounds = CalculateBoundsForSequence(targetSequence);
			SetViewProjectionMatrices(cmd, camera, bounds);

			var clip = (SwfClipAsset) target;
			var frame = targetFrame;
			var materials = clip.MaterialGroups[frame.MaterialGroupIndex].Materials;
			_matPropBlock.SetTexture("_MainTex", targetAtlas);
			for ( var i = 0; i < materials.Length; ++i )
				cmd.DrawMesh(frame.Mesh, Matrix4x4.identity, materials[i], i, -1, _matPropBlock);

			Graphics.ExecuteCommandBuffer(cmd);
			_previewUtils.EndAndDrawPreview(r);
			return;

			static void SetViewProjectionMatrices(CommandBuffer cmd, Camera camera, Bounds bounds)
			{
				var orthoY = Mathf.Max(
					Mathf.Abs(bounds.extents.x) + 0.2f, // 0.2 for padding
					Mathf.Abs(bounds.extents.y) + 0.2f); // 0.2 for padding
				var aspect = camera.aspect;
				var orthoX = orthoY * aspect;

				cmd.SetViewProjectionMatrices(
					Matrix4x4.TRS(
						new Vector3(-bounds.center.x, -bounds.center.y, -10.0f),
						Quaternion.identity, Vector3.one),
					Matrix4x4.Ortho(
						-orthoX, orthoX,
						-orthoY, orthoY,
						-10, 10));
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
		}
	}
}