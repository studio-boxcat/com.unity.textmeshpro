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

        [SerializeField]
        private Font m_SourceFontFile;

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
        private static ProfilerMarker k_ClearFontAssetDataMarker = new ProfilerMarker("TMP.ClearFontAssetData");


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

            // Only characters not present in the source font file will be synthesized.

            // Non visible and control characters with no metrics
            // Add End of Text \u0003
            AddSynthesizedCharacter(0x03);

            // Add Tab \u0009
            AddSynthesizedCharacter(0x09);

            // Add Line Feed (LF) \u000A
            AddSynthesizedCharacter(0x0A);

            // Add Vertical Tab (VT) \u000B
            AddSynthesizedCharacter(0x0B);

            // Add Carriage Return (CR) \u000D
            AddSynthesizedCharacter(0x0D);

            // Add Arabic Letter Mark \u061C
            AddSynthesizedCharacter(0x061C);

            // Add Zero Width Space <ZWSP> \u2000B
            AddSynthesizedCharacter(0x200B);

            // Add Left-To-Right Mark \u200E
            AddSynthesizedCharacter(0x200E);

            // Add Right-To-Left Mark \u200F
            AddSynthesizedCharacter(0x200F);

            // Add Line Separator \u2028
            AddSynthesizedCharacter(0x2028);

            // Add Paragraph Separator \u2029
            AddSynthesizedCharacter(0x2029);

            // Add Word Joiner <WJ> / Zero Width Non-Breaking Space \u2060
            AddSynthesizedCharacter(0x2060);

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

        void AddSynthesizedCharacter(uint unicode)
        {
            // Check if unicode is already present in the font asset
            if (m_CharacterLookupDictionary.ContainsKey(unicode))
                return;

            // Synthesize and add missing glyph and character
            var glyph = new Glyph(0, new GlyphMetrics(0, 0, 0, 0, 0), GlyphRect.zero, 1.0f, 0);
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
        /// List of all the glyph indexes contained in the font asset.
        /// </summary>
        private List<uint> m_GlyphIndexList = new List<uint>();


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
