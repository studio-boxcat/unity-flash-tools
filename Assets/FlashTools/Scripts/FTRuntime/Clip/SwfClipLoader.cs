using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

namespace FTRuntime
{
    public static class SwfClipLoader
    {
        class State
        {
            [NotNull] public object Current; // SwfClip or AssetBundleRequest or AssetBundleCreateRequest
            [CanBeNull] public AssetBundle Bundle; // For unloading.
            [CanBeNull] public Action<SwfClip> Completed;

            public State(SwfClip clip, AssetBundle bundle)
            {
                Current = clip;
                Bundle = bundle;
            }

            public State(AssetBundleCreateRequest req, Action<SwfClip> completed)
            {
                Current = req;
                Completed = completed;
            }

            public SwfClip CompleteImmediately()
            {
                if (Current is SwfClip clip)
                {
                    // XXX: Clip could be destroyed after play mode exit.
                    // Assert.IsNotNull(clip, "Clip is destroyed.");
                    return clip;
                }

                if (Current is AssetBundleRequest br)
                {
                    // Note that accessing asset before isDone is true will stall the loading process.
                    // https://docs.unity3d.com/ScriptReference/AssetBundleRequest-asset.html
                    clip = (SwfClip) br.asset;
                    Current = clip;
                    return clip;
                }

                // Note that accessing asset before isDone is true will stall the loading process.
                // https://docs.unity3d.com/ScriptReference/AssetBundleCreateRequest-assetBundle.html
                // no need to invoke Completed callback as unity will do it.
                Bundle = ((AssetBundleCreateRequest) Current).assetBundle;
#if UNITY_EDITOR
                Assert.IsNotNull(Bundle, "Failed to load bundle. This could happen on exiting play mode.");
#endif

                clip = Bundle!.LoadAsset<SwfClip>(Bundle.name);
                Current = clip;
                return clip;
            }

            public void AddCompleted(Action<SwfClip> completed)
            {
                if (Current is SwfClip clip)
                {
                    completed(clip);
                    return;
                }

                if (Completed is not null)
                    Completed += completed;
                else
                    Completed = completed;
            }
        }

        static readonly Dictionary<string, State> _states = new();

        public static SwfClip Load(string name)
        {
            L.I($"Load: {name}");

            if (_states.TryGetValue(name, out var state))
                return state.CompleteImmediately();

            var bundle = AssetBundle.LoadFromFile(GetPath(name));
            var clip = (SwfClip) bundle.LoadAsset(name);
            _states.Add(name, new State(clip, bundle));
            return clip;
        }

        public static void LoadAsync(string name, [CanBeNull] Action<SwfClip> completed)
        {
            L.I($"LoadAsync: {name}");

            if (_states.TryGetValue(name, out var state))
            {
                state.AddCompleted(completed);
                return;
            }

            var r = AssetBundle.LoadFromFileAsync(GetPath(name));
            _states.Add(name, new State(r, completed)); // set _states before registering callback. callback could be invoked immediately.
            r.completed += _processLoadedBundle ??= ProcessLoadedBundle;
        }

        static Action<AsyncOperation> _processLoadedBundle;
        static void ProcessLoadedBundle(AsyncOperation op)
        {
            var bundle = ((AssetBundleCreateRequest) op).assetBundle;
            var name = bundle.name; // asset name = bundle name
            var r = bundle.LoadAssetAsync<SwfClip>(name);
            var state = _states[name]; // setup state before registering callback. callback could be invoked immediately.
            state.Current = r;
            state.Bundle = bundle;
            r.completed += _processLoadedAsset ??= ProcessLoadedAsset;
        }

        static Action<AsyncOperation> _processLoadedAsset;
        static void ProcessLoadedAsset(AsyncOperation op)
        {
            var clip = (SwfClip) ((AssetBundleRequest) op).asset;
            var state = _states[clip.name];
            state.Current = clip;
            var callback = state.Completed; // to ensure re-entrance.
            state.Completed = null;
            callback?.Invoke(clip);
        }

        const string _subdir = "clips";
        static StringBuilder _pathBuilder;
        static int _pathBaseLength;

        static string GetPath(string name)
        {
            if (_pathBuilder is null)
            {
#if UNITY_EDITOR
                _pathBuilder = new StringBuilder(GetBuildDir(UnityEditor.BuildTarget.iOS));
#else
                _pathBuilder = new StringBuilder(Application.streamingAssetsPath).Append("/" + _subdir + "/");
#endif

                _pathBaseLength = _pathBuilder.Length;
            }

            _pathBuilder.Length = _pathBaseLength;
            return _pathBuilder.Append(name).ToString();
        }

#if UNITY_EDITOR
        static SwfClipLoader()
        {
            UnityEditor.EditorApplication.playModeStateChanged += change =>
            {
                if (change is not
                    (UnityEditor.PlayModeStateChange.EnteredEditMode
                    or UnityEditor.PlayModeStateChange.ExitingEditMode))
                {
                    return;
                }

                L.I("Unload all bundles");
                foreach (var state in _states.Values)
                {
                    try
                    {
                        state.CompleteImmediately(); // wait for bundle to be loaded.
                        if (state.Bundle != null)
                            state.Bundle.Unload(true);
                    }
                    catch (Exception e)
                    {
                        // Exception could be thrown when exiting play mode.
                        L.E(e);
                    }
                }
                _states.Clear();
                L.I("Unloaded all bundles");
            };
        }

        internal static string GetBuildDir(UnityEditor.BuildTarget buildTarget)
        {
            return buildTarget switch
            {
                UnityEditor.BuildTarget.iOS => "proj-ios/Data/Raw/" + _subdir + "/",
                UnityEditor.BuildTarget.Android => "proj-android/unityLibrary/src/main/assets/" + _subdir + "/",
                _ => throw new NotSupportedException($"Unsupported build target: {buildTarget}"),
            };
        }
#endif
    }
}