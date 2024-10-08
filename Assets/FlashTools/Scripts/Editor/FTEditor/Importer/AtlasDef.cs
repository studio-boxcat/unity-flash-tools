using System.Collections.Generic;
using System.Linq;
using FTSwfTools.SwfTypes;
using UnityEditor;
using UnityEngine;

namespace FTEditor.Importer
{
    readonly struct SpriteData
    {
        public readonly Vector2[] Poses;
        public readonly Vector2[] UVs;
        public readonly ushort[] Indices;

        public SpriteData(Sprite sprite)
        {
            var verts = sprite.vertices;
            Poses = new Vector2[verts.Length];
            for (var i = 0; i < verts.Length; i++)
                Poses[i] = verts[i] * ImportConfig.PixelsPerUnit;
            UVs = sprite.uv;
            Indices = sprite.triangles;
        }
    }

    readonly struct AtlasDef
    {
        readonly Dictionary<int, SpriteData> _data;

        AtlasDef(Dictionary<int, SpriteData> data) => _data = data;

        public static AtlasDef FromTexture(Texture2D atlas)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(atlas)).OfType<Sprite>().ToArray();
            var data = sprites.ToDictionary(
                x => int.Parse(x.name),
                x => new SpriteData(x));
            return new AtlasDef(data);
        }

        public SpriteData this[BitmapId id] => _data[(int) id];
    }
}