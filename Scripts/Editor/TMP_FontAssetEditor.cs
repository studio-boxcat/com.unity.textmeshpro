using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.TextCore.LowLevel;

#if UNITY_2018_4_OR_NEWER && !UNITY_2018_4_0 && !UNITY_2018_4_1 && !UNITY_2018_4_2 && !UNITY_2018_4_3 && !UNITY_2018_4_4
    using UnityEditor.TextCore.LowLevel;
#endif


namespace TMPro.EditorUtilities
{
    [CustomEditor(typeof(TMP_FontAsset))]
    public class TMP_FontAssetEditor : Editor
    {
        private struct UI_PanelState
        {
            public static bool faceInfoPanel = true;
            public static bool generationSettingsPanel = true;
            public static bool fontAtlasInfoPanel = true;
            public static bool fontWeightPanel = true;
            public static bool glyphTablePanel = false;
            public static bool characterTablePanel = false;
        }

        private struct AtlasSettings
        {
            public GlyphRenderMode glyphRenderMode;
            public int pointSize;
            public int padding;
            public int atlasWidth;
            public int atlasHeight;
        }

        /// <summary>
        /// Material used to display SDF glyphs in the Character and Glyph tables.
        /// </summary>
        internal static Material internalSDFMaterial
        {
            get
            {
                if (s_InternalSDFMaterial == null)
                {
                    Shader shader = Shader.Find("Hidden/TMP/Internal/Editor/Distance Field SSD");

                    if (shader != null)
                        s_InternalSDFMaterial = new Material(shader);
                }

                return s_InternalSDFMaterial;
            }
        }
        static Material s_InternalSDFMaterial;

        /// <summary>
        /// Material used to display Bitmap glyphs in the Character and Glyph tables.
        /// </summary>
        internal static Material internalBitmapMaterial
        {
            get
            {
                if (s_InternalBitmapMaterial == null)
                {
                    Shader shader = Shader.Find("Hidden/Internal-GUITextureClipText");

                    if (shader != null)
                        s_InternalBitmapMaterial = new Material(shader);
                }

                return s_InternalBitmapMaterial;
            }
        }
        static Material s_InternalBitmapMaterial;

        private static string[] s_UiStateLabel = new string[] { "<i>(Click to collapse)</i> ", "<i>(Click to expand)</i> " };
        private GUIContent[] m_AtlasResolutionLabels = { new GUIContent("8"), new GUIContent("16"), new GUIContent("32"), new GUIContent("64"), new GUIContent("128"), new GUIContent("256"), new GUIContent("512"), new GUIContent("1024"), new GUIContent("2048"), new GUIContent("4096"), new GUIContent("8192") };
        private int[] m_AtlasResolutions = { 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 };

        private struct Warning
        {
            public bool isEnabled;
            public double expirationTime;
        }

        private int m_CurrentGlyphPage = 0;
        private int m_CurrentCharacterPage = 0;

        private int m_SelectedGlyphRecord = -1;
        private int m_SelectedCharacterRecord = -1;

        private string m_dstGlyphID;
        private string m_dstUnicode;
        private const string k_placeholderUnicodeHex = "<i>New Unicode (Hex)</i>";
        private string m_unicodeHexLabel = k_placeholderUnicodeHex;
        private const string k_placeholderGlyphID = "<i>New Glyph ID</i>";
        private string m_GlyphIDLabel = k_placeholderGlyphID;

        private Warning m_AddGlyphWarning;
        private Warning m_AddCharacterWarning;
        private bool m_DisplayDestructiveChangeWarning;
        private AtlasSettings m_AtlasSettings;
        private bool m_MaterialPresetsRequireUpdate;

        private string m_GlyphSearchPattern;
        private List<int> m_GlyphSearchList;

        private string m_CharacterSearchPattern;
        private List<int> m_CharacterSearchList;

        private bool m_isSearchDirty;

        private const string k_UndoRedo = "UndoRedoPerformed";

        private SerializedProperty m_AtlasPopulationMode_prop;
        private SerializedProperty font_atlas_prop;
        private SerializedProperty font_material_prop;

        private SerializedProperty m_AtlasRenderMode_prop;
        private SerializedProperty m_SamplingPointSize_prop;

        // Unity 6 changed FaceInfo.pointSize from int to float
        private int GetPointSize() => (int)m_SamplingPointSize_prop.floatValue;
        private void SetPointSize(int value) => m_SamplingPointSize_prop.floatValue = value;

        private SerializedProperty m_AtlasPadding_prop;
        private SerializedProperty m_AtlasWidth_prop;
        private SerializedProperty m_AtlasHeight_prop;
        private SerializedProperty m_IsMultiAtlasTexturesEnabled_prop;
        private SerializedProperty m_ClearDynamicDataOnBuild_prop;

        private SerializedProperty font_normalStyle_prop;
        private SerializedProperty font_normalSpacing_prop;

        private SerializedProperty font_boldStyle_prop;
        private SerializedProperty font_boldSpacing_prop;

        private SerializedProperty font_italicStyle_prop;
        private SerializedProperty font_tabSize_prop;

        private SerializedProperty m_FaceInfo_prop;
        private SerializedProperty m_GlyphTable_prop;
        private SerializedProperty m_CharacterTable_prop;

        private TMP_FontAsset m_fontAsset;

        private Material[] m_materialPresets;

        private bool isAssetDirty = false;

        private System.DateTime timeStamp;


