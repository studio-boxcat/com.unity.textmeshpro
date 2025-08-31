// ReSharper disable InconsistentNaming

using System;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using Unity.Profiling;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;

#if UNITY_EDITOR && UNITY_2018_4_OR_NEWER && !UNITY_2018_4_0 && !UNITY_2018_4_1 && !UNITY_2018_4_2 && !UNITY_2018_4_3 && !UNITY_2018_4_4
    using UnityEditor.TextCore.LowLevel;
#endif


namespace TMPro
{
    public enum AtlasPopulationMode
    {
        Static = 0x0,
        Dynamic = 0x1,
    }


    [Serializable][ExcludeFromPreset]
    public class TMP_FontAsset : ScriptableObject
    {
        /// <summary>
        /// Instance ID of the TMP Asset
        /// </summary>
        public int instanceID => GetInstanceID();

        /// <summary>
        /// The material used by this asset.
        /// </summary>
        [Required]
        public Material material;

        /// <summary>
        /// This field is set when the font asset is first created.
        /// </summary>
        [SerializeField]
        internal string m_SourceFontFileGUID;

        #if UNITY_EDITOR
        /// <summary>
        /// Persistent reference to the source font file maintained in the editor.
        /// </summary>
        [SerializeField]
        internal Font m_SourceFontFile_EditorRef;
        #endif

        /// <summary>
        /// Source font file when atlas population mode is set to dynamic. Null when the atlas population mode is set to static.
        /// </summary>
        public Font sourceFontFile
        {
            get => m_SourceFontFile;
            internal set => m_SourceFontFile = value;
        }
        [SerializeField]
        private Font m_SourceFontFile;

        public AtlasPopulationMode atlasPopulationMode
        {
            get { return m_AtlasPopulationMode; }

            set
            {
                m_AtlasPopulationMode = value;

                #if UNITY_EDITOR
                if (m_AtlasPopulationMode == AtlasPopulationMode.Static)
                    m_SourceFontFile = null;
                else if (m_AtlasPopulationMode == AtlasPopulationMode.Dynamic)
                    m_SourceFontFile = m_SourceFontFile_EditorRef;
                #endif
            }
        }
        [SerializeField]
        private AtlasPopulationMode m_AtlasPopulationMode;


        /// <summary>
        /// Information about the font's face.
        /// </summary>
        public FaceInfo faceInfo
        {
            get => m_FaceInfo;
            set => m_FaceInfo = value;
        }
        [SerializeField]
        internal FaceInfo m_FaceInfo;


        /// <summary>
        /// List of glyphs contained in the font asset.
        /// </summary>
        public List<Glyph> glyphTable
        {
            get => m_GlyphTable;
            internal set => m_GlyphTable = value;
        }
        [SerializeField]
        internal List<Glyph> m_GlyphTable = new();

        /// <summary>
        /// Dictionary used to lookup glyphs contained in the font asset by their index.
        /// </summary>
        public Dictionary<uint, Glyph> glyphLookupTable
        {
            get
            {
                if (m_GlyphLookupDictionary == null)
                    ReadFontAssetDefinition();

                return m_GlyphLookupDictionary;
            }
        }
        internal Dictionary<uint, Glyph> m_GlyphLookupDictionary;


        /// <summary>
        /// List containing the characters of the given font asset.
        /// </summary>
        public List<TMP_Character> characterTable
        {
            get { return m_CharacterTable; }
            internal set { m_CharacterTable = value; }
        }
        [SerializeField]
        internal List<TMP_Character> m_CharacterTable = new();

        /// <summary>
        /// Dictionary used to lookup characters contained in the font asset by their unicode values.
        /// </summary>
        public Dictionary<uint, TMP_Character> characterLookupTable
        {
            get
            {
                if (m_CharacterLookupDictionary == null)
                    ReadFontAssetDefinition();
                return m_CharacterLookupDictionary;
            }
        }
        internal Dictionary<uint, TMP_Character> m_CharacterLookupDictionary;


        /// <summary>
        /// The font atlas used by this font asset.
        /// This is always the texture at index [0] of the fontAtlasTextures.
        /// </summary>
        public Texture2D atlasTexture
        {
            get
            {
                if (m_AtlasTexture == null)
                    m_AtlasTexture = atlasTextures[0];
                return m_AtlasTexture;
            }
        }
        internal Texture2D m_AtlasTexture;

        /// <summary>
        /// Array of atlas textures that contain the glyphs used by this font asset.
        /// </summary>
        public Texture2D[] atlasTextures
        {
            get => m_AtlasTextures;
            set => m_AtlasTextures = value;
        }
        [SerializeField]
        internal Texture2D[] m_AtlasTextures;

        /// <summary>
        /// Index of the font atlas texture that still has available space to add new glyphs.
        /// </summary>
        [SerializeField]
        internal int m_AtlasTextureIndex;

        /// <summary>
        /// Enables the font asset to create additional atlas textures as needed.
        /// </summary>
        public bool isMultiAtlasTexturesEnabled
        {
            get { return m_IsMultiAtlasTexturesEnabled; }
            set { m_IsMultiAtlasTexturesEnabled = value; }
        }

        [SerializeField]
        private bool m_IsMultiAtlasTexturesEnabled;

        /// <summary>
        /// Determines if dynamic font asset data should be cleared before builds.
        /// </summary>
        internal bool clearDynamicDataOnBuild
        {
            get { return m_ClearDynamicDataOnBuild; }
            set { m_ClearDynamicDataOnBuild = value; }
        }
        [SerializeField]
        private bool m_ClearDynamicDataOnBuild;

        /// <summary>
        /// List of spaces occupied by glyphs in a given texture.
        /// </summary>
        internal List<GlyphRect> usedGlyphRects
        {
            get { return m_UsedGlyphRects; }
            set { m_UsedGlyphRects = value; }
        }
        [SerializeField]
        private List<GlyphRect> m_UsedGlyphRects;

        /// <summary>
        /// List of spaces available in a given texture to add new glyphs.
        /// </summary>
        internal List<GlyphRect> freeGlyphRects
        {
            get { return m_FreeGlyphRects; }
            set { m_FreeGlyphRects = value; }
        }
        [SerializeField]
        private List<GlyphRect> m_FreeGlyphRects;

