using UnityEngine;
using UnityEngine.Rendering;

namespace FTSwfTools
{
    internal enum SwfBlendMode : byte
    {
        Normal,
        Layer,
        Multiply,
        Screen,
        Lighten,
        Darken,
        Difference,
        Add,
        Subtract,
        Invert,
        Alpha,
        Erase,
        Overlay,
        Hardlight
    }

    internal static partial class ExtensionMethods
    {
        public static SwfBlendMode ReadBlendMode(this SwfStreamReader reader)
        {
            var value = reader.ReadByte();
            return value switch
            {
                0 or 1 => SwfBlendMode.Normal,
                2 => SwfBlendMode.Layer,
                3 => SwfBlendMode.Multiply,
                4 => SwfBlendMode.Screen,
                5 => SwfBlendMode.Lighten,
                6 => SwfBlendMode.Darken,
                7 => SwfBlendMode.Difference,
                8 => SwfBlendMode.Add,
                9 => SwfBlendMode.Subtract,
                10 => SwfBlendMode.Invert,
                11 => SwfBlendMode.Alpha,
                12 => SwfBlendMode.Erase,
                13 => SwfBlendMode.Overlay,
                14 => SwfBlendMode.Hardlight,
                _ => throw new System.Exception($"Incorrect blend SwfBlendMode id: {value}")
            };
        }

        public static SwfBlendMode Composite(this SwfBlendMode a, SwfBlendMode b)
            => a is (SwfBlendMode.Normal or SwfBlendMode.Layer) ? b : a;
        
        public static (BlendOp BlendOp, BlendMode SrcBlend, BlendMode DstBlend) GetMaterialiProperties(this SwfBlendMode value)
        {
			return value switch
			{
				SwfBlendMode.Normal => (BlendOp.Add, BlendMode.One, BlendMode.OneMinusSrcAlpha),
				SwfBlendMode.Layer => (BlendOp.Add, BlendMode.One, BlendMode.OneMinusSrcAlpha),
				SwfBlendMode.Multiply => (BlendOp.Add, BlendMode.DstColor, BlendMode.OneMinusSrcAlpha),
				SwfBlendMode.Screen => (BlendOp.Add, BlendMode.OneMinusDstColor, BlendMode.One),
				SwfBlendMode.Lighten => (BlendOp.Max, BlendMode.One, BlendMode.OneMinusSrcAlpha),
				SwfBlendMode.Add => (BlendOp.Add, BlendMode.One, BlendMode.One),
				SwfBlendMode.Subtract => (BlendOp.ReverseSubtract, BlendMode.One, BlendMode.One),
				_ => throw new UnityException($"SwfMaterialCache. Incorrect blend mode=> {value}"),
			};
        }
    }
}