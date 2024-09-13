using System.Collections.Generic;
using System.Linq;
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
            var ppu = sprite.pixelsPerUnit;

            Poses = new Vector2[verts.Length];
            // For swf, the pivot is in the top-right corner.
            var offset = (sprite.rect.size / 2) / ppu;
            for (var i = 0; i < verts.Length; i++)
                Poses[i] = (verts[i] + offset) * ImportConfig.PixelsPerUnit;
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

        public SpriteData this[ushort id] => _data[id];
    }
}