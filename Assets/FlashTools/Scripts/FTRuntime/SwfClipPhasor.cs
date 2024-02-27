using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;

namespace FTRuntime {
	[AddComponentMenu("FlashTools/SwfClipController")]
	[ExecuteAlways, DisallowMultipleComponent]
	[RequireComponent(typeof(SwfClip))]
	public class SwfClipPhasor : MonoBehaviour {

		[SerializeField, Required, ChildGameObjectsOnly]
		SwfClip _clip      = null;
		bool    _isPlaying = false;
		bool    _isVisible = false;
		float   _frameTimer = 0.0f;

		// ---------------------------------------------------------------------
		//
		// Events
		//
		// ---------------------------------------------------------------------

		/// <summary>
		/// Occurs when the controller stops played clip
		/// </summary>
		public event Action<SwfClipPhasor> OnStopPlayingEvent;

		/// <summary>
		/// Occurs when the controller plays stopped clip
		/// </summary>
		public event Action<SwfClipPhasor> OnPlayStoppedEvent;

		// ---------------------------------------------------------------------
		//
		// Serialized fields
		//
		// ---------------------------------------------------------------------

		[SerializeField]
		bool _autoPlay = true;

		[SerializeField]
		PlayModes _playMode = PlayModes.Forward;

		[SerializeField]
		LoopModes _loopMode = LoopModes.Loop;

		// ---------------------------------------------------------------------
		//
		// Properties
		//
		// ---------------------------------------------------------------------

		/// <summary>
		/// Controller play modes
		/// </summary>
		public enum PlayModes {
			/// <summary>
			/// Forward play mode
			/// </summary>
			Forward,
			/// <summary>
			/// Backward play mode
			/// </summary>
			Backward
		}

		/// <summary>
		/// Controller loop modes
		/// </summary>
		public enum LoopModes {
			/// <summary>
			/// Once loop mode
			/// </summary>
			Once,
			/// <summary>
			/// Repeat loop mode
			/// </summary>
			Loop
		}

		/// <summary>
		/// Gets or sets a value indicating whether controller play after awake on scene
		/// </summary>
		/// <value><c>true</c> if auto play; otherwise, <c>false</c></value>
		public bool autoPlay {
			get => _autoPlay;
			set => _autoPlay = value;
		}

		/// <summary>
		/// Gets or sets the controller play mode
		/// </summary>
		/// <value>The play mode</value>
		public PlayModes playMode => _playMode;

		/// <summary>
		/// Gets or sets the controller loop mode
		/// </summary>
		/// <value>The loop mode</value>
		public LoopModes loopMode => _loopMode;

		/// <summary>
		/// Gets the controller clip
		/// </summary>
		/// <value>The clip</value>
		public SwfClip clip => _clip;

		/// <summary>
		/// Gets a value indicating whether controller is playing
		/// </summary>
		/// <value><c>true</c> if is playing; otherwise, <c>false</c></value>
		public bool isPlaying => _isPlaying;

		/// <summary>
		/// Gets a value indicating whether controller is stopped
		/// </summary>
		/// <value><c>true</c> if is stopped; otherwise, <c>false</c></value>
		public bool isStopped => !_isPlaying;

		// ---------------------------------------------------------------------
		//
		// Functions
		//
		// ---------------------------------------------------------------------

		/// <summary>
		/// Changes the animation frame with stops it
		/// </summary>
		/// <param name="frame">The new current frame</param>
		public void GotoAndStop(int frame) {
			Assert.IsNotNull(clip);
			clip.SetFrame(frame);
			Stop(false);
		}

		/// <summary>
		/// Changes the animation sequence and frame with stops it
		/// </summary>
		/// <param name="sequence">The new sequence</param>
		/// <param name="frame">The new current frame</param>
		public void GotoAndStop(string sequence, int frame) {
			Assert.IsNotNull(clip);
			clip.SetSequence(sequence, frame);
			GotoAndStop(frame);
		}

		/// <summary>
		/// Changes the animation frame with plays it
		/// </summary>
		/// <param name="frame">The new current frame</param>
		public void GotoAndPlay(int frame) {
			Assert.IsNotNull(clip);
			clip.SetFrame(frame);
			Play(false);
		}

