namespace TMPro
{

    /// <summary>
    /// Structure which contains information about the individual lines of text.
    /// </summary>
    public struct TMP_LineInfo
    {
        public int characterCount;
        public int firstCharacterIndex;
        public int lastCharacterIndex;
        public float maxAdvance;
        public float width;
        public HorizontalAlignmentOptions alignment;
    }
}