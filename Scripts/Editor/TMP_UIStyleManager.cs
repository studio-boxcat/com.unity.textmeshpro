using UnityEngine;
using UnityEditor;


namespace TMPro.EditorUtilities
{
    public static class TMP_UIStyleManager
    {
        public static readonly GUIStyle label;
        public static readonly GUIStyle textAreaBoxWindow;
        public static readonly GUIStyle panelTitle;
        public static readonly GUIStyle sectionHeader;
        public static readonly GUIStyle centeredLabel;
        public static readonly GUIStyle rightLabel;

        public static readonly GUIStyle alignmentButtonLeft;
        public static readonly GUIStyle alignmentButtonMid;
        public static readonly GUIStyle alignmentButtonRight;

        public static readonly GUIContent[] alignContentA;
        public static readonly GUIContent[] alignContentB;

        static TMP_UIStyleManager()
        {
            var dark = EditorGUIUtility.isProSkin;
            var alignLeft = T("btn_AlignLeft", dark);
            var alignCenter = T("btn_AlignCenter", dark);
            var alignRight = T("btn_AlignRight", dark);
            var alignTop = T("btn_AlignTop", dark);
            var alignMiddle = T("btn_AlignMiddle", dark);
            var alignBottom = T("btn_AlignBottom", dark);
            var alignBaseline = T("btn_AlignBaseLine", dark);
            var alignMidline = T("btn_AlignMidLine", dark);
            var alignCapline = T("btn_AlignCapLine", dark);
            var sectionHeaderTexture = T("SectionHeader", dark);

            label = new GUIStyle(EditorStyles.label) { richText = true, wordWrap = true, stretchWidth = true };
            textAreaBoxWindow = new GUIStyle(EditorStyles.textArea) { richText = true };
            panelTitle = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold };

            sectionHeader = new GUIStyle(EditorStyles.label) { fixedHeight = 22, richText = true, border = new RectOffset(9, 9, 0, 0), overflow = new RectOffset(9, 0, 0, 0), padding = new RectOffset(0, 0, 4, 0) };
            sectionHeader.normal.background = sectionHeaderTexture;

            centeredLabel = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
            rightLabel = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight, richText = true };


            alignmentButtonLeft = new GUIStyle(EditorStyles.miniButtonLeft);
            alignmentButtonLeft.padding.left = 4;
            alignmentButtonLeft.padding.right = 4;
            alignmentButtonLeft.padding.top = 2;
            alignmentButtonLeft.padding.bottom = 2;

            alignmentButtonMid = new GUIStyle(EditorStyles.miniButtonMid);
            alignmentButtonMid.padding.left = 4;
            alignmentButtonMid.padding.right = 4;
            alignmentButtonLeft.padding.top = 2;
            alignmentButtonLeft.padding.bottom = 2;

            alignmentButtonRight = new GUIStyle(EditorStyles.miniButtonRight);
            alignmentButtonRight.padding.left = 4;
            alignmentButtonRight.padding.right = 4;
            alignmentButtonLeft.padding.top = 2;
            alignmentButtonLeft.padding.bottom = 2;

            alignContentA = new[]
            {
                new GUIContent(alignLeft, "Left"),
                new GUIContent(alignCenter, "Center"),
                new GUIContent(alignRight, "Right"),
            };

            alignContentB = new[]
            {
                new GUIContent(alignTop, "Top"),
                new GUIContent(alignMiddle, "Middle"),
                new GUIContent(alignBottom, "Bottom"),
                new GUIContent(alignBaseline, "Baseline"),
                new GUIContent(alignMidline, "Midline"),
                new GUIContent(alignCapline, "Capline")
            };

            return;

            static Texture2D T(string name, bool dark)
            {
                var suffix = dark ? ".psd" : "_Light.psd";
                return (Texture2D) AssetDatabase.LoadAssetAtPath("Packages/com.unity.textmeshpro/Editor Resources/Textures/" + name + suffix, typeof(Texture));
            }
        }
    }
}