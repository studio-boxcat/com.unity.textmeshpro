using UnityEngine;
using UnityEngine.AddressableAssets;


#pragma warning disable 0649 // Disabled warnings related to serialized fields not assigned in this script but used in the editor.

namespace TMPro
{
    [System.Serializable] [ExcludeFromPreset]
    public class TMP_Settings : ScriptableObject
    {
        private static TMP_Settings s_Instance;

        /// <summary>
        /// Get a singleton instance of the settings class.
        /// </summary>
        public static TMP_Settings instance => s_Instance ??= Addressables.LoadAsset<TMP_Settings>("TMP Settings");

        /// <summary>
        /// Controls if Word Wrapping will be enabled on newly created text objects by default.
        /// </summary>
        public static bool enableWordWrapping => instance.m_enableWordWrapping;
        [SerializeField]
        private bool m_enableWordWrapping;

        /// <summary>
        /// Controls if Kerning is enabled on newly created text objects by default.
        /// </summary>
        public static bool enableKerning => instance.m_enableKerning;
        [SerializeField]
        private bool m_enableKerning;

        /// <summary>
        /// Controls if Extra Padding is enabled on newly created text objects by default.
        /// </summary>
        public static bool enableExtraPadding => instance.m_enableExtraPadding;
        [SerializeField]
        private bool m_enableExtraPadding;

        /// <summary>
        /// Controls if Raycast Target is enabled by default on newly created text objects.
        /// </summary>
        public static bool enableRaycastTarget => instance.m_EnableRaycastTarget;
        [SerializeField]
        private bool m_EnableRaycastTarget = true;

        /// <summary>
        /// The Default Point Size of newly created text objects.
        /// </summary>
        public static float defaultFontSize => instance.m_defaultFontSize;
        [SerializeField]
        private float m_defaultFontSize;

        /// <summary>
        /// The multiplier used to computer the default Min point size when Text Auto Sizing is used.
        /// </summary>
        public static float defaultTextAutoSizingMinRatio => instance.m_defaultAutoSizeMinRatio;
        [SerializeField]
        private float m_defaultAutoSizeMinRatio;

        /// <summary>
        /// The multiplier used to computer the default Max point size when Text Auto Sizing is used.
        /// </summary>
        public static float defaultTextAutoSizingMaxRatio => instance.m_defaultAutoSizeMaxRatio;
        [SerializeField]
        private float m_defaultAutoSizeMaxRatio;

        /// <summary>
        /// The Default Size of the Text Container of a TextMeshPro object.
        /// </summary>
        public static Vector2 defaultTextMeshProTextContainerSize => instance.m_defaultTextMeshProTextContainerSize;
        [SerializeField]
        private Vector2 m_defaultTextMeshProTextContainerSize;

        /// <summary>
        /// The Default Width of the Text Container of a TextMeshProUI object.
        /// </summary>
        public static Vector2 defaultTextMeshProUITextContainerSize => instance.m_defaultTextMeshProUITextContainerSize;
        [SerializeField]
        private Vector2 m_defaultTextMeshProUITextContainerSize;

        /// <summary>
        /// Disables InternalUpdate() calls when true. This can improve performance when the scale of the text object is static.
        /// </summary>
        public static bool isTextObjectScaleStatic
        {
            get => instance.m_IsTextObjectScaleStatic;
            set => instance.m_IsTextObjectScaleStatic = value;
        }
        [SerializeField]
        private bool m_IsTextObjectScaleStatic;
    }
}