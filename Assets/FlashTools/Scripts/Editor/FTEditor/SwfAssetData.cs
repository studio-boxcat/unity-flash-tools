using UnityEngine;

using System.Collections.Generic;
using FTSwfTools.SwfTypes;
using UnityEngine.Assertions;

namespace FTEditor {
	struct SwfBlendModeData {
		public enum Types : byte {
			Normal,
			Layer,
			Multiply,
			Screen,
			Lighten,
			Add,
			Subtract,
		}
		public Types type;

		public SwfBlendModeData(Types type) => this.type = type;

		public static SwfBlendModeData identity => new() {type = Types.Normal};

		public static SwfBlendModeData operator*(SwfBlendModeData a, SwfBlendModeData b)
			=> a.type is (Types.Normal or Types.Layer) ? b : a;

		public static explicit operator SwfBlendModeData(SwfBlendMode value)
		{
			return value.Value switch
			{
				SwfBlendMode.Mode.Normal => new SwfBlendModeData(Types.Normal),
				SwfBlendMode.Mode.Layer => new SwfBlendModeData(Types.Layer),
				SwfBlendMode.Mode.Multiply => new SwfBlendModeData(Types.Multiply),
				SwfBlendMode.Mode.Screen => new SwfBlendModeData(Types.Screen),
				SwfBlendMode.Mode.Lighten => new SwfBlendModeData(Types.Lighten),
				SwfBlendMode.Mode.Add => new SwfBlendModeData(Types.Add),
				SwfBlendMode.Mode.Subtract => new SwfBlendModeData(Types.Subtract),
				_ => throw new System.Exception("Unsupported blend mode: " + value.Value)
			};
		}
	}

	struct SwfColorTransData
	{
		public int Depth;
		public SwfVec4Int Mul;
		public SwfVec4Int Add;

		public static readonly SwfColorTransData identity = new()
		{
			Depth = 0,
			Mul = SwfVec4Int.Uniform(1),
			Add = default,
		};

		public Color CalculateMul() => CalculateColor(Mul, Depth);
		public Color CalculateAdd() => CalculateColor(Add, Depth);

		static Color CalculateColor(SwfVec4Int v, int depth)
		{
			Assert.IsTrue(depth is >= 0 and < 4, "Depth must be in range [0, 3]");
			var div = 1u << (8 * depth);
			if (div is 1) return new Color(v.X, v.Y, v.Z, v.W);
			float divf = div;
			return new Color(v.X / divf, v.Y / divf, v.Z / divf, v.W / divf);
		}

		public static SwfColorTransData operator*(
			SwfColorTransData a, SwfColorTransform b)
		{
			if (b.Mul.HasValue is false && b.Add.HasValue is false)
				return a;

			var mul = (SwfVec4Int) (b.Mul ?? SwfColorTransform.Color.White);
			var add = (SwfVec4Int) (b.Add ?? default);
			return new SwfColorTransData{
				Depth = a.Depth + 1,
				Mul = mul * a.Mul,
				Add = add * a.Mul + a.Add};
		}
	}

	class SwfInstanceData {
		public enum Types {
			Mask,
			Group,
			Masked,
			MaskReset
		}
		public Types                 Type        = Types.Group;
		public ushort                ClipDepth   = 0;
		public ushort                Bitmap      = 0;
		public Matrix4x4             Matrix      = Matrix4x4.identity;
		public SwfBlendModeData      BlendMode   = SwfBlendModeData.identity;
		public SwfColorTransData     ColorTrans  = SwfColorTransData.identity;

		public static SwfInstanceData MaskReset(SwfInstanceData mask)
		{
			return new SwfInstanceData
			{
				Type = Types.MaskReset,
				ClipDepth = 0,
				Bitmap     = mask.Bitmap,
				Matrix     = mask.Matrix,
				BlendMode  = mask.BlendMode,
				ColorTrans = mask.ColorTrans,
			};
		}
	}

	class SwfFrameData {
		public string                Anchor      = string.Empty;
		public List<string>          Labels      = new();
		public List<SwfInstanceData> Instances   = new();
	}

	class SwfSymbolData {
		public string                Name        = string.Empty;
		public List<SwfFrameData>    Frames      = new();
	}
}