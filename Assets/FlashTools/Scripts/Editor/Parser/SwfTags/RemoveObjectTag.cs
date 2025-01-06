using FTEditor;

namespace FTSwfTools {
	internal class RemoveObjectTagBase : SwfTagBase {
		public Depth Depth;
	}

	internal class RemoveObjectTag : RemoveObjectTagBase {
		public ushort CharacterId;

		public static RemoveObjectTag Create(SwfStreamReader reader) {
			return new RemoveObjectTag{
				CharacterId = reader.ReadUInt16(),
				Depth       = (Depth) reader.ReadUInt16()};
		}
	}

	internal class RemoveObject2Tag : RemoveObjectTagBase {
		public static RemoveObject2Tag Create(SwfStreamReader reader) {
			return new RemoveObject2Tag{
				Depth = (Depth) reader.ReadUInt16()};
		}
	}
}