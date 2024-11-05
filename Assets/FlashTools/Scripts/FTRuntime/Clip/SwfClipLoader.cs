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
            public AssetBundle Bundle;
            public SwfClip Clip;
            [CanBeNull] public Action<SwfClip> Completed;
        }

        static readonly Dictionary<string, State> _states = new();

        public static SwfClip Load(string name)
        {
            L.I($"Load: {name}");

            if (_states.TryGetValue(name, out var state))
            {
                if (state.Clip is not null)
                {
                    Assert.IsNotNull(state.Clip, "Clip is destroyed.");
                    return state.Clip;
                }

                if (state.Bundle is not null)
                {
                    var clip = (SwfClip) state.Bundle.LoadAsset(name);
                    state.Clip = clip;
                    return clip;
                }
            }
            else
            {
                state = new State();
                _states.Add(name, state);
            }

            {
                var bundle = AssetBundle.LoadFromFile(GetPath(name));
                var clip = (SwfClip) bundle.LoadAsset(name);
                state.Bundle = bundle;
                state.Clip = clip;
                return clip;
            }
        }

        public static void Load(string name, [CanBeNull] Action<SwfClip> completed)
        {
            L.I($"Load: {name} (async)");

            if (_states.TryGetValue(name, out var state))
            {
                if (state.Clip is not null)
                {
                    completed?.Invoke(state.Clip);
                    return;
                }

                L.W($"Request for {name} is in progress.");
                state.Completed += completed;
                return;
            }

            state = new State { Completed = completed };
            _states.Add(name, state);
            var op = AssetBundle.LoadFromFileAsync(GetPath(name));
            op.completed += _processLoadedBundle ??= ProcessLoadedBundle;
        }

        static Action<AsyncOperation> _processLoadedBundle;
        static void ProcessLoadedBundle(AsyncOperation op)
        {
            var bundle = ((AssetBundleCreateRequest) op).assetBundle;
            var name = bundle.name; // asset name = bundle name
            _states[name].Bundle = bundle;
            bundle.LoadAssetAsync<SwfClip>(name).completed += _processLoadedAsset ??= ProcessLoadedAsset;
        }

        static Action<AsyncOperation> _processLoadedAsset;
        static void ProcessLoadedAsset(AsyncOperation op)
        {
            var clip = (SwfClip) ((AssetBundleRequest) op).asset;
            var state = _states[clip.name];
            state.Clip ??= clip;
            state.Completed?.Invoke(clip);
            state.Completed = null;
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
                if (change is UnityEditor.PlayModeStateChange.EnteredEditMode
                    or UnityEditor.PlayModeStateChange.ExitingEditMode)
                {
                    L.I("Unload all bundles");
                    foreach (var state in _states.Values)
                    {
                        if (state.Bundle != null)
                            state.Bundle.Unload(true);
                    }
                    _states.Clear();
                }
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