using System;
using Boxcat.Core;
using UnityEngine.Assertions;

namespace FT
{
    public class SwfRandomSequenceLooper
    {
        private readonly SwfPhasor _phasor;

        private SwfSequenceId[] _sequences;
        private byte[] _weights;
        private bool _subscribed;

        private Action<SwfPhasor> _handleOnFinishBacking;
        private Action<SwfPhasor> _handleOnFinish => _handleOnFinishBacking ??= HandleOnFinish;


        public SwfRandomSequenceLooper(SwfPhasor phasor)
        {
            _phasor = phasor;
            Assert.IsFalse(_phasor.isPlaying);
        }

        public void DisposeWithoutStop()
        {
            if (_subscribed)
            {
                _phasor.OnFinish -= _handleOnFinish;
                _subscribed = false;
            }
        }

        public void SetSequences(SwfSequenceId[] sequences, byte[] weights)
        {
            _sequences = sequences;
            _weights = weights;
        }

        public void Play()
        {
            var sequenceId = _sequences[Sampler.FromWeights(_weights)];
            _phasor.Play(sequenceId, resetFrameTimer: true);

            if (_subscribed is false)
            {
                _phasor.OnFinish += _handleOnFinish;
                _subscribed = true;
            }
        }

        private void HandleOnFinish(SwfPhasor phasor)
        {
            Assert.IsTrue(_subscribed);
            var sequenceId = _sequences[Sampler.FromWeights(_weights)];
            _phasor.Play(sequenceId, resetFrameTimer: false); // keep frame timer
        }
    }
}