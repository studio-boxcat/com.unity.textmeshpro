using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

#pragma warning disable 0414 // Disabled a few warnings related to serialized variables not used in this script but used in the editor.


namespace TMPro
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("UI/TextMeshPro - Text (UI)", 11)]
    [ExecuteAlways]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.0")]
    public partial class TextMeshProUGUI : TMP_Text, ILayoutElement
    {
        /// <summary>
        /// Get the material that will be used for rendering.
        /// </summary>
        public override Material materialForRendering
        {
            get { return TMP_MaterialManager.GetMaterialForRendering(this, m_sharedMaterial); }
        }


        /// <summary>
        /// Reference to the Mesh used by the text object.
        /// </summary>
        public override Mesh mesh
        {
            get { return m_mesh; }
        }


        /// <summary>
        /// Reference to the CanvasRenderer used by the text object.
        /// </summary>
        public new CanvasRenderer canvasRenderer
        {
            get
            {
                if (m_canvasRenderer == null) m_canvasRenderer = GetComponent<CanvasRenderer>();

                return m_canvasRenderer;
            }
        }


        public override void SetVerticesDirty()
        {
            if (this == null || !this.IsActive())
                return;

            if (CanvasUpdateRegistry.IsRebuildingGraphics())
                return;

            CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);
        }


        /// <summary>
        ///
        /// </summary>
        public override void SetLayoutDirty()
        {
            m_isPreferredWidthDirty = true;
            m_isPreferredHeightDirty = true;

            if (this == null || !this.IsActive())
                return;

            LayoutRebuilder.MarkLayoutForRebuild(this.rectTransform);

            m_isLayoutDirty = true;
        }


        /// <summary>
        ///
        /// </summary>
        public override void SetMaterialDirty()
        {
            if (this == null || !this.IsActive())
                return;

            if (CanvasUpdateRegistry.IsRebuildingGraphics())
                return;

            m_isMaterialDirty = true;
            CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);
        }


        /// <summary>
        ///
        /// </summary>
        public override void SetAllDirty()
        {
            SetLayoutDirty();
            SetVerticesDirty();
            SetMaterialDirty();
        }


        /// <summary>
        ///
        /// </summary>
        /// <param name="update"></param>
        public override void Rebuild(CanvasUpdate update)
        {
            if (this == null) return;

            if (update == CanvasUpdate.PreRender)
            {
                OnPreRenderCanvas();

                if (!m_isMaterialDirty) return;

                UpdateMaterial();
                m_isMaterialDirty = false;
            }
        }


        /// <summary>
        /// Method to keep the pivot of the sub text objects in sync with the parent pivot.
        /// </summary>
        private void UpdateSubObjectPivot()
        {
            if (m_textInfo == null) return;

            for (int i = 1; i < m_subTextObjects.Length && m_subTextObjects[i] != null; i++)
            {
                m_subTextObjects[i].SetPivotDirty();
            }
        }


        /// <summary>
        ///
        /// </summary>
        /// <param name="baseMaterial"></param>
        /// <returns></returns>
        public override Material GetModifiedMaterial(Material baseMaterial)
        {
            Material mat = baseMaterial;

            if (m_ShouldRecalculateStencil)
            {
                m_StencilValue = maskable ? MaskUtilities.GetStencilDepth(transform) : 0;
                m_ShouldRecalculateStencil = false;
            }

            if (m_StencilValue > 0)
            {
                var maskMat = StencilMaterial.Add(mat, (1 << m_StencilValue) - 1, StencilOp.Keep, CompareFunction.Equal, ColorWriteMask.All, (1 << m_StencilValue) - 1, 0);
                StencilMaterial.Remove(m_MaskMaterial);
                m_MaskMaterial = maskMat;
                mat = m_MaskMaterial;
            }

            return mat;
        }


        /// <summary>
        ///
        /// </summary>
        protected override void UpdateMaterial()
        {
            //Debug.Log("*** UpdateMaterial() ***");

            if (m_sharedMaterial == null || canvasRenderer == null) return;

            m_canvasRenderer.materialCount = 1;
            m_canvasRenderer.SetMaterial(materialForRendering, 0);
            //m_canvasRenderer.SetTexture(materialForRendering.mainTexture);
        }


        // MASKING RELATED PROPERTIES
        /// <summary>
        /// Sets the masking offset from the bounds of the object
        /// </summary>
        public Vector4 maskOffset
        {
            get { return m_maskOffset; }
            set { m_maskOffset = value; UpdateMask(); m_havePropertiesChanged = true; }
        }


        /// <summary>
        /// Override of the Cull function to provide for the ability to override the culling of the text object.
        /// </summary>
        /// <param name="clipRect"></param>
        /// <param name="validRect"></param>
        public override void Cull(Rect clipRect, bool validRect)
        {
            // Delay culling check in the event the text layout is dirty and geometry has to be updated.
            if (m_isLayoutDirty)
            {
                TMP_UpdateManager.RegisterTextElementForCullingUpdate(this);
                m_ClipRect = clipRect;
                m_ValidRect = validRect;
                return;
            }

            // Get compound rect for the text object and sub text objects in local canvas space.
            Rect rect = GetCanvasSpaceClippingRect();

            // No point culling if geometry bounds have no width or height.
            if (rect.width == 0 || rect.height == 0)
                return;

            var cull = !validRect || !clipRect.Overlaps(rect, true);
            if (m_canvasRenderer.cull != cull)
            {
                m_canvasRenderer.cull = cull;
                // XXX: 지나친 힙할당을 유발해서 제거.
                // onCullStateChanged.Invoke(cull);
                OnCullingChanged();

                // Update any potential sub mesh objects
                for (int i = 1; i < m_subTextObjects.Length && m_subTextObjects[i] != null; i++)
                {
                    m_subTextObjects[i].canvasRenderer.cull = cull;
                }
            }
        }

        private Rect m_ClipRect;
        private bool m_ValidRect;

        /// <summary>
        /// Internal function to allow delay of culling until the text geometry has been updated.
        /// </summary>
        internal override void UpdateCulling()
        {
            // Get compound rect for the text object and sub text objects in local canvas space.
            Rect rect = GetCanvasSpaceClippingRect();

            // No point culling if geometry bounds have no width or height.
            if (rect.width == 0 || rect.height == 0)
                return;

            var cull = !m_ValidRect || !m_ClipRect.Overlaps(rect, true);
            if (m_canvasRenderer.cull != cull)
            {
                m_canvasRenderer.cull = cull;
                // XXX: 지나친 힙할당을 유발해서 제거.
                // onCullStateChanged.Invoke(cull);
                OnCullingChanged();

                // Update any potential sub mesh objects
                for (int i = 1; i < m_subTextObjects.Length && m_subTextObjects[i] != null; i++)
                {
                    m_subTextObjects[i].canvasRenderer.cull = cull;
                }
            }
        }


        /// <summary>
        /// Function to be used to force recomputing of character padding when Shader / Material properties have been changed via script.
        /// </summary>
        public override void UpdateMeshPadding()
        {
            m_padding = ShaderUtilities.GetPadding(m_sharedMaterial, m_enableExtraPadding, m_isUsingBold);
            m_isMaskingEnabled = ShaderUtilities.IsMaskingEnabled(m_sharedMaterial);
            m_havePropertiesChanged = true;
            checkPaddingRequired = false;

            // Return if text object is not awake yet.
            if (m_textInfo == null) return;

            // Update sub text objects
            for (int i = 1; i < m_textInfo.materialCount; i++)
                m_subTextObjects[i].UpdateMeshPadding(m_enableExtraPadding, m_isUsingBold);
        }


        /// <summary>
        /// Function to force regeneration of the text object before its normal process time. This is useful when changes to the text object properties need to be applied immediately.
        /// </summary>
        /// <param name="ignoreActiveState">Ignore Active State of text objects. Inactive objects are ignored by default.</param>
        /// <param name="forceTextReparsing">Force re-parsing of the text.</param>
        public override void ForceMeshUpdate(bool ignoreActiveState = false, bool forceTextReparsing = false)
        {
            m_havePropertiesChanged = true;
            m_ignoreActiveState = ignoreActiveState;

            // Special handling in the event the Canvas is only disabled
            if (m_canvas == null)
                m_canvas = GetComponentInParent<Canvas>();

            OnPreRenderCanvas();
        }


        /// <summary>
        /// Function to clear the geometry of the Primary and Sub Text objects.
        /// </summary>
        public override void ClearMesh()
        {
            m_canvasRenderer.SetMesh(null);

            for (int i = 1; i < m_subTextObjects.Length && m_subTextObjects[i] != null; i++)
                m_subTextObjects[i].canvasRenderer.SetMesh(null);
        }


        /// <summary>
        /// Function to update the geometry of the main and sub text objects.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="index"></param>
        public override void UpdateGeometry(Mesh mesh, int index)
        {
            mesh.RecalculateBounds();

            if (index == 0)
            {
                m_canvasRenderer.SetMesh(mesh);
            }
            else
            {
                m_subTextObjects[index].canvasRenderer.SetMesh(mesh);
            }
        }


        /// <summary>
        /// Function to upload the updated vertex data and renderer.
        /// </summary>
        public override void UpdateVertexData(TMP_VertexDataUpdateFlags flags)
        {
            int materialCount = m_textInfo.materialCount;

            for (int i = 0; i < materialCount; i++)
            {
                Mesh mesh;

                if (i == 0)
                    mesh = m_mesh;
                else
                {
                    mesh = m_subTextObjects[i].mesh;
                }

                if ((flags & TMP_VertexDataUpdateFlags.Vertices) == TMP_VertexDataUpdateFlags.Vertices)
                    mesh.vertices = m_textInfo.meshInfo[i].vertices;

                if ((flags & TMP_VertexDataUpdateFlags.Uv0) == TMP_VertexDataUpdateFlags.Uv0)
                    mesh.uv = m_textInfo.meshInfo[i].uvs0;

                if ((flags & TMP_VertexDataUpdateFlags.Uv2) == TMP_VertexDataUpdateFlags.Uv2)
                    mesh.uv2 = m_textInfo.meshInfo[i].uvs2;

                if ((flags & TMP_VertexDataUpdateFlags.Colors32) == TMP_VertexDataUpdateFlags.Colors32)
                    mesh.colors32 = m_textInfo.meshInfo[i].colors32;

                mesh.RecalculateBounds();

                if (i == 0)
                    m_canvasRenderer.SetMesh(mesh);
                else
                    m_subTextObjects[i].canvasRenderer.SetMesh(mesh);
            }
        }


        /// <summary>
        /// Function to upload the updated vertex data and renderer.
        /// </summary>
        public override void UpdateVertexData()
        {
            int materialCount = m_textInfo.materialCount;

            for (int i = 0; i < materialCount; i++)
            {
                Mesh mesh;

                if (i == 0)
                    mesh = m_mesh;
                else
                {
                    // Clear unused vertices
                    m_textInfo.meshInfo[i].ClearUnusedVertices();

                    mesh = m_subTextObjects[i].mesh;
                }

                //mesh.MarkDynamic();
                mesh.vertices = m_textInfo.meshInfo[i].vertices;
                mesh.uv = m_textInfo.meshInfo[i].uvs0;
                mesh.uv2 = m_textInfo.meshInfo[i].uvs2;
                //mesh.uv4 = m_textInfo.meshInfo[i].uvs4;
                mesh.colors32 = m_textInfo.meshInfo[i].colors32;

                mesh.RecalculateBounds();

                if (i == 0)
                    m_canvasRenderer.SetMesh(mesh);
                else
                    m_subTextObjects[i].canvasRenderer.SetMesh(mesh);
            }
        }
    }
}
