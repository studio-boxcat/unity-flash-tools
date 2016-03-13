﻿using FlashTools.Internal.SwfTools.SwfTypes;

namespace FlashTools.Internal.SwfTools.SwfTags {
	class DefineSpriteTag : SwfTagBase {
		public ushort         SpriteId;
		public ushort         FrameCount;
		public SwfControlTags ControlTags;

		public override SwfTagType TagType {
			get { return SwfTagType.DefineSprite; }
		}

		public override string ToString() {
			return string.Format(
				"DefineSpriteTag. " +
				"SpriteId: {0}, FrameCount: {1}, ControlTags: {2}",
				SpriteId, FrameCount, ControlTags.Tags);
		}

		public static DefineSpriteTag Create(SwfStreamReader reader) {
			var tag         = new DefineSpriteTag();
			tag.SpriteId    = reader.ReadUInt16();
			tag.FrameCount  = reader.ReadUInt16();
			tag.ControlTags = SwfControlTags.Read(reader);
			return tag;
		}
	}
}