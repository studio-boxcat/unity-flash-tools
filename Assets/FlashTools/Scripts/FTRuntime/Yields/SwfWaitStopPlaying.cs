using UnityEngine;

namespace FTRuntime.Yields {
	public class SwfWaitStopPlaying : CustomYieldInstruction {
		SwfClipPhasor _waitCtrl;

		public SwfWaitStopPlaying(SwfClipPhasor ctrl) {
			Subscribe(ctrl);
		}

		public SwfWaitStopPlaying Reuse(SwfClipPhasor ctrl) {
			return Subscribe(ctrl);
		}

		public override bool keepWaiting {
			get {
				return _waitCtrl != null;
			}
		}

		// ---------------------------------------------------------------------
		//
		// Internal
		//
		// ---------------------------------------------------------------------

		SwfWaitStopPlaying Subscribe(SwfClipPhasor ctrl) {
			Unsubscribe();
			if ( ctrl ) {
				_waitCtrl = ctrl;
				ctrl.OnStopPlayingEvent += OnStopPlaying;
			}
			return this;
		}

		void Unsubscribe() {
			if ( _waitCtrl != null ) {
				_waitCtrl.OnStopPlayingEvent -= OnStopPlaying;
				_waitCtrl = null;
			}
		}

		void OnStopPlaying(SwfClipPhasor ctrl) {
			Unsubscribe();
		}
	}
}