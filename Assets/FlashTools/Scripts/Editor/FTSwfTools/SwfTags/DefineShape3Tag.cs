using FTSwfTools.SwfTypes;
using UnityEngine;

namespace FTSwfTools.SwfTags {
	class DefineShape3Tag : SwfTagBase {
		public ushort             ShapeId;
		public Rect               ShapeBounds;
		public SwfShapesWithStyle Shapes;

		public static DefineShape3Tag Create(SwfStreamReader reader) {
			return new DefineShape3Tag
			{
				ShapeId = reader.ReadUInt16(),
				ShapeBounds = SwfRect.Read(reader),
				Shapes = SwfShapesWithStyle.Read(reader, SwfShapesWithStyle.ShapeStyleType.Shape3)
			};
		}
	}
}