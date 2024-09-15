using FTSwfTools.SwfTypes;
using UnityEngine;

namespace FTSwfTools.SwfTags {
	class DefineShapeTagBase : SwfTagBase {
		public ushort             ShapeId;
		public Rect               ShapeBounds;
		public SwfShapesWithStyle Shapes;
	}

	class DefineShapeTag : DefineShapeTagBase {
		public static DefineShapeTag Create(SwfStreamReader reader) {
			return new DefineShapeTag
			{
				ShapeId = reader.ReadUInt16(),
				ShapeBounds = SwfRect.Read(reader),
				Shapes = SwfShapesWithStyle.Read(reader, SwfShapesWithStyle.ShapeStyleType.Shape)
			};
		}
	}

	class DefineShape2Tag : DefineShapeTagBase {
		public static DefineShape2Tag Create(SwfStreamReader reader) {
			return new DefineShape2Tag
			{
				ShapeId = reader.ReadUInt16(),
				ShapeBounds = SwfRect.Read(reader),
				Shapes = SwfShapesWithStyle.Read(reader, SwfShapesWithStyle.ShapeStyleType.Shape2)
			};
		}
	}

	class DefineShape3Tag : DefineShapeTagBase {
		public static DefineShape3Tag Create(SwfStreamReader reader) {
			return new DefineShape3Tag
			{
				ShapeId = reader.ReadUInt16(),
				ShapeBounds = SwfRect.Read(reader),
				Shapes = SwfShapesWithStyle.Read(reader, SwfShapesWithStyle.ShapeStyleType.Shape3)
			};
		}
	}

	class DefineShape4Tag : DefineShapeTagBase {
		public Rect               EdgeBounds;
		public byte               Flags;

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