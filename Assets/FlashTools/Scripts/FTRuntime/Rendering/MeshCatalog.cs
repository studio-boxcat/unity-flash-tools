using UnityEngine;
using UnityEngine.Assertions;

namespace FTRuntime
{
    readonly struct MeshCatalog
    {
        readonly AssetBundle _bundle;
        readonly Mesh[] _cache;

        public MeshCatalog(AssetBundle bundle, int meshCount)
        {
            _bundle = bundle;
            _cache = new Mesh[meshCount];
        }

        public bool IsValid => _cache is not null;

        public Mesh this[MeshId id]
        {
            get
            {
                Assert.IsNotNull(_cache, "MeshCatalog is not initialized");
                return _cache[(ushort) id] ??= _bundle.LoadAsset<Mesh>(id.ToName());
            }
        }

#if UNITY_EDITOR
        public MeshCatalog(Mesh[] meshes)
        {
            _bundle = null;
            _cache = meshes;
        }
#endif
    }
}