using FTSwfTools.SwfTypes;

namespace FTSwfTools.SwfTags {
	class PlaceObjectTag : SwfTagBase {
		public ushort            CharacterId;
		public Depth             Depth;
		public SwfMatrix         Matrix;
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