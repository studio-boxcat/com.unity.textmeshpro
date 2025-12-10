// ReSharper disable InconsistentNaming
using System.Diagnostics;
using UnityEngine;


namespace TMPro
{
    public struct TMP_Vertex
    {
        public Vector2 position;
        public Vector2 uv;
        public Vector2 uv2; // x: packed uv (0-1), y: sdf scale
        public Color32 color;
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

        public float pointSize;

        public int lineNumber;


        public TMP_Vertex vertex_BL;
        public TMP_Vertex vertex_TL;
        public TMP_Vertex vertex_TR;
        public TMP_Vertex vertex_BR;

        public Vector2 topLeft;
        public Vector2 bottomLeft;
        public Vector2 topRight;
        public Vector2 bottomRight;

        public float xAdvance;
        internal float adjustedAscender;
        internal float adjustedDescender;

        public float scale;
        public FontStyles style;
        public bool isVisible;
    }
}