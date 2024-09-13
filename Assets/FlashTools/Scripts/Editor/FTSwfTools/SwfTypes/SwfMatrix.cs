using UnityEngine;

namespace FTSwfTools.SwfTypes {
	static class SwfMatrix {
		public static Matrix4x4 Read(SwfStreamReader reader) {
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
			var mat = Matrix4x4.identity;
			mat.m00 = scaleX;
			mat.m10 = rotateSkew0;
			mat.m01 = rotateSkew1;
			mat.m11 = scaleY;
			mat.m03 = translateX;
			mat.m13 = translateY;
			return mat;
		}
	}
}