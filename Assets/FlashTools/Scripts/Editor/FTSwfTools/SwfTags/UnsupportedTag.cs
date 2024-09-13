namespace FTSwfTools.SwfTags {
	class UnsupportedTag : SwfTagBase {
		SwfTagType _tagType;

		public override SwfTagType TagType => _tagType;

		public override TResult AcceptVisitor<TArg, TResult>(SwfTagVisitor<TArg, TResult> visitor, TArg arg) => visitor.Visit(this, arg);

		public override string ToString() => $"UnsupportedTag. TagType: {TagType}";

		public static UnsupportedTag Create(SwfTagType tag_type) => new() { _tagType = tag_type};
	}
}