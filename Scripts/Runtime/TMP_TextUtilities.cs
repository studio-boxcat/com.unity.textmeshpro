using UnityEngine;


namespace TMPro
{
    public static class TMP_TextUtilities
    {
        // CHARACTER HANDLING

        /// <summary>
        /// Function which returns a simple hashcode from a string.
        /// </summary>
        /// <returns></returns>
        public static int GetSimpleHashCode(string s)
        {
            int hashCode = 0;

            for (int i = 0; i < s.Length; i++)
                hashCode = ((hashCode << 5) + hashCode) ^ s[i];

            return hashCode;
        }

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
            {
                value += HexToInt(s[i]) * (int)Mathf.Pow(16, (s.Length - 1) - i);
            }

            return value;
        }

    }
}