        public void OnEnable()
        {
            m_FaceInfo_prop = serializedObject.FindProperty("m_FaceInfo");

            font_atlas_prop = serializedObject.FindProperty("m_AtlasTextures").GetArrayElementAtIndex(0);
            font_material_prop = serializedObject.FindProperty("material");

            m_AtlasPopulationMode_prop = serializedObject.FindProperty("m_AtlasPopulationMode");
            m_AtlasRenderMode_prop = serializedObject.FindProperty("m_AtlasRenderMode");
            m_SamplingPointSize_prop = m_FaceInfo_prop.FindPropertyRelative("m_PointSize");
            m_AtlasPadding_prop = serializedObject.FindProperty("m_AtlasPadding");
            m_AtlasWidth_prop = serializedObject.FindProperty("m_AtlasWidth");
            m_AtlasHeight_prop = serializedObject.FindProperty("m_AtlasHeight");
            m_IsMultiAtlasTexturesEnabled_prop = serializedObject.FindProperty("m_IsMultiAtlasTexturesEnabled");
            m_ClearDynamicDataOnBuild_prop = serializedObject.FindProperty("m_ClearDynamicDataOnBuild");

            font_normalStyle_prop = serializedObject.FindProperty("normalStyle");
            font_normalSpacing_prop = serializedObject.FindProperty("normalSpacingOffset");

            font_boldStyle_prop = serializedObject.FindProperty("boldStyle");
            font_boldSpacing_prop = serializedObject.FindProperty("boldSpacing");

            font_italicStyle_prop = serializedObject.FindProperty("italicStyle");
            font_tabSize_prop = serializedObject.FindProperty("tabSize");

            m_CharacterTable_prop = serializedObject.FindProperty("m_CharacterTable");
            m_GlyphTable_prop = serializedObject.FindProperty("m_GlyphTable");

            m_fontAsset = target as TMP_FontAsset;

            m_materialPresets = TMP_EditorUtility.FindMaterialReferences(m_fontAsset);

            m_GlyphSearchList = new List<int>();

            // Sort Font Asset Tables
            m_fontAsset.SortAllTables();
        }


        public void OnDisable()
        {
            // Revert changes if user closes or changes selection without having made a choice.
            if (m_DisplayDestructiveChangeWarning)
            {
                m_DisplayDestructiveChangeWarning = false;
                RestoreAtlasGenerationSettings();
                GUIUtility.keyboardControl = 0;

                serializedObject.ApplyModifiedProperties();
            }
        }


        public override void OnInspectorGUI()
        {
            //Debug.Log("OnInspectorGUI Called.");

            serializedObject.Update();

            Rect rect = EditorGUILayout.GetControlRect(false, 24);
            float labelWidth = EditorGUIUtility.labelWidth;
            float fieldWidth = EditorGUIUtility.fieldWidth;

            // FACE INFO PANEL
            #region Face info
            GUI.Label(rect, new GUIContent("<b>Face Info</b>"), TMP_UIStyleManager.sectionHeader);

            rect.x += rect.width - 132f;
            rect.y += 2;
            rect.width = 130f;
            rect.height = 18f;
            if (GUI.Button(rect, new GUIContent("Update Atlas Texture")))
            {
                TMPro_FontAssetCreatorWindow.ShowFontAtlasCreatorWindow(target as TMP_FontAsset);
            }

            EditorGUI.indentLevel = 1;
            GUI.enabled = false; // Lock UI

            // TODO : Consider creating a property drawer for these.
            EditorGUILayout.PropertyField(m_FaceInfo_prop.FindPropertyRelative("m_FamilyName"));
            EditorGUILayout.PropertyField(m_FaceInfo_prop.FindPropertyRelative("m_StyleName"));
            EditorGUILayout.PropertyField(m_FaceInfo_prop.FindPropertyRelative("m_PointSize"));

            GUI.enabled = true;

            EditorGUILayout.PropertyField(m_FaceInfo_prop.FindPropertyRelative("m_Scale"));
            EditorGUILayout.PropertyField(m_FaceInfo_prop.FindPropertyRelative("m_LineHeight"));

            EditorGUILayout.PropertyField(m_FaceInfo_prop.FindPropertyRelative("m_AscentLine"));
            EditorGUILayout.PropertyField(m_FaceInfo_prop.FindPropertyRelative("m_CapLine"));
            EditorGUILayout.PropertyField(m_FaceInfo_prop.FindPropertyRelative("m_MeanLine"));
            EditorGUILayout.PropertyField(m_FaceInfo_prop.FindPropertyRelative("m_Baseline"));
            EditorGUILayout.PropertyField(m_FaceInfo_prop.FindPropertyRelative("m_DescentLine"));
            EditorGUILayout.PropertyField(m_FaceInfo_prop.FindPropertyRelative("m_TabWidth"));
            // TODO : Add clamping for some of these values.
            //subSize_prop.floatValue = Mathf.Clamp(subSize_prop.floatValue, 0.25f, 1f);

            EditorGUILayout.Space();
            #endregion

            // GENERATION SETTINGS
            #region Generation Settings
            rect = EditorGUILayout.GetControlRect(false, 24);

            if (GUI.Button(rect, new GUIContent("<b>Generation Settings</b>"), TMP_UIStyleManager.sectionHeader))
                UI_PanelState.generationSettingsPanel = !UI_PanelState.generationSettingsPanel;

            GUI.Label(rect, (UI_PanelState.generationSettingsPanel ? "" : s_UiStateLabel[1]), TMP_UIStyleManager.rightLabel);

            if (UI_PanelState.generationSettingsPanel)
            {
                EditorGUI.indentLevel = 1;

                EditorGUI.BeginChangeCheck();
                Font sourceFont = (Font)EditorGUILayout.ObjectField("Source Font File", m_fontAsset.m_SourceFontFile_EditorRef, typeof(Font), false);
                if (EditorGUI.EndChangeCheck())
                {
                    string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sourceFont));
                    m_fontAsset.m_SourceFontFileGUID = guid;
                    m_fontAsset.m_SourceFontFile_EditorRef = sourceFont;
                }

                EditorGUI.BeginDisabledGroup(sourceFont == null);
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(m_AtlasPopulationMode_prop, new GUIContent("Atlas Population Mode"));
                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.ApplyModifiedProperties();

                        bool isDatabaseRefreshRequired = false;

