using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
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
            public Action<SwfClip> Complete;
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
                    clip.Init(state.Bundle);
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
                clip.Init(bundle);
                state.Bundle = bundle;
                state.Clip = clip;
                return clip;
            }
        }

        public static void Load(string name, [NotNull] Action<SwfClip> onComplete)
        {
            L.I($"Load: {name} (async)");

            if (_states.TryGetValue(name, out var state))
            {
                if (state.Clip is not null)
                {
                    onComplete(state.Clip);
                    return;
                }

                L.W($"Request for {name} is in progress.");
                state.Complete += onComplete;
                return;
            }

            state = new State { Complete = onComplete };
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

            if (state.Clip is null)
            {
                state.Clip = clip;
                clip.Init(state.Bundle);
            }

            state.Complete.Invoke(clip);
            state.Complete = null;
        }

        static StringBuilder _pathBuilder;
        static int _pathBaseLength;

        static string GetPath(string name)
        {
            if (_pathBuilder is null)
            {
                _pathBuilder = new StringBuilder(Application.streamingAssetsPath).Append("/SwfClips/");
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
#endif
    }
}