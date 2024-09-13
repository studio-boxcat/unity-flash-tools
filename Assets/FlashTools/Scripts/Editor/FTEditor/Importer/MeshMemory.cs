using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FTEditor.Importer
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

        public Mesh[] GetMeshes() => _dict.Values.ToArray();
    }
}