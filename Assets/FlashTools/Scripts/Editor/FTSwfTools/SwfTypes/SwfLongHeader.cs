using UnityEngine;

namespace FTSwfTools.SwfTypes {
	public struct SwfLongHeader {
		public SwfShortHeader ShortHeader;
		public Rect           FrameSize;
		public float          FrameRate;
		public ushort         FrameCount;

		public static SwfLongHeader Read(SwfStreamReader reader) {
			return new SwfLongHeader{
				ShortHeader = SwfShortHeader.Read(reader),
				FrameSize   = reader.ReadRect(),
				FrameRate   = reader.ReadFixedPoint_8_8(),
				FrameCount  = reader.ReadUInt16()};
		}

		public override string ToString() =>
			$"SwfLongHeader. Format: {ShortHeader.Format}, Version: {ShortHeader.Version}, FileLength: {ShortHeader.FileLength}, FrameSize: {FrameSize}, FrameRate: {FrameRate}, FrameCount: {FrameCount}";
	}
}