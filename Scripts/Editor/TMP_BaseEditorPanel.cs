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
        static readonly GUIContent k_LineSpacingLabel = new GUIContent("Line");

        static readonly GUIContent k_AlignmentLabel = new GUIContent("Alignment", "Horizontal and vertical aligment of the text within its container.");

        static readonly GUIContent k_WrappingLabel = new GUIContent("Wrapping", "Wraps text to the next line when reaching the edge of the container.");
        static readonly GUIContent[] k_WrappingOptions = { new GUIContent("Disabled"), new GUIContent("Enabled") };
        static readonly GUIContent k_OverflowLabel = new GUIContent("Overflow", "How to display text which goes past the edge of the container.");

        static readonly GUIContent k_RichTextLabel = new GUIContent("Rich Text", "Enables the use of rich text tags such as <color> and <font>.");
        static readonly GUIContent k_VisibleDescenderLabel = new GUIContent("Visible Descender", "Compute descender values from visible characters only. Used to adjust layout behavior when hiding and revealing characters dynamically.");

        static readonly GUIContent k_KerningLabel = new GUIContent("Kerning", "Enables character specific spacing between pairs of characters.");
        static readonly GUIContent k_PaddingLabel = new GUIContent("Extra Padding", "Adds some padding between the characters and the edge of the text mesh. Can reduce graphical errors when displaying small text.");

        protected static readonly string[] k_UiStateLabel = new string[] { "<i>(Click to collapse)</i> ", "<i>(Click to expand)</i> " };

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
        protected readonly Dictionary<int, int> m_MaterialPresetIndexLookup = new Dictionary<int, int>();
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
        protected SerializedProperty m_LineSpacingProp;

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

        protected bool m_HavePropertiesChanged;

        protected TMP_Text m_TextComponent;

        protected Material m_TargetMaterial;

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
            m_LineSpacingProp = serializedObject.FindProperty("m_lineSpacing");

            m_HorizontalAlignmentProp = serializedObject.FindProperty("m_HorizontalAlignment");
            m_VerticalAlignmentProp = serializedObject.FindProperty("m_VerticalAlignment");

            m_EnableWordWrappingProp = serializedObject.FindProperty("m_enableWordWrapping");
            m_TextOverflowModeProp = serializedObject.FindProperty("m_overflowMode");

            m_EnableKerningProp = serializedObject.FindProperty("m_enableKerning");

            m_EnableExtraPaddingProp = serializedObject.FindProperty("m_enableExtraPadding");
            m_IsRichTextProp = serializedObject.FindProperty("m_isRichText");
            m_CheckPaddingRequiredProp = serializedObject.FindProperty("checkPaddingRequired");
            m_UseMaxVisibleDescenderProp = serializedObject.FindProperty("m_useMaxVisibleDescender");

            m_HasFontAssetChangedProp = serializedObject.FindProperty("m_hasFontAssetChanged");

            m_TextComponent = (TMP_Text)target;

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

        void DrawTextInput()
        {
            GUILayout.Label(new GUIContent("<b>Text Input</b>"), TMP_UIStyleManager.sectionHeader);

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

        void DrawMainSettings()
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
            EditorGUI.PropertyField(rect, m_LineSpacingProp, k_LineSpacingLabel);

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
        GUIContent[] GetMaterialPresets()
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
        protected abstract bool IsMixSelectionTypes();

        // Special Handling of Undo / Redo Events.
        protected abstract void OnUndoRedo();

    }
}
