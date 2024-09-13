using FTSwfTools.SwfTypes;
using UnityEngine;

namespace FTSwfTools.SwfTags {
	class DefineShape3Tag : SwfTagBase {
		public ushort             ShapeId;
		public Rect               ShapeBounds;
		public SwfShapesWithStyle Shapes;

		public override SwfTagType TagType => SwfTagType.DefineShape3;

		public override TResult AcceptVisitor<TArg, TResult>(SwfTagVisitor<TArg, TResult> visitor, TArg arg) {
			return visitor.Visit(this, arg);
		}

		public override string ToString() =>
            $"DefineShape3Tag. ShapeId: {ShapeId}, ShapeBounds: {ShapeBounds}, Shapes: {Shapes}";

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