using UnityEngine;

namespace FTRuntime.Yields {
	public class SwfWaitStopPlaying : CustomYieldInstruction {
		private SwfPhasor _waitCtrl;

		public SwfWaitStopPlaying(SwfPhasor ctrl) {
			Subscribe(ctrl);
		}

		public SwfWaitStopPlaying Reuse(SwfPhasor ctrl) {
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

		private SwfWaitStopPlaying Subscribe(SwfPhasor ctrl) {
			Unsubscribe();
			if ( ctrl ) {
				_waitCtrl = ctrl;
				ctrl.OnStopPlayingEvent += OnStopPlaying;
			}
			return this;
		}

		private void Unsubscribe() {
			if ( _waitCtrl != null ) {
				_waitCtrl.OnStopPlayingEvent -= OnStopPlaying;
				_waitCtrl = null;
			}
		}

		private void OnStopPlaying(SwfPhasor ctrl) {
			Unsubscribe();
		}
	}
}