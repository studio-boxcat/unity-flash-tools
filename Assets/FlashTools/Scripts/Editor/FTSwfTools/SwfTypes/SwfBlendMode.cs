namespace FTSwfTools.SwfTypes {
	public struct SwfBlendMode {
		public enum Mode {
			Normal,
			Layer,
			Multiply,
			Screen,
			Lighten,
			Darken,
			Difference,
			Add,
			Subtract,
			Invert,
			Alpha,
			Erase,
			Overlay,
			Hardlight
		}
		public Mode Value;

		public static SwfBlendMode identity => new() {Value = Mode.Normal};

		public static SwfBlendMode Read(SwfStreamReader reader) {
			var mode_id = reader.ReadByte();
			return new SwfBlendMode{Value = ModeFromByte(mode_id)};
		}

		public override string ToString() => $"SwfBlendMode. Mode: {Value}";

		static Mode ModeFromByte(byte mode_id)
		{
			return mode_id switch
			{
				0 or 1 => Mode.Normal,
				2 => Mode.Layer,
				3 => Mode.Multiply,
				4 => Mode.Screen,
				5 => Mode.Lighten,
				6 => Mode.Darken,
				7 => Mode.Difference,
				8 => Mode.Add,
				9 => Mode.Subtract,
				10 => Mode.Invert,
				11 => Mode.Alpha,
				12 => Mode.Erase,
				13 => Mode.Overlay,
				14 => Mode.Hardlight,
				_ => throw new System.Exception($"Incorrect blend mode id: {mode_id}")
			};
		}
	}
}