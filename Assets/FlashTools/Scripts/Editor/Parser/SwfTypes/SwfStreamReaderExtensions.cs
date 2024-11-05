using FTRuntime;
using UnityEngine;

namespace FTSwfTools
{
    static class SwfStreamReaderExtensions
    {
        public static Rect ReadRect(this SwfStreamReader reader)
        {
            var bits = reader.ReadUnsignedBits(5);
            var xmin = reader.ReadSignedBits(bits);
            var xmax = reader.ReadSignedBits(bits);
            var ymin = reader.ReadSignedBits(bits);
            var ymax = reader.ReadSignedBits(bits);
            reader.AlignToByte();
            return Rect.MinMaxRect(xmin, ymin, xmax, ymax);
        }

        public static SwfMatrix ReadMatrix(this SwfStreamReader reader)
        {
            // scale
            var scaleX = 1f;
            var scaleY = 1f;
            if (reader.ReadBit())
            {
                var bits = (byte) reader.ReadUnsignedBits(5);
                scaleX = reader.ReadFixedPoint16(bits);
                scaleY = reader.ReadFixedPoint16(bits);
            }

            // rotate
            var rotateSkew0 = 0f;
            var rotateSkew1 = 0f;
            if (reader.ReadBit())
            {
                var bits = (byte) reader.ReadUnsignedBits(5);
                rotateSkew0 = reader.ReadFixedPoint16(bits);
                rotateSkew1 = reader.ReadFixedPoint16(bits);
            }

            // translate
            var translate_bits = (byte) reader.ReadUnsignedBits(5);
            var translateX = reader.ReadSignedBits(translate_bits);
            var translateY = reader.ReadSignedBits(translate_bits);

            reader.AlignToByte();

            // convert to matrix
            return new SwfMatrix(
                translateX,
                translateY,
                rotateSkew0,
                rotateSkew1,
                scaleX,
                scaleY);
        }
    }
}