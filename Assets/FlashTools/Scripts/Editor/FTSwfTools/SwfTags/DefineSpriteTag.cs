using FTSwfTools.SwfTypes;

namespace FTSwfTools.SwfTags {
	class DefineSpriteTag : SwfTagBase {
		public ushort         SpriteId;
		public ushort         FrameCount;
		public SwfTagBase[]   ControlTags;

		public override SwfTagType TagType => SwfTagType.DefineSprite;

		public override TResult AcceptVisitor<TArg, TResult>(SwfTagVisitor<TArg, TResult> visitor, TArg arg) => visitor.Visit(this, arg);

		public override string ToString() =>
            $"DefineSpriteTag. SpriteId: {SpriteId}, FrameCount: {FrameCount}, ControlTags: {ControlTags.Length}";

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