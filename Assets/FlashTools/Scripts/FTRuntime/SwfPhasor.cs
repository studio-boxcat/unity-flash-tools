using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;

namespace FTRuntime {
	[ExecuteAlways, DisallowMultipleComponent]
	[RequireComponent(typeof(SwfView))]
	public class SwfPhasor : MonoBehaviour {

		[SerializeField, Required, ChildGameObjectsOnly]
		private SwfView _view;
		public SwfView View => _view;
		[SerializeField] private bool _autoPlay = true;
		public bool autoPlay { set => _autoPlay = value; }
		[SerializeField] private LoopModes _loopMode = LoopModes.Loop;
		public LoopModes loopMode => _loopMode;


		private bool    _isPlaying = false;
		public bool isPlaying => _isPlaying;
		private bool    _isVisible = false;
		private float   _frameTimer = 0.0f;

		public enum LoopModes : byte { Once, Loop }

		public event Action<SwfPhasor, bool> OnPause;


		public void Play(SwfSequenceId sequence) {
			_isPlaying = true;
			_frameTimer = 0.0f;
			_view.SetSequence(sequence, 0);
		}

		public void Play(bool rewind) {
			_isPlaying = true;
			_frameTimer = 0.0f;
			if ( rewind ) _view.ToBeginFrame();
		}

		public void Pause() {
			var was_playing = _isPlaying;
			_isPlaying = false;
			_frameTimer = 0.0f;
			if (was_playing)
				OnPause?.Invoke(this, false);
		}

		private void ApplyPlayEnded() {
			Assert.IsTrue(_isPlaying, "SwfClipController. Incorrect state.");
			var was_playing = _isPlaying;
			_isPlaying = false;
			if (was_playing)
				OnPause?.Invoke(this, true);
		}

		private void OnEnable() {
#if UNITY_EDITOR
			if (!Application.isPlaying)
				return;
#endif

			if ( _autoPlay )
				Play(false);
		}

		private void OnDisable() => Pause();

		private void LateUpdate()
		{
			Assert.IsNotNull(_view, "Clip is destroyed.");

			if (_isPlaying is false || _isVisible is false)
				return;

			_frameTimer += Time.deltaTime;

			// Calculate the number of frames that have passed.
			var frame_passed = (int) (_frameTimer * _view.frameRate);
			if (frame_passed is 0)
				return;

			// Deduct the frame time from the timer.
			_frameTimer -= frame_passed / (float) _view.frameRate;
			Assert.IsTrue(_frameTimer >= 0, "SwfClipController. Incorrect frame timer.");

			// Update the frame according to the loop mode.
			Assert.IsTrue(loopMode is LoopModes.Loop or LoopModes.Once,
				"SwfClipController. Incorrect loop mode.");
			if (loopMode is LoopModes.Loop)
			{
				_view.UpdateFrame_Loop(frame_passed);
			}
			else
			{
				if (!_view.UpdateFrame_Clamp(frame_passed))
					ApplyPlayEnded(); // Pause if it reaches the end.
			}
		}

		private void OnBecameVisible() => _isVisible = true;
		private void OnBecameInvisible() => _isVisible = false;
	}
}