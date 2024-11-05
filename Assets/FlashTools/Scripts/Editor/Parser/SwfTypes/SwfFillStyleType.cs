namespace FTSwfTools {
	enum SwfFillStyleType : byte {
		SolidColor,
		LinearGradient,
		RadialGradient,
		FocalGradient,
		RepeatingBitmap,
		ClippedBitmap,
		NonSmoothedRepeatingBitmap,
		NonSmoothedClippedBitmap
	}

	static class SwfFillStyleUtils
	{
		public static SwfFillStyleType Read(SwfStreamReader reader)
		{
			var value = reader.ReadByte();
			return value switch
			{
				0x00 => SwfFillStyleType.SolidColor,
				0x10 => SwfFillStyleType.LinearGradient,
				0x12 => SwfFillStyleType.RadialGradient,
				0x13 => SwfFillStyleType.FocalGradient,
				0x40 => SwfFillStyleType.RepeatingBitmap,
				0x41 => SwfFillStyleType.ClippedBitmap,
				0x42 => SwfFillStyleType.NonSmoothedRepeatingBitmap,
				0x43 => SwfFillStyleType.NonSmoothedClippedBitmap,
				_ => throw new System.Exception($"Incorrect fill stype type id: {value}")
			};
		}

		public static bool IsSolidType(this SwfFillStyleType value)
			=> value is SwfFillStyleType.SolidColor;
		public static bool IsGradientType(this SwfFillStyleType value)
			=> value is SwfFillStyleType.LinearGradient or SwfFillStyleType.RadialGradient or SwfFillStyleType.FocalGradient;
		public static bool IsBitmapType(this SwfFillStyleType value)
			=> value is SwfFillStyleType.RepeatingBitmap or SwfFillStyleType.ClippedBitmap or SwfFillStyleType.NonSmoothedRepeatingBitmap or SwfFillStyleType.NonSmoothedClippedBitmap;
	}
}