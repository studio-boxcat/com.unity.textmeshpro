using UnityEngine;
using UnityEditor;
using System.Collections.Generic;


namespace TMPro.EditorUtilities
{
    public abstract class TMP_BaseEditorPanel : Editor
    {
        //Labels and Tooltips
        static readonly GUIContent k_FontAssetLabel = new GUIContent("Font Asset", "The Font Asset containing the glyphs that can be rendered for this text.");
        static readonly GUIContent k_MaterialPresetLabel = new GUIContent("Material Preset", "The material used for rendering. Only materials created from the Font Asset can be used.");
        static readonly GUIContent k_AutoSizeLabel = new GUIContent("Auto Size", "Auto sizes the text to fit the available space.");
        static readonly GUIContent k_FontSizeLabel = new GUIContent("Font Size", "The size the text will be rendered at in points.");
        static readonly GUIContent k_AutoSizeOptionsLabel = new GUIContent("Auto Size Options");
        static readonly GUIContent k_MinLabel = new GUIContent("Min", "The minimum font size.");
        static readonly GUIContent k_MaxLabel = new GUIContent("Max", "The maximum font size.");
        static readonly GUIContent k_WdLabel = new GUIContent("WD%", "Compresses character width up to this value before reducing font size.");
        static readonly GUIContent k_LineLabel = new GUIContent("Line", "Negative value only. Compresses line height down to this value before reducing font size.");
        static readonly GUIContent k_FontStyleLabel = new GUIContent("Font Style", "Styles to apply to the text such as Bold or Italic.");

        static readonly GUIContent k_BoldLabel = new GUIContent("B", "Bold");
        static readonly GUIContent k_ItalicLabel = new GUIContent("I", "Italic");

        static readonly GUIContent k_BaseColorLabel = new GUIContent("Vertex Color", "The base color of the text vertices.");

        static readonly GUIContent k_SpacingOptionsLabel = new GUIContent("Spacing Options (em)", "Spacing adjustments between different elements of the text. Values are in font units where a value of 1 equals 1/100em.");
        static readonly GUIContent k_CharacterSpacingLabel = new GUIContent("Character");
        static readonly GUIContent k_WordSpacingLabel = new GUIContent("Word");
        static readonly GUIContent k_LineSpacingLabel = new GUIContent("Line");
        static readonly GUIContent k_ParagraphSpacingLabel = new GUIContent("Paragraph");

        static readonly GUIContent k_AlignmentLabel = new GUIContent("Alignment", "Horizontal and vertical aligment of the text within its container.");

        static readonly GUIContent k_WrappingLabel = new GUIContent("Wrapping", "Wraps text to the next line when reaching the edge of the container.");
        static readonly GUIContent[] k_WrappingOptions = { new GUIContent("Disabled"), new GUIContent("Enabled") };
        static readonly GUIContent k_OverflowLabel = new GUIContent("Overflow", "How to display text which goes past the edge of the container.");

        static readonly GUIContent k_MarginsLabel = new GUIContent("Margins", "The space between the text and the edge of its container.");
        static readonly GUIContent k_IsTextObjectScaleStatic = new GUIContent("Is Scale Static", "Controls whether a text object will be excluded from the InteralUpdate callback to handle scale changes of the text object or its parent(s).");
        static readonly GUIContent k_RichTextLabel = new GUIContent("Rich Text", "Enables the use of rich text tags such as <color> and <font>.");
        static readonly GUIContent k_VisibleDescenderLabel = new GUIContent("Visible Descender", "Compute descender values from visible characters only. Used to adjust layout behavior when hiding and revealing characters dynamically.");

        static readonly GUIContent k_KerningLabel = new GUIContent("Kerning", "Enables character specific spacing between pairs of characters.");
        static readonly GUIContent k_PaddingLabel = new GUIContent("Extra Padding", "Adds some padding between the characters and the edge of the text mesh. Can reduce graphical errors when displaying small text.");

        protected static string[] k_UiStateLabel = new string[] { "<i>(Click to collapse)</i> ", "<i>(Click to expand)</i> " };

        protected struct Foldout
        {
            // Track Inspector foldout panel states, globally.
            public static bool extraSettings = false;
            public static bool materialInspector = true;
        }

