using System;
using Sirenix.OdinInspector;
using UnityEngine;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace FT
{
    [Serializable]
    public struct SwfMatrix
    {
        [HorizontalGroup("T")]
        public float TX; // m03
        [HorizontalGroup("T")]
        public float TY; // m13
        [HorizontalGroup("R")]
        public float R0; // m10
        [HorizontalGroup("R")]
        public float R1; // m01
        [HorizontalGroup("S")]
        public float SX; // m00
        [HorizontalGroup("S")]
        public float SY; // m11

        public SwfMatrix(float tx, float ty, float r0, float r1, float sx, float sy)
        {
            TX = tx;
            TY = ty;
            R0 = r0;
            R1 = r1;
            SX = sx;
            SY = sy;
        }

        public bool Equals(SwfMatrix o)
        {
            return TX == o.TX
                   && TY == o.TY
                   && R0 == o.R0
                   && R1 == o.R1
                   && SX == o.SX
                   && SY == o.SY;
        }

        public static readonly SwfMatrix Identity = new(0, 0, 0, 0, 1, 1);
        public static SwfMatrix Translate(float x, float y) => new(x, y, 0, 0, 1, 1);
        public static SwfMatrix Scale(float x, float y) => new(0, 0, 0, 0, x, y);

        public Vector2 MultiplyPoint(float x, float y)
        {
            return new Vector2(
                SX * x + R1 * y + TX,
                R0 * x + SY * y + TY);
        }

        public Vector2 MultiplyPoint(Vector2 point) => MultiplyPoint(point.x, point.y);

        public static SwfMatrix operator *(SwfMatrix a, SwfMatrix b)
        {
            return new SwfMatrix
            (
                a.SX * b.TX + a.R1 * b.TY + a.TX,
                a.R0 * b.TX + a.SY * b.TY + a.TY,
                a.R0 * b.SX + a.SY * b.R0,
                a.SX * b.R1 + a.R1 * b.SY,
                a.SX * b.SX + a.R1 * b.R0,
                a.R0 * b.R1 + a.SY * b.SY
            );
        }

        public static explicit operator Matrix4x4(SwfMatrix m)
        {
            return new Matrix4x4
            {
                m00 = m.SX,
                m01 = m.R1,
                m03 = m.TX,
                m10 = m.R0,
                m11 = m.SY,
                m13 = m.TY,
                m22 = 1, // z scale
                m33 = 1, // homogeneous
            };
        }
    }
}