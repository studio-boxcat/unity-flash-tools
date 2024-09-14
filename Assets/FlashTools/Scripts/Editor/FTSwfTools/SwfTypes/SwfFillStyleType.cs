namespace FTSwfTools.SwfTypes {
	public struct SwfFillStyleType {
		public enum Type {
			SolidColor,
			LinearGradient,
			RadialGradient,
			FocalGradient,
			RepeatingBitmap,
			ClippedBitmap,
			NonSmoothedRepeatingBitmap,
			NonSmoothedClippedBitmap
		}

		public Type Value;

		public static SwfFillStyleType Read(SwfStreamReader reader) {
			var type_id = reader.ReadByte();
			return new SwfFillStyleType{
				Value = TypeFromByte(type_id)};
		}

		public override string ToString() => "SwfFillStyleType. " + $"Type: {Value}";

		public bool IsSolidType => Value == Type.SolidColor;
		public bool IsBitmapType => Value is Type.RepeatingBitmap or Type.ClippedBitmap or Type.NonSmoothedRepeatingBitmap or Type.NonSmoothedClippedBitmap;
		public bool IsGradientType => Value is Type.LinearGradient or Type.RadialGradient or Type.FocalGradient;

		static Type TypeFromByte(byte type_id)
		{
			return type_id switch
			{
				0x00 => Type.SolidColor,
				0x10 => Type.LinearGradient,
				0x12 => Type.RadialGradient,
				0x13 => Type.FocalGradient,
				0x40 => Type.RepeatingBitmap,
				0x41 => Type.ClippedBitmap,
				0x42 => Type.NonSmoothedRepeatingBitmap,
				0x43 => Type.NonSmoothedClippedBitmap,
				_ => throw new System.Exception($"Incorrect fill stype type id: {type_id}")
			};
		}
	}
}