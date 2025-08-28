using UnityEngine;
using UnityEngine.UI;

#pragma warning disable 0414 // Disabled a few warnings related to serialized variables not used in this script but used in the editor.

namespace TMPro
{
    public class TMP_SubMeshUI : Graphic
    {
        /// <summary>
        /// The TMP Font Asset assigned to this sub text object.
        /// </summary>
        public TMP_FontAsset fontAsset
        {
            get { return m_fontAsset; }
            set { m_fontAsset = value; }
        }
        [SerializeField]
        private TMP_FontAsset m_fontAsset;


        /// <summary>
        ///
        /// </summary>
        public override Texture mainTexture
        {
            get
            {
                if (this.sharedMaterial != null)
                    return this.sharedMaterial.GetTexture(ShaderUtilities.ID_MainTex);

                return null;
            }
        }


        /// <summary>
        /// The material to be assigned to this object. Returns an instance of the material.
        /// </summary>
        public override Material material
        {
            // Return a new Instance of the Material if none exists. Otherwise return the current Material Instance.
            get => GetMaterial(m_sharedMaterial);

            // Assign new font material
            set
            {
                if (m_sharedMaterial != null && m_sharedMaterial.GetInstanceID() == value.GetInstanceID())
                    return;

                m_sharedMaterial = m_material = value;

                m_padding = GetPaddingForMaterial();

                SetVerticesDirty();
                SetMaterialDirty();
            }
        }
        [SerializeField]
        private Material m_material;


        /// <summary>
        /// The material to be assigned to this text object.
        /// </summary>
        public Material sharedMaterial
        {
            get => m_sharedMaterial;
            set => SetSharedMaterial(value);
        }
        [SerializeField]
        private Material m_sharedMaterial;


        /// <summary>
        /// Get the material that will be used for rendering.
        /// </summary>
        public override Material materialForRendering => MaterialModifierUtils.ResolveMaterialForRendering(this, m_sharedMaterial);


        /// <summary>
        /// Is the text object using the default font asset material.
        /// </summary>
        public bool isDefaultMaterial
        {
            get { return m_isDefaultMaterial; }
            set { m_isDefaultMaterial = value; }
        }
        [SerializeField]
        private bool m_isDefaultMaterial;


        /// <summary>
        /// Padding value resulting for the property settings on the material.
        /// </summary>
        public float padding
        {
            get { return m_padding; }
            set { m_padding = value; }
        }
        [SerializeField]
        private float m_padding;


        /// <summary>
        /// The Mesh of this text sub object.
        /// </summary>
        public Mesh mesh
        {
            get
            {
                if (m_mesh == null)
                {
                    m_mesh = new Mesh();
                    m_mesh.hideFlags = HideFlags.HideAndDontSave;
                }

                return m_mesh;
            }
            set { m_mesh = value; }
        }
        private Mesh m_mesh;


        /// <summary>
        /// Reference to the parent Text Component.
        /// </summary>
        public TMP_Text textComponent
        {
            get
            {
                if (m_TextComponent == null)
                    m_TextComponent = GetComponentInParent<TextMeshProUGUI>();

                return m_TextComponent;
            }
        }
        [SerializeField]
        private TextMeshProUGUI m_TextComponent;


        [System.NonSerialized]
        private bool m_isRegisteredForEvents;
        private bool m_materialDirty;



        /// <summary>
        /// Function to add a new sub text object.
        /// </summary>
        /// <param name="textComponent"></param>
        /// <param name="materialReference"></param>
        /// <returns></returns>
        public static TMP_SubMeshUI AddSubTextObject(TextMeshProUGUI textComponent, MaterialReference materialReference)
        {
            GameObject go = new GameObject("TMP UI SubObject [" + materialReference.material.name + "]", typeof(RectTransform));
            go.hideFlags = HideFlags.DontSave;

            go.transform.SetParent(textComponent.transform, false);
            go.transform.SetAsFirstSibling();
            go.layer = textComponent.gameObject.layer;

            RectTransform rectTransform = go.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.pivot = textComponent.transform.pivot;

            go.AddComponent<LayoutIgnorer>();

            TMP_SubMeshUI subMesh = go.AddComponent<TMP_SubMeshUI>();

            //subMesh.canvasRenderer = subMesh.canvasRenderer;
            subMesh.m_TextComponent = textComponent;

            subMesh.m_fontAsset = materialReference.fontAsset;
            subMesh.m_isDefaultMaterial = materialReference.isDefaultMaterial;
            subMesh.SetSharedMaterial(materialReference.material);

            return subMesh;
        }



