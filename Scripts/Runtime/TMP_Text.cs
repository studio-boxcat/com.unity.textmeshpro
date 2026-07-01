#define TMP_PRESENT
// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.TextCore;
using UnityEngine.UI;


namespace TMPro
{
    /// <summary>
    /// Horizontal text alignment options.
    /// </summary>
    [Flags]
    public enum HorizontalAlignmentOptions
    {
        Left = 0x1, Center = 0x2, Right = 0x4,
    }

    /// <summary>
    /// Vertical text alignment options.
    /// </summary>
    [Flags]
    public enum VerticalAlignmentOptions
    {
        Top = 0x100, Middle = 0x200, Bottom = 0x400, Baseline = 0x800, Geometry = 0x1000, Capline = 0x2000,
    }

    public enum TextOverflowModes { Overflow = 0, Truncate = 3 }

    [Flags]
    public enum FontStyles { Normal = 0x0, Bold = 0x1, Italic = 0x2 };

    /// <summary>
    /// Base class which contains common properties and functions shared between the TextMeshPro and TextMeshProUGUI component.
    /// </summary>
    public abstract class TMP_Text : Graphic
    {
        /// <summary>
        /// A string containing the text to be displayed.
        /// </summary>
        public string text
        {
            get
            {
                if (m_IsTextBackingStringDirty)
                    return InternalTextBackingArrayToString();

                return m_text;
            }
            set
            {
                if (m_IsTextBackingStringDirty == false && m_text != null && value != null && m_text.Length == value.Length && m_text == value)
                    return;

                m_IsTextBackingStringDirty = false;
                m_text = value;
                m_inputSource = TextInputSources.TextString;
                m_havePropertiesChanged = true;
                SetVerticesDirty();
                SetLayoutDirty();
            }
        }
        [SerializeField]
        [TextArea(5, 10)]
        protected string m_text;

        /// <summary>
        ///
        /// </summary>
        private bool m_IsTextBackingStringDirty;

