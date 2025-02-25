using UnityEngine;

namespace FT {
	internal class DefineShapeTagBase : SwfTagBase {
		public DefineId           ShapeId;
		public Rect               ShapeBounds;
		public SwfShapesWithStyle Shapes;
	}

	internal class DefineShapeTag : DefineShapeTagBase {
		public static DefineShapeTag Create(SwfStreamReader reader) {
			return new DefineShapeTag
			{
				ShapeId = (DefineId) reader.ReadUInt16(),
				ShapeBounds = reader.ReadRect(),
				Shapes = SwfShapesWithStyle.Read(reader, SwfShapesWithStyle.ShapeStyleType.Shape)
			};
		}
	}

	internal class DefineShape2Tag : DefineShapeTagBase {
		public static DefineShape2Tag Create(SwfStreamReader reader) {
			return new DefineShape2Tag
			{
				ShapeId = (DefineId) reader.ReadUInt16(),
				ShapeBounds = reader.ReadRect(),
				Shapes = SwfShapesWithStyle.Read(reader, SwfShapesWithStyle.ShapeStyleType.Shape2)
			};
		}
	}

	internal class DefineShape3Tag : DefineShapeTagBase {
		public static DefineShape3Tag Create(SwfStreamReader reader) {
			return new DefineShape3Tag
			{
				ShapeId = (DefineId) reader.ReadUInt16(),
				ShapeBounds = reader.ReadRect(),
				Shapes = SwfShapesWithStyle.Read(reader, SwfShapesWithStyle.ShapeStyleType.Shape3)
			};
		}
	}

	internal class DefineShape4Tag : DefineShapeTagBase {
		public Rect               EdgeBounds;
		public byte               Flags;

		public static DefineShape4Tag Create(SwfStreamReader reader) {
			var tag         = new DefineShape4Tag();
			tag.ShapeId     = (DefineId) reader.ReadUInt16();
			tag.ShapeBounds = reader.ReadRect();
			tag.EdgeBounds  = reader.ReadRect();
			tag.Flags       = reader.ReadByte();
			tag.Shapes      = SwfShapesWithStyle.Read(reader, SwfShapesWithStyle.ShapeStyleType.Shape4);
			return tag;
		}
	}
}