using System.Collections.Generic;
using System.Linq;
using FTRuntime;
using UnityEngine;
using UnityEngine.Assertions;

namespace FTEditor.Postprocessors
{
    public class MaterialMemory
    {
        readonly List<Material[]> _list = new();

        public byte ResolveMaterialGroupIndex(Material[] materials)
        {
            Assert.AreNotEqual(byte.MaxValue, _list.Count);

            for (byte index = 0; index < _list.Count; index++)
            {
                var materialGroup = _list[index];
                if (SequenceEqual(materialGroup, materials))
                    return index;
            }

            _list.Add(materials);
            return (byte) (_list.Count - 1);
        }

        public SwfClipAsset.MaterialGroup[] Bake()
        {
            return _list.Select(x => new SwfClipAsset.MaterialGroup(x)).ToArray();
        }

        static bool SequenceEqual(Material[] a, Material[] b)
        {
            if (a.Length != b.Length)
                return false;

            for (var i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                    return false;
            }

            return true;
        }
    }
}