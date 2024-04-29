﻿using System;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;


namespace TMPro
{
    [Flags]
    public enum FontFeatureLookupFlags
    {
        None                        =     0x0,
        IgnoreLigatures             =   0x004,
        IgnoreSpacingAdjustments    =   0x100,
    }

    /// <summary>
    /// The values used to adjust the position of a glyph or set of glyphs.
    /// </summary>
    [Serializable]
    public struct TMP_GlyphValueRecord
    {
        /// <summary>
        /// The positional adjustment affecting the horizontal bearing X of the glyph.
        /// </summary>
        public float xPlacement { get { return m_XPlacement; } set { m_XPlacement = value; } }

        /// <summary>
        /// The positional adjustment affecting the horizontal bearing Y of the glyph.
        /// </summary>
        public float yPlacement { get { return m_YPlacement; } set { m_YPlacement = value; } }

        /// <summary>
        /// The positional adjustment affecting the horizontal advance of the glyph.
        /// </summary>
        public float xAdvance { get { return m_XAdvance; } set { m_XAdvance = value; } }

        /// <summary>
        /// The positional adjustment affecting the vertical advance of the glyph.
        /// </summary>
        public float yAdvance { get { return m_YAdvance; } set { m_YAdvance = value; } }

        // =============================================
        // Private backing fields for public properties.
        // =============================================

        [SerializeField]
        internal float m_XPlacement;

        [SerializeField]
        internal float m_YPlacement;

        [SerializeField]
        internal float m_XAdvance;

        [SerializeField]
        internal float m_YAdvance;


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="xPlacement">The positional adjustment affecting the horizontal bearing X of the glyph.</param>
        /// <param name="yPlacement">The positional adjustment affecting the horizontal bearing Y of the glyph.</param>
        /// <param name="xAdvance">The positional adjustment affecting the horizontal advance of the glyph.</param>
        /// <param name="yAdvance">The positional adjustment affecting the vertical advance of the glyph.</param>
        public TMP_GlyphValueRecord(float xPlacement, float yPlacement, float xAdvance, float yAdvance)
        {
            m_XPlacement = xPlacement;
            m_YPlacement = yPlacement;
            m_XAdvance = xAdvance;
            m_YAdvance = yAdvance;
        }

        internal TMP_GlyphValueRecord(GlyphValueRecord valueRecord)
        {
            m_XPlacement = valueRecord.xPlacement;
            m_YPlacement = valueRecord.yPlacement;
            m_XAdvance = valueRecord.xAdvance;
            m_YAdvance = valueRecord.yAdvance;
        }

        public static TMP_GlyphValueRecord operator +(TMP_GlyphValueRecord a, TMP_GlyphValueRecord b)
        {
            TMP_GlyphValueRecord c;
            c.m_XPlacement = a.xPlacement + b.xPlacement;
            c.m_YPlacement = a.yPlacement + b.yPlacement;
            c.m_XAdvance = a.xAdvance + b.xAdvance;
            c.m_YAdvance = a.yAdvance + b.yAdvance;

            return c;
        }
    }

    /// <summary>
    /// The positional adjustment values of a glyph.
    /// </summary>
    [Serializable]
    public struct TMP_GlyphAdjustmentRecord
    {
        /// <summary>
        /// The index of the glyph in the source font file.
        /// </summary>
        public uint glyphIndex { get { return m_GlyphIndex; } set { m_GlyphIndex = value; } }

        /// <summary>
        /// The GlyphValueRecord contains the positional adjustments of the glyph.
        /// </summary>
        public TMP_GlyphValueRecord glyphValueRecord { get { return m_GlyphValueRecord; } set { m_GlyphValueRecord = value; } }

        // =============================================
        // Private backing fields for public properties.
        // =============================================

        [SerializeField]
        internal uint m_GlyphIndex;

        [SerializeField]
        internal TMP_GlyphValueRecord m_GlyphValueRecord;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="glyphIndex">The index of the glyph in the source font file.</param>
        /// <param name="glyphValueRecord">The GlyphValueRecord contains the positional adjustments of the glyph.</param>
        public TMP_GlyphAdjustmentRecord(uint glyphIndex, TMP_GlyphValueRecord glyphValueRecord)
        {
            m_GlyphIndex = glyphIndex;
            m_GlyphValueRecord = glyphValueRecord;
        }

        internal TMP_GlyphAdjustmentRecord(GlyphAdjustmentRecord adjustmentRecord)
        {
            m_GlyphIndex = adjustmentRecord.glyphIndex;
            m_GlyphValueRecord = new TMP_GlyphValueRecord(adjustmentRecord.glyphValueRecord);
        }
    }
}
