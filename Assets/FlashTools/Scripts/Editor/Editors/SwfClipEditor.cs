using System.Collections.Generic;
using System.Linq;
using FTRuntime;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace FTEditor.Editors
{
    [CustomEditor(typeof(SwfClip)), CanEditMultipleObjects]
    class SwfClipAssetEditor : OdinEditor
    {
        List<SwfClip> _clips = new();
        SwfClipPreview _preview = null;

        protected override void OnEnable()
        {
            base.OnEnable();
            _clips = targets.OfType<SwfClip>().ToList();
            SetupPreviews();
        }

        protected override void OnDisable()
        {
            base.OnEnable();
            ShutdownPreviews();
            _clips.Clear();
        }

        public override bool RequiresConstantRepaint() => _clips.Count > 0;
        public override bool HasPreviewGUI() => _clips.Count > 0;
        public override void OnPreviewSettings() => _preview?.OnPreviewSettings();
        public override void OnPreviewGUI(Rect r, GUIStyle background) => _preview?.OnPreviewGUI(r, background);

        void SetupPreviews()
        {
            ShutdownPreviews();
            _preview = new SwfClipPreview();
            _preview.Initialize(targets.Where(x => x != null && x is SwfClip).ToArray());
        }

        void ShutdownPreviews()
        {
            if (_preview is null) return;
            _preview.Shutdown();
            _preview = null;
        }
    }
}