using System.Collections.Generic;

namespace FT {
	internal class DefineSpriteTag : SwfTagBase {
		public readonly DefineId       SpriteId;
		public readonly ushort         FrameCount;
		public readonly SwfTagBase[]   ControlTags;

		private DefineSpriteTag(DefineId spriteId, ushort frameCount, SwfTagBase[] controlTags)
		{
			SpriteId = spriteId;
			FrameCount = frameCount;
			ControlTags = controlTags;
		}

		public static DefineSpriteTag Create(SwfStreamReader reader)
		{
			return new DefineSpriteTag(
				(DefineId) reader.ReadUInt16(),
				reader.ReadUInt16(),
				ReadControlTags(reader));

			static SwfTagBase[] ReadControlTags(SwfStreamReader reader) {
				var tags = new List<SwfTagBase>();
				while ( true ) {
					var tag = SwfTagBase.Read(reader);
					if ( tag is EndTag ) break;
					tags.Add(tag);
				}
				return tags.ToArray();
			}
		}
	}
}