        /// <summary>
        ///
        /// </summary>
        [SerializeField]
        public Texture2D atlas; // Should add a property to make this read-only.

        /// <summary>
        /// The width of the atlas texture(s) used by this font asset.
        /// </summary>
        public int atlasWidth
        {
            get { return m_AtlasWidth; }
            internal set { m_AtlasWidth = value; }
        }
        [SerializeField]
        internal int m_AtlasWidth;

        /// <summary>
        /// The height of the atlas texture(s) used by this font asset.
        /// </summary>
        public int atlasHeight
        {
            get { return m_AtlasHeight; }
            internal set { m_AtlasHeight = value; }
        }
        [SerializeField]
        internal int m_AtlasHeight;

        /// <summary>
        /// The padding used between glyphs contained in the atlas texture(s) used by this font asset.
        /// </summary>
        public int atlasPadding
        {
            get { return m_AtlasPadding; }
            internal set { m_AtlasPadding = value; }
        }
        [SerializeField]
        internal int m_AtlasPadding;

        public GlyphRenderMode atlasRenderMode
        {
            get { return m_AtlasRenderMode; }
            internal set { m_AtlasRenderMode = value; }
        }
        [SerializeField]
        internal GlyphRenderMode m_AtlasRenderMode;

        /// <summary>
        /// The settings used in the Font Asset Creator when this font asset was created or edited.
        /// </summary>
        public FontAssetCreationSettings creationSettings
        {
            get { return m_CreationSettings; }
            set { m_CreationSettings = value; }
        }
        [SerializeField]
        internal FontAssetCreationSettings m_CreationSettings;

        /// <summary>
        /// Defines the dilation of the text when using regular style.
        /// </summary>
        public float normalStyle = 0;

        /// <summary>
        /// The spacing between characters when using regular style.
        /// </summary>
        public float normalSpacingOffset = 0;

        /// <summary>
        /// Defines the dilation of the text when using bold style.
        /// </summary>
        public float boldStyle = 0.75f;

        /// <summary>
        /// The spacing between characters when using regular style.
        /// </summary>
        public float boldSpacing = 7f;

        /// <summary>
        /// Defines the slant of the text when using italic style.
        /// </summary>
        public byte italicStyle = 35;

        public byte tabSize = 10;

        internal bool IsFontAssetLookupTablesDirty;

        // Profiler Marker declarations
        private static ProfilerMarker k_ReadFontAssetDefinitionMarker = new ProfilerMarker("TMP.ReadFontAssetDefinition");
        private static ProfilerMarker k_AddSynthesizedCharactersMarker = new ProfilerMarker("TMP.AddSynthesizedCharacters");
        private static ProfilerMarker k_TryAddCharacterMarker = new ProfilerMarker("TMP.TryAddCharacter");
        private static ProfilerMarker k_TryAddCharactersMarker = new ProfilerMarker("TMP.TryAddCharacters");
        private static ProfilerMarker k_ClearFontAssetDataMarker = new ProfilerMarker("TMP.ClearFontAssetData");
        private static ProfilerMarker k_UpdateFontAssetDataMarker = new ProfilerMarker("TMP.UpdateFontAssetData");

        /// <summary>
        /// Create new instance of a font asset using default settings.
        /// </summary>
        /// <param name="font"></param>
        /// <returns></returns>
        public static TMP_FontAsset CreateFontAsset(Font font)
        {
            return CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
        }

        /// <summary>
        /// Create new instance of a font asset.
        /// </summary>
        /// <param name="font">The source font file.</param>
        /// <param name="samplingPointSize">The sampling point size.</param>
        /// <param name="atlasPadding">The padding / spread between individual glyphs in the font asset.</param>
        /// <param name="renderMode"></param>
        /// <param name="atlasWidth">The atlas texture width.</param>
        /// <param name="atlasHeight">The atlas texture height.</param>
        /// <param name="atlasPopulationMode"></param>
        /// <returns></returns>
        public static TMP_FontAsset CreateFontAsset(Font font, int samplingPointSize, int atlasPadding, GlyphRenderMode renderMode, int atlasWidth, int atlasHeight, AtlasPopulationMode atlasPopulationMode = AtlasPopulationMode.Dynamic, bool enableMultiAtlasSupport = true)
        {
            // Initialize FontEngine
            FontEngine.InitializeFontEngine();

            // Load Font Face
            if (FontEngine.LoadFontFace(font, samplingPointSize) != FontEngineError.Success)
            {
                Debug.LogWarning("Unable to load font face for [" + font.name + "]. Make sure \"Include Font Data\" is enabled in the Font Import Settings.", font);
                return null;
            }

            // Create new font asset
            var fontAsset = CreateInstance<TMP_FontAsset>();
            fontAsset.faceInfo = FontEngine.GetFaceInfo();

            // Set font reference and GUID
            if (atlasPopulationMode == AtlasPopulationMode.Dynamic)
                fontAsset.sourceFontFile = font;

            // Set persistent reference to source font file in the Editor only.
            #if UNITY_EDITOR
            UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(font, out var guid, out long _);
            fontAsset.m_SourceFontFileGUID = guid;
            fontAsset.m_SourceFontFile_EditorRef = font;
            #endif

            fontAsset.atlasPopulationMode = atlasPopulationMode;

            fontAsset.atlasWidth = atlasWidth;
            fontAsset.atlasHeight = atlasHeight;
            fontAsset.atlasPadding = atlasPadding;
            fontAsset.atlasRenderMode = renderMode;

            // Initialize array for the font atlas textures.
            fontAsset.atlasTextures = new Texture2D[1];

            // Create and add font atlas texture.
            Texture2D texture = new Texture2D(0, 0, TextureFormat.Alpha8, false);
            fontAsset.atlasTextures[0] = texture;

            fontAsset.isMultiAtlasTexturesEnabled = enableMultiAtlasSupport;

            // Add free rectangle of the size of the texture.
            int packingModifier;
            if (((GlyphRasterModes)renderMode & GlyphRasterModes.RASTER_MODE_BITMAP) == GlyphRasterModes.RASTER_MODE_BITMAP)
            {
                throw new NotSupportedException("Bitmap font atlases are no longer supported.");
            }
            else
            {
                packingModifier = 1;

                // Optimize by adding static ref to shader.
                Material tmp_material = new Material(ShaderUtilities.ShaderRef_MobileSDF);

                //tmp_material.name = texture.name + " Material";
                tmp_material.SetTexture(ShaderUtilities.ID_MainTex, texture);
                tmp_material.SetFloat(ShaderUtilities.ID_TextureWidth, atlasWidth);
                tmp_material.SetFloat(ShaderUtilities.ID_TextureHeight, atlasHeight);

                tmp_material.SetFloat(ShaderUtilities.ID_GradientScale, atlasPadding + packingModifier);

                tmp_material.SetFloat(ShaderUtilities.ID_WeightNormal, fontAsset.normalStyle);
                tmp_material.SetFloat(ShaderUtilities.ID_WeightBold, fontAsset.boldStyle);

                fontAsset.material = tmp_material;
            }

            fontAsset.freeGlyphRects = new List<GlyphRect>(8) { new GlyphRect(0, 0, atlasWidth - packingModifier, atlasHeight - packingModifier) };
            fontAsset.usedGlyphRects = new List<GlyphRect>(8);

            // TODO: Consider adding support for extracting glyph positioning data

            fontAsset.ReadFontAssetDefinition();

            return fontAsset;
        }


