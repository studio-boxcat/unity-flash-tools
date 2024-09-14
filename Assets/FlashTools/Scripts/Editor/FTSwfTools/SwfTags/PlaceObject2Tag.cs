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
		public ushort            Depth;
		public ushort            CharacterId;
		public Matrix4x4         Matrix;
		public SwfColorTransform ColorTransform;
		public ushort            Ratio;
		public string            Name;
		public ushort            ClipDepth;
		public SwfClipActions    ClipActions;

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
			tag.Depth             = reader.ReadUInt16();

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

			tag.ClipActions       = tag.HasClipActions
				? SwfClipActions.Read(reader)
				: SwfClipActions.identity;

			return tag;
		}
	}
}