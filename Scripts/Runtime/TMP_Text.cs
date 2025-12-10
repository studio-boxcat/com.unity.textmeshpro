#define TMP_PRESENT

using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.UI;


namespace TMPro
{
    public enum TextAlignmentOptions
    {
        TopLeft = HorizontalAlignmentOptions.Left | VerticalAlignmentOptions.Top,
        Top = HorizontalAlignmentOptions.Center | VerticalAlignmentOptions.Top,
        TopRight = HorizontalAlignmentOptions.Right | VerticalAlignmentOptions.Top,
        TopJustified = HorizontalAlignmentOptions.Justified | VerticalAlignmentOptions.Top,

        Left = HorizontalAlignmentOptions.Left | VerticalAlignmentOptions.Middle,
        Center = HorizontalAlignmentOptions.Center | VerticalAlignmentOptions.Middle,
        Right = HorizontalAlignmentOptions.Right | VerticalAlignmentOptions.Middle,
        Justified = HorizontalAlignmentOptions.Justified | VerticalAlignmentOptions.Middle,

        BottomLeft = HorizontalAlignmentOptions.Left | VerticalAlignmentOptions.Bottom,
        Bottom = HorizontalAlignmentOptions.Center | VerticalAlignmentOptions.Bottom,
        BottomRight = HorizontalAlignmentOptions.Right | VerticalAlignmentOptions.Bottom,
        BottomJustified = HorizontalAlignmentOptions.Justified | VerticalAlignmentOptions.Bottom,

        BaselineLeft = HorizontalAlignmentOptions.Left | VerticalAlignmentOptions.Baseline,
        Baseline = HorizontalAlignmentOptions.Center | VerticalAlignmentOptions.Baseline,
        BaselineRight = HorizontalAlignmentOptions.Right | VerticalAlignmentOptions.Baseline,
        BaselineJustified = HorizontalAlignmentOptions.Justified | VerticalAlignmentOptions.Baseline,

        MidlineLeft = HorizontalAlignmentOptions.Left | VerticalAlignmentOptions.Geometry,
        Midline = HorizontalAlignmentOptions.Center | VerticalAlignmentOptions.Geometry,
        MidlineRight = HorizontalAlignmentOptions.Right | VerticalAlignmentOptions.Geometry,
        MidlineJustified = HorizontalAlignmentOptions.Justified | VerticalAlignmentOptions.Geometry,

        CaplineLeft = HorizontalAlignmentOptions.Left | VerticalAlignmentOptions.Capline,
        Capline = HorizontalAlignmentOptions.Center | VerticalAlignmentOptions.Capline,
        CaplineRight = HorizontalAlignmentOptions.Right | VerticalAlignmentOptions.Capline,
        CaplineJustified = HorizontalAlignmentOptions.Justified | VerticalAlignmentOptions.Capline,

        Converted = 0xFFFF
    };

    /// <summary>
    /// Horizontal text alignment options.
    /// </summary>
    public enum HorizontalAlignmentOptions
    {
        Left = 0x1, Center = 0x2, Right = 0x4, Justified = 0x8, Flush = 0x10, Geometry = 0x20
    }

    /// <summary>
    /// Vertical text alignment options.
    /// </summary>
    public enum VerticalAlignmentOptions
    {
        Top = 0x100, Middle = 0x200, Bottom = 0x400, Baseline = 0x800, Geometry = 0x1000, Capline = 0x2000,
    }


    /// <summary>
    /// Flags controlling what vertex data gets pushed to the mesh.
    /// </summary>
    public enum TextRenderFlags
    {
        DontRender = 0x0,
        Render = 0xFF
    };

    public enum MaskingTypes { MaskOff = 0, MaskHard = 1, MaskSoft = 2 }
    public enum TextOverflowModes { Overflow = 0, Truncate = 3 }

    [Flags]
    public enum FontStyles { Normal = 0x0, Bold = 0x1, Italic = 0x2 };
    public enum FontWeight { Thin = 100, ExtraLight = 200, Light = 300, Regular = 400, Medium = 500, SemiBold = 600, Bold = 700, Heavy = 800, Black = 900 };

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
        /// The ITextPreprocessor component referenced by the text object (if any)
        /// </summary>
        public ITextPreprocessor textPreprocessor
        {
            get { return m_TextPreprocessor; }
            set { m_TextPreprocessor = value; }
        }
        [SerializeField]
        protected ITextPreprocessor m_TextPreprocessor;

