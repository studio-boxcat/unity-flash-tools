namespace FT {
	internal class ScriptLimitsTag : SwfTagBase {
		public ushort MaxRecursionDepth;
		public ushort ScriptTimeoutSeconds;

		public static ScriptLimitsTag Create(SwfStreamReader reader) {
			return new ScriptLimitsTag{
				MaxRecursionDepth    = reader.ReadUInt16(),
				ScriptTimeoutSeconds = reader.ReadUInt16()};
		}
	}
}