﻿using System.Collections.Generic;

namespace FTSwfTools.SwfTags {
	class SymbolClassTag : SwfTagBase {
		public struct SymbolTagData {
			public ushort Tag;
			public string Name;
		}

		public List<SymbolTagData> SymbolTags;

		public static SymbolClassTag Create(SwfStreamReader reader) {
			var symbol_tag_count = reader.ReadUInt16();
			var symbol_tags      = new List<SymbolTagData>(symbol_tag_count);
			for ( var i = 0; i < symbol_tag_count; ++i ) {
				symbol_tags.Add(new SymbolTagData{
					Tag  = reader.ReadUInt16(),
					Name = reader.ReadString()});
			}
			return new SymbolClassTag{
				SymbolTags = symbol_tags};
		}
	}
}