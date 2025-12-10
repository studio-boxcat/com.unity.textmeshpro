using UnityEngine;
using System.Collections.Generic;


namespace TMPro
{
    public struct MaterialReference
    {
        public TMP_FontAsset fontAsset;
        public Material material;
        public bool isDefaultMaterial;
        public int referenceCount;


        /// <summary>
        /// Constructor for new Material Reference.
        /// </summary>
        /// <param name="fontAsset"></param>
        /// <param name="material"></param>
        public MaterialReference(TMP_FontAsset fontAsset, Material material)
        {
            this.fontAsset = fontAsset;
            this.material = material;
            isDefaultMaterial = material.GetInstanceID() == fontAsset.material.GetInstanceID() ? true : false;
            referenceCount = 0;
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

            if (materialReferenceIndexLookup.TryGetValue(materialID, out var index))
                return index;

            index = materialReferenceIndexLookup.Count;

            // Add new reference index
            materialReferenceIndexLookup[materialID] = index;

            if (index >= materialReferences.Length)
                System.Array.Resize(ref materialReferences, Mathf.NextPowerOfTwo(index + 1));

            materialReferences[index].fontAsset = fontAsset;
            materialReferences[index].material = material;
            materialReferences[index].isDefaultMaterial = materialID == fontAsset.material.GetInstanceID();
            materialReferences[index].referenceCount = 0;

            return index;
        }
    }
}