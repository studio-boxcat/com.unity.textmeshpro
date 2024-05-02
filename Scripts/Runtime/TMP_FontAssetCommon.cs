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
}