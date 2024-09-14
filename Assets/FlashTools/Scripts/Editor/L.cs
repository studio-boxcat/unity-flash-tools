using UnityEngine;

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
}