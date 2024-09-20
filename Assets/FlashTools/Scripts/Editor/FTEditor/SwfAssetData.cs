using FTSwfTools;
using UnityEngine;
using FTSwfTools.SwfTypes;
using UnityEngine.Assertions;

namespace FTEditor {
	readonly struct SwfBlendModeData {
		public enum Types : byte {
			Normal,
			Layer,
			Multiply,
			Screen,
			Lighten,
			Add,
			Subtract,
		}
		public readonly Types type;

		SwfBlendModeData(Types type) => this.type = type;

		public static SwfBlendModeData normal => new(Types.Normal);

		public static SwfBlendModeData operator*(SwfBlendModeData a, SwfBlendMode b)
			=> a.type is (Types.Normal or Types.Layer) ? ((SwfBlendModeData) b) : a;

		public static explicit operator SwfBlendModeData(SwfBlendMode value)
		{
			var t = value.Value switch
			{
				SwfBlendMode.Mode.Normal => Types.Normal,
				SwfBlendMode.Mode.Layer => Types.Layer,
				SwfBlendMode.Mode.Multiply => Types.Multiply,
				SwfBlendMode.Mode.Screen => Types.Screen,
				SwfBlendMode.Mode.Lighten => Types.Lighten,
				SwfBlendMode.Mode.Add => Types.Add,
				SwfBlendMode.Mode.Subtract => Types.Subtract,
				_ => throw new System.Exception("Unsupported blend mode: " + value.Value)
			};

			return new SwfBlendModeData(t);
		}
	}

	readonly struct SwfColorTransData
	{
		public readonly int Depth;
		public readonly SwfVec4Int Mul;
		public readonly SwfVec4Int Add;

		SwfColorTransData(int depth, SwfVec4Int mul, SwfVec4Int add)
		{
			Depth = depth;
			Mul = mul;
			Add = add;
		}

		public static readonly SwfColorTransData identity = new(0, SwfVec4Int.Uniform(1), default);

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
			return new SwfColorTransData(
				a.Depth + 1,
				mul * a.Mul,
				add * a.Mul + a.Add);
		}
	}

	class SwfInstanceData {
		public enum Types : byte {
			Simple,
			Masked,
			MaskIn,
			MaskOut
		}
		public Types                 Type        = Types.Simple;
		public Depth                 ClipDepth   = 0; // Stencil
		public ushort                Bitmap      = 0;
		public Matrix4x4             Matrix      = Matrix4x4.identity; // Swf space.
		public SwfBlendModeData      BlendMode   = SwfBlendModeData.normal;
		public SwfColorTransData     ColorTrans  = SwfColorTransData.identity;
		public float                 TintAlpha => ColorTrans.CalculateMul().a;

		public static SwfInstanceData MaskOut(SwfInstanceData mask)
		{
			return new SwfInstanceData
			{
				Type = Types.MaskOut,
				ClipDepth = 0,
				Bitmap     = mask.Bitmap,
				Matrix     = mask.Matrix,
				BlendMode  = mask.BlendMode,
				ColorTrans = mask.ColorTrans,
			};
		}
	}

	readonly struct SwfFrameData {
		public readonly string            Anchor;
		public readonly string[]          Labels;
		public readonly SwfInstanceData[] Instances;

		public SwfFrameData(string anchor, string[] labels, SwfInstanceData[] instances)
		{
			Anchor = anchor;
			Labels = labels;
			Instances = instances;
		}
	}

	class SwfSymbolData {
		public readonly string         Name;
		public readonly SwfFrameData[] Frames;

		public SwfSymbolData(string name, SwfFrameData[] frames)
		{
			Name = name;
			Frames = frames;
		}
	}
}