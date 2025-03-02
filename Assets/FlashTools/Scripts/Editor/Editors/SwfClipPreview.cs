using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace FT.Editors {
	internal class SwfClipPreview : ObjectPreview {
		private int                   _sequence     = 0;
		private PreviewRenderUtility  _previewUtils = null;

		// ---------------------------------------------------------------------
		//
		// Functions
		//
		// ---------------------------------------------------------------------

		public void Shutdown() {
			_previewUtils.Cleanup();
			Cleanup();
		}

		// ---------------------------------------------------------------------
		//
		// Messages
		//
		// ---------------------------------------------------------------------

		public override void Initialize(Object[] targets) {
			base.Initialize(targets);
			_previewUtils ??= new PreviewRenderUtility();
		}

		public override bool HasPreviewGUI() {
			return true;
		}

		public override void OnPreviewSettings() {
			var clip = m_Targets.Length == 1
				? m_Targets[0] as SwfClip
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
			GUILayout.Label(SwfSequenceIdUtils.ToName(sequence.Id), EditorStyles.whiteLabel);

			if ( clip.Sequences.Length > 1 ) {
				if ( GUILayout.Button(">", EditorStyles.miniButton) ) {
					++_sequence;
					if ( _sequence >= clip.Sequences.Length ) {
						_sequence = 0;
					}
				}
			}
		}

		private MaterialPropertyBlock _mpb;

		public override void OnPreviewGUI(Rect r, GUIStyle background)
		{
			// Check eligibility
			if (Event.current.type is not EventType.Repaint) return;
			var clip = target as SwfClip;
			if (clip == null) return;
			var camera = _previewUtils.camera;
			if (camera == null) return;

			// Prepare
			_previewUtils.BeginPreview(r, background);
			var cmd = new CommandBuffer();
			cmd.ClearRenderTarget(true, false, default);
			var mesh = MeshBuilder.CreateEmpty();

			// Setup camera
			var seq = clip.Sequences[_sequence];
			var bounds = new Bounds(new Vector3(0, 1.4f, 0), new Vector3(4, 4, 0));
			SetViewProjectionMatrices(cmd, camera, bounds);

			// Draw
			clip.BuildMesh(GetFrameForClip(seq.Frames, clip.FrameRate), mesh);
			_mpb ??= new MaterialPropertyBlock();
			MaterialConfigurator.SetTexture(_mpb, clip.Atlas);
			var materials = MaterialStore.Get(seq.MaterialGroup);
			for ( var i = 0; i < materials.Length; ++i )
				cmd.DrawMesh(mesh, Matrix4x4.identity, materials[i], i, -1, _mpb);
			Graphics.ExecuteCommandBuffer(cmd);

			// Cleanup
			Object.DestroyImmediate(mesh);
			cmd.Dispose();
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

			static SwfFrameId GetFrameForClip(SwfFrameId[] frames, byte frameRate) {
				var frame_time = (float)(EditorApplication.timeSinceStartup * frameRate);
				var frame_index = Mathf.FloorToInt(frame_time % frames.Length);
				return frames[frame_index];
			}
		}
	}
}