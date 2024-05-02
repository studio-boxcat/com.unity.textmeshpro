using System;
using UnityEngine.TextCore;

namespace TMPro
{
    /// <summary>
    /// A basic element of text.
    /// </summary>
    [Serializable]
    public class TMP_Character : TMP_TextElement
    {
        public TMP_Character()
        {
        }

        /// <summary>
        /// Constructor for new character
        /// </summary>
        /// <param name="unicode">Unicode value.</param>
        /// <param name="glyph">Glyph</param>
        public TMP_Character(uint unicode, Glyph glyph)
        {
            this.unicode = unicode;
            this.textAsset = null;
            this.glyph = glyph;
            this.glyphIndex = glyph.index;
        }

        /// <summary>
        /// Constructor for new character
        /// </summary>
        /// <param name="unicode">Unicode value.</param>
        /// <param name="fontAsset">The font asset to which this character belongs.</param>
        /// <param name="glyph">Glyph</param>
        public TMP_Character(uint unicode, TMP_FontAsset fontAsset, Glyph glyph)
        {
            this.unicode = unicode;
            this.textAsset = fontAsset;
            this.glyph = glyph;
            this.glyphIndex = glyph.index;
        }

        /// <summary>
        /// Constructor for new character
        /// </summary>
        /// <param name="unicode">Unicode value.</param>
        /// <param name="glyphIndex">Glyph index.</param>
        internal TMP_Character(uint unicode, uint glyphIndex)
        {
            this.unicode = unicode;
            this.textAsset = null;
            this.glyph = null;
            this.glyphIndex = glyphIndex;
        }
    }
}
