﻿namespace TMPro
{
    /// <summary>
    /// Defines the type of value used by a rich text tag or tag attribute.
    /// </summary>
    public enum TagValueType
    {
        None = 0x0,
        NumericalValue = 0x1,
        StringValue = 0x2,
        ColorValue = 0x4,
    }

    public enum TagUnitType
    {
        Pixels = 0x0,
        FontUnits = 0x1,
        Percentage = 0x2,
    }

    /// <summary>
    /// Commonly referenced Unicode characters in the text generation process.
    /// </summary>
    internal static class CodePoint
    {
        public const uint HIGH_SURROGATE_START = 0xD800;
        public const uint HIGH_SURROGATE_END = 0xDBFF;
        public const uint LOW_SURROGATE_START = 0xDC00;
        public const uint LOW_SURROGATE_END = 0xDFFF;
        public const uint UNICODE_PLANE01_START = 0x10000;
    }
}
