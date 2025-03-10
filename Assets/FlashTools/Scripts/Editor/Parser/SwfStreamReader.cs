﻿using System.IO;
using System.Text;
using System.Collections.Generic;
using System.IO.Compression;

namespace FT {
	public class SwfStreamReader {
		private struct BitContext {
			public byte CachedByte;
			public byte BitIndex;
		}

		private BitContext   _bitContext;
		private BinaryReader _binaryReader;

		// ---------------------------------------------------------------------
		//
		// Public
		//
		// ---------------------------------------------------------------------

		public SwfStreamReader(byte[] data) {
			var memory_stream = new MemoryStream(data);
			_binaryReader = new BinaryReader(memory_stream);
		}

		public SwfStreamReader(Stream stream) {
			_binaryReader = new BinaryReader(stream);
		}

		public bool IsEOF => Position >= Length;

		public uint Length {
			get {
				var longLength = _binaryReader.BaseStream.Length;
				return longLength < 0 ? 0 : (uint)longLength;
			}
		}

		public uint Position {
			get {
				var longPosition = _binaryReader.BaseStream.Position;
				return longPosition < 0 ? 0 : (uint)longPosition;
			}
		}

		public uint BytesLeft {
			get { return Length - Position; }
		}

		public void AlignToByte() {
			_bitContext.BitIndex   = 0;
			_bitContext.CachedByte = 0;
		}

		public byte[] ReadRest() {
			return ReadBytes(BytesLeft);
		}

		public bool ReadBit() {
			var bit_index = _bitContext.BitIndex & 0x07;
			if ( bit_index == 0 ) {
				_bitContext.CachedByte = ReadByte();
			}
			++_bitContext.BitIndex;
			return ((_bitContext.CachedByte << bit_index) & 0x80) != 0;
		}

		public byte ReadByte() {
			return _binaryReader.ReadByte();
		}

		public byte[] ReadBytes(uint count) {
			if ( count > int.MaxValue ) {
				throw new IOException();
			}
			return _binaryReader.ReadBytes((int)count);
		}

		public char[] ReadChars(uint count) {
			if ( count > int.MaxValue ) {
				throw new IOException();
			}
			return _binaryReader.ReadChars((int)count);
		}

		public short ReadInt16() {
			return _binaryReader.ReadInt16();
		}

		public int ReadInt32() {
			return _binaryReader.ReadInt32();
		}

		public ushort ReadUInt16() {
			return _binaryReader.ReadUInt16();
		}

		public uint ReadUInt32() {
			return _binaryReader.ReadUInt32();
		}

		public float ReadFloat32() {
			return _binaryReader.ReadSingle();
		}

		public int ReadSignedBits(uint count) {
			if ( count == 0 ) {
				return 0;
			}
			bool sign = ReadBit();
			var res = sign ? uint.MaxValue : 0;
			--count;
			for ( var i = 0; i < count; ++i ) {
				var bit = ReadBit();
				res = (res << 1 | (bit ? 1u : 0u));
			}
			return (int)res;
		}

		public uint ReadUnsignedBits(uint count) {
			if ( count == 0 ) {
				return 0;
			}
			uint res = 0;
			for ( var i = 0; i < count; ++i ) {
				var bit = ReadBit();
				res = (res << 1 | (bit ? 1u : 0u));
			}
			return res;
		}

		public string ReadString() {
			var bytes = new List<byte>();
			while ( true ) {
				var bt = ReadByte();
				if ( bt == 0 ) {
					break;
				}
				bytes.Add(bt);
			}
			return Encoding.UTF8.GetString(bytes.ToArray());
		}

		public float ReadFixedPoint_8_8() {
			var value = ReadInt16();
			return value / 256.0f;
		}

		public float ReadFixedPoint_16_16() {
			var value = ReadInt32();
			return value / 65536.0f;
		}

		public float ReadFixedPoint16(uint bits) {
			var value = ReadSignedBits(bits);
			return value / 65536.0f;
		}

		public uint ReadEncodedU32() {
			uint val = 0;
			var bt = ReadByte();
			val |= bt & 0x7Fu;
			if ( (bt & 0x80) == 0 ) {
				return val;
			}
			bt = ReadByte();
			val |= (bt & 0x7Fu) << 7;
			if ( (bt & 0x80) == 0 ) {
				return val;
			}
			bt = ReadByte();
			val |= (bt & 0x7Fu) << 14;
			if ( (bt & 0x80) == 0 ) {
				return val;
			}
			bt = ReadByte();
			val |= (bt & 0x7Fu) << 21;
			if ( (bt & 0x80) == 0 ) {
				return val;
			}
			bt = ReadByte();
			val |= (bt & 0x7Fu) << 28;
			return val;
		}

		static public Stream DecompressZBytes(byte[] compressed_bytes) {
			var compressed_stream = new MemoryStream(compressed_bytes);
			compressed_stream.Position = 2; // Skip magic bytes.
			return new DeflateStream(compressed_stream, CompressionMode.Decompress);
		}

		static public SwfStreamReader DecompressZBytesToReader(byte[] compressd_bytes) {
			return new SwfStreamReader(DecompressZBytes(compressd_bytes));
		}
	}
}