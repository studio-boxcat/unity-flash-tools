namespace FTRuntime.Yields {
	public static class SwfWaitExtensions {

		// ---------------------------------------------------------------------
		//
		// WaitFor[Event]
		//
		// ---------------------------------------------------------------------

		/// <summary>Yield instruction for wait animation stop event</summary>
		/// <returns>Yield instruction for wait animation stop event</returns>
		/// <param name="ctrl">The controller</param>
		public static SwfWaitStopPlaying WaitForStopPlaying(
			this SwfPhasor ctrl)
		{
			return new SwfWaitStopPlaying(ctrl);
		}

		/// <summary>Yield instruction for wait animation play event</summary>
		/// <returns>Yield instruction for wait animation play event</returns>
		/// <param name="ctrl">The controller</param>
		public static SwfWaitPlayStopped WaitForPlayStopped(
			this SwfPhasor ctrl)
		{
			return new SwfWaitPlayStopped(ctrl);
		}

		// ---------------------------------------------------------------------
		//
		// PlayAndWait[Event]
		//
		// ---------------------------------------------------------------------

		/// <summary>Play with specified rewind action</summary>
		/// <returns>Yield instruction for wait animation stop event</returns>
		/// <param name="ctrl">The clip controller</param>
		/// <param name="rewind">If set to <c>true</c> rewind animation to begin frame</param>
		public static SwfWaitStopPlaying PlayAndWaitStop(
			this SwfPhasor ctrl, bool rewind)
		{
			ctrl.Play(rewind);
			return WaitForStopPlaying(ctrl);
		}

		/// <summary>Changes the animation sequence and play controller with rewind</summary>
		/// <returns>Yield instruction for wait animation stop event</returns>
		/// <param name="ctrl">The clip controller</param>
		/// <param name="sequence">The new sequence</param>
		public static SwfWaitStopPlaying PlayAndWaitStop(
			this SwfPhasor ctrl, SwfSequenceId sequence)
		{
			ctrl.Play(sequence);
			return WaitForStopPlaying(ctrl);
		}

		// ---------------------------------------------------------------------
		//
		// GotoAndPlayAndWait[Event]
		//
		// ---------------------------------------------------------------------

		/// <summary>Changes the animation frame with plays it</summary>
		/// <returns>Yield instruction for wait animation stop event</returns>
		/// <param name="ctrl">The clip controller</param>
		/// <param name="frame">The new current frame</param>
		public static SwfWaitStopPlaying GotoAndPlayAndWaitStop(
			this SwfPhasor ctrl, ushort frame)
		{
			ctrl.GotoAndPlay(frame);
			return WaitForStopPlaying(ctrl);
		}

		/// <summary>Changes the animation sequence and frame with plays it</summary>
		/// <returns>Yield instruction for wait animation stop event</returns>
		/// <param name="ctrl">The clip controller</param>
		/// <param name="sequence">The new sequence</param>
		/// <param name="frame">The new current frame</param>
		public static SwfWaitStopPlaying GotoAndPlayAndWaitStop(
			this SwfPhasor ctrl, SwfSequenceId sequence, ushort frame)
		{
			ctrl.GotoAndPlay(sequence, frame);
			return WaitForStopPlaying(ctrl);
		}

		// ---------------------------------------------------------------------
		//
		// StopAndWait[Event]
		//
		// ---------------------------------------------------------------------

		/// <summary>Stop with specified rewind action</summary>
		/// <returns>Yield instruction for wait animation play event</returns>
		/// <param name="ctrl">The clip controller</param>
		/// <param name="rewind">If set to <c>true</c> rewind animation to begin frame</param>
		public static SwfWaitPlayStopped StopAndWaitPlay(
			this SwfPhasor ctrl, bool rewind)
		{
			ctrl.Stop(rewind);
			return WaitForPlayStopped(ctrl);
		}

		/// <summary>Changes the animation sequence and stop controller with rewind</summary>
		/// <returns>Yield instruction for wait animation play event</returns>
		/// <param name="ctrl">The clip controller</param>
		/// <param name="sequence">The new sequence</param>
		public static SwfWaitPlayStopped StopAndWaitPlay(
			this SwfPhasor ctrl, SwfSequenceId sequence)
		{
			ctrl.Stop(sequence);
			return WaitForPlayStopped(ctrl);
		}

		// ---------------------------------------------------------------------
		//
		// GotoAndStopAndWait[Event]
		//
		// ---------------------------------------------------------------------

		/// <summary>Changes the animation frame with stops it</summary>
		/// <returns>Yield instruction for wait animation play event</returns>
		/// <param name="ctrl">The clip controller</param>
		/// <param name="frame">The new current frame</param>
		public static SwfWaitPlayStopped GotoAndStopAndWaitPlay(
			this SwfPhasor ctrl, ushort frame)
		{
			ctrl.GotoAndStop(frame);
			return WaitForPlayStopped(ctrl);
		}

		/// <summary>Changes the animation sequence and frame with stops it</summary>
		/// <returns>Yield instruction for wait animation play event</returns>
		/// <param name="ctrl">The clip controller</param>
		/// <param name="sequence">The new sequence</param>
		/// <param name="frame">The new current frame</param>
		public static SwfWaitPlayStopped GotoAndStopAndWaitPlay(
			this SwfPhasor ctrl, SwfSequenceId sequence, ushort frame)
		{
			ctrl.GotoAndStop(sequence, frame);
			return WaitForPlayStopped(ctrl);
		}
	}
}