using FTEditor;

namespace FTSwfTools.SwfTypes {
	struct SwfColorTransform
	{
		public Color? Mul;
		public Color? Add;

		public override string ToString() => $"SwfColorTransform. Mul={Mul}, Add={Add}";

		public static SwfColorTransform Read(SwfStreamReader reader, bool withAlpha) {
			var transform    = default(SwfColorTransform);
			var hasAdd = reader.ReadBit();
			var hasMul = reader.ReadBit();
			var channelBitCount = reader.ReadUnsignedBits(4);
			if ( hasMul ) {
				var mul = Color.Read(reader, channelBitCount, withAlpha);
				if (withAlpha is false) mul.A = 256; // XXX: byte.MaxValue in original code
				transform.Mul = mul;
			}
			if ( hasAdd ) {
				var add = Color.Read(reader, channelBitCount, withAlpha);
				if (withAlpha is false) add.A = 0;
				transform.Add = add;
			}
			reader.AlignToByte();
			return transform;
		}

		public struct Color
		{
			public short R;
			public short G;
			public short B;
			public short A;

			public static Color Read(SwfStreamReader reader, uint channelBitCount, bool withAlpha) {
				var r = (short)reader.ReadSignedBits(channelBitCount);
				var g = (short)reader.ReadSignedBits(channelBitCount);
				var b = (short)reader.ReadSignedBits(channelBitCount);
				var a = withAlpha ? (short)reader.ReadSignedBits(channelBitCount) : default;
				return new Color{ R = r, G = g, B = b, A = a};
			}

			public static readonly Color White = new() { R = 256, G = 256, B = 256, A = 256 };

			public override string ToString() => $"({R}, {G}, {B}, {A})";

			public static explicit operator SwfVec4Int(Color color) {
				return new SwfVec4Int{ X = color.R, Y = color.G, Z = color.B, W = color.A };
			}
		}
	}
}