// ReSharper disable InconsistentNaming

#nullable enable
using UnityEngine;
using System.Collections.Generic;


namespace TMPro
{
    internal static class TMP_UpdateManager
    {
        private static readonly Dictionary<int, TMP_Text> _entries = new();

        internal static void Register(TMP_Text textObject)
        {
            if (_entries.IsEmpty())
            {
                _update ??= Update;
                Canvas.willRenderCanvases += _update;
            }

            _entries[textObject.GetInstanceID()] = textObject;
        }

        internal static void Unregister(TMP_Text textObject)
        {
            if (_entries.Remove(textObject.GetInstanceID()) && _entries.IsEmpty())
                Canvas.willRenderCanvases -= _update;
        }

        private static Canvas.WillRenderCanvases? _update;

        private static void Update()
        {
            // Handle text objects the require an update either as a result of scale changes.
            foreach (var entry in _entries.Values)
                entry.InternalUpdate();
        }
    }
}