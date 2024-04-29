namespace TMPro
{
    public static class TMP_TextParsingUtilities
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="highSurrogate"></param>
        /// <param name="lowSurrogate"></param>
        /// <returns></returns>
        internal static uint ConvertToUTF32(uint highSurrogate, uint lowSurrogate)
        {
            return (highSurrogate - CodePoint.HIGH_SURROGATE_START) * 0x400
                   + (lowSurrogate - CodePoint.LOW_SURROGATE_START)
                   + CodePoint.UNICODE_PLANE01_START;
        }
    }
}
