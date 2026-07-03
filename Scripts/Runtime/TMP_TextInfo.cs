using UnityEngine;
using System;


namespace TMPro
{
    /// <summary>
    /// Class which contains information about every element contained within the text object.
    /// </summary>
    [Serializable]
    public class TMP_TextInfo
    {
        public TMP_CharacterInfo[] characterInfo;
        public TMP_LineInfo[] lineInfo;
        public TMP_MeshInfo meshInfo;

        internal TMP_TextInfo(int characterCount)
        {
            characterInfo = new TMP_CharacterInfo[characterCount];
            lineInfo = new TMP_LineInfo[2];
            meshInfo = new TMP_MeshInfo();
        }

        public TMP_TextInfo(Mesh mesh)
        {
            characterInfo = new TMP_CharacterInfo[8];
            lineInfo = new TMP_LineInfo[2];
            meshInfo.mesh = mesh;
        }


        /// <summary>
        /// Function to clear the counters of the text object.
        /// </summary>
        public void Clear()
        {
            this.meshInfo.vertexCount = 0;
        }


        /// <summary>
        /// Function to clear the content of the MeshInfo array while preserving the Triangles, Normals and Tangents.
        /// </summary>
        public void ClearMeshInfo(bool updateMesh)
        {
            this.meshInfo.Clear(updateMesh);
        }


        /// <summary>
        /// Function to clear and initialize the lineInfo array.
        /// </summary>
        public void ClearLineInfo()
        {
            if (this.lineInfo == null)
                this.lineInfo = new TMP_LineInfo[2];

            int length = this.lineInfo.Length;

            for (int i = 0; i < length; i++)
            {
                this.lineInfo[i].characterCount = 0;
                this.lineInfo[i].width = 0;
                this.lineInfo[i].maxAdvance = 0;
            }
        }




        /// <summary>
        /// Function to resize any of the structure contained in the TMP_TextInfo class.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="size"></param>
        public static void Resize<T> (ref T[] array, int size)
        {
            // Allocated to the next power of two
            int newSize = size > 1024 ? size + 256 : Mathf.NextPowerOfTwo(size);

            Array.Resize(ref array, newSize);
        }


        /// <summary>
        /// Function to resize any of the structure contained in the TMP_TextInfo class.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="size"></param>
        /// <param name="isFixedSize"></param>
        public static void Resize<T>(ref T[] array, int size, bool isBlockAllocated)
        {
            if (isBlockAllocated) size = size > 1024 ? size + 256 : Mathf.NextPowerOfTwo(size);

            if (size == array.Length) return;

            //Debug.Log("Resizing TextInfo from [" + array.Length + "] to [" + size + "]");

            Array.Resize(ref array, size);
        }

    }
}
