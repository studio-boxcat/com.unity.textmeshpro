using UnityEngine;

namespace TMPro
{
    public static class TMPro_ExtensionMethods
    {
        public static bool Compare(this Color32 a, Color32 b)
        {
            return a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;
        }
    }

    public static class TMP_Math
    {
        public const float FLOAT_MAX = 32767;
        public const float FLOAT_MIN = -32767;
        public const int INT_MAX = 2147483647;
        public const int INT_MIN = -2147483647;
        public const float FLOAT_UNSET = -32767;
    }
}