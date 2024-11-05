using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FTRuntime
{
    // strong type
    public enum SwfFrameId : ushort
    {
        Invalid = 0xFFFF,
    }

    // strong type
    public enum SwfSequenceId : uint { }

    public partial class SwfClip : ScriptableObject
    {
        public byte FrameRate = 8; // FramePerSecond
        [SerializeField, Required, AssetsOnly]
        public Texture2D Atlas;
        [ListDrawerSettings(IsReadOnly = true)]
        public SwfFrame[] Frames;
        [ListDrawerSettings(IsReadOnly = true)]
        public SwfSequence[] Sequences;
        public ushort MeshCount;

        MeshCatalog _meshes;

        public void Init(AssetBundle bundle)
            => _meshes = new MeshCatalog(bundle, MeshCount);

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
        {
#if UNITY_EDITOR
            Editor_LoadMeshCatalog();
#endif
            MeshBuilder.Build(GetFrame(frame), _meshes, mesh);
        }
    }
}