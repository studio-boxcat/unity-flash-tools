namespace FT {
	internal class ProtectTag : SwfTagBase {
		public string MD5Password;

		public static ProtectTag Create(SwfStreamReader reader) {
			var md5_password = reader.IsEOF
				? string.Empty
				: reader.ReadString();
			return new ProtectTag{
				MD5Password = md5_password};
		}
	}
}