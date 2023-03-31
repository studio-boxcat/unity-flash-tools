using System.Runtime.CompilerServices;

namespace Unity.Mathematics
{
    struct half
    {
        public ushort value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public half(float v)
        {
            value = (ushort) f32tof16(v);
        }

        static uint f32tof16(float x)
        {
            const int infinity_32 = 255 << 23;
            const uint msk = 0x7FFFF000u;

            uint ux = asuint(x);
            uint uux = ux & msk;
            uint h = asuint(min(asfloat(uux) * 1.92592994e-34f, 260042752.0f)) + 0x1000 >> 13; // Clamp to signed infinity if overflowed
            h = select(h, select(0x7c00u, 0x7e00u, (int) uux > infinity_32), (int) uux >= infinity_32); // NaN->qNaN and Inf->Inf
            return h | (ux & ~msk) >> 16;

            static float min(float x, float y)
            {
                return float.IsNaN(y) || x < y ? x : y;
            }

            static uint asuint(float x)
            {
                unsafe
                {
                    return *(uint*) &x;
                }
            }

            static uint select(uint falseValue, uint trueValue, bool test)
            {
                return test ? trueValue : falseValue;
            }

            static float asfloat(uint x)
            {
                unsafe
                {
                    return *(float*) &x;
                }
            }
        }
    }

    struct half2
    {
        public half x;
        public half y;

        public half2(float x, float y)
        {
            this.x = new half(x);
            this.y = new half(y);
        }
    }
}