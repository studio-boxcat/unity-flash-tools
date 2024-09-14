namespace FTSwfTools.SwfTags {
	class RemoveObject2Tag : SwfTagBase {
		public ushort Depth;

		public static RemoveObject2Tag Create(SwfStreamReader reader) {
			return new RemoveObject2Tag{
				Depth = reader.ReadUInt16()};
		}
	}
}