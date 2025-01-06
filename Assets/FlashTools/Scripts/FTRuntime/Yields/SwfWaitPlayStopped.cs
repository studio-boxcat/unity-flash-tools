using UnityEngine;

namespace FTRuntime.Yields {
	public class SwfWaitPlayStopped : CustomYieldInstruction {
		private SwfPhasor _waitCtrl;

		public SwfWaitPlayStopped(SwfPhasor ctrl) {
			Subscribe(ctrl);
		}

		public SwfWaitPlayStopped Reuse(SwfPhasor ctrl) {
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

		private SwfWaitPlayStopped Subscribe(SwfPhasor ctrl) {
			Unsubscribe();
			if ( ctrl ) {
				_waitCtrl = ctrl;
				ctrl.OnPlayStoppedEvent += OnPlayStopped;
			}
			return this;
		}

		private void Unsubscribe() {
			if ( _waitCtrl != null ) {
				_waitCtrl.OnPlayStoppedEvent -= OnPlayStopped;
				_waitCtrl = null;
			}
		}

		private void OnPlayStopped(SwfPhasor ctrl) {
			Unsubscribe();
		}
	}
}