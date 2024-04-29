using UnityEngine;
using System.Collections.Generic;

namespace TMPro
{
    public static class TMPro_ExtensionMethods
    {
        internal static string UintToString(this List<uint> unicodes)
        {
            char[] chars = new char[unicodes.Count];

            for (int i = 0; i < unicodes.Count; i++)
            {
                chars[i] = (char)unicodes[i];
            }

            return new string(chars);
        }

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
