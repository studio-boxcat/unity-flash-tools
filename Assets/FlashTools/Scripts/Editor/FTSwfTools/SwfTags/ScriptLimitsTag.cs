namespace FTSwfTools.SwfTags {
	class ScriptLimitsTag : SwfTagBase {
		public ushort MaxRecursionDepth;
		public ushort ScriptTimeoutSeconds;

		public override SwfTagType TagType => SwfTagType.ScriptLimits;

		public override TResult AcceptVisitor<TArg, TResult>(SwfTagVisitor<TArg, TResult> visitor, TArg arg) {
			return visitor.Visit(this, arg);
		}

		public override string ToString() =>
			$"ScriptLimitsTag. MaxRecursionDepth: {MaxRecursionDepth}, ScriptTimeoutSeconds: {ScriptTimeoutSeconds}";

		public static ScriptLimitsTag Create(SwfStreamReader reader) {
			return new ScriptLimitsTag{
				MaxRecursionDepth    = reader.ReadUInt16(),
				ScriptTimeoutSeconds = reader.ReadUInt16()};
		}
	}
}