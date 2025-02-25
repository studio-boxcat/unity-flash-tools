namespace FT {
	internal class UnsupportedTag : SwfTagBase {
		private readonly SwfTagType _tagType;

		public UnsupportedTag(SwfTagType tagType) => _tagType = tagType;
		public override string ToString() => $"UnsupportedTag. TagType: {_tagType}";
	}
}