        #if UNITY_EDITOR
        private void OnValidate()
        {
            // Make sure our lookup dictionary have been initialized.
            if (m_CharacterLookupDictionary == null || m_GlyphLookupDictionary == null)
                ReadFontAssetDefinition();
        }
        #endif

        public void ReadFontAssetDefinition()
        {
            k_ReadFontAssetDefinitionMarker.Begin();

            // Initialize lookup tables for characters and glyphs.
            InitializeDictionaryLookupTables();

            // Add synthesized characters and adjust face metrics
            AddSynthesizedCharactersAndFaceMetrics();

            // Adjust Font Scale for compatibility reasons
            if (m_FaceInfo.scale == 0)
                m_FaceInfo.scale = 1.0f;

            // Set Strikethrough Offset (if needed)
            if (m_FaceInfo.strikethroughOffset == 0)
                m_FaceInfo.strikethroughOffset = m_FaceInfo.capLine / 2.5f;

            // Set Padding value for legacy font assets.
            if (m_AtlasPadding == 0)
            {
                if (material.HasProperty(ShaderUtilities.ID_GradientScale))
                    m_AtlasPadding = (int)material.GetFloat(ShaderUtilities.ID_GradientScale) - 1;
            }

            IsFontAssetLookupTablesDirty = false;

            k_ReadFontAssetDefinitionMarker.End();
        }


        /// <summary>
        /// Read the various data tables of the font asset to populate its different dictionaries to allow for faster lookup of related font asset data.
        /// </summary>
        internal void InitializeDictionaryLookupTables()
        {
            // Initialize and populate glyph lookup dictionary
            InitializeGlyphLookupDictionary();

            // Initialize and populate character lookup dictionary
            InitializeCharacterLookupDictionary();
        }

        internal void InitializeGlyphLookupDictionary()
        {
            // Create new instance of the glyph lookup dictionary or clear the existing one.
            if (m_GlyphLookupDictionary == null)
                m_GlyphLookupDictionary = new Dictionary<uint, Glyph>(m_GlyphTable.Count);
            else
                m_GlyphLookupDictionary.Clear();

            // Initialize or clear list of glyph indexes.
            if (m_GlyphIndexList == null)
                m_GlyphIndexList = new List<uint>();
            else
                m_GlyphIndexList.Clear();

            // Initialize or clear list of glyph indexes.
            if (m_GlyphIndexListNewlyAdded == null)
                m_GlyphIndexListNewlyAdded = new List<uint>();
            else
                m_GlyphIndexListNewlyAdded.Clear();

            //
            int glyphCount = m_GlyphTable.Count;

            // Add glyphs contained in the glyph table to dictionary for faster lookup.
            for (int i = 0; i < glyphCount; i++)
            {
                Glyph glyph = m_GlyphTable[i];

                uint index = glyph.index;

                // TODO: Not sure it is necessary to check here.
                if (m_GlyphLookupDictionary.TryAdd(index, glyph))
                    m_GlyphIndexList.Add(index);
            }
        }

        internal void InitializeCharacterLookupDictionary()
        {
            // Create new instance of the character lookup dictionary or clear the existing one.
            if (m_CharacterLookupDictionary == null)
                m_CharacterLookupDictionary = new Dictionary<uint, TMP_Character>(m_CharacterTable.Count);
            else
                m_CharacterLookupDictionary.Clear();

            // Add the characters contained in the character table to the dictionary for faster lookup.
            for (int i = 0; i < m_CharacterTable.Count; i++)
            {
                TMP_Character character = m_CharacterTable[i];

                uint unicode = character.unicode;
                uint glyphIndex = character.glyphIndex;

                // Add character along with reference to text asset and glyph
                if (m_CharacterLookupDictionary.ContainsKey(unicode) == false)
                {
                    m_CharacterLookupDictionary.Add(unicode, character);
                    character.textAsset = this;
                    character.glyph = m_GlyphLookupDictionary[glyphIndex];
                }
            }

            // Clear internal fallback references
            if (FallbackSearchQueryLookup == null)
                FallbackSearchQueryLookup = new HashSet<int>();
            else
                FallbackSearchQueryLookup.Clear();
        }

