namespace TMPro
{
    // Mirror of the internal UnityEngine.TextCore.LowLevel.GlyphRasterModes. Unity 6.3
    // pruned UnityEngine.TextCoreFontEngineModule's [InternalsVisibleTo] list down to the
    // Unity.TextCore* / Unity.TextMeshPro* assemblies, and this fork compiles into
    // Unity.Analytics / UnityEditor.Purchasing (Assembly.asmref) instead. The values mirror
    // the native GlyphRasterModes bitfield, so a local copy costs nothing and unqualified
    // uses across TMPro.* resolve here ahead of the file-scope `using`.
    internal enum GlyphRasterModes
    {
        RASTER_MODE_8BIT = 0x1,
        RASTER_MODE_MONO = 0x2,
        RASTER_MODE_NO_HINTING = 0x4,
        RASTER_MODE_HINTED = 0x8,
        RASTER_MODE_BITMAP = 0x10,
        RASTER_MODE_SDF = 0x20,
        RASTER_MODE_SDFAA = 0x40,
        RASTER_MODE_STRONG = 0x80,
        RASTER_MODE_MSDF = 0x100,
        RASTER_MODE_MSDFA = 0x200,
        RASTER_MODE_1X = 0x1000,
        RASTER_MODE_8X = 0x2000,
        RASTER_MODE_16X = 0x4000,
        RASTER_MODE_32X = 0x8000,
        RASTER_MODE_COLOR = 0x10000,
    }
}
