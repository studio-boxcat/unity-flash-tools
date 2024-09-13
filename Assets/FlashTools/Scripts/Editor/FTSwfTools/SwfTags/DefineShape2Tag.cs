using FTSwfTools.SwfTypes;
using UnityEngine;

namespace FTSwfTools.SwfTags {
	class DefineShape2Tag : SwfTagBase {
		public ushort             ShapeId;
		public Rect               ShapeBounds;
		public SwfShapesWithStyle Shapes;

		public override SwfTagType TagType => SwfTagType.DefineShape2;

		public override TResult AcceptVisitor<TArg, TResult>(SwfTagVisitor<TArg, TResult> visitor, TArg arg) => visitor.Visit(this, arg);

		public override string ToString() =>
			$"DefineShape2Tag. ShapeId: {ShapeId}, ShapeBounds: {ShapeBounds}, Shapes: {Shapes}";

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