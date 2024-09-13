namespace FTSwfTools.SwfTags {
	class ShowFrameTag : SwfTagBase {
		public override SwfTagType TagType => SwfTagType.ShowFrame;

		public override TResult AcceptVisitor<TArg, TResult>(SwfTagVisitor<TArg, TResult> visitor, TArg arg) {
			return visitor.Visit(this, arg);
		}

		public override string ToString() => "ShowFrameTag.";

		public static ShowFrameTag Create(SwfStreamReader reader) => new();
	}
}