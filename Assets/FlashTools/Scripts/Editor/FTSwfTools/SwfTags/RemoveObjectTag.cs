namespace FTSwfTools.SwfTags {
	class RemoveObjectTagBase : SwfTagBase {
		public ushort Depth;
	}

	class RemoveObjectTag : RemoveObjectTagBase {
		public ushort CharacterId;

		public static RemoveObjectTag Create(SwfStreamReader reader) {
			return new RemoveObjectTag{
				CharacterId = reader.ReadUInt16(),
				Depth       = reader.ReadUInt16()};
		}
	}

	class RemoveObject2Tag : RemoveObjectTagBase {
		public static RemoveObject2Tag Create(SwfStreamReader reader) {
			return new RemoveObject2Tag{
				Depth = reader.ReadUInt16()};
		}
	}
}