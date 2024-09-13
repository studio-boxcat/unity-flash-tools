namespace FTSwfTools.SwfTags {
	class RemoveObject2Tag : SwfTagBase {
		public ushort Depth;

		public override SwfTagType TagType => SwfTagType.RemoveObject2;

		public override TResult AcceptVisitor<TArg, TResult>(SwfTagVisitor<TArg, TResult> visitor, TArg arg) {
			return visitor.Visit(this, arg);
		}

		public override string ToString() {
			return $"RemoveObject2Tag. Depth: {Depth}";
		}

		public static RemoveObject2Tag Create(SwfStreamReader reader) {
			return new RemoveObject2Tag{
				Depth = reader.ReadUInt16()};
		}
	}
}