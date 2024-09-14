namespace FTSwfTools.SwfTags {
	class UnknownTag : SwfTagBase {
		readonly int _tagId;
		public UnknownTag(int tagId) => _tagId = tagId;
		public override string ToString() => $"UnknownTag. TagId: {_tagId}";
	}
}