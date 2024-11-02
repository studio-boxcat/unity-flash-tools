using System;
using UnityEngine;

namespace FTRuntime.Internal
{
    [Serializable]
    struct SwfMatrix
    {
        public float TranslateX; // m03
        public float TranslateY; // m13
        public float RotateSkew0; // m10
        public float RotateSkew1; // m01
        public float ScaleX; // m00
        public float ScaleY; // m11

        public SwfMatrix(float translateX, float translateY, float rotateSkew0, float rotateSkew1, float scaleX, float scaleY)
        {
            TranslateX = translateX;
            TranslateY = translateY;
            RotateSkew0 = rotateSkew0;
            RotateSkew1 = rotateSkew1;
            ScaleX = scaleX;
            ScaleY = scaleY;
        }

        public static readonly SwfMatrix identity = new(0, 0, 0, 0, 1, 1);
        public static SwfMatrix Translate(float x, float y) => new(x, y, 0, 0, 1, 1);
        public static SwfMatrix Scale(float x, float y) => new(0, 0, 0, 0, x, y);

        public Vector2 MultiplyPoint(float x, float y)
        {
            return new Vector2(
                ScaleX * x + RotateSkew1 * y + TranslateX,
                RotateSkew0 * x + ScaleY * y + TranslateY);
        }

        public Vector2 MultiplyPoint(Vector2 point) => MultiplyPoint(point.x, point.y);

        public static SwfMatrix operator *(SwfMatrix a, SwfMatrix b)
        {
            return new SwfMatrix
            (
                a.ScaleX * b.TranslateX + a.RotateSkew1 * b.TranslateY + a.TranslateX,
                a.RotateSkew0 * b.TranslateX + a.ScaleY * b.TranslateY + a.TranslateY,
                a.RotateSkew0 * b.ScaleX + a.ScaleY * b.RotateSkew0,
                a.ScaleX * b.RotateSkew1 + a.RotateSkew1 * b.ScaleY,
                a.ScaleX * b.ScaleX + a.RotateSkew1 * b.RotateSkew0,
                a.RotateSkew0 * b.RotateSkew1 + a.ScaleY * b.ScaleY
            );
        }
    }
}