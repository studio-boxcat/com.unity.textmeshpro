using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;


namespace TMPro.EditorUtilities
{

    public static class TMP_EditorUtility
    {
        /// <summary>
        /// Returns the relative path of the package.
        /// </summary>
        public static string packageRelativePath => m_PackagePath ??= GetPackageRelativePath();
        private static string m_PackagePath;

        // Static Fields Related to locating the TextMesh Pro Asset
        private static string folderPath = "Not Found";
        

        // Function used to find all materials which reference a font atlas so we can update all their references.
        public static Material[] FindMaterialReferences(TMP_FontAsset fontAsset)
        {
            List<Material> refs = new List<Material>();
            Material mat = fontAsset.material;
            refs.Add(mat);

            // Get materials matching the search pattern.
            string searchPattern = "t:Material" + " " + fontAsset.name.Split(new char[] { ' ' })[0];
            string[] materialAssetGUIDs = AssetDatabase.FindAssets(searchPattern);

            for (int i = 0; i < materialAssetGUIDs.Length; i++)
            {
                string materialPath = AssetDatabase.GUIDToAssetPath(materialAssetGUIDs[i]);
                Material targetMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

                if (targetMaterial.HasProperty(ShaderUtilities.ID_MainTex) && targetMaterial.GetTexture(ShaderUtilities.ID_MainTex) != null && mat.GetTexture(ShaderUtilities.ID_MainTex) != null && targetMaterial.GetTexture(ShaderUtilities.ID_MainTex).GetInstanceID() == mat.GetTexture(ShaderUtilities.ID_MainTex).GetInstanceID())
                {
                    if (!refs.Contains(targetMaterial))
                        refs.Add(targetMaterial);
                }
            }

            return refs.ToArray();
        }


        // Function used to find the Font Asset which matches the given Material Preset and Font Atlas Texture.
        public static TMP_FontAsset FindMatchingFontAsset(Material mat)
        {
            if (mat.GetTexture(ShaderUtilities.ID_MainTex) == null) return null;

            // Find the dependent assets of this material.
            string[] dependentAssets = AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(mat), false);

            for (int i = 0; i < dependentAssets.Length; i++)
            {
                TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(dependentAssets[i]);
                if (fontAsset != null)
                    return fontAsset;
            }

            return null;
        }


        private static string GetPackageRelativePath()
        {
            // Check for potential UPM package
            string packagePath = Path.GetFullPath("Packages/com.unity.textmeshpro");
            if (Directory.Exists(packagePath))
                return "Packages/com.unity.textmeshpro";

            packagePath = Path.GetFullPath("Assets/..");
            if (Directory.Exists(packagePath))
            {
                // Search default location for development package
                if (Directory.Exists(packagePath + "/Assets/Packages/com.unity.TextMeshPro/Editor Resources"))
                {
                    return "Assets/Packages/com.unity.TextMeshPro";
                }

                // Search for default location of normal TextMesh Pro AssetStore package
                if (Directory.Exists(packagePath + "/Assets/TextMesh Pro/Editor Resources"))
                {
                    return "Assets/TextMesh Pro";
                }

                // Search for potential alternative locations in the user project
                string[] matchingPaths = Directory.GetDirectories(packagePath, "TextMesh Pro", SearchOption.AllDirectories);
                packagePath = ValidateLocation(matchingPaths, packagePath);
                if (packagePath != null) return packagePath;
            }

            return null;
        }

        /// <summary>
        /// Method to validate the location of the asset folder by making sure the GUISkins folder exists.
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        private static string ValidateLocation(string[] paths, string projectPath)
        {
            for (int i = 0; i < paths.Length; i++)
            {
                // Check if any of the matching directories contain a GUISkins directory.
                if (Directory.Exists(paths[i] + "/Editor Resources"))
                {
                    folderPath = paths[i].Replace(projectPath, "");
                    folderPath = folderPath.TrimStart('\\', '/');
                    return folderPath;
                }
            }

            return null;
        }


