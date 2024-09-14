namespace FTSwfTools.SwfTags {
	class EnableDebuggerTag : SwfTagBase {
		public string MD5PasswordHash;

		public static EnableDebuggerTag Create(SwfStreamReader reader) {
			var md5 = reader.IsEOF
				? string.Empty
				: reader.ReadString();
			return new EnableDebuggerTag{
				MD5PasswordHash = md5};
		}
	}
}