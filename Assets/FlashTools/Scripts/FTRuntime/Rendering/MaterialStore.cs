using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FT
{
    // strong type
    public enum MaterialGroupIndex : byte
    {
        Invalid = 0xFF,
    }

    public class MaterialStore : ScriptableObject // Resources
    {
        [NonSerialized]
        private static MaterialStore _instance;
        public static MaterialStore Instance => _instance ??= Addressables.LoadAsset<MaterialStore>("SwfMaterialStore");

        public static Material[] Get(MaterialGroupIndex index) => Instance[index];

        [Serializable, InlineProperty]
        private struct MaterialGroup
        {
            [ListDrawerSettings(IsReadOnly = true)]
            public Material[] Materials;
        }

        [SerializeField, ListDrawerSettings(IsReadOnly = true, ShowFoldout = true)]
        private MaterialGroup[] _groups;

        private Material[] this[MaterialGroupIndex index] => _groups[(int) index].Materials;

#if UNITY_EDITOR
        public MaterialGroupIndex Put(Material[] materials)
        {
            var gi = 0;
            for (; gi < _groups.Length; gi++) // group index
            {
                var cmp = _groups[gi].Materials;
                if (cmp.Length != materials.Length)
                    continue;

                for (var mi = 0; mi < materials.Length; mi++) // material index
                    if (ReferenceEquals(cmp[mi], materials[mi]) is false)
                        break;

                return (MaterialGroupIndex) gi;
            }

            Array.Resize(ref _groups, _groups.Length + 1);
            _groups[gi] = new MaterialGroup { Materials = materials };
            UnityEditor.EditorUtility.SetDirty(this);
            return (MaterialGroupIndex) gi;
        }
#endif
    }
}