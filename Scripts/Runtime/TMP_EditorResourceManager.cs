#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace TMPro
{
    public class TMP_EditorResourceManager
    {
        private static TMP_EditorResourceManager s_Instance;

        private readonly List<Object> m_ObjectUpdateQueue = new List<Object>();
        private HashSet<int> m_ObjectUpdateQueueLookup = new HashSet<int>();

        private readonly List<Object> m_ObjectReImportQueue = new List<Object>();
        private HashSet<int> m_ObjectReImportQueueLookup = new HashSet<int>();

        /// <summary>
        /// Get a singleton instance of the manager.
        /// </summary>
        public static TMP_EditorResourceManager instance => s_Instance ??= new TMP_EditorResourceManager();

        /// <summary>
        /// Register to receive rendering callbacks.
        /// </summary>
        private TMP_EditorResourceManager()
        {
            Camera.onPostRender += OnCameraPostRender;
        }

        void OnCameraPostRender(Camera cam)
        {
            // Exclude the PreRenderCamera
            if (cam.cameraType != CameraType.SceneView)
                return;

            DoPostRenderUpdates();
        }

        /// <summary>
        /// Register resource for re-import.
        /// </summary>
        /// <param name="obj"></param>
        internal static void RegisterResourceForReimport(Object obj)
        {
            instance.InternalRegisterResourceForReimport(obj);
        }

        private void InternalRegisterResourceForReimport(Object obj)
        {
            if (m_ObjectReImportQueueLookup.Add(obj.GetInstanceID()))
                m_ObjectReImportQueue.Add(obj);
        }

        /// <summary>
        /// Register resource to be updated.
        /// </summary>
        /// <param name="textObject"></param>
        internal static void RegisterResourceForUpdate(Object obj)
        {
            instance.InternalRegisterResourceForUpdate(obj);
        }

        private void InternalRegisterResourceForUpdate(Object obj)
        {
            if (m_ObjectUpdateQueueLookup.Add(obj.GetInstanceID()))
                m_ObjectUpdateQueue.Add(obj);
        }

        void DoPostRenderUpdates()
        {
            // Handle objects that need updating
            int objUpdateCount = m_ObjectUpdateQueue.Count;

            for (int i = 0; i < objUpdateCount; i++)
            {
                Object obj = m_ObjectUpdateQueue[i];
                if (obj != null)
                {
                    EditorUtility.SetDirty(obj);
                }
            }

            if (objUpdateCount > 0)
            {
                //Debug.Log("Saving assets");
                //AssetDatabase.SaveAssets();

                m_ObjectUpdateQueue.Clear();
                m_ObjectUpdateQueueLookup.Clear();
            }

            // Handle objects that need re-importing
            int objReImportCount = m_ObjectReImportQueue.Count;

            for (int i = 0; i < objReImportCount; i++)
            {
                Object obj = m_ObjectReImportQueue[i];
                if (obj != null)
                {
                    AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(obj));
                }
            }

            if (objReImportCount > 0)
            {
                m_ObjectReImportQueue.Clear();
                m_ObjectReImportQueueLookup.Clear();
            }
        }
    }
}
#endif
