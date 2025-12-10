using UnityEngine;
using UnityEngine.Serialization;
using System;


namespace TMPro
{
    // Structure which holds the font creation settings
    [Serializable]
    public struct FontAssetCreationSettings
    {
        public string sourceFontFileGUID;
        public int pointSizeSamplingMode;
        public int pointSize;
        public int padding;
        public int packingMode;
        public int atlasWidth;
        public int atlasHeight;
        public int characterSetSelectionMode;
        public string characterSequence;
        public string referencedFontAssetGUID;
        public string referencedTextAssetGUID;
        public int renderMode;
        public bool includeFontFeatures;

        internal FontAssetCreationSettings(string sourceFontFileGUID, int pointSize, int pointSizeSamplingMode, int padding, int packingMode, int atlasWidth, int atlasHeight, int characterSelectionMode, string characterSet, int renderMode)
        {
            this.sourceFontFileGUID = sourceFontFileGUID;
            this.pointSize = pointSize;
            this.pointSizeSamplingMode = pointSizeSamplingMode;
            this.padding = padding;
            this.packingMode = packingMode;
            this.atlasWidth = atlasWidth;
            this.atlasHeight = atlasHeight;
            this.characterSequence = characterSet;
            this.characterSetSelectionMode = characterSelectionMode;
            this.renderMode = renderMode;

            this.referencedFontAssetGUID = string.Empty;
            this.referencedTextAssetGUID = string.Empty;
            this.includeFontFeatures = false;
        }
    }

    /// <summary>
    /// Contains the font assets for the regular and italic styles associated with a given font weight.
    /// </summary>
    [Serializable]
    public struct TMP_FontWeightPair
    {
        public TMP_FontAsset regularTypeface;
        public TMP_FontAsset italicTypeface;
    }


    /// <summary>
    /// Positional adjustments of a glyph
    /// </summary>
    [Serializable]
    public struct GlyphValueRecord_Legacy
    {
        public float xPlacement;
        public float yPlacement;
        public float xAdvance;
        public float yAdvance;

        internal GlyphValueRecord_Legacy(UnityEngine.TextCore.LowLevel.GlyphValueRecord valueRecord)
        {
            this.xPlacement = valueRecord.xPlacement;
            this.yPlacement = valueRecord.yPlacement;
            this.xAdvance = valueRecord.xAdvance;
            this.yAdvance = valueRecord.yAdvance;
        }

        public static GlyphValueRecord_Legacy operator +(GlyphValueRecord_Legacy a, GlyphValueRecord_Legacy b)
        {
            GlyphValueRecord_Legacy c;
            c.xPlacement = a.xPlacement + b.xPlacement;
            c.yPlacement = a.yPlacement + b.yPlacement;
            c.xAdvance = a.xAdvance + b.xAdvance;
            c.yAdvance = a.yAdvance + b.yAdvance;

            return c;
        }
    }

    [Serializable]
    public class KerningPair
    {
        /// <summary>
        /// The first glyph part of a kerning pair.
        /// </summary>
        public uint firstGlyph
        {
            get { return m_FirstGlyph; }
            set { m_FirstGlyph = value; }
        }
        [FormerlySerializedAs("AscII_Left")]
        [SerializeField]
        private uint m_FirstGlyph;

        /// <summary>
        /// The positional adjustment of the first glyph.
        /// </summary>
        public GlyphValueRecord_Legacy firstGlyphAdjustments
        {
            get { return m_FirstGlyphAdjustments; }
        }
        [SerializeField]
        private GlyphValueRecord_Legacy m_FirstGlyphAdjustments;

        /// <summary>
        /// The second glyph part of a kerning pair.
        /// </summary>
        public uint secondGlyph
        {
            get { return m_SecondGlyph; }
            set { m_SecondGlyph = value; }
        }
        [FormerlySerializedAs("AscII_Right")]
        [SerializeField]
        private uint m_SecondGlyph;

        /// <summary>
        /// The positional adjustment of the second glyph.
        /// </summary>
        public GlyphValueRecord_Legacy secondGlyphAdjustments
        {
            get { return m_SecondGlyphAdjustments; }
        }
        [SerializeField]
        private GlyphValueRecord_Legacy m_SecondGlyphAdjustments;

        [FormerlySerializedAs("XadvanceOffset")]
        public float xOffset;

        public KerningPair()
        {
            m_FirstGlyph = 0;
            m_FirstGlyphAdjustments = new GlyphValueRecord_Legacy();

            m_SecondGlyph = 0;
            m_SecondGlyphAdjustments = new GlyphValueRecord_Legacy();
        }

        public KerningPair(uint left, uint right, float offset)
        {
            firstGlyph = left;
            m_SecondGlyph = right;
            xOffset = offset;
        }

        public KerningPair(uint firstGlyph, GlyphValueRecord_Legacy firstGlyphAdjustments, uint secondGlyph, GlyphValueRecord_Legacy secondGlyphAdjustments)
        {
            m_FirstGlyph = firstGlyph;
            m_FirstGlyphAdjustments = firstGlyphAdjustments;
            m_SecondGlyph = secondGlyph;
            m_SecondGlyphAdjustments = secondGlyphAdjustments;
        }

        internal void ConvertLegacyKerningData()
        {
            m_FirstGlyphAdjustments.xAdvance = xOffset;
        }
    }
}