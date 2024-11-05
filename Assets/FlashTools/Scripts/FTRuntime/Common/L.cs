using System;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace FTRuntime
{
    static class L
    {
        [Conditional("DEBUG")]
        public static void I(string message, Object context = null) => Debug.Log($"[FlashTools] {message}", context);
        [Conditional("DEBUG")]
        public static void W(string message) => Debug.LogWarning($"[FlashTools] {message}");
        public static void E(string message) => Debug.LogError($"[FlashTools] {message}");
        public static void E(Exception exception) => Debug.LogError(exception);
    }
}