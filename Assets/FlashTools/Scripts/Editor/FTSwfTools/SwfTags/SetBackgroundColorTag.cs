using FTSwfTools.SwfTypes;

namespace FTSwfTools.SwfTags {
	class SetBackgroundColorTag : SwfTagBase {
		public SwfColor BackgroundColor;

		public override SwfTagType TagType => SwfTagType.SetBackgroundColor;

		public override TResult AcceptVisitor<TArg, TResult>(SwfTagVisitor<TArg, TResult> visitor, TArg arg) => visitor.Visit(this, arg);

		public override string ToString() => $"SetBackgroundColorTag. BackgroundColor: {BackgroundColor}";

		public static SetBackgroundColorTag Create(SwfStreamReader reader) {
			return new SetBackgroundColorTag{
				BackgroundColor = SwfColor.Read(reader, false)};
		}
	}
}