        internal void AddSynthesizedCharactersAndFaceMetrics()
        {
            k_AddSynthesizedCharactersMarker.Begin();

            bool isFontFaceLoaded = false;

            if (m_AtlasPopulationMode == AtlasPopulationMode.Dynamic)
                isFontFaceLoaded = FontEngine.LoadFontFace(sourceFontFile, m_FaceInfo.pointSize) == FontEngineError.Success;

            // Only characters not present in the source font file will be synthesized.

            // Non visible and control characters with no metrics
            // Add End of Text \u0003
            AddSynthesizedCharacter(0x03, isFontFaceLoaded, true);

            // Add Tab \u0009
            AddSynthesizedCharacter(0x09, isFontFaceLoaded, true);

            // Add Line Feed (LF) \u000A
            AddSynthesizedCharacter(0x0A, isFontFaceLoaded);

            // Add Vertical Tab (VT) \u000B
            AddSynthesizedCharacter(0x0B, isFontFaceLoaded);

            // Add Carriage Return (CR) \u000D
            AddSynthesizedCharacter(0x0D, isFontFaceLoaded);

            // Add Arabic Letter Mark \u061C
            AddSynthesizedCharacter(0x061C, isFontFaceLoaded);

            // Add Zero Width Space <ZWSP> \u2000B
            AddSynthesizedCharacter(0x200B, isFontFaceLoaded);

            // Add Left-To-Right Mark \u200E
            AddSynthesizedCharacter(0x200E, isFontFaceLoaded);

            // Add Right-To-Left Mark \u200F
            AddSynthesizedCharacter(0x200F, isFontFaceLoaded);

            // Add Line Separator \u2028
            AddSynthesizedCharacter(0x2028, isFontFaceLoaded);

            // Add Paragraph Separator \u2029
            AddSynthesizedCharacter(0x2029, isFontFaceLoaded);

            // Add Word Joiner <WJ> / Zero Width Non-Breaking Space \u2060
            AddSynthesizedCharacter(0x2060, isFontFaceLoaded);

            // Set Cap Line using the capital letter 'X'
            if (m_FaceInfo.capLine == 0 && m_CharacterLookupDictionary.ContainsKey('X'))
            {
                uint glyphIndex = m_CharacterLookupDictionary['X'].glyphIndex;
                m_FaceInfo.capLine = m_GlyphLookupDictionary[glyphIndex].metrics.horizontalBearingY;
            }

            // Set Mean Line using the lowercase letter 'x'
            if (m_FaceInfo.meanLine == 0 && m_CharacterLookupDictionary.ContainsKey('x'))
            {
                uint glyphIndex = m_CharacterLookupDictionary['x'].glyphIndex;
                m_FaceInfo.meanLine = m_GlyphLookupDictionary[glyphIndex].metrics.horizontalBearingY;
            }

            k_AddSynthesizedCharactersMarker.End();
        }

        void AddSynthesizedCharacter(uint unicode, bool isFontFaceLoaded, bool addImmediately = false)
        {
            // Check if unicode is already present in the font asset
            if (m_CharacterLookupDictionary.ContainsKey(unicode))
                return;

            Glyph glyph;

            if (isFontFaceLoaded)
            {
                // Check if unicode is present in font file
                if (FontEngine.GetGlyphIndex(unicode) != 0)
                {
                    if (addImmediately == false)
                        return;

                    //Debug.Log("Adding Unicode [" + unicode.ToString("X4") + "].");

                    var glyphLoadFlags = ((GlyphRasterModes)m_AtlasRenderMode & GlyphRasterModes.RASTER_MODE_NO_HINTING) == GlyphRasterModes.RASTER_MODE_NO_HINTING
                        ? GlyphLoadFlags.LOAD_NO_BITMAP | GlyphLoadFlags.LOAD_NO_HINTING
                        : GlyphLoadFlags.LOAD_NO_BITMAP;

                    if (FontEngine.TryGetGlyphWithUnicodeValue(unicode, glyphLoadFlags, out glyph))
                        m_CharacterLookupDictionary.Add(unicode, new TMP_Character(unicode, this, glyph));

                    return;
                }
            }

            //Debug.Log("Synthesizing Unicode [" + unicode.ToString("X4") + "].");

            // Synthesize and add missing glyph and character
            glyph = new Glyph(0, new GlyphMetrics(0, 0, 0, 0, 0), GlyphRect.zero, 1.0f, 0);
            m_CharacterLookupDictionary.Add(unicode, new TMP_Character(unicode, this, glyph));
        }

        internal HashSet<int> FallbackSearchQueryLookup = new HashSet<int>();

        internal void AddCharacterToLookupCache(uint unicode, TMP_Character character)
        {
            m_CharacterLookupDictionary.Add(unicode, character);

            // Add font asset to fallback references.
            FallbackSearchQueryLookup.Add(character.textAsset.instanceID);
        }

        /// <summary>
        /// Sort the Character table by Unicode values.
        /// </summary>
        internal void SortCharacterTable()
        {
            if (m_CharacterTable != null && m_CharacterTable.Count > 0)
                m_CharacterTable = m_CharacterTable.OrderBy(c => c.unicode).ToList();
        }

        /// <summary>
        /// Sort the Glyph table by index values.
        /// </summary>
        internal void SortGlyphTable()
        {
            if (m_GlyphTable != null && m_GlyphTable.Count > 0)
                m_GlyphTable = m_GlyphTable.OrderBy(c => c.index).ToList();
        }

        /// <summary>
        /// Sort both glyph and character tables.
        /// </summary>
        internal void SortAllTables()
        {
            SortGlyphTable();
            SortCharacterTable();
        }

        /// <summary>
        /// HashSet of font asset instance ID used in the process of searching for through fallback font assets for a given character or characters.
        /// </summary>
        private static HashSet<int> k_SearchedFontAssetLookup;

        /// <summary>
        /// Function to extract all the characters from a font asset.
        /// </summary>
        /// <param name="fontAsset"></param>
        /// <returns></returns>
        public static string GetCharacters(TMP_FontAsset fontAsset)
        {
            string characters = string.Empty;
            for (int i = 0; i < fontAsset.characterTable.Count; i++)
                characters += (char)fontAsset.characterTable[i].unicode;
            return characters;
        }


        /// <summary>
        /// Function which returns an array that contains all the characters from a font asset.
        /// </summary>
        /// <param name="fontAsset"></param>
        /// <returns></returns>
        public static int[] GetCharactersArray(TMP_FontAsset fontAsset)
        {
            int[] characters = new int[fontAsset.characterTable.Count];

            for (int i = 0; i < fontAsset.characterTable.Count; i++)
            {
                characters[i] = (int)fontAsset.characterTable[i].unicode;
            }

            return characters;
        }

        /// <summary>
        /// Internal function used to get the glyph index for the given Unicode.
        /// </summary>
        /// <param name="unicode"></param>
        /// <returns></returns>
        internal uint GetGlyphIndex(uint unicode)
        {
            // Check if glyph already exists in font asset.
            if (m_CharacterLookupDictionary.ContainsKey(unicode))
                return m_CharacterLookupDictionary[unicode].glyphIndex;

            // Load font face.
            if (FontEngine.LoadFontFace(sourceFontFile, m_FaceInfo.pointSize) != FontEngineError.Success)
                return 0;

            return FontEngine.GetGlyphIndex(unicode);
        }

