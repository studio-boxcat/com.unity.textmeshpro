using System;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.UI;
using Object = UnityEngine.Object;

#pragma warning disable 0414 // Disabled a few warnings related to serialized variables not used in this script but used in the editor.
#pragma warning disable 0618 // Disabled warning due to SetVertices being deprecated until new release with SetMesh() is available.

namespace TMPro
{
    public sealed partial class TextMeshProUGUI
    {
        [SerializeField]
        bool m_hasFontAssetChanged = false; // Used to track when font properties have changed.

        float m_previousLossyScaleY = -1; // Used for Tracking lossy scale changes in the transform;

        Rect m_RectTransformRect;
        float m_CanvasScaleFactor;


        [NonSerialized]
        bool m_isRegisteredForEvents;

        // Profiler Marker declarations
        static ProfilerMarker k_GenerateTextMarker = new ProfilerMarker("TMP.GenerateText");
        static ProfilerMarker k_SetArraySizesMarker = new ProfilerMarker("TMP.SetArraySizes");
        static ProfilerMarker k_GenerateTextPhaseIMarker = new ProfilerMarker("TMP GenerateText - Phase I");
        static ProfilerMarker k_CharacterLookupMarker = new ProfilerMarker("TMP Lookup Character & Glyph Data");
        static ProfilerMarker k_CalculateVerticesPositionMarker = new ProfilerMarker("TMP Calculate Vertices Position");
        static ProfilerMarker k_ComputeTextMetricsMarker = new ProfilerMarker("TMP Compute Text Metrics");
        static ProfilerMarker k_HandleVisibleCharacterMarker = new ProfilerMarker("TMP Handle Visible Character");
        static ProfilerMarker k_HandleHorizontalLineBreakingMarker = new ProfilerMarker("TMP Handle Horizontal Line Breaking");
        static ProfilerMarker k_HandleVerticalLineBreakingMarker = new ProfilerMarker("TMP Handle Vertical Line Breaking");
        static ProfilerMarker k_SaveGlyphVertexDataMarker = new ProfilerMarker("TMP Save Glyph Vertex Data");
        static ProfilerMarker k_ComputeCharacterAdvanceMarker = new ProfilerMarker("TMP Compute Character Advance");
        static ProfilerMarker k_HandleCarriageReturnMarker = new ProfilerMarker("TMP Handle Carriage Return");
        static ProfilerMarker k_HandleLineTerminationMarker = new ProfilerMarker("TMP Handle Line Termination");
        static ProfilerMarker k_SavePageInfoMarker = new ProfilerMarker("TMP Save Text Extent & Page Info");
        static ProfilerMarker k_SaveProcessingStatesMarker = new ProfilerMarker("TMP Save Processing States");
        static ProfilerMarker k_GenerateTextPhaseIIMarker = new ProfilerMarker("TMP GenerateText - Phase II");
        static ProfilerMarker k_GenerateTextPhaseIIIMarker = new ProfilerMarker("TMP GenerateText - Phase III");


        void Awake()
        {
            //Debug.Log("***** Awake() called on object ID " + GetInstanceID() + ". *****");

            if (m_mesh == null)
            {
                m_mesh = MeshPool.CreateDynamicMesh("TextMeshPro UI Mesh");
                // Create new TextInfo for the text object.
                m_textInfo = new TMP_TextInfo(mesh);
            }

            // Load the font asset and assign material to renderer.
            LoadFontAsset();

            m_cached_TextElement = new TMP_Character();

            // Set flags to ensure our text is parsed and redrawn.
            m_havePropertiesChanged = true;

            m_isAwake = true;
        }


        protected override void OnEnable()
        {
            //Debug.Log("***** OnEnable() called on object ID " + GetInstanceID() + ". *****");

            // Return if Awake() has not been called on the text object.
            if (m_isAwake == false)
                return;

            if (!m_isRegisteredForEvents)
            {
                //Debug.Log("Registering for Events.");

                #if UNITY_EDITOR
                // Refresh live text when its font asset is regenerated via the Font Creator / inspector.
                TMPro_EventManager.FONT_PROPERTY_EVENT += ON_FONT_PROPERTY_CHANGED;
                #endif
                m_isRegisteredForEvents = true;
            }

            // Register Graphic Component to receive event triggers
            m_RaycastRegisterLink.Reset(canvas, this);

            // Register text object for internal updates
            TMP_UpdateManager.Register(this);

            ComputeMarginSize();

            SetAllDirty();
        }


        protected override void OnDisable()
        {
            //Debug.Log("***** OnDisable() called on object ID " + GetInstanceID() + ". *****");

            // Return if Awake() has not been called on the text object.
            if (m_isAwake == false)
                return;

            // UnRegister Graphic Component
            m_RaycastRegisterLink.TryUnlink(this);

            TMP_UpdateManager.Unregister(this);

            canvasRenderer.Clear();

            LayoutRebuilder.SetDirty(rectTransform);
        }


        void OnDestroy()
        {
            //Debug.Log("***** OnDestroy() called on object ID " + GetInstanceID() + ". *****");

            // UnRegister Graphic Component
            m_RaycastRegisterLink.TryUnlink(this);

            TMP_UpdateManager.Unregister(this);

            // Clean up remaining mesh
            if (m_mesh != null)
                DestroyImmediate(m_mesh);

            #if UNITY_EDITOR
            // Unregister the event this object was listening to
            TMPro_EventManager.FONT_PROPERTY_EVENT -= ON_FONT_PROPERTY_CHANGED;
            #endif
            m_isRegisteredForEvents = false;
        }


