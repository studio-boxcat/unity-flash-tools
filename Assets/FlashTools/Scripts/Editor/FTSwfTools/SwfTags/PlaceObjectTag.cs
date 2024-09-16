using FTSwfTools.SwfTypes;
using UnityEngine;

namespace FTSwfTools.SwfTags {
	class PlaceObjectTag : SwfTagBase {
		public ushort            CharacterId;
		public Depth             Depth;
		public Matrix4x4         Matrix;
		public SwfColorTransform ColorTransform;

		public static PlaceObjectTag Create(SwfStreamReader reader) {
			return new PlaceObjectTag
			{
				CharacterId = reader.ReadUInt16(),
				Depth = (Depth) reader.ReadUInt16(),
				Matrix = SwfMatrix.Read(reader),
				ColorTransform = reader.IsEOF ? default : SwfColorTransform.Read(reader, false)
			};
		}
	}
}