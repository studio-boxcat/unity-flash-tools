namespace FTSwfTools {
	class SetBackgroundColorTag : SwfTagBase {
		public SwfColor BackgroundColor;

		public static SetBackgroundColorTag Create(SwfStreamReader reader) {
			return new SetBackgroundColorTag{BackgroundColor = SwfColor.Read(reader, false)};
		}
	}
}