using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

namespace FT.Editors
{
    [UsedImplicitly]
    internal class SwfSequenceIdDrawer : OdinValueDrawer<SwfSequenceId>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            Init();
            var curValue = ValueEntry.SmartValue;
            var selectedIndex = Array.IndexOf(_sequenceIds, curValue);
            ValueEntry.SmartValue = _sequenceIds[EditorGUILayout.Popup(label, selectedIndex, _sequenceNames)];
        }

        private static SwfSequenceId[] _sequenceIds;
        private static string[] _sequenceNames;

        private static void Init()
        {
            if (_sequenceIds is not null)
                return;

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
    }
}