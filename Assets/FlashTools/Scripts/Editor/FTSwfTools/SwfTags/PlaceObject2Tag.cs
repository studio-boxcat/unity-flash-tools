using FTSwfTools.SwfTypes;
using UnityEngine;

namespace FTSwfTools.SwfTags {
	class PlaceObject2Tag : SwfTagBase {
		public bool              HasClipActions;
		public bool              HasClipDepth;
		public bool              HasName;
		public bool              HasRatio;
		public bool              HasColorTransform;
		public bool              HasMatrix;
		public bool              HasCharacter;
		public bool              Move;
		public Depth             Depth;
		public ushort            CharacterId;
		public SwfMatrix         Matrix;
		public SwfColorTransform ColorTransform;
		public Depth             ClipDepth;

		public static PlaceObject2Tag Create(SwfStreamReader reader) {
			var tag               = new PlaceObject2Tag();
			tag.HasClipActions    = reader.ReadBit();
			tag.HasClipDepth      = reader.ReadBit();
			tag.HasName           = reader.ReadBit();
			tag.HasRatio          = reader.ReadBit();
			tag.HasColorTransform = reader.ReadBit();
			tag.HasMatrix         = reader.ReadBit();
			tag.HasCharacter      = reader.ReadBit();
			tag.Move              = reader.ReadBit();
			tag.Depth             = (Depth)reader.ReadUInt16();

			tag.CharacterId       = tag.HasCharacter
				? reader.ReadUInt16()
				: (ushort)0;

			tag.Matrix            = tag.HasMatrix
				? SwfMatrix.Read(reader)
				: SwfMatrix.identity;

			tag.ColorTransform    = tag.HasColorTransform
				? SwfColorTransform.Read(reader, true)
				: default;

			_                     = tag.HasRatio
				? reader.ReadUInt16()
				: (ushort)0;

			_                     = tag.HasName
				? reader.ReadString()
				: string.Empty;

			tag.ClipDepth         = tag.HasClipDepth
				? (Depth) reader.ReadUInt16()
				: 0;

			if (tag.HasClipActions)
				throw new System.Exception("Clip actions is unsupported");

			return tag;
		}
	}
}