                        if (m_AtlasPopulationMode_prop.intValue == 0)
                        {
                            m_fontAsset.sourceFontFile = null;

                            //Set atlas textures to non readable.
                            for (int i = 0; i < m_fontAsset.atlasTextures.Length; i++)
                            {
                                Texture2D tex = m_fontAsset.atlasTextures[i];

                                if (tex != null && tex.isReadable)
                                {
                                    #if UNITY_2018_4_OR_NEWER && !UNITY_2018_4_0 && !UNITY_2018_4_1 && !UNITY_2018_4_2 && !UNITY_2018_4_3 && !UNITY_2018_4_4
                                        FontEngineEditorUtilities.SetAtlasTextureIsReadable(tex, false);
                                    #endif
                                }
                            }

                            Debug.Log("Atlas Population mode set to [Static].");
                        }
                        else if (m_AtlasPopulationMode_prop.intValue == 1)
                        {
                            if (m_fontAsset.m_SourceFontFile_EditorRef.dynamic == false)
                            {
                                Debug.LogWarning("Please set the [" + m_fontAsset.name + "] font to dynamic mode as this is required for Dynamic SDF support.", m_fontAsset.m_SourceFontFile_EditorRef);
                                m_AtlasPopulationMode_prop.intValue = 0;

                                serializedObject.ApplyModifiedProperties();
                            }
                            else
                            {
                                m_fontAsset.sourceFontFile = m_fontAsset.m_SourceFontFile_EditorRef;

                                // Set atlas textures to non readable.
                                for (int i = 0; i < m_fontAsset.atlasTextures.Length; i++)
                                {
                                    Texture2D tex = m_fontAsset.atlasTextures[i];

                                    if (tex != null && tex.isReadable == false)
                                    {
                                        #if UNITY_2018_4_OR_NEWER && !UNITY_2018_4_0 && !UNITY_2018_4_1 && !UNITY_2018_4_2 && !UNITY_2018_4_3 && !UNITY_2018_4_4
                                            FontEngineEditorUtilities.SetAtlasTextureIsReadable(tex, true);
                                        #endif
                                    }
                                }

                                Debug.Log("Atlas Population mode set to [Dynamic].");
                            }
                        }

                        if (isDatabaseRefreshRequired)
                            AssetDatabase.Refresh();

                        serializedObject.Update();
                        isAssetDirty = true;
                    }

                    // Save state of atlas settings
                    if (m_DisplayDestructiveChangeWarning == false)
                    {
                        SavedAtlasGenerationSettings();
                        //Undo.RegisterCompleteObjectUndo(m_fontAsset, "Font Asset Changes");
                    }

                    EditorGUI.BeginDisabledGroup(m_AtlasPopulationMode_prop.intValue == (int)AtlasPopulationMode.Static);
                    {
                        EditorGUI.BeginChangeCheck();
                        // TODO: Switch shaders depending on GlyphRenderMode.
                        EditorGUILayout.PropertyField(m_AtlasRenderMode_prop);
                        EditorGUILayout.PropertyField(m_SamplingPointSize_prop, new GUIContent("Sampling Point Size"));
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_DisplayDestructiveChangeWarning = true;
                        }

