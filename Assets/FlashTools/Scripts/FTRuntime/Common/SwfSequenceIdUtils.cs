#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.Utilities;
using UnityEditor;

namespace FT
{
    internal static class SwfSequenceIdUtils
    {
        private static readonly SwfSequenceId[] _sequenceIds;
        private static readonly string[] _sequenceNames;

        static SwfSequenceIdUtils()
        {
            var ids = new List<SwfSequenceId>();
            var names = new List<string>();
            TypeCache.GetTypesWithAttribute<SwfSequenceIdDefAttribute>()
                .SelectMany(t => t.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                .Where(f => f.FieldType == typeof(SwfSequenceId))
                .ForEach(f =>
                {
                    ids.Add((SwfSequenceId) f.GetValue(null));
                    names.Add(f.Name);
                });

            _sequenceIds = ids.ToArray();
            _sequenceNames = names.ToArray();
        }

        public static string ToName(SwfSequenceId sequenceId)
        {
            var index = Array.IndexOf(_sequenceIds, sequenceId);
            return index >= 0 ? _sequenceNames[index] : "Unknown";
        }

        public static SwfSequenceId EnumPopup(string label, SwfSequenceId value)
        {
            var curIndex = Array.IndexOf(_sequenceIds, value);
            if (curIndex is -1) return default;
            var newIndex = EditorGUILayout.Popup(label, curIndex, _sequenceNames);
            return _sequenceIds[newIndex];
        }
    }
}
#endif