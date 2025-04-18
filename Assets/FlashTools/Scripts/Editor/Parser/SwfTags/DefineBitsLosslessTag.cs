﻿using UnityEngine;

namespace FT {
	internal interface IBitmapData
	{
		Vector2Int Size { get; }
		byte[] ToARGB32();
	}

	internal interface IDefineBitsLosslessTag : IBitmapData {
		public DefineId CharacterId { get; }
	}

	internal class DefineBitsLosslessTag : SwfTagBase, IDefineBitsLosslessTag {
		public DefineId CharacterId { get; private set; }
		public byte   BitmapFormat;
		public ushort BitmapWidth;
		public ushort BitmapHeight;
		public ushort BitmapColorTableSize;
		public byte[] ZlibBitmapData;

		public static DefineBitsLosslessTag Create(SwfStreamReader reader) {
			var tag          = new DefineBitsLosslessTag();
			tag.CharacterId  = (DefineId) reader.ReadUInt16();
			tag.BitmapFormat = reader.ReadByte();
			tag.BitmapWidth  = reader.ReadUInt16();
			tag.BitmapHeight = reader.ReadUInt16();
			if ( tag.BitmapFormat == 3 ) {
				tag.BitmapColorTableSize = (ushort)(reader.ReadByte() + 1);
			}
			tag.ZlibBitmapData = reader.ReadRest();
			return tag;
		}

		Vector2Int IBitmapData.Size => new(BitmapWidth, BitmapHeight);

		byte[] IBitmapData.ToARGB32() {
			var result     = new byte[BitmapWidth * BitmapHeight * 4];
			var swf_reader = SwfStreamReader.DecompressZBytesToReader(ZlibBitmapData);
			if ( BitmapFormat == 3 ) {
				var palette = new SwfColor[BitmapColorTableSize];
				for ( var i = 0; i < palette.Length; ++i ) {
					palette[i] = SwfColor.Read(swf_reader, false);
				}
				var palette_pitch = BitmapWidth % 4 == 0
					? BitmapWidth
					: BitmapWidth + (4 - BitmapWidth % 4);
				var palette_indices = swf_reader.ReadRest();
				for ( var i = 0; i < BitmapHeight; ++i ) {
					for ( var j = 0; j < BitmapWidth; ++j ) {
						var result_index  = j + i * BitmapWidth;
						var palette_index = palette_indices[j + i * palette_pitch];
						var palette_color = palette[palette_index];
						result[result_index * 4 + 0] = palette_color.A;
						result[result_index * 4 + 1] = palette_color.R;
						result[result_index * 4 + 2] = palette_color.G;
						result[result_index * 4 + 3] = palette_color.B;
					}
				}
			} else if ( BitmapFormat == 5 ) {
				for ( var i = 0; i < BitmapWidth * BitmapHeight; ++i ) {
					var pix24 = swf_reader.ReadUInt32();
					result[i * 4 + 0] = 255;
					result[i * 4 + 1] = (byte)((pix24 >>  8) & 0xFF);
					result[i * 4 + 2] = (byte)((pix24 >> 16) & 0xFF);
					result[i * 4 + 3] = (byte)((pix24 >> 24) & 0xFF);
				}
			} else {
				throw new System.Exception($"Incorrect DefineBitsLossless format: {BitmapFormat}");
			}
			return result;
		}
	}
}