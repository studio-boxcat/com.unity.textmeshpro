using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.TextCore;


namespace TMPro.EditorUtilities
{

    public static class TMP_EditorUtility
    {
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

        public static int GetPointSize(this FaceInfo faceInfo)
        {
            var sizeF = faceInfo.pointSize;
            var sizeI = Mathf.RoundToInt(sizeF);
            if (Mathf.Abs(sizeF - sizeI) > 0.01f)
                Debug.LogError($"Font Point Size of {faceInfo.familyName} is not an integer: {sizeF}");
            return sizeI;
        }
    }
}
