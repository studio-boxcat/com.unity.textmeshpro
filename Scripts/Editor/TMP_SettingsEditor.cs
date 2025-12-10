using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

#pragma warning disable 0414 // Disabled a few warnings for not yet implemented features.

namespace TMPro.EditorUtilities
{
    [CustomEditor(typeof(TMP_Settings))]
    public class TMP_SettingsEditor : Editor
    {
        internal class Styles
        {
            public static readonly GUIContent defaultFontAssetLabel = new GUIContent("Default Font Asset", "The Font Asset that will be assigned by default to newly created text objects when no Font Asset is specified.");
            public static readonly GUIContent defaultFontAssetPathLabel = new GUIContent("Path:        Resources/", "The relative path to a Resources folder where the Font Assets and Material Presets are located.\nExample \"Fonts & Materials/\"");

            public static readonly GUIContent fallbackFontAssetsLabel = new GUIContent("Fallback Font Assets", "The Font Assets that will be searched to locate and replace missing characters from a given Font Asset.");
            public static readonly GUIContent fallbackFontAssetsListLabel = new GUIContent("Fallback Font Assets List", "The Font Assets that will be searched to locate and replace missing characters from a given Font Asset.");

            public static readonly GUIContent fallbackMaterialSettingsLabel = new GUIContent("Fallback Material Settings");
            public static readonly GUIContent matchMaterialPresetLabel = new GUIContent("Match Material Presets");

            public static readonly GUIContent containerDefaultSettingsLabel = new GUIContent("Text Container Default Settings");

            public static readonly GUIContent textMeshProLabel = new GUIContent("TextMeshPro");
            public static readonly GUIContent textMeshProUiLabel = new GUIContent("TextMeshPro UI");
            public static readonly GUIContent enableRaycastTarget = new GUIContent("Enable Raycast Target");
            public static readonly GUIContent isTextObjectScaleStaticLabel = new GUIContent("Is Object Scale Static", "Disables calling InternalUpdate() when enabled. This can improve performance when text object scale is static.");

            public static readonly GUIContent textComponentDefaultSettingsLabel = new GUIContent("Text Component Default Settings");
            public static readonly GUIContent defaultFontSize = new GUIContent("Default Font Size");
            public static readonly GUIContent autoSizeRatioLabel = new GUIContent("Text Auto Size Ratios");
            public static readonly GUIContent minLabel = new GUIContent("Min");
            public static readonly GUIContent maxLabel = new GUIContent("Max");

            public static readonly GUIContent wordWrappingLabel = new GUIContent("Word Wrapping");
            public static readonly GUIContent kerningLabel = new GUIContent("Kerning");
            public static readonly GUIContent extraPaddingLabel = new GUIContent("Extra Padding");

            public static readonly GUIContent dynamicFontSystemSettingsLabel = new GUIContent("Dynamic Font System Settings");
            public static readonly GUIContent getFontFeaturesAtRuntime = new GUIContent("Get Font Features at Runtime", "Determines if Glyph Adjustment Data will be retrieved from font files at runtime when new characters and glyphs are added to font assets.");

            public static readonly GUIContent missingGlyphLabel = new GUIContent("Missing Character Unicode", "The character to be displayed when the requested character is not found in any font asset or fallbacks.");
            public static readonly GUIContent disableWarningsLabel = new GUIContent("Disable warnings", "Disable warning messages in the Console.");

            public static readonly GUIContent lineBreakingLabel = new GUIContent("Line Breaking for Asian languages", "The text assets that contain the Leading and Following characters which define the rules for line breaking with Asian languages.");
            public static readonly GUIContent koreanSpecificRules = new GUIContent("Korean Language Options");
        }

        SerializedProperty m_PropFontAsset;
        SerializedProperty m_PropDefaultFontAssetPath;
        SerializedProperty m_PropDefaultFontSize;
        SerializedProperty m_PropDefaultAutoSizeMinRatio;
        SerializedProperty m_PropDefaultAutoSizeMaxRatio;
        SerializedProperty m_PropDefaultTextMeshProTextContainerSize;
        SerializedProperty m_PropDefaultTextMeshProUITextContainerSize;
        SerializedProperty m_PropEnableRaycastTarget;
        SerializedProperty m_PropIsTextObjectScaleStatic;


        ReorderableList m_List;

        SerializedProperty m_PropMatchMaterialPreset;
        SerializedProperty m_PropWordWrapping;
        SerializedProperty m_PropKerning;
        SerializedProperty m_PropExtraPadding;
        SerializedProperty m_PropMissingGlyphCharacter;

        SerializedProperty m_GetFontFeaturesAtRuntime;

        SerializedProperty m_PropWarningsDisabled;

        SerializedProperty m_PropLeadingCharacters;
        SerializedProperty m_PropFollowingCharacters;
        SerializedProperty m_PropUseModernHangulLineBreakingRules;

        private const string k_UndoRedo = "UndoRedoPerformed";

