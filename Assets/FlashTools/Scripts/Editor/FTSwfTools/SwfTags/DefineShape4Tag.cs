using FTSwfTools.SwfTypes;
using UnityEngine;

namespace FTSwfTools.SwfTags {
	class DefineShape4Tag : SwfTagBase {
		public ushort             ShapeId;
		public Rect               ShapeBounds;
		public Rect               EdgeBounds;
		public byte               Flags;
		public SwfShapesWithStyle Shapes;

		public static DefineShape4Tag Create(SwfStreamReader reader) {
			var tag         = new DefineShape4Tag();
			tag.ShapeId     = reader.ReadUInt16();
			tag.ShapeBounds = SwfRect.Read(reader);
			tag.EdgeBounds  = SwfRect.Read(reader);
			tag.Flags       = reader.ReadByte();
			tag.Shapes      = SwfShapesWithStyle.Read(reader, SwfShapesWithStyle.ShapeStyleType.Shape4);
			return tag;
		}
	}
}