using UnityEngine;
using UnityEditor;


namespace TMPro.EditorUtilities
{
    public static class TMP_UIStyleManager
    {
        public static readonly GUIStyle label;
        public static readonly GUIStyle textAreaBoxWindow;
        public static readonly GUIStyle sectionHeader;
        public static readonly GUIStyle centeredLabel;
        public static readonly GUIStyle rightLabel;

        static TMP_UIStyleManager()
        {
            var dark = EditorGUIUtility.isProSkin;
            var sectionHeaderTexture = T("SectionHeader", dark);

            label = new GUIStyle(EditorStyles.label) { richText = true, wordWrap = true, stretchWidth = true };
            textAreaBoxWindow = new GUIStyle(EditorStyles.textArea) { richText = true };

            sectionHeader = new GUIStyle(EditorStyles.label) { fixedHeight = 22, richText = true, border = new RectOffset(9, 9, 0, 0), overflow = new RectOffset(9, 0, 0, 0), padding = new RectOffset(0, 0, 4, 0) };
            sectionHeader.normal.background = sectionHeaderTexture;

            centeredLabel = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
            rightLabel = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight, richText = true };

            return;

            static Texture2D T(string name, bool dark)
            {
                var suffix = dark ? ".psd" : "_Light.psd";
                return (Texture2D) AssetDatabase.LoadAssetAtPath("Packages/com.unity.textmeshpro/Editor Resources/Textures/" + name + suffix, typeof(Texture));
            }
        }
    }
}
