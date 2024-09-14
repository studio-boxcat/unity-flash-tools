using FTSwfTools.SwfTypes;

namespace FTSwfTools.SwfTags {
	class DefineSpriteTag : SwfTagBase {
		public ushort         SpriteId;
		public ushort         FrameCount;
		public SwfTagBase[]   ControlTags;

		public static DefineSpriteTag Create(SwfStreamReader reader) {
			return new DefineSpriteTag
			{
				SpriteId = reader.ReadUInt16(),
				FrameCount = reader.ReadUInt16(),
				ControlTags = SwfControlTags.Read(reader)
			};
		}
	}
}