#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace FTRuntime
{
    public partial class SwfClip
    {
        void Editor_LoadMeshCatalog()
        {
            if (_meshes.IsValid)
                return;

            Assert.IsTrue(AssetDatabase.Contains(this), "Given SwfClip is not an asset");

            var atlasPath = AssetDatabase.GetAssetPath(Atlas);
            var meshDir = Path.GetDirectoryName(atlasPath)!;
            var meshes = new Mesh[MeshCount];
            for (var i = 0; i < MeshCount; i++)
            {
                var meshId = (MeshId) i;
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{meshDir}/{meshId.ToName()}.asset");
                meshes[meshId.ToPrimitive()] = mesh;
            }

            L.I($"[SwfClip] Loaded {meshes.Length} meshes from {meshDir}");
            _meshes = new MeshCatalog(meshes);
        }
    }
}
#endif