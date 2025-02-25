using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;

namespace FT
{
    [Serializable, Preserve]
    public struct SwfSequence
    {
        [DisplayAsString] public SwfSequenceId Id;
        [HideInInspector] public SwfFrameId[] Frames;
        [DisplayAsString] public MaterialGroupIndex MaterialGroup;

        public SwfSequence(SwfSequenceId id, SwfFrameId[] frames, MaterialGroupIndex materialGroup)
        {
            Id = id;
            Frames = frames;
            MaterialGroup = materialGroup;
        }

        public bool IsValid => Frames is not null;
        public bool IsInvalid => Frames is null;
        public ushort FrameCount => (ushort) Frames.Length;

#if UNITY_EDITOR
        [ShowInInspector, DisplayAsString]
        private string _frames => Frames is null ? "null" : string.Join(", ", Frames);
#endif
    }
}