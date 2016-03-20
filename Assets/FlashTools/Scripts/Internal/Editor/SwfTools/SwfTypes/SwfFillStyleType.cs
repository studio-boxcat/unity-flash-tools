﻿using UnityEngine;

namespace FlashTools.Internal.SwfTools.SwfTypes {
	public struct SwfFillStyleType {
		public enum Type {
			SolidColor,
			LinearGradient,
			RadialGradient,
			FocalGradient,
			RepeatingBitmap,
			ClippedBitmap,
			NonSmoothedRepeatingBitmap,
			NonSmoothedClippedBitmap
		}
		public Type Value;

		public static SwfFillStyleType Read(SwfStreamReader reader) {
			var type_id = reader.ReadByte();
			var type    = TypeFromByte(type_id);
			return new SwfFillStyleType{Value = type};
		}

		public override string ToString() {
			return string.Format(
				"SwfFillStyleType. Type: {0}",
				Value);
		}

		public bool IsSolidType {
			get { return Value == Type.SolidColor; }
		}

		public bool IsBitmapType {
			get { return
				Value == Type.RepeatingBitmap ||
				Value == Type.ClippedBitmap ||
				Value == Type.NonSmoothedRepeatingBitmap ||
				Value == Type.NonSmoothedClippedBitmap;
			}
		}

		public bool IsGradientType {
			get { return
				Value == Type.LinearGradient ||
				Value == Type.RadialGradient ||
				Value == Type.FocalGradient;
			}
		}

		static Type TypeFromByte(byte type_id) {
			switch ( type_id ) {
			case 0x00: return Type.SolidColor;
			case 0x10: return Type.LinearGradient;
			case 0x12: return Type.RadialGradient;
			case 0x13: return Type.FocalGradient;
			case 0x40: return Type.RepeatingBitmap;
			case 0x41: return Type.ClippedBitmap;
			case 0x42: return Type.NonSmoothedRepeatingBitmap;
			case 0x43: return Type.NonSmoothedClippedBitmap;
			default:
				Debug.LogWarningFormat("Incorrect FillStyleType Id: {0}", type_id);
				return Type.SolidColor;
			}
		}
	}
}