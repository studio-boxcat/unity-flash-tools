using System.Collections.Generic;
using FTSwfTools.SwfTags;

namespace FTSwfTools.SwfTypes {
	static class SwfControlTags {
		public static SwfTagBase[] Read(SwfStreamReader reader) {
			var tags = new List<SwfTagBase>();
			while ( true ) {
				var tag = SwfTagBase.Read(reader);
				if ( tag is EndTag ) break;
				tags.Add(tag);
			}
			return tags.ToArray();
		}
	}
}