        /// <summary>
        /// The Font Asset to be assigned to this text object.
        /// </summary>
        public TMP_FontAsset font
        {
            get { return m_fontAsset; }
            set { if (m_fontAsset == value) return; m_fontAsset = value; LoadFontAsset(); m_havePropertiesChanged = true; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField, Required]
        protected TMP_FontAsset m_fontAsset;
        protected TMP_FontAsset m_currentFontAsset;
        protected bool m_isSDFShader;


        /// <summary>
        /// The material to be assigned to this text object.
        /// </summary>
        public Material fontSharedMaterial
        {
            get { return m_sharedMaterial; }
            set { if (m_sharedMaterial == value) return; SetSharedMaterial(value); m_havePropertiesChanged = true; SetVerticesDirty(); SetMaterialDirty(); }
        }
        [SerializeField, Required]
        protected Material m_sharedMaterial;
        protected Material m_currentMaterial;

        protected int m_currentMaterialIndex;


        /// <summary>
        /// The material to be assigned to this text object. An instance of the material will be assigned to the object's renderer.
        /// </summary>
        public Material fontMaterial
        {
            // Return an Instance of the current material.
            get { return GetMaterial(m_sharedMaterial); }

            // Assign new font material
            set
            {
                if (m_sharedMaterial.RefEq(value)) return;

                m_sharedMaterial = value;

                m_padding = GetPaddingForMaterial();
                m_havePropertiesChanged = true;

                SetVerticesDirty();
                SetMaterialDirty();
            }
        }
        [SerializeField]
        protected Material m_fontMaterial;


        protected bool m_isMaterialDirty;


        /// <summary>
        /// This is the default vertex color assigned to each vertices. Color tags will override vertex colors unless the overrideColorTags is set.
        /// </summary>
        public override Color color
        {
            get => m_fontColor;
            set { if (m_fontColor == value) return; m_havePropertiesChanged = true; m_fontColor = value; SetVerticesDirty(); }
        }
        //[UnityEngine.Serialization.FormerlySerializedAs("m_fontColor")] // Required for backwards compatibility with pre-Unity 4.6 releases.
        [SerializeField]
        protected Color32 m_fontColor32 = Color.white;
        [SerializeField]
        protected Color m_fontColor = Color.white;

        /// <summary>
        /// Sets the vertex color alpha value.
        /// </summary>
        public float alpha
        {
            get { return m_fontColor.a; }
            set { if (m_fontColor.a == value) return; m_fontColor.a = value; m_havePropertiesChanged = true; SetVerticesDirty(); }
        }

        /// <summary>
        /// The point size of the font.
        /// </summary>
        public float fontSize
        {
            get { return m_fontSize; }
            set { if (m_fontSize == value) return; m_havePropertiesChanged = true; m_fontSize = value; m_fontSizeBase = m_fontSize; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected float m_fontSize = -99; // Font Size
        protected float m_currentFontSize; // Temporary Font Size affected by tags
        [SerializeField] // TODO: Review if this should be serialized
        protected float m_fontSizeBase = 36;


        /// <summary>
        /// The style of the text
        /// </summary>
        public FontStyles fontStyle
        {
            get { return m_fontStyle; }
            set { if (m_fontStyle == value) return; m_fontStyle = value; m_havePropertiesChanged = true; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected FontStyles m_fontStyle = FontStyles.Normal;
        protected FontStyles m_FontStyleInternal = FontStyles.Normal;

        /// <summary>
        /// Property used in conjunction with padding calculation for the geometry.
        /// </summary>
        public bool isUsingBold { get { return m_isUsingBold; } }
        protected bool m_isUsingBold = false; // Used to ensure GetPadding & Ratios take into consideration bold characters.

        /// <summary>
        /// Horizontal alignment options
        /// </summary>
        public HorizontalAlignmentOptions horizontalAlignment
        {
            get { return m_HorizontalAlignment; }
            set
            {
                if (m_HorizontalAlignment == value)
                    return;

                m_HorizontalAlignment = value;

                m_havePropertiesChanged = true;
                SetVerticesDirty();
            }
        }
        [SerializeField]
        protected HorizontalAlignmentOptions m_HorizontalAlignment = HorizontalAlignmentOptions.Left;

        /// <summary>
        /// Vertical alignment options
        /// </summary>
        public VerticalAlignmentOptions verticalAlignment
        {
            get => m_VerticalAlignment;
            set
            {
                if (m_VerticalAlignment == value)
                    return;

                m_VerticalAlignment = value;

                m_havePropertiesChanged = true;
                SetVerticesDirty();
            }
        }
        [SerializeField]
        protected VerticalAlignmentOptions m_VerticalAlignment = VerticalAlignmentOptions.Top;

        protected HorizontalAlignmentOptions m_lineJustification;

        /// <summary>
        /// The amount of additional spacing between characters.
        /// </summary>
        public float characterSpacing
        {
            get { return m_characterSpacing; }
            set { if (m_characterSpacing == value) return; m_havePropertiesChanged = true; m_characterSpacing = value; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected float m_characterSpacing = 0;
        protected float m_cSpacing = 0;
        protected float m_monoSpacing = 0;

        /// <summary>
        /// The amount of additional spacing to add between each lines of text.
        /// </summary>
        public float lineSpacing
        {
            get { return m_lineSpacing; }
            set { if (m_lineSpacing == value) return; m_havePropertiesChanged = true; m_lineSpacing = value; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected float m_lineSpacing = 0;
        protected float m_lineSpacingDelta = 0; // Always 0 (auto-sizing removed); retained in line-offset math.
        protected float m_lineHeight = TMP_Math.FLOAT_UNSET; // Used with the <line-height=xx.x> tag.
        protected bool m_IsDrivenLineSpacing;

        protected float m_charWidthAdjDelta = 0; // Always 0 (auto-sizing removed); retained in advance math.


        /// <summary>
        /// Controls whether or not word wrapping is applied. When disabled, the text will be displayed on a single line.
        /// </summary>
        public bool enableWordWrapping
        {
            get { return m_enableWordWrapping; }
            set { if (m_enableWordWrapping == value) return; m_havePropertiesChanged = true; m_enableWordWrapping = value; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected bool m_enableWordWrapping = false;

        /// <summary>
        /// Controls the Text Overflow Mode
        /// </summary>
        public TextOverflowModes overflowMode
        {
            get { return m_overflowMode; }
            set { if (m_overflowMode == value) return; m_overflowMode = value; m_havePropertiesChanged = true; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected TextOverflowModes m_overflowMode = TextOverflowModes.Overflow;


        /// <summary>
        /// Indicates if the text exceeds the vertical bounds of its text container.
        /// </summary>
        public bool isTextOverflowing
        {
            get { if (m_firstOverflowCharacterIndex != -1) return true; return false; }
        }


        /// <summary>
        /// The first character which exceeds the vertical bounds of its text container.
        /// </summary>
        public int firstOverflowCharacterIndex => m_firstOverflowCharacterIndex;
        //[SerializeField]
        protected int m_firstOverflowCharacterIndex = -1;



        protected float m_GlyphHorizontalAdvanceAdjustment;

        /// <summary>
        /// Adds extra padding around each character. This may be necessary when the displayed text is very small to prevent clipping.
        /// </summary>
        public bool extraPadding
        {
            get { return m_enableExtraPadding; }
            set { if (m_enableExtraPadding == value) return; m_havePropertiesChanged = true; m_enableExtraPadding = value; UpdateMeshPadding(); SetVerticesDirty(); /* SetLayoutDirty();*/ }
        }
        [SerializeField]
        protected bool m_enableExtraPadding = false;
        [SerializeField]
        protected bool checkPaddingRequired;




        /// <summary>
        /// Determines if the data structures allocated to contain the geometry of the text object will be reduced in size if the number of characters required to display the text is reduced by more than 256 characters.
        /// This reduction has the benefit of reducing the amount of vertex data being submitted to the graphic device but results in GC when it occurs.
        /// </summary>
        public bool vertexBufferAutoSizeReduction
        {
            get { return m_VertexBufferAutoSizeReduction; }
            set { m_VertexBufferAutoSizeReduction = value; m_havePropertiesChanged = true; SetVerticesDirty(); }
        }
        [SerializeField]
        protected bool m_VertexBufferAutoSizeReduction = false;

        /// <summary>
        /// Determines if the text's vertical alignment will be adjusted based on visible descender of the text.
        /// </summary>
        public bool useMaxVisibleDescender
        {
            get { return m_useMaxVisibleDescender; }
            set { if (m_useMaxVisibleDescender == value) return; m_havePropertiesChanged = true; m_useMaxVisibleDescender = value; SetVerticesDirty(); }
        }
        [SerializeField]
        protected bool m_useMaxVisibleDescender = true;


        protected float m_marginWidth;  // Width of the RectTransform minus left and right margins.
        protected float m_marginHeight; // Height of the RectTransform minus top and bottom margins.
        protected float m_width = -1;


        /// <summary>
        /// Returns data about the text object which includes information about each character, word, line, link, etc.
        /// </summary>
        public TMP_TextInfo textInfo => m_textInfo;
        [NonSerialized]
        protected TMP_TextInfo m_textInfo; // Class which holds information about the Text object such as characters, lines, mesh data as well as metrics.

        /// <summary>
        /// Property tracking if any of the text properties have changed. Flag is set before the text is regenerated.
        /// </summary>
        public bool havePropertiesChanged
        {
            get { return m_havePropertiesChanged; }
            set { if (m_havePropertiesChanged == value) return; m_havePropertiesChanged = value; SetAllDirty(); }
        }
        //[SerializeField]
        protected bool m_havePropertiesChanged;  // Used to track when properties of the text object have changed.


        /// <summary>
        /// Used to track potential changes in RectTransform size to allow us to ignore OnRectTransformDimensionsChange getting called due to rounding errors when using Stretch Anchors.
        /// </summary>
        protected Vector2 m_PreviousRectTransformSize;

        /// <summary>
        /// Used to track potential changes in pivot position to allow us to ignore OnRectTransformDimensionsChange getting called due to rounding errors when using Stretch Anchors.
        /// </summary>
        protected Vector2 m_PreviousPivotPosition;


        /// <summary>
        /// The mesh used by the font asset and material assigned to the text object.
        /// </summary>
        protected Mesh m_mesh;

        /// <summary>
        /// Computed preferred width of the text object.
        /// </summary>
        // Preferred size is unused (no layout consumer); GenerateTextMesh leaves these at their reset 0.
        public virtual float preferredWidth => m_preferredWidth;
        protected float m_preferredWidth;

        /// <summary>
        /// Computed preferred height of the text object.
        /// </summary>
        public virtual float preferredHeight => m_preferredHeight;
        protected float m_preferredHeight;

        protected bool m_isCalculatingPreferredValues;


        protected bool m_isLayoutDirty;

        protected bool m_isAwake;

        // Protected Fields
        internal enum TextInputSources { TextInputBox = 0, SetText = 1, SetTextArray = 2, TextString = 3 };
        //[SerializeField]
        internal TextInputSources m_inputSource;

        /// <summary>
        /// Array containing the Unicode characters to be parsed.
        /// </summary>
        internal UnicodeChar[] m_TextProcessingArray = new UnicodeChar[8];

        /// <summary>
        /// The number of Unicode characters that have been parsed and contained in the m_InternalParsingBuffer
        /// </summary>
        internal int m_InternalTextProcessingArraySize;

        [System.Diagnostics.DebuggerDisplay("Unicode ({unicode})  '{(char)unicode}'")]
        internal struct UnicodeChar
        {
            public int unicode;
        }

        private TMP_CharacterInfo[] m_internalCharacterInfo; // Used by functions to calculate preferred values.
        protected int m_totalCharacterCount;

        // Fields whose state is saved in conjunction with text parsing and word wrapping.
        protected int m_characterCount;
        protected int m_firstCharacterOfLine;
        protected int m_firstVisibleCharacterOfLine;
        protected int m_lastCharacterOfLine;
        protected int m_lastVisibleCharacterOfLine;
        protected int m_lineNumber;
        protected int m_lineVisibleCharacterCount;
        protected float m_PageAscender;
        protected float m_maxTextAscender;
        protected float m_maxCapHeight;
        protected float m_ElementDescender;
        protected float m_maxLineAscender;
        protected float m_maxLineDescender;
        protected float m_startOfLineAscender;
        protected float m_lineOffset;
        protected Extents m_meshExtents;


        // Fields used for vertex colors
        protected Color32 m_htmlColor = new Color(255, 255, 255, 128);


        protected float m_padding = 0;
        protected float m_baselineOffset; // Used for superscript and subscript.
        protected float m_xAdvance; // Tracks x advancement from character to character.

        protected TMP_TextElement m_cached_TextElement; // Glyph / Character information is cached into this variable which is faster than having to fetch from the Dictionary multiple times.

        // Profiler Marker declarations
        private static ProfilerMarker k_ParseTextMarker = new("TMP Parse Text");

        /// <summary>
        /// Method which derived classes need to override to load Font Assets.
        /// </summary>
        protected abstract void LoadFontAsset();

        /// <summary>
        /// Function called internally when a new shared material is assigned via the fontSharedMaterial property.
        /// </summary>
        /// <param name="mat"></param>
        protected abstract void SetSharedMaterial(Material mat);

        /// <summary>
        /// Function called internally when a new material is assigned via the fontMaterial property.
        /// </summary>
        protected abstract Material GetMaterial(Material mat);

        /// <summary>
        /// Function used to create an instance of the material
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        protected static Material CreateMaterialInstance(Material source)
        {
            var mat = new Material(source);
            mat.shaderKeywords = source.shaderKeywords;
            mat.name += " (Instance)";
            return mat;
        }

        /// <summary>
        ///
        /// </summary>
        internal virtual void UpdateCulling() {}

        /// <summary>
        /// Get the padding value for the currently assigned material
        /// </summary>
        /// <returns></returns>
        protected float GetPaddingForMaterial()
        {
            ShaderUtilities.GetShaderPropertyIDs();

            m_padding = ShaderUtilities.GetPadding(m_sharedMaterial, m_enableExtraPadding, m_isUsingBold);
            m_isSDFShader = m_sharedMaterial.HasProperty(ShaderUtilities.ID_WeightNormal);

            return m_padding;
        }


        /// <summary>
        /// Get the padding value for the given material
        /// </summary>
        /// <returns></returns>
        protected float GetPaddingForMaterial(Material mat)
        {
            if (mat == null)
                return 0;

            m_padding = ShaderUtilities.GetPadding(mat, m_enableExtraPadding, m_isUsingBold);
            m_isSDFShader = mat.HasProperty(ShaderUtilities.ID_WeightNormal);

            return m_padding;
        }


        // PUBLIC FUNCTIONS
        protected bool m_ignoreActiveState;


        /// <summary>
        /// Function to be used to force recomputing of character padding when Shader / Material properties have been changed via script.
        /// </summary>
        protected abstract void UpdateMeshPadding();


        /// <summary>
        ///
        /// </summary>
        struct TextBackingContainer
        {
            public int Capacity => m_Array.Length;

            public int Count
            {
                get { return m_Count; }
                set { m_Count = value; }
            }

            private uint[] m_Array;
            private int m_Count;

            public uint this[int index]
            {
                get { return m_Array[index]; }
                set
                {
                    if (index >= m_Array.Length)
                        Resize(index);

                    m_Array[index] = value;
                }
            }

            public TextBackingContainer(int size)
            {
                m_Array = new uint[size];
                m_Count = 0;
            }

            public void Resize(int size)
            {
                size = Mathf.NextPowerOfTwo(size + 1);

                Array.Resize(ref m_Array, size);
            }

        }

        /// <summary>
        /// Internal array containing the converted source text used in the text parsing process.
        /// </summary>
        private TextBackingContainer m_TextBackingArray = new(4);


        /// <summary>
        /// Method to parse the input text based on its source
        /// </summary>
        protected void ParseInputText()
        {
            k_ParseTextMarker.Begin();

            switch (m_inputSource)
            {
                case TextInputSources.TextString:
                case TextInputSources.TextInputBox:
                    PopulateTextBackingArray(m_text);
                    PopulateTextProcessingArray();
                    break;
                case TextInputSources.SetText:
                    break;
                case TextInputSources.SetTextArray:
                    break;
            }

            SetArraySizes(m_TextProcessingArray);

            k_ParseTextMarker.End();
        }


        /// <summary>
        /// Convert source text to Unicode (uint) and populate internal text backing array.
        /// </summary>
        /// <param name="sourceText">Source text to be converted</param>
        void PopulateTextBackingArray(string sourceText)
        {
            var srcLength = sourceText?.Length ?? 0;
            PopulateTextBackingArray(sourceText, 0, srcLength);
        }

        /// <summary>
        /// Convert source text to uint and populate internal text backing array.
        /// </summary>
        /// <param name="sourceText">string containing the source text to be converted</param>
        /// <param name="start">Index of the first element of the source array to be converted and copied to the internal text backing array.</param>
        /// <param name="length">Number of elements in the array to be converted and copied to the internal text backing array.</param>
        void PopulateTextBackingArray(string sourceText, int start, int length)
        {
            int readIndex;
            int writeIndex = 0;

            // Range check
            if (sourceText == null)
            {
                readIndex = 0;
                length = 0;
            }
            else
            {
                readIndex = Mathf.Clamp(start, 0, sourceText.Length);
                length = Mathf.Clamp(length, 0, start + length < sourceText.Length ? length : sourceText.Length - start);
            }

            // Make sure array size is appropriate
            if (length >= m_TextBackingArray.Capacity)
                m_TextBackingArray.Resize((length));

            int end = readIndex + length;
            for (; readIndex < end; readIndex++)
            {
                m_TextBackingArray[writeIndex] = sourceText[readIndex];
                writeIndex += 1;
            }

            // Terminate array with zero as we are not clearing the array on new invocation of this function.
            m_TextBackingArray[writeIndex] = 0;
            m_TextBackingArray.Count = writeIndex;
        }

        /// <summary>
        ///
        /// </summary>
        void PopulateTextProcessingArray()
        {
            int srcLength = m_TextBackingArray.Count;

            // Make sure parsing buffer is large enough to handle the required text.
            if (m_TextProcessingArray.Length < srcLength)
                ResizeInternalArray(ref m_TextProcessingArray, srcLength);

            int writeIndex = 0;

            int readIndex = 0;
            for (; readIndex < srcLength; readIndex++)
            {
                uint c = m_TextBackingArray[readIndex];

                if (c == 0)
                    break;

                if (m_inputSource == TextInputSources.TextInputBox && c == '\\' && readIndex < srcLength - 1)
                {
                    switch (m_TextBackingArray[readIndex + 1])
                    {
                        case 117: // \u0000 for UTF-16 Unicode
                            if (srcLength > readIndex + 5)
                            {
                                if (writeIndex == m_TextProcessingArray.Length) ResizeInternalArray(ref m_TextProcessingArray);

                                m_TextProcessingArray[writeIndex].unicode = GetUTF16(m_TextBackingArray, readIndex + 2);

                                readIndex += 5;
                                writeIndex += 1;
                                continue;
                            }
                            break;
                        case 85: // \U00000000 for UTF-32 Unicode
                            if (srcLength > readIndex + 9)
                            {
                                if (writeIndex == m_TextProcessingArray.Length) ResizeInternalArray(ref m_TextProcessingArray);

                                m_TextProcessingArray[writeIndex].unicode = GetUTF32(m_TextBackingArray, readIndex + 2);

                                readIndex += 9;
                                writeIndex += 1;
                                continue;
                            }
                            break;
                    }
                }

                if (writeIndex == m_TextProcessingArray.Length) ResizeInternalArray(ref m_TextProcessingArray);

                m_TextProcessingArray[writeIndex].unicode = (int)c;

                writeIndex += 1;
            }

            if (writeIndex == m_TextProcessingArray.Length) ResizeInternalArray(ref m_TextProcessingArray);

            m_TextProcessingArray[writeIndex].unicode = 0;
            m_InternalTextProcessingArraySize = writeIndex;
        }

        /// <summary>
        ///
        /// </summary>
        static void ResizeInternalArray <T>(ref T[] array)
        {
            int size = Mathf.NextPowerOfTwo(array.Length + 1);
            Array.Resize(ref array, size);
        }

        static void ResizeInternalArray<T>(ref T[] array, int size)
        {
            size = Mathf.NextPowerOfTwo(size + 1);
            Array.Resize(ref array, size);
        }

        string InternalTextBackingArrayToString()
        {
            char[] array = new char[m_TextBackingArray.Count];

            for (int i = 0; i < m_TextBackingArray.Capacity; i++)
            {
                char c = (char)m_TextBackingArray[i];

                if (c == 0)
                    break;

                array[i] = c;
            }

            m_IsTextBackingStringDirty = false;

            return new string(array);
        }


        /// <summary>
        /// Method used to determine the number of visible characters and required buffer allocations.
        /// </summary>
        /// <param name="unicodeChars"></param>
        /// <returns></returns>
        internal abstract int SetArraySizes(UnicodeChar[] unicodeChars);


        /// <summary>
        /// Method to adjust line spacing as a result of using different fonts or font point size.
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        /// <param name="offset"></param>
        // Function to offset vertices position to account for line spacing changes.
        protected void AdjustLineOffset(int startIndex, int endIndex, float offset)
        {
            Vector2 vertexOffset = new Vector2(0, offset);

            for (int i = startIndex; i <= endIndex; i++)
            {
                m_textInfo.characterInfo[i].bottomLeft -= vertexOffset;
                m_textInfo.characterInfo[i].topLeft -= vertexOffset;
                m_textInfo.characterInfo[i].topRight -= vertexOffset;
                m_textInfo.characterInfo[i].bottomRight -= vertexOffset;

                if (m_textInfo.characterInfo[i].isVisible)
                {
                    m_textInfo.characterInfo[i].vertex_BL.position -= vertexOffset;
                    m_textInfo.characterInfo[i].vertex_TL.position -= vertexOffset;
                    m_textInfo.characterInfo[i].vertex_TR.position -= vertexOffset;
                    m_textInfo.characterInfo[i].vertex_BR.position -= vertexOffset;
                }
            }
        }


        /// <summary>
        /// Function to increase the size of the Line Extents Array.
        /// </summary>
        /// <param name="size"></param>
        protected void ResizeLineExtents(int size)
        {
            size = size > 1024 ? size + 256 : Mathf.NextPowerOfTwo(size + 1);

            var temp_lineInfo = new TMP_LineInfo[size];
            for (int i = 0; i < size; i++)
            {
                if (i < m_textInfo.lineInfo.Length)
                    temp_lineInfo[i] = m_textInfo.lineInfo[i];
            }

            m_textInfo.lineInfo = temp_lineInfo;
        }
        protected static Vector2 k_LargePositiveVector2 = new(TMP_Math.INT_MAX, TMP_Math.INT_MAX);
        protected static Vector2 k_LargeNegativeVector2 = new(TMP_Math.INT_MIN, TMP_Math.INT_MIN);
        protected static float k_LargePositiveFloat = TMP_Math.FLOAT_MAX;
        protected static float k_LargeNegativeFloat = TMP_Math.FLOAT_MIN;





        /// <summary>
        /// Store vertex information for each character.
        /// </summary>
        /// <param name="style_padding">Style_padding.</param>
        /// <param name="vertexColor">Vertex color.</param>
        protected void SaveGlyphVertexInfo(float padding, float style_padding, Color32 vertexColor)
        {
            // Save the Vertex Position for the Character
            #region Setup Mesh Vertices
            m_textInfo.characterInfo[m_characterCount].vertex_BL.position = m_textInfo.characterInfo[m_characterCount].bottomLeft;
            m_textInfo.characterInfo[m_characterCount].vertex_TL.position = m_textInfo.characterInfo[m_characterCount].topLeft;
            m_textInfo.characterInfo[m_characterCount].vertex_TR.position = m_textInfo.characterInfo[m_characterCount].topRight;
            m_textInfo.characterInfo[m_characterCount].vertex_BR.position = m_textInfo.characterInfo[m_characterCount].bottomRight;
            #endregion


            #region Setup Vertex Colors
            // Alpha is the lower of the vertex color or tag color alpha used.
            vertexColor.a = m_fontColor32.a < vertexColor.a ? m_fontColor32.a : vertexColor.a;

            // Handle Vertex Colors & Vertex Color Gradient
            m_textInfo.characterInfo[m_characterCount].vertex_BL.color = vertexColor;
            m_textInfo.characterInfo[m_characterCount].vertex_TL.color = vertexColor;
            m_textInfo.characterInfo[m_characterCount].vertex_TR.color = vertexColor;
            m_textInfo.characterInfo[m_characterCount].vertex_BR.color = vertexColor;
            #endregion

            // Apply style_padding only if this is a SDF Shader.
            if (!m_isSDFShader)
                style_padding = 0f;


            // Setup UVs for the Character
            #region Setup UVs

            GlyphRect glyphRect = m_cached_TextElement.m_Glyph.glyphRect;

            Vector2 uv0;
            uv0.x = (glyphRect.x - padding - style_padding) / m_currentFontAsset.m_AtlasWidth;
            uv0.y = (glyphRect.y - padding - style_padding) / m_currentFontAsset.m_AtlasHeight;

            Vector2 uv1;
            uv1.x = uv0.x;
            uv1.y = (glyphRect.y + padding + style_padding + glyphRect.height) / m_currentFontAsset.m_AtlasHeight;

            Vector2 uv2;
            uv2.x = (glyphRect.x + padding + style_padding + glyphRect.width) / m_currentFontAsset.m_AtlasWidth;
            uv2.y = uv1.y;

            Vector2 uv3;
            uv3.x = uv2.x;
            uv3.y = uv0.y;

            // Store UV Information
            m_textInfo.characterInfo[m_characterCount].vertex_BL.uv = uv0;
            m_textInfo.characterInfo[m_characterCount].vertex_TL.uv = uv1;
            m_textInfo.characterInfo[m_characterCount].vertex_TR.uv = uv2;
            m_textInfo.characterInfo[m_characterCount].vertex_BR.uv = uv3;
            #endregion Setup UVs
        }


        /// <summary>
        /// Store vertex attributes into the appropriate TMP_MeshInfo.
        /// </summary>
        /// <param name="i"></param>
        protected void FillCharacterVertexBuffers(int i)
        {
            var materialIndex = m_textInfo.characterInfo[i].materialReferenceIndex;

            ref var meshInfo = ref m_textInfo.meshInfo[materialIndex];
            var si = meshInfo.vertexCount; // start index.
            // Check to make sure our current mesh buffer allocations can hold these new Quads.
            if (si >= meshInfo.vertices.Length)
                meshInfo.ResizeMeshInfo(Mathf.NextPowerOfTwo((si + 4) / 4));


            var charInfo = m_textInfo.characterInfo[i];

            // Setup Vertices for Characters
            meshInfo.vertices[0 + si] = charInfo.vertex_BL.position;
            meshInfo.vertices[1 + si] = charInfo.vertex_TL.position;
            meshInfo.vertices[2 + si] = charInfo.vertex_TR.position;
            meshInfo.vertices[3 + si] = charInfo.vertex_BR.position;


            // Setup UVS0
            meshInfo.uvs0[0 + si] = charInfo.vertex_BL.uv;
            meshInfo.uvs0[1 + si] = charInfo.vertex_TL.uv;
            meshInfo.uvs0[2 + si] = charInfo.vertex_TR.uv;
            meshInfo.uvs0[3 + si] = charInfo.vertex_BR.uv;


            // Setup UVS2
            meshInfo.uvs2[0 + si] = charInfo.vertex_BL.uv2;
            meshInfo.uvs2[1 + si] = charInfo.vertex_TL.uv2;
            meshInfo.uvs2[2 + si] = charInfo.vertex_TR.uv2;
            meshInfo.uvs2[3 + si] = charInfo.vertex_BR.uv2;


            // setup Vertex Colors
            meshInfo.colors32[0 + si] = charInfo.vertex_BL.color;
            meshInfo.colors32[1 + si] = charInfo.vertex_TL.color;
            meshInfo.colors32[2 + si] = charInfo.vertex_TR.color;
            meshInfo.colors32[3 + si] = charInfo.vertex_BR.color;

            meshInfo.vertexCount = si + 4;
        }


        /// <summary>
        /// Internal function used to load the default settings of text objects.
        /// </summary>
        protected void LoadDefaultSettings()
        {
            if (m_fontSize == -99)
            {
                m_enableWordWrapping = TMP_Settings.enableWordWrapping;
                m_enableExtraPadding = TMP_Settings.enableExtraPadding;
                m_fontSize = m_fontSizeBase = TMP_Settings.defaultFontSize;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        protected static float PackUV(float x, float y)
        {
            double x0 = (int)(x * 511);
            double y0 = (int)(y * 511);

            return (float)((x0 * 4096) + y0);
        }


        /// <summary>
        /// Function used as a replacement for LateUpdate()
        /// </summary>
        internal abstract void InternalUpdate();


        /// <summary>
        /// Method to convert Hex to Int
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        static int HexToInt(char hex) => TMP_TextUtilities.HexToInt(hex);

        static int GetUTF16(TextBackingContainer text, int i)
        {
            int unicode = 0;
            unicode += HexToInt((char)text[i]) << 12;
            unicode += HexToInt((char)text[i + 1]) << 8;
            unicode += HexToInt((char)text[i + 2]) << 4;
            unicode += HexToInt((char)text[i + 3]);
            return unicode;
        }


        static int GetUTF32(TextBackingContainer text, int i)
        {
            int unicode = 0;
            unicode += HexToInt((char)text[i]) << 28;
            unicode += HexToInt((char)text[i + 1]) << 24;
            unicode += HexToInt((char)text[i + 2]) << 20;
            unicode += HexToInt((char)text[i + 3]) << 16;
            unicode += HexToInt((char)text[i + 4]) << 12;
            unicode += HexToInt((char)text[i + 5]) << 8;
            unicode += HexToInt((char)text[i + 6]) << 4;
            unicode += HexToInt((char)text[i + 7]);
            return unicode;
        }

    }
}
