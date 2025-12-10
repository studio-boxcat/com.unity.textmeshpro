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
        public int lastVisibleCharacterIndex;

        public float ascender;
        public float descender;
        public float maxAdvance;

        public float width;
        public float marginLeft;
        public float marginRight;

        public HorizontalAlignmentOptions alignment;
        public Extents lineExtents;
    }
}