                        // Changes to these properties require updating Material Presets for this font asset.
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(m_AtlasPadding_prop, new GUIContent("Padding"));
                        EditorGUILayout.IntPopup(m_AtlasWidth_prop, m_AtlasResolutionLabels, m_AtlasResolutions, new GUIContent("Atlas Width"));
                        EditorGUILayout.IntPopup(m_AtlasHeight_prop, m_AtlasResolutionLabels, m_AtlasResolutions, new GUIContent("Atlas Height"));
                        EditorGUILayout.PropertyField(m_IsMultiAtlasTexturesEnabled_prop, new GUIContent("Multi Atlas Textures", "Determines if the font asset will store glyphs in multiple atlas textures."));
                        EditorGUILayout.PropertyField(m_ClearDynamicDataOnBuild_prop, new GUIContent("Clear Dynamic Data On Build", "Clears all dynamic data restoring the font asset back to its default creation and empty state."));
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_MaterialPresetsRequireUpdate = true;
                            m_DisplayDestructiveChangeWarning = true;
                        }

                        EditorGUILayout.Space();

                        if (m_DisplayDestructiveChangeWarning)
                        {
                            // These changes are destructive on the font asset
                            rect = EditorGUILayout.GetControlRect(false, 60);
                            rect.x += 15;
                            rect.width -= 15;
                            EditorGUI.HelpBox(rect, "Changing these settings will clear the font asset's character, glyph and texture data.", MessageType.Warning);

                            if (GUI.Button(new Rect(rect.width - 140, rect.y + 36, 80, 18), new GUIContent("Apply")))
                            {
                                m_DisplayDestructiveChangeWarning = false;

                                // Update face info is sampling point size was changed.
                                if (m_AtlasSettings.pointSize != GetPointSize())
                                {
                                    FontEngine.LoadFontFace(m_fontAsset.sourceFontFile, GetPointSize());
                                    m_fontAsset.faceInfo = FontEngine.GetFaceInfo();
                                }

                                Material mat = m_fontAsset.material;

                                // Update material
                                mat.SetFloat(ShaderUtilities.ID_TextureWidth, m_AtlasWidth_prop.intValue);
                                mat.SetFloat(ShaderUtilities.ID_TextureHeight, m_AtlasHeight_prop.intValue);

                                if (mat.HasProperty(ShaderUtilities.ID_GradientScale))
                                    mat.SetFloat(ShaderUtilities.ID_GradientScale, m_AtlasPadding_prop.intValue + 1);

                                // Update material presets if any of the relevant properties have been changed.
                                if (m_MaterialPresetsRequireUpdate)
                                {
                                    m_MaterialPresetsRequireUpdate = false;

                                    Material[] materialPresets = TMP_EditorUtility.FindMaterialReferences(m_fontAsset);
                                    for (int i = 0; i < materialPresets.Length; i++)
                                    {
                                        mat = materialPresets[i];

                                        mat.SetFloat(ShaderUtilities.ID_TextureWidth, m_AtlasWidth_prop.intValue);
                                        mat.SetFloat(ShaderUtilities.ID_TextureHeight, m_AtlasHeight_prop.intValue);

                                        if (mat.HasProperty(ShaderUtilities.ID_GradientScale))
                                            mat.SetFloat(ShaderUtilities.ID_GradientScale, m_AtlasPadding_prop.intValue + 1);
                                    }
                                }

                                m_fontAsset.UpdateFontAssetData();
                                GUIUtility.keyboardControl = 0;
                                isAssetDirty = true;

                                // Update Font Asset Creation Settings to reflect new changes.
                                UpdateFontAssetCreationSettings();

                                // TODO: Clear undo buffers.
                                //Undo.ClearUndo(m_fontAsset);
                            }

                            if (GUI.Button(new Rect(rect.width - 56, rect.y + 36, 80, 18), new GUIContent("Revert")))
                            {
                                m_DisplayDestructiveChangeWarning = false;
                                RestoreAtlasGenerationSettings();
                                GUIUtility.keyboardControl = 0;

                                // TODO: Clear undo buffers.
                                //Undo.ClearUndo(m_fontAsset);
                            }
                        }
                    }
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.Space();
            }
            #endregion

            // ATLAS & MATERIAL PANEL
            #region Atlas & Material
            rect = EditorGUILayout.GetControlRect(false, 24);

            if (GUI.Button(rect, new GUIContent("<b>Atlas & Material</b>"), TMP_UIStyleManager.sectionHeader))
                UI_PanelState.fontAtlasInfoPanel = !UI_PanelState.fontAtlasInfoPanel;

            GUI.Label(rect, (UI_PanelState.fontAtlasInfoPanel ? "" : s_UiStateLabel[1]), TMP_UIStyleManager.rightLabel);

            if (UI_PanelState.fontAtlasInfoPanel)
            {
                EditorGUI.indentLevel = 1;

                GUI.enabled = false;
                EditorGUILayout.PropertyField(font_atlas_prop, new GUIContent("Font Atlas"));
                EditorGUILayout.PropertyField(font_material_prop, new GUIContent("Font Material"));
                GUI.enabled = true;
                EditorGUILayout.Space();
            }
            #endregion

            string evt_cmd = Event.current.commandName; // Get Current Event CommandName to check for Undo Events

            // FONT WEIGHT PANEL
            #region Font Weights
            rect = EditorGUILayout.GetControlRect(false, 24);

            if (GUI.Button(rect, new GUIContent("<b>Font Weights</b>", "The Font Assets that will be used for different font weights and the settings used to simulate a typeface when no asset is available."), TMP_UIStyleManager.sectionHeader))
                UI_PanelState.fontWeightPanel = !UI_PanelState.fontWeightPanel;

            GUI.Label(rect, (UI_PanelState.fontWeightPanel ? "" : s_UiStateLabel[1]), TMP_UIStyleManager.rightLabel);

            if (UI_PanelState.fontWeightPanel)
            {
                EditorGUIUtility.labelWidth *= 0.75f;
                EditorGUIUtility.fieldWidth *= 0.25f;

                EditorGUILayout.BeginVertical();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(font_normalStyle_prop, new GUIContent("Normal Weight"));
                font_normalStyle_prop.floatValue = Mathf.Clamp(font_normalStyle_prop.floatValue, -3.0f, 3.0f);
                if (GUI.changed || evt_cmd == k_UndoRedo)
                {
                    GUI.changed = false;

                    // Modify the material property on matching material presets.
                    for (int i = 0; i < m_materialPresets.Length; i++)
                        m_materialPresets[i].SetFloat("_WeightNormal", font_normalStyle_prop.floatValue);
                }

                EditorGUILayout.PropertyField(font_boldStyle_prop, new GUIContent("Bold Weight"));
                font_boldStyle_prop.floatValue = Mathf.Clamp(font_boldStyle_prop.floatValue, -3.0f, 3.0f);
                if (GUI.changed || evt_cmd == k_UndoRedo)
                {
                    GUI.changed = false;

                    // Modify the material property on matching material presets.
                    for (int i = 0; i < m_materialPresets.Length; i++)
                        m_materialPresets[i].SetFloat("_WeightBold", font_boldStyle_prop.floatValue);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(font_normalSpacing_prop, new GUIContent("Spacing Offset"));
                font_normalSpacing_prop.floatValue = Mathf.Clamp(font_normalSpacing_prop.floatValue, -100, 100);
                if (GUI.changed || evt_cmd == k_UndoRedo)
                {
                    GUI.changed = false;
                }

                EditorGUILayout.PropertyField(font_boldSpacing_prop, new GUIContent("Bold Spacing"));
                font_boldSpacing_prop.floatValue = Mathf.Clamp(font_boldSpacing_prop.floatValue, 0, 100);
                if (GUI.changed || evt_cmd == k_UndoRedo)
                {
                    GUI.changed = false;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(font_italicStyle_prop, new GUIContent("Italic Style"));
                font_italicStyle_prop.intValue = Mathf.Clamp(font_italicStyle_prop.intValue, 15, 60);

                EditorGUILayout.PropertyField(font_tabSize_prop, new GUIContent("Tab Multiple"));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            EditorGUIUtility.labelWidth = 0;
            EditorGUIUtility.fieldWidth = 0;
            #endregion

            // CHARACTER TABLE TABLE
            #region Character Table
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUIUtility.fieldWidth = fieldWidth;
            EditorGUI.indentLevel = 0;
            rect = EditorGUILayout.GetControlRect(false, 24);

            int characterCount = m_fontAsset.characterTable.Count;

            if (GUI.Button(rect, new GUIContent("<b>Character Table</b>   [" + characterCount + "]" + (rect.width > 320 ? " Characters" : ""), "List of characters contained in this font asset."), TMP_UIStyleManager.sectionHeader))
                UI_PanelState.characterTablePanel = !UI_PanelState.characterTablePanel;

            GUI.Label(rect, (UI_PanelState.characterTablePanel ? "" : s_UiStateLabel[1]), TMP_UIStyleManager.rightLabel);

            if (UI_PanelState.characterTablePanel)
            {
                int arraySize = m_CharacterTable_prop.arraySize;
                int itemsPerPage = 15;

                // Display Glyph Management Tools
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    // Search Bar implementation
                    #region DISPLAY SEARCH BAR
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUIUtility.labelWidth = 130f;
                        EditorGUI.BeginChangeCheck();
                        string searchPattern = EditorGUILayout.TextField("Character Search", m_CharacterSearchPattern, "SearchTextField");
                        if (EditorGUI.EndChangeCheck() || m_isSearchDirty)
                        {
                            if (string.IsNullOrEmpty(searchPattern) == false)
                            {
                                m_CharacterSearchPattern = searchPattern;

                                // Search Character Table for potential matches
                                SearchCharacterTable (m_CharacterSearchPattern, ref m_CharacterSearchList);
                            }
                            else
                                m_CharacterSearchPattern = null;

                            m_isSearchDirty = false;
                        }

                        string styleName = string.IsNullOrEmpty(m_CharacterSearchPattern) ? "SearchCancelButtonEmpty" : "SearchCancelButton";
                        if (GUILayout.Button(GUIContent.none, styleName))
                        {
                            GUIUtility.keyboardControl = 0;
                            m_CharacterSearchPattern = string.Empty;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    #endregion

                    // Display Page Navigation
                    if (!string.IsNullOrEmpty(m_CharacterSearchPattern))
                        arraySize = m_CharacterSearchList.Count;

                    DisplayPageNavigation(ref m_CurrentCharacterPage, arraySize, itemsPerPage);
                }
                EditorGUILayout.EndVertical();

                // Display Character Table Elements
                if (arraySize > 0)
                {
                    // Display each character entry using the CharacterPropertyDrawer.
                    for (int i = itemsPerPage * m_CurrentCharacterPage; i < arraySize && i < itemsPerPage * (m_CurrentCharacterPage + 1); i++)
                    {
                        // Define the start of the selection region of the element.
                        Rect elementStartRegion = GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true));

                        int elementIndex = i;
                        if (!string.IsNullOrEmpty(m_CharacterSearchPattern))
                            elementIndex = m_CharacterSearchList[i];

                        SerializedProperty characterProperty = m_CharacterTable_prop.GetArrayElementAtIndex(elementIndex);

                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                        EditorGUI.BeginDisabledGroup(i != m_SelectedCharacterRecord);
                        {
                            EditorGUILayout.PropertyField(characterProperty);
                        }
                        EditorGUI.EndDisabledGroup();

                        EditorGUILayout.EndVertical();

                        // Define the end of the selection region of the element.
                        Rect elementEndRegion = GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true));

                        // Check for Item selection
                        Rect selectionArea = new Rect(elementStartRegion.x, elementStartRegion.y, elementEndRegion.width, elementEndRegion.y - elementStartRegion.y);
                        if (DoSelectionCheck(selectionArea))
                        {
                            if (m_SelectedCharacterRecord == i)
                                m_SelectedCharacterRecord = -1;
                            else
                            {
                                m_SelectedCharacterRecord = i;
                                m_AddCharacterWarning.isEnabled = false;
                                m_unicodeHexLabel = k_placeholderUnicodeHex;
                                GUIUtility.keyboardControl = 0;
                            }
                        }

                        // Draw Selection Highlight and Glyph Options
                        if (m_SelectedCharacterRecord == i)
                        {
                            TMP_EditorUtility.DrawBox(selectionArea, 2f, new Color32(40, 192, 255, 255));

                            // Draw Glyph management options
                            Rect controlRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight * 1f);
                            float optionAreaWidth = controlRect.width * 0.6f;
                            float btnWidth = optionAreaWidth / 3;

                            Rect position = new Rect(controlRect.x + controlRect.width * .4f, controlRect.y, btnWidth, controlRect.height);

                            // Copy Selected Glyph to Target Glyph ID
                            GUI.enabled = !string.IsNullOrEmpty(m_dstUnicode);
                            if (GUI.Button(position, new GUIContent("Copy to")))
                            {
                                GUIUtility.keyboardControl = 0;

                                // Convert Hex Value to Decimal
                                int dstGlyphID = TMP_TextUtilities.StringHexToInt(m_dstUnicode);

                                //Add new glyph at target Unicode hex id.
                                if (!AddNewCharacter(elementIndex, dstGlyphID))
                                {
                                    m_AddCharacterWarning.isEnabled = true;
                                    m_AddCharacterWarning.expirationTime = EditorApplication.timeSinceStartup + 1;
                                }

                                m_dstUnicode = string.Empty;
                                m_isSearchDirty = true;

                                TMPro_EventManager.ON_FONT_PROPERTY_CHANGED(true, m_fontAsset);
                            }

                            // Target Glyph ID
                            GUI.enabled = true;
                            position.x += btnWidth;

                            GUI.SetNextControlName("CharacterID_Input");
                            m_dstUnicode = EditorGUI.TextField(position, m_dstUnicode);

                            // Placeholder text
                            EditorGUI.LabelField(position, new GUIContent(m_unicodeHexLabel, "The Unicode (Hex) ID of the duplicated Character"), TMP_UIStyleManager.label);

                            // Only filter the input when the destination glyph ID text field has focus.
                            if (GUI.GetNameOfFocusedControl() == "CharacterID_Input")
                            {
                                m_unicodeHexLabel = string.Empty;

                                //Filter out unwanted characters.
                                char chr = Event.current.character;
                                if ((chr < '0' || chr > '9') && (chr < 'a' || chr > 'f') && (chr < 'A' || chr > 'F'))
                                {
                                    Event.current.character = '\0';
                                }
                            }
                            else
                            {
                                m_unicodeHexLabel = k_placeholderUnicodeHex;
                                //m_dstUnicode = string.Empty;
                            }


                            // Remove Glyph
                            position.x += btnWidth;
                            if (GUI.Button(position, "Remove"))
                            {
                                GUIUtility.keyboardControl = 0;

                                RemoveCharacterFromList(elementIndex);

                                isAssetDirty = true;
                                m_SelectedCharacterRecord = -1;
                                m_isSearchDirty = true;
                                break;
                            }

                            if (m_AddCharacterWarning.isEnabled && EditorApplication.timeSinceStartup < m_AddCharacterWarning.expirationTime)
                            {
                                EditorGUILayout.HelpBox("The Destination Character ID already exists", MessageType.Warning);
                            }

                        }
                    }
                }

                DisplayPageNavigation(ref m_CurrentCharacterPage, arraySize, itemsPerPage);

                EditorGUILayout.Space();
            }
            #endregion

            // GLYPH TABLE
            #region Glyph Table
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUIUtility.fieldWidth = fieldWidth;
            EditorGUI.indentLevel = 0;
            rect = EditorGUILayout.GetControlRect(false, 24);

            GUIStyle glyphPanelStyle = new GUIStyle(EditorStyles.helpBox);

            int glyphCount = m_fontAsset.glyphTable.Count;

            if (GUI.Button(rect, new GUIContent("<b>Glyph Table</b>   [" + glyphCount + "]" + (rect.width > 275 ? " Glyphs" : ""), "List of glyphs contained in this font asset."), TMP_UIStyleManager.sectionHeader))
                UI_PanelState.glyphTablePanel = !UI_PanelState.glyphTablePanel;

            GUI.Label(rect, (UI_PanelState.glyphTablePanel ? "" : s_UiStateLabel[1]), TMP_UIStyleManager.rightLabel);

            if (UI_PanelState.glyphTablePanel)
            {
                int arraySize = m_GlyphTable_prop.arraySize;
                int itemsPerPage = 15;

                // Display Glyph Management Tools
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    // Search Bar implementation
                    #region DISPLAY SEARCH BAR
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUIUtility.labelWidth = 130f;
                        EditorGUI.BeginChangeCheck();
                        string searchPattern = EditorGUILayout.TextField("Glyph Search", m_GlyphSearchPattern, "SearchTextField");
                        if (EditorGUI.EndChangeCheck() || m_isSearchDirty)
                        {
                            if (string.IsNullOrEmpty(searchPattern) == false)
                            {
                                m_GlyphSearchPattern = searchPattern;

                                // Search Glyph Table for potential matches
                                SearchGlyphTable(m_GlyphSearchPattern, ref m_GlyphSearchList);
                            }
                            else
                                m_GlyphSearchPattern = null;

                            m_isSearchDirty = false;
                        }

                        string styleName = string.IsNullOrEmpty(m_GlyphSearchPattern) ? "SearchCancelButtonEmpty" : "SearchCancelButton";
                        if (GUILayout.Button(GUIContent.none, styleName))
                        {
                            GUIUtility.keyboardControl = 0;
                            m_GlyphSearchPattern = string.Empty;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    #endregion

                    // Display Page Navigation
                    if (!string.IsNullOrEmpty(m_GlyphSearchPattern))
                        arraySize = m_GlyphSearchList.Count;

                    DisplayPageNavigation(ref m_CurrentGlyphPage, arraySize, itemsPerPage);
                }
                EditorGUILayout.EndVertical();

                // Display Glyph Table Elements

                if (arraySize > 0)
                {
                    // Display each GlyphInfo entry using the GlyphInfo property drawer.
                    for (int i = itemsPerPage * m_CurrentGlyphPage; i < arraySize && i < itemsPerPage * (m_CurrentGlyphPage + 1); i++)
                    {
                        // Define the start of the selection region of the element.
                        Rect elementStartRegion = GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true));

                        int elementIndex = i;
                        if (!string.IsNullOrEmpty(m_GlyphSearchPattern))
                            elementIndex = m_GlyphSearchList[i];

                        SerializedProperty glyphProperty = m_GlyphTable_prop.GetArrayElementAtIndex(elementIndex);

                        EditorGUILayout.BeginVertical(glyphPanelStyle);

                        using (new EditorGUI.DisabledScope(i != m_SelectedGlyphRecord))
                        {
                            EditorGUILayout.PropertyField(glyphProperty);
                        }

                        EditorGUILayout.EndVertical();

                        // Define the end of the selection region of the element.
                        Rect elementEndRegion = GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true));

                        // Check for Item selection
                        Rect selectionArea = new Rect(elementStartRegion.x, elementStartRegion.y, elementEndRegion.width, elementEndRegion.y - elementStartRegion.y);
                        if (DoSelectionCheck(selectionArea))
                        {
                            if (m_SelectedGlyphRecord == i)
                                m_SelectedGlyphRecord = -1;
                            else
                            {
                                m_SelectedGlyphRecord = i;
                                m_AddGlyphWarning.isEnabled = false;
                                m_unicodeHexLabel = k_placeholderUnicodeHex;
                                GUIUtility.keyboardControl = 0;
                            }
                        }

                        // Draw Selection Highlight and Glyph Options
                        if (m_SelectedGlyphRecord == i)
                        {
                            TMP_EditorUtility.DrawBox(selectionArea, 2f, new Color32(40, 192, 255, 255));

                            // Draw Glyph management options
                            Rect controlRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight * 1f);
                            float optionAreaWidth = controlRect.width * 0.6f;
                            float btnWidth = optionAreaWidth / 3;

                            Rect position = new Rect(controlRect.x + controlRect.width * .4f, controlRect.y, btnWidth, controlRect.height);

                            // Copy Selected Glyph to Target Glyph ID
                            GUI.enabled = !string.IsNullOrEmpty(m_dstGlyphID);
                            if (GUI.Button(position, new GUIContent("Copy to")))
                            {
                                GUIUtility.keyboardControl = 0;
                                int dstGlyphID;

                                // Convert Hex Value to Decimal
                                int.TryParse(m_dstGlyphID, out dstGlyphID);

                                //Add new glyph at target Unicode hex id.
                                if (!AddNewGlyph(elementIndex, dstGlyphID))
                                {
                                    m_AddGlyphWarning.isEnabled = true;
                                    m_AddGlyphWarning.expirationTime = EditorApplication.timeSinceStartup + 1;
                                }

                                m_dstGlyphID = string.Empty;
                                m_isSearchDirty = true;

                                TMPro_EventManager.ON_FONT_PROPERTY_CHANGED(true, m_fontAsset);
                            }

                            // Target Glyph ID
                            GUI.enabled = true;
                            position.x += btnWidth;

                            GUI.SetNextControlName("GlyphID_Input");
                            m_dstGlyphID = EditorGUI.TextField(position, m_dstGlyphID);

                            // Placeholder text
                            EditorGUI.LabelField(position, new GUIContent(m_GlyphIDLabel, "The Glyph ID of the duplicated Glyph"), TMP_UIStyleManager.label);

                            // Only filter the input when the destination glyph ID text field has focus.
                            if (GUI.GetNameOfFocusedControl() == "GlyphID_Input")
                            {
                                m_GlyphIDLabel = string.Empty;

                                //Filter out unwanted characters.
                                char chr = Event.current.character;
                                if ((chr < '0' || chr > '9'))
                                {
                                    Event.current.character = '\0';
                                }
                            }
                            else
                            {
                                m_GlyphIDLabel = k_placeholderGlyphID;
                                //m_dstGlyphID = string.Empty;
                            }

                            // Remove Glyph
                            position.x += btnWidth;
                            if (GUI.Button(position, "Remove"))
                            {
                                GUIUtility.keyboardControl = 0;

                                RemoveGlyphFromList(elementIndex);

                                isAssetDirty = true;
                                m_SelectedGlyphRecord = -1;
                                m_isSearchDirty = true;
                                break;
                            }

                            if (m_AddGlyphWarning.isEnabled && EditorApplication.timeSinceStartup < m_AddGlyphWarning.expirationTime)
                            {
                                EditorGUILayout.HelpBox("The Destination Glyph ID already exists", MessageType.Warning);
                            }

                        }
                    }
                }

                DisplayPageNavigation(ref m_CurrentGlyphPage, arraySize, itemsPerPage);

                EditorGUILayout.Space();
            }
            #endregion

            if (serializedObject.ApplyModifiedProperties() || evt_cmd == k_UndoRedo || isAssetDirty)
            {
                // Delay callback until user has decided to Apply or Revert the changes.
                if (m_DisplayDestructiveChangeWarning == false)
                    TMPro_EventManager.ON_FONT_PROPERTY_CHANGED(true, m_fontAsset);

                if (m_fontAsset.IsFontAssetLookupTablesDirty || evt_cmd == k_UndoRedo)
                    m_fontAsset.ReadFontAssetDefinition();

                isAssetDirty = false;
                EditorUtility.SetDirty(target);
            }


            // Clear selection if mouse event was not consumed.
            GUI.enabled = true;
        }

        void SavedAtlasGenerationSettings()
        {
            m_AtlasSettings.glyphRenderMode = (GlyphRenderMode)m_AtlasRenderMode_prop.intValue;
            m_AtlasSettings.pointSize       = GetPointSize();
            m_AtlasSettings.padding         = m_AtlasPadding_prop.intValue;
            m_AtlasSettings.atlasWidth      = m_AtlasWidth_prop.intValue;
            m_AtlasSettings.atlasHeight     = m_AtlasHeight_prop.intValue;
        }

        void RestoreAtlasGenerationSettings()
        {
            m_AtlasRenderMode_prop.intValue = (int)m_AtlasSettings.glyphRenderMode;
            SetPointSize(m_AtlasSettings.pointSize);
            m_AtlasPadding_prop.intValue = m_AtlasSettings.padding;
            m_AtlasWidth_prop.intValue = m_AtlasSettings.atlasWidth;
            m_AtlasHeight_prop.intValue = m_AtlasSettings.atlasHeight;
        }


        void UpdateFontAssetCreationSettings()
        {
            m_fontAsset.m_CreationSettings.pointSize = GetPointSize();
            m_fontAsset.m_CreationSettings.renderMode = m_AtlasRenderMode_prop.intValue;
            m_fontAsset.m_CreationSettings.padding = m_AtlasPadding_prop.intValue;
            m_fontAsset.m_CreationSettings.atlasWidth = m_AtlasWidth_prop.intValue;
            m_fontAsset.m_CreationSettings.atlasHeight = m_AtlasHeight_prop.intValue;
        }

        static void DisplayPageNavigation(ref int currentPage, int arraySize, int itemsPerPage)
        {
            Rect pagePos = EditorGUILayout.GetControlRect(false, 20);
            pagePos.width /= 3;

            int shiftMultiplier = Event.current.shift ? 10 : 1; // Page + Shift goes 10 page forward

            // Previous Page
            GUI.enabled = currentPage > 0;

            if (GUI.Button(pagePos, "Previous Page"))
                currentPage -= 1 * shiftMultiplier;


            // Page Counter
            GUI.enabled = true;
            pagePos.x += pagePos.width;
            int totalPages = (int)(arraySize / (float)itemsPerPage + 0.999f);
            GUI.Label(pagePos, "Page " + (currentPage + 1) + " / " + totalPages, TMP_UIStyleManager.centeredLabel);

            // Next Page
            pagePos.x += pagePos.width;
            GUI.enabled = itemsPerPage * (currentPage + 1) < arraySize;

            if (GUI.Button(pagePos, "Next Page"))
                currentPage += 1 * shiftMultiplier;

            // Clamp page range
            currentPage = Mathf.Clamp(currentPage, 0, arraySize / itemsPerPage);

            GUI.enabled = true;
        }


        /// <summary>
        ///
        /// </summary>
        /// <param name="srcGlyphID"></param>
        /// <param name="dstGlyphID"></param>
        bool AddNewGlyph(int srcIndex, int dstGlyphID)
        {
            // Make sure Destination Glyph ID doesn't already contain a Glyph
            if (m_fontAsset.glyphLookupTable.ContainsKey((uint)dstGlyphID))
                return false;

            // Add new element to glyph list.
            m_GlyphTable_prop.arraySize += 1;

            // Get a reference to the source glyph.
            SerializedProperty sourceGlyph = m_GlyphTable_prop.GetArrayElementAtIndex(srcIndex);

            int dstIndex = m_GlyphTable_prop.arraySize - 1;

            // Get a reference to the target / destination glyph.
            SerializedProperty targetGlyph = m_GlyphTable_prop.GetArrayElementAtIndex(dstIndex);

            CopyGlyphSerializedProperty(sourceGlyph, ref targetGlyph);

            // Update the ID of the glyph
            targetGlyph.FindPropertyRelative("m_Index").intValue = dstGlyphID;

            serializedObject.ApplyModifiedProperties();

            m_fontAsset.SortGlyphTable();

            m_fontAsset.ReadFontAssetDefinition();

            return true;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="glyphID"></param>
        void RemoveGlyphFromList(int index)
        {
            if (index > m_GlyphTable_prop.arraySize)
                return;

            int targetGlyphIndex = m_GlyphTable_prop.GetArrayElementAtIndex(index).FindPropertyRelative("m_Index").intValue;

            m_GlyphTable_prop.DeleteArrayElementAtIndex(index);

            // Remove all characters referencing this glyph.
            for (int i = 0; i < m_CharacterTable_prop.arraySize; i++)
            {
                int glyphIndex = m_CharacterTable_prop.GetArrayElementAtIndex(i).FindPropertyRelative("m_GlyphIndex").intValue;

                if (glyphIndex == targetGlyphIndex)
                {
                    // Remove character
                    m_CharacterTable_prop.DeleteArrayElementAtIndex(i);
                }
            }

            serializedObject.ApplyModifiedProperties();

            m_fontAsset.ReadFontAssetDefinition();
        }

        bool AddNewCharacter(int srcIndex, int dstGlyphID)
        {
            // Make sure Destination Glyph ID doesn't already contain a Glyph
            if (m_fontAsset.characterLookupTable.ContainsKey((uint)dstGlyphID))
                return false;

            // Add new element to glyph list.
            m_CharacterTable_prop.arraySize += 1;

            // Get a reference to the source glyph.
            SerializedProperty sourceCharacter = m_CharacterTable_prop.GetArrayElementAtIndex(srcIndex);

            int dstIndex = m_CharacterTable_prop.arraySize - 1;

            // Get a reference to the target / destination glyph.
            SerializedProperty targetCharacter = m_CharacterTable_prop.GetArrayElementAtIndex(dstIndex);

            CopyCharacterSerializedProperty(sourceCharacter, ref targetCharacter);

            // Update the ID of the glyph
            targetCharacter.FindPropertyRelative("m_Unicode").intValue = dstGlyphID;

            serializedObject.ApplyModifiedProperties();

            m_fontAsset.SortCharacterTable();

            m_fontAsset.ReadFontAssetDefinition();

            return true;
        }

        void RemoveCharacterFromList(int index)
        {
            if (index > m_CharacterTable_prop.arraySize)
                return;

            m_CharacterTable_prop.DeleteArrayElementAtIndex(index);

            serializedObject.ApplyModifiedProperties();

            m_fontAsset.ReadFontAssetDefinition();
        }


        // Check if any of the Style elements were clicked on.
        private bool DoSelectionCheck(Rect selectionArea)
        {
            Event currentEvent = Event.current;

            switch (currentEvent.type)
            {
                case EventType.MouseDown:
                    if (selectionArea.Contains(currentEvent.mousePosition) && currentEvent.button == 0)
                    {
                        currentEvent.Use();
                        return true;
                    }

                    break;
            }

            return false;
        }

        TMP_GlyphValueRecord GetValueRecord(SerializedProperty property)
        {
            TMP_GlyphValueRecord record = new TMP_GlyphValueRecord();
            record.xPlacement = property.FindPropertyRelative("m_XPlacement").floatValue;
            record.yPlacement = property.FindPropertyRelative("m_YPlacement").floatValue;
            record.xAdvance = property.FindPropertyRelative("m_XAdvance").floatValue;
            record.yAdvance = property.FindPropertyRelative("m_YAdvance").floatValue;

            return record;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="srcGlyph"></param>
        /// <param name="dstGlyph"></param>
        static void CopyGlyphSerializedProperty(SerializedProperty srcGlyph, ref SerializedProperty dstGlyph)
        {
            // TODO : Should make a generic function which copies each of the properties.
            dstGlyph.FindPropertyRelative("m_Index").intValue = srcGlyph.FindPropertyRelative("m_Index").intValue;

            // Glyph -> GlyphMetrics
            SerializedProperty srcGlyphMetrics = srcGlyph.FindPropertyRelative("m_Metrics");
            SerializedProperty dstGlyphMetrics = dstGlyph.FindPropertyRelative("m_Metrics");

            dstGlyphMetrics.FindPropertyRelative("m_Width").floatValue = srcGlyphMetrics.FindPropertyRelative("m_Width").floatValue;
            dstGlyphMetrics.FindPropertyRelative("m_Height").floatValue = srcGlyphMetrics.FindPropertyRelative("m_Height").floatValue;
            dstGlyphMetrics.FindPropertyRelative("m_HorizontalBearingX").floatValue = srcGlyphMetrics.FindPropertyRelative("m_HorizontalBearingX").floatValue;
            dstGlyphMetrics.FindPropertyRelative("m_HorizontalBearingY").floatValue = srcGlyphMetrics.FindPropertyRelative("m_HorizontalBearingY").floatValue;
            dstGlyphMetrics.FindPropertyRelative("m_HorizontalAdvance").floatValue = srcGlyphMetrics.FindPropertyRelative("m_HorizontalAdvance").floatValue;

            // Glyph -> GlyphRect
            SerializedProperty srcGlyphRect = srcGlyph.FindPropertyRelative("m_GlyphRect");
            SerializedProperty dstGlyphRect = dstGlyph.FindPropertyRelative("m_GlyphRect");

            dstGlyphRect.FindPropertyRelative("m_X").intValue = srcGlyphRect.FindPropertyRelative("m_X").intValue;
            dstGlyphRect.FindPropertyRelative("m_Y").intValue = srcGlyphRect.FindPropertyRelative("m_Y").intValue;
            dstGlyphRect.FindPropertyRelative("m_Width").intValue = srcGlyphRect.FindPropertyRelative("m_Width").intValue;
            dstGlyphRect.FindPropertyRelative("m_Height").intValue = srcGlyphRect.FindPropertyRelative("m_Height").intValue;

            dstGlyph.FindPropertyRelative("m_Scale").floatValue = srcGlyph.FindPropertyRelative("m_Scale").floatValue;
            dstGlyph.FindPropertyRelative("m_AtlasIndex").intValue = srcGlyph.FindPropertyRelative("m_AtlasIndex").intValue;
        }


        void CopyCharacterSerializedProperty(SerializedProperty source, ref SerializedProperty target)
        {
            // TODO : Should make a generic function which copies each of the properties.
            int unicode = source.FindPropertyRelative("m_Unicode").intValue;
            target.FindPropertyRelative("m_Unicode").intValue = unicode;

            int srcGlyphIndex = source.FindPropertyRelative("m_GlyphIndex").intValue;
            target.FindPropertyRelative("m_GlyphIndex").intValue = srcGlyphIndex;

            target.FindPropertyRelative("m_Scale").floatValue = source.FindPropertyRelative("m_Scale").floatValue;
        }


        /// <summary>
        ///
        /// </summary>
        /// <param name="searchPattern"></param>
        /// <returns></returns>
        void SearchGlyphTable (string searchPattern, ref List<int> searchResults)
        {
            if (searchResults == null) searchResults = new List<int>();

            searchResults.Clear();

            int arraySize = m_GlyphTable_prop.arraySize;

            for (int i = 0; i < arraySize; i++)
            {
                SerializedProperty sourceGlyph = m_GlyphTable_prop.GetArrayElementAtIndex(i);

                int id = sourceGlyph.FindPropertyRelative("m_Index").intValue;

                // Check for potential match against decimal id
                if (id.ToString().Contains(searchPattern))
                    searchResults.Add(i);
            }
        }


        void SearchCharacterTable(string searchPattern, ref List<int> searchResults)
        {
            if (searchResults == null) searchResults = new List<int>();

            searchResults.Clear();

            int arraySize = m_CharacterTable_prop.arraySize;

            for (int i = 0; i < arraySize; i++)
            {
                SerializedProperty sourceCharacter = m_CharacterTable_prop.GetArrayElementAtIndex(i);

                int id = sourceCharacter.FindPropertyRelative("m_Unicode").intValue;

                // Check for potential match against a character.
                if (searchPattern.Length == 1 && id == searchPattern[0])
                    searchResults.Add(i);
                else if (id.ToString("x").Contains(searchPattern))
                    searchResults.Add(i);
                else if (id.ToString("X").Contains(searchPattern))
                    searchResults.Add(i);

                // Check for potential match against decimal id
                //if (id.ToString().Contains(searchPattern))
                //    searchResults.Add(i);
            }
        }
    }
}