        // ================================================================================
        // Properties and functions related to character and glyph additions as well as
        // tacking glyphs that need to be added to various font asset atlas textures.
        // ================================================================================

        /// <summary>
        /// List of glyphs that need to be rendered and added to an atlas texture.
        /// </summary>
        private List<Glyph> m_GlyphsToRender = new List<Glyph>();

        /// <summary>
        /// List of glyphs that we just rendered and added to an atlas texture.
        /// </summary>
        private List<Glyph> m_GlyphsRendered = new List<Glyph>();

        /// <summary>
        /// List of all the glyph indexes contained in the font asset.
        /// </summary>
        private List<uint> m_GlyphIndexList = new List<uint>();

        /// <summary>
        /// List of glyph indexes newly added to the font asset.
        /// This list is used in the process of retrieving font features.
        /// </summary>
        private List<uint> m_GlyphIndexListNewlyAdded = new List<uint>();

        /// <summary>
        ///
        /// </summary>
        internal List<uint> m_GlyphsToAdd = new List<uint>();
        internal HashSet<uint> m_GlyphsToAddLookup = new HashSet<uint>();

        internal List<TMP_Character> m_CharactersToAdd = new List<TMP_Character>();
        internal HashSet<uint> m_CharactersToAddLookup = new HashSet<uint>();

        /// <summary>
        /// Internal list used to track characters that could not be added to the font asset.
        /// </summary>
        internal List<uint> s_MissingCharacterList = new List<uint>();

        /// <summary>
        /// Hash table used to track characters that are known to be missing from the font file.
        /// </summary>
        internal HashSet<uint> m_MissingUnicodesFromFontFile = new HashSet<uint>();

        /// <summary>
        /// Internal static array used to avoid allocations when using the GetGlyphPairAdjustmentTable().
        /// </summary>
        internal static uint[] k_GlyphIndexArray;

        /// <summary>
        /// Try adding the characters from the provided string to the font asset.
        /// </summary>
        /// <param name="unicodes">Array that contains the characters to add to the font asset.</param>
        /// <returns>Returns true if all the characters were successfully added to the font asset. Return false otherwise.</returns>
        public bool TryAddCharacters(uint[] unicodes)
        {
            k_TryAddCharactersMarker.Begin();

            // Make sure font asset is set to dynamic and that we have a valid list of characters.
            if (unicodes == null || unicodes.Length == 0 || m_AtlasPopulationMode == AtlasPopulationMode.Static)
            {
                if (m_AtlasPopulationMode == AtlasPopulationMode.Static)
                    Debug.LogWarning("Unable to add characters to font asset [" + this.name + "] because its AtlasPopulationMode is set to Static.", this);
                else
                    Debug.LogWarning("Unable to add characters to font asset [" + this.name + "] because the provided Unicode list is Null or Empty.", this);

                k_TryAddCharactersMarker.End();
                return false;
            }

            // Load font face.
            if (FontEngine.LoadFontFace(m_SourceFontFile, m_FaceInfo.pointSize) != FontEngineError.Success)
            {
                k_TryAddCharactersMarker.End();
                return false;
            }

            // Make sure font asset has been initialized
            if (m_CharacterLookupDictionary == null || m_GlyphLookupDictionary == null)
                ReadFontAssetDefinition();

            // Clear lists used to track which character and glyphs were added or missing.
            m_GlyphsToAdd.Clear();
            m_GlyphsToAddLookup.Clear();
            m_CharactersToAdd.Clear();
            m_CharactersToAddLookup.Clear();
            s_MissingCharacterList.Clear();

            bool isMissingCharacters = false;
            int unicodeCount = unicodes.Length;

            for (int i = 0; i < unicodeCount; i++)
            {
                uint unicode = unicodes[i];

                // Check if character is already contained in the character table.
                if (m_CharacterLookupDictionary.ContainsKey(unicode))
                    continue;

                // Get the index of the glyph for this Unicode value.
                uint glyphIndex = FontEngine.GetGlyphIndex(unicode);

                // Skip missing glyphs
                if (glyphIndex == 0)
                {
                    // Special handling for characters with potential alternative glyph representations
                    switch (unicode)
                    {
                        case 0xA0: // Non Breaking Space <NBSP>
                            // Use Space
                            glyphIndex = FontEngine.GetGlyphIndex(0x20);
                            break;
                        case 0xAD: // Soft Hyphen <SHY>
                        case 0x2011: // Non Breaking Hyphen
                            // Use Hyphen Minus
                            glyphIndex = FontEngine.GetGlyphIndex(0x2D);
                            break;
                    }

                    // Skip to next character if no potential alternative glyph representation is present in font file.
                    if (glyphIndex == 0)
                    {
                        // Add character to list of missing characters.
                        s_MissingCharacterList.Add(unicode);

                        isMissingCharacters = true;
                        continue;
                    }
                }

                TMP_Character character = new TMP_Character(unicode, glyphIndex);

                // Check if glyph is already contained in the font asset as the same glyph might be referenced by multiple characters.
                if (m_GlyphLookupDictionary.TryGetValue(glyphIndex, out var glyph))
                {
                    // Add a reference to the source text asset and glyph
                    character.glyph = glyph;
                    character.textAsset = this;

                    m_CharacterTable.Add(character);
                    m_CharacterLookupDictionary.Add(unicode, character);
                    continue;
                }

                // Make sure glyph index has not already been added to list of glyphs to add.
                if (m_GlyphsToAddLookup.Add(glyphIndex))
                    m_GlyphsToAdd.Add(glyphIndex);

                // Make sure unicode / character has not already been added.
                if (m_CharactersToAddLookup.Add(unicode))
                    m_CharactersToAdd.Add(character);
            }

            if (m_GlyphsToAdd.Count == 0)
            {
                //Debug.LogWarning("No characters will be added to font asset [" + this.name + "] either because they are already present in the font asset or missing from the font file.");
                k_TryAddCharactersMarker.End();
                return false;
            }

            // Resize the Atlas Texture to the appropriate size
            if (m_AtlasTextures[m_AtlasTextureIndex].width == 0 || m_AtlasTextures[m_AtlasTextureIndex].height == 0)
            {
                m_AtlasTextures[m_AtlasTextureIndex].Reinitialize(m_AtlasWidth, m_AtlasHeight);
                FontEngine.ResetAtlasTexture(m_AtlasTextures[m_AtlasTextureIndex]);
            }

            Glyph[] glyphs;
            bool allGlyphsAddedToTexture = FontEngine.TryAddGlyphsToTexture(m_GlyphsToAdd, m_AtlasPadding, GlyphPackingMode.BestShortSideFit, m_FreeGlyphRects, m_UsedGlyphRects, m_AtlasRenderMode, m_AtlasTextures[m_AtlasTextureIndex], out glyphs);

            // Add new glyphs to relevant font asset data structure
            for (int i = 0; i < glyphs.Length && glyphs[i] != null; i++)
            {
                Glyph glyph = glyphs[i];
                uint glyphIndex = glyph.index;

                glyph.atlasIndex = m_AtlasTextureIndex;

                // Add new glyph to glyph table.
                m_GlyphTable.Add(glyph);
                m_GlyphLookupDictionary.Add(glyphIndex, glyph);

                m_GlyphIndexListNewlyAdded.Add(glyphIndex);
                m_GlyphIndexList.Add(glyphIndex);
            }

            // Clear glyph index list to allow
            m_GlyphsToAdd.Clear();

            // Add new characters to relevant data structures as well as track glyphs that could not be added to the current atlas texture.
            for (int i = 0; i < m_CharactersToAdd.Count; i++)
            {
                TMP_Character character = m_CharactersToAdd[i];
                Glyph glyph;

                if (m_GlyphLookupDictionary.TryGetValue(character.glyphIndex, out glyph) == false)
                {
                    m_GlyphsToAdd.Add(character.glyphIndex);
                    continue;
                }

                // Add a reference to the source text asset and glyph
                character.glyph = glyph;
                character.textAsset = this;

                m_CharacterTable.Add(character);
                m_CharacterLookupDictionary.Add(character.unicode, character);

                // Remove character from list to add
                m_CharactersToAdd.RemoveAt(i);
                i -= 1;
            }

            // Try adding missing glyphs to
            if (m_IsMultiAtlasTexturesEnabled && allGlyphsAddedToTexture == false)
            {
                while (allGlyphsAddedToTexture == false)
                    allGlyphsAddedToTexture = TryAddGlyphsToNewAtlasTexture();
            }

            #if UNITY_EDITOR
            // Makes the changes to the font asset persistent.
            if (UnityEditor.EditorUtility.IsPersistent(this))
            {
                TMP_EditorResourceManager.RegisterResourceForUpdate(this);
            }
            #endif

            // Populate list of missing characters
            for (int i = 0; i < m_CharactersToAdd.Count; i++)
            {
                TMP_Character character = m_CharactersToAdd[i];
                s_MissingCharacterList.Add(character.unicode);
            }

            if (s_MissingCharacterList.Count > 0)
                s_MissingCharacterList.ToArray();

            k_TryAddCharactersMarker.End();

            return allGlyphsAddedToTexture && !isMissingCharacters;
        }

