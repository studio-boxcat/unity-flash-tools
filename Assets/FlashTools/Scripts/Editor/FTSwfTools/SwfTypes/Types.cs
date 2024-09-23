namespace FTSwfTools.SwfTypes
{
    enum DefineId : ushort { }

    enum BitmapId : ushort { }

    enum Depth : ushort { }

    static partial class SwfBlendModeUtils
    {
        public static string ToName(this BitmapId value) => ((ushort) value).ToString("D4");
    }
}