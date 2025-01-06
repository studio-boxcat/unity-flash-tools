using FTEditor;
using FTRuntime;

namespace FTSwfTools {
	internal class PlaceObjectTag : SwfTagBase {
		public DefineId          CharacterId;
		public Depth             Depth;
		public SwfMatrix         Matrix;
		public SwfColorTransform ColorTransform;

		public static PlaceObjectTag Create(SwfStreamReader reader) {
			return new PlaceObjectTag
			{
				CharacterId = (DefineId) reader.ReadUInt16(),
				Depth = (Depth) reader.ReadUInt16(),
				Matrix = reader.ReadMatrix(),
				ColorTransform = reader.IsEOF ? default : SwfColorTransform.Read(reader, false)
			};
		}
	}
}