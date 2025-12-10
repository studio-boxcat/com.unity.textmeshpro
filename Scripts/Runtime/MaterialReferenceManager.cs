using UnityEngine;
using System.Collections.Generic;


namespace TMPro
{

    public class MaterialReferenceManager
    {
        private static MaterialReferenceManager s_Instance;

        // Dictionaries used to track Asset references.
        private Dictionary<int, Material> m_FontMaterialReferenceLookup = new Dictionary<int, Material>();
        private Dictionary<int, TMP_FontAsset> m_FontAssetReferenceLookup = new Dictionary<int, TMP_FontAsset>();


        /// <summary>
        /// Get a singleton instance of the registry
        /// </summary>
        public static MaterialReferenceManager instance
        {
            get
            {
                if (MaterialReferenceManager.s_Instance == null)
                    MaterialReferenceManager.s_Instance = new MaterialReferenceManager();
                return MaterialReferenceManager.s_Instance;
            }
        }



        /// <summary>
        /// Add new font asset reference to dictionary.
        /// </summary>
        /// <param name="fontAsset"></param>
        public static void AddFontAsset(TMP_FontAsset fontAsset)
        {
            MaterialReferenceManager.instance.AddFontAssetInternal(fontAsset);
        }

        /// <summary>
        ///  Add new Font Asset reference to dictionary.
        /// </summary>
        /// <param name="fontAsset"></param>
        private void AddFontAssetInternal(TMP_FontAsset fontAsset)
        {
            if (m_FontAssetReferenceLookup.ContainsKey(fontAsset.hashCode)) return;

            // Add reference to the font asset.
            m_FontAssetReferenceLookup.Add(fontAsset.hashCode, fontAsset);

            // Add reference to the font material.
            m_FontMaterialReferenceLookup.Add(fontAsset.materialHashCode, fontAsset.material);
        }

        /// <summary>
        /// Add new Material reference to dictionary.
        /// </summary>
        /// <param name="hashCode"></param>
        /// <param name="material"></param>
        public static void AddFontMaterial(int hashCode, Material material)
        {
            MaterialReferenceManager.instance.AddFontMaterialInternal(hashCode, material);
        }

        /// <summary>
        /// Add new material reference to dictionary.
        /// </summary>
        /// <param name="hashCode"></param>
        /// <param name="material"></param>
        private void AddFontMaterialInternal(int hashCode, Material material)
        {
            // Since this function is called after checking if the material is
            // contained in the dictionary, there is no need to check again.
            m_FontMaterialReferenceLookup.Add(hashCode, material);
        }


        /// <summary>
        /// Function to check if the font asset is already referenced.
        /// </summary>
        /// <param name="font"></param>
        /// <returns></returns>
        public bool Contains(TMP_FontAsset font)
        {
            return m_FontAssetReferenceLookup.ContainsKey(font.hashCode);
        }


        /// <summary>
        /// Function returning the Font Asset corresponding to the provided hash code.
        /// </summary>
        /// <param name="hashCode"></param>
        /// <param name="fontAsset"></param>
        /// <returns></returns>
        public static bool TryGetFontAsset(int hashCode, out TMP_FontAsset fontAsset)
        {
            return MaterialReferenceManager.instance.TryGetFontAssetInternal(hashCode, out fontAsset);
        }

        /// <summary>
        /// Internal Function returning the Font Asset corresponding to the provided hash code.
        /// </summary>
        /// <param name="hashCode"></param>
        /// <param name="fontAsset"></param>
        /// <returns></returns>
        private bool TryGetFontAssetInternal(int hashCode, out TMP_FontAsset fontAsset)
        {
            fontAsset = null;

            return m_FontAssetReferenceLookup.TryGetValue(hashCode, out fontAsset);
        }



        /// <summary>
        /// Function returning the Font Material corresponding to the provided hash code.
        /// </summary>
        /// <param name="hashCode"></param>
        /// <param name="material"></param>
        /// <returns></returns>
        public static bool TryGetMaterial(int hashCode, out Material material)
        {
            return MaterialReferenceManager.instance.TryGetMaterialInternal(hashCode, out material);
        }

        /// <summary>
        /// Internal function returning the Font Material corresponding to the provided hash code.
        /// </summary>
        /// <param name="hashCode"></param>
        /// <param name="material"></param>
        /// <returns></returns>
        private bool TryGetMaterialInternal(int hashCode, out Material material)
        {
            material = null;

            return m_FontMaterialReferenceLookup.TryGetValue(hashCode, out material);
        }
    }


    public struct MaterialReference
    {

        public int index;
        public TMP_FontAsset fontAsset;
        public Material material;
        public bool isDefaultMaterial;
        public bool isFallbackMaterial;
        public Material fallbackMaterial;
        public int referenceCount;


        /// <summary>
        /// Constructor for new Material Reference.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="fontAsset"></param>
        /// <param name="material"></param>
        public MaterialReference(int index, TMP_FontAsset fontAsset, Material material)
        {
            this.index = index;
            this.fontAsset = fontAsset;
            this.material = material;
            this.isDefaultMaterial = material.GetInstanceID() == fontAsset.material.GetInstanceID() ? true : false;
            this.isFallbackMaterial = false;
            this.fallbackMaterial = null;
            this.referenceCount = 0;
        }


        /// <summary>
        /// Function to check if a certain font asset is contained in the material reference array.
        /// </summary>
        /// <param name="materialReferences"></param>
        /// <param name="fontAsset"></param>
        /// <returns></returns>
        public static bool Contains(MaterialReference[] materialReferences, TMP_FontAsset fontAsset)
        {
            int id = fontAsset.GetInstanceID();

            for (int i = 0; i < materialReferences.Length && materialReferences[i].fontAsset != null; i++)
            {
                if (materialReferences[i].fontAsset.GetInstanceID() == id)
                    return true;
            }

            return false;
        }


        /// <summary>
        /// Function to add a new material reference and returning its index in the material reference array.
        /// </summary>
        /// <param name="material"></param>
        /// <param name="fontAsset"></param>
        /// <param name="materialReferences"></param>
        /// <param name="materialReferenceIndexLookup"></param>
        /// <returns></returns>
        public static int AddMaterialReference(Material material, TMP_FontAsset fontAsset, ref MaterialReference[] materialReferences, Dictionary<int, int> materialReferenceIndexLookup)
        {
            int materialID = material.GetInstanceID();
            int index;

            if (materialReferenceIndexLookup.TryGetValue(materialID, out index))
                return index;

            index = materialReferenceIndexLookup.Count;

            // Add new reference index
            materialReferenceIndexLookup[materialID] = index;

            if (index >= materialReferences.Length)
                System.Array.Resize(ref materialReferences, Mathf.NextPowerOfTwo(index + 1));

            materialReferences[index].index = index;
            materialReferences[index].fontAsset = fontAsset;
            materialReferences[index].material = material;
            materialReferences[index].isDefaultMaterial = materialID == fontAsset.material.GetInstanceID() ? true : false;
            materialReferences[index].referenceCount = 0;

            return index;

        }
    }
}
