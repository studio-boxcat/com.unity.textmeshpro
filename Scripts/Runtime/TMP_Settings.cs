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
        /// Controls if Kerning is enabled on newly created text objects by default.
        /// </summary>
        public const bool enableKerning = true;

        /// <summary>
        /// Controls if Extra Padding is enabled on newly created text objects by default.
        /// </summary>
        public const bool enableExtraPadding = false;

        /// <summary>
        /// The Default Point Size of newly created text objects.
        /// </summary>
        public const float defaultFontSize = 36;

        /// <summary>
        /// The multiplier used to computer the default Min point size when Text Auto Sizing is used.
        /// </summary>
        public const float defaultTextAutoSizingMinRatio = 0.5f;

        /// <summary>
        /// The multiplier used to computer the default Max point size when Text Auto Sizing is used.
        /// </summary>
        public const float defaultTextAutoSizingMaxRatio = 2;

        /// <summary>
        /// Disables InternalUpdate() calls when true. This can improve performance when the scale of the text object is static.
        /// </summary>
        public const bool isTextObjectScaleStatic = false;
    }
}