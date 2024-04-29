using UnityEngine;
using UnityEditor;

namespace TMPro.EditorUtilities
{
    [CustomEditor(typeof(TMP_SubMeshUI)), CanEditMultipleObjects]
    public class TMP_SubMeshUI_Editor : Editor
    {
        SerializedProperty fontAsset_prop;


        public void OnEnable()
        {
            fontAsset_prop = serializedObject.FindProperty("m_fontAsset");
        }


        public override void OnInspectorGUI()
        {
            GUI.enabled = false;
            EditorGUILayout.PropertyField(fontAsset_prop);
            GUI.enabled = true;

            EditorGUILayout.Space();
        }

    }
}
