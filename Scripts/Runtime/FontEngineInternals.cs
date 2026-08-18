using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;

namespace TMPro
{
    // Reflection bridge to the font-engine natives the atlas paths need. Unity 6.3 pruned both
    // TextCoreFontEngineModule [InternalsVisibleTo] lists down to the Unity.TextCore* /
    // Unity.TextMeshPro* assemblies; this fork compiles into Unity.Analytics /
    // UnityEditor.Purchasing (Assembly.asmref), so none of these are reachable at compile time
    // and none has a public equivalent. Keeping them here means the package's whole
    // internal-access surface is one auditable file.
    //
    // Every parameter and return type below is public TextCore API — only the members are
    // internal — so the call sites stay typed and unchanged apart from the receiver.
    internal static class FontEngineInternals
    {
        private const string _fontEngine =
            "UnityEngine.TextCore.LowLevel.FontEngine, UnityEngine.TextCoreFontEngineModule";

        private static readonly MethodInfo _generationProgressGetter =
            ResolveProperty(_fontEngine, "generationProgress").GetMethod!;

        private static readonly MethodInfo _sendCancellationRequest =
            ResolveMethod(_fontEngine, "SendCancellationRequest", Type.EmptyTypes);

        private static readonly MethodInfo _resetAtlasTexture =
            ResolveMethod(_fontEngine, "ResetAtlasTexture", new[] { typeof(Texture2D) });

        private static readonly MethodInfo _tryPackGlyphsInAtlas = ResolveMethod(
            _fontEngine, "TryPackGlyphsInAtlas",
            new[]
            {
                typeof(List<Glyph>), typeof(List<Glyph>), typeof(int), typeof(GlyphPackingMode),
                typeof(GlyphRenderMode), typeof(int), typeof(int), typeof(List<GlyphRect>),
                typeof(List<GlyphRect>),
            });

        private static readonly MethodInfo _renderGlyphsToTexture = ResolveMethod(
            _fontEngine, "RenderGlyphsToTexture",
            new[]
            {
                typeof(List<Glyph>), typeof(int), typeof(GlyphRenderMode), typeof(byte[]),
                typeof(int), typeof(int),
            });

        /// Progress of the in-flight glyph generation, 0..1.
        internal static float generationProgress => (float) _generationProgressGetter.Invoke(null, null);

        internal static void SendCancellationRequest()
        {
            _sendCancellationRequest.Invoke(null, null);
        }

        /// Zero the texture's pixel data in place. Caller applies.
        internal static void ResetAtlasTexture(Texture2D texture)
        {
            _resetAtlasTexture.Invoke(null, new object[] { texture });
        }

        internal static bool TryPackGlyphsInAtlas(
            List<Glyph> glyphsToAdd, List<Glyph> glyphsAdded, int padding, GlyphPackingMode packingMode,
            GlyphRenderMode renderMode, int width, int height, List<GlyphRect> freeGlyphRects,
            List<GlyphRect> usedGlyphRects)
        {
            return (bool) _tryPackGlyphsInAtlas.Invoke(null, new object[]
            {
                glyphsToAdd, glyphsAdded, padding, packingMode, renderMode, width, height,
                freeGlyphRects, usedGlyphRects,
            });
        }

        internal static FontEngineError RenderGlyphsToTexture(
            List<Glyph> glyphs, int padding, GlyphRenderMode renderMode, byte[] texBuffer,
            int texWidth, int texHeight)
        {
            return (FontEngineError) _renderGlyphsToTexture.Invoke(null, new object[]
            {
                glyphs, padding, renderMode, texBuffer, texWidth, texHeight,
            });
        }

#if UNITY_EDITOR
        private static readonly MethodInfo _setAtlasTextureIsReadable = ResolveMethod(
            "UnityEditor.TextCore.LowLevel.FontEngineEditorUtilities, UnityEditor.TextCoreFontEngineModule",
            "SetAtlasTextureIsReadable", new[] { typeof(Texture2D), typeof(bool) });

        /// Flip the texture's native readability flag without a re-import.
        internal static void SetAtlasTextureIsReadable(Texture2D texture, bool isReadable)
        {
            _setAtlasTextureIsReadable.Invoke(null, new object[] { texture, isReadable });
        }
#endif

        private const BindingFlags _statics =
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

        private static MethodInfo ResolveMethod(string assemblyQualifiedType, string name, Type[] parameters)
        {
            return ResolveType(assemblyQualifiedType).GetMethod(name, _statics, null, parameters, null)
                   ?? throw new MissingMethodException(assemblyQualifiedType, name);
        }

        private static PropertyInfo ResolveProperty(string assemblyQualifiedType, string name)
        {
            return ResolveType(assemblyQualifiedType).GetProperty(name, _statics)
                   ?? throw new MissingMemberException(assemblyQualifiedType, name);
        }

        private static Type ResolveType(string assemblyQualifiedType)
        {
            return Type.GetType(assemblyQualifiedType, throwOnError: true)!;
        }
    }
}