        protected static int s_EventId;

        protected SerializedProperty m_TextProp;

        protected SerializedProperty m_FontAssetProp;

        protected SerializedProperty m_FontSharedMaterialProp;
        protected Material[] m_MaterialPresets;
        protected GUIContent[] m_MaterialPresetNames;
        protected Dictionary<int, int> m_MaterialPresetIndexLookup = new Dictionary<int, int>();
        protected int m_MaterialPresetSelectionIndex;

        protected SerializedProperty m_FontStyleProp;

        protected SerializedProperty m_FontColorProp;

        protected SerializedProperty m_FontSizeProp;
        protected SerializedProperty m_FontSizeBaseProp;

        protected SerializedProperty m_AutoSizingProp;
        protected SerializedProperty m_FontSizeMinProp;
        protected SerializedProperty m_FontSizeMaxProp;

        protected SerializedProperty m_LineSpacingMaxProp;
        protected SerializedProperty m_CharWidthMaxAdjProp;

        protected SerializedProperty m_CharacterSpacingProp;
        protected SerializedProperty m_WordSpacingProp;
        protected SerializedProperty m_LineSpacingProp;
        protected SerializedProperty m_ParagraphSpacingProp;

        protected SerializedProperty m_HorizontalAlignmentProp;
        protected SerializedProperty m_VerticalAlignmentProp;

        protected SerializedProperty m_EnableWordWrappingProp;
        protected SerializedProperty m_TextOverflowModeProp;

        protected SerializedProperty m_EnableKerningProp;

        protected SerializedProperty m_IsRichTextProp;

        protected SerializedProperty m_HasFontAssetChangedProp;

        protected SerializedProperty m_EnableExtraPaddingProp;
        protected SerializedProperty m_CheckPaddingRequiredProp;
        protected SerializedProperty m_UseMaxVisibleDescenderProp;
        protected SerializedProperty m_IsTextObjectScaleStaticProp;

        protected SerializedProperty m_MarginProp;

        protected bool m_HavePropertiesChanged;

        protected TMP_Text m_TextComponent;
        protected RectTransform m_RectTransform;

        protected Material m_TargetMaterial;

        protected Vector3[] m_RectCorners = new Vector3[4];
        protected Vector3[] m_HandlePoints = new Vector3[4];

