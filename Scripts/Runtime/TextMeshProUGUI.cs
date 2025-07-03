using UnityEngine;
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
        public override Material materialForRendering => MaterialModifierUtils.ResolveMaterialForRendering(this, m_sharedMaterial);


        /// <summary>
        /// Reference to the Mesh used by the text object.
        /// </summary>
        public Mesh mesh => m_mesh;


        public override void SetVerticesDirty()
        {
            if (this == null || !IsActive())
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

            LayoutRebuilder.MarkLayoutForRebuild(this.transform);

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
        protected override void UpdateMaterial()
        {
            //Debug.Log("*** UpdateMaterial() ***");

            if (m_sharedMaterial == null || base.canvasRenderer == null) return;

            m_canvasRenderer.materialCount = 1;
            m_canvasRenderer.SetMaterial(materialForRendering, 0);
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
            var rect = GetCanvasSpaceClippingRect();

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
        protected override void UpdateMeshPadding()
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
        /// Function to clear the geometry of the Primary and Sub Text objects.
        /// </summary>
        void ClearMesh()
        {
            m_canvasRenderer.SetMesh(null);

            for (int i = 1; i < m_subTextObjects.Length && m_subTextObjects[i] != null; i++)
                m_subTextObjects[i].canvasRenderer.SetMesh(null);
        }
    }
}
