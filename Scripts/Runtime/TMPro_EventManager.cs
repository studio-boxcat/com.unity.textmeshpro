using UnityEngine;


namespace TMPro
{
    public static class TMPro_EventManager
    {
        public static readonly FastAction<bool, Material> MATERIAL_PROPERTY_EVENT = new FastAction<bool, Material>();

        public static readonly FastAction<bool, Object> FONT_PROPERTY_EVENT = new FastAction<bool, Object>();

        public static readonly FastAction<bool, Object> TEXTMESHPRO_PROPERTY_EVENT = new FastAction<bool, Object>();

        public static readonly FastAction<GameObject, Material, Material> DRAG_AND_DROP_MATERIAL_EVENT = new FastAction<GameObject, Material, Material>();

        public static readonly FastAction<bool, Object> TEXTMESHPRO_UGUI_PROPERTY_EVENT = new FastAction<bool, Object>();

        public static void ON_MATERIAL_PROPERTY_CHANGED(bool isChanged, Material mat)
        {
            MATERIAL_PROPERTY_EVENT.Call(isChanged, mat);
        }

        public static void ON_FONT_PROPERTY_CHANGED(bool isChanged, Object obj)
        {
            FONT_PROPERTY_EVENT.Call(isChanged, obj);
        }

        public static void ON_TEXTMESHPRO_PROPERTY_CHANGED(bool isChanged, Object obj)
        {
            TEXTMESHPRO_PROPERTY_EVENT.Call(isChanged, obj);
        }

        public static void ON_DRAG_AND_DROP_MATERIAL_CHANGED(GameObject sender, Material currentMaterial, Material newMaterial)
        {
            DRAG_AND_DROP_MATERIAL_EVENT.Call(sender, currentMaterial, newMaterial);
        }

        public static void ON_TEXTMESHPRO_UGUI_PROPERTY_CHANGED(bool isChanged, Object obj)
        {
            TEXTMESHPRO_UGUI_PROPERTY_EVENT.Call(isChanged, obj);
        }
    }
}