        protected virtual void OnEnable()
        {
            m_TextProp = serializedObject.FindProperty("m_text");
            m_FontAssetProp = serializedObject.FindProperty("m_fontAsset");
            m_FontSharedMaterialProp = serializedObject.FindProperty("m_sharedMaterial");

            m_FontStyleProp = serializedObject.FindProperty("m_fontStyle");

            m_FontSizeProp = serializedObject.FindProperty("m_fontSize");
            m_FontSizeBaseProp = serializedObject.FindProperty("m_fontSizeBase");

            m_AutoSizingProp = serializedObject.FindProperty("m_enableAutoSizing");
            m_FontSizeMinProp = serializedObject.FindProperty("m_fontSizeMin");
            m_FontSizeMaxProp = serializedObject.FindProperty("m_fontSizeMax");

            m_LineSpacingMaxProp = serializedObject.FindProperty("m_lineSpacingMax");
            m_CharWidthMaxAdjProp = serializedObject.FindProperty("m_charWidthMaxAdj");

            // Colors & Gradient
            m_FontColorProp = serializedObject.FindProperty("m_fontColor");

            m_CharacterSpacingProp = serializedObject.FindProperty("m_characterSpacing");
            m_WordSpacingProp = serializedObject.FindProperty("m_wordSpacing");
            m_LineSpacingProp = serializedObject.FindProperty("m_lineSpacing");
            m_ParagraphSpacingProp = serializedObject.FindProperty("m_paragraphSpacing");

            m_HorizontalAlignmentProp = serializedObject.FindProperty("m_HorizontalAlignment");
            m_VerticalAlignmentProp = serializedObject.FindProperty("m_VerticalAlignment");

            m_EnableWordWrappingProp = serializedObject.FindProperty("m_enableWordWrapping");
            m_TextOverflowModeProp = serializedObject.FindProperty("m_overflowMode");

            m_EnableKerningProp = serializedObject.FindProperty("m_enableKerning");

            m_EnableExtraPaddingProp = serializedObject.FindProperty("m_enableExtraPadding");
            m_IsRichTextProp = serializedObject.FindProperty("m_isRichText");
            m_CheckPaddingRequiredProp = serializedObject.FindProperty("checkPaddingRequired");
            m_UseMaxVisibleDescenderProp = serializedObject.FindProperty("m_useMaxVisibleDescender");

            m_IsTextObjectScaleStaticProp = serializedObject.FindProperty("m_IsTextObjectScaleStatic");

            m_MarginProp = serializedObject.FindProperty("m_margin");

            m_HasFontAssetChangedProp = serializedObject.FindProperty("m_hasFontAssetChanged");

            m_TextComponent = (TMP_Text)target;
            m_RectTransform = m_TextComponent.transform;

            // Create new Material Editor if one does not exists
            m_TargetMaterial = m_TextComponent.fontSharedMaterial;

            // Set material inspector visibility
            if (m_TargetMaterial != null)
                UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(m_TargetMaterial, Foldout.materialInspector);

            // Find all Material Presets matching the current Font Asset Material
            m_MaterialPresetNames = GetMaterialPresets();

            // Initialize the Event Listener for Undo Events.
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        protected virtual void OnDisable()
        {
            // Set material inspector visibility
            if (m_TargetMaterial != null)
                Foldout.materialInspector = UnityEditorInternal.InternalEditorUtility.GetIsInspectorExpanded(m_TargetMaterial);

            if (Undo.undoRedoPerformed != null)
                Undo.undoRedoPerformed -= OnUndoRedo;
        }

        public override void OnInspectorGUI()
        {
            // Make sure Multi selection only includes TMP Text objects.
            if (IsMixSelectionTypes()) return;

            serializedObject.Update();

            DrawTextInput();

            DrawMainSettings();

            DrawExtraSettings();

            EditorGUILayout.Space();

            if (serializedObject.ApplyModifiedProperties() || m_HavePropertiesChanged)
            {
                m_TextComponent.havePropertiesChanged = true;
                m_HavePropertiesChanged = false;
                EditorUtility.SetDirty(target);
            }
        }

        public void OnSceneGUI()
        {
            if (IsMixSelectionTypes()) return;

            // Margin Frame & Handles
            m_RectTransform.GetWorldCorners(m_RectCorners);
            Vector4 marginOffset = m_TextComponent.margin;
            Vector3 lossyScale = m_RectTransform.lossyScale;

            m_HandlePoints[0] = m_RectCorners[0] + m_RectTransform.TransformDirection(new Vector3(marginOffset.x * lossyScale.x, marginOffset.w * lossyScale.y, 0));
            m_HandlePoints[1] = m_RectCorners[1] + m_RectTransform.TransformDirection(new Vector3(marginOffset.x * lossyScale.x, -marginOffset.y * lossyScale.y, 0));
            m_HandlePoints[2] = m_RectCorners[2] + m_RectTransform.TransformDirection(new Vector3(-marginOffset.z * lossyScale.x, -marginOffset.y * lossyScale.y, 0));
            m_HandlePoints[3] = m_RectCorners[3] + m_RectTransform.TransformDirection(new Vector3(-marginOffset.z * lossyScale.x, marginOffset.w * lossyScale.y, 0));

            Handles.DrawSolidRectangleWithOutline(m_HandlePoints, new Color32(255, 255, 255, 0), new Color32(255, 255, 0, 255));

            Matrix4x4 matrix = m_RectTransform.worldToLocalMatrix;

            // Draw & process FreeMoveHandles

            // LEFT HANDLE
            Vector3 oldLeft = (m_HandlePoints[0] + m_HandlePoints[1]) * 0.5f;
            var fmh_311_63_638425576009114480 = Quaternion.identity; Vector3 newLeft = Handles.FreeMoveHandle(oldLeft, HandleUtility.GetHandleSize(m_RectTransform.position) * 0.05f, Vector3.zero, Handles.DotHandleCap);
            bool hasChanged = false;
            if (oldLeft != newLeft)
            {
                oldLeft = matrix.MultiplyPoint(oldLeft);
                newLeft = matrix.MultiplyPoint(newLeft);

                float delta = (oldLeft.x - newLeft.x) * lossyScale.x;
                marginOffset.x += -delta / lossyScale.x;
                //Debug.Log("Left Margin H0:" + handlePoints[0] + "  H1:" + handlePoints[1]);
                hasChanged = true;
            }

            // TOP HANDLE
            Vector3 oldTop = (m_HandlePoints[1] + m_HandlePoints[2]) * 0.5f;
            var fmh_326_61_638425576009134060 = Quaternion.identity; Vector3 newTop = Handles.FreeMoveHandle(oldTop, HandleUtility.GetHandleSize(m_RectTransform.position) * 0.05f, Vector3.zero, Handles.DotHandleCap);
            if (oldTop != newTop)
            {
                oldTop = matrix.MultiplyPoint(oldTop);
                newTop = matrix.MultiplyPoint(newTop);

                float delta = (oldTop.y - newTop.y) * lossyScale.y;
                marginOffset.y += delta / lossyScale.y;
                //Debug.Log("Top Margin H1:" + handlePoints[1] + "  H2:" + handlePoints[2]);
                hasChanged = true;
            }

            // RIGHT HANDLE
            Vector3 oldRight = (m_HandlePoints[2] + m_HandlePoints[3]) * 0.5f;
            Vector3 newRight = Handles.FreeMoveHandle(oldRight, HandleUtility.GetHandleSize(m_RectTransform.position) * 0.05f, Vector3.zero, Handles.DotHandleCap);
            if (oldRight != newRight)
            {
                oldRight = matrix.MultiplyPoint(oldRight);
                newRight = matrix.MultiplyPoint(newRight);

                float delta = (oldRight.x - newRight.x) * lossyScale.x;
                marginOffset.z += delta / lossyScale.x;
                hasChanged = true;
                //Debug.Log("Right Margin H2:" + handlePoints[2] + "  H3:" + handlePoints[3]);
            }

            // BOTTOM HANDLE
            Vector3 oldBottom = (m_HandlePoints[3] + m_HandlePoints[0]) * 0.5f;
            Vector3 newBottom = Handles.FreeMoveHandle(oldBottom, HandleUtility.GetHandleSize(m_RectTransform.position) * 0.05f, Vector3.zero, Handles.DotHandleCap);
            if (oldBottom != newBottom)
            {
                oldBottom = matrix.MultiplyPoint(oldBottom);
                newBottom = matrix.MultiplyPoint(newBottom);

                float delta = (oldBottom.y - newBottom.y) * lossyScale.y;
                marginOffset.w += -delta / lossyScale.y;
                hasChanged = true;
                //Debug.Log("Bottom Margin H0:" + handlePoints[0] + "  H3:" + handlePoints[3]);
            }

            if (hasChanged)
            {
                Undo.RecordObjects(new Object[] {m_RectTransform, m_TextComponent }, "Margin Changes");
                m_TextComponent.margin = marginOffset;
                EditorUtility.SetDirty(target);
            }
        }

        protected void DrawTextInput()
        {
            EditorGUILayout.Space();

            Rect rect = EditorGUILayout.GetControlRect(false, 22);
            GUI.Label(rect, new GUIContent("<b>Text Input</b>"), TMP_UIStyleManager.sectionHeader);

            EditorGUI.indentLevel = 0;

            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_TextProp, GUIContent.none);

                // Need to also compare string content due to issue related to scroll bar drag handle
                if (EditorGUI.EndChangeCheck() && m_TextProp.stringValue != m_TextComponent.text)
                {
                    m_TextComponent.m_inputSource = TMP_Text.TextInputSources.TextInputBox;
                    m_HavePropertiesChanged = true;
                }
            }
        }