        /// <summary>
        /// Function which returns a string containing a sequence of Decimal character ranges.
        /// </summary>
        /// <param name="characterSet"></param>
        /// <returns></returns>
        public static string GetDecimalCharacterSequence(int[] characterSet)
        {
            if (characterSet == null || characterSet.Length == 0)
                return string.Empty;

            string characterSequence = string.Empty;
            int count = characterSet.Length;
            int first = characterSet[0];
            int last = first;

            for (int i = 1; i < count; i++)
            {
                if (characterSet[i - 1] + 1 == characterSet[i])
                {
                    last = characterSet[i];
                }
                else
                {
                    if (first == last)
                        characterSequence += first + ",";
                    else
                        characterSequence += first + "-" + last + ",";

                    first = last = characterSet[i];
                }

            }

            // handle the final group
            if (first == last)
                characterSequence += first;
            else
                characterSequence += first + "-" + last;

            return characterSequence;
        }


        /// <summary>
        /// Function which returns a string containing a sequence of Unicode (Hex) character ranges.
        /// </summary>
        /// <param name="characterSet"></param>
        /// <returns></returns>
        public static string GetUnicodeCharacterSequence(int[] characterSet)
        {
            if (characterSet == null || characterSet.Length == 0)
                return string.Empty;

            string characterSequence = string.Empty;
            int count = characterSet.Length;
            int first = characterSet[0];
            int last = first;

            for (int i = 1; i < count; i++)
            {
                if (characterSet[i - 1] + 1 == characterSet[i])
                {
                    last = characterSet[i];
                }
                else
                {
                    if (first == last)
                        characterSequence += first.ToString("X2") + ",";
                    else
                        characterSequence += first.ToString("X2") + "-" + last.ToString("X2") + ",";

                    first = last = characterSet[i];
                }

            }

            // handle the final group
            if (first == last)
                characterSequence += first.ToString("X2");
            else
                characterSequence += first.ToString("X2") + "-" + last.ToString("X2");

            return characterSequence;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="thickness"></param>
        /// <param name="color"></param>
        public static void DrawBox(Rect rect, float thickness, Color color)
        {
            EditorGUI.DrawRect(new Rect(rect.x - thickness, rect.y + thickness, rect.width + thickness * 2, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x - thickness, rect.y + thickness, thickness, rect.height - thickness * 2), color);
            EditorGUI.DrawRect(new Rect(rect.x - thickness, rect.y + rect.height - thickness * 2, rect.width + thickness * 2, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x + rect.width, rect.y + thickness, thickness, rect.height - thickness * 2), color);
        }


        /// <summary>
        /// Function to return the horizontal alignment grid value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int GetHorizontalAlignmentGridValue(int value)
        {
            if ((value & 0x1) == 0x1)
                return 0;
            else if ((value & 0x2) == 0x2)
                return 1;
            else if ((value & 0x4) == 0x4)
                return 2;
            else if ((value & 0x8) == 0x8)
                return 3;
            else if ((value & 0x10) == 0x10)
                return 4;
            else if ((value & 0x20) == 0x20)
                return 5;

            return 0;
        }

        /// <summary>
        /// Function to return the vertical alignment grid value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int GetVerticalAlignmentGridValue(int value)
        {
            if ((value & 0x100) == 0x100)
                return 0;
            if ((value & 0x200) == 0x200)
                return 1;
            if ((value & 0x400) == 0x400)
                return 2;
            if ((value & 0x800) == 0x800)
                return 3;
            if ((value & 0x1000) == 0x1000)
                return 4;
            if ((value & 0x2000) == 0x2000)
                return 5;

            return 0;
        }

        public static bool EditorToggle(Rect position, bool value, GUIContent content, GUIStyle style)
        {
            var id = GUIUtility.GetControlID(content, FocusType.Keyboard, position);
            var evt = Event.current;

            // Toggle selected toggle on space or return key
            if (GUIUtility.keyboardControl == id && evt.type == EventType.KeyDown && (evt.keyCode == KeyCode.Space || evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter))
            {
                value = !value;
                evt.Use();
                GUI.changed = true;
            }

            if (evt.type == EventType.MouseDown && position.Contains(Event.current.mousePosition))
            {
                GUIUtility.keyboardControl = id;
                EditorGUIUtility.editingTextField = false;
                HandleUtility.Repaint();
            }
            
            return GUI.Toggle(position, id, value, content, style);
        }

    }
}
