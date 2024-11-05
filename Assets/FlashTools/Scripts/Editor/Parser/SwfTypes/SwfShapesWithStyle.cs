using System.Collections.Generic;
using FTEditor;
using FTRuntime;

namespace FTSwfTools {
	readonly struct SwfShapesWithStyle {
		public enum ShapeStyleType {
			Shape,
			Shape2,
			Shape3,
			Shape4
		}

		public readonly struct FillStyle {
			public readonly SwfFillStyleType Type;
			public readonly BitmapId         BitmapId;
			public readonly SwfMatrix        BitmapMatrix;

			public FillStyle(SwfFillStyleType type, BitmapId bitmapId, SwfMatrix bitmapMatrix)
			{
				Type = type;
				BitmapId = bitmapId;
				BitmapMatrix = bitmapMatrix;
			}

			public override string ToString() => $"FillStyle. Type: {Type}, BitmapId: {BitmapId}, BitmapMatrix: {BitmapMatrix}";
		}

		public readonly FillStyle[] FillStyles;

		public SwfShapesWithStyle(FillStyle[] fillStyles) => FillStyles = fillStyles;

		public static SwfShapesWithStyle Read(SwfStreamReader reader, ShapeStyleType style_type) {
			List<FillStyle> fillStyles;

			switch ( style_type ) {
			case ShapeStyleType.Shape:
				fillStyles = ReadFillStyles(reader, false);
				SkipLineStyles(reader, false, false, false);
				ReadShapeRecords(reader, fillStyles, false, false, false);
				break;
			case ShapeStyleType.Shape2:
				fillStyles = ReadFillStyles(reader, true);
				SkipLineStyles(reader, true, false, false);
				ReadShapeRecords(reader, fillStyles, true, false, false);
				break;
			case ShapeStyleType.Shape3:
				fillStyles = ReadFillStyles(reader, true);
				SkipLineStyles(reader, true, true, false);
				ReadShapeRecords(reader, fillStyles, true, true, false);
				break;
			case ShapeStyleType.Shape4:
				fillStyles = ReadFillStyles(reader, true);
				SkipLineStyles(reader, true, true, true);
				ReadShapeRecords(reader, fillStyles, true, true, true);
				break;
			default:
				throw new System.Exception($"Unsupported ShapeStyleType: {style_type}");
			}

			return new SwfShapesWithStyle(fillStyles.ToArray());
		}

		public override string ToString() => $"SwfShapesWithStyle. FillStyles: {FillStyles.Length}";

		// ---------------------------------------------------------------------
		//
		// FillStyles
		//
		// ---------------------------------------------------------------------

		static List<FillStyle> ReadFillStyles(
			SwfStreamReader reader, bool allow_big_array)
		{
			const BitmapId invalid = (BitmapId) ushort.MaxValue;

			ushort count = reader.ReadByte();
			if ( allow_big_array && count == 255 )
				count = reader.ReadUInt16();

			var styles = new List<FillStyle>(count);
			for ( var i = 0; i < count; ++i )
			{
				var style = ReadFillStyle(reader);
				if (style.BitmapId is invalid)
				{
					L.W("ReadFillStyles: Unsupported bitmap id");
					continue;
				}
				styles.Add(style);
			}
			return styles;
		}

		// -----------------------------
		// FillStyle
		// -----------------------------

		static FillStyle ReadFillStyle(SwfStreamReader reader) {
			var type = SwfFillStyleUtils.Read(reader);
			if (!type.IsBitmapType())
				throw new System.Exception("Imported .swf file contains solid color fill style. You should use Tools/FlashExport.jsfl script for prepare .fla file");

			return new FillStyle(
				type,
				(BitmapId) reader.ReadUInt16(),
				reader.ReadMatrix());
		}

		// ---------------------------------------------------------------------
		//
		// LineStyles
		//
		// ---------------------------------------------------------------------

		static void SkipLineStyles(
			SwfStreamReader reader, bool allow_big_array, bool with_alpha, bool line2_type)
		{
			ushort count = reader.ReadByte();
			if ( allow_big_array && count == 255 ) {
				count = reader.ReadUInt16();
			}
			for ( var i = 0; i < count; ++i ) {
				if ( line2_type ) {
					SkipLine2Style(reader);
				} else {
					SkipLineStyle(reader, with_alpha);
				}
			}
		}

		// -----------------------------
		// LineStyles
		// -----------------------------

		static void SkipLineStyle(SwfStreamReader reader, bool with_alpha) {
			reader.ReadUInt16(); // Width
			SwfColor.Read(reader, with_alpha);
		}

		static void SkipLine2Style(SwfStreamReader reader) {
			reader.ReadUInt16();          // Width
			reader.ReadUnsignedBits(2);   // StartCapStyle
			var join_style    = reader.ReadUnsignedBits(2);
			var has_fill_flag = reader.ReadBit();
			reader.ReadBit();             // NoHScaleFlag
			reader.ReadBit();             // NoVScaleFlag
			reader.ReadBit();             // PixelHintingFlag
			reader.ReadUnsignedBits(5);   // Reserved
			reader.ReadBit();             // NoClose
			reader.ReadUnsignedBits(2);   // EndCapStyle
			if ( join_style == 2 ) {
				reader.ReadFixedPoint_8_8(); // MiterLimitFactor
			}
			if ( has_fill_flag ) {
				ReadFillStyle(reader); // FillStyle
			} else {
				SwfColor.Read(reader, true);
			}
		}

		// ---------------------------------------------------------------------
		//
		// ShapeRecords
		//
		// ---------------------------------------------------------------------

		static void ReadShapeRecords(
			SwfStreamReader reader, List<FillStyle> fill_styles,
			bool allow_big_array, bool with_alpha, bool line2_type)
		{
			var fill_style_bits = reader.ReadUnsignedBits(4);
			var line_style_bits = reader.ReadUnsignedBits(4);
			while ( !ReadShapeRecord(
				reader, fill_styles,
				ref fill_style_bits, ref line_style_bits,
				allow_big_array, with_alpha, line2_type) )
			{ }
		}

		static bool ReadShapeRecord(
			SwfStreamReader reader, List<FillStyle> fill_styles,
			ref uint fill_style_bits, ref uint line_style_bits,
			bool allow_big_array, bool with_alpha, bool line2_type)
		{
			var is_edge = reader.ReadBit();
			if ( is_edge ) {
				var straight = reader.ReadBit();
				if ( straight ) {
					SkipStraigtEdgeShapeRecord(reader);
				} else {
					SkipCurvedEdgeShapeRecord(reader);
				}
				return false;
			} else {
				var state_new_styles    = reader.ReadBit();
				var state_line_style    = reader.ReadBit();
				var state_fill_style1   = reader.ReadBit();
				var state_fill_style0   = reader.ReadBit();
				var state_move_to       = reader.ReadBit();
				var is_end_shape_record =
					!state_new_styles  && !state_line_style  &&
					!state_fill_style0 && !state_fill_style1 && !state_move_to;
				if ( is_end_shape_record ) {
					return true;
				}

				if ( state_move_to ) {
					var move_bits = reader.ReadUnsignedBits(5);
					reader.ReadSignedBits(move_bits); // move_delta_x
					reader.ReadSignedBits(move_bits); // move_delta_y
				}
				if ( state_fill_style0 ) {
					reader.ReadUnsignedBits(fill_style_bits); // fill_style_0
				}
				if ( state_fill_style1 ) {
					reader.ReadUnsignedBits(fill_style_bits); // fill_style_1
				}
				if ( state_line_style ) {
					reader.ReadUnsignedBits(line_style_bits); // line_style
				}
				if ( state_new_styles ) {
					reader.AlignToByte();
					fill_styles.AddRange(ReadFillStyles(reader, allow_big_array));
					SkipLineStyles(reader, allow_big_array, with_alpha, line2_type);
					fill_style_bits = reader.ReadUnsignedBits(4);
					line_style_bits = reader.ReadUnsignedBits(4);
				}
				return false;
			}
		}

		static void SkipStraigtEdgeShapeRecord(SwfStreamReader reader) {
			var num_bits          = reader.ReadUnsignedBits(4) + 2;
			var general_line_flag = reader.ReadBit();
			var vert_line_flag = general_line_flag ? false : reader.ReadBit();
			if ( general_line_flag || !vert_line_flag ) {
				reader.ReadSignedBits(num_bits); // delta_x
			}
			if ( general_line_flag || vert_line_flag ) {
				reader.ReadSignedBits(num_bits); // delta_y
			}
		}

		static void SkipCurvedEdgeShapeRecord(SwfStreamReader reader) {
			var num_bits = reader.ReadUnsignedBits(4) + 2;
			reader.ReadSignedBits(num_bits); // control_delta_x
			reader.ReadSignedBits(num_bits); // control_delta_y
			reader.ReadSignedBits(num_bits); // anchor_delta_x
			reader.ReadSignedBits(num_bits); // anchor_delta_y
		}
	}
}