using UnityEngine;

namespace FTSwfTools.SwfTypes {
	static class SwfRect {
		public static Rect Read(SwfStreamReader reader) {
			var bits = reader.ReadUnsignedBits(5);
			var xmin = reader.ReadSignedBits(bits);
			var xmax = reader.ReadSignedBits(bits);
			var ymin = reader.ReadSignedBits(bits);
			var ymax = reader.ReadSignedBits(bits);
			reader.AlignToByte();
			return Rect.MinMaxRect(xmin, ymin, xmax, ymax);
		}
	}
}