        public void OnEnable()
        {
            if (target == null)
                return;

            m_PropFontAsset = serializedObject.FindProperty("m_defaultFontAsset");
            m_PropDefaultFontAssetPath = serializedObject.FindProperty("m_defaultFontAssetPath");
            m_PropDefaultFontSize = serializedObject.FindProperty("m_defaultFontSize");
            m_PropDefaultAutoSizeMinRatio = serializedObject.FindProperty("m_defaultAutoSizeMinRatio");
            m_PropDefaultAutoSizeMaxRatio = serializedObject.FindProperty("m_defaultAutoSizeMaxRatio");
            m_PropDefaultTextMeshProTextContainerSize = serializedObject.FindProperty("m_defaultTextMeshProTextContainerSize");
            m_PropDefaultTextMeshProUITextContainerSize = serializedObject.FindProperty("m_defaultTextMeshProUITextContainerSize");
            m_PropEnableRaycastTarget = serializedObject.FindProperty("m_EnableRaycastTarget");
            m_PropIsTextObjectScaleStatic = serializedObject.FindProperty("m_IsTextObjectScaleStatic");

            m_List = new ReorderableList(serializedObject, serializedObject.FindProperty("m_fallbackFontAssets"), true, true, true, true);

            m_List.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var element = m_List.serializedProperty.GetArrayElementAtIndex(index);
                rect.y += 2;
                EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), element, GUIContent.none);
            };

            m_List.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, Styles.fallbackFontAssetsListLabel);
            };

            m_PropMatchMaterialPreset = serializedObject.FindProperty("m_matchMaterialPreset");

            m_PropWordWrapping = serializedObject.FindProperty("m_enableWordWrapping");
            m_PropKerning = serializedObject.FindProperty("m_enableKerning");
            m_PropExtraPadding = serializedObject.FindProperty("m_enableExtraPadding");
            m_PropMissingGlyphCharacter = serializedObject.FindProperty("m_missingGlyphCharacter");

            m_PropWarningsDisabled = serializedObject.FindProperty("m_warningsDisabled");

            m_GetFontFeaturesAtRuntime = serializedObject.FindProperty("m_GetFontFeaturesAtRuntime");

            m_PropLeadingCharacters = serializedObject.FindProperty("m_leadingCharacters");
            m_PropFollowingCharacters = serializedObject.FindProperty("m_followingCharacters");
            m_PropUseModernHangulLineBreakingRules = serializedObject.FindProperty("m_UseModernHangulLineBreakingRules");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            string evt_cmd = Event.current.commandName;

            float labelWidth = EditorGUIUtility.labelWidth;
            float fieldWidth = EditorGUIUtility.fieldWidth;

            // TextMeshPro Font Info Panel
            EditorGUI.indentLevel = 0;

            // FONT ASSET
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(Styles.defaultFontAssetLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(m_PropFontAsset, Styles.defaultFontAssetLabel);
            EditorGUILayout.PropertyField(m_PropDefaultFontAssetPath, Styles.defaultFontAssetPathLabel);
            EditorGUI.indentLevel = 0;

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            // FALLBACK FONT ASSETs
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(Styles.fallbackFontAssetsLabel, EditorStyles.boldLabel);
            m_List.DoLayoutList();

            GUILayout.Label(Styles.fallbackMaterialSettingsLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(m_PropMatchMaterialPreset, Styles.matchMaterialPresetLabel);
            EditorGUI.indentLevel = 0;

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            // MISSING GLYPHS
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(Styles.dynamicFontSystemSettingsLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(m_GetFontFeaturesAtRuntime, Styles.getFontFeaturesAtRuntime);
            EditorGUILayout.PropertyField(m_PropMissingGlyphCharacter, Styles.missingGlyphLabel);
            EditorGUILayout.PropertyField(m_PropWarningsDisabled, Styles.disableWarningsLabel);
            //EditorGUILayout.PropertyField(m_DynamicAtlasTextureManager, Styles.dynamicAtlasTextureManager);
            EditorGUI.indentLevel = 0;

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            // TEXT OBJECT DEFAULT PROPERTIES
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(Styles.containerDefaultSettingsLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;

            EditorGUILayout.PropertyField(m_PropDefaultTextMeshProTextContainerSize, Styles.textMeshProLabel);
            EditorGUILayout.PropertyField(m_PropDefaultTextMeshProUITextContainerSize, Styles.textMeshProUiLabel);
            EditorGUILayout.PropertyField(m_PropEnableRaycastTarget, Styles.enableRaycastTarget);
            EditorGUILayout.PropertyField(m_PropIsTextObjectScaleStatic, Styles.isTextObjectScaleStaticLabel);
            EditorGUI.indentLevel = 0;

            EditorGUILayout.Space();

            GUILayout.Label(Styles.textComponentDefaultSettingsLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(m_PropDefaultFontSize, Styles.defaultFontSize);

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.PrefixLabel(Styles.autoSizeRatioLabel);
                EditorGUIUtility.labelWidth = 32;
                EditorGUIUtility.fieldWidth = 10;

                EditorGUI.indentLevel = 0;
                EditorGUILayout.PropertyField(m_PropDefaultAutoSizeMinRatio, Styles.minLabel);
                EditorGUILayout.PropertyField(m_PropDefaultAutoSizeMaxRatio, Styles.maxLabel);
                EditorGUI.indentLevel = 1;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUIUtility.fieldWidth = fieldWidth;

            EditorGUILayout.PropertyField(m_PropWordWrapping, Styles.wordWrappingLabel);
            EditorGUILayout.PropertyField(m_PropKerning, Styles.kerningLabel);

            EditorGUILayout.PropertyField(m_PropExtraPadding, Styles.extraPaddingLabel);

            EditorGUI.indentLevel = 0;

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            // LINE BREAKING RULE
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(Styles.lineBreakingLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(m_PropLeadingCharacters);
            EditorGUILayout.PropertyField(m_PropFollowingCharacters);

            EditorGUILayout.Space();
            GUILayout.Label(Styles.koreanSpecificRules, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_PropUseModernHangulLineBreakingRules, new GUIContent("Use Modern Line Breaking", "Determines if traditional or modern line breaking rules will be used to control line breaking. Traditional line breaking rules use the Leading and Following Character rules whereas Modern uses spaces for line breaking."));

            EditorGUI.indentLevel = 0;

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            if (serializedObject.ApplyModifiedProperties() || evt_cmd == k_UndoRedo)
            {
                EditorUtility.SetDirty(target);
                TMPro_EventManager.ON_TMP_SETTINGS_CHANGED();
            }
        }
    }
}
