namespace FTSwfTools.SwfTags {
	class MetadataTag : SwfTagBase {
		public string Metadata;

		public override SwfTagType TagType => SwfTagType.Metadata;

		public override TResult AcceptVisitor<TArg, TResult>(SwfTagVisitor<TArg, TResult> visitor, TArg arg)
            => visitor.Visit(this, arg);

		public override string ToString() => $"MetadataTag.Metadata: {Metadata.Length}";

		public static MetadataTag Create(SwfStreamReader reader) => new() { Metadata = reader.ReadString()};
	}
}