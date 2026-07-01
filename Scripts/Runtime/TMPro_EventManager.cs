#if UNITY_EDITOR
using System;
using Object = UnityEngine.Object;


namespace TMPro
{
    // Notifies live text objects when a font asset is regenerated via the Font Creator / inspector.
    public static class TMPro_EventManager
    {
        public static event Action<bool, Object> FONT_PROPERTY_EVENT;

        public static void ON_FONT_PROPERTY_CHANGED(bool isChanged, Object obj) =>
            FONT_PROPERTY_EVENT?.Invoke(isChanged, obj);
    }
}
#endif