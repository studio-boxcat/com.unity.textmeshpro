//#define TMP_DEBUG_MODE

using UnityEngine;
using System.Collections.Generic;



namespace TMPro
{

    public static class TMP_MaterialManager
    {
        static List<MaskingMaterial> m_materialList = new();

        static Dictionary<long, FallbackMaterial> m_fallbackMaterials = new();
        static Dictionary<int, long> m_fallbackMaterialLookup = new();
        static List<FallbackMaterial> m_fallbackCleanupList = new();

        static bool isFallbackListDirty;

        static TMP_MaterialManager()
        {
            Canvas.willRenderCanvases += OnPreRender;
        }

        static void OnPreRender()
        {
            if (isFallbackListDirty)
            {
                //Debug.Log("2 - Cleaning up Fallback Materials.");
                CleanupFallbackMaterials();
                isFallbackListDirty = false;
            }
        }

        /// <summary>
        /// Function to release the stencil material.
        /// </summary>
        /// <param name="stencilMaterial"></param>
        public static void ReleaseStencilMaterial(Material stencilMaterial)
        {
            int stencilMaterialID = stencilMaterial.GetInstanceID();

            for (int i = 0; i < m_materialList.Count; i++)
            {
                if (m_materialList[i].stencilMaterial.GetInstanceID() == stencilMaterialID)
                {
                    if (m_materialList[i].count > 1)
                        m_materialList[i].count -= 1;
                    else
                    {
                        Object.DestroyImmediate(m_materialList[i].stencilMaterial);
                        m_materialList.RemoveAt(i);
                    }

                    break;
                }
            }
        }


        internal static Material GetFallbackMaterial(TMP_FontAsset fontAsset, Material sourceMaterial, int atlasIndex)
        {
            int sourceMaterialID = sourceMaterial.GetInstanceID();
            Texture tex = fontAsset.atlasTextures[atlasIndex];
            int texID = tex.GetInstanceID();
            long key = (long)sourceMaterialID << 32 | (uint)texID;
            FallbackMaterial fallback;

            if (m_fallbackMaterials.TryGetValue(key, out fallback))
            {
                // Check if source material properties have changed.
                int sourceMaterialCRC = sourceMaterial.ComputeCRC();
                if (sourceMaterialCRC == fallback.sourceMaterialCRC)
                    return fallback.fallbackMaterial;

                CopyMaterialPresetProperties(sourceMaterial, fallback.fallbackMaterial);
                fallback.sourceMaterialCRC = sourceMaterialCRC;
                return fallback.fallbackMaterial;
            }

            // Create new material from the source material and assign relevant atlas texture
            Material fallbackMaterial = new Material(sourceMaterial);
            fallbackMaterial.SetTexture(ShaderUtilities.ID_MainTex, tex);

            fallbackMaterial.hideFlags = HideFlags.HideAndDontSave;

            #if UNITY_EDITOR
                fallbackMaterial.name += " + " + tex.name;
            #endif

            fallback = new FallbackMaterial();
            fallback.fallbackID = key;
            fallback.sourceMaterialCRC = sourceMaterial.ComputeCRC();
            fallback.fallbackMaterial = fallbackMaterial;
            fallback.count = 0;

            m_fallbackMaterials.Add(key, fallback);
            m_fallbackMaterialLookup.Add(fallbackMaterial.GetInstanceID(), key);

            return fallbackMaterial;
        }


        /// <summary>
        /// This function returns a material instance using the material properties of a previous material but using the font atlas texture of the new font asset.
        /// </summary>
        /// <param name="sourceMaterial">The material containing the source material properties to be copied to the new material.</param>
        /// <param name="targetMaterial">The font atlas texture that should be assigned to the new material.</param>
        /// <returns></returns>
        public static Material GetFallbackMaterial (Material sourceMaterial, Material targetMaterial)
        {
            int sourceID = sourceMaterial.GetInstanceID();
            Texture tex = targetMaterial.GetTexture(ShaderUtilities.ID_MainTex);
            int texID = tex.GetInstanceID();
            long key = (long)sourceID << 32 | (long)(uint)texID;
            FallbackMaterial fallback;

            if (m_fallbackMaterials.TryGetValue(key, out fallback))
            {
                // Check if source material properties have changed.
                int sourceMaterialCRC = sourceMaterial.ComputeCRC();
                if (sourceMaterialCRC == fallback.sourceMaterialCRC)
                    return fallback.fallbackMaterial;

                CopyMaterialPresetProperties(sourceMaterial, fallback.fallbackMaterial);
                fallback.sourceMaterialCRC = sourceMaterialCRC;
                return fallback.fallbackMaterial;
            }

            // Create new material from the source material and copy properties if using distance field shaders.
            Material fallbackMaterial;
            if (sourceMaterial.HasProperty(ShaderUtilities.ID_GradientScale) && targetMaterial.HasProperty(ShaderUtilities.ID_GradientScale))
            {
                fallbackMaterial = new Material(sourceMaterial);
                fallbackMaterial.hideFlags = HideFlags.HideAndDontSave;

                #if UNITY_EDITOR
                fallbackMaterial.name += " + " + tex.name;
                //Debug.Log("Creating new fallback material for " + fallbackMaterial.name);
                #endif

                fallbackMaterial.SetTexture(ShaderUtilities.ID_MainTex, tex);
                // Retain material properties unique to target material.
                fallbackMaterial.SetFloat(ShaderUtilities.ID_GradientScale, targetMaterial.GetFloat(ShaderUtilities.ID_GradientScale));
                fallbackMaterial.SetFloat(ShaderUtilities.ID_TextureWidth, targetMaterial.GetFloat(ShaderUtilities.ID_TextureWidth));
                fallbackMaterial.SetFloat(ShaderUtilities.ID_TextureHeight, targetMaterial.GetFloat(ShaderUtilities.ID_TextureHeight));
                fallbackMaterial.SetFloat(ShaderUtilities.ID_WeightNormal, targetMaterial.GetFloat(ShaderUtilities.ID_WeightNormal));
                fallbackMaterial.SetFloat(ShaderUtilities.ID_WeightBold, targetMaterial.GetFloat(ShaderUtilities.ID_WeightBold));
            }
            else
            {
                fallbackMaterial = new Material(targetMaterial);
            }

            fallback = new FallbackMaterial();
            fallback.fallbackID = key;
            fallback.sourceMaterialCRC = sourceMaterial.ComputeCRC();
            fallback.fallbackMaterial = fallbackMaterial;
            fallback.count = 0;

            m_fallbackMaterials.Add(key, fallback);
            m_fallbackMaterialLookup.Add(fallbackMaterial.GetInstanceID(), key);

            #if TMP_DEBUG_MODE
            ListFallbackMaterials();
            #endif

            return fallbackMaterial;
        }


