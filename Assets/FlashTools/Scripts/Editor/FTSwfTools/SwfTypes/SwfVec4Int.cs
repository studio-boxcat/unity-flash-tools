namespace FTSwfTools.SwfTypes
{
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

}