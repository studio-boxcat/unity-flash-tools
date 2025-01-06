namespace FTSwfTools {
	internal class DoABCTag : SwfTagBase {
		public bool   ExecuteImmediately;
		public string Name;
		public byte[] ABCBytes;

		public static DoABCTag Create(SwfStreamReader reader) {
			const int kDoAbcLazyInitializeFlag = 1;
			var flags     = reader.ReadUInt32();
			var name      = reader.ReadString();
			var abc_bytes = reader.ReadRest();
			return new DoABCTag{
				ExecuteImmediately = (flags & kDoAbcLazyInitializeFlag) == 0,
				Name               = name,
				ABCBytes           = abc_bytes};
		}
	}
}