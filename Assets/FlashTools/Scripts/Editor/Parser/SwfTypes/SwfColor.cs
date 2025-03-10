﻿namespace FT {
	public struct SwfColor {
		public byte R;
		public byte G;
		public byte B;
		public byte A;

		public SwfColor(byte r, byte g, byte b, byte a)
		{
			R = r;
			G = g;
			B = b;
			A = a;
		}

		public static readonly SwfColor identity = new(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);

		public static SwfColor Read(SwfStreamReader reader, bool with_alpha) {
			var r = reader.ReadByte();
			var g = reader.ReadByte();
			var b = reader.ReadByte();
			var a = with_alpha ? reader.ReadByte() : byte.MaxValue;
			return new SwfColor{ R = r, G = g, B = b, A = a};
		}

		public override string ToString() {
			return string.Format(
				"SwfColor. " +
				"R: {0}, G: {1}, B: {2}, A: {3}",
				R, G, B, A);
		}
	}
}