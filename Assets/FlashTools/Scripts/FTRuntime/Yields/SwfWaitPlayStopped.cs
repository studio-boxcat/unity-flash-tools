using UnityEngine;

namespace FTRuntime.Yields {
	public class SwfWaitPlayStopped : CustomYieldInstruction {
		SwfClipPhasor _waitCtrl;

		public SwfWaitPlayStopped(SwfClipPhasor ctrl) {
			Subscribe(ctrl);
		}

		public SwfWaitPlayStopped Reuse(SwfClipPhasor ctrl) {
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

		SwfWaitPlayStopped Subscribe(SwfClipPhasor ctrl) {
			Unsubscribe();
			if ( ctrl ) {
				_waitCtrl = ctrl;
				ctrl.OnPlayStoppedEvent += OnPlayStopped;
			}
			return this;
		}

		void Unsubscribe() {
			if ( _waitCtrl != null ) {
				_waitCtrl.OnPlayStoppedEvent -= OnPlayStopped;
				_waitCtrl = null;
			}
		}

		void OnPlayStopped(SwfClipPhasor ctrl) {
			Unsubscribe();
		}
	}
}