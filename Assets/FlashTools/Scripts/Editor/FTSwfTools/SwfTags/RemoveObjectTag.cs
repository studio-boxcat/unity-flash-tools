namespace FTSwfTools.SwfTags {
	class RemoveObjectTag : SwfTagBase {
		public ushort CharacterId;
		public ushort Depth;

		public override SwfTagType TagType => SwfTagType.RemoveObject;

		public override TResult AcceptVisitor<TArg, TResult>(SwfTagVisitor<TArg, TResult> visitor, TArg arg) {
			return visitor.Visit(this, arg);
		}

		public override string ToString() {
			return $"RemoveObjectTag. CharacterId: {CharacterId}, Depth: {Depth}";
		}

		public static RemoveObjectTag Create(SwfStreamReader reader) {
			return new RemoveObjectTag{
				CharacterId = reader.ReadUInt16(),
				Depth       = reader.ReadUInt16()};
		}
	}
}