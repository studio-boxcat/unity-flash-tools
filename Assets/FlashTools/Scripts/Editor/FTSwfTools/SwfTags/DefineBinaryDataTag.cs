namespace FTSwfTools.SwfTags {
	class DefineBinaryDataTag : SwfTagBase {
		public ushort Tag;
		public byte[] Data;

		public static DefineBinaryDataTag Create(SwfStreamReader reader) {
			var tag = reader.ReadUInt16();
			reader.ReadUInt32(); // reserved
			var data = reader.ReadRest();
			return new DefineBinaryDataTag{
				Tag  = tag,
				Data = data};
		}
	}
}