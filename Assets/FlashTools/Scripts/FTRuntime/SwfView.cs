using System;
using JetBrains.Annotations;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Assertions;

namespace FT
{
    [ExecuteAlways, DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class SwfView : MonoBehaviour
    {
        [SerializeField, Required, ChildGameObjectsOnly]
        private MeshFilter _meshFilter;
        [SerializeField, Required, ChildGameObjectsOnly]
        private MeshRenderer _meshRenderer;

        [SerializeField]
        private Color _tint = Color.white;
        public Color tint
        {
            get => _tint;
            set
            {
                _tint = value;
                UpdatePropTint(value);
            }
        }

        [NonSerialized]
        private SwfClip _clip;
        public SwfClip clip => _clip;
        [NonSerialized]
        private SwfSequenceId _sequence;
        public SwfSequenceId sequence => _sequence;
        [NonSerialized]
        private ushort _currentFrame;
        public ushort currentFrame => _currentFrame;

        public byte frameRate { get; private set; }
        public ushort frameCount { get; private set; }

        [NonSerialized]
        private SwfSequence _curSequence;
        [NonSerialized, CanBeNull]
        private Mesh _mesh;


        private void OnDestroy()
        {
            if (_mesh is not null)
            {
                DestroyImmediate(_mesh);
                _mesh = null;
            }
        }

        public void SetClip(SwfClip clip, SwfSequenceId sequenceId, ushort frame = 0)
        {
            Assert.IsNotNull(clip);
            Assert.AreNotEqual(default, sequenceId, "Invalid sequenceId: " + sequenceId);

            _clip = clip;
            frameRate = _clip.FrameRate;
            UpdatePropTexture(_clip.Atlas);

            SetSequence(sequenceId, frame);
        }

        public void SetSequence(SwfSequenceId sequenceId, ushort frame = 0)
        {
            Assert.IsNotNull(_clip, "SwfClip is not set.");

            _sequence = sequenceId;
            _curSequence = _clip.GetSequence(sequenceId);
            frameCount = _curSequence.FrameCount;
            UpdateMaterial(_curSequence.MaterialGroup);

            _currentFrame = ushort.MaxValue; // invalid value
            _lastFrameId = SwfFrameId.Invalid;
            SetFrame(frame);
        }

        public void SetFrame(ushort frame)
        {
            Assert.IsNotNull(_clip, "SwfClip is not set.");
            Assert.IsTrue(_curSequence.IsValid, "Sequence is not set.");
            if (_currentFrame == frame) return; // Ignore if frame is not changed.
            _currentFrame = frame;
            UpdateMesh(_curSequence.Frames[_currentFrame]);
        }

        public void ToBeginFrame() => SetFrame(0);
        public void ToEndFrame() => SetFrame(frameCount);
        public bool ToPrevFrame() => UpdateFrame_Clamp(-1);
        public bool ToNextFrame() => UpdateFrame_Clamp(1);

        public void UpdateFrame_Loop(int frameDelta)
        {
            var newFrame = (_currentFrame + frameDelta) % frameCount;
            if (newFrame < 0) newFrame += frameCount;
            Assert.IsTrue(newFrame >= 0 && newFrame < frameCount, "Invalid frame: " + newFrame);
            SetFrame((ushort) newFrame);
        }

        // Returns true if frame is not out of range.
        public bool UpdateFrame_Clamp(int frameDelta)
        {
            // Calculate new frame.
            var newFrame = _currentFrame + frameDelta;
            var outOfRange = false;
            if (newFrame < 0)
            {
                newFrame = 0;
                outOfRange = true;
            }
            else if (newFrame >= frameCount)
            {
                newFrame = frameCount - 1;
                outOfRange = true;
            }

            // Update mesh and material if frame changed.
            SetFrame((ushort) newFrame);

            return !outOfRange;
        }

        // ---------------------------------------------------------------------
        //
        // Internal
        //
        // ---------------------------------------------------------------------

        [NonSerialized]
        private SwfFrameId _lastFrameId = SwfFrameId.Invalid;

        private void UpdateMesh(SwfFrameId frame)
        {
            Assert.AreNotEqual(SwfFrameId.Invalid, frame, "Frame is not set.");

            // Allocate mesh if not exists.
            if (_mesh is null)
            {
                _mesh = MeshBuilder.CreateEmpty();
                _meshFilter.sharedMesh = _mesh;
            }

            // Sequence 가 초기화되지 않은 경우.
            if (_curSequence.IsInvalid)
            {
                _mesh!.Clear(true);
                return;
            }

            // 프레임이 변경되었을 때만 메쉬를 갱신.
            if (_lastFrameId != frame)
            {
                _lastFrameId = frame;
                _clip.BuildMesh(frame, _mesh);
            }
        }

        [NonSerialized]
        private MaterialGroupIndex _lastMaterialGroup = MaterialGroupIndex.Invalid;

        private void UpdateMaterial(MaterialGroupIndex materialGroup)
        {
            if (materialGroup == _lastMaterialGroup) return;
            _lastMaterialGroup = materialGroup;
            _meshRenderer.sharedMaterials = MaterialStore.Get(materialGroup);
        }

        private MaterialPropertyBlock _mpb;

        private MaterialPropertyBlock GetPropBlock()
        {
            if (_mpb == null)
            {
                _mpb = new MaterialPropertyBlock();
                _meshRenderer.GetPropertyBlock(_mpb);
            }

            return _mpb;
        }

        private void UpdatePropTexture(Texture2D texture)
        {
            var mpb = GetPropBlock();
            MaterialConfigurator.SetTexture(mpb, texture);
            _meshRenderer.SetPropertyBlock(mpb);
        }

        private void UpdatePropTint(Color color)
        {
            var mpb = GetPropBlock();
            MaterialConfigurator.SetTint(mpb, color);
            _meshRenderer.SetPropertyBlock(mpb);
        }

#if UNITY_EDITOR
        public void Editor_OnClipChanges()
        {
            if (_clip == null)
                return;

            Assert.AreNotEqual(default, _sequence,
                "Sequence must be set when _clip is not null.");

            frameRate = _clip.FrameRate;

            UpdatePropTexture(_clip.Atlas);

            _curSequence = _clip.GetSequence(_sequence);

            var oldFrameCount = frameCount;
            frameCount = _curSequence.FrameCount;
            if (oldFrameCount != frameCount)
                _currentFrame = 0;

            _lastFrameId = SwfFrameId.Invalid;
            UpdateMesh(_curSequence.Frames[_currentFrame]);

            _lastMaterialGroup = MaterialGroupIndex.Invalid;
            UpdateMaterial(_curSequence.MaterialGroup);
        }
#endif
    }
}