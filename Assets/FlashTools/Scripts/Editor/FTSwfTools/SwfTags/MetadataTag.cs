namespace FTSwfTools.SwfTags {
	class MetadataTag : SwfTagBase {
		public string Metadata;

		public static MetadataTag Create(SwfStreamReader reader) => new() { Metadata = reader.ReadString()};
	}
}