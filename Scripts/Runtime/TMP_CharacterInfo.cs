using System.Diagnostics;
using UnityEngine;


namespace TMPro
{
    public struct TMP_Vertex
    {
        public Vector3 position;
        public Vector2 uv;
        public Vector2 uv2;
        public Color32 color;

        static readonly TMP_Vertex k_Zero = new();
    }

    /// <summary>
    /// Structure containing information about individual text elements (character or sprites).
    /// </summary>
    [DebuggerDisplay("Unicode '{character}'  ({((uint)character).ToString(\"X\")})")]
    public struct TMP_CharacterInfo
    {
        public char character; // Should be changed to an uint to handle UTF32

        public TMP_TextElement textElement;
        public TMP_FontAsset fontAsset;
        public Material material;
        public int materialReferenceIndex;
        public bool isUsingAlternateTypeface;

        public float pointSize;

        public int lineNumber;


        public TMP_Vertex vertex_BL;
        public TMP_Vertex vertex_TL;
        public TMP_Vertex vertex_TR;
        public TMP_Vertex vertex_BR;

        public Vector3 topLeft;
        public Vector3 bottomLeft;
        public Vector3 topRight;
        public Vector3 bottomRight;

        public float origin;
        public float xAdvance;
        public float ascender;
        public float descender;
        internal float adjustedAscender;
        internal float adjustedDescender;

        public float scale;
        public FontStyles style;
        public bool isVisible;
    }
}