        #if UNITY_EDITOR
        // Event received when font asset properties are changed in Font Inspector
        void ON_FONT_PROPERTY_CHANGED(bool isChanged, Object font)
        {
            if ((TMP_FontAsset) font == m_fontAsset)
            {
                //Debug.Log("ON_FONT_PROPERTY_CHANGED event received.");
                m_havePropertiesChanged = true;

                UpdateMeshPadding();

                SetLayoutDirty();
                SetVerticesDirty();
            }
        }
        #endif


        // Function which loads either the default font or a newly assigned font asset. This function also assigned the appropriate material to the renderer.
        protected void LoadFontAsset()
        {
            //Debug.Log("***** LoadFontAsset() *****"); //TextMeshPro LoadFontAsset() has been called."); // Current Font Asset is " + (font != null ? font.name: "Null") );

            ShaderUtilities.GetShaderPropertyIDs(); // Initialize & Get shader property IDs.

            // Read font definition if needed.
            if (m_fontAsset.characterLookupTable == null)
                m_fontAsset.ReadFontAssetDefinition();

            // If font atlas texture doesn't match the assigned material font atlas, switch back to default material specified in the Font Asset.
            if (m_sharedMaterial.GetTexture(ShaderUtilities.ID_MainTex) == null || m_fontAsset.atlasTexture.GetInstanceID() != m_sharedMaterial.GetTexture(ShaderUtilities.ID_MainTex).GetInstanceID())
            {
                m_sharedMaterial = m_fontAsset.material;
            }


            m_padding = GetPaddingForMaterial();

            SetMaterialDirty();
        }


        // This function parses through the Char[] to determine how many characters will be visible. It then makes sure the arrays are large enough for all those characters.
        internal override int SetArraySizes()
        {
            k_SetArraySizesMarker.Begin();

            m_totalCharacterCount = 0;
            m_isUsingBold = false;

            // Set allocations for the text object's TextInfo
            if (m_textInfo == null)
                m_textInfo = new TMP_TextInfo(m_InternalTextProcessingArraySize);
            else if (m_textInfo.characterInfo.Length < m_InternalTextProcessingArraySize)
                TMP_TextInfo.Resize(ref m_textInfo.characterInfo, m_InternalTextProcessingArraySize, false);

            // Single-mesh only: count visible (non-whitespace) characters to size the mesh buffer.
            int visibleCharacterCount = 0;

            for (int i = 0; i < m_TextProcessingArray.Length && m_TextProcessingArray[i].unicode != 0; i++)
            {
                //Make sure the characterInfo array can hold the next text element.
                if (m_textInfo.characterInfo == null || m_totalCharacterCount >= m_textInfo.characterInfo.Length)
                    TMP_TextInfo.Resize(ref m_textInfo.characterInfo, m_totalCharacterCount + 1, true);

                int unicode = m_TextProcessingArray[i].unicode;

                // Lookup the Glyph data for each character and cache it.
                // Replace missing glyph by the Square (9633) glyph or possibly the Space (32) glyph.
                var character = TMP_FontAssetUtilities.GetCharacterFromFontAsset((uint)unicode, 32, m_fontAsset);

                // Save text element data
                m_textInfo.characterInfo[m_totalCharacterCount].textElement = character;
                m_textInfo.characterInfo[m_totalCharacterCount].character = (char)unicode;

                // Handle Multi Atlas Texture support
                if (character != null && character.glyph.atlasIndex > 0)
                    throw new NotSupportedException("[TMP] Multi Atlas Texture is not supported.");

                if (!char.IsWhiteSpace((char)unicode) && unicode != 0x200B)
                    visibleCharacterCount += 1;

                m_totalCharacterCount += 1;
            }

            // Early return if we are calculating the preferred values.
            if (m_isCalculatingPreferredValues)
            {
                m_isCalculatingPreferredValues = false;

                k_SetArraySizesMarker.End();
                return m_totalCharacterCount;
            }

            // Set the primary mesh buffer allocation (index 0).
            int referenceCount = visibleCharacterCount;
            if (m_textInfo.meshInfo.vertices == null || m_textInfo.meshInfo.vertices.Length < referenceCount * 4)
            {
                if (m_textInfo.meshInfo.vertices == null)
                    m_textInfo.meshInfo = new TMP_MeshInfo(m_mesh, referenceCount + 1);
                else
                    m_textInfo.meshInfo.ResizeMeshInfo(referenceCount > 1024 ? referenceCount + 256 : Mathf.NextPowerOfTwo(referenceCount + 1));
            }

            k_SetArraySizesMarker.End();
            return m_totalCharacterCount;
        }

        /// <summary>
        /// Update the margin width and height
        /// </summary>
        private void ComputeMarginSize()
        {
            //Debug.Log("*** ComputeMarginSize() *** Current RectTransform's Width is " + m_rectTransform.rect.width + " and Height is " + m_rectTransform.rect.height); // + " and size delta is "  + m_rectTransform.sizeDelta);
            var t = rectTransform;
            var rect = t.rect;

            m_marginWidth = rect.width;
            m_marginHeight = rect.height;

            // Cache current RectTransform width and pivot referenced in OnRectTransformDimensionsChange() to get around potential rounding error in the reported width of the RectTransform.
            m_PreviousRectTransformSize = rect.size;
            m_PreviousPivotPosition = t.pivot;

            // Update the corners of the RectTransform
            m_RectTransformRect = rect;
        }


