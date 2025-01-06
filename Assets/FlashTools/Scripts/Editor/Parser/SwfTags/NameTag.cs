using FTEditor;

namespace FTSwfTools.SwfTags
{
    internal class NameTag : SwfTagBase
    {
        public readonly struct NameData
        {
            public readonly DefineId Tag;
            public readonly string Name;

            public NameData(DefineId tag, string name)
            {
                Tag = tag;
                Name = name;
            }
        }

        public readonly NameData[] Names;

        private NameTag(NameData[] names) => Names = names;

        public static NameTag Create(SwfStreamReader reader)
        {
            var count = reader.ReadUInt16();
            var data = new NameData[count];
            for (var i = 0; i < count; ++i)
                data[i] = new NameData((DefineId) reader.ReadUInt16(), reader.ReadString());
            return new NameTag(data);
        }
    }
}