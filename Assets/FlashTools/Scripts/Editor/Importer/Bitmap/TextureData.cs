using UnityEngine;

namespace FT.Importer
{
    internal readonly struct TextureData
    {
        public readonly int Width;
        public readonly int Height;
        public readonly Color32[] Data;

        public TextureData(int width, int height, Color32[] data)
        {
            Width = width;
            Height = height;
            Data = data;
        }

        public void Deconstruct(out int width, out int height, out Color32[] data)
        {
            width = Width;
            height = Height;
            data = Data;
        }
    }
}