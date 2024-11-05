namespace FTSwfTools {
	class UnsupportedTag : SwfTagBase {
		readonly SwfTagType _tagType;

		public UnsupportedTag(SwfTagType tagType) => _tagType = tagType;
		public override string ToString() => $"UnsupportedTag. TagType: {_tagType}";
	}
}