		/// <summary>
		/// Changes the animation sequence and frame with plays it
		/// </summary>
		/// <param name="sequence">The new sequence</param>
		/// <param name="frame">The new current frame</param>
		public void GotoAndPlay(string sequence, int frame) {
			Assert.IsNotNull(clip);
			clip.SetSequence(sequence, frame);
			GotoAndPlay(frame);
		}

		/// <summary>
		/// Stop with specified rewind action
		/// </summary>
		/// <param name="rewind">If set to <c>true</c> rewind animation to begin frame</param>
		public void Stop(bool rewind) {
			var is_playing = isPlaying;
			if ( is_playing ) {
				_isPlaying = false;
				_frameTimer = 0.0f;
			}
			if ( rewind ) {
				Rewind();
			}
			if ( is_playing && OnStopPlayingEvent != null ) {
				OnStopPlayingEvent(this);
			}
		}

		/// <summary>
		/// Changes the animation sequence and stop controller with rewind
		/// </summary>
		/// <param name="sequence">The new sequence</param>
		public void Stop(string sequence) {
			Assert.IsNotNull(clip);
			clip.SetSequence(sequence, 0);
			Stop(true);
		}

		/// <summary>
		/// Play with specified rewind action
		/// </summary>
		/// <param name="rewind">If set to <c>true</c> rewind animation to begin frame</param>
		public void Play(bool rewind) {
			var is_stopped = isStopped;
			if ( is_stopped ) {
				_isPlaying = true;
				_frameTimer = 0.0f;
			}
			if ( rewind ) {
				Rewind();
			}
			if ( is_stopped && OnPlayStoppedEvent != null ) {
				OnPlayStoppedEvent(this);
			}
		}

		/// <summary>
		/// Changes the animation sequence and play controller with rewind
		/// </summary>
		/// <param name="sequence">The new sequence</param>
		public void Play(string sequence) {
			Assert.IsNotNull(clip);
			clip.SetSequence(sequence, 0);
			Play(true);
		}

		/// <summary>
		/// Rewind animation to begin frame
		/// </summary>
		public void Rewind() {
			if (clip is null) return;

			switch ( playMode ) {
			case PlayModes.Forward:
				clip.ToBeginFrame();
				break;
			case PlayModes.Backward:
				clip.ToEndFrame();
				break;
			default:
				throw new UnityException($"SwfClipController. Incorrect play mode: {playMode}");
			}
		}

		// ---------------------------------------------------------------------
		//
		// Messages
		//
		// ---------------------------------------------------------------------

		void OnEnable() {
			if ( autoPlay && Application.isPlaying ) {
				Play(false);
			}
		}

		void OnDisable() {
			Stop(false);
		}

		void LateUpdate()
		{
			Assert.IsNotNull(clip, "Clip is destroyed.");

			if (_isPlaying is false || _isVisible is false)
				return;

			_frameTimer += Time.deltaTime;

			// Calculate the number of frames that have passed.
			var frame_passed = (int) (_frameTimer * clip.frameRate);
			if (frame_passed < 1.0f)
				return;

			// Deduct the frame time from the timer.
			_frameTimer -= frame_passed / clip.frameRate;
			Assert.IsTrue(_frameTimer >= 0, "SwfClipController. Incorrect frame timer.");

			// Reverse the frame passed if the play mode is backward.
			if (playMode is PlayModes.Backward)
				frame_passed = -frame_passed;

			// Update the frame according to the loop mode.
			if (loopMode is LoopModes.Loop)
			{
				clip.UpdateFrame_Loop(frame_passed);
			}
			else
			{
				Assert.AreEqual(loopMode, LoopModes.Once, "SwfClipController. Incorrect loop mode.");
				var normal = clip.UpdateFrame_Clamp(frame_passed);
				if (!normal) Stop(false); // Stop the animation if it reaches the end.
			}
		}

		void OnBecameVisible() => _isVisible = true;
		void OnBecameInvisible() => _isVisible = false;
	}
}