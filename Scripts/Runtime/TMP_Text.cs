#define TMP_PRESENT
// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.TextCore;
using UnityEngine.UI;


namespace TMPro
{
    public enum TextAlignmentOptions
    {
        TopLeft = HorizontalAlignmentOptions.Left | VerticalAlignmentOptions.Top,
        Top = HorizontalAlignmentOptions.Center | VerticalAlignmentOptions.Top,
        TopRight = HorizontalAlignmentOptions.Right | VerticalAlignmentOptions.Top,

        Left = HorizontalAlignmentOptions.Left | VerticalAlignmentOptions.Middle,
        Center = HorizontalAlignmentOptions.Center | VerticalAlignmentOptions.Middle,
        Right = HorizontalAlignmentOptions.Right | VerticalAlignmentOptions.Middle,

        BottomLeft = HorizontalAlignmentOptions.Left | VerticalAlignmentOptions.Bottom,
        Bottom = HorizontalAlignmentOptions.Center | VerticalAlignmentOptions.Bottom,
        BottomRight = HorizontalAlignmentOptions.Right | VerticalAlignmentOptions.Bottom,

        BaselineLeft = HorizontalAlignmentOptions.Left | VerticalAlignmentOptions.Baseline,
        Baseline = HorizontalAlignmentOptions.Center | VerticalAlignmentOptions.Baseline,
        BaselineRight = HorizontalAlignmentOptions.Right | VerticalAlignmentOptions.Baseline,

        MidlineLeft = HorizontalAlignmentOptions.Left | VerticalAlignmentOptions.Geometry,
        Midline = HorizontalAlignmentOptions.Center | VerticalAlignmentOptions.Geometry,
        MidlineRight = HorizontalAlignmentOptions.Right | VerticalAlignmentOptions.Geometry,

        CaplineLeft = HorizontalAlignmentOptions.Left | VerticalAlignmentOptions.Capline,
        Capline = HorizontalAlignmentOptions.Center | VerticalAlignmentOptions.Capline,
        CaplineRight = HorizontalAlignmentOptions.Right | VerticalAlignmentOptions.Capline,

        Converted = 0xFFFF
    }

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
    public abstract class TMP_Text : MaskableGraphic
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
        [SerializeField]
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
        [SerializeField]
        protected Material m_sharedMaterial;
        protected Material m_currentMaterial;
        protected static MaterialReference[] m_materialReferences = new MaterialReference[4];
        protected static Dictionary<int, int> m_materialReferenceIndexLookup = new();

        protected static TMP_TextProcessingStack<MaterialReference> m_materialReferenceStack = new(new MaterialReference[16]);
        protected int m_currentMaterialIndex;


        /// <summary>
        /// An array containing the materials used by the text object.
        /// </summary>
        public Material[] fontSharedMaterials
        {
            get => GetSharedMaterials();
            set { SetSharedMaterials(value); m_havePropertiesChanged = true; SetVerticesDirty(); SetMaterialDirty(); }
        }
        [SerializeField]
        protected Material[] m_fontSharedMaterials;


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
                if (m_sharedMaterial != null && m_sharedMaterial.GetInstanceID() == value.GetInstanceID()) return;

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
        /// Sets the color of the _FaceColor property of the assigned material. Changing face color will result in an instance of the material.
        /// </summary>
        public Color32 faceColor
        {
            get
            {
                if (m_sharedMaterial == null) return m_faceColor;

                m_faceColor = m_sharedMaterial.GetColor(ShaderUtilities.ID_FaceColor);
                return m_faceColor;
            }

            set { if (m_faceColor.Compare(value)) return; SetFaceColor(value); m_havePropertiesChanged = true; m_faceColor = value; SetVerticesDirty(); SetMaterialDirty(); }
        }
        [SerializeField]
        protected Color32 m_faceColor = Color.white;


        /// <summary>
        /// Sets the color of the _OutlineColor property of the assigned material. Changing outline color will result in an instance of the material.
        /// </summary>
        public Color32 outlineColor
        {
            get
            {
                if (m_sharedMaterial == null) return m_outlineColor;

                m_outlineColor = m_sharedMaterial.GetColor(ShaderUtilities.ID_OutlineColor);
                return m_outlineColor;
            }

            set { if (m_outlineColor.Compare(value)) return; SetOutlineColor(value); m_havePropertiesChanged = true; m_outlineColor = value; SetVerticesDirty(); }
        }
        //[SerializeField]
        protected Color32 m_outlineColor = Color.black;


        /// <summary>
        /// Sets the thickness of the outline of the font. Setting this value will result in an instance of the material.
        /// </summary>
        public float outlineWidth
        {
            get
            {
                if (m_sharedMaterial == null) return m_outlineWidth;

                m_outlineWidth = m_sharedMaterial.GetFloat(ShaderUtilities.ID_OutlineWidth);
                return m_outlineWidth;
            }
            set { if (m_outlineWidth == value) return; SetOutlineThickness(value); m_havePropertiesChanged = true; m_outlineWidth = value; SetVerticesDirty(); }
        }
        protected float m_outlineWidth = 0.0f;


