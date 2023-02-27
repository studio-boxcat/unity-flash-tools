using UnityEngine;
using UnityEngine.Rendering;
using FTRuntime.Internal;
using UnityEngine.Assertions;

namespace FTRuntime {
	[AddComponentMenu("FlashTools/SwfClip")]
	[ExecuteInEditMode, DisallowMultipleComponent]
	[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(SortingGroup))]
	public class SwfClip : MonoBehaviour {

		[SerializeField]
		MeshFilter            _meshFilter;
		[SerializeField]
		MeshRenderer          _meshRenderer;

		SwfClipAsset.Sequence _curSequence;

		// ---------------------------------------------------------------------
		//
		// Serialized fields
		//
		// ---------------------------------------------------------------------

		[Header("Animation")]

		[SerializeField]
		Color _tint = Color.white;

		[SerializeField]
		SwfClipAsset _clip;

		[SerializeField, HideInInspector]
		string _sequence;

		[SerializeField, HideInInspector]
		int _currentFrame;

		// ---------------------------------------------------------------------
		//
		// Properties
		//
		// ---------------------------------------------------------------------

		/// <summary>
		/// Gets or sets the animation tint color
		/// </summary>
		/// <value>The tint color</value>
		public Color tint {
			get { return _tint; }
			set {
				_tint = value;
				UpdatePropTint(value);
			}
		}

		/// <summary>
		/// Gets or sets the animation asset (reset sequence and current frame)
		/// </summary>
		/// <value>The animation asset</value>
		public SwfClipAsset clip => _clip;

		/// <summary>
		/// Gets or sets the animation sequence (reset current frame)
		/// </summary>
		/// <value>The animation sequence</value>
		public string sequence => _sequence;

		/// <summary>
		/// Gets or sets the animation current frame
		/// </summary>
		/// <value>The animation current frame</value>
		public int currentFrame => _currentFrame;

		/// <summary>
		/// Gets the current animation sequence frame count
		/// </summary>
		/// <value>The frame count</value>
		public int frameCount { get; private set; }

		/// <summary>
		/// Gets the animation frame rate
		/// </summary>
		/// <value>The frame rate</value>
		public float frameRate { get; private set; }

		/// <summary>
		/// Gets the current frame mesh bounding volume in world space
		/// (Since 1.3.8)
		/// </summary>
		/// <value>The bounding volume in world space</value>
		public Bounds currentWorldBounds {
			get {
				return _meshRenderer
					? _meshRenderer.bounds
					: new Bounds();
			}
		}

		// ---------------------------------------------------------------------
		//
		// Functions
		//
		// ---------------------------------------------------------------------

		public void SetClip(SwfClipAsset clip, string sequenceName, int frame = 0)
		{
			Assert.IsNotNull(clip);
			_clip = clip;
			frameRate = _clip.FrameRate;
			_sequence = sequenceName;
			_curSequence = _clip.GetSequence(sequenceName);
			frameCount = _curSequence.Frames.Length;
			_currentFrame = frame;
			_lastFrame = -1;
			_lastMaterialGroupIndex = -1;

			UpdatePropTexture(_clip.Atlas);
			UpdateMeshAndMaterial();
		}

		public void SetSequence(string sequenceName, int frame = 0)
		{
			Assert.IsNotNull(_clip);
			_sequence = sequenceName;
			_curSequence = _clip.GetSequence(sequenceName);
			frameCount = _curSequence.Frames.Length;
			_currentFrame = frame;
			_lastFrame = -1;
			UpdateMeshAndMaterial();
		}

		public void SetFrame(int frame)
		{
			Assert.IsNotNull(_clip);
			_currentFrame = frame;
			UpdateMeshAndMaterial();
		}

		/// <summary>
		/// Rewind current sequence to begin frame
		/// </summary>
		public void ToBeginFrame() {
			_currentFrame = 0;
			UpdateMeshAndMaterial();
		}

		/// <summary>
		/// Rewind current sequence to end frame
		/// </summary>
		public void ToEndFrame() {
			_currentFrame = frameCount > 0 ? frameCount - 1 : 0;
			UpdateMeshAndMaterial();
		}

		/// <summary>
		/// Rewind current sequence to previous frame
		/// </summary>
		/// <returns><c>true</c>, if animation was rewound, <c>false</c> otherwise</returns>
		public bool ToPrevFrame()
		{
			if (_currentFrame == 0)
				return false;

			--_currentFrame;
			UpdateMeshAndMaterial();
			return true;
		}

		/// <summary>
		/// Rewind current sequence to next frame
		/// </summary>
		/// <returns><c>true</c>, if animation was rewound, <c>false</c> otherwise</returns>
		public bool ToNextFrame() {
			if (_currentFrame >= frameCount - 1)
				return false;

			++_currentFrame;
			UpdateMeshAndMaterial();
			return true;
		}

		// ---------------------------------------------------------------------
		//
		// Internal
		//
		// ---------------------------------------------------------------------

		/// <summary>
		/// Update all animation properties (for internal use only)
		/// </summary>
		public void Internal_UpdateAllProperties()
		{
			if (_clip == null)
				return;
			if (_curSequence.IsInvalid)
				_curSequence = _clip.GetSequence(_sequence);

			frameRate = _clip.FrameRate;
			frameCount = _curSequence.Frames.Length;

			UpdatePropTexture(_clip.Atlas);
			UpdatePropTint(_tint);
			UpdateMeshAndMaterial();
		}

		int _lastFrame = -1;
		int _lastMaterialGroupIndex = -1;

		void UpdateMeshAndMaterial()
		{
			// Sequence 가 초기화되지 않은 경우.
			if (_curSequence.IsInvalid)
			{
				_meshFilter.sharedMesh = null;
				_meshRenderer.sharedMaterial = null;
				return;
			}

			// 프레임이 변경되었을 때만 메쉬와 매터리얼을 갱신.
			if (_lastFrame != _currentFrame)
			{
				_lastFrame = _currentFrame;
				var frame = _curSequence.Frames[_currentFrame];
				_meshFilter.sharedMesh = frame.Mesh;

				// 매터리얼이 변경되었을 때만 매터리얼을 갱신.
				if (_lastMaterialGroupIndex != frame.MaterialGroupIndex)
				{
					_lastMaterialGroupIndex = frame.MaterialGroupIndex;
					_meshRenderer.sharedMaterials = _clip.MaterialGroups[frame.MaterialGroupIndex].Materials;
				}
			}
		}

		MaterialPropertyBlock _propBlock;

		MaterialPropertyBlock GetPropBlock()
		{
			if (_propBlock == null)
			{
				_propBlock = new MaterialPropertyBlock();
				_meshRenderer.GetPropertyBlock(_propBlock);
			}

			return _propBlock;
		}

		void UpdatePropTexture(Texture2D texture)
		{
			var propBlock = GetPropBlock();
			propBlock.SetTexture(SwfUtils.MainTexShaderProp, texture);
			_meshRenderer.SetPropertyBlock(propBlock);
		}

		void UpdatePropTint(Color color)
		{
			var propBlock = GetPropBlock();
			propBlock.SetColor(SwfUtils.TintShaderProp, color);
			_meshRenderer.SetPropertyBlock(propBlock);
		}

		// ---------------------------------------------------------------------
		//
		// Messages
		//
		// ---------------------------------------------------------------------

		void Awake() {
			Internal_UpdateAllProperties();
		}

		void OnEnable() {
			var swf_manager = SwfManager.GetInstance(true);
			if ( swf_manager ) {
				swf_manager.AddClip(this);
			}
		}

		void OnDisable() {
			var swf_manager = SwfManager.GetInstance(false);
			if ( swf_manager ) {
				swf_manager.RemoveClip(this);
			}
		}

#if UNITY_EDITOR
		void Reset() {
			Internal_UpdateAllProperties();
		}

		void OnValidate() {
			Internal_UpdateAllProperties();
		}
#endif
	}
}