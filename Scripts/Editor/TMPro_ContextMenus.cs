using UnityEngine;
using UnityEditor;
using System.IO;


namespace TMPro.EditorUtilities
{

    public class TMP_ContextMenus : Editor
    {
        private static Texture m_copiedTexture;

        private static Material m_copiedProperties;
        private static Material m_copiedAtlasProperties;


        // Add a Context Menu to allow easy duplication of the Material.
        [MenuItem("CONTEXT/Material/Create Material Preset", false)]
        static void DuplicateMaterial(MenuCommand command)
        {
            // Get the type of text object
            // If material is not a base material, we get material leaks...

            Material source_Mat = (Material)command.context;
            if (!EditorUtility.IsPersistent(source_Mat))
            {
                Debug.LogWarning("Material is an instance and cannot be converted into a persistent asset.");
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(source_Mat).Split('.')[0];

            if (assetPath.IndexOf("Assets/", System.StringComparison.InvariantCultureIgnoreCase) == -1)
            {
                Debug.LogWarning("Material Preset cannot be created from a material that is located outside the project.");
                return;
            }

            Material duplicate = new Material(source_Mat);

            // Need to manually copy the shader keywords
            duplicate.shaderKeywords = source_Mat.shaderKeywords;

            AssetDatabase.CreateAsset(duplicate, AssetDatabase.GenerateUniqueAssetPath(assetPath + ".mat"));

            GameObject[] selectedObjects = Selection.gameObjects;

            // Assign new Material Preset to selected text objects.
            for (int i = 0; i < selectedObjects.Length; i++)
            {
                TMP_Text textObject = selectedObjects[i].GetComponent<TMP_Text>();

                if (textObject != null)
                {
                    textObject.fontSharedMaterial = duplicate;
                }
                else
                {
                    TMP_SubMeshUI subMeshUIObject = selectedObjects[i].GetComponent<TMP_SubMeshUI>();

                    if (subMeshUIObject != null)
                        subMeshUIObject.sharedMaterial = duplicate;
                }
            }

            // Ping newly created Material Preset.
            EditorUtility.FocusProjectWindow();
            EditorGUIUtility.PingObject(duplicate);
        }

        // Context Menus for TMPro Font Assets
        //This function is used for debugging and fixing potentially broken font atlas links.
        [MenuItem("CONTEXT/TMP_FontAsset/Extract Atlas", false, 2100)]
        static void ExtractAtlas(MenuCommand command)
        {
            TMP_FontAsset font = command.context as TMP_FontAsset;

            string fontPath = AssetDatabase.GetAssetPath(font);
            string texPath = Path.GetDirectoryName(fontPath) + "/" + Path.GetFileNameWithoutExtension(fontPath) + " Atlas.png";

            // Create a Serialized Object of the texture to allow us to make it readable.
            SerializedObject texprop = new SerializedObject(font.material.GetTexture(ShaderUtilities.ID_MainTex));
            texprop.FindProperty("m_IsReadable").boolValue = true;
            texprop.ApplyModifiedProperties();

            // Create a copy of the texture.
            Texture2D tex = Instantiate(font.material.GetTexture(ShaderUtilities.ID_MainTex)) as Texture2D;

            // Set the texture to not readable again.
            texprop.FindProperty("m_IsReadable").boolValue = false;
            texprop.ApplyModifiedProperties();

            Debug.Log(texPath);
            // Saving File for Debug
            var pngData = tex.EncodeToPNG();
            File.WriteAllBytes(texPath, pngData);

            AssetDatabase.Refresh();
            DestroyImmediate(tex);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="command"></param>
        [MenuItem("CONTEXT/TMP_FontAsset/Update Atlas Texture...", false, 2000)]
        static void RegenerateFontAsset(MenuCommand command)
        {
            TMP_FontAsset fontAsset = command.context as TMP_FontAsset;

            if (fontAsset != null)
            {
                TMPro_FontAssetCreatorWindow.ShowFontAtlasCreatorWindow(fontAsset);
            }
        }

        /// <summary>
        /// Clear Dynamic Font Asset data such as glyph, character and font features.
        /// </summary>
        /// <param name="command"></param>
        [MenuItem("CONTEXT/TMP_FontAsset/Reset", true, 100)]
        static bool ClearFontAssetDataValidate(MenuCommand command)
        {
            return AssetDatabase.IsOpenForEdit(command.context);
        }

        [MenuItem("CONTEXT/TMP_FontAsset/Reset", false, 100)]
        static void ClearFontAssetData(MenuCommand command)
        {
            TMP_FontAsset fontAsset = command.context as TMP_FontAsset;

            if (fontAsset != null && Selection.activeObject != fontAsset)
            {
                Selection.activeObject = fontAsset;
            }

            fontAsset.ClearFontAssetData(true);

            TMPro_EventManager.ON_FONT_PROPERTY_CHANGED(true, fontAsset);
        }


        [MenuItem("CONTEXT/TrueTypeFontImporter/Create TMP Font Asset...", false, 200)]
        static void CreateFontAsset(MenuCommand command)
        {
            TrueTypeFontImporter importer = command.context as TrueTypeFontImporter;

            if (importer != null)
            {
                Font sourceFontFile = AssetDatabase.LoadAssetAtPath<Font>(importer.assetPath);

                if (sourceFontFile)
                    TMPro_FontAssetCreatorWindow.ShowFontAtlasCreatorWindow(sourceFontFile);
            }
        }
    }
}
