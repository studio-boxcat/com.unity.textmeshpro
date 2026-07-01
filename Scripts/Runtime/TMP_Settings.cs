#pragma warning disable 0649 // Disabled warnings related to serialized fields not assigned in this script but used in the editor.

namespace TMPro
{
    public static class TMP_Settings
    {
        /// <summary>
        /// Controls if Word Wrapping will be enabled on newly created text objects by default.
        /// </summary>
        public const bool enableWordWrapping = true;

        /// <summary>
        /// Controls if Extra Padding is enabled on newly created text objects by default.
        /// </summary>
        public const bool enableExtraPadding = false;

        /// <summary>
        /// The Default Point Size of newly created text objects.
        /// </summary>
        public const float defaultFontSize = 36;
    }
}