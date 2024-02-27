using UnityEngine;
using FTRuntime.Internal;
using System.Collections.Generic;

namespace FTRuntime
{
    [AddComponentMenu("FlashTools/SwfManager")]
    [ExecuteInEditMode, DisallowMultipleComponent]
    public class SwfManager : MonoBehaviour
    {
        SwfAssocList<SwfClip> _clips = new SwfAssocList<SwfClip>();
        SwfAssocList<SwfClipController> _controllers = new SwfAssocList<SwfClipController>();
        SwfList<SwfClipController> _safeUpdates = new SwfList<SwfClipController>();

        bool _isPaused = false;
        bool _useUnscaledDt = false;
        float _rateScale = 1.0f;

        // ---------------------------------------------------------------------
        //
        // Instance
        //
        // ---------------------------------------------------------------------

        static SwfManager _instance;

        /// <summary>
        /// Get cached manager instance from scene or create it (if allowed)
        /// </summary>
        /// <returns>The manager instance</returns>
        /// <param name="allow_create">If set to <c>true</c> allow create</param>
        public static SwfManager GetInstance(bool allow_create)
        {
            if (!_instance)
            {
                _instance = FindAnyObjectByType<SwfManager>();
                if (allow_create && !_instance)
                {
                    var go = new GameObject("[SwfManager]", typeof(SwfManager));
                    _instance = go.GetComponent<SwfManager>();
                }
            }
            return _instance;
        }

        // ---------------------------------------------------------------------
        //
        // Properties
        //
        // ---------------------------------------------------------------------

        /// <summary>
        /// Get animation clip count on scene
        /// </summary>
        /// <value>Clip count</value>
        public int clipCount
        {
            get { return _clips.Count; }
        }

        /// <summary>
        /// Get animation clip controller count on scene
        /// </summary>
        /// <value>Clip controller count</value>
        public int controllerCount
        {
            get { return _controllers.Count; }
        }

        /// <summary>
        /// Get or set a value indicating whether animation updates is paused
        /// </summary>
        /// <value><c>true</c> if is paused; otherwise, <c>false</c></value>
        public bool isPaused
        {
            get { return _isPaused; }
            set { _isPaused = value; }
        }

        /// <summary>
        /// Get or set a value indicating whether animation updates is playing
        /// </summary>
        /// <value><c>true</c> if is playing; otherwise, <c>false</c></value>
        public bool isPlaying
        {
            get { return !_isPaused; }
            set { _isPaused = !value; }
        }

        /// <summary>
        /// Get or set a value indicating whether animation updates uses unscaled delta time
        /// </summary>
        /// <value><c>true</c> if uses unscaled delta time; otherwise, <c>false</c></value>
        public bool useUnscaledDt
        {
            get { return _useUnscaledDt; }
            set { _useUnscaledDt = value; }
        }

        /// <summary>
        /// Get or set the global animation rate scale
        /// </summary>
        /// <value>Global rate scale</value>
        public float rateScale
        {
            get { return _rateScale; }
            set { _rateScale = Mathf.Clamp(value, 0.0f, float.MaxValue); }
        }

        // ---------------------------------------------------------------------
        //
        // Functions
        //
        // ---------------------------------------------------------------------

        /// <summary>
        /// Pause animation updates
        /// </summary>
        public void Pause()
        {
            isPaused = true;
        }

        /// <summary>
        /// Resume animation updates
        /// </summary>
        public void Resume()
        {
            isPlaying = true;
        }

        // ---------------------------------------------------------------------
        //
        // Internal
        //
        // ---------------------------------------------------------------------

        internal void AddClip(SwfClip clip)
        {
            _clips.Add(clip);
        }

        internal void RemoveClip(SwfClip clip)
        {
            _clips.Remove(clip);
        }

        internal void GetAllClips(List<SwfClip> clips)
        {
            _clips.AssignTo(clips);
        }

        internal void AddController(SwfClipController controller)
        {
            _controllers.Add(controller);
        }

        internal void RemoveController(SwfClipController controller)
        {
            _controllers.Remove(controller);
        }

        void SetupCameras()
        {
            foreach (var camera in Camera.allCameras)
            {
                camera.clearStencilAfterLightingPass = true;
            }
        }

        void GrabEnabledClips()
        {
            var clips = FindObjectsByType<SwfClip>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0, e = clips.Length; i < e; ++i)
            {
                var clip = clips[i];
                if (clip.enabled)
                {
                    _clips.Add(clip);
                }
            }
        }

        void GrabEnabledControllers()
        {
            var controllers = FindObjectsByType<SwfClipController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0, e = controllers.Length; i < e; ++i)
            {
                var controller = controllers[i];
                if (controller.enabled)
                {
                    _controllers.Add(controller);
                }
            }
        }

        void DropClips()
        {
            _clips.Clear();
        }

        void DropControllers()
        {
            _controllers.Clear();
        }

        void LateUpdateControllers(float scaled_dt, float unscaled_dt)
        {
            _controllers.AssignTo(_safeUpdates);
            for (int i = 0, e = _safeUpdates.Count; i < e; ++i)
            {
                var ctrl = _safeUpdates[i];
                if (ctrl)
                {
                    ctrl.Internal_Update(scaled_dt, unscaled_dt);
                }
            }
            _safeUpdates.Clear();
        }

        // ---------------------------------------------------------------------
        //
        // Messages
        //
        // ---------------------------------------------------------------------

        void OnEnable()
        {
            SetupCameras();
            GrabEnabledClips();
            GrabEnabledControllers();
        }

        void OnDisable()
        {
            DropClips();
            DropControllers();
        }

        void LateUpdate()
        {
            if (isPlaying)
            {
                LateUpdateControllers(
                    rateScale * (useUnscaledDt ? Time.unscaledDeltaTime : Time.deltaTime),
                    rateScale * Time.unscaledDeltaTime);
            }
        }
    }
}