        /// <summary>
        /// Try adding character using Unicode value to font asset.
        /// Function assumes internal user has already checked to make sure the character is not already contained in the font asset.
        /// </summary>
        /// <param name="unicode">The Unicode value of the character.</param>
        /// <param name="character">The character data if successfully added to the font asset. Null otherwise.</param>
        /// <returns>Returns true if the character has been added. False otherwise.</returns>
        internal bool TryAddCharacterInternal(uint unicode, out TMP_Character character)
        {
            k_TryAddCharacterMarker.Begin();

            character = null;

            // Check if the Unicode character is already known to be missing from the source font file.
            if (m_MissingUnicodesFromFontFile.Contains(unicode))
            {
                k_TryAddCharacterMarker.End();
                return false;
            }

            // Load font face.
            if (FontEngine.LoadFontFace(sourceFontFile, m_FaceInfo.pointSize) != FontEngineError.Success)
            {
                k_TryAddCharacterMarker.End();
                return false;
            }

            uint glyphIndex = FontEngine.GetGlyphIndex(unicode);
            if (glyphIndex == 0)
            {
                // Special handling for characters with potential alternative glyph representations
                switch (unicode)
                {
                    case 0xA0: // Non Breaking Space <NBSP>
                        // Use Space
                        glyphIndex = FontEngine.GetGlyphIndex(0x20);
                        break;
                    case 0xAD: // Soft Hyphen <SHY>
                    case 0x2011: // Non Breaking Hyphen
                        // Use Hyphen Minus
                        glyphIndex = FontEngine.GetGlyphIndex(0x2D);
                        break;
                }

                // Return if no potential alternative glyph representation is present in font file.
                if (glyphIndex == 0)
                {
                    m_MissingUnicodesFromFontFile.Add(unicode);

                    k_TryAddCharacterMarker.End();
                    return false;
                }
            }

            // Check if glyph is already contained in the font asset as the same glyph might be referenced by multiple characters.
            if (m_GlyphLookupDictionary.ContainsKey(glyphIndex))
            {
                character = new TMP_Character(unicode, this, m_GlyphLookupDictionary[glyphIndex]);
                m_CharacterTable.Add(character);
                m_CharacterLookupDictionary.Add(unicode, character);

                #if UNITY_EDITOR
                // Makes the changes to the font asset persistent.
                // OPTIMIZATION: This could be handled when exiting Play mode if we added any new characters to the asset.
                // Could also add some update registry to handle this.
                //SortGlyphTable();
                if (UnityEditor.EditorUtility.IsPersistent(this))
                {
                    TMP_EditorResourceManager.RegisterResourceForUpdate(this);
                }
                #endif

                k_TryAddCharacterMarker.End();
                return true;
            }

            Glyph glyph = null;

            // Make sure atlas texture is readable.
            if (m_AtlasTextures[m_AtlasTextureIndex].isReadable == false)
            {
                Debug.LogWarning("Unable to add the requested character to font asset [" + this.name + "]'s atlas texture. Please make the texture [" + m_AtlasTextures[m_AtlasTextureIndex].name + "] readable.", m_AtlasTextures[m_AtlasTextureIndex]);

                k_TryAddCharacterMarker.End();
                return false;
            }

            // Resize the Atlas Texture to the appropriate size
            if (m_AtlasTextures[m_AtlasTextureIndex].width == 0 || m_AtlasTextures[m_AtlasTextureIndex].height == 0)
            {
                m_AtlasTextures[m_AtlasTextureIndex].Reinitialize(m_AtlasWidth, m_AtlasHeight);
                FontEngine.ResetAtlasTexture(m_AtlasTextures[m_AtlasTextureIndex]);
            }

            // Try adding glyph to local atlas texture
            if (FontEngine.TryAddGlyphToTexture(glyphIndex, m_AtlasPadding, GlyphPackingMode.BestShortSideFit, m_FreeGlyphRects, m_UsedGlyphRects, m_AtlasRenderMode, m_AtlasTextures[m_AtlasTextureIndex], out glyph))
            {
                // Update glyph atlas index
                glyph.atlasIndex = m_AtlasTextureIndex;

                // Add new glyph to glyph table.
                m_GlyphTable.Add(glyph);
                m_GlyphLookupDictionary.Add(glyphIndex, glyph);

                // Add new character
                character = new TMP_Character(unicode, this, glyph);
                m_CharacterTable.Add(character);
                m_CharacterLookupDictionary.Add(unicode, character);

                m_GlyphIndexList.Add(glyphIndex);
                m_GlyphIndexListNewlyAdded.Add(glyphIndex);

                #if UNITY_EDITOR
                // Makes the changes to the font asset persistent.
                // OPTIMIZATION: This could be handled when exiting Play mode if we added any new characters to the asset.
                // Could also add some update registry to handle this.
                //SortGlyphTable();
                if (UnityEditor.EditorUtility.IsPersistent(this))
                {
                    TMP_EditorResourceManager.RegisterResourceForUpdate(this);
                }
                #endif

                k_TryAddCharacterMarker.End();
                return true;
            }

            // Add glyph which did not fit in current atlas texture to new atlas texture.
            if (m_IsMultiAtlasTexturesEnabled)
            {
                // Create new atlas texture
                SetupNewAtlasTexture();

                // Try adding glyph to newly created atlas texture
                if (FontEngine.TryAddGlyphToTexture(glyphIndex, m_AtlasPadding, GlyphPackingMode.BestShortSideFit, m_FreeGlyphRects, m_UsedGlyphRects, m_AtlasRenderMode, m_AtlasTextures[m_AtlasTextureIndex], out glyph))
                {
                    // Update glyph atlas index
                    glyph.atlasIndex = m_AtlasTextureIndex;

                    // Add new glyph to glyph table.
                    m_GlyphTable.Add(glyph);
                    m_GlyphLookupDictionary.Add(glyphIndex, glyph);

                    // Add new character
                    character = new TMP_Character(unicode, this, glyph);
                    m_CharacterTable.Add(character);
                    m_CharacterLookupDictionary.Add(unicode, character);

                    m_GlyphIndexList.Add(glyphIndex);
                    m_GlyphIndexListNewlyAdded.Add(glyphIndex);

                    #if UNITY_EDITOR
                    //SortGlyphTable();
                    if (UnityEditor.EditorUtility.IsPersistent(this))
                    {
                        TMP_EditorResourceManager.RegisterResourceForUpdate(this);
                    }
                    #endif

                    k_TryAddCharacterMarker.End();
                    return true;
                }
            }

            k_TryAddCharacterMarker.End();

            return false;
        }


        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        bool TryAddGlyphsToNewAtlasTexture()
        {
            // Create and prepare new atlas texture
            SetupNewAtlasTexture();

            Glyph[] glyphs;

            // Try adding remaining glyphs in the newly created atlas texture
            bool allGlyphsAddedToTexture = FontEngine.TryAddGlyphsToTexture(m_GlyphsToAdd, m_AtlasPadding, GlyphPackingMode.BestShortSideFit, m_FreeGlyphRects, m_UsedGlyphRects, m_AtlasRenderMode, m_AtlasTextures[m_AtlasTextureIndex], out glyphs);

            // Add new glyphs to relevant data structures.
            for (int i = 0; i < glyphs.Length && glyphs[i] != null; i++)
            {
                Glyph glyph = glyphs[i];
                uint glyphIndex = glyph.index;

                glyph.atlasIndex = m_AtlasTextureIndex;

                // Add new glyph to glyph table.
                m_GlyphTable.Add(glyph);
                m_GlyphLookupDictionary.Add(glyphIndex, glyph);

                m_GlyphIndexListNewlyAdded.Add(glyphIndex);
                m_GlyphIndexList.Add(glyphIndex);
            }

            // Clear glyph index list to allow us to track glyphs
            m_GlyphsToAdd.Clear();

            // Add new characters to relevant data structures as well as track glyphs that could not be added to the current atlas texture.
            for (int i = 0; i < m_CharactersToAdd.Count; i++)
            {
                TMP_Character character = m_CharactersToAdd[i];
                Glyph glyph;

                if (m_GlyphLookupDictionary.TryGetValue(character.glyphIndex, out glyph) == false)
                {
                    m_GlyphsToAdd.Add(character.glyphIndex);
                    continue;
                }

                // Add a reference to the source text asset and glyph
                character.glyph = glyph;
                character.textAsset = this;

                m_CharacterTable.Add(character);
                m_CharacterLookupDictionary.Add(character.unicode, character);

                // Remove character
                m_CharactersToAdd.RemoveAt(i);
                i -= 1;
            }

            return allGlyphsAddedToTexture;
        }


