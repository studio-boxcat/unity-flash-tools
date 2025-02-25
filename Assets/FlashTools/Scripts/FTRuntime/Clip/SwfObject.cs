using System;
using Sirenix.OdinInspector;

namespace FT
{
    [Serializable]
    public struct SwfObject
    {
        [DisplayAsString]
        public ushort MeshIndex;
        public SwfMatrix Matrix;
        [DisplayAsString]
        public byte Alpha;

        public SwfObject(ushort meshIndex, SwfMatrix matrix, byte alpha)
        {
            MeshIndex = meshIndex;
            Matrix = matrix;
            Alpha = alpha;
        }

        public bool Equals(SwfObject obj)
        {
            return MeshIndex == obj.MeshIndex && Matrix.Equals(obj.Matrix) && Alpha == obj.Alpha;
        }
    }
}