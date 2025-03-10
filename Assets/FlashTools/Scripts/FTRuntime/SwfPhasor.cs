using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;

namespace FT {
	[ExecuteAlways, DisallowMultipleComponent]
	[RequireComponent(typeof(SwfView))]
	public class SwfPhasor : MonoBehaviour {

		[SerializeField, Required, ChildGameObjectsOnly]
		private SwfView _view;
		public SwfView View => _view;
		[SerializeField]
		private LoopModes _loopMode = LoopModes.Loop;
		public LoopModes loopMode => _loopMode;


		private bool    _isPlaying = false;
		public bool isPlaying => _isPlaying;

		private bool    _isVisible = false;
		private float   _frameTimer = 0.0f;

		public enum LoopModes : byte { Once, Loop }

		public event Action<SwfPhasor> OnFinish; // successfully reached the end of the sequence.


		public void Play(SwfSequenceId sequence, bool resetFrameTimer) {
			_isPlaying = true;
			if (resetFrameTimer) _frameTimer = 0.0f;
			_view.SetSequence(sequence, frame: 0);
		}

		public void Pause() {
			_isPlaying = false;
		}

		private void Finish() {
			Assert.IsTrue(_isPlaying, "SwfClipController. Incorrect state.");
			_isPlaying = false;
			OnFinish?.Invoke(this);
		}

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
					Finish(); // Pause if it reaches the end.
			}
		}

		private void OnBecameVisible() => _isVisible = true;
		private void OnBecameInvisible() => _isVisible = false;
	}
}