        protected void DrawMainSettings()
        {
            // MAIN SETTINGS SECTION
            GUILayout.Label(new GUIContent("<b>Main Settings</b>"), TMP_UIStyleManager.sectionHeader);

            DrawFont();

            DrawColor();

            DrawSpacing();

            DrawAlignment();

            DrawWrappingOverflow();
        }

        void DrawFont()
        {
            bool isFontAssetDirty = false;

            // FONT ASSET
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_FontAssetProp, k_FontAssetLabel);
            if (EditorGUI.EndChangeCheck())
            {
                m_HavePropertiesChanged = true;
                m_HasFontAssetChangedProp.boolValue = true;

                // Get new Material Presets for the new font asset
                m_MaterialPresetNames = GetMaterialPresets();
                m_MaterialPresetSelectionIndex = 0;

                isFontAssetDirty = true;
            }

            Rect rect;

            // MATERIAL PRESET
            if (m_MaterialPresetNames != null && !isFontAssetDirty )
            {
                EditorGUI.BeginChangeCheck();
                rect = EditorGUILayout.GetControlRect(false, 17);

                EditorGUI.BeginProperty(rect, k_MaterialPresetLabel, m_FontSharedMaterialProp);

                float oldHeight = EditorStyles.popup.fixedHeight;
                EditorStyles.popup.fixedHeight = rect.height;

                int oldSize = EditorStyles.popup.fontSize;
                EditorStyles.popup.fontSize = 11;

                if (m_FontSharedMaterialProp.objectReferenceValue != null)
                    m_MaterialPresetIndexLookup.TryGetValue(m_FontSharedMaterialProp.objectReferenceValue.GetInstanceID(), out m_MaterialPresetSelectionIndex);

                m_MaterialPresetSelectionIndex = EditorGUI.Popup(rect, k_MaterialPresetLabel, m_MaterialPresetSelectionIndex, m_MaterialPresetNames);

                EditorGUI.EndProperty();

                if (EditorGUI.EndChangeCheck())
                {
                    m_FontSharedMaterialProp.objectReferenceValue = m_MaterialPresets[m_MaterialPresetSelectionIndex];
                    m_HavePropertiesChanged = true;
                }

                EditorStyles.popup.fixedHeight = oldHeight;
                EditorStyles.popup.fontSize = oldSize;
            }