        /// <summary>
        ///
        /// </summary>
        void SetupNewAtlasTexture()
        {
            m_AtlasTextureIndex += 1;

            // Check size of atlas texture array
            if (m_AtlasTextures.Length == m_AtlasTextureIndex)
                Array.Resize(ref m_AtlasTextures, m_AtlasTextures.Length * 2);

            // Initialize new atlas texture
            m_AtlasTextures[m_AtlasTextureIndex] = new Texture2D(m_AtlasWidth, m_AtlasHeight, TextureFormat.Alpha8, false);
            FontEngine.ResetAtlasTexture(m_AtlasTextures[m_AtlasTextureIndex]);

            // Clear packing GlyphRects
            int packingModifier = ((GlyphRasterModes)m_AtlasRenderMode & GlyphRasterModes.RASTER_MODE_BITMAP) == GlyphRasterModes.RASTER_MODE_BITMAP ? 0 : 1;
            m_FreeGlyphRects.Clear();
            m_FreeGlyphRects.Add(new GlyphRect(0, 0, m_AtlasWidth - packingModifier, m_AtlasHeight - packingModifier));
            m_UsedGlyphRects.Clear();

            #if UNITY_EDITOR
            // Add new texture as sub asset to font asset
            if (UnityEditor.EditorUtility.IsPersistent(this))
            {
                Texture2D tex = m_AtlasTextures[m_AtlasTextureIndex];
                tex.name = m_AtlasTexture.name + " " + m_AtlasTextureIndex;

                UnityEditor.AssetDatabase.AddObjectToAsset(m_AtlasTextures[m_AtlasTextureIndex], this);
                TMP_EditorResourceManager.RegisterResourceForReimport(this);
            }
            #endif
        }


