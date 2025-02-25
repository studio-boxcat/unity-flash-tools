using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

namespace FT.Importer {
	internal readonly struct SwfColorTransData
	{
		public readonly int Depth;
		public readonly SwfVec4Int Mul;
		public readonly SwfVec4Int Add;

		private SwfColorTransData(int depth, SwfVec4Int mul, SwfVec4Int add)
		{
			Depth = depth;
			Mul = mul;
			Add = add;
		}

		public static readonly SwfColorTransData identity = new(0, SwfVec4Int.Uniform(1), default);

		public Color CalculateMul() => CalculateColor(Mul, Depth);
		public Color CalculateAdd() => CalculateColor(Add, Depth);

		private static Color CalculateColor(SwfVec4Int v, int depth)
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

	internal readonly struct SwfInstanceData {
		public enum Types : byte {
			Simple,
			Masked,
			MaskIn,
			MaskOut
		}

		public readonly Types                 Type;
		public readonly Depth                 ClipDepth; // Stencil
		public readonly BitmapId              Bitmap;
		public readonly SwfMatrix             Matrix; // Bitmap space -> Swf space.
		public readonly SwfBlendMode          BlendMode;
		public readonly SwfColorTransData     ColorTrans;
		public float                 TintAlpha => ColorTrans.CalculateMul().a;

		public SwfInstanceData(Types type, Depth clipDepth, BitmapId bitmap, SwfMatrix matrix, SwfBlendMode blendMode, SwfColorTransData colorTrans)
		{
			Type = type;
			ClipDepth = clipDepth;
			Bitmap = bitmap;
			Matrix = matrix;
			BlendMode = blendMode;
			ColorTrans = colorTrans;
		}

		public MaterialKey GetMaterialKey()
			=> new(Type, BlendMode, ClipDepth);

		[MustUseReturnValue]
		public SwfInstanceData WithMatrix( SwfMatrix matrix) =>
			new(Type, ClipDepth, Bitmap, matrix, BlendMode, ColorTrans);

		[MustUseReturnValue]
		public SwfInstanceData WithBitmap(BitmapId bitmap) =>
			new(Type, ClipDepth, bitmap, Matrix, BlendMode, ColorTrans);

		public static SwfInstanceData MaskOut(SwfInstanceData mask) =>
			new(Types.MaskOut, 0, mask.Bitmap, mask.Matrix, mask.BlendMode, mask.ColorTrans);

		public bool Equals(SwfInstanceData other) =>
			Type == other.Type
			&& ClipDepth == other.ClipDepth
			&& Bitmap == other.Bitmap
			&& Matrix.Equals(other.Matrix)
			&& BlendMode == other.BlendMode
			&& ColorTrans.Equals(other.ColorTrans);
	}

	internal readonly struct SwfFrameData {
		public readonly string            Anchor;
		public readonly SwfInstanceData[] Instances;

		public SwfFrameData(string anchor, SwfInstanceData[] instances)
		{
			Anchor = anchor;
			Instances = instances;
		}
	}

	internal class SwfSymbolData {
		public readonly string         Name;
		public readonly SwfFrameData[] Frames;

		public SwfSymbolData(string name, SwfFrameData[] frames)
		{
			Name = name;
			Frames = frames;
		}
	}
}