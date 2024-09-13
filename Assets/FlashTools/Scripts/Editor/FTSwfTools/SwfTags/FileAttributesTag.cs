namespace FTSwfTools.SwfTags {
	class FileAttributesTag : SwfTagBase {
		public override SwfTagType TagType {
			get { return SwfTagType.FileAttributes; }
		}

		public override TResult AcceptVisitor<TArg, TResult>(SwfTagVisitor<TArg, TResult> visitor, TArg arg) {
			return visitor.Visit(this, arg);
		}

		public override string ToString() {
			return "FileAttributesTag.";
		}

		public static FileAttributesTag Create(SwfStreamReader reader) {
			return new FileAttributesTag();
		}
	}
}