        /// <summary>
        ///
        /// </summary>
        protected override void OnDidApplyAnimationProperties()
        {
            m_havePropertiesChanged = true;
            SetVerticesDirty();
            SetLayoutDirty();
            //Debug.Log("Animation Properties have changed.");
        }


        protected override void OnTransformParentChanged()
        {
            //Debug.Log("***** OnTransformParentChanged *****");

            base.OnTransformParentChanged();

            ComputeMarginSize();
            m_havePropertiesChanged = true;
        }


        protected override void OnRectTransformDimensionsChange()
        {
            //Debug.Log("*** OnRectTransformDimensionsChange() *** ActiveInHierarchy: " + this.gameObject.activeInHierarchy + "  Frame: " + Time.frameCount);

            // Make sure object is active in Hierarchy
            if (!isActiveAndEnabled)
                return;

            // Check if Canvas scale factor has changed as this requires an update of the SDF Scale.
            var canvas = this.canvas;
            bool hasCanvasScaleFactorChanged = false;
            if (m_CanvasScaleFactor != canvas.scaleFactor)
            {
                m_CanvasScaleFactor = canvas.scaleFactor;
                hasCanvasScaleFactorChanged = true;
            }

            // Ignore changes to RectTransform SizeDelta that are very small and typically the result of rounding errors when using RectTransform in Anchor Stretch mode.
            var t = rectTransform;
            if (hasCanvasScaleFactorChanged == false &&
                Mathf.Abs(t.rect.width - m_PreviousRectTransformSize.x) < 0.0001f && Mathf.Abs(t.rect.height - m_PreviousRectTransformSize.y) < 0.0001f &&
                Mathf.Abs(t.pivot.x - m_PreviousPivotPosition.x) < 0.0001f && Mathf.Abs(t.pivot.y - m_PreviousPivotPosition.y) < 0.0001f)
            {
                return;
            }

            ComputeMarginSize();

            SetVerticesDirty();
            SetLayoutDirty();
        }


        /// <summary>
        /// Function used as a replacement for LateUpdate to check if the transform or scale of the text object has changed.
        /// </summary>
        internal override void InternalUpdate()
        {
            // We need to update the SDF scale or possibly regenerate the text object if lossy scale has changed.
            if (m_havePropertiesChanged == false)
            {
                float lossyScaleY = transform.lossyScale.y;

                // Ignore very small lossy scale changes as their effect on SDF Scale would not be visually noticeable.
                // Do not update SDF Scale if the text is null or empty
                if (Mathf.Abs(lossyScaleY - m_previousLossyScaleY) > 0.0001f && m_TextProcessingArray[0].unicode != 0)
                {
                    float scaleDelta = lossyScaleY / m_previousLossyScaleY;

                    UpdateSDFScale(scaleDelta);

                    m_previousLossyScaleY = lossyScaleY;
                }
            }
        }


        /// <summary>
        /// Function called when the text needs to be updated.
        /// </summary>
        void OnPreRenderCanvas()
        {
            //Debug.Log("*** OnPreRenderCanvas() *** Frame: " + Time.frameCount);

            // Make sure object is active and that we have a valid Canvas.
            if (!m_isAwake || (this.IsActive() == false && m_ignoreActiveState == false))
                return;

            if (m_havePropertiesChanged || m_isLayoutDirty)
            {
                //Debug.Log("Properties have changed!"); // Assigned Material is:" + m_sharedMaterial); // New Text is: " + m_text + ".");

                // Update mesh padding if necessary.
                if (checkPaddingRequired)
                    UpdateMeshPadding();

                // Reparse the text as input may have changed or been truncated.
                ParseInputText();

                m_lineSpacingDelta = 0;

                m_havePropertiesChanged = false;
                m_isLayoutDirty = false;
                m_ignoreActiveState = false;

                GenerateTextMesh();
            }
        }


