using System.Diagnostics;

namespace TMPro
{
    /// <summary>
    /// Minimal debug logging helper for TextMeshPro internals.
    /// Uses [Conditional("DEBUG")] to strip calls in release builds,
    /// avoiding string allocations and method call overhead.
    /// </summary>
    internal static class L
    {
        [Conditional("DEBUG")]
        public static void E(string message)
        {
            UnityEngine.Debug.LogError(message);
        }
    }
}
