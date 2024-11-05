namespace FTSwfTools {
	class FrameLabelTag : SwfTagBase {
		public string Name;
		public byte   AnchorFlag;

		public static FrameLabelTag Create(SwfStreamReader reader) {
			return new FrameLabelTag
			{
				Name = reader.ReadString(),
				AnchorFlag = reader.IsEOF ? (byte)0 : reader.ReadByte()
			};
		}
	}
}