        /// <summary>
        ///
        /// </summary>
        public bool isRightToLeftText
        {
            get { return m_isRightToLeft; }
            set { if (m_isRightToLeft == value) return; m_isRightToLeft = value; m_havePropertiesChanged = true; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected bool m_isRightToLeft = false;


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
        public virtual Material fontSharedMaterial
        {
            get { return m_sharedMaterial; }
            set { if (m_sharedMaterial == value) return; SetSharedMaterial(value); m_havePropertiesChanged = true; SetVerticesDirty(); SetMaterialDirty(); }
        }
        [SerializeField]
        protected Material m_sharedMaterial;
        protected Material m_currentMaterial;
        protected static MaterialReference[] m_materialReferences = new MaterialReference[4];
        protected static Dictionary<int, int> m_materialReferenceIndexLookup = new Dictionary<int, int>();

        protected static TMP_TextProcessingStack<MaterialReference> m_materialReferenceStack = new TMP_TextProcessingStack<MaterialReference>(new MaterialReference[16]);
        protected int m_currentMaterialIndex;


        /// <summary>
        /// An array containing the materials used by the text object.
        /// </summary>
        public virtual Material[] fontSharedMaterials
        {
            get { return GetSharedMaterials(); }
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


        /// <summary>
        /// The materials to be assigned to this text object. An instance of the materials will be assigned.
        /// </summary>
        [SerializeField]
        protected Material[] m_fontMaterials;

        protected bool m_isMaterialDirty;


        /// <summary>
        /// This is the default vertex color assigned to each vertices. Color tags will override vertex colors unless the overrideColorTags is set.
        /// </summary>
        public override Color color
        {
            get { return m_fontColor; }
            set { if (m_fontColor == value) return; m_havePropertiesChanged = true; m_fontColor = value; SetVerticesDirty(); }
        }
        //[UnityEngine.Serialization.FormerlySerializedAs("m_fontColor")] // Required for backwards compatibility with pre-Unity 4.6 releases.
        [SerializeField]
        protected Color32 m_fontColor32 = Color.white;
        [SerializeField]
        protected Color m_fontColor = Color.white;
        protected static Color32 s_colorWhite = new Color32(255, 255, 255, 255);

        /// <summary>
        /// Sets the vertex color alpha value.
        /// </summary>
        public float alpha
        {
            get { return m_fontColor.a; }
            set { if (m_fontColor.a == value) return; m_fontColor.a = value; m_havePropertiesChanged = true; SetVerticesDirty(); }
        }


        /// <summary>
        /// This overrides the color tags forcing the vertex colors to be the default font color.
        /// </summary>
        public bool overrideColorTags
        {
            get { return m_overrideHtmlColors; }
            set { if (m_overrideHtmlColors == value) return; m_havePropertiesChanged = true; m_overrideHtmlColors = value; SetVerticesDirty(); }
        }
        [SerializeField]
        protected bool m_overrideHtmlColors = false;


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
        protected TMP_TextProcessingStack<float> m_sizeStack = new TMP_TextProcessingStack<float>(16);


        /// <summary>
        /// Control the weight of the font if an alternative font asset is assigned for the given weight in the font asset editor.
        /// </summary>
        public FontWeight fontWeight
        {
            get { return m_fontWeight; }
            set { if (m_fontWeight == value) return; m_fontWeight = value; m_havePropertiesChanged = true; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected FontWeight m_fontWeight = FontWeight.Regular;
        protected FontWeight m_FontWeightInternal = FontWeight.Regular;
        protected TMP_TextProcessingStack<FontWeight> m_FontWeightStack = new TMP_TextProcessingStack<FontWeight>(8);

        /// <summary>
        ///
        /// </summary>
        public float pixelsPerUnit
        {
            get
            {
                var localCanvas = canvas;
                if (!localCanvas)
                    return 1;
                // For dynamic fonts, ensure we use one pixel per pixel on the screen.
                if (!font)
                    return localCanvas.scaleFactor;
                // For non-dynamic fonts, calculate pixels per unit based on specified font size relative to font object's own font size.
                if (m_currentFontAsset == null || m_currentFontAsset.faceInfo.pointSize <= 0 || m_fontSize <= 0)
                    return 1;
                return m_fontSize / m_currentFontAsset.faceInfo.pointSize;
            }
        }


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
                HorizontalAlignmentOptions horizontalAlignment = (HorizontalAlignmentOptions)((int)value & 0xFF);
                VerticalAlignmentOptions verticalAlignment = (VerticalAlignmentOptions)((int)value & 0xFF00);

                if (m_HorizontalAlignment == horizontalAlignment && m_VerticalAlignment == verticalAlignment)
                    return;

                m_HorizontalAlignment = horizontalAlignment;
                m_VerticalAlignment = verticalAlignment;
                m_havePropertiesChanged = true;
                SetVerticesDirty();
            }
        }
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("m_lineJustification")]
        protected TextAlignmentOptions m_textAlignment = TextAlignmentOptions.Converted;

        protected HorizontalAlignmentOptions m_lineJustification;
        protected TMP_TextProcessingStack<HorizontalAlignmentOptions> m_lineJustificationStack = new TMP_TextProcessingStack<HorizontalAlignmentOptions>(new HorizontalAlignmentOptions[16]);

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
        /// The amount of additional spacing between words.
        /// </summary>
        public float wordSpacing
        {
            get { return m_wordSpacing; }
            set { if (m_wordSpacing == value) return; m_havePropertiesChanged = true; m_wordSpacing = value; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected float m_wordSpacing = 0;

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
        /// The amount of additional spacing to add between each lines of text.
        /// </summary>
        public float paragraphSpacing
        {
            get { return m_paragraphSpacing; }
            set { if (m_paragraphSpacing == value) return; m_havePropertiesChanged = true; m_paragraphSpacing = value; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected float m_paragraphSpacing = 0;


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
        protected bool m_isNonBreakingSpace = false;

        /// <summary>
        /// Controls the blending between using character and word spacing to fill-in the space for justified text.
        /// </summary>
        public float wordWrappingRatios
        {
            get { return m_wordWrappingRatios; }
            set { if (m_wordWrappingRatios == value) return; m_wordWrappingRatios = value; m_havePropertiesChanged = true; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected float m_wordWrappingRatios = 0.4f; // Controls word wrapping ratios between word or characters.


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
        public int firstOverflowCharacterIndex
        {
            get { return m_firstOverflowCharacterIndex; }
        }
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
        /// Sets the RenderQueue along with Ztest to force the text to be drawn last and on top of scene elements.
        /// </summary>
        public bool isOverlay
        {
            get { return m_isOverlay; }
            set { if (m_isOverlay == value) return; m_isOverlay = value; SetShaderDepth(); m_havePropertiesChanged = true; SetVerticesDirty(); }
        }
        protected bool m_isOverlay = false;


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
        /// Forces objects that are not visible to get refreshed.
        /// </summary>
        public bool ignoreVisibility
        {
            get { return m_ignoreCulling; }
            set { if (m_ignoreCulling == value) return; m_havePropertiesChanged = true; m_ignoreCulling = value; }
        }
        //[SerializeField]
        protected bool m_ignoreCulling = true; // Not implemented yet.


        /// <summary>
        /// Determines if the Mesh will be rendered.
        /// </summary>
        public TextRenderFlags renderMode
        {
            get { return m_renderMode; }
            set { if (m_renderMode == value) return; m_renderMode = value; m_havePropertiesChanged = true; }
        }
        protected TextRenderFlags m_renderMode = TextRenderFlags.Render;


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
        /// The first character which should be made visible in conjunction with the Text Overflow Linked mode.
        /// </summary>
        public int firstVisibleCharacter
        {
            get { return m_firstVisibleCharacter; }
            set { if (m_firstVisibleCharacter == value) return; m_havePropertiesChanged = true; m_firstVisibleCharacter = value; SetVerticesDirty(); }
        }
        //[SerializeField]
        protected int m_firstVisibleCharacter;

        /// <summary>
        /// Allows to control how many characters are visible from the input.
        /// </summary>
        public int maxVisibleCharacters
        {
            get { return m_maxVisibleCharacters; }
            set { if (m_maxVisibleCharacters == value) return; m_havePropertiesChanged = true; m_maxVisibleCharacters = value; SetVerticesDirty(); }
        }
        protected int m_maxVisibleCharacters = 99999;


        /// <summary>
        /// Allows to control how many words are visible from the input.
        /// </summary>
        public int maxVisibleWords
        {
            get { return m_maxVisibleWords; }
            set { if (m_maxVisibleWords == value) return; m_havePropertiesChanged = true; m_maxVisibleWords = value; SetVerticesDirty(); }
        }
        protected int m_maxVisibleWords = 99999;


        /// <summary>
        /// Allows control over how many lines of text are displayed.
        /// </summary>
        public int maxVisibleLines
        {
            get { return m_maxVisibleLines; }
            set { if (m_maxVisibleLines == value) return; m_havePropertiesChanged = true; m_maxVisibleLines = value; SetVerticesDirty(); }
        }
        protected int m_maxVisibleLines = 99999;


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


        /// <summary>
        /// The margins of the text object.
        /// </summary>
        public Vector4 margin
        {
            get { return m_margin; }
            set { if (m_margin == value) return; m_margin = value; ComputeMarginSize(); m_havePropertiesChanged = true; SetVerticesDirty(); }
        }
        [SerializeField]
        protected Vector4 m_margin = new Vector4(0, 0, 0, 0);
        protected float m_marginLeft;
        protected float m_marginRight;
        protected float m_marginWidth;  // Width of the RectTransform minus left and right margins.
        protected float m_marginHeight; // Height of the RectTransform minus top and bottom margins.
        protected float m_width = -1;


        /// <summary>
        /// Returns data about the text object which includes information about each character, word, line, link, etc.
        /// </summary>
        public TMP_TextInfo textInfo
        {
            get { return m_textInfo; }
        }
        //[SerializeField]
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
        /// Property to handle legacy animation component.
        /// </summary>
        public bool isUsingLegacyAnimationComponent
        {
            get { return m_isUsingLegacyAnimationComponent; }
            set { m_isUsingLegacyAnimationComponent = value; }
        }
        [SerializeField]
        protected bool m_isUsingLegacyAnimationComponent;


        /// <summary>
        /// Returns are reference to the Transform
        /// </summary>
        public new RectTransform transform => m_transform ??= (RectTransform) base.transform;
        [NonSerialized] protected RectTransform m_transform;


        /// <summary>
        /// Returns are reference to the RectTransform
        /// </summary>
        public new RectTransform rectTransform
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => transform;
        }

        protected RectTransform m_rectTransform
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_transform;
        }


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
        public virtual Mesh mesh
        {
            get { return m_mesh; }
        }
        protected Mesh m_mesh;


        // *** PROPERTIES RELATED TO UNITY LAYOUT SYSTEM ***
        /// <summary>
        ///
        /// </summary>
        public float flexibleHeight { get { return m_flexibleHeight; } }
        protected float m_flexibleHeight = -1f;

        /// <summary>
        ///
        /// </summary>
        public float flexibleWidth { get { return m_flexibleWidth; } }
        protected float m_flexibleWidth = -1f;

        /// <summary>
        ///
        /// </summary>
        public float minWidth { get { return m_minWidth; } }
        protected float m_minWidth;

        /// <summary>
        ///
        /// </summary>
        public float minHeight { get { return m_minHeight; } }
        protected float m_minHeight;

        /// <summary>
        ///
        /// </summary>
        public float maxWidth { get { return m_maxWidth; } }
        protected float m_maxWidth;

        /// <summary>
        ///
        /// </summary>
        public float maxHeight { get { return m_maxHeight; } }
        protected float m_maxHeight;

        /// <summary>
        ///
        /// </summary>
        protected LayoutElement layoutElement
        {
            get
            {
                if (m_LayoutElement == null)
                {
                    m_LayoutElement = GetComponent<LayoutElement>();
                }

                return m_LayoutElement;
            }
        }
        protected LayoutElement m_LayoutElement;

        /// <summary>
        /// Computed preferred width of the text object.
        /// </summary>
        public virtual float preferredWidth { get { m_preferredWidth = GetPreferredWidth(); return m_preferredWidth; } }
        protected float m_preferredWidth;
        protected float m_renderedWidth;
        protected bool m_isPreferredWidthDirty;

        /// <summary>
        /// Computed preferred height of the text object.
        /// </summary>
        public virtual float preferredHeight { get { m_preferredHeight = GetPreferredHeight(); return m_preferredHeight; } }
        protected float m_preferredHeight;
        protected float m_renderedHeight;
        protected bool m_isPreferredHeightDirty;

        protected bool m_isCalculatingPreferredValues;


        /// <summary>
        /// Compute the rendered width of the text object.
        /// </summary>
        public virtual float renderedWidth { get { return GetRenderedWidth(); } }


        /// <summary>
        /// Compute the rendered height of the text object.
        /// </summary>
        public virtual float renderedHeight { get { return GetRenderedHeight(); } }


        /// <summary>
        ///
        /// </summary>
        public int layoutPriority { get { return m_layoutPriority; } }
        protected int m_layoutPriority = 0;

        protected bool m_isLayoutDirty;

        protected bool m_isAwake;
        internal bool m_isWaitingOnResourceLoad;

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

        protected float m_fontScaleMultiplier; // Used for handling of superscript and subscript.

        private static char[] m_htmlTag = new char[128]; // Maximum length of rich text tag. This is pre-allocated to avoid GC.
        private static RichTextTagAttribute[] m_xmlAttribute = new RichTextTagAttribute[8];
        private static float[] m_attributeParameterValues = new float[16];

        protected float tag_LineIndent = 0;
        protected float tag_Indent = 0;
        protected TMP_TextProcessingStack<float> m_indentStack = new TMP_TextProcessingStack<float>(new float[16]);
        protected bool tag_NoParsing;
        //protected TMP_LinkInfo tag_LinkInfo = new TMP_LinkInfo();

        protected bool m_isParsingText;
        protected Matrix4x4 m_FXMatrix;
        protected bool m_isFXMatrixSet;

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
        protected static WordWrapState m_SavedWordWrapState = new WordWrapState();
        protected static WordWrapState m_SavedLineState = new WordWrapState();
        protected static WordWrapState m_SavedLastValidState = new WordWrapState();
        protected static WordWrapState m_SavedSoftLineBreakState = new WordWrapState();

        // Fields whose state is saved in conjunction with text parsing and word wrapping.
        protected int m_characterCount;
        //protected int m_visibleCharacterCount;
        //protected int m_visibleSpriteCount;
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
        //protected float m_maxFontScale;
        protected float m_lineOffset;
        protected Extents m_meshExtents;


        // Fields used for vertex colors
        protected Color32 m_htmlColor = new Color(255, 255, 255, 128);
        protected TMP_TextProcessingStack<Color32> m_colorStack = new TMP_TextProcessingStack<Color32>(new Color32[16]);

        protected TMP_TextProcessingStack<int> m_ItalicAngleStack = new TMP_TextProcessingStack<int>(new int[16]);
        protected int m_ItalicAngle;

        protected TMP_TextProcessingStack<int> m_actionStack = new TMP_TextProcessingStack<int>(new int[16]);

        protected float m_padding = 0;
        protected float m_baselineOffset; // Used for superscript and subscript.
        protected TMP_TextProcessingStack<float> m_baselineOffsetStack = new TMP_TextProcessingStack<float>(new float[16]);
        protected float m_xAdvance; // Tracks x advancement from character to character.

        protected TMP_TextElement m_cached_TextElement; // Glyph / Character information is cached into this variable which is faster than having to fetch from the Dictionary multiple times.

        // Profiler Marker declarations
        private static ProfilerMarker k_ParseTextMarker = new ProfilerMarker("TMP Parse Text");
        private static ProfilerMarker k_InsertNewLineMarker = new ProfilerMarker("TMP.InsertNewLine");

        /// <summary>
        /// Method which derived classes need to override to load Font Assets.
        /// </summary>
        protected virtual void LoadFontAsset() { }

        /// <summary>
        /// Function called internally when a new shared material is assigned via the fontSharedMaterial property.
        /// </summary>
        /// <param name="mat"></param>
        protected virtual void SetSharedMaterial(Material mat) { }

        /// <summary>
        /// Function called internally when a new material is assigned via the fontMaterial property.
        /// </summary>
        protected virtual Material GetMaterial(Material mat) { return null; }

        /// <summary>
        /// Function called internally when assigning a new base material.
        /// </summary>
        /// <param name="mat"></param>
        protected virtual void SetFontBaseMaterial(Material mat) { }

        /// <summary>
        /// Method which returns an array containing the materials used by the text object.
        /// </summary>
        /// <returns></returns>
        protected virtual Material[] GetSharedMaterials() { return null; }

        /// <summary>
        ///
        /// </summary>
        protected virtual void SetSharedMaterials(Material[] materials) { }

        /// <summary>
        /// Method returning instances of the materials used by the text object.
        /// </summary>
        /// <returns></returns>
        protected virtual Material[] GetMaterials(Material[] mats) { return null; }

        /// <summary>
        /// Method to set the materials of the text and sub text objects.
        /// </summary>
        /// <param name="mats"></param>
        //protected virtual void SetMaterials (Material[] mats) { }

        /// <summary>
        /// Function used to create an instance of the material
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        protected Material CreateMaterialInstance(Material source)
        {
            Material mat = new Material(source);
            mat.shaderKeywords = source.shaderKeywords;
            mat.name += " (Instance)";

            return mat;
        }

        /// <summary>
        /// Function called internally to set the face color of the material. This will results in an instance of the material.
        /// </summary>
        /// <param name="color"></param>
        protected virtual void SetFaceColor(Color32 color) { }

        /// <summary>
        /// Function called internally to set the outline color of the material. This will results in an instance of the material.
        /// </summary>
        /// <param name="color"></param>
        protected virtual void SetOutlineColor(Color32 color) { }

        /// <summary>
        /// Function called internally to set the outline thickness property of the material. This will results in an instance of the material.
        /// </summary>
        /// <param name="thickness"></param>
        protected virtual void SetOutlineThickness(float thickness) { }

        /// <summary>
        /// Set the Render Queue and ZTest mode on the current material
        /// </summary>
        protected virtual void SetShaderDepth() { }

        /// <summary>
        /// Set the culling mode on the material.
        /// </summary>
        protected virtual void SetCulling() { }

        /// <summary>
        ///
        /// </summary>
        internal virtual void UpdateCulling() {}

        /// <summary>
        /// Get the padding value for the currently assigned material
        /// </summary>
        /// <returns></returns>
        protected virtual float GetPaddingForMaterial()
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
        protected virtual float GetPaddingForMaterial(Material mat)
        {
            if (mat == null)
                return 0;

            m_padding = ShaderUtilities.GetPadding(mat, m_enableExtraPadding, m_isUsingBold);
            m_isMaskingEnabled = ShaderUtilities.IsMaskingEnabled(m_sharedMaterial);
            m_isSDFShader = mat.HasProperty(ShaderUtilities.ID_WeightNormal);

            return m_padding;
        }


        /// <summary>
        /// Method to return the local corners of the Text Container or RectTransform.
        /// </summary>
        /// <returns></returns>
        protected virtual Vector3[] GetTextContainerLocalCorners() { return null; }


        // PUBLIC FUNCTIONS
        protected bool m_ignoreActiveState;
        /// <summary>
        /// Function to force regeneration of the text object before its normal process time. This is useful when changes to the text object properties need to be applied immediately.
        /// </summary>
        /// <param name="ignoreActiveState">Ignore Active State of text objects. Inactive objects are ignored by default.</param>
        /// <param name="forceTextReparsing">Force re-parsing of the text.</param>
        public virtual void ForceMeshUpdate(bool ignoreActiveState = false, bool forceTextReparsing = false) { }


        /// <summary>
        /// Function to update the geometry of the main and sub text objects.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="index"></param>
        public virtual void UpdateGeometry(Mesh mesh, int index) { }


        /// <summary>
        /// Function to push the updated vertex data into the mesh and renderer.
        /// </summary>
        public virtual void UpdateVertexData(TMP_VertexDataUpdateFlags flags) { }


        /// <summary>
        /// Function to push the updated vertex data into the mesh and renderer.
        /// </summary>
        public virtual void UpdateVertexData() { }


        /// <summary>
        /// Function to push a new set of vertices to the mesh.
        /// </summary>
        /// <param name="vertices"></param>
        public virtual void SetVertices(Vector3[] vertices) { }


        /// <summary>
        /// Function to be used to force recomputing of character padding when Shader / Material properties have been changed via script.
        /// </summary>
        public virtual void UpdateMeshPadding() { }


        /// <summary>
        ///
        /// </summary>
        //public virtual new void UpdateGeometry() { }


        /// <summary>
        /// Tweens the CanvasRenderer color associated with this Graphic.
        /// </summary>
        /// <param name="targetColor">Target color.</param>
        /// <param name="duration">Tween duration.</param>
        /// <param name="ignoreTimeScale">Should ignore Time.scale?</param>
        /// <param name="useAlpha">Should also Tween the alpha channel?</param>
        // XXX: 힙할당을 유발해서 제거.
        /*
        public override void CrossFadeColor(Color targetColor, float duration, bool ignoreTimeScale, bool useAlpha)
        {
            base.CrossFadeColor(targetColor, duration, ignoreTimeScale, useAlpha);
            InternalCrossFadeColor(targetColor, duration, ignoreTimeScale, useAlpha);
        }
        */


        /// <summary>
        /// Tweens the alpha of the CanvasRenderer color associated with this Graphic.
        /// </summary>
        /// <param name="alpha">Target alpha.</param>
        /// <param name="duration">Duration of the tween in seconds.</param>
        /// <param name="ignoreTimeScale">Should ignore Time.scale?</param>
        // XXX: 힙할당을 유발해서 제거.
        /*
        public override void CrossFadeAlpha(float alpha, float duration, bool ignoreTimeScale)
        {
            base.CrossFadeAlpha(alpha, duration, ignoreTimeScale);
            InternalCrossFadeAlpha(alpha, duration, ignoreTimeScale);
        }
        */


        /// <summary>
        ///
        /// </summary>
        /// <param name="targetColor"></param>
        /// <param name="duration"></param>
        /// <param name="ignoreTimeScale"></param>
        /// <param name="useAlpha"></param>
        /// <param name="useRGB"></param>
        protected virtual void InternalCrossFadeColor(Color targetColor, float duration, bool ignoreTimeScale, bool useAlpha) { }


        /// <summary>
        ///
        /// </summary>
        /// <param name="alpha"></param>
        /// <param name="duration"></param>
        /// <param name="ignoreTimeScale"></param>
        protected virtual void InternalCrossFadeAlpha(float alpha, float duration, bool ignoreTimeScale) { }

        /// <summary>
        ///
        /// </summary>
        struct TextBackingContainer
        {
            public int Capacity
            {
                get { return m_Array.Length; }
            }

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
        private TextBackingContainer m_TextBackingArray = new TextBackingContainer(4);


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
                    PopulateTextBackingArray(m_TextPreprocessor == null ? m_text : m_TextPreprocessor.PreprocessText(m_text));
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
            int srcLength = sourceText == null ? 0 : sourceText.Length;

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
        /// Convert source text to uint and populate internal text backing array.
        /// </summary>
        /// <param name="sourceText">char array containing the source text to be converted</param>
        /// <param name="start">Index of the first element of the source array to be converted and copied to the internal text backing array.</param>
        /// <param name="length">Number of elements in the array to be converted and copied to the internal text backing array.</param>
        void PopulateTextBackingArray(StringBuilder sourceText, int start, int length)
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
        /// Convert source text to Unicode (uint) and populate internal text backing array.
        /// </summary>
        /// <param name="sourceText">char array containing the source text to be converted</param>
        /// <param name="start">Index of the first element of the source array to be converted and copied to the internal text backing array.</param>
        /// <param name="length">Number of elements in the array to be converted and copied to the internal text backing array.</param>
        void PopulateTextBackingArray(char[] sourceText, int start, int length)
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
                if (c >= CodePoint.HIGH_SURROGATE_START && c <= CodePoint.HIGH_SURROGATE_END && srcLength > readIndex + 1 && m_TextBackingArray[readIndex + 1] >= CodePoint.LOW_SURROGATE_START && m_TextBackingArray[readIndex + 1] <= CodePoint.LOW_SURROGATE_END)
                {
                    if (writeIndex == m_TextProcessingArray.Length) ResizeInternalArray(ref m_TextProcessingArray);

                    m_TextProcessingArray[writeIndex].unicode = (int)TMP_TextParsingUtilities.ConvertToUTF32(c, m_TextBackingArray[readIndex + 1]);

                    readIndex += 1;
                    writeIndex += 1;
                    continue;
                }

                // Handle inline replacement of <style> and <br> tags.
                if (c == '<' && m_isRichText)
                {
                    // Read tag hash code
                    int hashCode = GetMarkupTagHashCode(m_TextBackingArray, readIndex + 1);

                    switch ((MarkupTag)hashCode)
                    {
                        case MarkupTag.BR:
                            if (writeIndex == m_TextProcessingArray.Length) ResizeInternalArray(ref m_TextProcessingArray);

                            m_TextProcessingArray[writeIndex].unicode = 10;

                            writeIndex += 1;
                            readIndex += 3;
                            continue;
                        case MarkupTag.NBSP:
                            if (writeIndex == m_TextProcessingArray.Length) ResizeInternalArray(ref m_TextProcessingArray);

                            m_TextProcessingArray[writeIndex].unicode = 160;

                            writeIndex += 1;
                            readIndex += 5;
                            continue;
                        case MarkupTag.ZWSP:
                            if (writeIndex == m_TextProcessingArray.Length) ResizeInternalArray(ref m_TextProcessingArray);

                            m_TextProcessingArray[writeIndex].unicode = 0x200B;

                            writeIndex += 1;
                            readIndex += 5;
                            continue;
                        case MarkupTag.STYLE:
                            throw new NotSupportedException();
                        case MarkupTag.SLASH_STYLE:
                            throw new NotSupportedException();
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
        /// Function used in conjunction with GetPreferredValues
        /// </summary>
        /// <param name="sourceText"></param>
        void SetTextInternal(string sourceText)
        {
            int srcLength = sourceText == null ? 0 : sourceText.Length;

            PopulateTextBackingArray(sourceText, 0, srcLength);

            // Set input source
            TextInputSources currentInputSource = m_inputSource;
            m_inputSource = TextInputSources.TextString;

            PopulateTextProcessingArray();

            m_inputSource = currentInputSource;
        }

        /// <summary>
        /// This function is the same as using the text property to set the text.
        /// </summary>
        /// <param name="sourceText">String containing the text.</param>
        /// <param name="syncTextInputBox">This optional parameter no longer provides any functionality as this function now simple sets the .text property which is reflected in the Text Input Box.</param>
        public void SetText(string sourceText, bool syncTextInputBox = true)
        {
            int srcLength = sourceText == null ? 0 : sourceText.Length;

            PopulateTextBackingArray(sourceText, 0, srcLength);

            m_text = sourceText;

            // Set input source
            m_inputSource = TextInputSources.TextString;

            PopulateTextProcessingArray();

            m_havePropertiesChanged = true;

            SetVerticesDirty();
            SetLayoutDirty();
        }

        /// <summary>
        /// <para>Formatted string containing a pattern and a value representing the text to be rendered.</para>
        /// <para>Ex. TMP_Text.SetText("A = {0}, B = {1:00}, C = {2:000.0}", 10.75f, 10.75f, 10.75f);</para>
        /// <para>Results "A = 10.75, B = 11, C = 010.8."</para>
        /// </summary>
        /// <param name="sourceText">String containing the pattern.</param>
        /// <param name="arg0">First float value.</param>
        public void SetText(string sourceText, float arg0)
        {
            SetText(sourceText, arg0, 0, 0, 0, 0, 0, 0, 0);
        }

        /// <summary>
        /// <para>Formatted string containing a pattern and a value representing the text to be rendered.</para>
        /// <para>Ex. TMP_Text.SetText("A = {0}, B = {1:00}, C = {2:000.0}", 10.75f, 10.75f, 10.75f);</para>
        /// <para>Results "A = 10.75, B = 11, C = 010.8."</para>
        /// </summary>
        /// <param name="sourceText">String containing the pattern.</param>
        /// <param name="arg0">First float value.</param>
        /// <param name="arg1">Second float value.</param>
        public void SetText(string sourceText, float arg0, float arg1)
        {
            SetText(sourceText, arg0, arg1, 0, 0, 0, 0, 0, 0);
        }

        /// <summary>
        /// <para>Formatted string containing a pattern and a value representing the text to be rendered.</para>
        /// <para>Ex. TMP_Text.SetText("A = {0}, B = {1:00}, C = {2:000.0}", 10.75f, 10.75f, 10.75f);</para>
        /// <para>Results "A = 10.75, B = 11, C = 010.8."</para>
        /// </summary>
        /// <param name="sourceText">String containing the pattern.</param>
        /// <param name="arg0">First float value.</param>
        /// <param name="arg1">Second float value.</param>
        /// <param name="arg2">Third float value.</param>
        public void SetText(string sourceText, float arg0, float arg1, float arg2)
        {
            SetText(sourceText, arg0, arg1, arg2, 0, 0, 0, 0, 0);
        }

        /// <summary>
        /// <para>Formatted string containing a pattern and a value representing the text to be rendered.</para>
        /// <para>Ex. TMP_Text.SetText("A = {0}, B = {1:00}, C = {2:000.0}", 10.75f, 10.75f, 10.75f);</para>
        /// <para>Results "A = 10.75, B = 11, C = 010.8."</para>
        /// </summary>
        /// <param name="sourceText">String containing the pattern.</param>
        /// <param name="arg0">First float value.</param>
        /// <param name="arg1">Second float value.</param>
        /// <param name="arg2">Third float value.</param>
        /// <param name="arg3">Forth float value.</param>
        public void SetText(string sourceText, float arg0, float arg1, float arg2, float arg3)
        {
            SetText(sourceText, arg0, arg1, arg2, arg3, 0, 0, 0, 0);
        }

        /// <summary>
        /// <para>Formatted string containing a pattern and a value representing the text to be rendered.</para>
        /// <para>Ex. TMP_Text.SetText("A = {0}, B = {1:00}, C = {2:000.0}", 10.75f, 10.75f, 10.75f);</para>
        /// <para>Results "A = 10.75, B = 11, C = 010.8."</para>
        /// </summary>
        /// <param name="sourceText">String containing the pattern.</param>
        /// <param name="arg0">First float value.</param>
        /// <param name="arg1">Second float value.</param>
        /// <param name="arg2">Third float value.</param>
        /// <param name="arg3">Forth float value.</param>
        /// <param name="arg4">Fifth float value.</param>
        public void SetText(string sourceText, float arg0, float arg1, float arg2, float arg3, float arg4)
        {
            SetText(sourceText, arg0, arg1, arg2, arg3, arg4, 0, 0, 0);
        }

        /// <summary>
        /// <para>Formatted string containing a pattern and a value representing the text to be rendered.</para>
        /// <para>Ex. TMP_Text.SetText("A = {0}, B = {1:00}, C = {2:000.0}", 10.75f, 10.75f, 10.75f);</para>
        /// <para>Results "A = 10.75, B = 11, C = 010.8."</para>
        /// </summary>
        /// <param name="sourceText">String containing the pattern.</param>
        /// <param name="arg0">First float value.</param>
        /// <param name="arg1">Second float value.</param>
        /// <param name="arg2">Third float value.</param>
        /// <param name="arg3">Forth float value.</param>
        /// <param name="arg4">Fifth float value.</param>
        /// <param name="arg5">Sixth float value.</param>
        public void SetText(string sourceText, float arg0, float arg1, float arg2, float arg3, float arg4, float arg5)
        {
            SetText(sourceText, arg0, arg1, arg2, arg3, arg4, arg5, 0, 0);
        }

        /// <summary>
        /// <para>Formatted string containing a pattern and a value representing the text to be rendered.</para>
        /// <para>Ex. TMP_Text.SetText("A = {0}, B = {1:00}, C = {2:000.0}", 10.75f, 10.75f, 10.75f);</para>
        /// <para>Results "A = 10.75, B = 11, C = 010.8."</para>
        /// </summary>
        /// <param name="sourceText">String containing the pattern.</param>
        /// <param name="arg0">First float value.</param>
        /// <param name="arg1">Second float value.</param>
        /// <param name="arg2">Third float value.</param>
        /// <param name="arg3">Forth float value.</param>
        /// <param name="arg4">Fifth float value.</param>
        /// <param name="arg5">Sixth float value.</param>
        /// <param name="arg6">Seventh float value.</param>
        public void SetText(string sourceText, float arg0, float arg1, float arg2, float arg3, float arg4, float arg5, float arg6)
        {
            SetText(sourceText, arg0, arg1, arg2, arg3, arg4, arg5, arg6, 0);
        }

        /// <summary>
        /// <para>Formatted string containing a pattern and a value representing the text to be rendered.</para>
        /// <para>Ex. TMP_Text.SetText("A = {0}, B = {1:00}, C = {2:000.0}", 10.75f, 10.75f, 10.75f);</para>
        /// <para>Results "A = 10.75, B = 11, C = 010.8."</para>
        /// </summary>
        /// <param name="sourceText">String containing the pattern.</param>
        /// <param name="arg0">First float value.</param>
        /// <param name="arg1">Second float value.</param>
        /// <param name="arg2">Third float value.</param>
        /// <param name="arg3">Forth float value.</param>
        /// <param name="arg4">Fifth float value.</param>
        /// <param name="arg5">Sixth float value.</param>
        /// <param name="arg6">Seventh float value.</param>
        /// <param name="arg7">Eighth float value.</param>
        public void SetText(string sourceText, float arg0, float arg1, float arg2, float arg3, float arg4, float arg5, float arg6, float arg7)
        {
            int argIndex = 0;
            int padding = 0;
            int decimalPrecision = 0;

            int readFlag = 0;

            int readIndex = 0;
            int writeIndex = 0;

            for (; readIndex < sourceText.Length; readIndex++)
            {
                char c = sourceText[readIndex];

                if (c == '{')
                {
                    readFlag = 1;
                    continue;
                }

                if (c == '}')
                {
                    // Add arg(index) to array
                    switch (argIndex)
                    {
                        case 0:
                            AddFloatToInternalTextBackingArray(arg0, padding, decimalPrecision, ref writeIndex);
                            break;
                        case 1:
                            AddFloatToInternalTextBackingArray(arg1, padding, decimalPrecision, ref writeIndex);
                            break;
                        case 2:
                            AddFloatToInternalTextBackingArray(arg2, padding, decimalPrecision, ref writeIndex);
                            break;
                        case 3:
                            AddFloatToInternalTextBackingArray(arg3, padding, decimalPrecision, ref writeIndex);
                            break;
                        case 4:
                            AddFloatToInternalTextBackingArray(arg4, padding, decimalPrecision, ref writeIndex);
                            break;
                        case 5:
                            AddFloatToInternalTextBackingArray(arg5, padding, decimalPrecision, ref writeIndex);
                            break;
                        case 6:
                            AddFloatToInternalTextBackingArray(arg6, padding, decimalPrecision, ref writeIndex);
                            break;
                        case 7:
                            AddFloatToInternalTextBackingArray(arg7, padding, decimalPrecision, ref writeIndex);
                            break;
                    }

                    argIndex = 0;
                    readFlag = 0;
                    padding = 0;
                    decimalPrecision = 0;
                    continue;
                }

                // Read Argument index
                if (readFlag == 1)
                {
                    if (c >= '0' && c <= '8')
                    {
                        argIndex = c - 48;
                        readFlag = 2;
                        continue;
                    }
                }

                // Read formatting for integral part of the value
                if (readFlag == 2)
                {
                    // Skip ':' separator
                    if (c == ':')
                        continue;

                    // Done reading integral formatting and value
                    if (c == '.')
                    {
                        readFlag = 3;
                        continue;
                    }

                    if (c == '#')
                    {
                        // do something
                        continue;
                    }

                    if (c == '0')
                    {
                        padding += 1;
                        continue;
                    }

                    if (c == ',')
                    {
                        // Use commas in the integral value
                        continue;
                    }

                    // Legacy mode
                    if (c >= '1' && c <= '9')
                    {
                        decimalPrecision = c - 48;
                        continue;
                    }
                }

                // Read Decimal Precision value
                if (readFlag == 3)
                {
                    if (c == '0')
                    {
                        decimalPrecision += 1;
                        continue;
                    }
                }

                // Write value
                m_TextBackingArray[writeIndex] = c;
                writeIndex += 1;
            }

            m_TextBackingArray[writeIndex] = 0;
            m_TextBackingArray.Count = writeIndex;

            m_IsTextBackingStringDirty = true;

            #if UNITY_EDITOR
            m_text = InternalTextBackingArrayToString();
            #endif

            m_inputSource = TextInputSources.SetText;

            PopulateTextProcessingArray();

            m_havePropertiesChanged = true;

            SetVerticesDirty();
            SetLayoutDirty();
        }


        /// <summary>
        /// Set the text using a StringBuilder object as the source.
        /// </summary>
        /// <description>
        /// Using a StringBuilder instead of concatenating strings prevents memory allocations with temporary objects.
        /// </description>
        /// <param name="sourceText">The StringBuilder object containing the source text.</param>
        public void SetText(StringBuilder sourceText)
        {
            int srcLength = sourceText == null ? 0 : sourceText.Length;

            SetText(sourceText, 0, srcLength);
        }

        /// <summary>
        /// Set the text using a StringBuilder object and specifying the starting character index and length.
        /// </summary>
        /// <param name="sourceText">The StringBuilder object containing the source text.</param>
        /// <param name="start">The index of the first character to read from in the array.</param>
        /// <param name="length">The number of characters in the array to be read.</param>
        void SetText(StringBuilder sourceText, int start, int length)
        {
            PopulateTextBackingArray(sourceText, start, length);

            m_IsTextBackingStringDirty = true;

            #if UNITY_EDITOR
            m_text = InternalTextBackingArrayToString();
            #endif

            // Set input source
            m_inputSource = TextInputSources.SetTextArray;

            PopulateTextProcessingArray();

            m_havePropertiesChanged = true;

            SetVerticesDirty();
            SetLayoutDirty();
        }


        /// <summary>
        /// Set the text using a char array and specifying the starting character index and length.
        /// </summary>
        /// <param name="sourceText">Source char array containing the Unicode characters of the text.</param>
        /// <param name="start">The index of the first character to read from in the array.</param>
        /// <param name="length">The number of characters in the array to be read.</param>
        public void SetCharArray(char[] sourceText, int start, int length)
        {
            PopulateTextBackingArray(sourceText, start, length);

            m_IsTextBackingStringDirty = true;

            #if UNITY_EDITOR
            m_text = InternalTextBackingArrayToString();
            #endif

            // Set input source
            m_inputSource = TextInputSources.SetTextArray;

            PopulateTextProcessingArray();

            m_havePropertiesChanged = true;

            SetVerticesDirty();
            SetLayoutDirty();
        }


        /// <summary>
        ///
        /// </summary>
        /// <param name="tagDefinition"></param>
        /// <param name="readIndex"></param>
        /// <returns></returns>
        int GetMarkupTagHashCode(TextBackingContainer tagDefinition, int readIndex)
        {
            int hashCode = 0;
            int maxReadIndex = readIndex + 16;
            int tagDefinitionLength = tagDefinition.Capacity;

            for (; readIndex < maxReadIndex && readIndex < tagDefinitionLength; readIndex++)
            {
                uint c = tagDefinition[readIndex];

                if (c == '>' || c == '=' || c == ' ')
                    return hashCode;

                hashCode = ((hashCode << 5) + hashCode) ^ (int)TMP_TextUtilities.ToUpperASCIIFast((uint)c);
            }

            return hashCode;
        }


        /// <summary>
        /// Get Hashcode for a given tag.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="index"></param>
        /// <param name="closeIndex"></param>
        /// <returns></returns>
        int GetStyleHashCode(ref int[] text, int index, out int closeIndex)
        {
            int hashCode = 0;
            closeIndex = 0;

            for (int i = index; i < text.Length; i++)
            {
                // Skip quote '"' character
                if (text[i] == 34) continue;

                // Break at '>'
                if (text[i] == 62) { closeIndex = i; break; }

                hashCode = (hashCode << 5) + hashCode ^ TMP_TextParsingUtilities.ToUpperASCIIFast((char)text[i]);
            }

            return hashCode;
        }


        /// <summary>
        /// Get Hashcode for a given tag.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="index"></param>
        /// <param name="closeIndex"></param>
        /// <returns></returns>
        int GetStyleHashCode(ref TextBackingContainer text, int index, out int closeIndex)
        {
            int hashCode = 0;
            closeIndex = 0;

            for (int i = index; i < text.Capacity; i++)
            {
                // Skip quote '"' character
                if (text[i] == 34) continue;

                // Break at '>'
                if (text[i] == 62) { closeIndex = i; break; }

                hashCode = (hashCode << 5) + hashCode ^ TMP_TextParsingUtilities.ToUpperASCIIFast((char)text[i]);
            }

            return hashCode;
        }

        /// <summary>
        ///
        /// </summary>
        void ResizeInternalArray <T>(ref T[] array)
        {
            int size = Mathf.NextPowerOfTwo(array.Length + 1);

            System.Array.Resize(ref array, size);
        }

        void ResizeInternalArray<T>(ref T[] array, int size)
        {
            size = Mathf.NextPowerOfTwo(size + 1);

            System.Array.Resize(ref array, size);
        }


        private readonly decimal[] k_Power = { 5e-1m, 5e-2m, 5e-3m, 5e-4m, 5e-5m, 5e-6m, 5e-7m, 5e-8m, 5e-9m, 5e-10m }; // Used by FormatText to enable rounding and avoid using Mathf.Pow.


        void AddFloatToInternalTextBackingArray(float value, int padding, int precision, ref int writeIndex)
        {
            if (value < 0)
            {
                m_TextBackingArray[writeIndex] = '-';
                writeIndex += 1;
                value = -value;
            }

            // Using decimal type due to floating point precision impacting formatting
            decimal valueD = (decimal)value;

            // Round up value to the specified prevision otherwise set precision to max.
            if (padding == 0 && precision == 0)
                precision = 9;
            else
                valueD += k_Power[Mathf.Min(9, precision)];

            long integer = (long)valueD;

            AddIntegerToInternalTextBackingArray(integer, padding, ref writeIndex);

            if (precision > 0)
            {
                valueD -= integer;

                // Add decimal point and values only if remainder is not zero.
                if (valueD != 0)
                {
                    // Add decimal point
                    m_TextBackingArray[writeIndex++] = '.';

                    for (int p = 0; p < precision; p++)
                    {
                        valueD *= 10;
                        long d = (long)valueD;

                        m_TextBackingArray[writeIndex++] = (char)(d + 48);
                        valueD -= d;

                        if (valueD == 0)
                            p = precision;
                    }
                }
            }
        }


        /// <summary>
        ///
        /// </summary>
        /// <param name="number"></param>
        /// <param name="padding"></param>
        /// <param name="writeIndex"></param>
        void AddIntegerToInternalTextBackingArray(double number, int padding, ref int writeIndex)
        {
            int integralCount = 0;
            int i = writeIndex;

            do
            {
                m_TextBackingArray[i++] = (char)(number % 10 + 48);
                number /= 10;
                integralCount += 1;
            } while (number > 0.999999999999999d || integralCount < padding);

            int lastIndex = i;

            //// Reverse string
            while (writeIndex + 1 < i)
            {
                i -= 1;
                uint t = m_TextBackingArray[writeIndex];
                m_TextBackingArray[writeIndex] = m_TextBackingArray[i];
                m_TextBackingArray[i] = t;
                writeIndex += 1;
            }
            writeIndex = lastIndex;
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
        internal virtual int SetArraySizes(UnicodeChar[] unicodeChars) { return 0; }


        /// <summary>
        /// Function to Calculate the Preferred Width and Height of the text object.
        /// </summary>
        /// <returns></returns>
        public Vector2 GetPreferredValues()
        {
            // CALCULATE PREFERRED WIDTH
            m_isPreferredWidthDirty = true;
            float preferredWidth = GetPreferredWidth();

            // CALCULATE PREFERRED HEIGHT
            m_isPreferredHeightDirty = true;
            float preferredHeight = GetPreferredHeight();

            // Reset dirty states as we always want to recalculate preferred values when this function is called.
            m_isPreferredWidthDirty = true;
            m_isPreferredHeightDirty = true;

            return new Vector2(preferredWidth, preferredHeight);
        }


        /// <summary>
        /// Function to Calculate the Preferred Width and Height of the text object given the provided width and height.
        /// </summary>
        /// <returns></returns>
        public Vector2 GetPreferredValues(float width, float height)
        {
            // Reparse input text
            m_isCalculatingPreferredValues = true;
            ParseInputText();

            Vector2 margin = new Vector2(width, height);

            // CALCULATE PREFERRED WIDTH
            float preferredWidth = GetPreferredWidth(margin);

            // CALCULATE PREFERRED HEIGHT
            float preferredHeight = GetPreferredHeight(margin);

            return new Vector2(preferredWidth, preferredHeight);
        }


        /// <summary>
        /// Function to Calculate the Preferred Width and Height of the text object given a certain string.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public Vector2 GetPreferredValues(string text)
        {
            m_isCalculatingPreferredValues = true;

            SetTextInternal(text);
            SetArraySizes(m_TextProcessingArray);

            Vector2 margin = k_LargePositiveVector2;

            // CALCULATE PREFERRED WIDTH
            float preferredWidth = GetPreferredWidth(margin);

            // CALCULATE PREFERRED HEIGHT
            float preferredHeight = GetPreferredHeight(margin);

            return new Vector2(preferredWidth, preferredHeight);
        }


        /// <summary>
        ///  Function to Calculate the Preferred Width and Height of the text object given a certain string and size of text container.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public Vector2 GetPreferredValues(string text, float width, float height)
        {
            m_isCalculatingPreferredValues = true;

            SetTextInternal(text);
            SetArraySizes(m_TextProcessingArray);

            Vector2 margin = new Vector2(width, height);

            // CALCULATE PREFERRED WIDTH
            float preferredWidth = GetPreferredWidth(margin);

            // CALCULATE PREFERRED HEIGHT
            float preferredHeight = GetPreferredHeight(margin);

            return new Vector2(preferredWidth, preferredHeight);
        }


        /// <summary>
        /// Method to calculate the preferred width of a text object.
        /// </summary>
        /// <returns></returns>
        protected float GetPreferredWidth()
        {
            if (TMP_Settings.instance == null) return 0;

            // Return cached preferred height if already computed
            if (!m_isPreferredWidthDirty)
                return m_preferredWidth;

            float fontSize = m_enableAutoSizing ? m_fontSizeMax : m_fontSize;

            // Reset auto sizing point size bounds
            m_minFontSize = m_fontSizeMin;
            m_maxFontSize = m_fontSizeMax;
            m_charWidthAdjDelta = 0;

            // Set Margins to Infinity
            Vector2 margin = k_LargePositiveVector2;

            m_isCalculatingPreferredValues = true;
            ParseInputText();

            m_AutoSizeIterationCount = 0;
            float preferredWidth = CalculatePreferredValues(ref fontSize, margin, false, false).x;

            m_isPreferredWidthDirty = false;

            //Debug.Log("GetPreferredWidth() called on Object ID: " + GetInstanceID() + " on frame: " + Time.frameCount + ". Returning width of " + preferredWidth);

            return preferredWidth;
        }


        /// <summary>
        /// Method to calculate the preferred width of a text object.
        /// </summary>
        /// <param name="margin"></param>
        /// <returns></returns>
        float GetPreferredWidth(Vector2 margin)
        {
            float fontSize = m_enableAutoSizing ? m_fontSizeMax : m_fontSize;

            // Reset auto sizing point size bounds
            m_minFontSize = m_fontSizeMin;
            m_maxFontSize = m_fontSizeMax;
            m_charWidthAdjDelta = 0;

            m_AutoSizeIterationCount = 0;
            float preferredWidth = CalculatePreferredValues(ref fontSize, margin, false, false).x;

            //Debug.Log("GetPreferredWidth() Called. Returning width of " + preferredWidth);

            return preferredWidth;
        }


        /// <summary>
        /// Method to calculate the preferred height of a text object.
        /// </summary>
        /// <returns></returns>
        protected float GetPreferredHeight()
        {
            if (TMP_Settings.instance == null) return 0;

            // Return cached preferred height if already computed
            if (!m_isPreferredHeightDirty)
                return m_preferredHeight;

            float fontSize = m_enableAutoSizing ? m_fontSizeMax : m_fontSize;

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
        /// Method to calculate the preferred height of a text object.
        /// </summary>
        /// <param name="margin"></param>
        /// <returns></returns>
        float GetPreferredHeight(Vector2 margin)
        {
            float fontSize = m_enableAutoSizing ? m_fontSizeMax : m_fontSize;

            // Reset auto sizing point size bounds
            m_minFontSize = m_fontSizeMin;
            m_maxFontSize = m_fontSizeMax;
            m_charWidthAdjDelta = 0;

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

            //Debug.Log("GetPreferredHeight() Called. Returning height of " + preferredHeight);

            return preferredHeight;
        }


        /// <summary>
        /// Method returning the rendered width and height of the text object.
        /// </summary>
        /// <returns></returns>
        public Vector2 GetRenderedValues()
        {
            return GetTextBounds().size;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="onlyVisibleCharacters">Should returned value only factor in visible characters and exclude those greater than maxVisibleCharacters for instance.</param>
        /// <returns></returns>
        public Vector2 GetRenderedValues(bool onlyVisibleCharacters)
        {
            return GetTextBounds(onlyVisibleCharacters).size;
        }


        /// <summary>
        /// Method returning the rendered width of the text object.
        /// </summary>
        /// <returns></returns>
        float GetRenderedWidth()
        {
            return GetRenderedValues().x;
        }

        /// <summary>
        /// Method returning the rendered width of the text object.
        /// </summary>
        /// <returns></returns>
        protected float GetRenderedWidth(bool onlyVisibleCharacters)
        {
            return GetRenderedValues(onlyVisibleCharacters).x;
        }

        /// <summary>
        /// Method returning the rendered height of the text object.
        /// </summary>
        /// <returns></returns>
        float GetRenderedHeight()
        {
            return GetRenderedValues().y;
        }

        /// <summary>
        /// Method returning the rendered height of the text object.
        /// </summary>
        /// <returns></returns>
        protected float GetRenderedHeight(bool onlyVisibleCharacters)
        {
            return GetRenderedValues(onlyVisibleCharacters).y;
        }


        /// <summary>
        /// Method to calculate the preferred width and height of the text object.
        /// </summary>
        /// <returns></returns>
        protected virtual Vector2 CalculatePreferredValues(ref float fontSize, Vector2 marginSize, bool isTextAutoSizingEnabled, bool isWordWrappingEnabled)
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
            m_materialReferenceStack.SetDefault(new MaterialReference(0, m_currentFontAsset, m_currentMaterial));

            // Total character count is computed when the text is parsed.
            int totalCharacterCount = m_totalCharacterCount; // m_VisibleCharacters.Count;

            if (m_internalCharacterInfo == null || totalCharacterCount > m_internalCharacterInfo.Length)
                m_internalCharacterInfo = new TMP_CharacterInfo[totalCharacterCount > 1024 ? totalCharacterCount + 256 : Mathf.NextPowerOfTwo(totalCharacterCount)];

            // Calculate the scale of the font based on selected font size and sampling point size.
            // baseScale is calculated using the font asset assigned to the text object.
            float baseScale = (fontSize / m_fontAsset.faceInfo.pointSize * m_fontAsset.faceInfo.scale * (m_isOrthographic ? 1 : 0.1f));
            float currentElementScale = baseScale;
            float currentEmScale = fontSize * 0.01f * (m_isOrthographic ? 1 : 0.1f);
            m_fontScaleMultiplier = 1;

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
            //float lineOffsetDelta = 0;
            m_xAdvance = 0; // Used to track the position of each character.
            float maxXAdvance = 0; // Used to determine Preferred Width.

            tag_LineIndent = 0; // Used for indentation of text.
            tag_Indent = 0;
            m_indentStack.SetDefault(0);
            tag_NoParsing = false;
            //m_isIgnoringAlignment = false;

            m_characterCount = 0; // Total characters in the char[]


            // Tracking of line information
            m_firstCharacterOfLine = 0;
            m_maxLineAscender = k_LargeNegativeFloat;
            m_maxLineDescender = k_LargePositiveFloat;
            m_lineNumber = 0;
            m_startOfLineAscender = 0;
            m_IsDrivenLineSpacing = false;

            float marginWidth = marginSize.x;
            float marginHeight = marginSize.y;
            m_marginLeft = 0;
            m_marginRight = 0;

            float lineMarginLeft = 0;
            float lineMarginRight = 0;

            m_width = -1;
            float widthOfTextArea = marginWidth + 0.0001f - m_marginLeft - m_marginRight;

            // Used by Unity's Auto Layout system.
            float renderedWidth = 0;
            float renderedHeight = 0;
            float textWidth = 0;
            m_isCalculatingPreferredValues = true;

            // Tracking of the highest Ascender
            m_maxCapHeight = 0;
            m_maxTextAscender = 0;
            m_ElementDescender = 0;
            float maxVisibleDescender = 0;
            bool isMaxVisibleDescenderSet = false;

            // Initialize struct to track states of word wrapping
            bool isFirstWordOfLine = true;
            m_isNonBreakingSpace = false;
            //bool isLastBreakingChar = false;
            bool isLastCharacterCJK = false;
            //int lastSoftLineBreak = 0;

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
                    m_isParsingText = true;
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

                int prev_MaterialIndex = m_currentMaterialIndex;
                bool isUsingAltTypeface = m_textInfo.characterInfo[m_characterCount].isUsingAlternateTypeface;

                m_isParsingText = false;

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


                // When using Linked text, mark character as ignored and skip to next character.
                #region Linked Text
                if (m_characterCount < m_firstVisibleCharacter && charCode != 0x03)
                {
                    m_internalCharacterInfo[m_characterCount].isVisible = false;
                    m_internalCharacterInfo[m_characterCount].character = (char)0x200B;
                    m_internalCharacterInfo[m_characterCount].lineNumber = 0;
                    m_characterCount += 1;
                    continue;
                }
                #endregion


                // Look up Character Data from Dictionary and cache it.
                #region Look up Character Data
                //float baselineOffset = 0;
                float elementAscentLine = 0;
                float elementDescentLine = 0;
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

                    currentElementScale = adjustedScale * m_fontScaleMultiplier * m_cached_TextElement.scale;
                    //baselineOffset = m_currentFontAsset.faceInfo.baseline * m_fontScale * m_fontScaleMultiplier * m_currentFontAsset.faceInfo.scale;
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
                {
                    TMP_GlyphPairAdjustmentRecord adjustmentPair;
                    uint baseGlyphIndex = m_cached_TextElement.m_GlyphIndex;

                    if (m_characterCount < totalCharacterCount - 1)
                    {
                        uint nextGlyphIndex = m_textInfo.characterInfo[m_characterCount + 1].textElement.m_GlyphIndex;
                        uint key = nextGlyphIndex << 16 | baseGlyphIndex;

                        if (m_currentFontAsset.m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookupDictionary.TryGetValue(key, out adjustmentPair))
                        {
                            glyphAdjustments = adjustmentPair.m_FirstAdjustmentRecord.m_GlyphValueRecord;
                            characterSpacingAdjustment = (adjustmentPair.m_FeatureLookupFlags & FontFeatureLookupFlags.IgnoreSpacingAdjustments) == FontFeatureLookupFlags.IgnoreSpacingAdjustments ? 0 : characterSpacingAdjustment;
                        }
                    }

                    if (m_characterCount >= 1)
                    {
                        uint previousGlyphIndex = m_textInfo.characterInfo[m_characterCount - 1].textElement.m_GlyphIndex;
                        uint key = baseGlyphIndex << 16 | previousGlyphIndex;

                        if (m_currentFontAsset.m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookupDictionary.TryGetValue(key, out adjustmentPair))
                        {
                            glyphAdjustments += adjustmentPair.m_SecondAdjustmentRecord.m_GlyphValueRecord;
                            characterSpacingAdjustment = (adjustmentPair.m_FeatureLookupFlags & FontFeatureLookupFlags.IgnoreSpacingAdjustments) == FontFeatureLookupFlags.IgnoreSpacingAdjustments ? 0 : characterSpacingAdjustment;
                        }
                    }

                    m_GlyphHorizontalAdvanceAdjustment = glyphAdjustments.m_XAdvance;
                }
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
                if (!isUsingAltTypeface && ((m_FontStyleInternal & FontStyles.Bold) == FontStyles.Bold)) // Checks for any combination of Bold Style.
                    boldSpacingAdjustment = m_currentFontAsset.boldSpacing;
                #endregion Handle Style Padding

                // Compute text metrics
                #region Compute Ascender & Descender values
                // Element Ascender in line space
                float elementAscender = elementAscentLine * currentElementScale / 1.0f + m_baselineOffset;

                // Element Descender in line space
                float elementDescender = elementDescentLine * currentElementScale / 1.0f + m_baselineOffset;

                float adjustedAscender = elementAscender;
                float adjustedDescender = elementDescender;

                bool isFirstCharacterOfLine = m_characterCount == m_firstCharacterOfLine;
                // Max line ascender and descender in line space
                if (isFirstCharacterOfLine || isWhiteSpace == false)
                {
                    // Special handling for Superscript and Subscript where we use the unadjusted line ascender and descender
                    if (m_baselineOffset != 0)
                    {
                        adjustedAscender = Mathf.Max((elementAscender - m_baselineOffset) / m_fontScaleMultiplier, adjustedAscender);
                        adjustedDescender = Mathf.Min((elementDescender - m_baselineOffset) / m_fontScaleMultiplier, adjustedDescender);
                    }

                    m_maxLineAscender = Mathf.Max(adjustedAscender, m_maxLineAscender);
                    m_maxLineDescender = Mathf.Min(adjustedDescender, m_maxLineDescender);
                }

                // Element Ascender and Descender in object space
                if (isFirstCharacterOfLine || isWhiteSpace == false)
                {
                    m_internalCharacterInfo[m_characterCount].adjustedAscender = adjustedAscender;
                    m_internalCharacterInfo[m_characterCount].adjustedDescender = adjustedDescender;

                    m_ElementDescender = m_internalCharacterInfo[m_characterCount].descender = elementDescender - m_lineOffset;
                }
                else
                {
                    m_internalCharacterInfo[m_characterCount].adjustedAscender = m_maxLineAscender;
                    m_internalCharacterInfo[m_characterCount].adjustedDescender = m_maxLineDescender;

                    m_ElementDescender = m_internalCharacterInfo[m_characterCount].descender = m_maxLineDescender - m_lineOffset;
                }

                // Max text object ascender and cap height
                if (m_lineNumber == 0)
                {
                    if (isFirstCharacterOfLine || isWhiteSpace == false)
                    {
                        m_maxTextAscender = m_maxLineAscender;
                        m_maxCapHeight = Mathf.Max(m_maxCapHeight, m_currentFontAsset.m_FaceInfo.capLine * currentElementScale / 1.0f);
                    }
                }

                // Page ascender
                if (m_lineOffset == 0)
                {
                    if (!isWhiteSpace || m_characterCount == m_firstCharacterOfLine)
                        m_PageAscender = m_PageAscender > elementAscender ? m_PageAscender : elementAscender;
                }
                #endregion

                bool isJustifiedOrFlush = (m_lineJustification & HorizontalAlignmentOptions.Flush) == HorizontalAlignmentOptions.Flush || (m_lineJustification & HorizontalAlignmentOptions.Justified) == HorizontalAlignmentOptions.Justified;

                // Setup Mesh for visible text elements. ie. not a SPACE / LINEFEED / CARRIAGE RETURN.
                #region Handle Visible Characters
                if (charCode == 9 || (isWhiteSpace == false && charCode != 0x200B && charCode != 0xAD && charCode != 0x03) || (charCode == 0xAD && isSoftHyphenIgnored == false))
                {
                    //float marginLeft = m_marginLeft;
                    //float marginRight = m_marginRight;

                    // Injected characters do not override margins
                    //if (isInjectingCharacter)
                    //{
                    //    marginLeft = m_textInfo.lineInfo[m_lineNumber].marginLeft;
                    //    marginRight = m_textInfo.lineInfo[m_lineNumber].marginRight;
                    //}

                    widthOfTextArea = m_width != -1 ? Mathf.Min(marginWidth + 0.0001f - m_marginLeft - m_marginRight, m_width) : marginWidth + 0.0001f - m_marginLeft - m_marginRight;

                    // Calculate the line breaking width of the text.
                    textWidth = Mathf.Abs(m_xAdvance) + currentGlyphMetrics.horizontalAdvance * (1 - m_charWidthAdjDelta) * (charCode == 0xAD ? currentElementUnmodifiedScale : currentElementScale);

                    int testedCharacterCount = m_characterCount;

                    // Handling of Horizontal Bounds
                    #region Current Line Horizontal Bounds Check
                    if (textWidth > widthOfTextArea * (isJustifiedOrFlush ? 1.05f : 1.0f))
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

                                    float adjustmentDelta = textWidth - (widthOfTextArea - 0.0001f) * (isJustifiedOrFlush ? 1.05f : 1.0f);
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
                            if (!isMaxVisibleDescenderSet)
                                maxVisibleDescender = m_ElementDescender;

                            if (m_useMaxVisibleDescender && (m_characterCount >= m_maxVisibleCharacters || m_lineNumber >= m_maxVisibleLines))
                                isMaxVisibleDescenderSet = true;

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

                            m_xAdvance = 0 + tag_Indent;
                            //isStartOfNewLine = true;
                            isFirstWordOfLine = true;
                            continue;
                        }
                    }
                    #endregion

                    lineMarginLeft = m_marginLeft;
                    lineMarginRight = m_marginRight;

                }
                #endregion Handle Visible Characters


                // Check if Line Spacing of previous line needs to be adjusted.
                #region Adjust Line Spacing
                /*if (m_lineOffset > 0 && !TMP_Math.Approximately(m_maxLineAscender, m_startOfLineAscender) && m_IsDrivenLineSpacing == false && !m_isNewPage)
                {
                    float offsetDelta = m_maxLineAscender - m_startOfLineAscender;
                    //AdjustLineOffset(m_firstCharacterOfLine, m_characterCount, offsetDelta);
                    m_ElementDescender -= offsetDelta;
                    m_lineOffset += offsetDelta;

                    m_startOfLineAscender += offsetDelta;
                    internalWordWrapState.lineOffset = m_lineOffset;
                    internalWordWrapState.startOfLineAscender = m_startOfLineAscender;
                }*/
                #endregion


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

                    if (isWhiteSpace || charCode == 0x200B)
                        m_xAdvance += m_wordSpacing * currentEmScale;
                }
                else
                {
                    m_xAdvance += ((currentGlyphMetrics.horizontalAdvance + glyphAdjustments.xAdvance) * currentElementScale + (m_currentFontAsset.normalSpacingOffset + characterSpacingAdjustment + boldSpacingAdjustment) * currentEmScale + m_cSpacing) * (1 - m_charWidthAdjDelta);

                    if (isWhiteSpace || charCode == 0x200B)
                        m_xAdvance += m_wordSpacing * currentEmScale;
                }
                #endregion Tabulation & Stops


                // Handle Carriage Return
                #region Carriage Return
                if (charCode == 13)
                {
                    maxXAdvance = Mathf.Max(maxXAdvance, renderedWidth + m_xAdvance);
                    renderedWidth = 0;
                    m_xAdvance = 0 + tag_Indent;
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
                        renderedWidth = Mathf.Max(maxXAdvance, renderedWidth + textWidth + lineMarginLeft + lineMarginRight);
                    else
                    {
                        maxXAdvance = Mathf.Max(maxXAdvance, renderedWidth + textWidth + lineMarginLeft + lineMarginRight);
                        renderedWidth = 0;
                    }

                    renderedHeight = m_maxTextAscender - m_ElementDescender;

                    // Add new line if not last lines or character.
                    if (charCode == 10 || charCode == 11 || charCode == 0x2D || charCode == 0x2028 || charCode == 0x2029)
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
                            float lineOffsetDelta = 0 - m_maxLineDescender + ascender + (lineGap + m_lineSpacingDelta) * baseScale + (m_lineSpacing + (charCode == 10 || charCode == 0x2029 ? m_paragraphSpacing : 0)) * currentEmScale;
                            m_lineOffset += lineOffsetDelta;
                            m_IsDrivenLineSpacing = false;
                        }
                        else
                        {
                            m_lineOffset += m_lineHeight + (m_lineSpacing + (charCode == 10 || charCode == 0x2029 ? m_paragraphSpacing : 0)) * currentEmScale;
                            m_IsDrivenLineSpacing = true;
                        }

                        m_maxLineAscender = k_LargeNegativeFloat;
                        m_maxLineDescender = k_LargePositiveFloat;
                        m_startOfLineAscender = ascender;

                        m_xAdvance = 0 + tag_LineIndent + tag_Indent;

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
                    if ((isWhiteSpace || charCode == 0x200B || charCode == 0x2D || charCode == 0xAD) && !m_isNonBreakingSpace && charCode != 0xA0 && charCode != 0x2007 && charCode != 0x2011 && charCode != 0x202F && charCode != 0x2060)
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
                    else if (m_isNonBreakingSpace == false &&
                             ((charCode > 0x1100 && charCode < 0x11ff || /* Hangul Jamo */
                               charCode > 0xA960 && charCode < 0xA97F || /* Hangul Jamo Extended-A */
                               charCode > 0xAC00 && charCode < 0xD7FF)&& /* Hangul Syllables */
                              TMP_Settings.useModernHangulLineBreakingRules == false ||

                              (charCode > 0x2E80 && charCode < 0x9FFF || /* CJK */
                               charCode > 0xF900 && charCode < 0xFAFF || /* CJK Compatibility Ideographs */
                               charCode > 0xFE30 && charCode < 0xFE4F || /* CJK Compatibility Forms */
                               charCode > 0xFF00 && charCode < 0xFFEF))) /* CJK Halfwidth */
                    {
                        bool isLeadingCharacter = TMP_Settings.linebreakingRules.leadingCharacters.ContainsKey(charCode);
                        bool isFollowingCharacter = m_characterCount < totalCharacterCount - 1 && TMP_Settings.linebreakingRules.followingCharacters.ContainsKey(m_internalCharacterInfo[m_characterCount + 1].character);

                        if (isFirstWordOfLine || isLeadingCharacter == false)
                        {
                            if (isFollowingCharacter == false)
                            {
                                SaveWordWrappingState(ref internalWordWrapState, i, m_characterCount);
                                isFirstWordOfLine = false;
                            }

                            if (isFirstWordOfLine)
                            {
                                // Special handling for non-breaking space and soft line breaks
                                if (isWhiteSpace)
                                    SaveWordWrappingState(ref internalSoftLineBreak, i, m_characterCount);

                                SaveWordWrappingState(ref internalWordWrapState, i, m_characterCount);
                            }
                        }

                        isLastCharacterCJK = true;
                    }
                    else if (isLastCharacterCJK)
                    {
                        bool isLeadingCharacter = TMP_Settings.linebreakingRules.leadingCharacters.ContainsKey(charCode);

                        if (isLeadingCharacter == false)
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

            // Adjust Preferred Width and Height to account for Margins.
            renderedWidth += m_margin.x > 0 ? m_margin.x : 0;
            renderedWidth += m_margin.z > 0 ? m_margin.z : 0;

            renderedHeight += m_margin.y > 0 ? m_margin.y : 0;
            renderedHeight += m_margin.w > 0 ? m_margin.w : 0;

            // Round Preferred Values to nearest 5/100.
            renderedWidth = (int)(renderedWidth * 100 + 1f) / 100f;
            renderedHeight = (int)(renderedHeight * 100 + 1f) / 100f;

            //Debug.Log("Preferred Values: (" + renderedWidth + ", " + renderedHeight + ") with Recursive count of " + m_recursiveCount);

            return new Vector2(renderedWidth, renderedHeight);
        }


        /// <summary>
        /// Method which returns the bounds of the text object;
        /// </summary>
        /// <returns></returns>
        protected Bounds GetTextBounds()
        {
            if (m_textInfo == null || m_textInfo.characterCount > m_textInfo.characterInfo.Length) return new Bounds();

            Extents extent = new Extents(k_LargePositiveVector2, k_LargeNegativeVector2);

            for (int i = 0; i < m_textInfo.characterCount && i < m_textInfo.characterInfo.Length; i++)
            {
                if (!m_textInfo.characterInfo[i].isVisible)
                    continue;

                extent.min.x = Mathf.Min(extent.min.x, m_textInfo.characterInfo[i].origin);
                extent.min.y = Mathf.Min(extent.min.y, m_textInfo.characterInfo[i].descender);

                extent.max.x = Mathf.Max(extent.max.x, m_textInfo.characterInfo[i].xAdvance);
                extent.max.y = Mathf.Max(extent.max.y, m_textInfo.characterInfo[i].ascender);
            }

            Vector2 size;
            size.x = extent.max.x - extent.min.x;
            size.y = extent.max.y - extent.min.y;

            Vector3 center = (extent.min + extent.max) / 2;

            return new Bounds(center, size);
        }


        /// <summary>
        /// Method which returns the bounds of the text object;
        /// </summary>
        /// <param name="onlyVisibleCharacters"></param>
        /// <returns></returns>
        protected Bounds GetTextBounds(bool onlyVisibleCharacters)
        {
            if (m_textInfo == null) return new Bounds();

            Extents extent = new Extents(k_LargePositiveVector2, k_LargeNegativeVector2);

            for (int i = 0; i < m_textInfo.characterCount; i++)
            {
                if ((i > maxVisibleCharacters || m_textInfo.characterInfo[i].lineNumber > m_maxVisibleLines) && onlyVisibleCharacters)
                    break;

                if (onlyVisibleCharacters && !m_textInfo.characterInfo[i].isVisible)
                    continue;

                extent.min.x = Mathf.Min(extent.min.x, m_textInfo.characterInfo[i].origin);
                extent.min.y = Mathf.Min(extent.min.y, m_textInfo.characterInfo[i].descender);

                extent.max.x = Mathf.Max(extent.max.x, m_textInfo.characterInfo[i].xAdvance);
                extent.max.y = Mathf.Max(extent.max.y, m_textInfo.characterInfo[i].ascender);
            }

            Vector2 size;
            size.x = extent.max.x - extent.min.x;
            size.y = extent.max.y - extent.min.y;

            Vector2 center = (extent.min + extent.max) / 2;

            return new Bounds(center, size);
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
            Vector3 vertexOffset = new Vector3(0, offset, 0);

            for (int i = startIndex; i <= endIndex; i++)
            {
                m_textInfo.characterInfo[i].bottomLeft -= vertexOffset;
                m_textInfo.characterInfo[i].topLeft -= vertexOffset;
                m_textInfo.characterInfo[i].topRight -= vertexOffset;
                m_textInfo.characterInfo[i].bottomRight -= vertexOffset;

                m_textInfo.characterInfo[i].ascender -= vertexOffset.y;
                m_textInfo.characterInfo[i].descender -= vertexOffset.y;

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

            TMP_LineInfo[] temp_lineInfo = new TMP_LineInfo[size];
            for (int i = 0; i < size; i++)
            {
                if (i < m_textInfo.lineInfo.Length)
                    temp_lineInfo[i] = m_textInfo.lineInfo[i];
                else
                {
                    temp_lineInfo[i].lineExtents.min = k_LargePositiveVector2;
                    temp_lineInfo[i].lineExtents.max = k_LargeNegativeVector2;

                    temp_lineInfo[i].ascender = k_LargeNegativeFloat;
                    temp_lineInfo[i].descender = k_LargePositiveFloat;
                }
            }

            m_textInfo.lineInfo = temp_lineInfo;
        }
        protected static Vector2 k_LargePositiveVector2 = new Vector2(TMP_Math.INT_MAX, TMP_Math.INT_MAX);
        protected static Vector2 k_LargeNegativeVector2 = new Vector2(TMP_Math.INT_MIN, TMP_Math.INT_MIN);
        protected static float k_LargePositiveFloat = TMP_Math.FLOAT_MAX;
        protected static float k_LargeNegativeFloat = TMP_Math.FLOAT_MIN;

        /// <summary>
        /// Function to force an update of the margin size.
        /// </summary>
        public virtual void ComputeMarginSize() { }


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
            float lineAscender = m_maxLineAscender - m_lineOffset;
            float lineDescender = m_maxLineDescender - m_lineOffset;

            // Update maxDescender and maxVisibleDescender
            m_ElementDescender = m_ElementDescender < lineDescender ? m_ElementDescender : lineDescender;
            if (!isMaxVisibleDescenderSet)
                maxVisibleDescender = m_ElementDescender;

            if (m_useMaxVisibleDescender && (m_characterCount >= m_maxVisibleCharacters || m_lineNumber >= m_maxVisibleLines))
                isMaxVisibleDescenderSet = true;

            // Track & Store lineInfo for the new line
            m_textInfo.lineInfo[m_lineNumber].firstCharacterIndex = m_firstCharacterOfLine;
            int lastCharacterIndex = m_textInfo.lineInfo[m_lineNumber].lastCharacterIndex = m_lastCharacterOfLine = m_characterCount - 1 > 0 ? m_characterCount - 1 : 0;
            m_textInfo.lineInfo[m_lineNumber].lastVisibleCharacterIndex = m_lastVisibleCharacterOfLine = m_lastVisibleCharacterOfLine < m_firstVisibleCharacterOfLine ? m_firstVisibleCharacterOfLine : m_lastVisibleCharacterOfLine;

            m_textInfo.lineInfo[m_lineNumber].characterCount = m_textInfo.lineInfo[m_lineNumber].lastCharacterIndex - m_textInfo.lineInfo[m_lineNumber].firstCharacterIndex + 1;
            m_textInfo.lineInfo[m_lineNumber].visibleCharacterCount = m_lineVisibleCharacterCount;
            m_textInfo.lineInfo[m_lineNumber].lineExtents.min = new Vector2(m_textInfo.characterInfo[m_firstVisibleCharacterOfLine].bottomLeft.x, lineDescender);
            m_textInfo.lineInfo[m_lineNumber].lineExtents.max = new Vector2(m_textInfo.characterInfo[m_lastVisibleCharacterOfLine].topRight.x, lineAscender);
            m_textInfo.lineInfo[m_lineNumber].width = width;

            float maxAdvanceOffset = (glyphAdjustment * currentElementScale + (m_currentFontAsset.normalSpacingOffset + characterSpacingAdjustment + boldSpacingAdjustment) * currentEmScale - m_cSpacing) * (1 - m_charWidthAdjDelta);
            float adjustedHorizontalAdvance = m_textInfo.lineInfo[m_lineNumber].maxAdvance = m_textInfo.characterInfo[m_lastVisibleCharacterOfLine].xAdvance + (m_isRightToLeft ? maxAdvanceOffset : - maxAdvanceOffset);
            m_textInfo.characterInfo[lastCharacterIndex].xAdvance = adjustedHorizontalAdvance;

            m_textInfo.lineInfo[m_lineNumber].ascender = lineAscender;
            m_textInfo.lineInfo[m_lineNumber].descender = lineDescender;

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

            m_xAdvance = 0 + tag_Indent;
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
            //state.maxFontScale = m_maxFontScale;
            state.fontScaleMultiplier = m_fontScaleMultiplier;
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
            state.marginLeft = m_marginLeft;
            state.marginRight = m_marginRight;

            state.vertexColor = m_htmlColor;

            state.isNonBreakingSpace = m_isNonBreakingSpace;
            state.tagNoParsing = tag_NoParsing;

            // XML Tag Stack
            state.basicStyleStack = m_fontStyleStack;
            state.italicAngleStack = m_ItalicAngleStack;
            state.colorStack = m_colorStack;
            state.sizeStack = m_sizeStack;
            state.indentStack = m_indentStack;
            state.fontWeightStack = m_FontWeightStack;
            //state.styleStack = m_styleStack;

            state.baselineStack = m_baselineOffsetStack;
            state.actionStack = m_actionStack;
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
            m_fontScaleMultiplier = state.fontScaleMultiplier;
            //m_maxFontScale = state.maxFontScale;
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
            m_marginLeft = state.marginLeft;
            m_marginRight = state.marginRight;

            m_htmlColor = state.vertexColor;

            m_isNonBreakingSpace = state.isNonBreakingSpace;
            tag_NoParsing = state.tagNoParsing;

            // XML Tag Stack
            m_fontStyleStack = state.basicStyleStack;
            m_ItalicAngleStack = state.italicAngleStack;
            m_colorStack = state.colorStack;
            m_sizeStack = state.sizeStack;
            m_indentStack = state.indentStack;
            m_FontWeightStack = state.fontWeightStack;
            //m_styleStack = state.styleStack;

            m_baselineOffsetStack = state.baselineStack;
            m_actionStack = state.actionStack;
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


            // Normal
            #region Setup Normals & Tangents
            //Vector3 normal = new Vector3(0, 0, -1);
            //m_textInfo.characterInfo[m_characterCount].vertex_BL.normal = normal;
            //m_textInfo.characterInfo[m_characterCount].vertex_TL.normal = normal;
            //m_textInfo.characterInfo[m_characterCount].vertex_TR.normal = normal;
            //m_textInfo.characterInfo[m_characterCount].vertex_BR.normal = normal;

            // Tangents
            //Vector4 tangent = new Vector4(-1, 0, 0, 1);
            //m_textInfo.characterInfo[m_characterCount].vertex_BL.tangent = tangent;
            //m_textInfo.characterInfo[m_characterCount].vertex_TL.tangent = tangent;
            //m_textInfo.characterInfo[m_characterCount].vertex_TR.tangent = tangent;
            //m_textInfo.characterInfo[m_characterCount].vertex_BR.tangent = tangent;
            #endregion end Normals & Tangents
        }


        /// <summary>
        /// Store vertex attributes into the appropriate TMP_MeshInfo.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="index_X4"></param>
        protected void FillCharacterVertexBuffers(int i, int index_X4)
        {
            int materialIndex = m_textInfo.characterInfo[i].materialReferenceIndex;
            index_X4 = m_textInfo.meshInfo[materialIndex].vertexCount;

            // Check to make sure our current mesh buffer allocations can hold these new Quads.
            if (index_X4 >= m_textInfo.meshInfo[materialIndex].vertices.Length)
                m_textInfo.meshInfo[materialIndex].ResizeMeshInfo(Mathf.NextPowerOfTwo((index_X4 + 4) / 4));


            var characterInfoArray = m_textInfo.characterInfo;

            // Setup Vertices for Characters
            m_textInfo.meshInfo[materialIndex].vertices[0 + index_X4] = characterInfoArray[i].vertex_BL.position;
            m_textInfo.meshInfo[materialIndex].vertices[1 + index_X4] = characterInfoArray[i].vertex_TL.position;
            m_textInfo.meshInfo[materialIndex].vertices[2 + index_X4] = characterInfoArray[i].vertex_TR.position;
            m_textInfo.meshInfo[materialIndex].vertices[3 + index_X4] = characterInfoArray[i].vertex_BR.position;


            // Setup UVS0
            m_textInfo.meshInfo[materialIndex].uvs0[0 + index_X4] = characterInfoArray[i].vertex_BL.uv;
            m_textInfo.meshInfo[materialIndex].uvs0[1 + index_X4] = characterInfoArray[i].vertex_TL.uv;
            m_textInfo.meshInfo[materialIndex].uvs0[2 + index_X4] = characterInfoArray[i].vertex_TR.uv;
            m_textInfo.meshInfo[materialIndex].uvs0[3 + index_X4] = characterInfoArray[i].vertex_BR.uv;


            // Setup UVS2
            m_textInfo.meshInfo[materialIndex].uvs2[0 + index_X4] = characterInfoArray[i].vertex_BL.uv2;
            m_textInfo.meshInfo[materialIndex].uvs2[1 + index_X4] = characterInfoArray[i].vertex_TL.uv2;
            m_textInfo.meshInfo[materialIndex].uvs2[2 + index_X4] = characterInfoArray[i].vertex_TR.uv2;
            m_textInfo.meshInfo[materialIndex].uvs2[3 + index_X4] = characterInfoArray[i].vertex_BR.uv2;


            // Setup UVS4
            //m_textInfo.meshInfo[0].uvs4[0 + index_X4] = characterInfoArray[i].vertex_BL.uv4;
            //m_textInfo.meshInfo[0].uvs4[1 + index_X4] = characterInfoArray[i].vertex_TL.uv4;
            //m_textInfo.meshInfo[0].uvs4[2 + index_X4] = characterInfoArray[i].vertex_TR.uv4;
            //m_textInfo.meshInfo[0].uvs4[3 + index_X4] = characterInfoArray[i].vertex_BR.uv4;


            // setup Vertex Colors
            m_textInfo.meshInfo[materialIndex].colors32[0 + index_X4] = characterInfoArray[i].vertex_BL.color;
            m_textInfo.meshInfo[materialIndex].colors32[1 + index_X4] = characterInfoArray[i].vertex_TL.color;
            m_textInfo.meshInfo[materialIndex].colors32[2 + index_X4] = characterInfoArray[i].vertex_TR.color;
            m_textInfo.meshInfo[materialIndex].colors32[3 + index_X4] = characterInfoArray[i].vertex_BR.color;

            m_textInfo.meshInfo[materialIndex].vertexCount = index_X4 + 4;
        }


        /// <summary>
        /// Internal function used to load the default settings of text objects.
        /// </summary>
        protected void LoadDefaultSettings()
        {
            if (m_fontSize == -99 || m_isWaitingOnResourceLoad)
            {
                var m_rectTransform = this.rectTransform;

                if (GetType() == typeof(TextMeshPro))
                {
                    if (m_rectTransform.sizeDelta == new Vector2(100, 100))
                        m_rectTransform.sizeDelta = TMP_Settings.defaultTextMeshProTextContainerSize;
                }
                else
                {
                    if (m_rectTransform.sizeDelta == new Vector2(100, 100))
                        m_rectTransform.sizeDelta = TMP_Settings.defaultTextMeshProUITextContainerSize;
                }

                m_enableWordWrapping = TMP_Settings.enableWordWrapping;
                m_enableKerning = TMP_Settings.enableKerning;
                m_enableExtraPadding = TMP_Settings.enableExtraPadding;
                m_fontSize = m_fontSizeBase = TMP_Settings.defaultFontSize;
                m_fontSizeMin = m_fontSize * TMP_Settings.defaultTextAutoSizingMinRatio;
                m_fontSizeMax = m_fontSize * TMP_Settings.defaultTextAutoSizingMaxRatio;
                m_isWaitingOnResourceLoad = false;
                raycastTarget = TMP_Settings.enableRaycastTarget;
                m_IsTextObjectScaleStatic = TMP_Settings.isTextObjectScaleStatic;
            }
            else if ((int)m_textAlignment < 0xFF)
            {
                // Convert Legacy TextAlignmentOptions enumerations from Unity 5.2 / 5.3.
                m_textAlignment = TMP_Compatibility.ConvertTextAlignmentEnumValues(m_textAlignment);
            }

            // Convert text alignment to independent horizontal and vertical alignment properties
            if (m_textAlignment != TextAlignmentOptions.Converted)
            {
                m_HorizontalAlignment = (HorizontalAlignmentOptions)((int)m_textAlignment & 0xFF);
                m_VerticalAlignment = (VerticalAlignmentOptions)((int)m_textAlignment & 0xFF00);
                m_textAlignment = TextAlignmentOptions.Converted;
            }
        }


        /// <summary>
        /// Replace a given number of characters (tag) in the array with a new character and shift subsequent characters in the array.
        /// </summary>
        /// <param name="chars">Array which contains the text.</param>
        /// <param name="insertionIndex">The index of where the new character will be inserted</param>
        /// <param name="tagLength">Length of the tag being replaced.</param>
        /// <param name="c">The replacement character.</param>
        protected void ReplaceTagWithCharacter(int[] chars, int insertionIndex, int tagLength, char c)
        {
            chars[insertionIndex] = c;

            for (int i = insertionIndex + tagLength; i < chars.Length; i++)
            {
                chars[i - 3] = chars[i];
            }
        }


        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        //protected int GetMaterialReferenceForFontWeight()
        //{
        //    //bool isItalic = (m_style & FontStyles.Italic) == FontStyles.Italic || (m_fontStyle & FontStyles.Italic) == FontStyles.Italic;

        //    m_currentMaterialIndex = MaterialReference.AddMaterialReference(m_currentFontAsset.fontWeights[0].italicTypeface.material, m_currentFontAsset.fontWeights[0].italicTypeface, m_materialReferences, m_materialReferenceIndexLookup);

        //    return 0;
        //}


        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        protected TMP_FontAsset GetFontAssetForWeight(int fontWeight)
        {
            bool isItalic = (m_FontStyleInternal & FontStyles.Italic) == FontStyles.Italic || (m_fontStyle & FontStyles.Italic) == FontStyles.Italic;

            TMP_FontAsset fontAsset = null;

            int weightIndex = fontWeight / 100;

            if (isItalic)
                fontAsset = m_currentFontAsset.fontWeightTable[weightIndex].italicTypeface;
            else
                fontAsset = m_currentFontAsset.fontWeightTable[weightIndex].regularTypeface;

            return fontAsset;
        }

        internal TMP_TextElement GetTextElement(uint unicode, TMP_FontAsset fontAsset, FontStyles fontStyle, FontWeight fontWeight, out bool isUsingAlternativeTypeface)
        {
            TMP_Character character = TMP_FontAssetUtilities.GetCharacterFromFontAsset(unicode, fontAsset, false, fontStyle, fontWeight, out isUsingAlternativeTypeface);

            if (character != null)
                return character;

            // Search potential list of fallback font assets assigned to the font asset.
            if (fontAsset.m_FallbackFontAssetTable != null && fontAsset.m_FallbackFontAssetTable.Count > 0)
                character = TMP_FontAssetUtilities.GetCharacterFromFontAssets(unicode, fontAsset, fontAsset.m_FallbackFontAssetTable, true, fontStyle, fontWeight, out isUsingAlternativeTypeface);

            if (character != null)
            {
                // Add character to font asset lookup cache
                //fontAsset.AddCharacterToLookupCache(unicode, character);

                return character;
            }

            // Search for the character in the primary font asset if not the current font asset
            if (fontAsset.instanceID != m_fontAsset.instanceID)
            {
                // Search primary font asset
                character = TMP_FontAssetUtilities.GetCharacterFromFontAsset(unicode, m_fontAsset, false, fontStyle, fontWeight, out isUsingAlternativeTypeface);

                // Use material and index of primary font asset.
                if (character != null)
                {
                    m_currentMaterialIndex = 0;
                    m_currentMaterial = m_materialReferences[0].material;

                    // Add character to font asset lookup cache
                    //fontAsset.AddCharacterToLookupCache(unicode, character);

                    return character;
                }

                // Search list of potential fallback font assets assigned to the primary font asset.
                if (m_fontAsset.m_FallbackFontAssetTable != null && m_fontAsset.m_FallbackFontAssetTable.Count > 0)
                    character = TMP_FontAssetUtilities.GetCharacterFromFontAssets(unicode, fontAsset, m_fontAsset.m_FallbackFontAssetTable, true, fontStyle, fontWeight, out isUsingAlternativeTypeface);

                if (character != null)
                {
                    // Add character to font asset lookup cache
                    //fontAsset.AddCharacterToLookupCache(unicode, character);

                    return character;
                }
            }

            // Search for the character in the list of fallback assigned in the TMP Settings (General Fallbacks).
            if (TMP_Settings.fallbackFontAssets != null && TMP_Settings.fallbackFontAssets.Count > 0)
                character = TMP_FontAssetUtilities.GetCharacterFromFontAssets(unicode, fontAsset, TMP_Settings.fallbackFontAssets, true, fontStyle, fontWeight, out isUsingAlternativeTypeface);

            if (character != null)
            {
                // Add character to font asset lookup cache
                //fontAsset.AddCharacterToLookupCache(unicode, character);

                return character;
            }

            // Search for the character in the Default Font Asset assigned in the TMP Settings file.
            if (TMP_Settings.defaultFontAsset != null)
                character = TMP_FontAssetUtilities.GetCharacterFromFontAsset(unicode, TMP_Settings.defaultFontAsset, true, fontStyle, fontWeight, out isUsingAlternativeTypeface);

            // Add character to font asset lookup cache
            //fontAsset.AddCharacterToLookupCache(unicode, character);
            return character;
        }


        /// <summary>
        /// Function to clear the geometry of the Primary and Sub Text objects.
        /// </summary>
        public virtual void ClearMesh() { }


        /// <summary>
        ///
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        protected float PackUV(float x, float y)
        {
            double x0 = (int)(x * 511);
            double y0 = (int)(y * 511);

            return (float)((x0 * 4096) + y0);
        }


        /// <summary>
        /// Function used as a replacement for LateUpdate()
        /// </summary>
        internal virtual void InternalUpdate() { }


        /// <summary>
        /// Method to convert Hex to Int
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        protected int HexToInt(char hex)
        {
            switch (hex)
            {
                case '0': return 0;
                case '1': return 1;
                case '2': return 2;
                case '3': return 3;
                case '4': return 4;
                case '5': return 5;
                case '6': return 6;
                case '7': return 7;
                case '8': return 8;
                case '9': return 9;
                case 'A': return 10;
                case 'B': return 11;
                case 'C': return 12;
                case 'D': return 13;
                case 'E': return 14;
                case 'F': return 15;
                case 'a': return 10;
                case 'b': return 11;
                case 'c': return 12;
                case 'd': return 13;
                case 'e': return 14;
                case 'f': return 15;
            }
            return 15;
        }


        private int GetUTF16(TextBackingContainer text, int i)
        {
            int unicode = 0;
            unicode += HexToInt((char)text[i]) << 12;
            unicode += HexToInt((char)text[i + 1]) << 8;
            unicode += HexToInt((char)text[i + 2]) << 4;
            unicode += HexToInt((char)text[i + 3]);
            return unicode;
        }


        int GetUTF32(TextBackingContainer text, int i)
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
        protected Color32 HexCharsToColor(char[] hexChars, int tagCount)
        {
            if (tagCount == 4)
            {
                byte r = (byte)(HexToInt(hexChars[1]) * 16 + HexToInt(hexChars[1]));
                byte g = (byte)(HexToInt(hexChars[2]) * 16 + HexToInt(hexChars[2]));
                byte b = (byte)(HexToInt(hexChars[3]) * 16 + HexToInt(hexChars[3]));

                return new Color32(r, g, b, 255);
            }
            else if (tagCount == 5)
            {
                byte r = (byte)(HexToInt(hexChars[1]) * 16 + HexToInt(hexChars[1]));
                byte g = (byte)(HexToInt(hexChars[2]) * 16 + HexToInt(hexChars[2]));
                byte b = (byte)(HexToInt(hexChars[3]) * 16 + HexToInt(hexChars[3]));
                byte a = (byte)(HexToInt(hexChars[4]) * 16 + HexToInt(hexChars[4]));

                return new Color32(r, g, b, a);
            }
            else if (tagCount == 7)
            {
                byte r = (byte)(HexToInt(hexChars[1]) * 16 + HexToInt(hexChars[2]));
                byte g = (byte)(HexToInt(hexChars[3]) * 16 + HexToInt(hexChars[4]));
                byte b = (byte)(HexToInt(hexChars[5]) * 16 + HexToInt(hexChars[6]));

                return new Color32(r, g, b, 255);
            }
            else if (tagCount == 9)
            {
                byte r = (byte)(HexToInt(hexChars[1]) * 16 + HexToInt(hexChars[2]));
                byte g = (byte)(HexToInt(hexChars[3]) * 16 + HexToInt(hexChars[4]));
                byte b = (byte)(HexToInt(hexChars[5]) * 16 + HexToInt(hexChars[6]));
                byte a = (byte)(HexToInt(hexChars[7]) * 16 + HexToInt(hexChars[8]));

                return new Color32(r, g, b, a);
            }
            else if (tagCount == 10)
            {
                byte r = (byte)(HexToInt(hexChars[7]) * 16 + HexToInt(hexChars[7]));
                byte g = (byte)(HexToInt(hexChars[8]) * 16 + HexToInt(hexChars[8]));
                byte b = (byte)(HexToInt(hexChars[9]) * 16 + HexToInt(hexChars[9]));

                return new Color32(r, g, b, 255);
            }
            else if (tagCount == 11)
            {
                byte r = (byte)(HexToInt(hexChars[7]) * 16 + HexToInt(hexChars[7]));
                byte g = (byte)(HexToInt(hexChars[8]) * 16 + HexToInt(hexChars[8]));
                byte b = (byte)(HexToInt(hexChars[9]) * 16 + HexToInt(hexChars[9]));
                byte a = (byte)(HexToInt(hexChars[10]) * 16 + HexToInt(hexChars[10]));

                return new Color32(r, g, b, a);
            }
            else if (tagCount == 13)
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
        /// Extracts a float value from char[] assuming we know the position of the start, end and decimal point.
        /// </summary>
        /// <param name="chars"></param>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        protected float ConvertToFloat(char[] chars, int startIndex, int length)
        {
            int lastIndex;

            return ConvertToFloat(chars, startIndex, length, out lastIndex);
        }


        /// <summary>
        /// Extracts a float value from char[] given a start index and length.
        /// </summary>
        /// <param name="chars"></param> The Char[] containing the numerical sequence.
        /// <param name="startIndex"></param> The index of the start of the numerical sequence.
        /// <param name="length"></param> The length of the numerical sequence.
        /// <param name="lastIndex"></param> Index of the last character in the validated sequence.
        /// <returns></returns>
        protected float ConvertToFloat(char[] chars, int startIndex, int length, out int lastIndex)
        {
            if (startIndex == 0)
            {
                lastIndex = 0;
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

                if (c >= '0' && c <= '9' || c == '.')
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
                        value = value + (c - 48) * decimalPointMultiplier * valueSignMultiplier;
                        decimalPointMultiplier *= 0.1f;
                    }

                    continue;
                }
                else if (c == ',')
                {
                    if (i + 1 < endIndex && chars[i + 1] == ' ')
                        lastIndex = i + 1;
                    else
                        lastIndex = i;

                    // Make sure value is within reasonable range.
                    if (value > 32767)
                        return Int16.MinValue;

                    return value;
                }
            }

            lastIndex = endIndex;

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
            TagValueType tagValueType = m_xmlAttribute[attributeIndex].valueType = TagValueType.None;
            TagUnitType tagUnitType = m_xmlAttribute[attributeIndex].unitType = TagUnitType.Pixels;

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
                        if (unicode == '+' || unicode == '-' || unicode == '.' || (unicode >= '0' && unicode <= '9'))
                        {
                            tagUnitType = TagUnitType.Pixels;
                            tagValueType = m_xmlAttribute[attributeIndex].valueType = TagValueType.NumericalValue;
                            m_xmlAttribute[attributeIndex].valueStartIndex = tagCharCount - 1;
                            m_xmlAttribute[attributeIndex].valueLength += 1;
                        }
                        else if (unicode == '#')
                        {
                            tagUnitType = TagUnitType.Pixels;
                            tagValueType = m_xmlAttribute[attributeIndex].valueType = TagValueType.ColorValue;
                            m_xmlAttribute[attributeIndex].valueStartIndex = tagCharCount - 1;
                            m_xmlAttribute[attributeIndex].valueLength += 1;
                        }
                        else if (unicode == '"')
                        {
                            tagUnitType = TagUnitType.Pixels;
                            tagValueType = m_xmlAttribute[attributeIndex].valueType = TagValueType.StringValue;
                            m_xmlAttribute[attributeIndex].valueStartIndex = tagCharCount;
                        }
                        else
                        {
                            tagUnitType = TagUnitType.Pixels;
                            tagValueType = m_xmlAttribute[attributeIndex].valueType = TagValueType.StringValue;
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
                            if (unicode == 'p' || unicode == 'e' || unicode == '%' || unicode == ' ')
                            {
                                attributeFlag = 2;
                                tagValueType = TagValueType.None;

                                switch (unicode)
                                {
                                    case 'e':
                                        m_xmlAttribute[attributeIndex].unitType = tagUnitType = TagUnitType.FontUnits;
                                        break;
                                    case '%':
                                        m_xmlAttribute[attributeIndex].unitType = tagUnitType = TagUnitType.Percentage;
                                        break;
                                    default:
                                        m_xmlAttribute[attributeIndex].unitType = tagUnitType = TagUnitType.Pixels;
                                        break;
                                }

                                attributeIndex += 1;
                                m_xmlAttribute[attributeIndex].nameHashCode = 0;
                                m_xmlAttribute[attributeIndex].valueHashCode = 0;
                                m_xmlAttribute[attributeIndex].valueType = TagValueType.None;
                                m_xmlAttribute[attributeIndex].unitType = TagUnitType.Pixels;
                                m_xmlAttribute[attributeIndex].valueStartIndex = 0;
                                m_xmlAttribute[attributeIndex].valueLength = 0;

                            }
                            else if (attributeFlag != 2)
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
                                m_xmlAttribute[attributeIndex].valueType = TagValueType.None;
                                m_xmlAttribute[attributeIndex].unitType = TagUnitType.Pixels;
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
                                m_xmlAttribute[attributeIndex].valueType = TagValueType.None;
                                m_xmlAttribute[attributeIndex].unitType = TagUnitType.Pixels;
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
                    m_xmlAttribute[attributeIndex].valueType = TagValueType.None;
                    m_xmlAttribute[attributeIndex].unitType = TagUnitType.Pixels;
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
            // Special handling of the no parsing tag </noparse> </NOPARSE> tag
            if (tag_NoParsing && (m_xmlAttribute[0].nameHashCode != 53822163 && m_xmlAttribute[0].nameHashCode != 49429939))
                return false;
            else if (m_xmlAttribute[0].nameHashCode == 53822163 || m_xmlAttribute[0].nameHashCode == 49429939)
            {
                tag_NoParsing = false;
                return true;
            }

            // Color <#FFF> 3 Hex values (short form)
            if (m_htmlTag[0] == 35 && tagCharCount == 4)
            {
                m_htmlColor = HexCharsToColor(m_htmlTag, tagCharCount);
                m_colorStack.Add(m_htmlColor);
                return true;
            }
            // Color <#FFF7> 4 Hex values with alpha (short form)
            else if (m_htmlTag[0] == 35 && tagCharCount == 5)
            {
                m_htmlColor = HexCharsToColor(m_htmlTag, tagCharCount);
                m_colorStack.Add(m_htmlColor);
                return true;
            }
            // Color <#FF00FF>
            else if (m_htmlTag[0] == 35 && tagCharCount == 7) // if Tag begins with # and contains 7 characters.
            {
                m_htmlColor = HexCharsToColor(m_htmlTag, tagCharCount);
                m_colorStack.Add(m_htmlColor);
                return true;
            }
            // Color <#FF00FF00> with alpha
            else if (m_htmlTag[0] == 35 && tagCharCount == 9) // if Tag begins with # and contains 9 characters.
            {
                m_htmlColor = HexCharsToColor(m_htmlTag, tagCharCount);
                m_colorStack.Add(m_htmlColor);
                return true;
            }
            else
            {
                float value = 0;
                float fontScale;

                switch (m_xmlAttribute[0].nameHashCode)
                {
                    case 98: // <b>
                    case 66: // <B>
                        m_FontStyleInternal |= FontStyles.Bold;
                        m_fontStyleStack.Add(FontStyles.Bold);

                        m_FontWeightInternal = FontWeight.Bold;
                        return true;
                    case 427: // </b>
                    case 395: // </B>
                        if ((m_fontStyle & FontStyles.Bold) != FontStyles.Bold)
                        {
                            if (m_fontStyleStack.Remove(FontStyles.Bold) == 0)
                            {
                                m_FontStyleInternal &= ~FontStyles.Bold;
                                m_FontWeightInternal = m_FontWeightStack.Peek();
                            }
                        }
                        return true;
                    case 105: // <i>
                    case 73: // <I>
                        m_FontStyleInternal |= FontStyles.Italic;
                        m_fontStyleStack.Add(FontStyles.Italic);

                        if (m_xmlAttribute[1].nameHashCode == 276531 || m_xmlAttribute[1].nameHashCode == 186899)
                        {
                            m_ItalicAngle = (int)ConvertToFloat(m_htmlTag, m_xmlAttribute[1].valueStartIndex, m_xmlAttribute[1].valueLength);

                            // Make sure angle is within valid range.
                            if (m_ItalicAngle < -180 || m_ItalicAngle > 180) return false;
                        }
                        else
                            m_ItalicAngle = m_currentFontAsset.italicStyle;

                        m_ItalicAngleStack.Add(m_ItalicAngle);

                        return true;
                    case 434: // </i>
                    case 402: // </I>
                        if ((m_fontStyle & FontStyles.Italic) != FontStyles.Italic)
                        {
                            m_ItalicAngle = m_ItalicAngleStack.Remove();

                            if (m_fontStyleStack.Remove(FontStyles.Italic) == 0)
                                m_FontStyleInternal &= ~FontStyles.Italic;
                        }
                        return true;
                    case 115: // <s>
                    case 83: // <S>
                        throw new NotSupportedException();
                    case 444: // </s>
                    case 412: // </S>
                        throw new NotSupportedException();
                    case 117: // <u>
                    case 85: // <U>
                        throw new NotSupportedException();
                    case 446: // </u>
                    case 414: // </U>
                        throw new NotSupportedException();
                    case 43045: // <mark=#FF00FF80>
                    case 30245: // <MARK>
                        throw new NotSupportedException();
                    case 155892: // </mark>
                    case 143092: // </MARK>
                        throw new NotSupportedException();
                    case -330774850: // <font-weight>
                    case 2012149182: // <FONT-WEIGHT>
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch ((int)value)
                        {
                            case 100:
                                m_FontWeightInternal = FontWeight.Thin;
                                break;
                            case 200:
                                m_FontWeightInternal = FontWeight.ExtraLight;
                                break;
                            case 300:
                                m_FontWeightInternal = FontWeight.Light;
                                break;
                            case 400:
                                m_FontWeightInternal = FontWeight.Regular;
                                break;
                            case 500:
                                m_FontWeightInternal = FontWeight.Medium;
                                break;
                            case 600:
                                m_FontWeightInternal = FontWeight.SemiBold;
                                break;
                            case 700:
                                m_FontWeightInternal = FontWeight.Bold;
                                break;
                            case 800:
                                m_FontWeightInternal = FontWeight.Heavy;
                                break;
                            case 900:
                                m_FontWeightInternal = FontWeight.Black;
                                break;
                        }

                        m_FontWeightStack.Add(m_FontWeightInternal);

                        return true;
                    case -1885698441: // </font-weight>
                    case 457225591: // </FONT-WEIGHT>
                        m_FontWeightStack.Remove();

                        if (m_FontStyleInternal == FontStyles.Bold)
                            m_FontWeightInternal = FontWeight.Bold;
                        else
                            m_FontWeightInternal = m_FontWeightStack.Peek();

                        return true;
                    case 6380: // <pos=000.00px> <pos=0em> <pos=50%>
                    case 4556: // <POS>
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                m_xAdvance = value * (m_isOrthographic ? 1.0f : 0.1f);
                                //m_isIgnoringAlignment = true;
                                return true;
                            case TagUnitType.FontUnits:
                                m_xAdvance = value * m_currentFontSize * (m_isOrthographic ? 1.0f : 0.1f);
                                //m_isIgnoringAlignment = true;
                                return true;
                            case TagUnitType.Percentage:
                                m_xAdvance = m_marginWidth * value / 100;
                                //m_isIgnoringAlignment = true;
                                return true;
                        }
                        return false;
                    case 22501: // </pos>
                    case 20677: // </POS>
                        return true;
                    case 16034505: // <voffset>
                    case 11642281: // <VOFFSET>
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                m_baselineOffset = value * (m_isOrthographic ? 1 : 0.1f);
                                return true;
                            case TagUnitType.FontUnits:
                                m_baselineOffset = value * (m_isOrthographic ? 1 : 0.1f) * m_currentFontSize;
                                return true;
                            case TagUnitType.Percentage:
                                //m_baselineOffset = m_marginHeight * val / 100;
                                return false;
                        }
                        return false;
                    case 54741026: // </voffset>
                    case 50348802: // </VOFFSET>
                        m_baselineOffset = 0;
                        return true;
                    // <BR> tag is now handled inline where it is replaced by a linefeed or \n.
                    //case 544: // <BR>
                    //case 800: // <br>
                    //    m_forceLineBreak = true;
                    //    return true;
                    case 43969: // <nobr>
                    case 31169: // <NOBR>
                        m_isNonBreakingSpace = true;
                        return true;
                    case 156816: // </nobr>
                    case 144016: // </NOBR>
                        m_isNonBreakingSpace = false;
                        return true;
                    case 45545: // <size=>
                    case 32745: // <SIZE>
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength);

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
                    case 145592: // </SIZE>
                        m_currentFontSize = m_sizeStack.Remove();
                        return true;
                    case 41311: // <font=xx>
                    case 28511: // <FONT>
                        int fontHashCode = m_xmlAttribute[0].valueHashCode;
                        int materialAttributeHashCode = m_xmlAttribute[1].nameHashCode;
                        int materialHashCode = m_xmlAttribute[1].valueHashCode;

                        // Special handling for <font=default> or <font=Default>
                        if (fontHashCode == 764638571 || fontHashCode == 523367755)
                        {
                            m_currentFontAsset = m_materialReferences[0].fontAsset;
                            m_currentMaterial = m_materialReferences[0].material;
                            m_currentMaterialIndex = 0;
                            //Debug.Log("<font=Default> assigning Font Asset [" + m_currentFontAsset.name + "] with Material [" + m_currentMaterial.name + "].");

                            m_materialReferenceStack.Add(m_materialReferences[0]);

                            return true;
                        }

                        TMP_FontAsset tempFont;
                        Material tempMaterial;

                        // HANDLE NEW FONT ASSET
                        //TMP_ResourceManager.TryGetFontAsset(fontHashCode, out tempFont);

                        // Check if we already have a reference to this font asset.
                        MaterialReferenceManager.TryGetFontAsset(fontHashCode, out tempFont);

                        // Try loading font asset from potential delegate or resources.
                        if (tempFont == null)
                        {
                            // Check for anyone registered to this callback
                            tempFont = null;

                            if (tempFont == null)
                            {
                                // Load Font Asset
                                tempFont = Resources.Load<TMP_FontAsset>(TMP_Settings.defaultFontAssetPath + new string(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength));
                            }

                            if (tempFont == null)
                                return false;

                            // Add new reference to the font asset as well as default material to the MaterialReferenceManager
                            MaterialReferenceManager.AddFontAsset(tempFont);
                        }

                        // HANDLE NEW MATERIAL
                        if (materialAttributeHashCode == 0 && materialHashCode == 0)
                        {
                            // No material specified then use default font asset material.
                            m_currentMaterial = tempFont.material;

                            m_currentMaterialIndex = MaterialReference.AddMaterialReference(m_currentMaterial, tempFont, ref m_materialReferences, m_materialReferenceIndexLookup);

                            m_materialReferenceStack.Add(m_materialReferences[m_currentMaterialIndex]);
                        }
                        else if (materialAttributeHashCode == 103415287 || materialAttributeHashCode == 72669687) // using material attribute
                        {
                            if (MaterialReferenceManager.TryGetMaterial(materialHashCode, out tempMaterial))
                            {
                                m_currentMaterial = tempMaterial;

                                m_currentMaterialIndex = MaterialReference.AddMaterialReference(m_currentMaterial, tempFont, ref m_materialReferences, m_materialReferenceIndexLookup);

                                m_materialReferenceStack.Add(m_materialReferences[m_currentMaterialIndex]);
                            }
                            else
                            {
                                // Load new material
                                tempMaterial = Resources.Load<Material>(TMP_Settings.defaultFontAssetPath + new string(m_htmlTag, m_xmlAttribute[1].valueStartIndex, m_xmlAttribute[1].valueLength));

                                if (tempMaterial == null)
                                    return false;

                                // Add new reference to this material in the MaterialReferenceManager
                                MaterialReferenceManager.AddFontMaterial(materialHashCode, tempMaterial);

                                m_currentMaterial = tempMaterial;

                                m_currentMaterialIndex = MaterialReference.AddMaterialReference(m_currentMaterial, tempFont, ref m_materialReferences, m_materialReferenceIndexLookup);

                                m_materialReferenceStack.Add(m_materialReferences[m_currentMaterialIndex]);
                            }
                        }
                        else
                            return false;

                        m_currentFontAsset = tempFont;

                        return true;
                    case 154158: // </font>
                    case 141358: // </FONT>
                        {
                            MaterialReference materialReference = m_materialReferenceStack.Remove();

                            m_currentFontAsset = materialReference.fontAsset;
                            m_currentMaterial = materialReference.material;
                            m_currentMaterialIndex = materialReference.index;

                            return true;
                        }
                    case 103415287: // <material="material name">
                    case 72669687: // <MATERIAL>
                        materialHashCode = m_xmlAttribute[0].valueHashCode;

                        // Special handling for <material=default> or <material=Default>
                        if (materialHashCode == 764638571 || materialHashCode == 523367755)
                        {
                            // Check if material font atlas texture matches that of the current font asset.
                            //if (m_currentFontAsset.atlas.GetInstanceID() != m_currentMaterial.GetTexture(ShaderUtilities.ID_MainTex).GetInstanceID()) return false;

                            m_currentMaterial = m_materialReferences[0].material;
                            m_currentMaterialIndex = 0;

                            m_materialReferenceStack.Add(m_materialReferences[0]);

                            return true;
                        }


                        // Check if material
                        if (MaterialReferenceManager.TryGetMaterial(materialHashCode, out tempMaterial))
                        {
                            // Check if material font atlas texture matches that of the current font asset.
                            //if (m_currentFontAsset.atlas.GetInstanceID() != tempMaterial.GetTexture(ShaderUtilities.ID_MainTex).GetInstanceID()) return false;

                            m_currentMaterial = tempMaterial;

                            m_currentMaterialIndex = MaterialReference.AddMaterialReference(m_currentMaterial, m_currentFontAsset, ref m_materialReferences, m_materialReferenceIndexLookup);

                            m_materialReferenceStack.Add(m_materialReferences[m_currentMaterialIndex]);
                        }
                        else
                        {
                            // Load new material
                            tempMaterial = Resources.Load<Material>(TMP_Settings.defaultFontAssetPath + new string(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength));

                            if (tempMaterial == null)
                                return false;

                            // Check if material font atlas texture matches that of the current font asset.
                            //if (m_currentFontAsset.atlas.GetInstanceID() != tempMaterial.GetTexture(ShaderUtilities.ID_MainTex).GetInstanceID()) return false;

                            // Add new reference to this material in the MaterialReferenceManager
                            MaterialReferenceManager.AddFontMaterial(materialHashCode, tempMaterial);

                            m_currentMaterial = tempMaterial;

                            m_currentMaterialIndex = MaterialReference.AddMaterialReference(m_currentMaterial, m_currentFontAsset, ref m_materialReferences, m_materialReferenceIndexLookup);

                            m_materialReferenceStack.Add(m_materialReferences[m_currentMaterialIndex]);
                        }
                        return true;
                    case 374360934: // </material>
                    case 343615334: // </MATERIAL>
                        {
                            //if (m_currentMaterial.GetTexture(ShaderUtilities.ID_MainTex).GetInstanceID() != m_materialReferenceStack.PreviousItem().material.GetTexture(ShaderUtilities.ID_MainTex).GetInstanceID())
                            //    return false;

                            MaterialReference materialReference = m_materialReferenceStack.Remove();

                            m_currentMaterial = materialReference.material;
                            m_currentMaterialIndex = materialReference.index;

                            return true;
                        }
                    case 320078: // <space=000.00>
                    case 230446: // <SPACE>
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                m_xAdvance += value * (m_isOrthographic ? 1 : 0.1f);
                                return true;
                            case TagUnitType.FontUnits:
                                m_xAdvance += value * (m_isOrthographic ? 1 : 0.1f) * m_currentFontSize;
                                return true;
                            case TagUnitType.Percentage:
                                // Not applicable
                                return false;
                        }
                        return false;
                    case 276254: // <alpha=#FF>
                    case 186622: // <ALPHA>
                        if (m_xmlAttribute[0].valueLength != 3) return false;

                        m_htmlColor.a = (byte)(HexToInt(m_htmlTag[7]) * 16 + HexToInt(m_htmlTag[8]));
                        return true;

                    case 1750458: // <a name=" ">
                        return false;
                    case 426: // </a>
                        return true;
                    case 275917: // <align=>
                    case 186285: // <ALIGN>
                        switch (m_xmlAttribute[0].valueHashCode)
                        {
                            case 3774683: // <align=left>
                                m_lineJustification = HorizontalAlignmentOptions.Left;
                                m_lineJustificationStack.Add(m_lineJustification);
                                return true;
                            case 136703040: // <align=right>
                                m_lineJustification = HorizontalAlignmentOptions.Right;
                                m_lineJustificationStack.Add(m_lineJustification);
                                return true;
                            case -458210101: // <align=center>
                                m_lineJustification = HorizontalAlignmentOptions.Center;
                                m_lineJustificationStack.Add(m_lineJustification);
                                return true;
                            case -523808257: // <align=justified>
                                m_lineJustification = HorizontalAlignmentOptions.Justified;
                                m_lineJustificationStack.Add(m_lineJustification);
                                return true;
                            case 122383428: // <align=flush>
                                m_lineJustification = HorizontalAlignmentOptions.Flush;
                                m_lineJustificationStack.Add(m_lineJustification);
                                return true;
                        }
                        return false;
                    case 1065846: // </align>
                    case 976214: // </ALIGN>
                        m_lineJustification = m_lineJustificationStack.Remove();
                        return true;
                    case 327550: // <width=xx>
                    case 237918: // <WIDTH>
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                m_width = value * (m_isOrthographic ? 1 : 0.1f);
                                break;
                            case TagUnitType.FontUnits:
                                return false;
                            //break;
                            case TagUnitType.Percentage:
                                m_width = m_marginWidth * value / 100;
                                break;
                        }
                        return true;
                    case 1117479: // </width>
                    case 1027847: // </WIDTH>
                        m_width = -1;
                        return true;
                    // STYLE tag is now handled inline and replaced by its definition.
                    //case 322689: // <style="name">
                    //case 233057: // <STYLE>
                    //    TMP_Style style = TMP_StyleSheet.GetStyle(m_xmlAttribute[0].valueHashCode);

                    //    if (style == null) return false;

                    //    m_styleStack.Add(style.hashCode);

                    //    // Parse Style Macro
                    //    for (int i = 0; i < style.styleOpeningTagArray.Length; i++)
                    //    {
                    //        if (style.styleOpeningTagArray[i] == 60)
                    //        {
                    //            if (ValidateHtmlTag(style.styleOpeningTagArray, i + 1, out i) == false) return false;
                    //        }
                    //    }
                    //    return true;
                    //case 1112618: // </style>
                    //case 1022986: // </STYLE>
                    //    style = TMP_StyleSheet.GetStyle(m_xmlAttribute[0].valueHashCode);

                    //    if (style == null)
                    //    {
                    //        // Get style from the Style Stack
                    //        int styleHashCode = m_styleStack.CurrentItem();
                    //        style = TMP_StyleSheet.GetStyle(styleHashCode);

                    //        m_styleStack.Remove();
                    //    }

                    //    if (style == null) return false;
                    //    //// Parse Style Macro
                    //    for (int i = 0; i < style.styleClosingTagArray.Length; i++)
                    //    {
                    //        if (style.styleClosingTagArray[i] == 60)
                    //            ValidateHtmlTag(style.styleClosingTagArray, i + 1, out i);
                    //    }
                    //    return true;
                    case 281955: // <color> <color=#FF00FF> or <color=#FF00FF00>
                    case 192323: // <COLOR=#FF00FF>
                        // <color=#FFF> 3 Hex (short hand)
                        if (m_htmlTag[6] == 35 && tagCharCount == 10)
                        {
                            m_htmlColor = HexCharsToColor(m_htmlTag, tagCharCount);
                            m_colorStack.Add(m_htmlColor);
                            return true;
                        }
                        // <color=#FFF7> 4 Hex (short hand)
                        else if (m_htmlTag[6] == 35 && tagCharCount == 11)
                        {
                            m_htmlColor = HexCharsToColor(m_htmlTag, tagCharCount);
                            m_colorStack.Add(m_htmlColor);
                            return true;
                        }
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

                        // <color=name>
                        switch (m_xmlAttribute[0].valueHashCode)
                        {
                            case 125395: // <color=red>
                                m_htmlColor = Color.red;
                                m_colorStack.Add(m_htmlColor);
                                return true;
                            case -992792864: // <color=lightblue>
                                m_htmlColor = new Color32(173, 216, 230, 255);
                                m_colorStack.Add(m_htmlColor);
                                return true;
                            case 3573310: // <color=blue>
                                m_htmlColor = Color.blue;
                                m_colorStack.Add(m_htmlColor);
                                return true;
                            case 3680713: // <color=grey>
                                m_htmlColor = new Color32(128, 128, 128, 255);
                                m_colorStack.Add(m_htmlColor);
                                return true;
                            case 117905991: // <color=black>
                                m_htmlColor = Color.black;
                                m_colorStack.Add(m_htmlColor);
                                return true;
                            case 121463835: // <color=green>
                                m_htmlColor = Color.green;
                                m_colorStack.Add(m_htmlColor);
                                return true;
                            case 140357351: // <color=white>
                                m_htmlColor = Color.white;
                                m_colorStack.Add(m_htmlColor);
                                return true;
                            case 26556144: // <color=orange>
                                m_htmlColor = new Color32(255, 128, 0, 255);
                                m_colorStack.Add(m_htmlColor);
                                return true;
                            case -36881330: // <color=purple>
                                m_htmlColor = new Color32(160, 32, 240, 255);
                                m_colorStack.Add(m_htmlColor);
                                return true;
                            case 554054276: // <color=yellow>
                                m_htmlColor = Color.yellow;
                                m_colorStack.Add(m_htmlColor);
                                return true;
                        }
                        return false;

                    case 1983971: // <cspace=xx.x>
                    case 1356515: // <CSPACE>
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                m_cSpacing = value * (m_isOrthographic ? 1 : 0.1f);
                                break;
                            case TagUnitType.FontUnits:
                                m_cSpacing = value * (m_isOrthographic ? 1 : 0.1f) * m_currentFontSize;
                                break;
                            case TagUnitType.Percentage:
                                return false;
                        }
                        return true;
                    case 7513474: // </cspace>
                    case 6886018: // </CSPACE>
                        if (!m_isParsingText) return true;

                        // Adjust xAdvance to remove extra space from last character.
                        if (m_characterCount > 0)
                        {
                            m_xAdvance -= m_cSpacing;
                            m_textInfo.characterInfo[m_characterCount - 1].xAdvance = m_xAdvance;
                        }
                        m_cSpacing = 0;
                        return true;
                    case 2152041: // <mspace=xx.x>
                    case 1524585: // <MSPACE>
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                m_monoSpacing = value * (m_isOrthographic ? 1 : 0.1f);
                                break;
                            case TagUnitType.FontUnits:
                                m_monoSpacing = value * (m_isOrthographic ? 1 : 0.1f) * m_currentFontSize;
                                break;
                            case TagUnitType.Percentage:
                                return false;
                        }
                        return true;
                    case 7681544: // </mspace>
                    case 7054088: // </MSPACE>
                        m_monoSpacing = 0;
                        return true;
                    case 280416: // <class="name">
                        return false;
                    case 1071884: // </color>
                    case 982252: // </COLOR>
                        m_htmlColor = m_colorStack.Remove();
                        return true;
                    case 2068980: // <indent=10px> <indent=10em> <indent=50%>
                    case 1441524: // <INDENT>
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                tag_Indent = value * (m_isOrthographic ? 1 : 0.1f);
                                break;
                            case TagUnitType.FontUnits:
                                tag_Indent = value * (m_isOrthographic ? 1 : 0.1f) * m_currentFontSize;
                                break;
                            case TagUnitType.Percentage:
                                tag_Indent = m_marginWidth * value / 100;
                                break;
                        }
                        m_indentStack.Add(tag_Indent);

                        m_xAdvance = tag_Indent;
                        return true;
                    case 7598483: // </indent>
                    case 6971027: // </INDENT>
                        tag_Indent = m_indentStack.Remove();
                        //m_xAdvance = tag_Indent;
                        return true;
                    case 1109386397: // <line-indent>
                    case -842656867: // <LINE-INDENT>
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                tag_LineIndent = value * (m_isOrthographic ? 1 : 0.1f);
                                break;
                            case TagUnitType.FontUnits:
                                tag_LineIndent = value * (m_isOrthographic ? 1 : 0.1f) * m_currentFontSize;
                                break;
                            case TagUnitType.Percentage:
                                tag_LineIndent = m_marginWidth * value / 100;
                                break;
                        }

                        m_xAdvance += tag_LineIndent;
                        return true;
                    case -445537194: // </line-indent>
                    case 1897386838: // </LINE-INDENT>
                        tag_LineIndent = 0;
                        return true;
                    case 2109854: // <margin=00.0> <margin=00em> <margin=50%>
                    case 1482398: // <MARGIN>
                        // Check value type
                        switch (m_xmlAttribute[0].valueType)
                        {
                            case TagValueType.NumericalValue:
                                value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength); // px

                                // Reject tag if value is invalid.
                                if (value == Int16.MinValue) return false;

                                // Determine tag unit type
                                switch (tagUnitType)
                                {
                                    case TagUnitType.Pixels:
                                        m_marginLeft = value * (m_isOrthographic ? 1 : 0.1f);
                                        break;
                                    case TagUnitType.FontUnits:
                                        m_marginLeft = value * (m_isOrthographic ? 1 : 0.1f) * m_currentFontSize;
                                        break;
                                    case TagUnitType.Percentage:
                                        m_marginLeft = (m_marginWidth - (m_width != -1 ? m_width : 0)) * value / 100;
                                        break;
                                }
                                m_marginLeft = m_marginLeft >= 0 ? m_marginLeft : 0;
                                m_marginRight = m_marginLeft;
                                return true;

                            case TagValueType.None:
                                for (int i = 1; i < m_xmlAttribute.Length && m_xmlAttribute[i].nameHashCode != 0; i++)
                                {
                                    // Get attribute name
                                    int nameHashCode = m_xmlAttribute[i].nameHashCode;

                                    switch (nameHashCode)
                                    {
                                        case 42823:  // <margin left=value>
                                            value = ConvertToFloat(m_htmlTag, m_xmlAttribute[i].valueStartIndex, m_xmlAttribute[i].valueLength); // px

                                            // Reject tag if value is invalid.
                                            if (value == Int16.MinValue) return false;

                                            switch (m_xmlAttribute[i].unitType)
                                            {
                                                case TagUnitType.Pixels:
                                                    m_marginLeft = value * (m_isOrthographic ? 1 : 0.1f);
                                                    break;
                                                case TagUnitType.FontUnits:
                                                    m_marginLeft = value * (m_isOrthographic ? 1 : 0.1f) * m_currentFontSize;
                                                    break;
                                                case TagUnitType.Percentage:
                                                    m_marginLeft = (m_marginWidth - (m_width != -1 ? m_width : 0)) * value / 100;
                                                    break;
                                            }
                                            m_marginLeft = m_marginLeft >= 0 ? m_marginLeft : 0;
                                            break;

                                        case 315620: // <margin right=value>
                                            value = ConvertToFloat(m_htmlTag, m_xmlAttribute[i].valueStartIndex, m_xmlAttribute[i].valueLength); // px

                                            // Reject tag if value is invalid.
                                            if (value == Int16.MinValue) return false;

                                            switch (m_xmlAttribute[i].unitType)
                                            {
                                                case TagUnitType.Pixels:
                                                    m_marginRight = value * (m_isOrthographic ? 1 : 0.1f);
                                                    break;
                                                case TagUnitType.FontUnits:
                                                    m_marginRight = value * (m_isOrthographic ? 1 : 0.1f) * m_currentFontSize;
                                                    break;
                                                case TagUnitType.Percentage:
                                                    m_marginRight = (m_marginWidth - (m_width != -1 ? m_width : 0)) * value / 100;
                                                    break;
                                            }
                                            m_marginRight = m_marginRight >= 0 ? m_marginRight : 0;
                                            break;
                                    }
                                }
                                return true;
                        }

                        return false;
                    case 7639357: // </margin>
                    case 7011901: // </MARGIN>
                        m_marginLeft = 0;
                        m_marginRight = 0;
                        return true;
                    case 1100728678: // <margin-left=xx.x>
                    case -855002522: // <MARGIN-LEFT>
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength); // px

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                m_marginLeft = value * (m_isOrthographic ? 1 : 0.1f);
                                break;
                            case TagUnitType.FontUnits:
                                m_marginLeft = value * (m_isOrthographic ? 1 : 0.1f) * m_currentFontSize;
                                break;
                            case TagUnitType.Percentage:
                                m_marginLeft = (m_marginWidth - (m_width != -1 ? m_width : 0)) * value / 100;
                                break;
                        }
                        m_marginLeft = m_marginLeft >= 0 ? m_marginLeft : 0;
                        return true;
                    case -884817987: // <margin-right=xx.x>
                    case -1690034531: // <MARGIN-RIGHT>
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength); // px

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                m_marginRight = value * (m_isOrthographic ? 1 : 0.1f);
                                break;
                            case TagUnitType.FontUnits:
                                m_marginRight = value * (m_isOrthographic ? 1 : 0.1f) * m_currentFontSize;
                                break;
                            case TagUnitType.Percentage:
                                m_marginRight = (m_marginWidth - (m_width != -1 ? m_width : 0)) * value / 100;
                                break;
                        }
                        m_marginRight = m_marginRight >= 0 ? m_marginRight : 0;
                        return true;
                    case 1109349752: // <line-height=xx.x>
                    case -842693512: // <LINE-HEIGHT>
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                m_lineHeight = value * (m_isOrthographic ? 1 : 0.1f);
                                break;
                            case TagUnitType.FontUnits:
                                m_lineHeight = value * (m_isOrthographic ? 1 : 0.1f) * m_currentFontSize;
                                break;
                            case TagUnitType.Percentage:
                                fontScale = (m_currentFontSize / m_currentFontAsset.faceInfo.pointSize * m_currentFontAsset.faceInfo.scale * (m_isOrthographic ? 1 : 0.1f));
                                m_lineHeight = m_fontAsset.faceInfo.lineHeight * value / 100 * fontScale;
                                break;
                        }
                        return true;
                    case -445573839: // </line-height>
                    case 1897350193: // </LINE-HEIGHT>
                        m_lineHeight = TMP_Math.FLOAT_UNSET;
                        return true;
                    case 15115642: // <noparse>
                    case 10723418: // <NOPARSE>
                        tag_NoParsing = true;
                        return true;
                    case 1913798: // <action>
                    case 1286342: // <ACTION>
                        int actionID = m_xmlAttribute[0].valueHashCode;

                        if (m_isParsingText)
                        {
                            m_actionStack.Add(actionID);

                            Debug.Log("Action ID: [" + actionID + "] First character index: " + m_characterCount);


                        }
                        //if (m_isParsingText)
                        //{
                        // TMP_Action action = TMP_Action.GetAction(m_xmlAttribute[0].valueHashCode);
                        //}
                        return true;
                    case 7443301: // </action>
                    case 6815845: // </ACTION>
                        if (m_isParsingText)
                        {
                            Debug.Log("Action ID: [" + m_actionStack.CurrentItem() + "] Last character index: " + (m_characterCount - 1));
                        }

                        m_actionStack.Remove();
                        return true;
                    case 315682: // <scale=xx.x>
                    case 226050: // <SCALE=xx.x>
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        m_FXMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(value, 1, 1));
                        m_isFXMatrixSet = true;

                        return true;
                    case 1105611: // </scale>
                    case 1015979: // </SCALE>
                        m_isFXMatrixSet = false;
                        return true;
                    case 2227963: // <rotate=xx.x>
                    case 1600507: // <ROTATE=xx.x>
                        // TODO: Add ability to use Random Rotation

                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        m_FXMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, value), Vector3.one);
                        m_isFXMatrixSet = true;

                        return true;
                    case 7757466: // </rotate>
                    case 7130010: // </ROTATE>
                        m_isFXMatrixSet = false;
                        return true;
                    case 317446: // <table>
                    case 227814: // <TABLE>
                        //switch (m_xmlAttribute[1].nameHashCode)
                        //{
                        //    case 327550: // width
                        //        float tableWidth = ConvertToFloat(m_htmlTag, m_xmlAttribute[1].valueStartIndex, m_xmlAttribute[1].valueLength);

                        //        // Reject tag if value is invalid.
                        //        if (tableWidth == Int16.MinValue) return false;

                        //        switch (tagUnitType)
                        //        {
                        //            case TagUnitType.Pixels:
                        //                Debug.Log("Table width = " + tableWidth + "px.");
                        //                break;
                        //            case TagUnitType.FontUnits:
                        //                Debug.Log("Table width = " + tableWidth + "em.");
                        //                break;
                        //            case TagUnitType.Percentage:
                        //                Debug.Log("Table width = " + tableWidth + "%.");
                        //                break;
                        //        }
                        //        break;
                        //}
                        return false;
                    case 1107375: // </table>
                    case 1017743: // </TABLE>
                        return true;
                    case 926: // <tr>
                    case 670: // <TR>
                        return true;
                    case 3229: // </tr>
                    case 2973: // </TR>
                        return true;
                    case 916: // <th>
                    case 660: // <TH>
                        // Set style to bold and center alignment
                        return true;
                    case 3219: // </th>
                    case 2963: // </TH>
                        return true;
                    case 912: // <td>
                    case 656: // <TD>
                              // Style options
                        //for (int i = 1; i < m_xmlAttribute.Length && m_xmlAttribute[i].nameHashCode != 0; i++)
                        //{
                        //    switch (m_xmlAttribute[i].nameHashCode)
                        //    {
                        //        case 327550: // width
                        //            float tableWidth = ConvertToFloat(m_htmlTag, m_xmlAttribute[i].valueStartIndex, m_xmlAttribute[i].valueLength);

                        //            switch (tagUnitType)
                        //            {
                        //                case TagUnitType.Pixels:
                        //                    Debug.Log("Table width = " + tableWidth + "px.");
                        //                    break;
                        //                case TagUnitType.FontUnits:
                        //                    Debug.Log("Table width = " + tableWidth + "em.");
                        //                    break;
                        //                case TagUnitType.Percentage:
                        //                    Debug.Log("Table width = " + tableWidth + "%.");
                        //                    break;
                        //            }
                        //            break;
                        //        case 275917: // align
                        //            switch (m_xmlAttribute[i].valueHashCode)
                        //            {
                        //                case 3774683: // left
                        //                    Debug.Log("TD align=\"left\".");
                        //                    break;
                        //                case 136703040: // right
                        //                    Debug.Log("TD align=\"right\".");
                        //                    break;
                        //                case -458210101: // center
                        //                    Debug.Log("TD align=\"center\".");
                        //                    break;
                        //                case -523808257: // justified
                        //                    Debug.Log("TD align=\"justified\".");
                        //                    break;
                        //            }
                        //            break;
                        //    }
                        //}

                        return false;
                    case 3215: // </td>
                    case 2959: // </TD>
                        return false;
                }
            }
            #endif
            #endregion

            return false;
        }
    }
}
