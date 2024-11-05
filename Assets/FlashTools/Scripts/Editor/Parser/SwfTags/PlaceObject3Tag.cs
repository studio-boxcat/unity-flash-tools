using FTRuntime;

namespace FTSwfTools {
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
		public Depth             Depth;
		public DefineId          CharacterId;
		public SwfMatrix         Matrix;
		public SwfColorTransform ColorTransform;
		public ushort            Ratio;
		public Depth             ClipDepth;
		public SwfBlendMode      BlendMode;
		public bool              BitmapCache;
		public bool              Visible;
		public SwfColor          BackgroundColor;

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
			tag.Depth             = (Depth) reader.ReadUInt16();

			_                     = tag.HasClassName
				? reader.ReadString()
				: string.Empty;

			tag.CharacterId       = tag.HasCharacter
				? (DefineId) reader.ReadUInt16()
				: 0;

			tag.Matrix            = tag.HasMatrix
				? reader.ReadMatrix()
				: SwfMatrix.Identity;

			tag.ColorTransform    = tag.HasColorTransform
				? SwfColorTransform.Read(reader, true)
				: default;

			tag.Ratio             = tag.HasRatio
				? reader.ReadUInt16()
				: (ushort)0;

			_                     = tag.HasName
				? reader.ReadString()
				: string.Empty;

			tag.ClipDepth         = tag.HasClipDepth
				? (Depth) reader.ReadUInt16()
				: 0;

			_                     = hasFilterList
				? SwfSurfaceFilters.Read(reader)
				: SwfSurfaceFilters.identity;

			tag.BlendMode         = tag.HasBlendMode
				? reader.ReadBlendMode()
				: SwfBlendMode.Normal;

			tag.BitmapCache       = tag.HasCacheAsBitmap && (0 != reader.ReadByte());

			tag.Visible           = !tag.HasVisible || reader.IsEOF || (0 != reader.ReadByte());

			tag.BackgroundColor   = tag.HasVisible && !reader.IsEOF
				? SwfColor.Read(reader, true)
				: SwfColor.identity;

			if (tag.HasClipActions)
				throw new System.Exception("Clip actions is unsupported");

			return tag;
		}
	}
}