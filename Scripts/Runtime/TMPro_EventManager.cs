#if UNITY_EDITOR
using System;
using UnityEngine;
using Object = UnityEngine.Object;


namespace TMPro
{
    public static class TMPro_EventManager
    {
        public static event Action<bool, Material> MATERIAL_PROPERTY_EVENT;
        public static event Action<bool, Object> FONT_PROPERTY_EVENT;
        public static event Action<bool, Object> TEXTMESHPRO_UGUI_PROPERTY_EVENT;

        public static void ON_MATERIAL_PROPERTY_CHANGED(bool isChanged, Material mat) =>
            MATERIAL_PROPERTY_EVENT?.Invoke(isChanged, mat);
        public static void ON_FONT_PROPERTY_CHANGED(bool isChanged, Object obj) =>
            FONT_PROPERTY_EVENT?.Invoke(isChanged, obj);
        public static void ON_TEXTMESHPRO_UGUI_PROPERTY_CHANGED(bool isChanged, Object obj) =>
            TEXTMESHPRO_UGUI_PROPERTY_EVENT?.Invoke(isChanged, obj);
    }
}
#endif