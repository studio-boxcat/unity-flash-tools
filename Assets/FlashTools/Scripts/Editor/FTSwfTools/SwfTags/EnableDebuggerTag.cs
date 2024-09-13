namespace FTSwfTools.SwfTags {
	class EnableDebuggerTag : SwfTagBase {
		public string MD5PasswordHash;

		public override SwfTagType TagType {
			get { return SwfTagType.EnableDebugger; }
		}

		public override TResult AcceptVisitor<TArg, TResult>(SwfTagVisitor<TArg, TResult> visitor, TArg arg) {
			return visitor.Visit(this, arg);
		}

		public override string ToString() => $"EnableDebuggerTag. MD5PasswordHash: {MD5PasswordHash.Length > 0}";

		public static EnableDebuggerTag Create(SwfStreamReader reader) {
			var md5 = reader.IsEOF
				? string.Empty
				: reader.ReadString();
			return new EnableDebuggerTag{
				MD5PasswordHash = md5};
		}
	}
}