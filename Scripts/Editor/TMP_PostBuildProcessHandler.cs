﻿using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using UnityEngine.AddressableAssets;


namespace TMPro
{
    public class TMP_PostBuildProcessHandler
    {
        [PostProcessBuildAttribute(10000)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target == BuildTarget.iOS)
            {
                // Try loading the TMP Settings
                TMP_Settings settings = Addressables.LoadAsset<TMP_Settings>("TMP Settings");

                if (settings == null)
                    return;

                string file = Path.Combine(pathToBuiltProject, "Classes/UI/Keyboard.mm");
                string content = File.ReadAllText(file);
                content = content.Replace("FILTER_EMOJIS_IOS_KEYBOARD 1", "FILTER_EMOJIS_IOS_KEYBOARD 0");
                File.WriteAllText(file, content);
            }
        }
    }
}
