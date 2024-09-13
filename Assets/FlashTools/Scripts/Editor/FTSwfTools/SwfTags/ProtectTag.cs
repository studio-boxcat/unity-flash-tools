namespace FTSwfTools.SwfTags {
	class ProtectTag : SwfTagBase {
		public string MD5Password;

		public override SwfTagType TagType => SwfTagType.Protect;

		public override TResult AcceptVisitor<TArg, TResult>(SwfTagVisitor<TArg, TResult> visitor, TArg arg)
			=> visitor.Visit(this, arg);

		public override string ToString() => $"ProtectTag. MD5Password: {MD5Password}";

		public static ProtectTag Create(SwfStreamReader reader) {
			var md5_password = reader.IsEOF
				? string.Empty
				: reader.ReadString();
			return new ProtectTag{
				MD5Password = md5_password};
		}
	}
}