using UnityEngine;
using System;
// ReSharper disable InconsistentNaming


namespace TMPro
{
    public struct Extents
    {
        public Vector2 min;
        public Vector2 max;

        public Extents(Vector2 min, Vector2 max)
        {
            this.min = min;
            this.max = max;
        }

        public override string ToString()
        {
            return "Min (" + min.x.ToString("f2") + ", " + min.y.ToString("f2") + ")   Max (" + max.x.ToString("f2") + ", " + max.y.ToString("f2") + ")";
        }
    }


    [Serializable]
    public struct Mesh_Extents
    {
        public Vector2 min;
        public Vector2 max;


        public Mesh_Extents(Vector2 min, Vector2 max)
        {
            this.min = min;
            this.max = max;
        }

        public override string ToString()
        {
            return "Min (" + min.x.ToString("f2") + ", " + min.y.ToString("f2") + ")   Max (" + max.x.ToString("f2") + ", " + max.y.ToString("f2") + ")";
        }
    }

    // Structure used for Word Wrapping which tracks the state of execution when the last space or carriage return character was encountered.
    public struct WordWrapState
    {
        public int previous_WordBreak;
        public int total_CharacterCount;
        public int visible_CharacterCount;
        public int firstCharacterIndex;
        public int firstVisibleCharacterIndex;
        public int lastVisibleCharIndex;
        public int lineNumber;

        public float maxCapHeight;
        public float maxAscender;
        public float maxDescender;
        public float startOfLineAscender;
        public float maxLineAscender;
        public float maxLineDescender;
        public float pageAscender;

        public HorizontalAlignmentOptions horizontalAlignment;

        public float xAdvance;
        public float preferredWidth;
        public float preferredHeight;

        public FontStyles fontStyle;
        public int italicAngle;

        public float currentFontSize;
        public float baselineOffset;
        public float lineOffset;
        public bool isDrivenLineSpacing;
        public float glyphHorizontalAdvanceAdjustment;

        public float cSpace;
        public float mSpace;

        public TMP_LineInfo lineInfo;

        public Color32 vertexColor;
        public TMP_FontStyleStack basicStyleStack;
        public TMP_TextProcessingStack<int> italicAngleStack;
        public TMP_TextProcessingStack<Color32> colorStack;
        public TMP_TextProcessingStack<float> sizeStack;
        public TMP_TextProcessingStack<float> baselineStack;
        public TMP_TextProcessingStack<MaterialReference> materialReferenceStack;
        public TMP_TextProcessingStack<HorizontalAlignmentOptions> lineJustificationStack;

        public TMP_FontAsset currentFontAsset;
        public Material currentMaterial;
        public int currentMaterialIndex;

        public Extents meshExtents;
    }


    public struct RichTextTagAttribute
    {
        public int nameHashCode;
        public int valueHashCode;
        public int valueStartIndex;
        public int valueLength;
    }

}
