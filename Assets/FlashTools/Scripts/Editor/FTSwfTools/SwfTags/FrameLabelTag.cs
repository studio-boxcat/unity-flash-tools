﻿namespace FTSwfTools.SwfTags {
	class FrameLabelTag : SwfTagBase {
		public string Name;
		public byte   AnchorFlag;

		public override SwfTagType TagType => SwfTagType.FrameLabel;

		public override TResult AcceptVisitor<TArg, TResult>(SwfTagVisitor<TArg, TResult> visitor, TArg arg) {
			return visitor.Visit(this, arg);
		}

		public override string ToString() {
			return string.Format(
				"FrameLabelTag. " +
				"Name: {0}, AnchorFlag: {1}",
				Name, AnchorFlag);
		}

		public static FrameLabelTag Create(SwfStreamReader reader) {
			var tag        = new FrameLabelTag();
			tag.Name       = reader.ReadString();
			tag.AnchorFlag = reader.IsEOF ? (byte)0 : reader.ReadByte();
			return tag;
		}
	}
}