using System;
using System.Collections.Generic;
using UnityEngine;

namespace FTEditor.Postprocessors
{
    public class MeshMemory
    {
        readonly Dictionary<int, Mesh> _dict = new();

        public Mesh GetOrAdd(int hash, Func<Mesh> constructor)
        {
            if (_dict.TryGetValue(hash, out var mesh))
                return mesh;

            mesh = constructor();
            _dict.Add(hash, mesh);
            return mesh;
        }

        public Dictionary<int, Mesh>.ValueCollection.Enumerator GetEnumerator() => _dict.Values.GetEnumerator();
    }
}