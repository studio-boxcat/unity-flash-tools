namespace FTSwfTools.SwfTags {
	class UnknownTag : SwfTagBase {
		public int TagId { get; private set; }

		public override SwfTagType TagType => SwfTagType.Unknown;

		public override TResult AcceptVisitor<TArg, TResult>(SwfTagVisitor<TArg, TResult> visitor, TArg arg) {
			return visitor.Visit(this, arg);
		}

		public override string ToString() => $"UnknownTag. TagId: {TagId}";

		public static UnknownTag Create(int tag_id) => new() { TagId = tag_id};
	}
}