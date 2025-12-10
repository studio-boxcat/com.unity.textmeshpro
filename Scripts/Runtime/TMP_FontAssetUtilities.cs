using System.Collections.Generic;


namespace TMPro
{
    public class TMP_FontAssetUtilities
    {
        /// <summary>
        /// HashSet containing instance ID of font assets already searched.
        /// </summary>
        private static HashSet<int> k_SearchedAssets;


        /// <summary>
        /// Returns the text element (character) for the given unicode value taking into consideration the requested font style and weight.
        /// Function searches the source font asset, its list of font assets assigned as alternative typefaces and potentially its fallbacks.
        /// The font asset out parameter contains a reference to the font asset containing the character.
        /// The typeface type indicates whether the returned font asset is the source font asset, an alternative typeface or fallback font asset.
        /// </summary>
        /// <param name="unicode">The unicode value of the requested character</param>
        /// <param name="sourceFontAsset">The font asset to be searched</param>
        /// <param name="includeFallbacks">Include the fallback font assets in the search</param>
        /// <param name="fontStyle">The font style</param>
        /// <param name="fontWeight">The font weight</param>
        /// <param name="isAlternativeTypeface">Indicates if the OUT font asset is an alternative typeface or fallback font asset</param>
        /// <param name="fontAsset">The font asset that contains the requested character</param>
        /// <returns></returns>
        public static TMP_Character GetCharacterFromFontAsset(uint unicode, TMP_FontAsset sourceFontAsset, bool includeFallbacks, FontStyles fontStyle, FontWeight fontWeight, out bool isAlternativeTypeface)
        {
            if (includeFallbacks)
            {
                if (k_SearchedAssets == null)
                    k_SearchedAssets = new HashSet<int>();
                else
                    k_SearchedAssets.Clear();
            }

            return GetCharacterFromFontAsset_Internal(unicode, sourceFontAsset, fontStyle, fontWeight, out isAlternativeTypeface);
        }


        /// <summary>
        /// Internal function returning the text element character for the given unicode value taking into consideration the font style and weight.
        /// Function searches the source font asset, list of font assets assigned as alternative typefaces and list of fallback font assets.
        /// </summary>
        private static TMP_Character GetCharacterFromFontAsset_Internal(uint unicode, TMP_FontAsset sourceFontAsset, FontStyles fontStyle, FontWeight fontWeight, out bool isAlternativeTypeface)
        {
            isAlternativeTypeface = false;
            TMP_Character character = null;

            #region FONT WEIGHT AND FONT STYLE HANDLING
            // Determine if a font weight or style is used. If so check if an alternative typeface is assigned for the given weight and / or style.
            bool isItalic = (fontStyle & FontStyles.Italic) == FontStyles.Italic;

            if (isItalic || fontWeight != FontWeight.Regular)
            {
                // Get reference to the font weight pairs of the given font asset.
                TMP_FontWeightPair[] fontWeights = sourceFontAsset.fontWeightTable;

                int fontWeightIndex = 4;
                switch (fontWeight)
                {
                    case FontWeight.Thin:
                        fontWeightIndex = 1;
                        break;
                    case FontWeight.ExtraLight:
                        fontWeightIndex = 2;
                        break;
                    case FontWeight.Light:
                        fontWeightIndex = 3;
                        break;
                    case FontWeight.Regular:
                        fontWeightIndex = 4;
                        break;
                    case FontWeight.Medium:
                        fontWeightIndex = 5;
                        break;
                    case FontWeight.SemiBold:
                        fontWeightIndex = 6;
                        break;
                    case FontWeight.Bold:
                        fontWeightIndex = 7;
                        break;
                    case FontWeight.Heavy:
                        fontWeightIndex = 8;
                        break;
                    case FontWeight.Black:
                        fontWeightIndex = 9;
                        break;
                }

                TMP_FontAsset temp = isItalic ? fontWeights[fontWeightIndex].italicTypeface : fontWeights[fontWeightIndex].regularTypeface;

                if (temp != null)
                {
                    if (temp.characterLookupTable.TryGetValue(unicode, out character))
                    {
                        isAlternativeTypeface = true;

                        return character;
                    }

                    if (temp.atlasPopulationMode == AtlasPopulationMode.Dynamic)
                    {
                        if (temp.TryAddCharacterInternal(unicode, out character))
                        {
                            isAlternativeTypeface = true;

                            return character;
                        }

                        // If we find the requested character, we add it to the font asset character table
                        // and return its character data.
                        // We also add this character to the list of characters we will need to add to the font atlas.
                        // We assume the font atlas has room otherwise this font asset should not be marked as dynamic.
                        // Alternatively, we could also add multiple pages of font atlas textures (feature consideration).
                    }

                    // At this point, we were not able to find the requested character in the alternative typeface
                    // so we check the source font asset and its potential fallbacks.
                }

            }
            #endregion

            // Search the source font asset for the requested character.
            if (sourceFontAsset.characterLookupTable.TryGetValue(unicode, out character))
                return character;

            if (sourceFontAsset.atlasPopulationMode == AtlasPopulationMode.Dynamic)
            {
                if (sourceFontAsset.TryAddCharacterInternal(unicode, out character))
                    return character;
            }

            return null;
        }


        /// <summary>
        /// Returns the text element (character) for the given unicode value taking into consideration the requested font style and weight.
        /// Function searches the provided list of font assets, the list of font assets assigned as alternative typefaces to them as well as their fallbacks.
        /// The font asset out parameter contains a reference to the font asset containing the character.
        /// The typeface type indicates whether the returned font asset is the source font asset, an alternative typeface or fallback font asset.
        /// </summary>
        /// <param name="unicode">The unicode value of the requested character</param>
        /// <param name="sourceFontAsset">The font asset originating the search query</param>
        /// <param name="fontAssets">The list of font assets to search</param>
        /// <param name="includeFallbacks">Determines if the fallback of each font assets on the list will be searched</param>
        /// <param name="fontStyle">The font style</param>
        /// <param name="fontWeight">The font weight</param>
        /// <param name="isAlternativeTypeface">Determines if the OUT font asset is an alternative typeface or fallback font asset</param>
        /// <returns></returns>
        public static TMP_Character GetCharacterFromFontAssets(uint unicode, TMP_FontAsset sourceFontAsset, List<TMP_FontAsset> fontAssets, bool includeFallbacks, FontStyles fontStyle, FontWeight fontWeight, out bool isAlternativeTypeface)
        {
            isAlternativeTypeface = false;

            // Make sure font asset list is valid
            if (fontAssets == null || fontAssets.Count == 0)
                return null;

            if (includeFallbacks)
            {
                if (k_SearchedAssets == null)
                    k_SearchedAssets = new HashSet<int>();
                else
                    k_SearchedAssets.Clear();
            }

            int fontAssetCount = fontAssets.Count;

            for (int i = 0; i < fontAssetCount; i++)
            {
                TMP_FontAsset fontAsset = fontAssets[i];

                if (fontAsset == null) continue;

                // Add reference to this search query
                sourceFontAsset.FallbackSearchQueryLookup.Add(fontAsset.instanceID);

                TMP_Character character = GetCharacterFromFontAsset_Internal(unicode, fontAsset, fontStyle, fontWeight, out isAlternativeTypeface);

                if (character != null)
                    return character;
            }

            return null;
        }
    }
}