        /// <summary>
        ///
        /// </summary>
        protected override void OnEnable()
        {
            //Debug.Log("*** SubObject OnEnable() ***");

            // Register Callbacks for various events.
            if (!m_isRegisteredForEvents)
            {

            #if UNITY_EDITOR
                TMPro_EventManager.MATERIAL_PROPERTY_EVENT.Add(ON_MATERIAL_PROPERTY_CHANGED);
                TMPro_EventManager.DRAG_AND_DROP_MATERIAL_EVENT.Add(ON_DRAG_AND_DROP_MATERIAL);
            #endif

                m_isRegisteredForEvents = true;
            }

            // Update HideFlags on previously created sub text objects.
            if (hideFlags != HideFlags.DontSave)
                hideFlags = HideFlags.DontSave;

            //SetAllDirty();
        }


        void OnDestroy()
        {
            //Debug.Log("*** OnDestroy() ***");

            // Destroy Mesh
            if (m_mesh != null) DestroyImmediate(m_mesh);

            #if UNITY_EDITOR
            // Unregister the event this object was listening to
            TMPro_EventManager.MATERIAL_PROPERTY_EVENT.Remove(ON_MATERIAL_PROPERTY_CHANGED);
            TMPro_EventManager.DRAG_AND_DROP_MATERIAL_EVENT.Remove(ON_DRAG_AND_DROP_MATERIAL);
            #endif

            m_isRegisteredForEvents = false;

            // Notify parent text object
            if (m_TextComponent != null)
            {
                m_TextComponent.havePropertiesChanged = true;
                m_TextComponent.SetAllDirty();
            }
        }


        #if UNITY_EDITOR
        // Event received when custom material editor properties are changed.
        void ON_MATERIAL_PROPERTY_CHANGED(bool isChanged, Material mat)
        {
            //Debug.Log("*** ON_MATERIAL_PROPERTY_CHANGED ***");
            if (m_sharedMaterial == null)
                return;

            m_padding = GetPaddingForMaterial();

            SetVerticesDirty();
        }


        // Event to Track Material Changed resulting from Drag-n-drop.
        void ON_DRAG_AND_DROP_MATERIAL(GameObject obj, Material currentMaterial, Material newMaterial)
        {
            // Check if event applies to this current object
            #if UNITY_2018_2_OR_NEWER
            if (obj == gameObject || UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(gameObject) == obj)
            #else
            if (obj == gameObject || UnityEditor.PrefabUtility.GetPrefabParent(gameObject) == obj)
            #endif
            {
                if (!m_isDefaultMaterial) return;

                // Make sure we have a valid reference to the renderer.
                //if (m_canvasRenderer == null) m_canvasRenderer = GetComponent<CanvasRenderer>();

                UnityEditor.Undo.RecordObject(this, "Material Assignment");
                UnityEditor.Undo.RecordObject(canvasRenderer, "Material Assignment");

                SetSharedMaterial(newMaterial);
                m_TextComponent.havePropertiesChanged = true;
            }
        }
        #endif


        /// <summary>
        /// Function called when the padding value for the material needs to be re-calculated.
        /// </summary>
        /// <returns></returns>
        public float GetPaddingForMaterial()
        {
            float padding = ShaderUtilities.GetPadding(m_sharedMaterial, m_TextComponent.extraPadding, m_TextComponent.isUsingBold);

            return padding;
        }


        /// <summary>
        /// Function called when the padding value for the material needs to be re-calculated.
        /// </summary>
        /// <returns></returns>
        public float GetPaddingForMaterial(Material mat)
        {
            float padding = ShaderUtilities.GetPadding(mat, m_TextComponent.extraPadding, m_TextComponent.isUsingBold);

            return padding;
        }


        /// <summary>
        ///
        /// </summary>
        /// <param name="isExtraPadding"></param>
        /// <param name="isBold"></param>
        public void UpdateMeshPadding(bool isExtraPadding, bool isUsingBold)
        {
            m_padding = ShaderUtilities.GetPadding(m_sharedMaterial, isExtraPadding, isUsingBold);
        }