        /// <summary>
        /// The point size of the font.
        /// </summary>
        public float fontSize
        {
            get { return m_fontSize; }
            set { if (m_fontSize == value) return; m_havePropertiesChanged = true; m_fontSize = value; if (!m_enableAutoSizing) m_fontSizeBase = m_fontSize; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected float m_fontSize = -99; // Font Size
        protected float m_currentFontSize; // Temporary Font Size affected by tags
        [SerializeField] // TODO: Review if this should be serialized
        protected float m_fontSizeBase = 36;
        protected TMP_TextProcessingStack<float> m_sizeStack = new(16);


        /// <summary>
        /// Enable text auto-sizing
        /// </summary>
        public bool enableAutoSizing
        {
            get { return m_enableAutoSizing; }
            set { if (m_enableAutoSizing == value) return; m_enableAutoSizing = value; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected bool m_enableAutoSizing;
        protected float m_maxFontSize; // Used in conjunction with auto-sizing
        protected float m_minFontSize; // Used in conjunction with auto-sizing
        protected int m_AutoSizeIterationCount;
        protected int m_AutoSizeMaxIterationCount = 100;

        protected bool m_IsAutoSizePointSizeSet;


        /// <summary>
        /// Minimum point size of the font when text auto-sizing is enabled.
        /// </summary>
        public float fontSizeMin
        {
            get { return m_fontSizeMin; }
            set { if (m_fontSizeMin == value) return; m_fontSizeMin = value; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected float m_fontSizeMin = 0; // Text Auto Sizing Min Font Size.


        /// <summary>
        /// Maximum point size of the font when text auto-sizing is enabled.
        /// </summary>
        public float fontSizeMax
        {
            get { return m_fontSizeMax; }
            set { if (m_fontSizeMax == value) return; m_fontSizeMax = value; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected float m_fontSizeMax = 0; // Text Auto Sizing Max Font Size.


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
        protected TMP_FontStyleStack m_fontStyleStack;

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

        /// <summary>
        /// Text alignment options
        /// </summary>
        public TextAlignmentOptions alignment
        {
            get => (TextAlignmentOptions)((int)m_HorizontalAlignment | (int)m_VerticalAlignment);
            set
            {
                var horizontalAlignment = (HorizontalAlignmentOptions)((int)value & 0xFF);
                var verticalAlignment = (VerticalAlignmentOptions)((int)value & 0xFF00);

                if (m_HorizontalAlignment == horizontalAlignment && m_VerticalAlignment == verticalAlignment)
                    return;

                m_HorizontalAlignment = horizontalAlignment;
                m_VerticalAlignment = verticalAlignment;
                m_havePropertiesChanged = true;
                SetVerticesDirty();
            }
        }

        protected HorizontalAlignmentOptions m_lineJustification;
        protected TMP_TextProcessingStack<HorizontalAlignmentOptions> m_lineJustificationStack = new(new HorizontalAlignmentOptions[16]);

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
        protected float m_lineSpacingDelta = 0; // Used with Text Auto Sizing feature
        protected float m_lineHeight = TMP_Math.FLOAT_UNSET; // Used with the <line-height=xx.x> tag.
        protected bool m_IsDrivenLineSpacing;


        /// <summary>
        /// The amount of potential line spacing adjustment before text auto sizing kicks in.
        /// </summary>
        public float lineSpacingAdjustment
        {
            get { return m_lineSpacingMax; }
            set { if (m_lineSpacingMax == value) return; m_havePropertiesChanged = true; m_lineSpacingMax = value; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected float m_lineSpacingMax = 0; // Text Auto Sizing Max Line spacing reduction.
        //protected bool m_forceLineBreak;

        /// <summary>
        /// Percentage the width of characters can be adjusted before text auto-sizing begins to reduce the point size.
        /// </summary>
        public float characterWidthAdjustment
        {
            get { return m_charWidthMaxAdj; }
            set { if (m_charWidthMaxAdj == value) return; m_havePropertiesChanged = true; m_charWidthMaxAdj = value; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected float m_charWidthMaxAdj = 0f; // Text Auto Sizing Max Character Width reduction.
        protected float m_charWidthAdjDelta = 0;


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


        /// <summary>
        /// Property indicating whether the text is Truncated or using Ellipsis.
        /// </summary>
        public bool isTextTruncated { get { return m_isTextTruncated; } }
        //[SerializeField]
        protected bool m_isTextTruncated;


        /// <summary>
        /// Determines if kerning is enabled or disabled.
        /// </summary>
        public bool enableKerning
        {
            get { return m_enableKerning; }
            set { if (m_enableKerning == value) return; m_havePropertiesChanged = true; m_enableKerning = value; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected bool m_enableKerning;
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
        /// Enables or Disables Rich Text Tags
        /// </summary>
        public bool richText
        {
            get { return m_isRichText; }
            set { if (m_isRichText == value) return; m_isRichText = value; m_havePropertiesChanged = true; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected bool m_isRichText = true; // Used to enable or disable Rich Text.


        /// <summary>
        /// Sets Perspective Correction to Zero for Orthographic Camera mode & 0.875f for Perspective Camera mode.
        /// </summary>
        public bool isOrthographic
        {
            get { return m_isOrthographic; }
            set { if (m_isOrthographic == value) return; m_havePropertiesChanged = true; m_isOrthographic = value; SetVerticesDirty(); }
        }
        [SerializeField]
        protected bool m_isOrthographic = false;


        /// <summary>
        /// Sets the culling on the shaders. Note changing this value will result in an instance of the material.
        /// </summary>
        public bool enableCulling
        {
            get { return m_isCullingEnabled; }
            set { if (m_isCullingEnabled == value) return; m_isCullingEnabled = value; SetCulling(); m_havePropertiesChanged = true; }
        }
        [SerializeField]
        protected bool m_isCullingEnabled = false;

        //
        protected bool m_isMaskingEnabled;
        protected bool isMaskUpdateRequired;

        /// <summary>
        /// Determines if a text object will be excluded from the InternalUpdate callback used to handle updates of SDF Scale when the scale of the text object or parent(s) changes.
        /// </summary>
        public bool isTextObjectScaleStatic
        {
            get { return m_IsTextObjectScaleStatic; }
            set
            {
                m_IsTextObjectScaleStatic = value;

                if (m_IsTextObjectScaleStatic)
                    TMP_UpdateManager.UnRegisterTextObjectForUpdate(this);
                else
                    TMP_UpdateManager.RegisterTextObjectForUpdate(this);
            }
        }
        [SerializeField]
        protected bool m_IsTextObjectScaleStatic;

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
        /// Returns are reference to the Transform
        /// </summary>
        public new RectTransform transform => m_transform ??= (RectTransform) base.transform;
        [NonSerialized] protected RectTransform m_transform;


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
        public virtual float preferredWidth { get { m_preferredWidth = GetPreferredWidth(); return m_preferredWidth; } }
        protected float m_preferredWidth;
        protected bool m_isPreferredWidthDirty;

        /// <summary>
        /// Computed preferred height of the text object.
        /// </summary>
        public virtual float preferredHeight { get { m_preferredHeight = GetPreferredHeight(); return m_preferredHeight; } }
        protected float m_preferredHeight;
        protected bool m_isPreferredHeightDirty;

        protected bool m_isCalculatingPreferredValues;


        protected bool m_isLayoutDirty;

        protected bool m_isAwake;

        protected struct CharacterSubstitution
        {
            public int index;
            public uint unicode;

            public CharacterSubstitution (int index, uint unicode)
            {
                this.index = index;
                this.unicode = unicode;
            }
        }

        // Protected Fields
        internal enum TextInputSources { TextInputBox = 0, SetText = 1, SetTextArray = 2, TextString = 3 };
        //[SerializeField]
        internal TextInputSources m_inputSource;

        private static char[] m_htmlTag = new char[128]; // Maximum length of rich text tag. This is pre-allocated to avoid GC.
        private static RichTextTagAttribute[] m_xmlAttribute = new RichTextTagAttribute[8];

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

        // Structures used to save the state of the text layout in conjunction with line breaking / word wrapping.
        protected static WordWrapState m_SavedWordWrapState = new();
        protected static WordWrapState m_SavedLineState = new();
        protected static WordWrapState m_SavedLastValidState = new();
        protected static WordWrapState m_SavedSoftLineBreakState = new();

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
        protected TMP_TextProcessingStack<Color32> m_colorStack = new(new Color32[16]);

        protected TMP_TextProcessingStack<int> m_ItalicAngleStack = new(new int[16]);
        protected int m_ItalicAngle;

        protected float m_padding = 0;
        protected float m_baselineOffset; // Used for superscript and subscript.
        protected TMP_TextProcessingStack<float> m_baselineOffsetStack = new(new float[16]);
        protected float m_xAdvance; // Tracks x advancement from character to character.

        protected TMP_TextElement m_cached_TextElement; // Glyph / Character information is cached into this variable which is faster than having to fetch from the Dictionary multiple times.

        // Profiler Marker declarations
        private static ProfilerMarker k_ParseTextMarker = new("TMP Parse Text");
        private static ProfilerMarker k_InsertNewLineMarker = new("TMP.InsertNewLine");

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
        /// Method which returns an array containing the materials used by the text object.
        /// </summary>
        /// <returns></returns>
        protected abstract Material[] GetSharedMaterials();

        /// <summary>
        ///
        /// </summary>
        protected abstract void SetSharedMaterials(Material[] materials);

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
        /// Function called internally to set the face color of the material. This will results in an instance of the material.
        /// </summary>
        /// <param name="color"></param>
        protected abstract void SetFaceColor(Color32 color);

        /// <summary>
        /// Function called internally to set the outline color of the material. This will results in an instance of the material.
        /// </summary>
        /// <param name="color"></param>
        protected abstract void SetOutlineColor(Color32 color);

        /// <summary>
        /// Function called internally to set the outline thickness property of the material. This will results in an instance of the material.
        /// </summary>
        /// <param name="thickness"></param>
        protected abstract void SetOutlineThickness(float thickness);

        /// <summary>
        /// Set the culling mode on the material.
        /// </summary>
        protected abstract void SetCulling();

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

            if (m_sharedMaterial == null) return 0;

            m_padding = ShaderUtilities.GetPadding(m_sharedMaterial, m_enableExtraPadding, m_isUsingBold);
            m_isMaskingEnabled = ShaderUtilities.IsMaskingEnabled(m_sharedMaterial);
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
            m_isMaskingEnabled = ShaderUtilities.IsMaskingEnabled(m_sharedMaterial);
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

                // Handle surrogate pair conversion in string, StringBuilder and char[] source.
                if (c is >= CodePoint.HIGH_SURROGATE_START and <= CodePoint.HIGH_SURROGATE_END && srcLength > readIndex + 1 && m_TextBackingArray[readIndex + 1] >= CodePoint.LOW_SURROGATE_START && m_TextBackingArray[readIndex + 1] <= CodePoint.LOW_SURROGATE_END)
                {
                    if (writeIndex == m_TextProcessingArray.Length) ResizeInternalArray(ref m_TextProcessingArray);

                    m_TextProcessingArray[writeIndex].unicode = (int)TMP_TextParsingUtilities.ConvertToUTF32(c, m_TextBackingArray[readIndex + 1]);

                    readIndex += 1;
                    writeIndex += 1;
                    continue;
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
        /// Method to calculate the preferred width of a text object.
        /// </summary>
        /// <returns></returns>
        float GetPreferredWidth()
        {
            // Return cached preferred height if already computed
            if (!m_isPreferredWidthDirty)
                return m_preferredWidth;

            var fontSize = m_enableAutoSizing ? m_fontSizeMax : m_fontSize;

            // Reset auto sizing point size bounds
            m_minFontSize = m_fontSizeMin;
            m_maxFontSize = m_fontSizeMax;
            m_charWidthAdjDelta = 0;

            // Set Margins to Infinity
            var margin = k_LargePositiveVector2;

            m_isCalculatingPreferredValues = true;
            ParseInputText();

            m_AutoSizeIterationCount = 0;
            var preferredWidth = CalculatePreferredValues(ref fontSize, margin, false, false).x;

            m_isPreferredWidthDirty = false;

            return preferredWidth;
        }

        /// <summary>
        /// Method to calculate the preferred height of a text object.
        /// </summary>
        /// <returns></returns>
        float GetPreferredHeight()
        {
            // Return cached preferred height if already computed
            if (!m_isPreferredHeightDirty)
                return m_preferredHeight;

            var fontSize = m_enableAutoSizing ? m_fontSizeMax : m_fontSize;

            // Reset auto sizing point size bounds
            m_minFontSize = m_fontSizeMin;
            m_maxFontSize = m_fontSizeMax;
            m_charWidthAdjDelta = 0;

            Vector2 margin = new Vector2(m_marginWidth != 0 ? m_marginWidth : k_LargePositiveFloat, k_LargePositiveFloat);

            m_isCalculatingPreferredValues = true;
            ParseInputText();

            // Reset Text Auto Size iteration tracking.
            m_IsAutoSizePointSizeSet = false;
            m_AutoSizeIterationCount = 0;

            // The CalculatePreferredValues function is potentially called repeatedly when text auto size is enabled.
            // This is a revised implementation to remove the use of recursion which could potentially result in stack overflow issues.
            float preferredHeight = 0;

            while (m_IsAutoSizePointSizeSet == false)
            {
                preferredHeight = CalculatePreferredValues(ref fontSize, margin, m_enableAutoSizing, m_enableWordWrapping).y;
                m_AutoSizeIterationCount += 1;
            }

            m_isPreferredHeightDirty = false;

            //Debug.Log("GetPreferredHeight() called on Object ID: " + GetInstanceID() + " on frame: " + Time.frameCount +". Returning height of " + preferredHeight);

            return preferredHeight;
        }


        /// <summary>
        /// Method to calculate the preferred width and height of the text object.
        /// </summary>
        /// <returns></returns>
        Vector2 CalculatePreferredValues(ref float fontSize, Vector2 marginSize, bool isTextAutoSizingEnabled, bool isWordWrappingEnabled)
        {
            //Debug.Log("*** CalculatePreferredValues() ***"); // ***** Frame: " + Time.frameCount);

            // Early exit if no font asset was assigned. This should not be needed since LiberationSans SDF will be assigned by default.
            if (m_fontAsset == null || m_fontAsset.characterLookupTable == null)
            {
                Debug.LogWarning("Can't Generate Mesh! No Font Asset has been assigned to Object ID: " + this.GetInstanceID());

                m_IsAutoSizePointSizeSet = true;
                return Vector2.zero;
            }

            // Early exit if we don't have any Text to generate.
            if (m_TextProcessingArray == null || m_TextProcessingArray.Length == 0 || m_TextProcessingArray[0].unicode == (char)0)
            {
                m_IsAutoSizePointSizeSet = true;
                return Vector2.zero;
            }

            m_currentFontAsset = m_fontAsset;
            m_currentMaterial = m_sharedMaterial;
            m_currentMaterialIndex = 0;
            m_materialReferenceStack.SetDefault(new MaterialReference(m_currentFontAsset, m_currentMaterial));

            // Total character count is computed when the text is parsed.
            int totalCharacterCount = m_totalCharacterCount; // m_VisibleCharacters.Count;

            if (m_internalCharacterInfo == null || totalCharacterCount > m_internalCharacterInfo.Length)
                m_internalCharacterInfo = new TMP_CharacterInfo[totalCharacterCount > 1024 ? totalCharacterCount + 256 : Mathf.NextPowerOfTwo(totalCharacterCount)];

            // Calculate the scale of the font based on selected font size and sampling point size.
            // baseScale is calculated using the font asset assigned to the text object.
            float baseScale = (fontSize / m_fontAsset.faceInfo.pointSize * m_fontAsset.faceInfo.scale * (m_isOrthographic ? 1 : 0.1f));
            float currentEmScale = fontSize * 0.01f * (m_isOrthographic ? 1 : 0.1f);

            m_currentFontSize = fontSize;
            m_sizeStack.SetDefault(m_currentFontSize);
            float fontSizeDelta = 0;

            m_FontStyleInternal = m_fontStyle; // Set the default style.

            m_lineJustification = m_HorizontalAlignment; // m_textAlignment; // Sets the line justification mode to match editor alignment.
            m_lineJustificationStack.SetDefault(m_lineJustification);

            m_baselineOffset = 0; // Used by subscript characters.
            m_baselineOffsetStack.Clear();

            m_lineOffset = 0; // Amount of space between lines (font line spacing + m_linespacing).
            m_lineHeight = TMP_Math.FLOAT_UNSET;
            float lineGap = m_currentFontAsset.faceInfo.lineHeight - (m_currentFontAsset.faceInfo.ascentLine - m_currentFontAsset.faceInfo.descentLine);

            m_cSpacing = 0; // Amount of space added between characters as a result of the use of the <cspace> tag.
            m_monoSpacing = 0;
            m_xAdvance = 0; // Used to track the position of each character.
            float maxXAdvance = 0; // Used to determine Preferred Width.

            m_characterCount = 0; // Total characters in the char[]


            // Tracking of line information
            m_firstCharacterOfLine = 0;
            m_maxLineAscender = k_LargeNegativeFloat;
            m_maxLineDescender = k_LargePositiveFloat;
            m_lineNumber = 0;
            m_startOfLineAscender = 0;
            m_IsDrivenLineSpacing = false;

            float marginWidth = marginSize.x;

            m_width = -1;

            // Used by Unity's Auto Layout system.
            float renderedWidth = 0;
            float renderedHeight = 0;
            float textWidth = 0;
            m_isCalculatingPreferredValues = true;

            // Tracking of the highest Ascender
            m_maxCapHeight = 0;
            m_maxTextAscender = 0;
            m_ElementDescender = 0;

            // Initialize struct to track states of word wrapping
            bool isFirstWordOfLine = true;
            bool isLastCharacterCJK = false;

            CharacterSubstitution characterToSubstitute = new CharacterSubstitution(-1, 0);
            bool isSoftHyphenIgnored = false;

            WordWrapState internalWordWrapState = new WordWrapState();
            WordWrapState internalLineState = new WordWrapState();
            WordWrapState internalSoftLineBreak = new WordWrapState();

            // Counter to prevent recursive lockup when computing preferred values.
            m_AutoSizeIterationCount += 1;

            // Parse through Character buffer to read HTML tags and begin creating mesh.
            for (int i = 0; i < m_TextProcessingArray.Length && m_TextProcessingArray[i].unicode != 0; i++)
            {
                int charCode = m_TextProcessingArray[i].unicode;

                // Parse Rich Text Tag
                #region Parse Rich Text Tag
                if (m_isRichText && charCode == 60)  // '<'
                {
                    int endTagIndex;

                    // Check if Tag is valid. If valid, skip to the end of the validated tag.
                    if (ValidateHtmlTag(m_TextProcessingArray, i + 1, out endTagIndex))
                    {
                        i = endTagIndex;

                        // Continue to next character or handle the sprite element
                        continue;
                    }
                }
                else
                {
                    m_currentMaterialIndex = m_textInfo.characterInfo[m_characterCount].materialReferenceIndex;
                    m_currentFontAsset = m_textInfo.characterInfo[m_characterCount].fontAsset;
                }
                #endregion End Parse Rich Text Tag

                // Handle potential character substitutions
                #region Character Substitutions
                bool isInjectingCharacter = false;

                if (characterToSubstitute.index == m_characterCount)
                {
                    charCode = (int)characterToSubstitute.unicode;
                    isInjectingCharacter = true;

                    switch (charCode)
                    {
                        case 0x03:
                            m_internalCharacterInfo[m_characterCount].textElement = m_currentFontAsset.characterLookupTable[0x03];
                            m_isTextTruncated = true;
                            break;
                        case 0x2D:
                            //
                            break;
                        case 0x2026:
                            throw new NotSupportedException();
                    }
                }
                #endregion


                // Look up Character Data from Dictionary and cache it.
                #region Look up Character Data
                float elementAscentLine = 0;
                float elementDescentLine = 0;
                float currentElementScale;
                {
                    m_cached_TextElement = m_textInfo.characterInfo[m_characterCount].textElement;
                    if (m_cached_TextElement == null) continue;

                    m_currentMaterialIndex = m_textInfo.characterInfo[m_characterCount].materialReferenceIndex;

                    float adjustedScale;
                    if (isInjectingCharacter && m_TextProcessingArray[i].unicode == 0x0A && m_characterCount != m_firstCharacterOfLine)
                        adjustedScale = m_textInfo.characterInfo[m_characterCount - 1].pointSize * 1.0f / m_currentFontAsset.m_FaceInfo.pointSize * m_currentFontAsset.m_FaceInfo.scale * (m_isOrthographic ? 1 : 0.1f);
                    else
                        adjustedScale = m_currentFontSize * 1.0f / m_currentFontAsset.m_FaceInfo.pointSize * m_currentFontAsset.m_FaceInfo.scale * (m_isOrthographic ? 1 : 0.1f);

                    // Special handling for injected Ellipsis
                    if (isInjectingCharacter && charCode == 0x2026)
                    {
                        elementAscentLine = 0;
                        elementDescentLine = 0;
                    }
                    else
                    {
                        elementAscentLine = m_currentFontAsset.m_FaceInfo.ascentLine;
                        elementDescentLine = m_currentFontAsset.m_FaceInfo.descentLine;
                    }

                    currentElementScale = adjustedScale;
                }
                #endregion


                // Handle Soft Hyphen
                #region Handle Soft Hyphen
                float currentElementUnmodifiedScale = currentElementScale;
                if (charCode == 0xAD || charCode == 0x03)
                    currentElementScale = 0;
                #endregion


                // Store some of the text object's information
                m_internalCharacterInfo[m_characterCount].character = (char)charCode;

                // Cache glyph metrics
                GlyphMetrics currentGlyphMetrics = m_cached_TextElement.m_Glyph.metrics;

                // Optimization to avoid calling this more than once per character.
                bool isWhiteSpace = charCode <= 0xFFFF && char.IsWhiteSpace((char)charCode);


                // Handle Kerning if Enabled.
                #region Handle Kerning
                TMP_GlyphValueRecord glyphAdjustments = new TMP_GlyphValueRecord();
                float characterSpacingAdjustment = m_characterSpacing;
                m_GlyphHorizontalAdvanceAdjustment = 0;
                if (m_enableKerning)
                    m_GlyphHorizontalAdvanceAdjustment = glyphAdjustments.m_XAdvance;
                #endregion


                // Handle Mono Spacing
                #region Handle Mono Spacing
                float monoAdvance = 0;
                if (m_monoSpacing != 0)
                {
                    monoAdvance = (m_monoSpacing / 2 - (m_cached_TextElement.glyph.metrics.width / 2 + m_cached_TextElement.glyph.metrics.horizontalBearingX) * currentElementScale) * (1 - m_charWidthAdjDelta);
                    m_xAdvance += monoAdvance;
                }
                #endregion


                // Set Padding based on selected font style
                #region Handle Style Padding
                float boldSpacingAdjustment = 0;
                if ((m_FontStyleInternal & FontStyles.Bold) == FontStyles.Bold) // Checks for any combination of Bold Style.
                    boldSpacingAdjustment = m_currentFontAsset.boldSpacing;
                #endregion Handle Style Padding

                // Compute text metrics
                #region Compute Ascender & Descender values
                // Element Ascender in line space
                float elementAscender = elementAscentLine * currentElementScale + m_baselineOffset;

                // Element Descender in line space
                float elementDescender = elementDescentLine * currentElementScale + m_baselineOffset;

                float adjustedAscender = elementAscender;
                float adjustedDescender = elementDescender;

                bool isFirstCharacterOfLine = m_characterCount == m_firstCharacterOfLine;
                // Max line ascender and descender in line space
                if (isFirstCharacterOfLine || isWhiteSpace == false)
                {
                    // Special handling for Superscript and Subscript where we use the unadjusted line ascender and descender
                    if (m_baselineOffset != 0)
                    {
                        adjustedAscender = Mathf.Max(elementAscender - m_baselineOffset, adjustedAscender);
                        adjustedDescender = Mathf.Min(elementDescender - m_baselineOffset, adjustedDescender);
                    }

                    m_maxLineAscender = Mathf.Max(adjustedAscender, m_maxLineAscender);
                    m_maxLineDescender = Mathf.Min(adjustedDescender, m_maxLineDescender);
                }

                // Element Ascender and Descender in object space
                if (isFirstCharacterOfLine || isWhiteSpace == false)
                {
                    m_internalCharacterInfo[m_characterCount].adjustedAscender = adjustedAscender;
                    m_internalCharacterInfo[m_characterCount].adjustedDescender = adjustedDescender;

                    m_ElementDescender = elementDescender - m_lineOffset;
                }
                else
                {
                    m_internalCharacterInfo[m_characterCount].adjustedAscender = m_maxLineAscender;
                    m_internalCharacterInfo[m_characterCount].adjustedDescender = m_maxLineDescender;

                    m_ElementDescender = m_maxLineDescender - m_lineOffset;
                }

                // Max text object ascender and cap height
                if (m_lineNumber == 0)
                {
                    if (isFirstCharacterOfLine || isWhiteSpace == false)
                    {
                        m_maxTextAscender = m_maxLineAscender;
                        m_maxCapHeight = Mathf.Max(m_maxCapHeight, m_currentFontAsset.m_FaceInfo.capLine * currentElementScale);
                    }
                }

                // Page ascender
                if (m_lineOffset == 0)
                {
                    if (!isWhiteSpace || m_characterCount == m_firstCharacterOfLine)
                        m_PageAscender = m_PageAscender > elementAscender ? m_PageAscender : elementAscender;
                }
                #endregion

                // Setup Mesh for visible text elements. ie. not a SPACE / LINEFEED / CARRIAGE RETURN.
                #region Handle Visible Characters
                if (charCode == 9 || (isWhiteSpace == false && charCode != 0x200B && charCode != 0xAD && charCode != 0x03) || (charCode == 0xAD && isSoftHyphenIgnored == false))
                {
                    var widthOfTextArea = m_width != -1 ? Mathf.Min(marginWidth + 0.0001f, m_width) : marginWidth + 0.0001f;

                    // Calculate the line breaking width of the text.
                    textWidth = Mathf.Abs(m_xAdvance) + currentGlyphMetrics.horizontalAdvance * (1 - m_charWidthAdjDelta) * (charCode == 0xAD ? currentElementUnmodifiedScale : currentElementScale);

                    // Handling of Horizontal Bounds
                    #region Current Line Horizontal Bounds Check
                    if (textWidth > widthOfTextArea)
                    {
                        // Handle Line Breaking (if still possible)
                        if (isWordWrappingEnabled && m_characterCount != m_firstCharacterOfLine) // && isFirstWord == false)
                        {
                            // Restore state to previous safe line breaking
                            i = RestoreWordWrappingState(ref internalWordWrapState);

                            // Replace Soft Hyphen by Hyphen Minus 0x2D
                            #region Handle Soft Hyphenation
                            if (m_internalCharacterInfo[m_characterCount - 1].character == 0xAD && isSoftHyphenIgnored == false && m_overflowMode == TextOverflowModes.Overflow)
                            {
                                characterToSubstitute.index = m_characterCount - 1;
                                characterToSubstitute.unicode = 0x2D;

                                i -= 1;
                                m_characterCount -= 1;
                                continue;
                            }

                            isSoftHyphenIgnored = false;

                            // Ignore Soft Hyphen to prevent it from wrapping
                            if (m_internalCharacterInfo[m_characterCount].character == 0xAD)
                            {
                                isSoftHyphenIgnored = true;
                                continue;
                            }
                            #endregion

                            // Adjust character spacing before breaking up word if auto size is enabled
                            #region Handle Text Auto Size (if word wrapping is no longer possible)
                            if (isTextAutoSizingEnabled && isFirstWordOfLine)
                            {
                                // Handle Character Width Adjustments
                                #region Character Width Adjustments
                                if (m_charWidthAdjDelta < m_charWidthMaxAdj / 100 && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                                {
                                    float adjustedTextWidth = textWidth;

                                    // Determine full width of the text
                                    if (m_charWidthAdjDelta > 0)
                                        adjustedTextWidth /= 1f - m_charWidthAdjDelta;

                                    float adjustmentDelta = textWidth - (widthOfTextArea - 0.0001f);
                                    m_charWidthAdjDelta += adjustmentDelta / adjustedTextWidth;
                                    m_charWidthAdjDelta = Mathf.Min(m_charWidthAdjDelta, m_charWidthMaxAdj / 100);

                                    //Debug.Log("[" + m_AutoSizeIterationCount + "] Reducing Character Width by " + (m_charWidthAdjDelta * 100) + "%");
                                    return Vector2.zero;
                                }
                                #endregion

                                // Handle Text Auto-sizing resulting from text exceeding vertical bounds.
                                #region Text Auto-Sizing (Text greater than vertical bounds)
                                if (fontSize > m_fontSizeMin && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                                {
                                    m_maxFontSize = fontSize;

                                    float sizeDelta = Mathf.Max((fontSize - m_minFontSize) / 2, 0.05f);
                                    fontSize -= sizeDelta;
                                    fontSize = Mathf.Max((int)(fontSize * 20 + 0.5f) / 20f, m_fontSizeMin);

                                    //Debug.Log("[" + m_AutoSizeIterationCount + "] Reducing Point Size from [" + m_maxFontSize.ToString("f3") + "] to [" + m_fontSize.ToString("f3") + "] with delta of [" + sizeDelta.ToString("f3") + "].");
                                    return Vector2.zero;
                                }
                                #endregion Text Auto-Sizing
                            }
                            #endregion

                            // Adjust line spacing if necessary
                            float baselineAdjustmentDelta = m_maxLineAscender - m_startOfLineAscender;
                            if (m_lineOffset > 0 && Math.Abs(baselineAdjustmentDelta) > 0.01f && m_IsDrivenLineSpacing == false)
                            {
                                //AdjustLineOffset(m_firstCharacterOfLine, m_characterCount, baselineAdjustmentDelta);
                                m_ElementDescender -= baselineAdjustmentDelta;
                                m_lineOffset += baselineAdjustmentDelta;
                            }

                            // Calculate line ascender and make sure if last character is superscript or subscript that we check that as well.
                            float lineAscender = m_maxLineAscender - m_lineOffset;
                            float lineDescender = m_maxLineDescender - m_lineOffset;

                            // Update maxDescender and maxVisibleDescender
                            m_ElementDescender = m_ElementDescender < lineDescender ? m_ElementDescender : lineDescender;

                            // Store first character of the next line.
                            m_firstCharacterOfLine = m_characterCount;
                            m_lineVisibleCharacterCount = 0;

                            // Compute Preferred Width & Height
                            renderedWidth += m_xAdvance;

                            if (isWordWrappingEnabled)
                                renderedHeight = m_maxTextAscender - m_ElementDescender;
                            else
                                renderedHeight = Mathf.Max(renderedHeight, lineAscender - lineDescender);

                            // Store the state of the line before starting on the new line.
                            SaveWordWrappingState(ref internalLineState, i, m_characterCount - 1);

                            m_lineNumber += 1;

                            float ascender = m_internalCharacterInfo[m_characterCount].adjustedAscender;

                            // Compute potential new line offset in the event a line break is needed.
                            if (m_lineHeight == TMP_Math.FLOAT_UNSET)
                            {
                                m_lineOffset += 0 - m_maxLineDescender + ascender + (lineGap + m_lineSpacingDelta) * baseScale + m_lineSpacing * currentEmScale;
                                m_IsDrivenLineSpacing = false;
                            }
                            else
                            {
                                m_lineOffset += m_lineHeight + m_lineSpacing * currentEmScale;
                                m_IsDrivenLineSpacing = true;
                            }

                            m_maxLineAscender = k_LargeNegativeFloat;
                            m_maxLineDescender = k_LargePositiveFloat;
                            m_startOfLineAscender = ascender;

                            m_xAdvance = 0;
                            //isStartOfNewLine = true;
                            isFirstWordOfLine = true;
                            continue;
                        }
                    }
                    #endregion

                }
                #endregion Handle Visible Characters


                // Handle xAdvance & Tabulation Stops. Tab stops at every 25% of Font Size.
                #region XAdvance, Tabulation & Stops
                if (charCode == 9)
                {
                    float tabSize = m_currentFontAsset.faceInfo.tabWidth * m_currentFontAsset.tabSize * currentElementScale;
                    float tabs = Mathf.Ceil(m_xAdvance / tabSize) * tabSize;
                    m_xAdvance = tabs > m_xAdvance ? tabs : m_xAdvance + tabSize;
                }
                else if (m_monoSpacing != 0)
                {
                    m_xAdvance += (m_monoSpacing - monoAdvance + ((m_currentFontAsset.normalSpacingOffset + characterSpacingAdjustment) * currentEmScale) + m_cSpacing) * (1 - m_charWidthAdjDelta);
                }
                else
                {
                    m_xAdvance += ((currentGlyphMetrics.horizontalAdvance + glyphAdjustments.xAdvance) * currentElementScale + (m_currentFontAsset.normalSpacingOffset + characterSpacingAdjustment + boldSpacingAdjustment) * currentEmScale + m_cSpacing) * (1 - m_charWidthAdjDelta);
                }
                #endregion Tabulation & Stops


                // Handle Carriage Return
                #region Carriage Return
                if (charCode == 13)
                {
                    maxXAdvance = Mathf.Max(maxXAdvance, renderedWidth + m_xAdvance);
                    renderedWidth = 0;
                    m_xAdvance = 0;
                }
                #endregion Carriage Return


                // Handle Line Spacing Adjustments + Word Wrapping & special case for last line.
                #region Check for Line Feed and Last Character
                if (charCode == 10 || charCode == 11 || charCode == 0x03 || charCode == 0x2028 || charCode == 0x2029 || m_characterCount == totalCharacterCount - 1)
                {
                    // Check if Line Spacing of previous line needs to be adjusted.
                    float baselineAdjustmentDelta = m_maxLineAscender - m_startOfLineAscender;
                    if (m_lineOffset > 0 && Math.Abs(baselineAdjustmentDelta) > 0.01f && m_IsDrivenLineSpacing == false)
                    {
                        m_ElementDescender -= baselineAdjustmentDelta;
                        m_lineOffset += baselineAdjustmentDelta;
                    }

                    // Calculate lineAscender & make sure if last character is superscript or subscript that we check that as well.
                    //float lineAscender = m_maxLineAscender - m_lineOffset;
                    float lineDescender = m_maxLineDescender - m_lineOffset;

                    // Update maxDescender and maxVisibleDescender
                    m_ElementDescender = m_ElementDescender < lineDescender ? m_ElementDescender : lineDescender;

                    // Store PreferredWidth paying attention to linefeed and last character of text.
                    if (m_characterCount == totalCharacterCount - 1)
                        renderedWidth = Mathf.Max(maxXAdvance, renderedWidth + textWidth);
                    else
                    {
                        maxXAdvance = Mathf.Max(maxXAdvance, renderedWidth + textWidth);
                        renderedWidth = 0;
                    }

                    renderedHeight = m_maxTextAscender - m_ElementDescender;

                    // Add new line if not last lines or character.
                    if (charCode is 10 or 11 or 0x2D or 0x2028 or 0x2029)
                    {
                        // Store the state of the line before starting on the new line.
                        SaveWordWrappingState(ref internalLineState, i, m_characterCount);
                        // Store the state of the last Character before the new line.
                        SaveWordWrappingState(ref internalWordWrapState, i, m_characterCount);

                        m_lineNumber += 1;
                        m_firstCharacterOfLine = m_characterCount + 1;

                        float ascender = m_internalCharacterInfo[m_characterCount].adjustedAscender;

                        // Apply Line Spacing with special handling for VT char(11)
                        if (m_lineHeight == TMP_Math.FLOAT_UNSET)
                        {
                            float lineOffsetDelta = 0 - m_maxLineDescender + ascender + (lineGap + m_lineSpacingDelta) * baseScale + (m_lineSpacing) * currentEmScale;
                            m_lineOffset += lineOffsetDelta;
                            m_IsDrivenLineSpacing = false;
                        }
                        else
                        {
                            m_lineOffset += m_lineHeight + m_lineSpacing * currentEmScale;
                            m_IsDrivenLineSpacing = true;
                        }

                        m_maxLineAscender = k_LargeNegativeFloat;
                        m_maxLineDescender = k_LargePositiveFloat;
                        m_startOfLineAscender = ascender;

                        m_xAdvance = 0;

                        m_characterCount += 1;
                        continue;
                    }

                    // If End of Text
                    if (charCode == 0x03)
                        i = m_TextProcessingArray.Length;
                }
                #endregion Check for Linefeed or Last Character


                // Save State of Mesh Creation for handling of Word Wrapping
                #region Save Word Wrapping State
                if (isWordWrappingEnabled || m_overflowMode == TextOverflowModes.Truncate)
                {
                    if ((isWhiteSpace || charCode == 0x200B || charCode == 0x2D || charCode == 0xAD) && charCode != 0xA0 && charCode != 0x2007 && charCode != 0x2011 && charCode != 0x202F && charCode != 0x2060)
                    {
                        // We store the state of numerous variables for the most recent Space, LineFeed or Carriage Return to enable them to be restored
                        // for Word Wrapping.
                        SaveWordWrappingState(ref internalWordWrapState, i, m_characterCount);
                        isFirstWordOfLine = false;
                        isLastCharacterCJK = false;

                        // Reset soft line breaking point since we now have a valid hard break point.
                        internalSoftLineBreak.previous_WordBreak = -1;
                    }
                    // Handling for East Asian languages
                    else if (TMP_TextUtilities.IsChineseOrJapanese(charCode))
                    {
                        if (isFirstWordOfLine)
                        {
                            SaveWordWrappingState(ref internalWordWrapState, i, m_characterCount);
                            isFirstWordOfLine = false;
                        }

                        isLastCharacterCJK = true;
                    }
                    else if (isLastCharacterCJK)
                    {
                        SaveWordWrappingState(ref internalWordWrapState, i, m_characterCount);

                        isLastCharacterCJK = false;
                    }
                    else if (isFirstWordOfLine)
                    {
                        // Special handling for non-breaking space and soft line breaks
                        if (isWhiteSpace || (charCode == 0xAD && isSoftHyphenIgnored == false))
                            SaveWordWrappingState(ref internalSoftLineBreak, i, m_characterCount);

                        SaveWordWrappingState(ref internalWordWrapState, i, m_characterCount);
                        isLastCharacterCJK = false;
                    }
                }
                #endregion Save Word Wrapping State

                m_characterCount += 1;
            }

            // Check Auto Sizing and increase font size to fill text container.
            #region Check Auto-Sizing (Upper Font Size Bounds)
            fontSizeDelta = m_maxFontSize - m_minFontSize;
            if (isTextAutoSizingEnabled && fontSizeDelta > 0.051f && fontSize < m_fontSizeMax && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
            {
                // Reset character width adjustment delta
                if (m_charWidthAdjDelta < m_charWidthMaxAdj / 100)
                    m_charWidthAdjDelta = 0;

                m_minFontSize = fontSize;

                float sizeDelta = Mathf.Max((m_maxFontSize - fontSize) / 2, 0.05f);
                fontSize += sizeDelta;
                fontSize = Mathf.Min((int)(fontSize * 20 + 0.5f) / 20f, m_fontSizeMax);

                //Debug.Log("[" + m_AutoSizeIterationCount + "] Increasing Point Size from [" + m_minFontSize.ToString("f3") + "] to [" + m_fontSize.ToString("f3") + "] with delta of [" + sizeDelta.ToString("f3") + "].");
                return Vector2.zero;
            }
            #endregion End Auto-sizing Check

            m_IsAutoSizePointSizeSet = true;

            m_isCalculatingPreferredValues = false;

            // Round Preferred Values to nearest 5/100.
            renderedWidth = (int)(renderedWidth * 100 + 1f) / 100f;
            renderedHeight = (int)(renderedHeight * 100 + 1f) / 100f;

            //Debug.Log("Preferred Values: (" + renderedWidth + ", " + renderedHeight + ") with Recursive count of " + m_recursiveCount);

            return new Vector2(renderedWidth, renderedHeight);
        }

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
        /// Function to force an update of the margin size.
        /// </summary>
        protected abstract void ComputeMarginSize();


        protected void InsertNewLine(int i, float baseScale, float currentElementScale, float currentEmScale, float glyphAdjustment, float boldSpacingAdjustment, float characterSpacingAdjustment, float width, float lineGap, ref bool isMaxVisibleDescenderSet, ref float maxVisibleDescender)
        {
            k_InsertNewLineMarker.Begin();

            // Adjust line spacing if necessary
            float baselineAdjustmentDelta = m_maxLineAscender - m_startOfLineAscender;
            if (m_lineOffset > 0 && Math.Abs(baselineAdjustmentDelta) > 0.01f && m_IsDrivenLineSpacing == false)
            {
                AdjustLineOffset(m_firstCharacterOfLine, m_characterCount, baselineAdjustmentDelta);
                m_ElementDescender -= baselineAdjustmentDelta;
                m_lineOffset += baselineAdjustmentDelta;
            }

            // Calculate lineAscender & make sure if last character is superscript or subscript that we check that as well.
            float lineDescender = m_maxLineDescender - m_lineOffset;

            // Update maxDescender and maxVisibleDescender
            m_ElementDescender = m_ElementDescender < lineDescender ? m_ElementDescender : lineDescender;
            if (!isMaxVisibleDescenderSet)
                maxVisibleDescender = m_ElementDescender;

            // Track & Store lineInfo for the new line
            m_textInfo.lineInfo[m_lineNumber].firstCharacterIndex = m_firstCharacterOfLine;
            int lastCharacterIndex = m_textInfo.lineInfo[m_lineNumber].lastCharacterIndex = m_lastCharacterOfLine = m_characterCount - 1 > 0 ? m_characterCount - 1 : 0;
            m_lastVisibleCharacterOfLine = m_lastVisibleCharacterOfLine < m_firstVisibleCharacterOfLine ? m_firstVisibleCharacterOfLine : m_lastVisibleCharacterOfLine;

            m_textInfo.lineInfo[m_lineNumber].characterCount = m_textInfo.lineInfo[m_lineNumber].lastCharacterIndex - m_textInfo.lineInfo[m_lineNumber].firstCharacterIndex + 1;
            m_textInfo.lineInfo[m_lineNumber].width = width;

            float maxAdvanceOffset = (glyphAdjustment * currentElementScale + (m_currentFontAsset.normalSpacingOffset + characterSpacingAdjustment + boldSpacingAdjustment) * currentEmScale - m_cSpacing) * (1 - m_charWidthAdjDelta);
            float adjustedHorizontalAdvance = m_textInfo.lineInfo[m_lineNumber].maxAdvance = m_textInfo.characterInfo[m_lastVisibleCharacterOfLine].xAdvance + (false ? maxAdvanceOffset : - maxAdvanceOffset);
            m_textInfo.characterInfo[lastCharacterIndex].xAdvance = adjustedHorizontalAdvance;

            m_firstCharacterOfLine = m_characterCount; // Store first character of the next line.
            m_lineVisibleCharacterCount = 0;

            // Store the state of the line before starting on the new line.
            SaveWordWrappingState(ref m_SavedLineState, i, m_characterCount - 1);

            m_lineNumber += 1;

            // Check to make sure Array is large enough to hold a new line.
            if (m_lineNumber >= m_textInfo.lineInfo.Length)
                ResizeLineExtents(m_lineNumber);

            // Apply Line Spacing based on scale of the last character of the line.
            if (m_lineHeight == TMP_Math.FLOAT_UNSET)
            {
                float ascender = m_textInfo.characterInfo[m_characterCount].adjustedAscender;
                float lineOffsetDelta = 0 - m_maxLineDescender + ascender + (lineGap + m_lineSpacingDelta) * baseScale + m_lineSpacing * currentEmScale;
                m_lineOffset += lineOffsetDelta;

                m_startOfLineAscender = ascender;
            }
            else
            {
                m_lineOffset += m_lineHeight + m_lineSpacing * currentEmScale;
            }

            m_maxLineAscender = k_LargeNegativeFloat;
            m_maxLineDescender = k_LargePositiveFloat;

            m_xAdvance = 0;
            k_InsertNewLineMarker.End();
        }


        /// <summary>
        /// Save the State of various variables used in the mesh creation loop in conjunction with Word Wrapping
        /// </summary>
        /// <param name="state"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        protected void SaveWordWrappingState(ref WordWrapState state, int index, int count)
        {
            // Multi Font & Material support related
            state.currentFontAsset = m_currentFontAsset;
            state.currentMaterial = m_currentMaterial;
            state.currentMaterialIndex = m_currentMaterialIndex;

            state.previous_WordBreak = index;
            state.total_CharacterCount = count;
            state.visible_CharacterCount = m_lineVisibleCharacterCount;

            state.firstCharacterIndex = m_firstCharacterOfLine;
            state.firstVisibleCharacterIndex = m_firstVisibleCharacterOfLine;
            state.lastVisibleCharIndex = m_lastVisibleCharacterOfLine;

            state.fontStyle = m_FontStyleInternal;
            state.italicAngle = m_ItalicAngle;
            state.currentFontSize = m_currentFontSize;

            state.xAdvance = m_xAdvance;
            state.maxCapHeight = m_maxCapHeight;
            state.maxAscender = m_maxTextAscender;
            state.maxDescender = m_ElementDescender;
            state.startOfLineAscender = m_startOfLineAscender;
            state.maxLineAscender = m_maxLineAscender;
            state.maxLineDescender = m_maxLineDescender;
            state.pageAscender = m_PageAscender;

            state.preferredWidth = m_preferredWidth;
            state.preferredHeight = m_preferredHeight;
            state.meshExtents = m_meshExtents;

            state.lineNumber = m_lineNumber;
            state.lineOffset = m_lineOffset;
            state.baselineOffset = m_baselineOffset;
            state.isDrivenLineSpacing = m_IsDrivenLineSpacing;
            state.glyphHorizontalAdvanceAdjustment = m_GlyphHorizontalAdvanceAdjustment;

            state.cSpace = m_cSpacing;
            state.mSpace = m_monoSpacing;

            state.horizontalAlignment = m_lineJustification;

            state.vertexColor = m_htmlColor;

            // XML Tag Stack
            state.basicStyleStack = m_fontStyleStack;
            state.italicAngleStack = m_ItalicAngleStack;
            state.colorStack = m_colorStack;
            state.sizeStack = m_sizeStack;

            state.baselineStack = m_baselineOffsetStack;
            state.materialReferenceStack = m_materialReferenceStack;
            state.lineJustificationStack = m_lineJustificationStack;

            if (m_lineNumber < m_textInfo.lineInfo.Length)
                state.lineInfo = m_textInfo.lineInfo[m_lineNumber];
        }


        /// <summary>
        /// Restore the State of various variables used in the mesh creation loop.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        protected int RestoreWordWrappingState(ref WordWrapState state)
        {
            int index = state.previous_WordBreak;

            // Multi Font & Material support related
            m_currentFontAsset = state.currentFontAsset;
            m_currentMaterial = state.currentMaterial;
            m_currentMaterialIndex = state.currentMaterialIndex;

            m_characterCount = state.total_CharacterCount + 1;
            m_lineVisibleCharacterCount = state.visible_CharacterCount;

            m_firstCharacterOfLine = state.firstCharacterIndex;
            m_firstVisibleCharacterOfLine = state.firstVisibleCharacterIndex;
            m_lastVisibleCharacterOfLine = state.lastVisibleCharIndex;

            m_FontStyleInternal = state.fontStyle;
            m_ItalicAngle = state.italicAngle;
            m_currentFontSize = state.currentFontSize;

            m_xAdvance = state.xAdvance;
            m_maxCapHeight = state.maxCapHeight;
            m_maxTextAscender = state.maxAscender;
            m_ElementDescender = state.maxDescender;
            m_startOfLineAscender = state.startOfLineAscender;
            m_maxLineAscender = state.maxLineAscender;
            m_maxLineDescender = state.maxLineDescender;
            m_PageAscender = state.pageAscender;

            m_preferredWidth = state.preferredWidth;
            m_preferredHeight = state.preferredHeight;
            m_meshExtents = state.meshExtents;

            m_lineNumber = state.lineNumber;
            m_lineOffset = state.lineOffset;
            m_baselineOffset = state.baselineOffset;
            m_IsDrivenLineSpacing = state.isDrivenLineSpacing;
            m_GlyphHorizontalAdvanceAdjustment = state.glyphHorizontalAdvanceAdjustment;

            m_cSpacing = state.cSpace;
            m_monoSpacing = state.mSpace;

            m_lineJustification = state.horizontalAlignment;

            m_htmlColor = state.vertexColor;

            // XML Tag Stack
            m_fontStyleStack = state.basicStyleStack;
            m_ItalicAngleStack = state.italicAngleStack;
            m_colorStack = state.colorStack;
            m_sizeStack = state.sizeStack;

            m_baselineOffsetStack = state.baselineStack;
            m_materialReferenceStack = state.materialReferenceStack;
            m_lineJustificationStack = state.lineJustificationStack;

            if (m_lineNumber < m_textInfo.lineInfo.Length)
                m_textInfo.lineInfo[m_lineNumber] = state.lineInfo;

            return index;
        }


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
                m_enableKerning = TMP_Settings.enableKerning;
                m_enableExtraPadding = TMP_Settings.enableExtraPadding;
                m_fontSize = m_fontSizeBase = TMP_Settings.defaultFontSize;
                m_fontSizeMin = m_fontSize * TMP_Settings.defaultTextAutoSizingMinRatio;
                m_fontSizeMax = m_fontSize * TMP_Settings.defaultTextAutoSizingMaxRatio;
                m_IsTextObjectScaleStatic = TMP_Settings.isTextObjectScaleStatic;
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


        /// <summary>
        /// Method to convert Hex color values to Color32
        /// </summary>
        /// <param name="hexChars"></param>
        /// <param name="tagCount"></param>
        /// <returns></returns>
        static Color32 HexCharsToColor(char[] hexChars, int tagCount)
        {
            Assert.IsTrue(tagCount is 13 or 15, "Hex color tag must be either 7 or 9 characters long.");

            if (tagCount == 13)
            {
                byte r = (byte)(HexToInt(hexChars[7]) * 16 + HexToInt(hexChars[8]));
                byte g = (byte)(HexToInt(hexChars[9]) * 16 + HexToInt(hexChars[10]));
                byte b = (byte)(HexToInt(hexChars[11]) * 16 + HexToInt(hexChars[12]));

                return new Color32(r, g, b, 255);
            }
            else if (tagCount == 15)
            {
                byte r = (byte)(HexToInt(hexChars[7]) * 16 + HexToInt(hexChars[8]));
                byte g = (byte)(HexToInt(hexChars[9]) * 16 + HexToInt(hexChars[10]));
                byte b = (byte)(HexToInt(hexChars[11]) * 16 + HexToInt(hexChars[12]));
                byte a = (byte)(HexToInt(hexChars[13]) * 16 + HexToInt(hexChars[14]));

                return new Color32(r, g, b, a);
            }

            return new Color32(255, 255, 255, 255);
        }


        /// <summary>
        /// Extracts a float value from char[] given a start index and length.
        /// </summary>
        /// <param name="chars"></param> The Char[] containing the numerical sequence.
        /// <param name="startIndex"></param> The index of the start of the numerical sequence.
        /// <param name="length"></param> The length of the numerical sequence.
        /// Index of the last character in the validated sequence.
        /// <returns></returns>
        static float ConvertToFloat(char[] chars, int startIndex, int length)
        {
            if (startIndex == 0)
            {
                return Int16.MinValue;
            }

            int endIndex = startIndex + length;

            bool isIntegerValue = true;
            float decimalPointMultiplier = 0;

            // Set value multiplier checking the first character to determine if we are using '+' or '-'
            int valueSignMultiplier = 1;
            if (chars[startIndex] == '+')
            {
                valueSignMultiplier = 1;
                startIndex += 1;
            }
            else if (chars[startIndex] == '-')
            {
                valueSignMultiplier = -1;
                startIndex += 1;
            }

            float value = 0;

            for (int i = startIndex; i < endIndex; i++)
            {
                uint c = chars[i];

                if (c is >= '0' and <= '9' or '.')
                {
                    if (c == '.')
                    {
                        isIntegerValue = false;
                        decimalPointMultiplier = 0.1f;
                        continue;
                    }

                    //Calculate integer and floating point value
                    if (isIntegerValue)
                        value = value * 10 + (c - 48) * valueSignMultiplier;
                    else
                    {
                        value += (c - 48) * decimalPointMultiplier * valueSignMultiplier;
                        decimalPointMultiplier *= 0.1f;
                    }
                }
                else if (c == ',')
                {
                    // Make sure value is within reasonable range.
                    if (value > 32767)
                        return Int16.MinValue;

                    return value;
                }
            }

            // Make sure value is within reasonable range.
            if (value > 32767)
                return Int16.MinValue;

            return value;
        }


        /// <summary>
        /// Function to identify and validate the rich tag. Returns the position of the > if the tag was valid.
        /// </summary>
        /// <param name="chars"></param>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        /// <returns></returns>
        internal bool ValidateHtmlTag(UnicodeChar[] chars, int startIndex, out int endIndex)
        {
            int tagCharCount = 0;
            byte attributeFlag = 0;

            int attributeIndex = 0;
            m_xmlAttribute[attributeIndex].nameHashCode = 0;
            m_xmlAttribute[attributeIndex].valueHashCode = 0;
            m_xmlAttribute[attributeIndex].valueStartIndex = 0;
            m_xmlAttribute[attributeIndex].valueLength = 0;
            TagValueType tagValueType = TagValueType.None;
            TagUnitType tagUnitType = TagUnitType.Pixels;

            // Clear attribute name hash codes
            m_xmlAttribute[1].nameHashCode = 0;
            m_xmlAttribute[2].nameHashCode = 0;
            m_xmlAttribute[3].nameHashCode = 0;
            m_xmlAttribute[4].nameHashCode = 0;

            endIndex = startIndex;
            bool isTagSet = false;
            bool isValidHtmlTag = false;

            for (int i = startIndex; i < chars.Length && chars[i].unicode != 0 && tagCharCount < m_htmlTag.Length && chars[i].unicode != '<'; i++)
            {
                int unicode = chars[i].unicode;

                if (unicode == '>') // ASCII Code of End HTML tag '>'
                {
                    isValidHtmlTag = true;
                    endIndex = i;
                    m_htmlTag[tagCharCount] = (char)0;
                    break;
                }

                m_htmlTag[tagCharCount] = (char)unicode;
                tagCharCount += 1;

                if (attributeFlag == 1)
                {
                    if (tagValueType == TagValueType.None)
                    {
                        // Check for attribute type
                        if (unicode is '+' or '-' or '.' or >= '0' and <= '9')
                        {
                            tagUnitType = TagUnitType.Pixels;
                            tagValueType = TagValueType.NumericalValue;
                            m_xmlAttribute[attributeIndex].valueStartIndex = tagCharCount - 1;
                            m_xmlAttribute[attributeIndex].valueLength += 1;
                        }
                        else if (unicode == '#')
                        {
                            tagUnitType = TagUnitType.Pixels;
                            tagValueType = TagValueType.ColorValue;
                            m_xmlAttribute[attributeIndex].valueStartIndex = tagCharCount - 1;
                            m_xmlAttribute[attributeIndex].valueLength += 1;
                        }
                        else if (unicode == '"')
                        {
                            tagUnitType = TagUnitType.Pixels;
                            tagValueType = TagValueType.StringValue;
                            m_xmlAttribute[attributeIndex].valueStartIndex = tagCharCount;
                        }
                        else
                        {
                            tagUnitType = TagUnitType.Pixels;
                            tagValueType = TagValueType.StringValue;
                            m_xmlAttribute[attributeIndex].valueStartIndex = tagCharCount - 1;
                            m_xmlAttribute[attributeIndex].valueHashCode = (m_xmlAttribute[attributeIndex].valueHashCode << 5) + m_xmlAttribute[attributeIndex].valueHashCode ^ unicode;
                            m_xmlAttribute[attributeIndex].valueLength += 1;
                        }
                    }
                    else
                    {
                        if (tagValueType == TagValueType.NumericalValue)
                        {
                            // Check for termination of numerical value.
                            if (unicode is 'p' or 'e' or '%' or ' ')
                            {
                                attributeFlag = 2;
                                tagValueType = TagValueType.None;

                                tagUnitType = unicode switch
                                {
                                    'e' => TagUnitType.FontUnits,
                                    '%' => TagUnitType.Percentage,
                                    _ => TagUnitType.Pixels
                                };

                                attributeIndex += 1;
                                m_xmlAttribute[attributeIndex].nameHashCode = 0;
                                m_xmlAttribute[attributeIndex].valueHashCode = 0;
                                m_xmlAttribute[attributeIndex].valueStartIndex = 0;
                                m_xmlAttribute[attributeIndex].valueLength = 0;

                            }
                            else
                            {
                                m_xmlAttribute[attributeIndex].valueLength += 1;
                            }
                        }
                        else if (tagValueType == TagValueType.ColorValue)
                        {
                            if (unicode != ' ')
                            {
                                m_xmlAttribute[attributeIndex].valueLength += 1;
                            }
                            else
                            {
                                attributeFlag = 2;
                                tagValueType = TagValueType.None;
                                tagUnitType = TagUnitType.Pixels;
                                attributeIndex += 1;
                                m_xmlAttribute[attributeIndex].nameHashCode = 0;
                                m_xmlAttribute[attributeIndex].valueHashCode = 0;
                                m_xmlAttribute[attributeIndex].valueStartIndex = 0;
                                m_xmlAttribute[attributeIndex].valueLength = 0;
                            }
                        }
                        else if (tagValueType == TagValueType.StringValue)
                        {
                            // Compute HashCode value for the named tag.
                            if (unicode != '"')
                            {
                                m_xmlAttribute[attributeIndex].valueHashCode = (m_xmlAttribute[attributeIndex].valueHashCode << 5) + m_xmlAttribute[attributeIndex].valueHashCode ^ unicode;
                                m_xmlAttribute[attributeIndex].valueLength += 1;
                            }
                            else
                            {
                                attributeFlag = 2;
                                tagValueType = TagValueType.None;
                                tagUnitType = TagUnitType.Pixels;
                                attributeIndex += 1;
                                m_xmlAttribute[attributeIndex].nameHashCode = 0;
                                m_xmlAttribute[attributeIndex].valueHashCode = 0;
                                m_xmlAttribute[attributeIndex].valueStartIndex = 0;
                                m_xmlAttribute[attributeIndex].valueLength = 0;
                            }
                        }
                    }
                }


                if (unicode == '=') // '='
                    attributeFlag = 1;

                // Compute HashCode for the name of the attribute
                if (attributeFlag == 0 && unicode == ' ')
                {
                    if (isTagSet) return false;

                    isTagSet = true;
                    attributeFlag = 2;

                    tagValueType = TagValueType.None;
                    tagUnitType = TagUnitType.Pixels;
                    attributeIndex += 1;
                    m_xmlAttribute[attributeIndex].nameHashCode = 0;
                    m_xmlAttribute[attributeIndex].valueHashCode = 0;
                    m_xmlAttribute[attributeIndex].valueStartIndex = 0;
                    m_xmlAttribute[attributeIndex].valueLength = 0;
                }

                if (attributeFlag == 0)
                    m_xmlAttribute[attributeIndex].nameHashCode = (m_xmlAttribute[attributeIndex].nameHashCode << 3) - m_xmlAttribute[attributeIndex].nameHashCode + unicode;

                if (attributeFlag == 2 && unicode == ' ')
                    attributeFlag = 0;

            }

            if (!isValidHtmlTag)
            {
                return false;
            }

            #region Rich Text Tag Processing
            #if !RICH_TEXT_ENABLED

            switch (m_xmlAttribute[0].nameHashCode)
                {
                    case 98: // <b>
                        m_FontStyleInternal |= FontStyles.Bold;
                        m_fontStyleStack.Add(FontStyles.Bold);
                        return true;
                    case 427: // </b>
                        if ((m_fontStyle & FontStyles.Bold) != FontStyles.Bold)
                        {
                            if (m_fontStyleStack.Remove(FontStyles.Bold) == 0)
                                m_FontStyleInternal &= ~FontStyles.Bold;
                        }
                        return true;
                    case 105: // <i>
                        m_FontStyleInternal |= FontStyles.Italic;
                        m_fontStyleStack.Add(FontStyles.Italic);

                        if (m_xmlAttribute[1].nameHashCode is 276531 or 186899)
                        {
                            m_ItalicAngle = (int)ConvertToFloat(m_htmlTag, m_xmlAttribute[1].valueStartIndex, m_xmlAttribute[1].valueLength);

                            // Make sure angle is within valid range.
                            if (m_ItalicAngle is < -180 or > 180) return false;
                        }
                        else
                            m_ItalicAngle = m_currentFontAsset.italicStyle;

                        m_ItalicAngleStack.Add(m_ItalicAngle);

                        return true;
                    case 434: // </i>
                        if ((m_fontStyle & FontStyles.Italic) != FontStyles.Italic)
                        {
                            m_ItalicAngle = m_ItalicAngleStack.Remove();

                            if (m_fontStyleStack.Remove(FontStyles.Italic) == 0)
                                m_FontStyleInternal &= ~FontStyles.Italic;
                        }
                        return true;
                    case 45545: // <size=>
                        var value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                if (m_htmlTag[5] == 43) // <size=+00>
                                {
                                    m_currentFontSize = m_fontSize + value;
                                    m_sizeStack.Add(m_currentFontSize);
                                    return true;
                                }
                                else if (m_htmlTag[5] == 45) // <size=-00>
                                {
                                    m_currentFontSize = m_fontSize + value;
                                    m_sizeStack.Add(m_currentFontSize);
                                    return true;
                                }
                                else // <size=00.0>
                                {
                                    m_currentFontSize = value;
                                    m_sizeStack.Add(m_currentFontSize);
                                    return true;
                                }
                            case TagUnitType.FontUnits:
                                m_currentFontSize = m_fontSize * value;
                                m_sizeStack.Add(m_currentFontSize);
                                return true;
                            case TagUnitType.Percentage:
                                m_currentFontSize = m_fontSize * value / 100;
                                m_sizeStack.Add(m_currentFontSize);
                                return true;
                        }
                        return false;
                    case 158392: // </size>
                        m_currentFontSize = m_sizeStack.Remove();
                        return true;
                    case 281955: // <color> <color=#FF00FF> or <color=#FF00FF00>
                        // <color=#FF00FF> 3 Hex pairs
                        if (m_htmlTag[6] == 35 && tagCharCount == 13)
                        {
                            m_htmlColor = HexCharsToColor(m_htmlTag, tagCharCount);
                            m_colorStack.Add(m_htmlColor);
                            return true;
                        }
                        // <color=#FF00FF00> 4 Hex pairs
                        else if (m_htmlTag[6] == 35 && tagCharCount == 15)
                        {
                            m_htmlColor = HexCharsToColor(m_htmlTag, tagCharCount);
                            m_colorStack.Add(m_htmlColor);
                            return true;
                        }
                        return false;
                    case 1071884: // </color>
                        m_htmlColor = m_colorStack.Remove();
                        return true;
                }
            #endif
            #endregion

            return false;
        }
    }
}
