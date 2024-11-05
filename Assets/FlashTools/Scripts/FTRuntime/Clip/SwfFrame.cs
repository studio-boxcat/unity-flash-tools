using System;
using Sirenix.OdinInspector;
using UnityEngine.Scripting;

namespace FTRuntime
{
    [Serializable, Preserve]
    public struct SwfFrame
    {
        [ListDrawerSettings(IsReadOnly = true)]
        public SwfObject[] Objects;
        [ListDrawerSettings(IsReadOnly = true), ShowIf("@SubMeshIndices.Length > 0")]
        public ushort[] SubMeshIndices; // end index of each submesh

        public SwfFrame(SwfObject[] objects, ushort[] subMeshIndices)
        {
            Objects = objects;
            SubMeshIndices = subMeshIndices;
        }

        public bool Equals(SwfFrame other)
        {
            if (Objects.Length != other.Objects.Length)
                return false;
            for (var i = 0; i < Objects.Length; i++)
            {
                if (!Objects[i].Equals(other.Objects[i]))
                    return false;
            }
            return true;
        }
    }
}