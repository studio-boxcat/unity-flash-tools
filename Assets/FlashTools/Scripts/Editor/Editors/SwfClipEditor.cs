using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace FT.Editors
{
    [CustomEditor(typeof(SwfClip)), CanEditMultipleObjects]
    internal class SwfClipAssetEditor : OdinEditor
    {
        private List<SwfClip> _clips = new();
        private SwfClipPreview _preview = null;

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

        private void SetupPreviews()
        {
            ShutdownPreviews();
            _preview = new SwfClipPreview();
            _preview.Initialize(targets.Where(x => x != null && x is SwfClip).ToArray());
        }

        private void ShutdownPreviews()
        {
            if (_preview is null) return;
            _preview.Shutdown();
            _preview = null;
        }
    }
}