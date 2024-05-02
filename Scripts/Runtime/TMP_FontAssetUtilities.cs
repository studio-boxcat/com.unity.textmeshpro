namespace TMPro
{
    public static class TMP_FontAssetUtilities
    {
        /// <summary>
        /// Returns the text element (character) for the given unicode value taking into consideration the requested font style and weight.
        /// Function searches the source font asset, its list of font assets assigned as alternative typefaces and potentially its fallbacks.
        /// The font asset out parameter contains a reference to the font asset containing the character.
        /// The typeface type indicates whether the returned font asset is the source font asset, an alternative typeface or fallback font asset.
        /// </summary>
        /// <param name="unicode">The unicode value of the requested character</param>
        /// <param name="fontAsset">The font asset to be searched</param>
        /// <returns></returns>
        public static TMP_Character GetCharacterFromFontAsset(uint unicode, TMP_FontAsset fontAsset)
        {
            // Search the source font asset for the requested character.
            if (fontAsset.characterLookupTable.TryGetValue(unicode, out var character))
                return character;

            if (fontAsset.atlasPopulationMode == AtlasPopulationMode.Dynamic)
            {
                if (fontAsset.TryAddCharacterInternal(unicode, out character))
                    return character;
            }

            return null;
        }

        public static TMP_Character GetCharacterFromFontAsset(uint unicode, uint fallback, TMP_FontAsset fontAsset)
        {
            var character = GetCharacterFromFontAsset(unicode, fontAsset);
            if (character is not null) return character;

            // Check for the missing glyph character in the currently assigned font asset and its fallbacks
            character = GetCharacterFromFontAsset(fallback, fontAsset);
            L.E("[TMP] Character with ASCII value of " + unicode + " was not found in the Font Asset Glyph Table. It was replaced by a Space (32) Glyph.");
            return character;
        }
    }
}