using System.Diagnostics;

namespace TMPro
{
    public static class L
    {
        [Conditional("DEBUG")]
        public static void E(string message)
        {
            UnityEngine.Debug.LogError(message);
        }
    }
}