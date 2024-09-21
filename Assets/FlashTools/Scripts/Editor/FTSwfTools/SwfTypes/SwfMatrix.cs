using UnityEngine;

namespace FTSwfTools.SwfTypes {
	readonly struct SwfMatrix
	{
		public readonly float TranslateX; // m03
		public readonly float TranslateY; // m13
		public readonly float RotateSkew0; // m10
		public readonly float RotateSkew1; // m01
		public readonly float ScaleX; // m00
		public readonly float ScaleY; // m11

		public SwfMatrix(float translateX, float translateY, float rotateSkew0, float rotateSkew1, float scaleX, float scaleY)
		{
			TranslateX = translateX;
			TranslateY = translateY;
			RotateSkew0 = rotateSkew0;
			RotateSkew1 = rotateSkew1;
			ScaleX = scaleX;
			ScaleY = scaleY;
		}

		public static readonly SwfMatrix identity = new(0, 0, 0, 0, 1, 1);
		public static SwfMatrix Translate(float x, float y) => new(x, y, 0, 0, 1, 1);
		public static SwfMatrix Scale(float x, float y) => new(0, 0, 0, 0, x, y);

		public static SwfMatrix Read(SwfStreamReader reader) {
			// scale
			var scaleX = 1f;
			var scaleY = 1f;
			if ( reader.ReadBit() ) {
				var bits      = (byte)reader.ReadUnsignedBits(5);
				scaleX = reader.ReadFixedPoint16(bits);
				scaleY = reader.ReadFixedPoint16(bits);
			}

			// rotate
			var rotateSkew0 = 0f;
			var rotateSkew1 = 0f;
			if ( reader.ReadBit() ) {
				var bits           = (byte)reader.ReadUnsignedBits(5);
				rotateSkew0 = reader.ReadFixedPoint16(bits);
				rotateSkew1 = reader.ReadFixedPoint16(bits);
			}

			// translate
			var translate_bits = (byte)reader.ReadUnsignedBits(5);
			var translateX  = reader.ReadSignedBits(translate_bits);
			var translateY  = reader.ReadSignedBits(translate_bits);

			reader.AlignToByte();

			// convert to matrix
			return new SwfMatrix(
				translateX,
				translateY,
				rotateSkew0,
				rotateSkew1,
				scaleX,
				scaleY);
		}

		public Vector2 MultiplyPoint(float x, float y)
		{
			return new Vector2(
				ScaleX * x + RotateSkew1 * y + TranslateX,
				RotateSkew0 * x + ScaleY * y + TranslateY);
		}

		public Vector2 MultiplyPoint(Vector2 point) => MultiplyPoint(point.x, point.y);

		public static SwfMatrix operator *(SwfMatrix a, SwfMatrix b) {
			return new SwfMatrix
			(
				a.ScaleX * b.TranslateX + a.RotateSkew1 * b.TranslateY + a.TranslateX,
				a.RotateSkew0 * b.TranslateX + a.ScaleY * b.TranslateY + a.TranslateY,
				a.RotateSkew0 * b.ScaleX + a.ScaleY * b.RotateSkew0,
				a.ScaleX * b.RotateSkew1 + a.RotateSkew1 * b.ScaleY,
				a.ScaleX * b.ScaleX + a.RotateSkew1 * b.RotateSkew0,
				a.RotateSkew0 * b.RotateSkew1 + a.ScaleY * b.ScaleY
			);
		}
	}
}