namespace FTSwfTools.SwfTags {
	class RemoveObjectTag : SwfTagBase {
		public ushort CharacterId;
		public ushort Depth;

		public static RemoveObjectTag Create(SwfStreamReader reader) {
			return new RemoveObjectTag{
				CharacterId = reader.ReadUInt16(),
				Depth       = reader.ReadUInt16()};
		}
	}
}