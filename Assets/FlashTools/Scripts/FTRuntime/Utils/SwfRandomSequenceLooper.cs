using System;
using Boxcat.Core;
using UnityEngine.Assertions;

namespace FT
{
    public class SwfRandomSequenceLooper
    {
        private readonly SwfPhasor _swfPhasor;

        private SwfSequenceId[] _sequences;
        private byte[] _weights;
        private bool _subscribed;

        private Action<SwfPhasor, bool> _handleOnPauseBacking;
        private Action<SwfPhasor, bool> _handleOnPause => _handleOnPauseBacking ??= HandleOnPause;


        public SwfRandomSequenceLooper(SwfPhasor swfPhasor)
        {
            _swfPhasor = swfPhasor;
            Assert.IsFalse(_swfPhasor.isPlaying);
        }

        public void DisposeWithoutStop()
        {
            if (_subscribed)
            {
                _swfPhasor.OnPause -= _handleOnPause;
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
            if (_subscribed)
            {
                _swfPhasor.OnPause -= _handleOnPause;
                _subscribed = false;
            }

            Internal_Play();
        }

        private void Internal_Play()
        {
            Assert.IsFalse(_subscribed);

            var sequenceIndex = Sampler.FromWeights(_weights);
            _swfPhasor.Play(_sequences[sequenceIndex]);
            _swfPhasor.OnPause += _handleOnPause;
            _subscribed = true;
        }

        private void HandleOnPause(SwfPhasor phasor, bool playEnded)
        {
            Assert.IsTrue(_subscribed);

            _swfPhasor.OnPause -= _handleOnPause;
            _subscribed = false;

            if (playEnded)
                Internal_Play();
        }
    }
}