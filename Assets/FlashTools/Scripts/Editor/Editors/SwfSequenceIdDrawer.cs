using JetBrains.Annotations;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace FT.Editors
{
    [UsedImplicitly]
    internal class SwfSequenceIdDrawer : OdinValueDrawer<SwfSequenceId>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            var curValue = ValueEntry.SmartValue;
            ValueEntry.SmartValue = SwfSequenceIdUtils.EnumPopup(label.text, curValue);
        }
    }
}