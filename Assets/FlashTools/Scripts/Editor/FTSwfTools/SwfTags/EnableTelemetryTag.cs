namespace FTSwfTools.SwfTags {
	class EnableTelemetryTag : SwfTagBase {
		public byte[] SHA256PasswordHash;

		public static EnableTelemetryTag Create(SwfStreamReader reader) {
			reader.ReadUInt16(); // reserved
			var sha256 = reader.IsEOF
				? new byte[0]
				: reader.ReadBytes(32);
			return new EnableTelemetryTag{
				SHA256PasswordHash = sha256};
		}
	}
}