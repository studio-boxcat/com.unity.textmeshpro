using UnityEngine;
using UnityEngine.UI;

#pragma warning disable 0414 // Disabled a few warnings related to serialized variables not used in this script but used in the editor.


namespace TMPro
{
    [DisallowMultipleComponent]
    public partial class TextMeshProUGUI : TMP_Text, ILayoutElement
    {
        // TMP manages its own font materials — bypass Graphic.materialForRendering.
        private Material materialForRendering => m_sharedMaterial;


        /// <summary>
        /// Reference to the Mesh used by the text object.
        /// </summary>
        public Mesh mesh => m_mesh;


        public override void SetVerticesDirty()
        {
            if (!IsActive())
                return;

            CanvasUpdateRegistry.QueueGraphic(this);
        }


        /// <summary>
        ///
        /// </summary>
        public override void SetLayoutDirty()
        {
            if (this == null || !this.IsActive())
                return;

            LayoutRebuilder.SetDirty(this);

            m_isLayoutDirty = true;
        }


        /// <summary>
        ///
        /// </summary>
        public override void SetMaterialDirty()
        {
            if (!IsActive())
                return;

            m_isMaterialDirty = true;
            CanvasUpdateRegistry.QueueGraphic(this);
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


        public override void Rebuild()
        {
            OnPreRenderCanvas();

            if (!m_isMaterialDirty) return;

            UpdateMaterial();
            m_isMaterialDirty = false;
        }


        /// <summary>
        ///
        /// </summary>
        protected override void UpdateMaterial()
        {
            //Debug.Log("*** UpdateMaterial() ***");

            canvasRenderer.SetMaterialSingle(materialForRendering);
        }


        /// <summary>
        /// Function to be used to force recomputing of character padding when Shader / Material properties have been changed via script.
        /// </summary>
        protected void UpdateMeshPadding()
        {
            m_padding = ShaderUtilities.GetPadding(m_sharedMaterial, m_enableExtraPadding, m_isUsingBold);
            m_havePropertiesChanged = true;
            checkPaddingRequired = false;
        }


        /// <summary>
        /// Function to clear the geometry of the text object.
        /// </summary>
        void ClearMesh() => canvasRenderer.SetMesh(null);
    }
}
