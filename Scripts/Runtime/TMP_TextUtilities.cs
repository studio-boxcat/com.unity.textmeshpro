using System;
using UnityEngine;


namespace TMPro
{
    public static class TMP_TextUtilities
    {
        /// <summary>
        /// Function to convert Hex to Int
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public static int HexToInt(char hex)
        {
            return hex switch
            {
                '0' => 0,
                '1' => 1,
                '2' => 2,
                '3' => 3,
                '4' => 4,
                '5' => 5,
                '6' => 6,
                '7' => 7,
                '8' => 8,
                '9' => 9,
                'A' => 10,
                'B' => 11,
                'C' => 12,
                'D' => 13,
                'E' => 14,
                'F' => 15,
                'a' => 10,
                'b' => 11,
                'c' => 12,
                'd' => 13,
                'e' => 14,
                'f' => 15,
                _ => 15
            };
        }


        /// <summary>
        /// Function to convert a properly formatted string which contains an hex value to its decimal value.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static int StringHexToInt(string s)
        {
            int value = 0;
            for (int i = 0; i < s.Length; i++)
                value += HexToInt(s[i]) * (int) Mathf.Pow(16, (s.Length - 1) - i);
            return value;
        }

        public static bool IsChineseOrJapanese(int c)
        {
            return c is > 0x2E80 and < 0x9FFF || /* CJK */
                   c is > 0xF900 and < 0xFAFF || /* CJK Compatibility Ideographs */
                   c is > 0xFE30 and < 0xFE4F || /* CJK Compatibility Forms */
                   c is > 0xFF00 and < 0xFFEF; /* CJK Halfwidth */
        }

        public static float CalculateJustificationOffset(TMP_LineInfo lineInfo, HorizontalAlignmentOptions lineAlignment)
        {
            return lineAlignment switch
            {
                HorizontalAlignmentOptions.Left => 0,
                HorizontalAlignmentOptions.Center => lineInfo.width / 2 - lineInfo.maxAdvance / 2,
                HorizontalAlignmentOptions.Right => lineInfo.width - lineInfo.maxAdvance,
                _ => throw new ArgumentOutOfRangeException(nameof(lineAlignment), lineAlignment, null)
            };
        }
    }
}