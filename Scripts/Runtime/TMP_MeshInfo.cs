﻿using UnityEngine;
using System;
using System.Collections.Generic;


namespace TMPro
{
    public enum VertexSortingOrder { Normal, Reverse };

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
        //public Vector2[] uvs4;
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
            //this.uvs4 = new Vector2[sizeX4]; // SDF scale data
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


        public void SortGeometry (VertexSortingOrder order)
        {
            switch (order)
            {
                case VertexSortingOrder.Normal:
                    // Do nothing
                    break;
                case VertexSortingOrder.Reverse:
                    int size = vertexCount / 4;
                    for (int i = 0; i < size; i++)
                    {
                        int src = i * 4;
                        int dst = (size - i - 1) * 4;

                        if (src < dst)
                            SwapVertexData(src, dst);

                    }
                    break;
                //case VertexSortingOrder.Depth:
                //    break;

            }
        }


        /// <summary>
        /// Function to rearrange the quads of the text object to change their rendering order.
        /// </summary>
        /// <param name="sortingOrder"></param>
        public void SortGeometry(IList<int> sortingOrder)
        {
            // Make sure the sorting order array is not larger than the vertices array.
            int indexCount = sortingOrder.Count;

            if (indexCount * 4 > vertices.Length) return;

            int src_index;

            for (int dst_index = 0; dst_index < indexCount; dst_index++)
            {
                src_index = sortingOrder[dst_index];

                while (src_index < dst_index)
                {
                    src_index = sortingOrder[src_index];
                }

                // Swap items
                if (src_index != dst_index)
                    SwapVertexData(src_index * 4, dst_index * 4);

                //Debug.Log("Swap element [" + dst_index + "] with [" + src_index + "]. Vertex[" + dst_index + "] is " + vertices[dst_index * 4].z);
            }
        }


        /// <summary>
        /// Method to swap the vertex attributes between src and dst quads.
        /// </summary>
        /// <param name="src">Index of the first vertex attribute of the source character / quad.</param>
        /// <param name="dst">Index of the first vertex attribute of the destination character / quad.</param>
        public void SwapVertexData(int src, int dst)
        {
            int src_Index = src; //  * 4;
            int dst_Index = dst; // * 4;

            // Swap vertices
            Vector3 vertex;
            vertex = vertices[dst_Index + 0];
            vertices[dst_Index + 0] = vertices[src_Index + 0];
            vertices[src_Index + 0] = vertex;

            vertex = vertices[dst_Index + 1];
            vertices[dst_Index + 1] = vertices[src_Index + 1];
            vertices[src_Index + 1] = vertex;

            vertex = vertices[dst_Index + 2];
            vertices[dst_Index + 2] = vertices[src_Index + 2];
            vertices[src_Index + 2] = vertex;

            vertex = vertices[dst_Index + 3];
            vertices[dst_Index + 3] = vertices[src_Index + 3];
            vertices[src_Index + 3] = vertex;


            //Swap UVs0
            Vector2 uvs;
            uvs = uvs0[dst_Index + 0];
            uvs0[dst_Index + 0] = uvs0[src_Index + 0];
            uvs0[src_Index + 0] = uvs;

            uvs = uvs0[dst_Index + 1];
            uvs0[dst_Index + 1] = uvs0[src_Index + 1];
            uvs0[src_Index + 1] = uvs;

            uvs = uvs0[dst_Index + 2];
            uvs0[dst_Index + 2] = uvs0[src_Index + 2];
            uvs0[src_Index + 2] = uvs;

            uvs = uvs0[dst_Index + 3];
            uvs0[dst_Index + 3] = uvs0[src_Index + 3];
            uvs0[src_Index + 3] = uvs;

            // Swap UVs2
            uvs = uvs2[dst_Index + 0];
            uvs2[dst_Index + 0] = uvs2[src_Index + 0];
            uvs2[src_Index + 0] = uvs;

            uvs = uvs2[dst_Index + 1];
            uvs2[dst_Index + 1] = uvs2[src_Index + 1];
            uvs2[src_Index + 1] = uvs;

            uvs = uvs2[dst_Index + 2];
            uvs2[dst_Index + 2] = uvs2[src_Index + 2];
            uvs2[src_Index + 2] = uvs;

            uvs = uvs2[dst_Index + 3];
            uvs2[dst_Index + 3] = uvs2[src_Index + 3];
            uvs2[src_Index + 3] = uvs;

            // Vertex Colors
            Color32 color;
            color = colors32[dst_Index + 0];
            colors32[dst_Index + 0] = colors32[src_Index + 0];
            colors32[src_Index + 0] = color;

            color = colors32[dst_Index + 1];
            colors32[dst_Index + 1] = colors32[src_Index + 1];
            colors32[src_Index + 1] = color;

            color = colors32[dst_Index + 2];
            colors32[dst_Index + 2] = colors32[src_Index + 2];
            colors32[src_Index + 2] = color;

            color = colors32[dst_Index + 3];
            colors32[dst_Index + 3] = colors32[src_Index + 3];
            colors32[src_Index + 3] = color;
        }


        //int Partition (int start, int end)
        //{
        //    float pivot = vertices[end].z;

        //    int partitionIndex = start;
        //    for (int i = start; i < end; i++)
        //    {
        //        if (vertices[i].z <= pivot)
        //        {
        //            Swap(vertices[i], vertices[partitionIndex]);
        //            partitionIndex += 1;
        //        }
        //    }
        //    Swap(vertices[partitionIndex], vertices[end]);
        //    return partitionIndex;
        //}


        //void Swap(Vector3 a, Vector3 b)
        //{
        //    Vector3 temp = a;
        //    a = b;
        //    b = a;
        //}

    }
}