        /// <summary>
        ///
        /// </summary>
        /// <param name="targetMaterial"></param>
        public static void AddFallbackMaterialReference(Material targetMaterial)
        {
            if (targetMaterial == null) return;

            int sourceID = targetMaterial.GetInstanceID();
            long key;

            // Lookup key to retrieve
            if (m_fallbackMaterialLookup.TryGetValue(sourceID, out key))
            {
                FallbackMaterial fallback;
                if (m_fallbackMaterials.TryGetValue(key, out fallback))
                {
                    //Debug.Log("Adding Fallback material " + fallback.fallbackMaterial.name + " with reference count of " + (fallback.count + 1));
                    fallback.count += 1;
                }
            }
        }


        /// <summary>
        ///
        /// </summary>
        public static void CleanupFallbackMaterials()
        {
            // Return if the list is empty.
            if (m_fallbackCleanupList.Count == 0) return;

            for (int i = 0; i < m_fallbackCleanupList.Count; i++)
            {
                FallbackMaterial fallback = m_fallbackCleanupList[i];

                if (fallback.count < 1)
                {
                    //Debug.Log("Cleaning up " + fallback.fallbackMaterial.name);

                    Material mat = fallback.fallbackMaterial;
                    m_fallbackMaterials.Remove(fallback.fallbackID);
                    m_fallbackMaterialLookup.Remove(mat.GetInstanceID());
                    Object.DestroyImmediate(mat);
                    mat = null;
                }
            }

            m_fallbackCleanupList.Clear();
        }


        /// <summary>
        /// Function to release the fallback material.
        /// </summary>
        /// <param name="fallbackMaterial">Material to be released.</param>
        public static void ReleaseFallbackMaterial(Material fallbackMaterial)
        {
            if (fallbackMaterial == null) return;

            int materialID = fallbackMaterial.GetInstanceID();
            long key;

            if (m_fallbackMaterialLookup.TryGetValue(materialID, out key))
            {
                FallbackMaterial fallback;
                if (m_fallbackMaterials.TryGetValue(key, out fallback))
                {
                    //Debug.Log("Releasing Fallback material " + fallback.fallbackMaterial.name + " with remaining reference count of " + (fallback.count - 1));

                    fallback.count -= 1;

                    if (fallback.count < 1)
                        m_fallbackCleanupList.Add(fallback);
                }
            }

            isFallbackListDirty = true;

            #if TMP_DEBUG_MODE
            ListFallbackMaterials();
            #endif
        }


        class FallbackMaterial
        {
            public long fallbackID;
            internal int sourceMaterialCRC;
            public Material fallbackMaterial;
            public int count;
        }


        class MaskingMaterial
        {
            public Material baseMaterial;
            public Material stencilMaterial;
            public int count;
            public int stencilID;
        }


        /// <summary>
        /// Function to copy the properties of a source material preset to another while preserving the unique font asset properties of the destination material.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        public static void CopyMaterialPresetProperties(Material source, Material destination)
        {
            if (!source.HasProperty(ShaderUtilities.ID_GradientScale) || !destination.HasProperty(ShaderUtilities.ID_GradientScale))
                return;

            // Save unique material properties
            Texture dst_texture = destination.GetTexture(ShaderUtilities.ID_MainTex);
            float dst_gradientScale = destination.GetFloat(ShaderUtilities.ID_GradientScale);
            float dst_texWidth = destination.GetFloat(ShaderUtilities.ID_TextureWidth);
            float dst_texHeight = destination.GetFloat(ShaderUtilities.ID_TextureHeight);
            float dst_weightNormal = destination.GetFloat(ShaderUtilities.ID_WeightNormal);
            float dst_weightBold = destination.GetFloat(ShaderUtilities.ID_WeightBold);

            // Copy all material properties
            destination.CopyPropertiesFromMaterial(source);

            // Copy shader keywords
            destination.shaderKeywords = source.shaderKeywords;

            // Restore unique material properties
            destination.SetTexture(ShaderUtilities.ID_MainTex, dst_texture);
            destination.SetFloat(ShaderUtilities.ID_GradientScale, dst_gradientScale);
            destination.SetFloat(ShaderUtilities.ID_TextureWidth, dst_texWidth);
            destination.SetFloat(ShaderUtilities.ID_TextureHeight, dst_texHeight);
            destination.SetFloat(ShaderUtilities.ID_WeightNormal, dst_weightNormal);
            destination.SetFloat(ShaderUtilities.ID_WeightBold, dst_weightBold);
        }
    }
}

