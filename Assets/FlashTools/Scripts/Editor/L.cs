using System;
using UnityEngine;
using Object = UnityEngine.Object;

static class L
{
    public static void I(string message, Object context = null)
    {
        Debug.Log($"[FlashTools] {message}", context);
    }

    public static void W(string message)
    {
        Debug.LogWarning($"[FlashTools] {message}");
    }

    public static void E(string message)
    {
        Debug.LogError($"[FlashTools] {message}");
    }

    public static void E(Exception exception)
    {
        Debug.LogError(exception);
    }
}