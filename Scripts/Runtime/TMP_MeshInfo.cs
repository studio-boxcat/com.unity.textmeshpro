using UnityEngine;
using System;


namespace TMPro
{
    /// <summary>
    /// Structure which contains the vertex attributes (geometry) of the text object.
    /// </summary>
    public struct TMP_MeshInfo
    {
        private static readonly Color32 s_DefaultColor = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
        private static readonly Bounds s_DefaultBounds = new Bounds();

        public Mesh mesh;
        public int vertexCount;

        public Vector3[] vertices;

        public Vector2[] uvs0;
        public Vector2[] uvs2;
        public Color32[] colors32;
        public int[] triangles;


        /// <summary>
        /// Function to pre-allocate vertex attributes for a mesh of size X.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="size"></param>
        public TMP_MeshInfo(Mesh mesh, int size)
        {
            // Reference to the TMP Text Component.
            //this.textComponent = null;

            // Clear existing mesh data
            if (mesh == null)
                mesh = new Mesh();
            else
                mesh.Clear();

            this.mesh = mesh;

            // Limit the mesh to less than 65535 vertices which is the limit for Unity's Mesh.
            size = Mathf.Min(size, 16383);

            int sizeX4 = size * 4;
            int sizeX6 = size * 6;

            this.vertexCount = 0;

            this.vertices = new Vector3[sizeX4];
            this.uvs0 = new Vector2[sizeX4];
            this.uvs2 = new Vector2[sizeX4];
            this.colors32 = new Color32[sizeX4];

            this.triangles = new int[sizeX6];

            int index_X6 = 0;
            int index_X4 = 0;
            while (index_X4 / 4 < size)
            {
                for (int i = 0; i < 4; i++)
                {
                    this.vertices[index_X4 + i] = Vector3.zero;
                    this.uvs0[index_X4 + i] = Vector2.zero;
                    this.uvs2[index_X4 + i] = Vector2.zero;
                    //this.uvs4[index_X4 + i] = Vector2.zero;
                    this.colors32[index_X4 + i] = s_DefaultColor;
                }

                this.triangles[index_X6 + 0] = index_X4 + 0;
                this.triangles[index_X6 + 1] = index_X4 + 1;
                this.triangles[index_X6 + 2] = index_X4 + 2;
                this.triangles[index_X6 + 3] = index_X4 + 2;
                this.triangles[index_X6 + 4] = index_X4 + 3;
                this.triangles[index_X6 + 5] = index_X4 + 0;

                index_X4 += 4;
                index_X6 += 6;
            }

            // Pre-assign base vertex attributes.
            this.mesh.vertices = this.vertices;
            this.mesh.triangles = this.triangles;
            this.mesh.bounds = s_DefaultBounds;
        }


        /// <summary>
        /// Function to resized the content of MeshData and re-assign normals, tangents and triangles.
        /// </summary>
        /// <param name="meshData"></param>
        /// <param name="size"></param>
        public void ResizeMeshInfo(int size)
        {
            // If the requested size will exceed the 16 bit mesh limit, switch mesh to use 32 bit.
            //if (size > 16383 && this.mesh.indexFormat == IndexFormat.UInt16)
            //    this.mesh.indexFormat = IndexFormat.UInt32;
            size = Mathf.Min(size, 16383);

            int size_X4 = size * 4;
            int size_X6 = size * 6;

            int previousSize = this.vertices.Length / 4;

            Array.Resize(ref this.vertices, size_X4);

            Array.Resize(ref this.uvs0, size_X4);
            Array.Resize(ref this.uvs2, size_X4);
            //Array.Resize(ref this.uvs4, size_X4);

            Array.Resize(ref this.colors32, size_X4);

            Array.Resize(ref this.triangles, size_X6);


            // Re-assign Normals, Tangents and Triangles
            if (size <= previousSize)
            {
                this.mesh.triangles = this.triangles;
                this.mesh.vertices = this.vertices;

                return;
            }

            for (int i = previousSize; i < size; i++)
            {
                int index_X4 = i * 4;
                int index_X6 = i * 6;

                // Setup Triangles
                this.triangles[0 + index_X6] = 0 + index_X4;
                this.triangles[1 + index_X6] = 1 + index_X4;
                this.triangles[2 + index_X6] = 2 + index_X4;
                this.triangles[3 + index_X6] = 2 + index_X4;
                this.triangles[4 + index_X6] = 3 + index_X4;
                this.triangles[5 + index_X6] = 0 + index_X4;
            }

            this.mesh.vertices = this.vertices;
            this.mesh.triangles = this.triangles;
        }


        /// <summary>
        /// Function to clear the vertices while preserving the Triangles, Normals and Tangents.
        /// </summary>
        public void Clear(bool uploadChanges)
        {
            if (this.vertices == null) return;

            Array.Clear(this.vertices, 0, this.vertices.Length);
            this.vertexCount = 0;

            if (uploadChanges && this.mesh != null)
                this.mesh.vertices = this.vertices;

            if (this.mesh != null)
                this.mesh.bounds = s_DefaultBounds;
        }


        /// <summary>
        /// Function to clear the vertices while preserving the Triangles, Normals and Tangents.
        /// </summary>
        public void ClearUnusedVertices()
        {
            int length = vertices.Length - vertexCount;

            if (length > 0)
                Array.Clear(vertices, vertexCount, length);
        }


        /// <summary>
        /// Function used to mark unused vertices as degenerate.
        /// </summary>
        /// <param name="startIndex"></param>
        public void ClearUnusedVertices(int startIndex)
        {
            int length = this.vertices.Length - startIndex;

            if (length > 0)
                Array.Clear(this.vertices, startIndex, length);
        }


        /// <summary>
        /// Function used to mark unused vertices as degenerate an upload resulting data to the mesh.
        /// </summary>
        /// <param name="startIndex"></param>
        public void ClearUnusedVertices(int startIndex, bool updateMesh)
        {
            int length = this.vertices.Length - startIndex;

            if (length > 0)
                Array.Clear(this.vertices, startIndex, length);

            if (updateMesh && mesh != null)
                this.mesh.vertices = this.vertices;
        }

    }
}
