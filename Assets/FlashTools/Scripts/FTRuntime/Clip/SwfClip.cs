using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;

namespace FTRuntime
{
    // strong type
    public enum SwfFrameId : ushort
    {
        Invalid = 0xFFFF,
    }

    // strong type
    public enum SwfSequenceId : uint { }

    [Preserve]
    public class SwfClip : ScriptableObject
    {
        public byte FrameRate = 8; // FramePerSecond
        [SerializeField, Required, AssetsOnly]
        public Texture2D Atlas;
        [ListDrawerSettings(IsReadOnly = true)]
        public SwfFrame[] Frames;
        [ListDrawerSettings(IsReadOnly = true)]
        public SwfSequence[] Sequences;
        [SerializeField, Required, AssetsOnly]
        public Mesh Mesh;

        public SwfFrame GetFrame(SwfFrameId id)
            => Frames[(int) id];

        public SwfSequence GetSequence(SwfSequenceId id)
        {
            foreach (var s in Sequences)
                if (s.Id == id)
                    return s;

            throw new KeyNotFoundException(id.ToString());
        }

        public void BuildMesh(SwfFrameId frame, Mesh mesh)
            => MeshBuilder.Build(GetFrame(frame), Mesh, mesh);
    }
}