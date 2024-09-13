using UnityEngine;

using System.Collections.Generic;
using FTSwfTools.SwfTypes;
using UnityEngine.Assertions;

namespace FTEditor {
	struct SwfVec4Int
	{
		public int X;
		public int Y;
		public int Z;
		public int W;

		public SwfVec4Int(int x, int y, int z, int w)
		{
			X = x;
			Y = y;
			Z = z;
			W = w;
		}

		public static SwfVec4Int Uniform(int value) => new(value, value, value, value);

		public static SwfVec4Int operator+(SwfVec4Int a, SwfVec4Int b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);
		public static SwfVec4Int operator*(SwfVec4Int a, SwfVec4Int b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z, a.W * b.W);
	}

	struct SwfMatrixData {
		Vector2 sc;
		Vector2 sk;
		Vector2 tr;

		public static readonly SwfMatrixData identity = new() {
			sc = new Vector2(1, 1),
			sk = default,
			tr = default};

		public Matrix4x4 ToUMatrix() {
			var mat = Matrix4x4.identity;
			mat.m00 = sc.x;
			mat.m11 = sc.y;
			mat.m10 = sk.x;
			mat.m01 = sk.y;
			mat.m03 = tr.x;
			mat.m13 = tr.y;
			return mat;
		}

		public static SwfMatrixData FromUMatrix(Matrix4x4 mat) {
			return new SwfMatrixData{
				sc = new Vector2(mat.m00, mat.m11),
				sk = new Vector2(mat.m10, mat.m01),
				tr = new Vector2(mat.m03, mat.m13)};
		}
	}

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