        /// <summary>
        ///
        /// </summary>
        public override void SetAllDirty()
        {
            //SetLayoutDirty();
            //SetVerticesDirty();
            //SetMaterialDirty();
        }


        /// <summary>
        ///
        /// </summary>
        public override void SetVerticesDirty()
        {
            if (!this.IsActive())
                return;

            // This is called on the parent TextMeshPro component.
            if (m_TextComponent != null)
            {
                m_TextComponent.havePropertiesChanged = true;
                m_TextComponent.SetVerticesDirty();
            }
        }


        /// <summary>
        ///
        /// </summary>
        public override void SetLayoutDirty()
        {
        }


        /// <summary>
        ///
        /// </summary>
        public override void SetMaterialDirty()
        {
            m_materialDirty = true;
            UpdateMaterial();
        }


        /// <summary>
        ///
        /// </summary>
        public void SetPivotDirty()
        {
            if (!this.IsActive())
                return;

            this.rectTransform.pivot = m_TextComponent.transform.pivot;
        }

        /// <summary>
        /// Override Cull function as this is handled by the parent text object.
        /// </summary>
        /// <param name="clipRect"></param>
        /// <param name="validRect"></param>
        public override void Cull(Rect clipRect, bool validRect)
        {
            // Do nothing as this functionality is handled by the parent text object.
        }


        /// <summary>
        ///
        /// </summary>
        protected override void UpdateGeometry()
        {
            // Need to override to prevent Unity from changing the geometry of the object.
            //Debug.Log("UpdateGeometry()");
        }


        public override void Rebuild()
        {
            if (!m_materialDirty) return;

            UpdateMaterial();
            m_materialDirty = false;
        }


        /// <summary>
        ///
        /// </summary>
        protected override void UpdateMaterial()
        {
            //Debug.Log("*** STO-UI - UpdateMaterial() *** FRAME (" + Time.frameCount + ")");

            if (m_sharedMaterial == null)
                return;

            //if (canvasRenderer == null) m_canvasRenderer = this.canvasRenderer;

            canvasRenderer.materialCount = 1;
            canvasRenderer.SetMaterial(materialForRendering, 0);
            //canvasRenderer.SetTexture(materialForRendering.mainTexture);

            #if UNITY_EDITOR
            if (m_sharedMaterial != null && gameObject.name != "TMP SubMeshUI [" + m_sharedMaterial.name + "]")
                gameObject.name = "TMP SubMeshUI [" + m_sharedMaterial.name + "]";
            #endif
        }


        // Function called internally when a new material is assigned via the fontMaterial property.
        Material GetMaterial(Material mat)
        {
            // Check in case Object is disabled. If so, we don't have a valid reference to the Renderer.
            // This can occur when the Duplicate Material Context menu is used on an inactive object.
            //if (m_renderer == null)
            //    m_renderer = GetComponent<Renderer>();

            // Create Instance Material only if the new material is not the same instance previously used.
            if (m_material == null || m_material.GetInstanceID() != mat.GetInstanceID())
                m_material = CreateMaterialInstance(mat);

            m_sharedMaterial = m_material;

            // Compute and Set new padding values for this new material.
            m_padding = GetPaddingForMaterial();

            SetVerticesDirty();
            SetMaterialDirty();

            return m_sharedMaterial;
        }


        /// <summary>
        /// Method used to create an instance of the material
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        Material CreateMaterialInstance(Material source)
        {
            Material mat = new Material(source);
            mat.shaderKeywords = source.shaderKeywords;
            mat.name += " (Instance)";

            return mat;
        }


        /// <summary>
        /// Method to set the shared material.
        /// </summary>
        /// <param name="mat"></param>
        void SetSharedMaterial(Material mat)
        {
            //Debug.Log("*** SetSharedMaterial UI() *** FRAME (" + Time.frameCount + ")");

            // Assign new material.
            m_sharedMaterial = mat;
            m_Material = m_sharedMaterial;

            // Compute and Set new padding values for this new material.
            m_padding = GetPaddingForMaterial();

            //SetVerticesDirty();
            SetMaterialDirty();
        }
    }
}