            // FONT STYLE
            EditorGUI.BeginChangeCheck();

            int v1, v2;

            if (EditorGUIUtility.wideMode)
            {
                rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight + 2f);

                EditorGUI.BeginProperty(rect, k_FontStyleLabel, m_FontStyleProp);

                EditorGUI.PrefixLabel(rect, k_FontStyleLabel);

                int styleValue = m_FontStyleProp.intValue;

                rect.x += EditorGUIUtility.labelWidth;
                rect.width -= EditorGUIUtility.labelWidth;

                rect.width = Mathf.Max(25f, rect.width / 7f);

                v1 = TMP_EditorUtility.EditorToggle(rect, (styleValue & 1) == 1, k_BoldLabel, TMP_UIStyleManager.alignmentButtonLeft) ? 1 : 0; // Bold
                rect.x += rect.width;
                v2 = TMP_EditorUtility.EditorToggle(rect, (styleValue & 2) == 2, k_ItalicLabel, TMP_UIStyleManager.alignmentButtonMid) ? 2 : 0; // Italics
                rect.x += rect.width;

                EditorGUI.EndProperty();
            }
            else
            {
                rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight + 2f);

                EditorGUI.BeginProperty(rect, k_FontStyleLabel, m_FontStyleProp);

                EditorGUI.PrefixLabel(rect, k_FontStyleLabel);

                int styleValue = m_FontStyleProp.intValue;

                rect.x += EditorGUIUtility.labelWidth;
                rect.width -= EditorGUIUtility.labelWidth;
                rect.width = Mathf.Max(25f, rect.width / 4f);

                v1 = TMP_EditorUtility.EditorToggle(rect, (styleValue & 1) == 1, k_BoldLabel, TMP_UIStyleManager.alignmentButtonLeft) ? 1 : 0; // Bold
                rect.x += rect.width;
                v2 = TMP_EditorUtility.EditorToggle(rect, (styleValue & 2) == 2, k_ItalicLabel, TMP_UIStyleManager.alignmentButtonMid) ? 2 : 0; // Italics
                rect.x += rect.width;

                rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight + 2f);

                rect.x += EditorGUIUtility.labelWidth;
                rect.width -= EditorGUIUtility.labelWidth;

                rect.width = Mathf.Max(25f, rect.width / 4f);

                EditorGUI.EndProperty();
            }

            if (EditorGUI.EndChangeCheck())
            {
                m_FontStyleProp.intValue = v1 + v2;
                m_HavePropertiesChanged = true;
            }

            // FONT SIZE
            EditorGUI.BeginChangeCheck();

            EditorGUI.BeginDisabledGroup(m_AutoSizingProp.boolValue);
            EditorGUILayout.PropertyField(m_FontSizeProp, k_FontSizeLabel, GUILayout.MaxWidth(EditorGUIUtility.labelWidth + 50f));
            EditorGUI.EndDisabledGroup();

            if (EditorGUI.EndChangeCheck())
            {
                float fontSize = Mathf.Clamp(m_FontSizeProp.floatValue, 0, 32767);

                m_FontSizeProp.floatValue = fontSize;
                m_FontSizeBaseProp.floatValue = fontSize;
                m_HavePropertiesChanged = true;
            }

            EditorGUI.indentLevel += 1;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_AutoSizingProp, k_AutoSizeLabel);
            if (EditorGUI.EndChangeCheck())
            {
                if (m_AutoSizingProp.boolValue == false)
                    m_FontSizeProp.floatValue = m_FontSizeBaseProp.floatValue;

                m_HavePropertiesChanged = true;
            }

            // Show auto sizing options
            if (m_AutoSizingProp.boolValue)
            {
                rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);

                EditorGUI.PrefixLabel(rect, k_AutoSizeOptionsLabel);

                int previousIndent = EditorGUI.indentLevel;

                EditorGUI.indentLevel = 0;

                rect.width = (rect.width - EditorGUIUtility.labelWidth) / 4f;
                rect.x += EditorGUIUtility.labelWidth;

                EditorGUIUtility.labelWidth = 24;
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(rect, m_FontSizeMinProp, k_MinLabel);
                if (EditorGUI.EndChangeCheck())
                {
                    float minSize = m_FontSizeMinProp.floatValue;

                    minSize = Mathf.Max(0, minSize);

                    m_FontSizeMinProp.floatValue = Mathf.Min(minSize, m_FontSizeMaxProp.floatValue);
                    m_HavePropertiesChanged = true;
                }
                rect.x += rect.width;

                EditorGUIUtility.labelWidth = 27;
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(rect, m_FontSizeMaxProp, k_MaxLabel);
                if (EditorGUI.EndChangeCheck())
                {
                    float maxSize = Mathf.Clamp(m_FontSizeMaxProp.floatValue, 0, 32767);

                    m_FontSizeMaxProp.floatValue = Mathf.Max(m_FontSizeMinProp.floatValue, maxSize);
                    m_HavePropertiesChanged = true;
                }
                rect.x += rect.width;

                EditorGUI.BeginChangeCheck();
                EditorGUIUtility.labelWidth = 36;
                EditorGUI.PropertyField(rect, m_CharWidthMaxAdjProp, k_WdLabel);
                rect.x += rect.width;
                EditorGUIUtility.labelWidth = 28;
                EditorGUI.PropertyField(rect, m_LineSpacingMaxProp, k_LineLabel);

                EditorGUIUtility.labelWidth = 0;

                if (EditorGUI.EndChangeCheck())
                {
                    m_CharWidthMaxAdjProp.floatValue = Mathf.Clamp(m_CharWidthMaxAdjProp.floatValue, 0, 50);
                    m_LineSpacingMaxProp.floatValue = Mathf.Min(0, m_LineSpacingMaxProp.floatValue);
                    m_HavePropertiesChanged = true;
                }

                EditorGUI.indentLevel = previousIndent;
            }

            EditorGUI.indentLevel -= 1;



            EditorGUILayout.Space();
        }

        void DrawColor()
        {
            // FACE VERTEX COLOR
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_FontColorProp, k_BaseColorLabel);
            if (EditorGUI.EndChangeCheck())
            {
                m_HavePropertiesChanged = true;
            }

            EditorGUIUtility.fieldWidth = 0;

            EditorGUILayout.Space();
        }

        void DrawSpacing()
        {
            // CHARACTER, LINE & PARAGRAPH SPACING
            EditorGUI.BeginChangeCheck();

            Rect rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);

            EditorGUI.PrefixLabel(rect, k_SpacingOptionsLabel);

            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            float currentLabelWidth = EditorGUIUtility.labelWidth;
            rect.x += currentLabelWidth;
            rect.width = (rect.width - currentLabelWidth - 3f) / 2f;

            EditorGUIUtility.labelWidth = Mathf.Min(rect.width * 0.55f, 80f);

            EditorGUI.PropertyField(rect, m_CharacterSpacingProp, k_CharacterSpacingLabel);
            rect.x += rect.width + 3f;
            EditorGUI.PropertyField(rect, m_WordSpacingProp, k_WordSpacingLabel);

            rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

            rect.x += currentLabelWidth;
            rect.width = (rect.width - currentLabelWidth -3f) / 2f;
            EditorGUIUtility.labelWidth = Mathf.Min(rect.width * 0.55f, 80f);

            EditorGUI.PropertyField(rect, m_LineSpacingProp, k_LineSpacingLabel);
            rect.x += rect.width + 3f;
            EditorGUI.PropertyField(rect, m_ParagraphSpacingProp, k_ParagraphSpacingLabel);

            EditorGUIUtility.labelWidth = currentLabelWidth;
            EditorGUI.indentLevel = oldIndent;

            if (EditorGUI.EndChangeCheck())
            {
                m_HavePropertiesChanged = true;
            }

            EditorGUILayout.Space();
        }

        void DrawAlignment()
        {
            // TEXT ALIGNMENT
            EditorGUI.BeginChangeCheck();

            Rect rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.currentViewWidth > 504 ? 20 : 40 + 3);
            EditorGUI.BeginProperty(rect, k_AlignmentLabel, m_HorizontalAlignmentProp);
            EditorGUI.BeginProperty(rect, k_AlignmentLabel, m_VerticalAlignmentProp);

            EditorGUI.PrefixLabel(rect, k_AlignmentLabel);
            rect.x += EditorGUIUtility.labelWidth;

            EditorGUI.PropertyField(rect, m_HorizontalAlignmentProp, GUIContent.none);
            EditorGUI.PropertyField(rect, m_VerticalAlignmentProp, GUIContent.none);

            if (EditorGUI.EndChangeCheck())
                m_HavePropertiesChanged = true;

            EditorGUI.EndProperty();
            EditorGUI.EndProperty();

            EditorGUILayout.Space();
        }

        void DrawWrappingOverflow()
        {
            // TEXT WRAPPING
            Rect rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginProperty(rect, k_WrappingLabel, m_EnableWordWrappingProp);

            EditorGUI.BeginChangeCheck();
            int wrapSelection = EditorGUI.Popup(rect, k_WrappingLabel, m_EnableWordWrappingProp.boolValue ? 1 : 0, k_WrappingOptions);
            if (EditorGUI.EndChangeCheck())
            {
                m_EnableWordWrappingProp.boolValue = wrapSelection == 1;
                m_HavePropertiesChanged = true;
            }

            EditorGUI.EndProperty();

            // TEXT OVERFLOW
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(m_TextOverflowModeProp, k_OverflowLabel);

            if (EditorGUI.EndChangeCheck())
            {
                m_HavePropertiesChanged = true;
            }

            EditorGUILayout.Space();
        }

        protected abstract void DrawExtraSettings();

        protected void DrawMargins()
        {
            EditorGUI.BeginChangeCheck();
            DrawMarginProperty(m_MarginProp, k_MarginsLabel);
            if (EditorGUI.EndChangeCheck())
            {
                m_HavePropertiesChanged = true;
            }

            EditorGUILayout.Space();
        }

        protected void DrawIsTextObjectScaleStatic()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(m_IsTextObjectScaleStaticProp, k_IsTextObjectScaleStatic);

            if (EditorGUI.EndChangeCheck())
            {
                m_TextComponent.isTextObjectScaleStatic = m_IsTextObjectScaleStaticProp.boolValue;
                m_HavePropertiesChanged = true;
            }

            EditorGUILayout.Space();
        }


        protected void DrawRichText()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(m_IsRichTextProp, k_RichTextLabel);
            if (EditorGUI.EndChangeCheck())
                m_HavePropertiesChanged = true;
        }

        protected void DrawParsing()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_UseMaxVisibleDescenderProp, k_VisibleDescenderLabel);

            if (EditorGUI.EndChangeCheck())
                m_HavePropertiesChanged = true;

            EditorGUILayout.Space();
        }

        protected void DrawKerning()
        {
            // KERNING
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_EnableKerningProp, k_KerningLabel);
            if (EditorGUI.EndChangeCheck())
            {
                m_HavePropertiesChanged = true;
            }
        }

        protected void DrawPadding()
        {
            // EXTRA PADDING
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_EnableExtraPaddingProp, k_PaddingLabel);
            if (EditorGUI.EndChangeCheck())
            {
                m_HavePropertiesChanged = true;
                m_CheckPaddingRequiredProp.boolValue = true;
            }
        }

        /// <summary>
        /// Method to retrieve the material presets that match the currently selected font asset.
        /// </summary>
        protected GUIContent[] GetMaterialPresets()
        {
            TMP_FontAsset fontAsset = m_FontAssetProp.objectReferenceValue as TMP_FontAsset;
            if (fontAsset == null) return null;

            m_MaterialPresets = TMP_EditorUtility.FindMaterialReferences(fontAsset);
            m_MaterialPresetNames = new GUIContent[m_MaterialPresets.Length];

            m_MaterialPresetIndexLookup.Clear();

            for (int i = 0; i < m_MaterialPresetNames.Length; i++)
            {
                m_MaterialPresetNames[i] = new GUIContent(m_MaterialPresets[i].name);
                m_MaterialPresetIndexLookup.Add(m_MaterialPresets[i].GetInstanceID(), i);
            }

            return m_MaterialPresetNames;
        }

        // DRAW MARGIN PROPERTY
        protected void DrawMarginProperty(SerializedProperty property, GUIContent label)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 2 * 18);

            EditorGUI.BeginProperty(rect, label, property);

            Rect pos0 = new Rect(rect.x, rect.y + 2, rect.width - 15, 18);

            float width = rect.width + 3;
            pos0.width = EditorGUIUtility.labelWidth;
            EditorGUI.PrefixLabel(pos0, label);

            Vector4 margins = property.vector4Value;

            float widthB = width - EditorGUIUtility.labelWidth;
            float fieldWidth = widthB / 4;
            pos0.width = Mathf.Max(fieldWidth - 5, 45f);

            // Labels
            pos0.x = EditorGUIUtility.labelWidth + 15;
            margins.x = DrawMarginField(pos0, "Left", margins.x);

            pos0.x += fieldWidth;
            margins.y = DrawMarginField(pos0, "Top", margins.y);

            pos0.x += fieldWidth;
            margins.z = DrawMarginField(pos0, "Right", margins.z);

            pos0.x += fieldWidth;
            margins.w = DrawMarginField(pos0, "Bottom", margins.w);

            property.vector4Value = margins;

            EditorGUI.EndProperty();
        }

        float DrawMarginField(Rect position, string label, float value)
        {
            int controlId = GUIUtility.GetControlID(FocusType.Keyboard, position);
            EditorGUI.PrefixLabel(position, controlId, new GUIContent(label));

            Rect dragZone = new Rect(position.x, position.y, position.width, position.height);
            position.y += EditorGUIUtility.singleLineHeight;

            return EditorGUI.DoFloatField(EditorGUI.s_RecycledEditor, position, dragZone, controlId, value, EditorGUI.kFloatFieldFormatString, EditorStyles.numberField, true);
        }

        void DrawPropertySlider(GUIContent label, SerializedProperty property)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 17);

            GUIContent content = label ?? GUIContent.none;
            EditorGUI.Slider(new Rect(rect.x, rect.y, rect.width, rect.height), property, 0.0f, 1.0f, content);
        }

        protected abstract bool IsMixSelectionTypes();

        // Special Handling of Undo / Redo Events.
        protected abstract void OnUndoRedo();

    }
}
