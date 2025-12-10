using System;
using UnityEditor;


namespace TMPro.EditorUtilities
{
    /// <summary>
    /// Asset post processor used to handle text assets changes.
    /// </summary>
    class TMPro_TexturePostProcessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var asset in importedAssets)
            {
                // Return if imported asset path is outside of the project.
                if (asset.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) == false)
                    continue;

                var assetType = AssetDatabase.GetMainAssetTypeAtPath(asset);
                if (assetType != typeof(TMP_FontAsset)) continue;

                var fontAsset = AssetDatabase.LoadAssetAtPath(asset, typeof(TMP_FontAsset)) as TMP_FontAsset;
                // Only refresh font asset definition if font asset was previously initialized.
                if (fontAsset != null && fontAsset.m_CharacterLookupDictionary != null)
                    TMP_EditorResourceManager.RegisterFontAssetForDefinitionRefresh(fontAsset);
            }
        }
    }
}
