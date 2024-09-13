using System;
using System.Collections.Generic;
using FTSwfTools.SwfTags;

namespace FTSwfTools.SwfTypes {
	readonly struct SwfControlTags {
		public readonly SwfTagBase[] Tags;

		public SwfControlTags(SwfTagBase[] tags) => Tags = tags;

		public static readonly SwfControlTags identity = new(Array.Empty<SwfTagBase>());

		public static SwfControlTags Read(SwfStreamReader reader) {
			var tags = new List<SwfTagBase>();
			while ( true ) {
				var tag = SwfTagBase.Read(reader);
				if ( tag.TagType == SwfTagType.End ) break;
				tags.Add(tag);
			}
			return new SwfControlTags(tags.ToArray());
		}

		public override string ToString() => $"SwfControlTags. Tags: {Tags.Length}";
	}
}