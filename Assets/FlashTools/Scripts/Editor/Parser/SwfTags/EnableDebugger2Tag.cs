namespace FTSwfTools {
	internal class EnableDebugger2Tag : SwfTagBase {
		public string MD5PasswordHash;

		public static EnableDebugger2Tag Create(SwfStreamReader reader) {
			reader.ReadUInt16(); // reserved
			var md5 = reader.IsEOF
				? string.Empty
				: reader.ReadString();
			return new EnableDebugger2Tag{
				MD5PasswordHash = md5};
		}
	}
}