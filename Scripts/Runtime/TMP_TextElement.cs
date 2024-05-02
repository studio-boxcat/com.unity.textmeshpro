using System;
using UnityEngine;
using UnityEngine.TextCore;

namespace TMPro
{
    /// <summary>
    /// Base class for all text elements like Character and SpriteCharacter.
    /// </summary>
    [Serializable]
    public class TMP_TextElement
    {
        /// <summary>
        /// The unicode value (code point) of the character.
        /// </summary>
        public uint unicode { get { return m_Unicode; } set { m_Unicode = value; } }

        /// <summary>
        /// The Text Asset to which this Text Element belongs.
        /// </summary>
        public TMP_FontAsset textAsset { get { return m_TextAsset; } set { m_TextAsset = value; } }

        /// <summary>
        /// The glyph used by this text element.
        /// </summary>
        public Glyph glyph { get { return m_Glyph; } set { m_Glyph = value; } }

        /// <summary>
        /// The index of the glyph used by this text element.
        /// </summary>
        public uint glyphIndex { get { return m_GlyphIndex; } set { m_GlyphIndex = value; } }

        // =============================================
        // Private backing fields for public properties.
        // =============================================

        [SerializeField]
        internal uint m_Unicode;

        internal TMP_FontAsset m_TextAsset;

        internal Glyph m_Glyph;

        [SerializeField]
        internal uint m_GlyphIndex;
    }
}