        /// <summary>
        /// This is the main function that is responsible for creating / displaying the text.
        /// </summary>
        void GenerateTextMesh()
        {
            k_GenerateTextMarker.Begin();

            // Early exit if no font asset was assigned. This should not be needed since LiberationSans SDF will be assigned by default.
            if (m_fontAsset.characterLookupTable == null)
            {
                Debug.LogWarning("Can't Generate Mesh! No Font Asset has been assigned to Object ID: " + this.GetInstanceID());
                k_GenerateTextMarker.End();
                return;
            }

            // Clear TextInfo
            if (m_textInfo != null)
                m_textInfo.Clear();

            // Early exit if we don't have any Text to generate.
            if (m_TextProcessingArray == null || m_TextProcessingArray.Length == 0 || m_TextProcessingArray[0].unicode == 0)
            {
                // Clear mesh and upload changes to the mesh.
                ClearMesh();

                m_preferredWidth = 0;
                m_preferredHeight = 0;

                k_GenerateTextMarker.End();
                return;
            }

            // Total character count is computed when the text is parsed.
            int totalCharacterCount = m_totalCharacterCount;

            // Calculate the scale of the font based on selected font size and sampling point size.
            // baseScale is calculated using the font asset assigned to the text object.
            float baseScale = (m_fontSize / m_fontAsset.m_FaceInfo.pointSize * m_fontAsset.m_FaceInfo.scale);
            float currentEmScale = m_fontSize * 0.01f;

            m_currentFontSize = m_fontSize;

            int charCode = 0; // Holds the character code of the currently being processed character.

            m_lineJustification = m_HorizontalAlignment; // Sets the line justification mode to match editor alignment.

            float padding = 0;
            float style_padding = 0;

            m_baselineOffset = 0;

            m_fontColor32 = m_fontColor;
            m_htmlColor = m_fontColor32;

            m_lineOffset = 0; // Amount of space between lines (font line spacing + m_linespacing).
            m_lineHeight = TMP_Math.FLOAT_UNSET;
            float lineGap = m_fontAsset.m_FaceInfo.lineHeight - (m_fontAsset.m_FaceInfo.ascentLine - m_fontAsset.m_FaceInfo.descentLine);

            m_cSpacing = 0; // Amount of space added between characters as a result of the use of the <cspace> tag.
            m_monoSpacing = 0;
            m_xAdvance = 0; // Used to track the position of each character.

            m_characterCount = 0; // Total characters in the char[]

            // Tracking of line information
            m_lastCharacterOfLine = 0;
            m_firstVisibleCharacterOfLine = 0;
            m_lastVisibleCharacterOfLine = 0;
            m_maxLineAscender = k_LargeNegativeFloat;
            m_maxLineDescender = k_LargePositiveFloat;
            m_lineNumber = 0;
            m_startOfLineAscender = 0;
            bool isStartOfNewLine = true;
            m_IsDrivenLineSpacing = false;
            m_firstOverflowCharacterIndex = -1;

            float marginWidth = m_marginWidth > 0 ? m_marginWidth : 0;
            float marginHeight = m_marginHeight > 0 ? m_marginHeight : 0;
            m_width = -1;
            float widthOfTextArea = marginWidth + 0.0001f;

            // Need to initialize these Extents structures
            m_meshExtents.min = k_LargePositiveVector2;
            m_meshExtents.max = k_LargeNegativeVector2;

            // Initialize lineInfo
            m_textInfo.ClearLineInfo();

            // Tracking of the highest Ascender
            m_maxCapHeight = 0;
            m_maxTextAscender = 0;
            m_ElementDescender = 0;
            m_PageAscender = 0;
            float maxVisibleDescender = 0;
            bool isMaxVisibleDescenderSet = false;
            bool isSoftHyphenIgnored = false;

            // Save character and line state before we begin layout.

            k_GenerateTextPhaseIMarker.Begin();

            // Parse through Character buffer to read HTML tags and begin creating mesh.
            for (int i = 0; i < m_TextProcessingArray.Length && m_TextProcessingArray[i].unicode != 0; i++)
            {
                charCode = m_TextProcessingArray[i].unicode;

                // Look up Character Data from Dictionary and cache it.
                #region Look up Character Data
                k_CharacterLookupMarker.Begin();

                float baselineOffset = 0;
                float elementAscentLine = 0;
                float elementDescentLine = 0;
                float currentElementScale;
                {
                    m_cached_TextElement = m_textInfo.characterInfo[m_characterCount].textElement;
                    if (m_cached_TextElement == null)
                    {
                        k_CharacterLookupMarker.End();
                        continue;
                    }

                    float adjustedScale = m_currentFontSize / m_fontAsset.m_FaceInfo.pointSize * m_fontAsset.m_FaceInfo.scale;

                    {
                        elementAscentLine = m_fontAsset.m_FaceInfo.ascentLine;
                        elementDescentLine = m_fontAsset.m_FaceInfo.descentLine;
                    }

                    currentElementScale = adjustedScale * m_cached_TextElement.m_Glyph.scale;
                    baselineOffset = m_fontAsset.m_FaceInfo.baseline * adjustedScale * m_fontAsset.m_FaceInfo.scale;

                    m_textInfo.characterInfo[m_characterCount].scale = currentElementScale;

                    padding = m_padding;
                }
                k_CharacterLookupMarker.End();
                #endregion


                // Handle Soft Hyphen
                #region Handle Soft Hyphen
                float currentElementUnmodifiedScale = currentElementScale;
                if (charCode is 0xAD or 0x03)
                    currentElementScale = 0;
                #endregion


                // Store some of the text object's information
                m_textInfo.characterInfo[m_characterCount].character = (char)charCode;

                // Cache glyph metrics
                GlyphMetrics currentGlyphMetrics = m_cached_TextElement.m_Glyph.metrics;

                // Optimization to avoid calling this more than once per character.
                bool isWhiteSpace = charCode <= 0xFFFF && char.IsWhiteSpace((char)charCode);

                float characterSpacingAdjustment = m_characterSpacing;


                // Handle Mono Spacing
                #region Handle Mono Spacing
                float monoAdvance = 0;
                if (m_monoSpacing != 0)
                {
                    monoAdvance = (m_monoSpacing / 2 - (currentGlyphMetrics.width / 2 + currentGlyphMetrics.horizontalBearingX) * currentElementScale) * (1 - m_charWidthAdjDelta);
                    m_xAdvance += monoAdvance;
                }
                #endregion


                // Set Padding (Normal font style only; bold/italic unsupported).
                #region Handle Style Padding
                if (m_sharedMaterial != null && m_sharedMaterial.HasProperty(ShaderUtilities.ID_GradientScale) && m_sharedMaterial.HasProperty(ShaderUtilities.ID_ScaleRatio_A))
                {
                    float gradientScale = m_sharedMaterial.GetFloat(ShaderUtilities.ID_GradientScale);
                    style_padding = m_fontAsset.normalStyle / 4.0f * gradientScale * m_sharedMaterial.GetFloat(ShaderUtilities.ID_ScaleRatio_A);

                    // Clamp overall padding to Gradient Scale size.
                    if (style_padding + padding > gradientScale)
                        padding = gradientScale - style_padding;
                }
                else
                    style_padding = 0;
                #endregion Handle Style Padding


                // Determine the position of the vertices of the Character.
                #region Calculate Vertices Position
                k_CalculateVerticesPositionMarker.Begin();
                Vector2 top_left;
                top_left.x = m_xAdvance + ((currentGlyphMetrics.horizontalBearingX - padding - style_padding) * currentElementScale * (1 - m_charWidthAdjDelta));
                top_left.y = baselineOffset + (currentGlyphMetrics.horizontalBearingY + padding) * currentElementScale - m_lineOffset + m_baselineOffset;

                Vector2 bottom_left;
                bottom_left.x = top_left.x;
                bottom_left.y = top_left.y - ((currentGlyphMetrics.height + padding * 2) * currentElementScale);

                Vector2 top_right;
                top_right.x = bottom_left.x + ((currentGlyphMetrics.width + padding * 2 + style_padding * 2) * currentElementScale * (1 - m_charWidthAdjDelta));
                top_right.y = top_left.y;

                Vector2 bottom_right;
                bottom_right.x = top_right.x;
                bottom_right.y = bottom_left.y;

                k_CalculateVerticesPositionMarker.End();
                #endregion




                // Store vertex information for the character.
                m_textInfo.characterInfo[m_characterCount].bottomLeft = bottom_left;
                m_textInfo.characterInfo[m_characterCount].topLeft = top_left;
                m_textInfo.characterInfo[m_characterCount].topRight = top_right;
                m_textInfo.characterInfo[m_characterCount].bottomRight = bottom_right;


                // Compute text metrics
                #region Compute Ascender & Descender values
                k_ComputeTextMetricsMarker.Begin();
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
                    m_textInfo.characterInfo[m_characterCount].adjustedAscender = adjustedAscender;

                    m_ElementDescender = elementDescender - m_lineOffset;
                }
                else
                {
                    m_textInfo.characterInfo[m_characterCount].adjustedAscender = m_maxLineAscender;

                    m_ElementDescender = m_maxLineDescender - m_lineOffset;
                }

                // Max text object ascender and cap height
                if (m_lineNumber == 0)
                {
                    if (isFirstCharacterOfLine || isWhiteSpace == false)
                    {
                        m_maxTextAscender = m_maxLineAscender;
                        m_maxCapHeight = Mathf.Max(m_maxCapHeight, m_fontAsset.m_FaceInfo.capLine * currentElementScale);
                    }
                }

                // Page ascender
                if (m_lineOffset == 0)
                {
                    if (isFirstCharacterOfLine || isWhiteSpace == false)
                        m_PageAscender = m_PageAscender > elementAscender ? m_PageAscender : elementAscender;
                }
                k_ComputeTextMetricsMarker.End();
                #endregion


                // Set Characters to not visible by default.
                m_textInfo.characterInfo[m_characterCount].isVisible = false;

                // Setup Mesh for visible text elements. ie. not a SPACE / LINEFEED / CARRIAGE RETURN.
                #region Handle Visible Characters
                if (charCode == 9 || (isWhiteSpace == false && charCode != 0x200B && charCode != 0xAD && charCode != 0x03) || (charCode == 0xAD && isSoftHyphenIgnored == false))
                {
                    k_HandleVisibleCharacterMarker.Begin();

                    m_textInfo.characterInfo[m_characterCount].isVisible = true;

                    widthOfTextArea = m_width != -1 ? Mathf.Min(marginWidth + 0.0001f, m_width) : marginWidth + 0.0001f;

                    // Calculate the line breaking width of the text.
                    float textWidth = Mathf.Abs(m_xAdvance) + currentGlyphMetrics.horizontalAdvance * (1 - m_charWidthAdjDelta) * (charCode == 0xAD ? currentElementUnmodifiedScale : currentElementScale);
                    float textHeight = m_maxTextAscender - (m_maxLineDescender - m_lineOffset) + (m_lineOffset > 0 && m_IsDrivenLineSpacing == false ? m_maxLineAscender - m_startOfLineAscender : 0);

                    // Overflow mode ignores vertical/horizontal bounds (Truncate unsupported); just track the overflow index.
                    if (textHeight > marginHeight + 0.0001f && m_firstOverflowCharacterIndex == -1)
                        m_firstOverflowCharacterIndex = m_characterCount;


                    // Special handling of characters that are not ignored at the end of a line.
                    if (charCode == 9)
                    {
                        m_textInfo.characterInfo[m_characterCount].isVisible = false;
                        m_lastVisibleCharacterOfLine = m_characterCount;
                    }
                    else if (charCode == 0xAD)
                    {
                        m_textInfo.characterInfo[m_characterCount].isVisible = false;
                    }
                    else
                    {
                        k_SaveGlyphVertexDataMarker.Begin();
                        // Save Character Vertex Data
                        SaveGlyphVertexInfo(padding, style_padding, m_htmlColor);
                        k_SaveGlyphVertexDataMarker.End();

                        if (isStartOfNewLine)
                        {
                            isStartOfNewLine = false;
                            m_firstVisibleCharacterOfLine = m_characterCount;
                        }

                        m_lastVisibleCharacterOfLine = m_characterCount;
                    }

                    k_HandleVisibleCharacterMarker.End();
                }
                #endregion Handle Visible Characters


                // Store Rectangle positions for each Character.
                #region Store Character Data
                m_textInfo.characterInfo[m_characterCount].lineNumber = m_lineNumber;

                if (charCode != 10 && charCode != 11 && charCode != 13 || m_textInfo.lineInfo[m_lineNumber].characterCount == 1)
                    m_textInfo.lineInfo[m_lineNumber].alignment = m_lineJustification;
                #endregion Store Character Data


                // Handle xAdvance & Tabulation Stops. Tab stops at every 25% of Font Size.
                #region XAdvance, Tabulation & Stops
                k_ComputeCharacterAdvanceMarker.Begin();
                if (charCode == 9)
                {
                    float tabSize = m_fontAsset.m_FaceInfo.tabWidth * m_fontAsset.tabSize * currentElementScale;
                    float tabs = Mathf.Ceil(m_xAdvance / tabSize) * tabSize;
                    m_xAdvance = tabs > m_xAdvance ? tabs : m_xAdvance + tabSize;
                }
                else if (m_monoSpacing != 0)
                {
                    m_xAdvance += (m_monoSpacing - monoAdvance + ((m_fontAsset.normalSpacingOffset + characterSpacingAdjustment) * currentEmScale) + m_cSpacing) * (1 - m_charWidthAdjDelta);
                }
                else
                {
                    m_xAdvance += ((currentGlyphMetrics.horizontalAdvance) * currentElementScale + (m_fontAsset.normalSpacingOffset + characterSpacingAdjustment) * currentEmScale + m_cSpacing) * (1 - m_charWidthAdjDelta);
                }

                // Store xAdvance information
                m_textInfo.characterInfo[m_characterCount].xAdvance = m_xAdvance;
                k_ComputeCharacterAdvanceMarker.End();
                #endregion Tabulation & Stops


                // Handle Carriage Return
                #region Carriage Return
                if (charCode == 13)
                {
                    k_HandleCarriageReturnMarker.Begin();
                    m_xAdvance = 0;
                    k_HandleCarriageReturnMarker.End();
                }
                #endregion Carriage Return


                // Handle Line Spacing Adjustments + Word Wrapping & special case for last line.
                #region Check for Line Feed and Last Character
                if (charCode == 10 || charCode == 11 || charCode == 0x03 || charCode == 0x2028 || charCode == 0x2029 || m_characterCount == totalCharacterCount - 1)
                {
                    k_HandleLineTerminationMarker.Begin();

                    // Adjust current line spacing (if necessary) before inserting new line
                    float baselineAdjustmentDelta = m_maxLineAscender - m_startOfLineAscender;
                    if (m_lineOffset > 0 && Math.Abs(baselineAdjustmentDelta) > 0.01f && m_IsDrivenLineSpacing == false)
                    {
                        //Debug.Log("Line Feed - Adjusting Line Spacing on line #" + m_lineNumber);
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

                    // Save Line Information
                    m_textInfo.lineInfo[m_lineNumber].firstCharacterIndex = m_firstCharacterOfLine;
                    m_textInfo.lineInfo[m_lineNumber].lastCharacterIndex = m_lastCharacterOfLine = m_characterCount;
                    m_lastVisibleCharacterOfLine = m_lastVisibleCharacterOfLine < m_firstVisibleCharacterOfLine ? m_firstVisibleCharacterOfLine : m_lastVisibleCharacterOfLine;

                    m_textInfo.lineInfo[m_lineNumber].characterCount = m_textInfo.lineInfo[m_lineNumber].lastCharacterIndex - m_textInfo.lineInfo[m_lineNumber].firstCharacterIndex + 1;
                    m_textInfo.lineInfo[m_lineNumber].width = widthOfTextArea;

                    if (m_textInfo.lineInfo[m_lineNumber].characterCount == 1)
                        m_textInfo.lineInfo[m_lineNumber].alignment = m_lineJustification;

                    float maxAdvanceOffset = ((m_fontAsset.normalSpacingOffset + characterSpacingAdjustment) * currentEmScale - m_cSpacing) * (1 - m_charWidthAdjDelta);
                    if (m_textInfo.characterInfo[m_lastVisibleCharacterOfLine].isVisible)
                        m_textInfo.lineInfo[m_lineNumber].maxAdvance = m_textInfo.characterInfo[m_lastVisibleCharacterOfLine].xAdvance - maxAdvanceOffset;
                    else
                        m_textInfo.lineInfo[m_lineNumber].maxAdvance = m_textInfo.characterInfo[m_lastCharacterOfLine].xAdvance - maxAdvanceOffset;

                    // Add new line if not last line or character.
                    if (charCode is 10 or 11 or 0x2D or 0x2028 or 0x2029)
                    {
                        m_lineNumber += 1;
                        isStartOfNewLine = true;

                        m_firstCharacterOfLine = m_characterCount + 1;

                        // Check to make sure Array is large enough to hold a new line.
                        if (m_lineNumber >= m_textInfo.lineInfo.Length)
                            ResizeLineExtents(m_lineNumber);

                        float lastVisibleAscender = m_textInfo.characterInfo[m_characterCount].adjustedAscender;

                        // Apply Line Spacing with special handling for VT char(11)
                        if (m_lineHeight == TMP_Math.FLOAT_UNSET)
                        {
                            float lineOffsetDelta = 0 - m_maxLineDescender + lastVisibleAscender + (lineGap + m_lineSpacingDelta) * baseScale + m_lineSpacing * currentEmScale;
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
                        m_startOfLineAscender = lastVisibleAscender;

                        m_xAdvance = 0;


                        m_characterCount += 1;

                        k_HandleLineTerminationMarker.End();

                        continue;
                    }

                    // If End of Text
                    if (charCode == 0x03)
                        i = m_TextProcessingArray.Length;

                    k_HandleLineTerminationMarker.End();
                }
                #endregion Check for Linefeed or Last Character


                // Store Rectangle positions for each Character.
                #region Save CharacterInfo for the current character.
                k_SavePageInfoMarker.Begin();
                // Determine the bounds of the Mesh.
                if (m_textInfo.characterInfo[m_characterCount].isVisible)
                {
                    m_meshExtents.min.x = Mathf.Min(m_meshExtents.min.x, m_textInfo.characterInfo[m_characterCount].bottomLeft.x);
                    m_meshExtents.min.y = Mathf.Min(m_meshExtents.min.y, m_textInfo.characterInfo[m_characterCount].bottomLeft.y);

                    m_meshExtents.max.x = Mathf.Max(m_meshExtents.max.x, m_textInfo.characterInfo[m_characterCount].topRight.x);
                    m_meshExtents.max.y = Mathf.Max(m_meshExtents.max.y, m_textInfo.characterInfo[m_characterCount].topRight.y);
                }


                // Save pageInfo Data
                k_SavePageInfoMarker.End();
                #endregion Saving CharacterInfo


                m_characterCount += 1;
            }

            // If there are no visible characters or only character is End of Text (0x03)... no need to continue
            if (m_characterCount == 0 || (m_characterCount == 1 && charCode == 0x03))
            {
                ClearMesh();

                // Event indicating the text has been regenerated.
                k_GenerateTextPhaseIMarker.End();
                k_GenerateTextMarker.End();
                return;
            }

            // End Sampling of Phase I
            k_GenerateTextPhaseIMarker.End();

            // *** PHASE II of Text Generation ***
            k_GenerateTextPhaseIIMarker.Begin();

            // Partial clear of the vertices array to mark unused vertices as degenerate.
            m_textInfo.meshInfo.Clear(false);

            // Handle Text Alignment
            #region Text Vertical Alignment
            Vector2 anchorOffset = default;

            // Handle Vertical Text Alignment
            anchorOffset.x = m_RectTransformRect.x;
            var yMin = m_RectTransformRect.y;
            var yMax = m_RectTransformRect.yMax;
            anchorOffset.y = m_VerticalAlignment switch
            {
                VerticalAlignmentOptions.Top => yMax - m_maxTextAscender,
                VerticalAlignmentOptions.Middle => (yMin + yMax) / 2 - (m_maxTextAscender + maxVisibleDescender) / 2,
                VerticalAlignmentOptions.Bottom => yMin - maxVisibleDescender,
                VerticalAlignmentOptions.Baseline => (yMin + yMax) / 2,
                VerticalAlignmentOptions.Geometry => (yMin + yMax) / 2 + 0 - (m_meshExtents.max.y + m_meshExtents.min.y) / 2,
                VerticalAlignmentOptions.Capline => (yMin + yMax) / 2 + 0 - m_maxCapHeight / 2,
            };
            #endregion


            // Initialization for Second Pass
            int lastLine = 0;
            bool isStartOfWord = false;

            // Second Pass : Line Justification, UV Mapping, Character & Line Visibility & more.
            // Variables used to handle Canvas Render Modes and SDF Scaling
            var canvas = this.canvas;
            float lossyScale = m_previousLossyScaleY = this.rectTransform.lossyScale.y;

            TMP_CharacterInfo[] characterInfos = m_textInfo.characterInfo;
            #region Handle Line Justification & UV Mapping & Character Visibility & More
            for (int i = 0; i < m_characterCount; i++)
            {
                char unicode = characterInfos[i].character;

                int currentLine = characterInfos[i].lineNumber;
                TMP_LineInfo lineInfo = m_textInfo.lineInfo[currentLine];

                // Process Line Justification
                var justificationOffset = TMP_TextUtilities.CalculateJustificationOffset(lineInfo, lineInfo.alignment);
                var offset = new Vector2(anchorOffset.x + justificationOffset, anchorOffset.y);

                // Handle UV2 mapping options and packing of scale information into UV2.
                #region Handling of UV2 mapping & Scale packing
                bool isCharacterVisible = characterInfos[i].isVisible;
                if (isCharacterVisible)
                {
                    // Pack UV's so that we can pass Xscale needed for Shader to maintain 1:1 ratio.
                    #region Pack Scale into UV2
                    var xScale = characterInfos[i].scale * (1 - m_charWidthAdjDelta);

                    xScale *= Mathf.Abs(lossyScale);

                    // Optimization to avoid having a vector2 returned from the Pack UV function.
                    characterInfos[i].vertex_BL.uv2 = new Vector2(PackUV(0, 0), xScale);
                    characterInfos[i].vertex_TL.uv2 = new Vector2(PackUV(0, 1), xScale);
                    characterInfos[i].vertex_TR.uv2 = new Vector2(PackUV(1, 1), xScale);
                    characterInfos[i].vertex_BR.uv2 = new Vector2(PackUV(1, 0), xScale);
                    #endregion

                    characterInfos[i].vertex_BL.position += offset;
                    characterInfos[i].vertex_TL.position += offset;
                    characterInfos[i].vertex_TR.position += offset;
                    characterInfos[i].vertex_BR.position += offset;

                    // Fill Vertex Buffers for the various types of element
                    FillCharacterVertexBuffers(i);
                }
                #endregion

                // Apply Alignment and Justification Offset
                m_textInfo.characterInfo[i].bottomLeft += offset;
                m_textInfo.characterInfo[i].topLeft += offset;
                m_textInfo.characterInfo[i].topRight += offset;
                m_textInfo.characterInfo[i].bottomRight += offset;

                m_textInfo.characterInfo[i].xAdvance += offset.x;

                // Need to recompute lineExtent to account for the offset from justification.
                #region Adjust lineExtents resulting from alignment offset
                if (currentLine != lastLine || i == m_characterCount - 1)
                {
                    // Update the previous line's extents
                    if (currentLine != lastLine)
                        m_textInfo.lineInfo[lastLine].maxAdvance += offset.x;

                    // Update the current line's extents
                    if (i == m_characterCount - 1)
                        m_textInfo.lineInfo[currentLine].maxAdvance += offset.x;
                }
                #endregion


                // Track Word Count per line and for the object
                #region Track Word Count
                if (char.IsLetterOrDigit(unicode) || unicode == 0x2D || unicode == 0xAD || unicode == 0x2010 || unicode == 0x2011)
                {
                    isStartOfWord = true;
                }
                else if (isStartOfWord || i == 0 && (!char.IsPunctuation(unicode) || char.IsWhiteSpace(unicode) || unicode == 0x200B || i == m_characterCount - 1))
                {
                    if (i > 0 && i < characterInfos.Length - 1 && i < m_characterCount && (unicode == 39 || unicode == 8217) && char.IsLetterOrDigit(characterInfos[i - 1].character) && char.IsLetterOrDigit(characterInfos[i + 1].character))
                    {
                    }
                    else
                    {
                        isStartOfWord = false;
                    }
                }
                #endregion


                lastLine = currentLine;
            }
            #endregion

            // End Sampling of Phase II
            k_GenerateTextPhaseIIMarker.End();

            // Phase III - Update Mesh Vertex Data
            k_GenerateTextPhaseIIIMarker.Begin();

            if (IsActive())
            {
                // Must ensure the Canvas support the additional vertex attributes used by TMP.
                // This could be optimized based on canvas render mode settings but gets complicated to handle with multiple text objects using different material presets.
                if ((canvas.additionalShaderChannels & AdditionalCanvasShaderChannels.TexCoord1) == 0)
                    canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;

                // Upload Mesh Data
                m_mesh.MarkDynamic();
                m_mesh.vertices = m_textInfo.meshInfo.vertices;
                m_mesh.uv = m_textInfo.meshInfo.uvs0;
                m_mesh.uv2 = m_textInfo.meshInfo.uvs2;
                m_mesh.colors32 = m_textInfo.meshInfo.colors32;

                // Compute Bounds for the mesh. Manual computation is more efficient then using Mesh.RecalcualteBounds.
                m_mesh.RecalculateBounds();

                canvasRenderer.SetMesh(m_mesh);
            }

            // Event indicating the text has been regenerated.

            //Debug.Log("***** Done rendering text object ID " + GetInstanceID() + ". *****");

            // End Sampling
            k_GenerateTextPhaseIIIMarker.End();
            k_GenerateTextMarker.End();
        }


        /// <summary>
        /// Method to update the SDF Scale in UV2.
        /// </summary>
        /// <param name="scaleDelta"></param>
        void UpdateSDFScale(float scaleDelta)
        {
            // mesh will be generated by,
            // OnPreRenderCanvas() -> GenerateTextMesh()

            if (scaleDelta == 0 || scaleDelta == float.PositiveInfinity || scaleDelta == float.NegativeInfinity)
            {
                m_havePropertiesChanged = true;
                OnPreRenderCanvas();
                return;
            }

            var meshInfo = m_textInfo.meshInfo;
            for (int i = 0; i < meshInfo.uvs2.Length; i++)
                meshInfo.uvs2[i].y *= Mathf.Abs(scaleDelta);

            // Push the updated uv2 scale information to the mesh.
            m_mesh.uv2 = meshInfo.uvs2;
            canvasRenderer.SetMesh(m_mesh);
        }
    }
}
