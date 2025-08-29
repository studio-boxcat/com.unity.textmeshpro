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
        public int materialCount;

        public TMP_CharacterInfo[] characterInfo;
        public TMP_LineInfo[] lineInfo;
        public TMP_MeshInfo[] meshInfo;

        private TMP_MeshInfo[] m_CachedMeshInfo;

        internal TMP_TextInfo(int characterCount)
        {
            characterInfo = new TMP_CharacterInfo[characterCount];
            lineInfo = new TMP_LineInfo[2];
            meshInfo = new TMP_MeshInfo[1];
        }

        public TMP_TextInfo(Mesh mesh)
        {
            characterInfo = new TMP_CharacterInfo[8];
            lineInfo = new TMP_LineInfo[2];
            meshInfo = new TMP_MeshInfo[1];
            meshInfo[0].mesh = mesh;
            materialCount = 1;
        }


        /// <summary>
        /// Function to clear the counters of the text object.
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < this.meshInfo.Length; i++)
                this.meshInfo[i].vertexCount = 0;
        }


        /// <summary>
        /// Function to clear the content of the MeshInfo array while preserving the Triangles, Normals and Tangents.
        /// </summary>
        public void ClearMeshInfo(bool updateMesh)
        {
            for (int i = 0; i < this.meshInfo.Length; i++)
                this.meshInfo[i].Clear(updateMesh);
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
        /// Function to copy the MeshInfo Arrays and their primary vertex data content.
        /// </summary>
        /// <returns>A copy of the MeshInfo[]</returns>
        public TMP_MeshInfo[] CopyMeshInfoVertexData()
        {
            if (m_CachedMeshInfo == null || m_CachedMeshInfo.Length != meshInfo.Length)
            {
                m_CachedMeshInfo = new TMP_MeshInfo[meshInfo.Length];

                // Initialize all the vertex data arrays
                for (int i = 0; i < m_CachedMeshInfo.Length; i++)
                {
                    int length = meshInfo[i].vertices.Length;

                    m_CachedMeshInfo[i].vertices = new Vector3[length];
                    m_CachedMeshInfo[i].uvs0 = new Vector2[length];
                    m_CachedMeshInfo[i].uvs2 = new Vector2[length];
                    m_CachedMeshInfo[i].colors32 = new Color32[length];
                }
            }

            for (int i = 0; i < m_CachedMeshInfo.Length; i++)
            {
                int length = meshInfo[i].vertices.Length;

                if (m_CachedMeshInfo[i].vertices.Length != length)
                {
                    m_CachedMeshInfo[i].vertices = new Vector3[length];
                    m_CachedMeshInfo[i].uvs0 = new Vector2[length];
                    m_CachedMeshInfo[i].uvs2 = new Vector2[length];
                    m_CachedMeshInfo[i].colors32 = new Color32[length];

                    //m_CachedMeshInfo[i].normals = new Vector3[length];
                    //m_CachedMeshInfo[i].tangents = new Vector4[length];
                    //m_CachedMeshInfo[i].triangles = new int[meshInfo[i].triangles.Length];
                }


                // Only copy the primary vertex data
                Array.Copy(meshInfo[i].vertices, m_CachedMeshInfo[i].vertices, length);
                Array.Copy(meshInfo[i].uvs0, m_CachedMeshInfo[i].uvs0, length);
                Array.Copy(meshInfo[i].uvs2, m_CachedMeshInfo[i].uvs2, length);
                Array.Copy(meshInfo[i].colors32, m_CachedMeshInfo[i].colors32, length);

                //Array.Copy(meshInfo[i].normals, m_CachedMeshInfo[i].normals, length);
                //Array.Copy(meshInfo[i].tangents, m_CachedMeshInfo[i].tangents, length);
                //Array.Copy(meshInfo[i].triangles, m_CachedMeshInfo[i].triangles, meshInfo[i].triangles.Length);
            }

            return m_CachedMeshInfo;
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
