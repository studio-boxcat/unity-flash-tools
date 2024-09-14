using FTSwfTools.SwfTypes;
using UnityEngine;

namespace FTSwfTools.SwfTags {
	class PlaceObject3Tag : SwfTagBase {
		public bool              HasClipActions;
		public bool              HasClipDepth;
		public bool              HasName;
		public bool              HasRatio;
		public bool              HasColorTransform;
		public bool              HasMatrix;
		public bool              HasCharacter;
		public bool              Move;
		public bool              OpaqueBackground;
		public bool              HasVisible;
		public bool              HasImage;
		public bool              HasClassName;
		public bool              HasCacheAsBitmap;
		public bool              HasBlendMode;
		public ushort            Depth;
		public string            ClassName;
		public ushort            CharacterId;
		public Matrix4x4         Matrix;
		public SwfColorTransform ColorTransform;
		public ushort            Ratio;
		public string            Name;
		public ushort            ClipDepth;
		public SwfBlendMode      BlendMode;
		public bool              BitmapCache;
		public bool              Visible;
		public SwfColor          BackgroundColor;
		public SwfClipActions    ClipActions;

		public static PlaceObject3Tag Create(SwfStreamReader reader) {
			var tag               = new PlaceObject3Tag();
			tag.HasClipActions    = reader.ReadBit();
			tag.HasClipDepth      = reader.ReadBit();
			tag.HasName           = reader.ReadBit();
			tag.HasRatio          = reader.ReadBit();
			tag.HasColorTransform = reader.ReadBit();
			tag.HasMatrix         = reader.ReadBit();
			tag.HasCharacter      = reader.ReadBit();
			tag.Move              = reader.ReadBit();
			reader.ReadBit(); // reserved
			tag.OpaqueBackground  = reader.ReadBit();
			tag.HasVisible        = reader.ReadBit();
			tag.HasImage          = reader.ReadBit();
			tag.HasClassName      = reader.ReadBit();
			tag.HasCacheAsBitmap  = reader.ReadBit();
			tag.HasBlendMode      = reader.ReadBit();
			var hasFilterList     = reader.ReadBit(); // HasFilterList
			tag.Depth             = reader.ReadUInt16();

			tag.ClassName         = tag.HasClassName
				? reader.ReadString()
				: string.Empty;

			tag.CharacterId       = tag.HasCharacter
				? reader.ReadUInt16()
				: (ushort)0;

			tag.Matrix            = tag.HasMatrix
				? SwfMatrix.Read(reader)
				: Matrix4x4.identity;

			tag.ColorTransform    = tag.HasColorTransform
				? SwfColorTransform.Read(reader, true)
				: default;

			tag.Ratio             = tag.HasRatio
				? reader.ReadUInt16()
				: (ushort)0;

			tag.Name              = tag.HasName
				? reader.ReadString()
				: string.Empty;

			tag.ClipDepth         = tag.HasClipDepth
				? reader.ReadUInt16()
				: (ushort)0;

			_                     = hasFilterList
				? SwfSurfaceFilters.Read(reader)
				: SwfSurfaceFilters.identity;

			tag.BlendMode         = tag.HasBlendMode
				? SwfBlendMode.Read(reader)
				: SwfBlendMode.identity;

			tag.BitmapCache       = tag.HasCacheAsBitmap && (0 != reader.ReadByte());

			tag.Visible           = !tag.HasVisible || reader.IsEOF || (0 != reader.ReadByte());

			tag.BackgroundColor   = tag.HasVisible && !reader.IsEOF
				? SwfColor.Read(reader, true)
				: SwfColor.identity;

			tag.ClipActions       = tag.HasClipActions && !reader.IsEOF
				? SwfClipActions.Read(reader)
				: SwfClipActions.identity;

			return tag;
		}
	}
}