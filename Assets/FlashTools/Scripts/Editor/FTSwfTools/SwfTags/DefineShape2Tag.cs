using FTSwfTools.SwfTypes;
using UnityEngine;

namespace FTSwfTools.SwfTags {
	class DefineShape2Tag : SwfTagBase {
		public ushort             ShapeId;
		public Rect               ShapeBounds;
		public SwfShapesWithStyle Shapes;

		public static DefineShape2Tag Create(SwfStreamReader reader) {
			return new DefineShape2Tag
			{
				ShapeId = reader.ReadUInt16(),
				ShapeBounds = SwfRect.Read(reader),
				Shapes = SwfShapesWithStyle.Read(reader, SwfShapesWithStyle.ShapeStyleType.Shape2)
			};
		}
	}
}