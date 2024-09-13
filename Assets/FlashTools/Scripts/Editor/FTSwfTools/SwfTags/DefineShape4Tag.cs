using FTSwfTools.SwfTypes;
using UnityEngine;

namespace FTSwfTools.SwfTags {
	class DefineShape4Tag : SwfTagBase {
		public ushort             ShapeId;
		public Rect               ShapeBounds;
		public Rect               EdgeBounds;
		public byte               Flags;
		public SwfShapesWithStyle Shapes;

		public override SwfTagType TagType => SwfTagType.DefineShape4;

		public override TResult AcceptVisitor<TArg, TResult>(SwfTagVisitor<TArg, TResult> visitor, TArg arg) => visitor.Visit(this, arg);

		public override string ToString() =>
			$"DefineShape4Tag. ShapeId: {ShapeId}, ShapeBounds: {ShapeBounds}, EdgeBounds: {EdgeBounds}, Flags: {Flags}, Shapes: {Shapes}";

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