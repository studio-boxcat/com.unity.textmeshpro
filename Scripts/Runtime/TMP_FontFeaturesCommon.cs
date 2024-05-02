﻿using System;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;


namespace TMPro
{
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
}
