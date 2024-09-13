﻿namespace FTSwfTools.SwfTags {
	class EndTag : SwfTagBase {
		public override SwfTagType TagType {
			get { return SwfTagType.End; }
		}

		public override TResult AcceptVisitor<TArg, TResult>(SwfTagVisitor<TArg, TResult> visitor, TArg arg) {
			return visitor.Visit(this, arg);
		}

		public override string ToString() {
			return "EndTag.";
		}

		public static EndTag Create(SwfStreamReader reader) {
			return new EndTag();
		}
	}
}