// ReSharper disable InconsistentNaming
using UnityEngine;
using Unity.Profiling;
using System.Collections.Generic;


namespace TMPro
{

    public class TMP_UpdateManager
    {
        private static TMP_UpdateManager s_Instance;
        static TMP_UpdateManager instance => s_Instance ??= new TMP_UpdateManager();

        private readonly HashSet<int> m_InternalUpdateLookup = new HashSet<int>();
        private readonly List<TMP_Text> m_InternalUpdateQueue = new List<TMP_Text>();

        // Profiler Marker declarations
        private static ProfilerMarker k_RegisterTextObjectForUpdateMarker = new ProfilerMarker("TMP.RegisterTextObjectForUpdate");
        private static ProfilerMarker k_UnregisterTextObjectForUpdateMarker = new ProfilerMarker("TMP.UnregisterTextObjectForUpdate");

        /// <summary>
        /// Register to receive rendering callbacks.
        /// </summary>
        TMP_UpdateManager()
        {
            Canvas.willRenderCanvases += DoRebuilds;
        }

        /// <summary>
        /// Function used as a replacement for LateUpdate() to handle SDF Scale updates and Legacy Animation updates.
        /// </summary>
        /// <param name="textObject"></param>
        internal static void RegisterTextObjectForUpdate(TMP_Text textObject)
        {
            k_RegisterTextObjectForUpdateMarker.Begin();

            instance.InternalRegisterTextObjectForUpdate(textObject);

            k_RegisterTextObjectForUpdateMarker.End();
        }

        private void InternalRegisterTextObjectForUpdate(TMP_Text textObject)
        {
            if (m_InternalUpdateLookup.Add(textObject.GetInstanceID()))
                m_InternalUpdateQueue.Add(textObject);
        }

        /// <summary>
        /// Process the rebuild requests in the rebuild queues.
        /// </summary>
        void DoRebuilds()
        {
            // Handle text objects the require an update either as a result of scale changes or legacy animation.
            for (int i = 0; i < m_InternalUpdateQueue.Count; i++)
            {
                m_InternalUpdateQueue[i].InternalUpdate();
            }
        }

        internal static void UnRegisterTextObjectForUpdate(TMP_Text textObject)
        {
            k_UnregisterTextObjectForUpdateMarker.Begin();

            instance.InternalUnRegisterTextObjectForUpdate(textObject);

            k_UnregisterTextObjectForUpdateMarker.End();
        }

        private void InternalUnRegisterTextObjectForUpdate(TMP_Text textObject)
        {
            int id = textObject.GetInstanceID();

            m_InternalUpdateQueue.Remove(textObject);
            m_InternalUpdateLookup.Remove(id);
        }
    }
}
