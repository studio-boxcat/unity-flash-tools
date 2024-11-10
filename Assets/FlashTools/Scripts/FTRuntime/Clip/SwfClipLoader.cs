using System;
using Boxcat.Bundler;
using Object = UnityEngine.Object;

namespace FTRuntime
{
    public static class SwfClipLoader
    {
        public static void LoadAsync(string name, Action<Object> onLoaded)
            => SingleAssetLoader.LoadAsync(name, onLoaded);

        public static SwfClip Load(string name)
            => (SwfClip) SingleAssetLoader.Load(name);
    }
}