        /// <summary>
        /// Clears font asset data including the glyph and character tables and textures.
        /// Function might be changed to Internal and only used in tests.
        /// </summary>
        /// <param name="setAtlasSizeToZero">Will set the atlas texture size to zero width and height if true.</param>
        public void ClearFontAssetData(bool setAtlasSizeToZero = false)
        {
            k_ClearFontAssetDataMarker.Begin();

            #if UNITY_EDITOR
            // Record full object undo in the Editor.
            //UnityEditor.Undo.RecordObjects(new UnityEngine.Object[] { this, this.atlasTexture }, "Resetting Font Asset");
            #endif

            // Clear glyph, character and font feature tables
            ClearFontAssetTables();

            // Clear atlas textures
            ClearAtlasTextures(setAtlasSizeToZero);

            ReadFontAssetDefinition();

            //TMP_ResourceManager.RebuildFontAssetCache(instanceID);

            #if UNITY_EDITOR
            // Makes the changes to the font asset persistent.
            TMP_EditorResourceManager.RegisterResourceForUpdate(this);
            #endif

            k_ClearFontAssetDataMarker.End();
        }

        internal void ClearFontAssetDataInternal()
        {
            // Clear glyph, character and font feature tables
            ClearFontAssetTables();

            // Clear atlas textures
            ClearAtlasTextures(true);

            #if UNITY_EDITOR
            // Makes the changes to the font asset persistent.
            TMP_EditorResourceManager.RegisterResourceForUpdate(this);
            #endif
        }

        /// <summary>
        ///
        /// </summary>
        internal void UpdateFontAssetData()
        {
            k_UpdateFontAssetDataMarker.Begin();

            // Get list of all characters currently contained in the font asset.
            uint[] unicodeCharacters = new uint[m_CharacterTable.Count];

            for (int i = 0; i < m_CharacterTable.Count; i++)
                unicodeCharacters[i] = m_CharacterTable[i].unicode;

            // Clear glyph, character and font feature tables
            ClearFontAssetTables();

            // Clear atlas textures
            ClearAtlasTextures(true);

            ReadFontAssetDefinition();

            //TMP_ResourceManager.RebuildFontAssetCache(instanceID);

            // Add existing glyphs and characters back in the font asset (if any)
            if (unicodeCharacters.Length > 0)
                TryAddCharacters(unicodeCharacters);

            k_UpdateFontAssetDataMarker.End();
        }

        /// <summary>
        ///
        /// </summary>
        internal void ClearFontAssetTables()
        {
            // Clear glyph and character tables
            m_GlyphTable?.Clear();

            m_CharacterTable?.Clear();

            // Clear glyph rectangles
            m_UsedGlyphRects?.Clear();

            if (m_FreeGlyphRects != null)
            {
                int packingModifier = ((GlyphRasterModes)m_AtlasRenderMode & GlyphRasterModes.RASTER_MODE_BITMAP) == GlyphRasterModes.RASTER_MODE_BITMAP ? 0 : 1;
                m_FreeGlyphRects.Clear();
                m_FreeGlyphRects.Add(new GlyphRect(0, 0, m_AtlasWidth - packingModifier, m_AtlasHeight - packingModifier));
            }

            m_GlyphsToRender?.Clear();

            m_GlyphsRendered?.Clear();
        }

        /// <summary>
        /// Internal function to clear all atlas textures.
        /// </summary>
        /// <param name="setAtlasSizeToZero">Set main atlas texture size to zero if true.</param>
        internal void ClearAtlasTextures(bool setAtlasSizeToZero = false)
        {
            m_AtlasTextureIndex = 0;

            // Return if we don't have any atlas textures
            if (m_AtlasTextures == null)
                return;

            Texture2D texture = null;

            // Clear all additional atlas textures
            for (int i = 1; i < m_AtlasTextures.Length; i++)
            {
                texture = m_AtlasTextures[i];

                if (texture == null)
                    continue;

                DestroyImmediate(texture, true);

                #if UNITY_EDITOR
                if (UnityEditor.EditorUtility.IsPersistent(this))
                    TMP_EditorResourceManager.RegisterResourceForReimport(this);
                #endif
            }

            // Resize atlas texture array down to one texture
            Array.Resize(ref m_AtlasTextures, 1);

            texture = m_AtlasTexture = m_AtlasTextures[0];

            // Clear main atlas texture
            if (texture.isReadable == false)
            {
                #if UNITY_EDITOR && UNITY_2018_4_OR_NEWER && !UNITY_2018_4_0 && !UNITY_2018_4_1 && !UNITY_2018_4_2 && !UNITY_2018_4_3 && !UNITY_2018_4_4
                    FontEngineEditorUtilities.SetAtlasTextureIsReadable(texture, true);
                #else
                    Debug.LogWarning("Unable to reset font asset [" + this.name + "]'s atlas texture. Please make the texture [" + texture.name + "] readable.", texture);
                    return;
                #endif
            }

            if (setAtlasSizeToZero)
            {
                texture.Reinitialize(0, 0, TextureFormat.Alpha8, false);
            }
            else if (texture.width != m_AtlasWidth || texture.height != m_AtlasHeight)
            {
                texture.Reinitialize(m_AtlasWidth, m_AtlasHeight, TextureFormat.Alpha8, false);
            }

            // Clear texture atlas
            FontEngine.ResetAtlasTexture(texture);
            texture.Apply();
        }
    }
}
