using System;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TextCore;
using Object = UnityEngine.Object;


#pragma warning disable 0414 // Disabled a few warnings related to serialized variables not used in this script but used in the editor.

namespace TMPro
{
    public sealed partial class TextMeshPro
    {
        [SerializeField]
        private bool m_hasFontAssetChanged = false; // Used to track when font properties have changed.

        float m_previousLossyScaleY = -1; // Used for Tracking lossy scale changes in the transform;

        [SerializeField]
        private Renderer m_renderer;
        private MeshFilter m_meshFilter;

        private bool m_isFirstAllocation; // Flag to determine if this is the first allocation of the buffers.

        private TMP_SubMesh[] m_subTextObjects = new TMP_SubMesh[8];

        // Text Container / RectTransform Component
        private Rect m_RectTransformRect;

        [NonSerialized]
        private bool m_isRegisteredForEvents;

        // Profiler Marker declarations
        private static ProfilerMarker k_GenerateTextMarker = new ProfilerMarker("TMP Layout Text");
        private static ProfilerMarker k_SetArraySizesMarker = new ProfilerMarker("TMP.SetArraySizes");
        private static ProfilerMarker k_GenerateTextPhaseIMarker = new ProfilerMarker("TMP GenerateText - Phase I");
        private static ProfilerMarker k_ParseMarkupTextMarker = new ProfilerMarker("TMP Parse Markup Text");
        private static ProfilerMarker k_CharacterLookupMarker = new ProfilerMarker("TMP Lookup Character & Glyph Data");
        private static ProfilerMarker k_CalculateVerticesPositionMarker = new ProfilerMarker("TMP Calculate Vertices Position");
        private static ProfilerMarker k_ComputeTextMetricsMarker = new ProfilerMarker("TMP Compute Text Metrics");
        private static ProfilerMarker k_HandleVisibleCharacterMarker = new ProfilerMarker("TMP Handle Visible Character");
        private static ProfilerMarker k_HandleHorizontalLineBreakingMarker = new ProfilerMarker("TMP Handle Horizontal Line Breaking");
        private static ProfilerMarker k_HandleVerticalLineBreakingMarker = new ProfilerMarker("TMP Handle Vertical Line Breaking");
        private static ProfilerMarker k_SaveGlyphVertexDataMarker = new ProfilerMarker("TMP Save Glyph Vertex Data");
        private static ProfilerMarker k_ComputeCharacterAdvanceMarker = new ProfilerMarker("TMP Compute Character Advance");
        private static ProfilerMarker k_HandleCarriageReturnMarker = new ProfilerMarker("TMP Handle Carriage Return");
        private static ProfilerMarker k_HandleLineTerminationMarker = new ProfilerMarker("TMP Handle Line Termination");
        private static ProfilerMarker k_SavePageInfoMarker = new ProfilerMarker("TMP Save Text Extent & Page Info");
        private static ProfilerMarker k_SaveProcessingStatesMarker = new ProfilerMarker("TMP Save Processing States");
        private static ProfilerMarker k_GenerateTextPhaseIIMarker = new ProfilerMarker("TMP GenerateText - Phase II");
        private static ProfilerMarker k_GenerateTextPhaseIIIMarker = new ProfilerMarker("TMP GenerateText - Phase III");


        void Awake()
        {
            //Debug.Log("***** Awake() called on object ID " + GetInstanceID() + ". *****");

            // Cache Reference to the Mesh Renderer.
            m_renderer = GetComponent<Renderer>();
            if (m_renderer == null)
                m_renderer = gameObject.AddComponent<Renderer>();

            // Cache Reference to the transform;
            m_transform ??= (RectTransform) ((MonoBehaviour) this).transform;

            // Cache a reference to the Mesh Filter.
            m_meshFilter = GetComponent<MeshFilter>();
            if (m_meshFilter == null)
                m_meshFilter = gameObject.AddComponent<MeshFilter>();

            // Create new Mesh if necessary and cache reference to it.
            if (m_mesh == null)
            {
                m_mesh = new Mesh();
                m_mesh.hideFlags = HideFlags.HideAndDontSave;
                #if DEVELOPMENT_BUILD || UNITY_EDITOR
                m_mesh.name = "TextMeshPro Mesh";
                #endif
                m_meshFilter.sharedMesh = m_mesh;

                // Create new TextInfo for the text object.
                m_textInfo = new TMP_TextInfo(this.mesh);
            }
            m_meshFilter.hideFlags = HideFlags.HideInInspector | HideFlags.HideAndDontSave;

            #if UNITY_EDITOR
            // Special handling for the CanvasRenderer which used to be automatically added by the Graphic class.
            CanvasRenderer canvasRendererComponent = GetComponent<CanvasRenderer>();
            if (canvasRendererComponent != null)
            {
                Debug.LogWarning("Please remove the CanvasRenderer component from the [" + this.name + "] GameObject as this component is no longer necessary.", this);
                canvasRendererComponent.hideFlags = HideFlags.None;
            }
            #endif

            // Load TMP Settings for new text object instances.
            LoadDefaultSettings();

            // Load the font asset and assign material to renderer.
            LoadFontAsset();

            // Allocate our initial buffers.
            m_TextProcessingArray ??= new UnicodeChar[8];

            m_cached_TextElement = new TMP_Character();
            m_isFirstAllocation = true;

            // Check to make sure Sub Text Objects are tracked correctly in the event a Prefab is used.
            TMP_SubMesh[] subTextObjects = GetComponentsInChildren<TMP_SubMesh>();
            if (subTextObjects.Length > 0)
            {
                int subTextObjectCount = subTextObjects.Length;

                if (subTextObjectCount + 1 > m_subTextObjects.Length)
                    Array.Resize(ref m_subTextObjects, subTextObjectCount + 1);

                for (int i = 0; i < subTextObjectCount; i++)
                    m_subTextObjects[i + 1] = subTextObjects[i];
            }

            // Set flags to ensure our text is parsed and redrawn.
            m_havePropertiesChanged = true;

            m_isAwake = true;
        }


        protected override void OnEnable()
        {
            //Debug.Log("***** OnEnable() called on object ID " + GetInstanceID() + ". *****");

            // Return if Awake() has not been called on the text object.
            if (m_isAwake == false)
                return;

            // Register Callbacks for various events.
            if (!m_isRegisteredForEvents)
            {
                #if UNITY_EDITOR
                TMPro_EventManager.MATERIAL_PROPERTY_EVENT.Add(ON_MATERIAL_PROPERTY_CHANGED);
                TMPro_EventManager.FONT_PROPERTY_EVENT.Add(ON_FONT_PROPERTY_CHANGED);
                TMPro_EventManager.TEXTMESHPRO_PROPERTY_EVENT.Add(ON_TEXTMESHPRO_PROPERTY_CHANGED);
                TMPro_EventManager.DRAG_AND_DROP_MATERIAL_EVENT.Add(ON_DRAG_AND_DROP_MATERIAL);

                UnityEditor.PrefabUtility.prefabInstanceUpdated += OnPrefabInstanceUpdate;
                #endif
                m_isRegisteredForEvents = true;
            }

            // Register text object for internal updates
            if (m_IsTextObjectScaleStatic == false)
                TMP_UpdateManager.RegisterTextObjectForUpdate(this);

            meshFilter.sharedMesh = mesh;
            SetActiveSubMeshes(true);

            // Schedule potential text object update (if any of the properties have changed.
            ComputeMarginSize();

            SetAllDirty();

            //m_havePropertiesChanged = true;
        }


        protected override void OnDisable()
        {
            //Debug.Log("***** OnDisable() called on object ID " + GetInstanceID() + ". *****");

            // Return if Awake() has not been called on the text object.
            if (m_isAwake == false)
                return;

            TMP_UpdateManager.UnRegisterTextElementForRebuild(this);
            TMP_UpdateManager.UnRegisterTextObjectForUpdate(this);

            meshFilter.sharedMesh = null;
            SetActiveSubMeshes(false);
        }


        void OnDestroy()
        {
            //Debug.Log("***** OnDestroy() called on object ID " + GetInstanceID() + ". *****");

            // Destroy the mesh if we have one.
            if (m_mesh != null)
                DestroyImmediate(m_mesh);

            // Unregister the event this object was listening to
            #if UNITY_EDITOR
            TMPro_EventManager.MATERIAL_PROPERTY_EVENT.Remove(ON_MATERIAL_PROPERTY_CHANGED);
            TMPro_EventManager.FONT_PROPERTY_EVENT.Remove(ON_FONT_PROPERTY_CHANGED);
            TMPro_EventManager.TEXTMESHPRO_PROPERTY_EVENT.Remove(ON_TEXTMESHPRO_PROPERTY_CHANGED);
            TMPro_EventManager.DRAG_AND_DROP_MATERIAL_EVENT.Remove(ON_DRAG_AND_DROP_MATERIAL);

            UnityEditor.PrefabUtility.prefabInstanceUpdated -= OnPrefabInstanceUpdate;
            #endif

            m_isRegisteredForEvents = false;
            TMP_UpdateManager.UnRegisterTextElementForRebuild(this);
            TMP_UpdateManager.UnRegisterTextObjectForUpdate(this);
        }


        #if UNITY_EDITOR
        protected override void Reset()
        {
            //Debug.Log("***** Reset() called on object ID " + GetInstanceID() + ". *****");

            // Return if Awake() has not been called on the text object.
            if (m_isAwake == false)
                return;

            if (m_mesh != null)
                DestroyImmediate(m_mesh);

            Awake();
        }


        protected override void OnValidate()
        {
            //Debug.Log("***** OnValidate() called on object ID " + GetInstanceID() + ". *****", this);

            if (m_isAwake == false)
                return;

            if (meshFilter != null && m_meshFilter.hideFlags != (HideFlags.HideInInspector | HideFlags.HideAndDontSave))
                m_meshFilter.hideFlags = HideFlags.HideInInspector | HideFlags.HideAndDontSave;

            // Handle Font Asset changes in the inspector
            if (m_fontAsset == null || m_hasFontAssetChanged)
            {
                LoadFontAsset();
                m_hasFontAssetChanged = false;
            }

            m_padding = GetPaddingForMaterial();
            ComputeMarginSize();

            m_inputSource = TextInputSources.TextInputBox;
            m_havePropertiesChanged = true;
            m_isPreferredWidthDirty = true;
            m_isPreferredHeightDirty = true;

            SetAllDirty();
        }

        void OnBecameVisible()
        {
            // Keep the parent text object's renderer in sync with child sub objects' renderers.
            SetActiveSubTextObjectRenderers(true);
        }

        void OnBecameInvisible()
        {
            // Keep the parent text object's renderer in sync with child sub objects' renderers.
            SetActiveSubTextObjectRenderers(false);
        }


        /// <summary>
        /// Callback received when Prefabs are updated.
        /// </summary>
        /// <param name="go">The affected GameObject</param>
        void OnPrefabInstanceUpdate(GameObject go)
        {
            // Remove Callback if this prefab has been deleted.
            if (this == null)
            {
                UnityEditor.PrefabUtility.prefabInstanceUpdated -= OnPrefabInstanceUpdate;
                return;
            }

            if (go == this.gameObject)
            {
                TMP_SubMesh[] subTextObjects = GetComponentsInChildren<TMP_SubMesh>();
                if (subTextObjects.Length > 0)
                {
                    for (int i = 0; i < subTextObjects.Length; i++)
                        m_subTextObjects[i + 1] = subTextObjects[i];
                }
            }
        }


        // Event received when custom material editor properties are changed.
        void ON_MATERIAL_PROPERTY_CHANGED(bool isChanged, Material mat)
        {
            //Debug.Log("ON_MATERIAL_PROPERTY_CHANGED event received. Targeted Material is: " + mat.name + "  m_sharedMaterial: " + m_sharedMaterial.name + "  m_renderer.sharedMaterial: " + m_renderer.sharedMaterial);

            if (m_renderer.sharedMaterial == null)
            {
                if (m_fontAsset != null)
                {
                    m_renderer.sharedMaterial = m_fontAsset.material;
                    Debug.LogWarning("No Material was assigned to " + name + ". " + m_fontAsset.material.name + " was assigned.", this);
                }
                else
                    Debug.LogWarning("No Font Asset assigned to " + name + ". Please assign a Font Asset.", this);
            }

            if (m_renderer.sharedMaterial != m_sharedMaterial) //    || m_renderer.sharedMaterials.Contains(mat))
            {
                //Debug.Log("ON_MATERIAL_PROPERTY_CHANGED Called on Target ID: " + GetInstanceID() + ". Previous Material:" + m_sharedMaterial + "  New Material:" + m_renderer.sharedMaterial); // on Object ID:" + GetInstanceID() + ". m_sharedMaterial: " + m_sharedMaterial.name + "  m_renderer.sharedMaterial: " + m_renderer.sharedMaterial.name);
                m_sharedMaterial = m_renderer.sharedMaterial;
            }

            m_padding = GetPaddingForMaterial();

            UpdateMask();
            UpdateEnvMapMatrix();
            m_havePropertiesChanged = true;

            SetVerticesDirty();
        }


        // Event received when font asset properties are changed in Font Inspector
        void ON_FONT_PROPERTY_CHANGED(bool isChanged, Object fontAsset)
        {
            //Debug.Log("ON_FONT_PROPERTY_CHANGED event received. Target is [" + font.name + "]");

            if (MaterialReference.Contains(m_materialReferences, (TMP_FontAsset)fontAsset))
            {
                //Debug.Log("ON_FONT_PROPERTY_CHANGED event received.");
                m_havePropertiesChanged = true;

                UpdateMeshPadding();

                SetMaterialDirty();
                SetVerticesDirty();
            }
        }


        // Event received when UNDO / REDO Event alters the properties of the object.
        void ON_TEXTMESHPRO_PROPERTY_CHANGED(bool isChanged, Object textComponent)
        {
            if (textComponent == this)
            {
                //Debug.Log("Undo / Redo Event Received by Object ID:" + GetInstanceID());
                m_havePropertiesChanged = true;

                m_padding = GetPaddingForMaterial();
                ComputeMarginSize(); // Verify this change

                SetVerticesDirty();
            }
        }


        // Event to Track Material Changed resulting from Drag-n-drop.
        void ON_DRAG_AND_DROP_MATERIAL(GameObject obj, Material currentMaterial, Material newMaterial)
        {
            //Debug.Log("Drag-n-Drop Event - Receiving Object ID " + GetInstanceID()); // + ". Target Object ID " + obj.GetInstanceID() + ".  New Material is " + mat.name + " with ID " + mat.GetInstanceID() + ". Base Material is " + m_baseMaterial.name + " with ID " + m_baseMaterial.GetInstanceID());

            // Check if event applies to this current object
            if (obj == gameObject || UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(gameObject) == obj)
            {
                UnityEditor.Undo.RecordObject(this, "Material Assignment");
                UnityEditor.Undo.RecordObject(m_renderer, "Material Assignment");

                m_sharedMaterial = newMaterial;

                m_padding = GetPaddingForMaterial();
                m_havePropertiesChanged = true;

                SetVerticesDirty();
                SetMaterialDirty();
            }
        }
#endif


            // Function which loads either the default font or a newly assigned font asset. This function also assigned the appropriate material to the renderer.
        protected override void LoadFontAsset()
        {
            //Debug.Log("TextMeshPro LoadFontAsset() has been called."); // Current Font Asset is " + (font != null ? font.name: "Null") );

            ShaderUtilities.GetShaderPropertyIDs(); // Initialize & Get shader property IDs.

            if (m_fontAsset.characterLookupTable == null)
                m_fontAsset.ReadFontAssetDefinition();

            // If font atlas texture doesn't match the assigned material font atlas, switch back to default material specified in the Font Asset.
            if (m_sharedMaterial == null || m_sharedMaterial.GetTexture(ShaderUtilities.ID_MainTex) == null || m_fontAsset.atlasTexture.GetInstanceID() != m_sharedMaterial.GetTexture(ShaderUtilities.ID_MainTex).GetInstanceID())
            {
                if (m_fontAsset.material == null)
                    Debug.LogWarning("The Font Atlas Texture of the Font Asset " + m_fontAsset.name + " assigned to " + gameObject.name + " is missing.", this);
                else
                    m_sharedMaterial = m_fontAsset.material;
            }

            m_sharedMaterial.SetFloat(ShaderUtilities.ShaderTag_ZTestMode, 4);

            // Check if we are using the SDF Surface Shader
            if (m_sharedMaterial.passCount == 1)
            {
                m_renderer.receiveShadows = false;
                m_renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            m_padding = GetPaddingForMaterial();
            m_isMaskingEnabled = ShaderUtilities.IsMaskingEnabled(m_sharedMaterial);

            SetMaterialDirty();
        }


        void UpdateEnvMapMatrix()
        {
            if (!m_sharedMaterial.HasProperty(ShaderUtilities.ID_EnvMap) || m_sharedMaterial.GetTexture(ShaderUtilities.ID_EnvMap) == null)
                return;

            //Debug.Log("Updating Env Matrix...");
            Vector3 rotation = m_sharedMaterial.GetVector(ShaderUtilities.ID_EnvMatrixRotation);
            var mat = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(rotation), Vector3.one);
            m_sharedMaterial.SetMatrix(ShaderUtilities.ID_EnvMatrix, mat);
        }


        void UpdateMask()
        {
            if (m_isMaskingEnabled && m_fontMaterial == null)
            {
                CreateMaterialInstance();
            }
        }


        // Function called internally when a new material is assigned via the fontMaterial property.
        protected override Material GetMaterial(Material mat)
        {
            // Check in case Object is disabled. If so, we don't have a valid reference to the Renderer.
            // This can occur when the Duplicate Material Context menu is used on an inactive object.
            //if (m_renderer == null)
            //    m_renderer = GetComponent<Renderer>();

            // Create Instance Material only if the new material is not the same instance previously used.
            if (m_fontMaterial == null || m_fontMaterial.GetInstanceID() != mat.GetInstanceID())
                m_fontMaterial = CreateMaterialInstance(mat);

            m_sharedMaterial = m_fontMaterial;

            m_padding = GetPaddingForMaterial();

            SetVerticesDirty();
            SetMaterialDirty();

            return m_sharedMaterial;
        }


        // Function called internally when a new shared material is assigned via the fontSharedMaterial property.
        protected override void SetSharedMaterial(Material mat)
        {
            // Check in case Object is disabled. If so, we don't have a valid reference to the Renderer.
            // This can occur when the Duplicate Material Context menu is used on an inactive object.
            //if (m_renderer == null)
            //    m_renderer = GetComponent<Renderer>();

            m_sharedMaterial = mat;

            m_padding = GetPaddingForMaterial();

            SetMaterialDirty();
        }


        /// <summary>
        /// Method returning an array containing the materials used by the text object.
        /// </summary>
        /// <returns></returns>
        protected override Material[] GetSharedMaterials()
        {
            int materialCount = m_textInfo.materialCount;

            if (m_fontSharedMaterials == null)
                m_fontSharedMaterials = new Material[materialCount];
            else if (m_fontSharedMaterials.Length != materialCount)
                TMP_TextInfo.Resize(ref m_fontSharedMaterials, materialCount, false);

            for (int i = 0; i < materialCount; i++)
            {
                if (i == 0)
                    m_fontSharedMaterials[i] = m_sharedMaterial;
                else
                    m_fontSharedMaterials[i] = m_subTextObjects[i].sharedMaterial;
            }

            return m_fontSharedMaterials;
        }


        /// <summary>
        /// Method used to assign new materials to the text and sub text objects.
        /// </summary>
        protected override void SetSharedMaterials(Material[] materials)
        {
            int materialCount = m_textInfo.materialCount;

            // Check allocation of the fontSharedMaterials array.
            if (m_fontSharedMaterials == null)
                m_fontSharedMaterials = new Material[materialCount];
            else if (m_fontSharedMaterials.Length != materialCount)
                TMP_TextInfo.Resize(ref m_fontSharedMaterials, materialCount, false);

            // Only assign as many materials as the text object contains.
            for (int i = 0; i < materialCount; i++)
            {
                Texture mat_MainTex = materials[i].GetTexture(ShaderUtilities.ID_MainTex);

                if (i == 0)
                {
                    // Only assign new material if the font atlas textures match.
                    if ( mat_MainTex == null || mat_MainTex.GetInstanceID() != m_sharedMaterial.GetTexture(ShaderUtilities.ID_MainTex).GetInstanceID())
                        continue;

                    m_sharedMaterial = m_fontSharedMaterials[i] = materials[i];
                    m_padding = GetPaddingForMaterial(m_sharedMaterial);
                }
                else
                {
                    // Only assign new material if the font atlas textures match.
                    if (mat_MainTex == null || mat_MainTex.GetInstanceID() != m_subTextObjects[i].sharedMaterial.GetTexture(ShaderUtilities.ID_MainTex).GetInstanceID())
                        continue;

                    // Only assign a new material if none were specified in the text input.
                    if (m_subTextObjects[i].isDefaultMaterial)
                        m_subTextObjects[i].sharedMaterial = m_fontSharedMaterials[i] = materials[i];
                }
            }
        }


        // This function will create an instance of the Font Material.
        protected override void SetOutlineThickness(float thickness)
        {
            thickness = Mathf.Clamp01(thickness);
            m_renderer.material.SetFloat(ShaderUtilities.ID_OutlineWidth, thickness);

            if (m_fontMaterial == null)
                m_fontMaterial = m_renderer.material;

            m_fontMaterial = m_renderer.material;
            m_sharedMaterial = m_fontMaterial;
            m_padding = GetPaddingForMaterial();
        }


        // This function will create an instance of the Font Material.
        protected override void SetFaceColor(Color32 color)
        {
            m_renderer.material.SetColor(ShaderUtilities.ID_FaceColor, color);

            if (m_fontMaterial == null)
                m_fontMaterial = m_renderer.material;

            m_sharedMaterial = m_fontMaterial;
        }


        // This function will create an instance of the Font Material.
        protected override void SetOutlineColor(Color32 color)
        {
            m_renderer.material.SetColor(ShaderUtilities.ID_OutlineColor, color);

            if (m_fontMaterial == null)
                m_fontMaterial = m_renderer.material;

            //Debug.Log("Material ID:" + m_fontMaterial.GetInstanceID());
            m_sharedMaterial = m_fontMaterial;
        }


        // Function used to create an instance of the material
        void CreateMaterialInstance()
        {
            Material mat = new Material(m_sharedMaterial);
            mat.shaderKeywords = m_sharedMaterial.shaderKeywords;

            //mat.hideFlags = HideFlags.DontSave;
            mat.name += " Instance";

            m_fontMaterial = mat;
        }


        // Sets the Culling mode of the material
        protected override void SetCulling()
        {
            if (m_isCullingEnabled)
            {
                m_renderer.material.SetFloat("_CullMode", 2);

                for (int i = 1; i < m_subTextObjects.Length && m_subTextObjects[i] != null; i++)
                {
                    Renderer renderer = m_subTextObjects[i].renderer;

                    if (renderer != null)
                    {
                        renderer.material.SetFloat(ShaderUtilities.ShaderTag_CullMode, 2);
                    }
                }
            }
            else
            {
                m_renderer.material.SetFloat("_CullMode", 0);

                for (int i = 1; i < m_subTextObjects.Length && m_subTextObjects[i] != null; i++)
                {
                    Renderer renderer = m_subTextObjects[i].renderer;

                    if (renderer != null)
                    {
                        renderer.material.SetFloat(ShaderUtilities.ShaderTag_CullMode, 0);
                    }
                }
            }
        }


        // This function parses through the Char[] to determine how many characters will be visible. It then makes sure the arrays are large enough for all those characters.
        internal override int SetArraySizes(UnicodeChar[] unicodeChars)
        {
            k_SetArraySizesMarker.Begin();

            m_totalCharacterCount = 0;
            m_isUsingBold = false;
            m_FontStyleInternal = m_fontStyle;
            m_fontStyleStack.Clear();

            m_currentFontAsset = m_fontAsset;
            m_currentMaterial = m_sharedMaterial;
            m_currentMaterialIndex = 0;

            m_materialReferenceStack.SetDefault(new MaterialReference(m_currentFontAsset, m_currentMaterial));

            m_materialReferenceIndexLookup.Clear();
            MaterialReference.AddMaterialReference(m_currentMaterial, m_currentFontAsset, ref m_materialReferences, m_materialReferenceIndexLookup);

            // Set allocations for the text object's TextInfo
            if (m_textInfo == null)
                m_textInfo = new TMP_TextInfo(m_InternalTextProcessingArraySize);
            else if (m_textInfo.characterInfo.Length < m_InternalTextProcessingArraySize)
                TMP_TextInfo.Resize(ref m_textInfo.characterInfo, m_InternalTextProcessingArraySize, false);

            // Parsing XML tags in the text
            for (int i = 0; i < unicodeChars.Length && unicodeChars[i].unicode != 0; i++)
            {
                //Make sure the characterInfo array can hold the next text element.
                if (m_textInfo.characterInfo == null || m_totalCharacterCount >= m_textInfo.characterInfo.Length)
                    TMP_TextInfo.Resize(ref m_textInfo.characterInfo, m_totalCharacterCount + 1, true);

                int unicode = unicodeChars[i].unicode;

                // PARSE XML TAGS
                #region PARSE XML TAGS
                if (m_isRichText && unicode == 60) // if Char '<'
                {
                    // Check if Tag is Valid
                    if (ValidateHtmlTag(unicodeChars, i + 1, out var endTagIndex))
                    {
                        i = endTagIndex;

                        if ((m_FontStyleInternal & FontStyles.Bold) == FontStyles.Bold)
                            m_isUsingBold = true;

                        continue;
                    }
                }
                #endregion

                // Lookup the Glyph data for each character and cache it.
                // Replace missing glyph by the Square (9633) glyph or possibly the Space (32) glyph.
                var character = TMP_FontAssetUtilities.GetCharacterFromFontAsset((uint)unicode, 32, m_currentFontAsset);

                // Save text element data
                m_textInfo.characterInfo[m_totalCharacterCount].textElement = character;
                m_textInfo.characterInfo[m_totalCharacterCount].character = (char)unicode;
                m_textInfo.characterInfo[m_totalCharacterCount].fontAsset = m_currentFontAsset;

                // Handle Multi Atlas Texture support
                if (character != null && character.glyph.atlasIndex > 0)
                    throw new NotSupportedException("[TMP] Multi Atlas Texture is not supported.");

                if (!char.IsWhiteSpace((char)unicode) && unicode != 0x200B)
                {
                    // Limit the mesh of the main text object to 65535 vertices and use sub objects for the overflow.
                    if (m_materialReferences[m_currentMaterialIndex].referenceCount < 16383)
                        m_materialReferences[m_currentMaterialIndex].referenceCount += 1;
                    else
                    {
                        m_currentMaterialIndex = MaterialReference.AddMaterialReference(new Material(m_currentMaterial), m_currentFontAsset, ref m_materialReferences, m_materialReferenceIndexLookup);
                        m_materialReferences[m_currentMaterialIndex].referenceCount += 1;
                    }
                }

                m_textInfo.characterInfo[m_totalCharacterCount].material = m_currentMaterial;
                m_textInfo.characterInfo[m_totalCharacterCount].materialReferenceIndex = m_currentMaterialIndex;

                m_totalCharacterCount += 1;
            }

            // Early return if we are calculating the preferred values.
            if (m_isCalculatingPreferredValues)
            {
                m_isCalculatingPreferredValues = false;

                k_SetArraySizesMarker.End();
                return m_totalCharacterCount;
            }

            // Save material and sprite count.
            int materialCount = m_textInfo.materialCount = m_materialReferenceIndexLookup.Count;

            // Check if we need to resize the MeshInfo array for handling different materials.
            if (materialCount > m_textInfo.meshInfo.Length)
                TMP_TextInfo.Resize(ref m_textInfo.meshInfo, materialCount, false);

            // Resize SubTextObject array if necessary
            if (materialCount > m_subTextObjects.Length)
                TMP_TextInfo.Resize(ref m_subTextObjects, Mathf.NextPowerOfTwo(materialCount + 1));

            // Resize CharacterInfo[] if allocations are excessive
            if (m_VertexBufferAutoSizeReduction && m_textInfo.characterInfo.Length - m_totalCharacterCount > 256)
                TMP_TextInfo.Resize(ref m_textInfo.characterInfo, Mathf.Max(m_totalCharacterCount + 1, 256), true);


            // Iterate through the material references to set the mesh buffer allocations
            for (int i = 0; i < materialCount; i++)
            {
                // Add new sub text object for each material reference
                if (i > 0)
                {
                    if (m_subTextObjects[i] == null)
                    {
                        m_subTextObjects[i] = TMP_SubMesh.AddSubTextObject(this, m_materialReferences[i]);

                        // Not sure this is necessary
                        m_textInfo.meshInfo[i].vertices = null;
                    }
                    //else if (m_subTextObjects[i].gameObject.activeInHierarchy == false)
                    //    m_subTextObjects[i].gameObject.SetActive(true);

                    // Check if the material has changed.
                    if (m_subTextObjects[i].sharedMaterial == null || m_subTextObjects[i].sharedMaterial.GetInstanceID() != m_materialReferences[i].material.GetInstanceID())
                    {
                        m_subTextObjects[i].sharedMaterial = m_materialReferences[i].material;
                        m_subTextObjects[i].fontAsset = m_materialReferences[i].fontAsset;
                    }
                }

                int referenceCount = m_materialReferences[i].referenceCount;

                // Check to make sure our buffers allocations can accommodate the required text elements.
                if (m_textInfo.meshInfo[i].vertices == null || m_textInfo.meshInfo[i].vertices.Length < referenceCount * 4)
                {
                    if (m_textInfo.meshInfo[i].vertices == null)
                    {
                        if (i == 0)
                            m_textInfo.meshInfo[i] = new TMP_MeshInfo(m_mesh, referenceCount + 1);
                        else
                            m_textInfo.meshInfo[i] = new TMP_MeshInfo(m_subTextObjects[i].mesh, referenceCount + 1);
                    }
                    else
                        m_textInfo.meshInfo[i].ResizeMeshInfo(referenceCount > 1024 ? referenceCount + 256 : Mathf.NextPowerOfTwo(referenceCount + 1));
                }
                else if (m_VertexBufferAutoSizeReduction && referenceCount > 0 && m_textInfo.meshInfo[i].vertices.Length / 4 - referenceCount > 256)
                {
                    // Resize vertex buffers if allocations are excessive.
                    //Debug.Log("Reducing the size of the vertex buffers.");
                    m_textInfo.meshInfo[i].ResizeMeshInfo(referenceCount > 1024 ? referenceCount + 256 : Mathf.NextPowerOfTwo(referenceCount + 1));
                }
            }

            //TMP_MaterialManager.CleanupFallbackMaterials();

            // Clean up unused SubMeshes
            for (int i = materialCount; i < m_subTextObjects.Length && m_subTextObjects[i] != null; i++)
            {
                if (i < m_textInfo.meshInfo.Length)
                    m_textInfo.meshInfo[i].ClearUnusedVertices(0, true);
            }

            k_SetArraySizesMarker.End();
            return m_totalCharacterCount;
        }


        /// <summary>
        /// Update the margin width and height
        /// </summary>
        protected override void ComputeMarginSize()
        {
            if (this.transform != null)
            {
                //Debug.Log("*** ComputeMarginSize() *** Current RectTransform's Width is " + m_rectTransform.rect.width + " and Height is " + m_rectTransform.rect.height); // + " and size delta is "  + m_rectTransform.sizeDelta);
                Rect rect = m_transform.rect;

                m_marginWidth = rect.width;
                m_marginHeight = rect.height;

                // Cache current RectTransform width and pivot referenced in OnRectTransformDimensionsChange() to get around potential rounding error in the reported width of the RectTransform.
                m_PreviousRectTransformSize = rect.size;
                m_PreviousPivotPosition = m_transform.pivot;

                // Update the corners of the RectTransform
                m_RectTransformRect = m_RectTransformRect = transform.rect;
            }
        }


        /// <summary>
        ///
        /// </summary>
        protected override void OnDidApplyAnimationProperties()
        {
            //Debug.Log("*** OnDidApplyAnimationProperties() ***");
            m_havePropertiesChanged = true;
            isMaskUpdateRequired = true;

            SetVerticesDirty();
        }


        protected override void OnTransformParentChanged()
        {
            //Debug.Log("*** OnTransformParentChanged() ***");
            //ComputeMarginSize();

            SetVerticesDirty();
            SetLayoutDirty();
        }


        protected override void OnRectTransformDimensionsChange()
        {
            //Debug.Log("*** OnRectTransformDimensionsChange() ***");

            // Ignore changes to RectTransform SizeDelta that are very small and typically the result of rounding errors when using RectTransform in Anchor Stretch mode.
            if (transform != null &&
                Mathf.Abs(m_transform.rect.width - m_PreviousRectTransformSize.x) < 0.0001f && Mathf.Abs(m_transform.rect.height - m_PreviousRectTransformSize.y) < 0.0001f &&
                Mathf.Abs(m_transform.pivot.x - m_PreviousPivotPosition.x) < 0.0001f && Mathf.Abs(m_transform.pivot.y - m_PreviousPivotPosition.y) < 0.0001f)
            {
                return;
            }

            ComputeMarginSize();

            SetVerticesDirty();
            SetLayoutDirty();
        }


        /// <summary>
        /// Function used as a replacement for LateUpdate to check if the transform or scale of the text object has changed.
        /// </summary>
        internal override void InternalUpdate()
        {
            // We need to update the SDF scale or possibly regenerate the text object if lossy scale has changed.
            if (m_havePropertiesChanged == false)
            {
                float lossyScaleY = m_transform.lossyScale.y;

                // Ignore very small lossy scale changes as their effect on SDF Scale would not be visually noticeable.
                // Do not update SDF Scale if the text is null or empty
                if (Mathf.Abs(lossyScaleY - m_previousLossyScaleY) > 0.0001f && m_TextProcessingArray[0].unicode != 0)
                {
                    float scaleDelta = lossyScaleY / m_previousLossyScaleY;

                    UpdateSDFScale(scaleDelta);

                    m_previousLossyScaleY = lossyScaleY;
                }
            }
        }


        /// <summary>
        /// Function called when the text needs to be updated.
        /// </summary>
        void OnPreRenderObject()
        {
            //Debug.Log("*** OnPreRenderObject() called on object [" + this.name + "] ***");

            // Make sure object is active.
            if (!m_isAwake || (this.IsActive() == false && m_ignoreActiveState == false))
                return;

            // Check if we have a font asset assigned. Return if we don't because no one likes to see purple squares on screen.
            if (m_fontAsset == null)
            {
                Debug.LogWarning("Please assign a Font Asset to this " + transform.name + " gameobject.", this);
                return;
            }

            if (m_havePropertiesChanged || m_isLayoutDirty)
            {
                //Debug.Log("Properties have changed!"); // Assigned Material is:" + m_sharedMaterial); // New Text is: " + m_text + ".");

                if (isMaskUpdateRequired)
                {
                    UpdateMask();
                    isMaskUpdateRequired = false;
                }

                // Update mesh padding if necessary.
                if (checkPaddingRequired)
                    UpdateMeshPadding();

                // Reparse the text as input may have changed or been truncated.
                ParseInputText();

                // Reset Font min / max used with Auto-sizing
                if (m_enableAutoSizing)
                    m_fontSize = Mathf.Clamp(m_fontSizeBase, m_fontSizeMin, m_fontSizeMax);

                m_maxFontSize = m_fontSizeMax;
                m_minFontSize = m_fontSizeMin;
                m_lineSpacingDelta = 0;
                m_charWidthAdjDelta = 0;

                m_isTextTruncated = false;

                m_havePropertiesChanged = false;
                m_isLayoutDirty = false;
                m_ignoreActiveState = false;

                // Reset Text Auto Size iteration tracking.
                m_IsAutoSizePointSizeSet = false;
                m_AutoSizeIterationCount = 0;

                // Make sure state of MeshRenderer is mirrored on potential sub text objects.
                SetActiveSubTextObjectRenderers(m_renderer.enabled);

                // The GenerateTextMesh function is potentially called repeatedly when text auto size is enabled.
                // This is a revised implementation to remove the use of recursion which could potentially result in stack overflow issues.
                while (m_IsAutoSizePointSizeSet == false)
                {
                    GenerateTextMesh();
                    m_AutoSizeIterationCount += 1;
                }
            }
        }


        /// <summary>
        /// This is the main function that is responsible for creating / displaying the text.
        /// </summary>
        void GenerateTextMesh()
        {
            k_GenerateTextMarker.Begin();

            // Early exit if no font asset was assigned. This should not be needed since LiberationSans SDF will be assigned by default.
            if (m_fontAsset == null || m_fontAsset.characterLookupTable == null)
            {
                Debug.LogWarning("Can't Generate Mesh! No Font Asset has been assigned to Object ID: " + this.GetInstanceID());
                m_IsAutoSizePointSizeSet = true;
                k_GenerateTextMarker.End();
                return;
            }

            // Clear TextInfo
            if (m_textInfo != null)
                m_textInfo.Clear();

            // Early exit if we don't have any Text to generate.
            if (m_TextProcessingArray == null || m_TextProcessingArray.Length == 0 || m_TextProcessingArray[0].unicode == 0)
            {
                // Clear mesh and upload changes to the mesh.
                ClearMesh(true);

                m_preferredWidth = 0;
                m_preferredHeight = 0;

                // Event indicating the text has been regenerated.
                m_IsAutoSizePointSizeSet = true;
                k_GenerateTextMarker.End();
                return;
            }

            m_currentFontAsset = m_fontAsset;
            m_currentMaterial = m_sharedMaterial;
            m_currentMaterialIndex = 0;
            m_materialReferenceStack.SetDefault(new MaterialReference(m_currentFontAsset, m_currentMaterial));

            // Total character count is computed when the text is parsed.
            int totalCharacterCount = m_totalCharacterCount;

            // Calculate the scale of the font based on selected font size and sampling point size.
            // baseScale is calculated using the font asset assigned to the text object.
            float baseScale = (m_fontSize / m_fontAsset.m_FaceInfo.pointSize * m_fontAsset.m_FaceInfo.scale * (m_isOrthographic ? 1 : 0.1f));
            float currentEmScale = m_fontSize * 0.01f * (m_isOrthographic ? 1 : 0.1f);

            m_currentFontSize = m_fontSize;
            m_sizeStack.SetDefault(m_currentFontSize);
            float fontSizeDelta = 0;

            int charCode = 0; // Holds the character code of the currently being processed character.

            m_FontStyleInternal = m_fontStyle; // Set the default style.
            m_fontStyleStack.Clear();

            m_lineJustification = m_HorizontalAlignment; // m_textAlignment; // Sets the line justification mode to match editor alignment.
            m_lineJustificationStack.SetDefault(m_lineJustification);

            float padding = 0;
            float style_padding = 0; // Extra padding required to accommodate Bold style.
            float boldSpacingAdjustment = 0;
            //float bold_xAdvance_multiplier = 1; // Used to increase spacing between character when style is bold.

            m_baselineOffset = 0; // Used by subscript characters.
            m_baselineOffsetStack.Clear();

            m_fontColor32 = m_fontColor;
            m_htmlColor = m_fontColor32;

            m_colorStack.SetDefault(m_htmlColor);

            m_ItalicAngle = m_currentFontAsset.italicStyle;
            m_ItalicAngleStack.SetDefault(m_ItalicAngle);

            m_lineOffset = 0; // Amount of space between lines (font line spacing + m_linespacing).
            m_lineHeight = TMP_Math.FLOAT_UNSET;
            float lineGap = m_currentFontAsset.m_FaceInfo.lineHeight - (m_currentFontAsset.m_FaceInfo.ascentLine - m_currentFontAsset.m_FaceInfo.descentLine);

            m_cSpacing = 0; // Amount of space added between characters as a result of the use of the <cspace> tag.
            m_monoSpacing = 0;
            m_xAdvance = 0; // Used to track the position of each character.

            m_characterCount = 0; // Total characters in the char[]

            // Tracking of line information
            m_lastCharacterOfLine = 0;
            m_firstVisibleCharacterOfLine = 0;
            m_lastVisibleCharacterOfLine = 0;
            m_maxLineAscender = k_LargeNegativeFloat;
            m_maxLineDescender = k_LargePositiveFloat;
            m_lineNumber = 0;
            m_startOfLineAscender = 0;
            m_lineVisibleCharacterCount = 0;
            bool isStartOfNewLine = true;
            m_IsDrivenLineSpacing = false;
            m_firstOverflowCharacterIndex = -1;

            float marginWidth = m_marginWidth > 0 ? m_marginWidth : 0;
            float marginHeight = m_marginHeight > 0 ? m_marginHeight : 0;
            m_width = -1;
            float widthOfTextArea = marginWidth + 0.0001f;

            // Need to initialize these Extents structures
            m_meshExtents.min = k_LargePositiveVector2;
            m_meshExtents.max = k_LargeNegativeVector2;

            // Initialize lineInfo
            m_textInfo.ClearLineInfo();

            // Tracking of the highest Ascender
            m_maxCapHeight = 0;
            m_maxTextAscender = 0;
            m_ElementDescender = 0;
            m_PageAscender = 0;
            float maxVisibleDescender = 0;
            bool isMaxVisibleDescenderSet = false;

            // Initialize struct to track states of word wrapping
            bool isFirstWordOfLine = true;
            int lastSoftLineBreak = 0;

            CharacterSubstitution characterToSubstitute = new CharacterSubstitution(-1, 0);
            bool isSoftHyphenIgnored = false;

            // Save character and line state before we begin layout.
            SaveWordWrappingState(ref m_SavedWordWrapState, -1, -1);
            SaveWordWrappingState(ref m_SavedLineState, -1, -1);
            SaveWordWrappingState(ref m_SavedLastValidState, -1, -1);
            SaveWordWrappingState(ref m_SavedSoftLineBreakState, -1, -1);

            k_GenerateTextPhaseIMarker.Begin();

            // Parse through Character buffer to read HTML tags and begin creating mesh.
            for (int i = 0; i < m_TextProcessingArray.Length && m_TextProcessingArray[i].unicode != 0; i++)
            {
                charCode = m_TextProcessingArray[i].unicode;

                // Parse Rich Text Tag
                #region Parse Rich Text Tag
                if (m_isRichText && charCode == 60)  // '<'
                {
                    k_ParseMarkupTextMarker.Begin();

                    int endTagIndex;

                    // Check if Tag is valid. If valid, skip to the end of the validated tag.
                    if (ValidateHtmlTag(m_TextProcessingArray, i + 1, out endTagIndex))
                    {
                        i = endTagIndex;

                        // Continue to next character or handle the sprite element
                        k_ParseMarkupTextMarker.End();
                        continue;
                    }
                    k_ParseMarkupTextMarker.End();
                }
                else
                {
                    m_currentMaterialIndex = m_textInfo.characterInfo[m_characterCount].materialReferenceIndex;
                    m_currentFontAsset = m_textInfo.characterInfo[m_characterCount].fontAsset;
                }
                #endregion End Parse Rich Text Tag

                // Handle potential character substitutions
                #region Character Substitutions
                bool isInjectingCharacter = false;

                if (characterToSubstitute.index == m_characterCount)
                {
                    charCode = (int)characterToSubstitute.unicode;
                    isInjectingCharacter = true;

                    switch (charCode)
                    {
                        case 0x03:
                            m_textInfo.characterInfo[m_characterCount].textElement = m_currentFontAsset.characterLookupTable[0x03];
                            m_isTextTruncated = true;
                            break;
                        case 0x2D:
                            //
                            break;
                        case 0x2026:
                            throw new NotSupportedException();
                    }
                }
                #endregion


                // Look up Character Data from Dictionary and cache it.
                #region Look up Character Data
                k_CharacterLookupMarker.Begin();

                float baselineOffset = 0;
                float elementAscentLine = 0;
                float elementDescentLine = 0;
                float currentElementScale;
                {
                    m_cached_TextElement = m_textInfo.characterInfo[m_characterCount].textElement;
                    if (m_cached_TextElement == null)
                    {
                        k_CharacterLookupMarker.End();
                        continue;
                    }

                    m_currentFontAsset = m_textInfo.characterInfo[m_characterCount].fontAsset;
                    m_currentMaterial = m_textInfo.characterInfo[m_characterCount].material;
                    m_currentMaterialIndex = m_textInfo.characterInfo[m_characterCount].materialReferenceIndex;

                    // Special handling if replaced character was a line feed where in this case we have to use the scale of the previous character.
                    float adjustedScale;
                    if (isInjectingCharacter && m_TextProcessingArray[i].unicode == 0x0A && m_characterCount != m_firstCharacterOfLine)
                        adjustedScale = m_textInfo.characterInfo[m_characterCount - 1].pointSize / m_currentFontAsset.m_FaceInfo.pointSize * m_currentFontAsset.m_FaceInfo.scale * (m_isOrthographic ? 1 : 0.1f);
                    else
                        adjustedScale = m_currentFontSize / m_currentFontAsset.m_FaceInfo.pointSize * m_currentFontAsset.m_FaceInfo.scale * (m_isOrthographic ? 1 : 0.1f);

                    // Special handling for injected Ellipsis
                    if (isInjectingCharacter && charCode == 0x2026)
                    {
                        elementAscentLine = 0;
                        elementDescentLine = 0;
                    }
                    else
                    {
                        elementAscentLine = m_currentFontAsset.m_FaceInfo.ascentLine;
                        elementDescentLine = m_currentFontAsset.m_FaceInfo.descentLine;
                    }

                    currentElementScale = adjustedScale * m_cached_TextElement.m_Glyph.scale;
                    baselineOffset = m_currentFontAsset.m_FaceInfo.baseline * adjustedScale * m_currentFontAsset.m_FaceInfo.scale;

                    m_textInfo.characterInfo[m_characterCount].scale = currentElementScale;

                    padding = m_currentMaterialIndex == 0 ? m_padding : m_subTextObjects[m_currentMaterialIndex].padding;
                }
                k_CharacterLookupMarker.End();
                #endregion


                // Handle Soft Hyphen
                #region Handle Soft Hyphen
                float currentElementUnmodifiedScale = currentElementScale;
                if (charCode is 0xAD or 0x03)
                    currentElementScale = 0;
                #endregion


                // Store some of the text object's information
                m_textInfo.characterInfo[m_characterCount].character = (char)charCode;
                m_textInfo.characterInfo[m_characterCount].pointSize = m_currentFontSize;
                m_textInfo.characterInfo[m_characterCount].style = m_FontStyleInternal;

                // Cache glyph metrics
                GlyphMetrics currentGlyphMetrics = m_cached_TextElement.m_Glyph.metrics;

                // Optimization to avoid calling this more than once per character.
                bool isWhiteSpace = charCode <= 0xFFFF && char.IsWhiteSpace((char)charCode);

                // Handle Kerning if Enabled.
                #region Handle Kerning
                TMP_GlyphValueRecord glyphAdjustments = new TMP_GlyphValueRecord();
                float characterSpacingAdjustment = m_characterSpacing;
                m_GlyphHorizontalAdvanceAdjustment = 0;
                if (m_enableKerning)
                    m_GlyphHorizontalAdvanceAdjustment = glyphAdjustments.xAdvance;
                #endregion


                // Handle Mono Spacing
                #region Handle Mono Spacing
                float monoAdvance = 0;
                if (m_monoSpacing != 0)
                {
                    monoAdvance = (m_monoSpacing / 2 - (currentGlyphMetrics.width / 2 + currentGlyphMetrics.horizontalBearingX) * currentElementScale) * (1 - m_charWidthAdjDelta);
                    m_xAdvance += monoAdvance;
                }
                #endregion


                // Set Padding based on selected font style
                #region Handle Style Padding
                if ((m_FontStyleInternal & FontStyles.Bold) == FontStyles.Bold) // Checks for any combination of Bold Style.
                {
                    if (m_currentMaterial != null && m_currentMaterial.HasProperty(ShaderUtilities.ID_GradientScale))
                    {
                        float gradientScale = m_currentMaterial.GetFloat(ShaderUtilities.ID_GradientScale);
                        style_padding = m_currentFontAsset.boldStyle / 4.0f * gradientScale * m_currentMaterial.GetFloat(ShaderUtilities.ID_ScaleRatio_A);

                        // Clamp overall padding to Gradient Scale size.
                        if (style_padding + padding > gradientScale)
                            padding = gradientScale - style_padding;
                    }
                    else
                        style_padding = 0;

                    boldSpacingAdjustment = m_currentFontAsset.boldSpacing;
                }
                else
                {
                    if (m_currentMaterial != null && m_currentMaterial.HasProperty(ShaderUtilities.ID_GradientScale) && m_currentMaterial.HasProperty(ShaderUtilities.ID_ScaleRatio_A))
                    {
                        float gradientScale = m_currentMaterial.GetFloat(ShaderUtilities.ID_GradientScale);
                        style_padding = m_currentFontAsset.normalStyle / 4.0f * gradientScale * m_currentMaterial.GetFloat(ShaderUtilities.ID_ScaleRatio_A);

                        // Clamp overall padding to Gradient Scale size.
                        if (style_padding + padding > gradientScale)
                            padding = gradientScale - style_padding;
                    }
                    else
                        style_padding = 0;

                    boldSpacingAdjustment = 0;
                }
                #endregion Handle Style Padding


                // Determine the position of the vertices of the Character or Sprite.
                #region Calculate Vertices Position
                k_CalculateVerticesPositionMarker.Begin();
                Vector2 top_left;
                top_left.x = m_xAdvance + ((currentGlyphMetrics.horizontalBearingX - padding - style_padding + glyphAdjustments.m_XPlacement) * currentElementScale * (1 - m_charWidthAdjDelta));
                top_left.y = baselineOffset + (currentGlyphMetrics.horizontalBearingY + padding + glyphAdjustments.m_YPlacement) * currentElementScale - m_lineOffset + m_baselineOffset;

                Vector2 bottom_left;
                bottom_left.x = top_left.x;
                bottom_left.y = top_left.y - ((currentGlyphMetrics.height + padding * 2) * currentElementScale);

                Vector2 top_right;
                top_right.x = bottom_left.x + ((currentGlyphMetrics.width + padding * 2 + style_padding * 2) * currentElementScale * (1 - m_charWidthAdjDelta));
                top_right.y = top_left.y;

                Vector2 bottom_right;
                bottom_right.x = top_right.x;
                bottom_right.y = bottom_left.y;

                k_CalculateVerticesPositionMarker.End();
                #endregion


                // Check if we need to Shear the rectangles for Italic styles
                #region Handle Italic & Shearing
                if ((m_FontStyleInternal & FontStyles.Italic) == FontStyles.Italic)
                {
                    // Shift Top vertices forward by half (Shear Value * height of character) and Bottom vertices back by same amount.
                    float shear_value = m_ItalicAngle * 0.01f;
                    Vector2 topShear = new Vector2(shear_value * ((currentGlyphMetrics.horizontalBearingY + padding + style_padding) * currentElementScale), 0);
                    Vector2 bottomShear = new Vector2(shear_value * (((currentGlyphMetrics.horizontalBearingY - currentGlyphMetrics.height - padding - style_padding)) * currentElementScale), 0);

                    Vector2 shearAdjustment = new Vector2((topShear.x - bottomShear.x) / 2, 0);

                    top_left = top_left + topShear - shearAdjustment;
                    bottom_left = bottom_left + bottomShear - shearAdjustment;
                    top_right = top_right + topShear - shearAdjustment;
                    bottom_right = bottom_right + bottomShear - shearAdjustment;
                }
                #endregion Handle Italics & Shearing


                // Store vertex information for the character or sprite.
                m_textInfo.characterInfo[m_characterCount].bottomLeft = bottom_left;
                m_textInfo.characterInfo[m_characterCount].topLeft = top_left;
                m_textInfo.characterInfo[m_characterCount].topRight = top_right;
                m_textInfo.characterInfo[m_characterCount].bottomRight = bottom_right;


                // Compute text metrics
                #region Compute Ascender & Descender values
                k_ComputeTextMetricsMarker.Begin();
                // Element Ascender in line space
                float elementAscender = elementAscentLine * currentElementScale + m_baselineOffset;

                // Element Descender in line space
                float elementDescender = elementDescentLine * currentElementScale + m_baselineOffset;

                float adjustedAscender = elementAscender;
                float adjustedDescender = elementDescender;

                bool isFirstCharacterOfLine = m_characterCount == m_firstCharacterOfLine;
                // Max line ascender and descender in line space
                if (isFirstCharacterOfLine || isWhiteSpace == false)
                {
                    // Special handling for Superscript and Subscript where we use the unadjusted line ascender and descender
                    if (m_baselineOffset != 0)
                    {
                        adjustedAscender = Mathf.Max(elementAscender - m_baselineOffset, adjustedAscender);
                        adjustedDescender = Mathf.Min(elementDescender - m_baselineOffset, adjustedDescender);
                    }

                    m_maxLineAscender = Mathf.Max(adjustedAscender, m_maxLineAscender);
                    m_maxLineDescender = Mathf.Min(adjustedDescender, m_maxLineDescender);
                }

                // Element Ascender and Descender in object space
                if (isFirstCharacterOfLine || isWhiteSpace == false)
                {
                    m_textInfo.characterInfo[m_characterCount].adjustedAscender = adjustedAscender;
                    m_textInfo.characterInfo[m_characterCount].adjustedDescender = adjustedDescender;

                    m_ElementDescender = elementDescender - m_lineOffset;
                }
                else
                {
                    m_textInfo.characterInfo[m_characterCount].adjustedAscender = m_maxLineAscender;
                    m_textInfo.characterInfo[m_characterCount].adjustedDescender = m_maxLineDescender;

                    m_ElementDescender = m_maxLineDescender - m_lineOffset;
                }

                // Max text object ascender and cap height
                if (m_lineNumber == 0)
                {
                    if (isFirstCharacterOfLine || isWhiteSpace == false)
                    {
                        m_maxTextAscender = m_maxLineAscender;
                        m_maxCapHeight = Mathf.Max(m_maxCapHeight, m_currentFontAsset.m_FaceInfo.capLine * currentElementScale);
                    }
                }

                // Page ascender
                if (m_lineOffset == 0)
                {
                    if (isFirstCharacterOfLine || isWhiteSpace == false)
                        m_PageAscender = m_PageAscender > elementAscender ? m_PageAscender : elementAscender;
                }
                k_ComputeTextMetricsMarker.End();
                #endregion


                // Set Characters to not visible by default.
                m_textInfo.characterInfo[m_characterCount].isVisible = false;

                // Setup Mesh for visible text elements. ie. not a SPACE / LINEFEED / CARRIAGE RETURN.
                #region Handle Visible Characters
                if (charCode == 9 || (isWhiteSpace == false && charCode != 0x200B && charCode != 0xAD && charCode != 0x03) || (charCode == 0xAD && isSoftHyphenIgnored == false))
                {
                    k_HandleVisibleCharacterMarker.Begin();

                    m_textInfo.characterInfo[m_characterCount].isVisible = true;

                    widthOfTextArea = m_width != -1 ? Mathf.Min(marginWidth + 0.0001f, m_width) : marginWidth + 0.0001f;

                    // Calculate the line breaking width of the text.
                    float textWidth = Mathf.Abs(m_xAdvance) + currentGlyphMetrics.horizontalAdvance * (1 - m_charWidthAdjDelta) * (charCode == 0xAD ? currentElementUnmodifiedScale : currentElementScale);
                    float textHeight = m_maxTextAscender - (m_maxLineDescender - m_lineOffset) + (m_lineOffset > 0 && m_IsDrivenLineSpacing == false ? m_maxLineAscender - m_startOfLineAscender : 0);

                    int testedCharacterCount = m_characterCount;

                    // Handling of current line Vertical Bounds
                    #region Current Line Vertical Bounds Check
                    if (textHeight > marginHeight + 0.0001f)
                    {
                        k_HandleVerticalLineBreakingMarker.Begin();

                        // Set isTextOverflowing and firstOverflowCharacterIndex
                        if (m_firstOverflowCharacterIndex == -1)
                            m_firstOverflowCharacterIndex = m_characterCount;

                        // Check if Auto-Size is enabled
                        if (m_enableAutoSizing)
                        {
                            // Handle Line spacing adjustments
                            #region Line Spacing Adjustments
                            if (m_lineSpacingDelta > m_lineSpacingMax && m_lineOffset > 0 && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                            {
                                float adjustmentDelta = (marginHeight - textHeight) / m_lineNumber;

                                m_lineSpacingDelta = Mathf.Max(m_lineSpacingDelta + adjustmentDelta / baseScale, m_lineSpacingMax);

                                //Debug.Log("[" + m_AutoSizeIterationCount + "] Reducing Line Spacing. Delta of [" + m_lineSpacingDelta.ToString("f3") + "].");
                                k_HandleVerticalLineBreakingMarker.End();
                                k_HandleVisibleCharacterMarker.End();
                                k_GenerateTextPhaseIMarker.End();
                                k_GenerateTextMarker.End();
                                return;
                            }
                            #endregion


                            // Handle Text Auto-sizing resulting from text exceeding vertical bounds.
                            #region Text Auto-Sizing (Text greater than vertical bounds)
                            if (m_fontSize > m_fontSizeMin && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                            {
                                m_maxFontSize = m_fontSize;

                                float sizeDelta = Mathf.Max((m_fontSize - m_minFontSize) / 2, 0.05f);
                                m_fontSize -= sizeDelta;
                                m_fontSize = Mathf.Max((int)(m_fontSize * 20 + 0.5f) / 20f, m_fontSizeMin);

                                //Debug.Log("[" + m_AutoSizeIterationCount + "] Reducing Point Size from [" + m_maxFontSize.ToString("f3") + "] to [" + m_fontSize.ToString("f3") + "] with delta of [" + sizeDelta.ToString("f3") + "].");
                                k_HandleVerticalLineBreakingMarker.End();
                                k_HandleVisibleCharacterMarker.End();
                                k_GenerateTextPhaseIMarker.End();
                                k_GenerateTextMarker.End();
                                return;
                            }
                            #endregion Text Auto-Sizing
                        }

                        // Handle Vertical Overflow on current line
                        switch (m_overflowMode)
                        {
                            case TextOverflowModes.Overflow:
                                // Nothing happens as vertical bounds are ignored in this mode.
                                break;

                            case TextOverflowModes.Truncate:
                                i = RestoreWordWrappingState(ref m_SavedLastValidState);

                                characterToSubstitute.index = testedCharacterCount;
                                characterToSubstitute.unicode = 0x03;
                                k_HandleVerticalLineBreakingMarker.End();
                                k_HandleVisibleCharacterMarker.End();
                                continue;
                        }

                        k_HandleVerticalLineBreakingMarker.End();
                    }
                    #endregion


                    // Handling of Horizontal Bounds
                    #region Current Line Horizontal Bounds Check
                    if (textWidth > widthOfTextArea)
                    {
                        k_HandleHorizontalLineBreakingMarker.Begin();

                        // Handle Line Breaking (if still possible)
                        if (m_enableWordWrapping && m_characterCount != m_firstCharacterOfLine)
                        {
                            // Restore state to previous safe line breaking
                            i = RestoreWordWrappingState(ref m_SavedWordWrapState);

                            // Compute potential new line offset in the event a line break is needed.
                            float lineOffsetDelta = 0;
                            if (m_lineHeight == TMP_Math.FLOAT_UNSET)
                            {
                                float ascender = m_textInfo.characterInfo[m_characterCount].adjustedAscender;
                                lineOffsetDelta = (m_lineOffset > 0 && m_IsDrivenLineSpacing == false ? m_maxLineAscender - m_startOfLineAscender : 0) - m_maxLineDescender + ascender + (lineGap + m_lineSpacingDelta) * baseScale + m_lineSpacing * currentEmScale;
                            }
                            else
                            {
                                lineOffsetDelta = m_lineHeight + m_lineSpacing * currentEmScale;
                                m_IsDrivenLineSpacing = true;
                            }

                            // Calculate new text height
                            float newTextHeight = m_maxTextAscender + lineOffsetDelta + m_lineOffset - m_textInfo.characterInfo[m_characterCount].adjustedDescender;

                            // Replace Soft Hyphen by Hyphen Minus 0x2D
                            #region Handle Soft Hyphenation
                            if (m_textInfo.characterInfo[m_characterCount - 1].character == 0xAD && isSoftHyphenIgnored == false)
                            {
                                // Only inject Hyphen Minus if new line is possible
                                if (m_overflowMode == TextOverflowModes.Overflow || newTextHeight < marginHeight + 0.0001f)
                                {
                                    characterToSubstitute.index = m_characterCount - 1;
                                    characterToSubstitute.unicode = 0x2D;

                                    i -= 1;
                                    m_characterCount -= 1;
                                    k_HandleHorizontalLineBreakingMarker.End();
                                    k_HandleVisibleCharacterMarker.End();
                                    continue;
                                }
                            }

                            isSoftHyphenIgnored = false;

                            // Ignore Soft Hyphen to prevent it from wrapping
                            if (m_textInfo.characterInfo[m_characterCount].character == 0xAD)
                            {
                                isSoftHyphenIgnored = true;
                                k_HandleHorizontalLineBreakingMarker.End();
                                k_HandleVisibleCharacterMarker.End();
                                continue;
                            }
                            #endregion

                            // Adjust character spacing before breaking up word if auto size is enabled
                            if (m_enableAutoSizing && isFirstWordOfLine)
                            {
                                // Handle Character Width Adjustments
                                #region Character Width Adjustments
                                if (m_charWidthAdjDelta < m_charWidthMaxAdj / 100 && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                                {
                                    float adjustedTextWidth = textWidth;

                                    // Determine full width of the text
                                    if (m_charWidthAdjDelta > 0)
                                        adjustedTextWidth /= 1f - m_charWidthAdjDelta;

                                    float adjustmentDelta = textWidth - (widthOfTextArea - 0.0001f);
                                    m_charWidthAdjDelta += adjustmentDelta / adjustedTextWidth;
                                    m_charWidthAdjDelta = Mathf.Min(m_charWidthAdjDelta, m_charWidthMaxAdj / 100);

                                    //Debug.Log("[" + m_AutoSizeIterationCount + "] Reducing Character Width by " + (m_charWidthAdjDelta * 100) + "%");
                                    k_HandleHorizontalLineBreakingMarker.End();
                                    k_HandleVisibleCharacterMarker.End();
                                    k_GenerateTextPhaseIMarker.End();
                                    k_GenerateTextMarker.End();
                                    return;
                                }
                                #endregion

                                // Handle Text Auto-sizing resulting from text exceeding vertical bounds.
                                #region Text Auto-Sizing (Text greater than vertical bounds)
                                if (m_fontSize > m_fontSizeMin && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                                {
                                    m_maxFontSize = m_fontSize;

                                    float sizeDelta = Mathf.Max((m_fontSize - m_minFontSize) / 2, 0.05f);
                                    m_fontSize -= sizeDelta;
                                    m_fontSize = Mathf.Max((int)(m_fontSize * 20 + 0.5f) / 20f, m_fontSizeMin);

                                    //Debug.Log("[" + m_AutoSizeIterationCount + "] Reducing Point Size from [" + m_maxFontSize.ToString("f3") + "] to [" + m_fontSize.ToString("f3") + "] with delta of [" + sizeDelta.ToString("f3") + "].");
                                    k_HandleHorizontalLineBreakingMarker.End();
                                    k_HandleVisibleCharacterMarker.End();
                                    k_GenerateTextPhaseIMarker.End();
                                    k_GenerateTextMarker.End();
                                    return;
                                }
                                #endregion Text Auto-Sizing
                            }


                            // Special handling if first word of line and non breaking space
                            int savedSoftLineBreakingSpace = m_SavedSoftLineBreakState.previous_WordBreak;
                            if (isFirstWordOfLine && savedSoftLineBreakingSpace != -1)
                            {
                                if (savedSoftLineBreakingSpace != lastSoftLineBreak)
                                {
                                    i = RestoreWordWrappingState(ref m_SavedSoftLineBreakState);
                                    lastSoftLineBreak = savedSoftLineBreakingSpace;

                                    // check if soft hyphen
                                    if (m_textInfo.characterInfo[m_characterCount - 1].character == 0xAD)
                                    {
                                        characterToSubstitute.index = m_characterCount - 1;
                                        characterToSubstitute.unicode = 0x2D;

                                        i -= 1;
                                        m_characterCount -= 1;
                                        k_HandleHorizontalLineBreakingMarker.End();
                                        k_HandleVisibleCharacterMarker.End();
                                        continue;
                                    }
                                }
                            }

                            // Determine if new line of text would exceed the vertical bounds of text container
                            if (newTextHeight > marginHeight + 0.0001f)
                            {
                                k_HandleVerticalLineBreakingMarker.Begin();

                                // Set isTextOverflowing and firstOverflowCharacterIndex
                                if (m_firstOverflowCharacterIndex == -1)
                                    m_firstOverflowCharacterIndex = m_characterCount;

                                // Check if Auto-Size is enabled
                                if (m_enableAutoSizing)
                                {
                                    // Handle Line spacing adjustments
                                    #region Line Spacing Adjustments
                                    if (m_lineSpacingDelta > m_lineSpacingMax && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                                    {
                                        float adjustmentDelta = (marginHeight - newTextHeight) / (m_lineNumber + 1);

                                        m_lineSpacingDelta = Mathf.Max(m_lineSpacingDelta + adjustmentDelta / baseScale, m_lineSpacingMax);

                                        //Debug.Log("[" + m_AutoSizeIterationCount + "] Reducing Line Spacing. Delta of [" + m_lineSpacingDelta.ToString("f3") + "].");
                                        k_HandleVerticalLineBreakingMarker.End();
                                        k_HandleHorizontalLineBreakingMarker.End();
                                        k_HandleVisibleCharacterMarker.End();
                                        k_GenerateTextPhaseIMarker.End();
                                        k_GenerateTextMarker.End();
                                        return;
                                    }
                                    #endregion

                                    // Handle Character Width Adjustments
                                    #region Character Width Adjustments
                                    if (m_charWidthAdjDelta < m_charWidthMaxAdj / 100 && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                                    {
                                        float adjustedTextWidth = textWidth;

                                        // Determine full width of the text
                                        if (m_charWidthAdjDelta > 0)
                                            adjustedTextWidth /= 1f - m_charWidthAdjDelta;

                                        float adjustmentDelta = textWidth - (widthOfTextArea - 0.0001f);
                                        m_charWidthAdjDelta += adjustmentDelta / adjustedTextWidth;
                                        m_charWidthAdjDelta = Mathf.Min(m_charWidthAdjDelta, m_charWidthMaxAdj / 100);

                                        //Debug.Log("[" + m_AutoSizeIterationCount + "] Reducing Character Width by " + (m_charWidthAdjDelta * 100) + "%");
                                        k_HandleVerticalLineBreakingMarker.End();
                                        k_HandleHorizontalLineBreakingMarker.End();
                                        k_HandleVisibleCharacterMarker.End();
                                        k_GenerateTextPhaseIMarker.End();
                                        k_GenerateTextMarker.End();
                                        return;
                                    }
                                    #endregion

                                    // Handle Text Auto-sizing resulting from text exceeding vertical bounds.
                                    #region Text Auto-Sizing (Text greater than vertical bounds)
                                    if (m_fontSize > m_fontSizeMin && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                                    {
                                        m_maxFontSize = m_fontSize;

                                        float sizeDelta = Mathf.Max((m_fontSize - m_minFontSize) / 2, 0.05f);
                                        m_fontSize -= sizeDelta;
                                        m_fontSize = Mathf.Max((int)(m_fontSize * 20 + 0.5f) / 20f, m_fontSizeMin);

                                        //Debug.Log("[" + m_AutoSizeIterationCount + "] Reducing Point Size from [" + m_maxFontSize.ToString("f3") + "] to [" + m_fontSize.ToString("f3") + "] with delta of [" + sizeDelta.ToString("f3") + "].");
                                        k_HandleVerticalLineBreakingMarker.End();
                                        k_HandleHorizontalLineBreakingMarker.End();
                                        k_HandleVisibleCharacterMarker.End();
                                        k_GenerateTextPhaseIMarker.End();
                                        k_GenerateTextMarker.End();
                                        return;
                                    }
                                    #endregion Text Auto-Sizing
                                }

                                // Check Text Overflow Modes
                                switch (m_overflowMode)
                                {
                                    case TextOverflowModes.Overflow:
                                        InsertNewLine(i, baseScale, currentElementScale, currentEmScale, m_GlyphHorizontalAdvanceAdjustment, boldSpacingAdjustment, characterSpacingAdjustment, widthOfTextArea, lineGap, ref isMaxVisibleDescenderSet, ref maxVisibleDescender);
                                        isStartOfNewLine = true;
                                        isFirstWordOfLine = true;
                                        k_HandleVerticalLineBreakingMarker.End();
                                        k_HandleHorizontalLineBreakingMarker.End();
                                        k_HandleVisibleCharacterMarker.End();
                                        continue;

                                    case TextOverflowModes.Truncate:
                                        i = RestoreWordWrappingState(ref m_SavedLastValidState);

                                        characterToSubstitute.index = testedCharacterCount;
                                        characterToSubstitute.unicode = 0x03;
                                        k_HandleVerticalLineBreakingMarker.End();
                                        k_HandleHorizontalLineBreakingMarker.End();
                                        k_HandleVisibleCharacterMarker.End();
                                        continue;
                                }
                            }
                            else
                            {
                                // New line of text does not exceed vertical bounds of text container
                                InsertNewLine(i, baseScale, currentElementScale, currentEmScale, m_GlyphHorizontalAdvanceAdjustment, boldSpacingAdjustment, characterSpacingAdjustment, widthOfTextArea, lineGap, ref isMaxVisibleDescenderSet, ref maxVisibleDescender);
                                isStartOfNewLine = true;
                                isFirstWordOfLine = true;
                                k_HandleHorizontalLineBreakingMarker.End();
                                k_HandleVisibleCharacterMarker.End();
                                continue;
                            }
                        }
                        else
                        {
                            if (m_enableAutoSizing && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                            {
                                // Handle Character Width Adjustments
                                #region Character Width Adjustments
                                if (m_charWidthAdjDelta < m_charWidthMaxAdj / 100)
                                {
                                    float adjustedTextWidth = textWidth;

                                    // Determine full width of the text
                                    if (m_charWidthAdjDelta > 0)
                                        adjustedTextWidth /= 1f - m_charWidthAdjDelta;

                                    float adjustmentDelta = textWidth - (widthOfTextArea - 0.0001f);
                                    m_charWidthAdjDelta += adjustmentDelta / adjustedTextWidth;
                                    m_charWidthAdjDelta = Mathf.Min(m_charWidthAdjDelta, m_charWidthMaxAdj / 100);

                                    //Debug.Log("[" + m_AutoSizeIterationCount + "] Reducing Character Width by " + (m_charWidthAdjDelta * 100) + "%");
                                    k_HandleHorizontalLineBreakingMarker.End();
                                    k_HandleVisibleCharacterMarker.End();
                                    k_GenerateTextPhaseIMarker.End();
                                    k_GenerateTextMarker.End();
                                    return;
                                }
                                #endregion

                                // Handle Text Auto-sizing resulting from text exceeding horizontal bounds.
                                #region Text Exceeds Horizontal Bounds - Reducing Point Size
                                if (m_fontSize > m_fontSizeMin)
                                {
                                    // Reset character width adjustment delta
                                    //m_charWidthAdjDelta = 0;

                                    // Adjust Point Size
                                    m_maxFontSize = m_fontSize;

                                    float sizeDelta = Mathf.Max((m_fontSize - m_minFontSize) / 2, 0.05f);
                                    m_fontSize -= sizeDelta;
                                    m_fontSize = Mathf.Max((int)(m_fontSize * 20 + 0.5f) / 20f, m_fontSizeMin);

                                    //Debug.Log("[" + m_AutoSizeIterationCount + "] Reducing Point Size from [" + m_maxFontSize.ToString("f3") + "] to [" + m_fontSize.ToString("f3") + "] with delta of [" + sizeDelta.ToString("f3") + "].");
                                    k_HandleHorizontalLineBreakingMarker.End();
                                    k_HandleVisibleCharacterMarker.End();
                                    k_GenerateTextPhaseIMarker.End();
                                    k_GenerateTextMarker.End();
                                    return;
                                }
                                #endregion

                            }

                            // Check Text Overflow Modes
                            switch (m_overflowMode)
                            {
                                case TextOverflowModes.Overflow:
                                    // Nothing happens as horizontal bounds are ignored in this mode.
                                    break;

                                case TextOverflowModes.Truncate:
                                    i = RestoreWordWrappingState(ref m_SavedWordWrapState);

                                    characterToSubstitute.index = testedCharacterCount;
                                    characterToSubstitute.unicode = 0x03;
                                    k_HandleHorizontalLineBreakingMarker.End();
                                    k_HandleVisibleCharacterMarker.End();
                                    continue;
                            }

                        }

                        k_HandleHorizontalLineBreakingMarker.End();
                    }
                    #endregion


                    // Special handling of characters that are not ignored at the end of a line.
                    if (charCode == 9)
                    {
                        m_textInfo.characterInfo[m_characterCount].isVisible = false;
                        m_lastVisibleCharacterOfLine = m_characterCount;
                    }
                    else if (charCode == 0xAD)
                    {
                        m_textInfo.characterInfo[m_characterCount].isVisible = false;
                    }
                    else
                    {
                        k_SaveGlyphVertexDataMarker.Begin();
                        // Save Character Vertex Data
                        SaveGlyphVertexInfo(padding, style_padding, m_htmlColor);
                        k_SaveGlyphVertexDataMarker.End();

                        if (isStartOfNewLine)
                        {
                            isStartOfNewLine = false;
                            m_firstVisibleCharacterOfLine = m_characterCount;
                        }

                        m_lineVisibleCharacterCount += 1;
                        m_lastVisibleCharacterOfLine = m_characterCount;
                    }

                    k_HandleVisibleCharacterMarker.End();
                }
                #endregion Handle Visible Characters


                // Store Rectangle positions for each Character.
                #region Store Character Data
                m_textInfo.characterInfo[m_characterCount].lineNumber = m_lineNumber;

                if (charCode != 10 && charCode != 11 && charCode != 13 && isInjectingCharacter == false /* && charCode != 8230 */ || m_textInfo.lineInfo[m_lineNumber].characterCount == 1)
                    m_textInfo.lineInfo[m_lineNumber].alignment = m_lineJustification;
                #endregion Store Character Data


                // Handle xAdvance & Tabulation Stops. Tab stops at every 25% of Font Size.
                #region XAdvance, Tabulation & Stops
                k_ComputeCharacterAdvanceMarker.Begin();
                if (charCode == 9)
                {
                    float tabSize = m_currentFontAsset.m_FaceInfo.tabWidth * m_currentFontAsset.tabSize * currentElementScale;
                    float tabs = Mathf.Ceil(m_xAdvance / tabSize) * tabSize;
                    m_xAdvance = tabs > m_xAdvance ? tabs : m_xAdvance + tabSize;
                }
                else if (m_monoSpacing != 0)
                {
                    m_xAdvance += (m_monoSpacing - monoAdvance + ((m_currentFontAsset.normalSpacingOffset + characterSpacingAdjustment) * currentEmScale) + m_cSpacing) * (1 - m_charWidthAdjDelta);
                }
                else
                {
                    m_xAdvance += ((currentGlyphMetrics.horizontalAdvance + glyphAdjustments.m_XAdvance) * currentElementScale + (m_currentFontAsset.normalSpacingOffset + characterSpacingAdjustment + boldSpacingAdjustment) * currentEmScale + m_cSpacing) * (1 - m_charWidthAdjDelta);
                }

                // Store xAdvance information
                m_textInfo.characterInfo[m_characterCount].xAdvance = m_xAdvance;
                k_ComputeCharacterAdvanceMarker.End();
                #endregion Tabulation & Stops


                // Handle Carriage Return
                #region Carriage Return
                if (charCode == 13)
                {
                    k_HandleCarriageReturnMarker.Begin();
                    m_xAdvance = 0;
                    k_HandleCarriageReturnMarker.End();
                }
                #endregion Carriage Return


                // Handle Line Spacing Adjustments + Word Wrapping & special case for last line.
                #region Check for Line Feed and Last Character
                if (charCode == 10 || charCode == 11 || charCode == 0x03 || charCode == 0x2028 || charCode == 0x2029 || (charCode == 0x2D && isInjectingCharacter) || m_characterCount == totalCharacterCount - 1)
                {
                    k_HandleLineTerminationMarker.Begin();

                    // Adjust current line spacing (if necessary) before inserting new line
                    float baselineAdjustmentDelta = m_maxLineAscender - m_startOfLineAscender;
                    if (m_lineOffset > 0 && Math.Abs(baselineAdjustmentDelta) > 0.01f && m_IsDrivenLineSpacing == false)
                    {
                        //Debug.Log("Line Feed - Adjusting Line Spacing on line #" + m_lineNumber);
                        AdjustLineOffset(m_firstCharacterOfLine, m_characterCount, baselineAdjustmentDelta);
                        m_ElementDescender -= baselineAdjustmentDelta;
                        m_lineOffset += baselineAdjustmentDelta;
                    }

                    // Calculate lineAscender & make sure if last character is superscript or subscript that we check that as well.
                    float lineDescender = m_maxLineDescender - m_lineOffset;

                    // Update maxDescender and maxVisibleDescender
                    m_ElementDescender = m_ElementDescender < lineDescender ? m_ElementDescender : lineDescender;
                    if (!isMaxVisibleDescenderSet)
                        maxVisibleDescender = m_ElementDescender;

                    // Save Line Information
                    m_textInfo.lineInfo[m_lineNumber].firstCharacterIndex = m_firstCharacterOfLine;
                    m_textInfo.lineInfo[m_lineNumber].lastCharacterIndex = m_lastCharacterOfLine = m_characterCount;
                    m_lastVisibleCharacterOfLine = m_lastVisibleCharacterOfLine < m_firstVisibleCharacterOfLine ? m_firstVisibleCharacterOfLine : m_lastVisibleCharacterOfLine;

                    m_textInfo.lineInfo[m_lineNumber].characterCount = m_textInfo.lineInfo[m_lineNumber].lastCharacterIndex - m_textInfo.lineInfo[m_lineNumber].firstCharacterIndex + 1;
                    m_textInfo.lineInfo[m_lineNumber].width = widthOfTextArea;

                    if (m_textInfo.lineInfo[m_lineNumber].characterCount == 1)
                        m_textInfo.lineInfo[m_lineNumber].alignment = m_lineJustification;

                    var maxAdvanceOffset = ((m_currentFontAsset.normalSpacingOffset + characterSpacingAdjustment + boldSpacingAdjustment) * currentEmScale - m_cSpacing) * (1 - m_charWidthAdjDelta);
                    if (m_textInfo.characterInfo[m_lastVisibleCharacterOfLine].isVisible)
                        m_textInfo.lineInfo[m_lineNumber].maxAdvance = m_textInfo.characterInfo[m_lastVisibleCharacterOfLine].xAdvance - maxAdvanceOffset;
                    else
                        m_textInfo.lineInfo[m_lineNumber].maxAdvance = m_textInfo.characterInfo[m_lastCharacterOfLine].xAdvance - maxAdvanceOffset;

                    // Add new line if not last line or character.
                    if (charCode is 10 or 11 or 0x2D or 0x2028 or 0x2029)
                    {
                        // Store the state of the line before starting on the new line.
                        SaveWordWrappingState(ref m_SavedLineState, i, m_characterCount);

                        m_lineNumber += 1;
                        isStartOfNewLine = true;
                        isFirstWordOfLine = true;

                        m_firstCharacterOfLine = m_characterCount + 1;
                        m_lineVisibleCharacterCount = 0;

                        // Check to make sure Array is large enough to hold a new line.
                        if (m_lineNumber >= m_textInfo.lineInfo.Length)
                            ResizeLineExtents(m_lineNumber);

                        float lastVisibleAscender = m_textInfo.characterInfo[m_characterCount].adjustedAscender;

                        // Apply Line Spacing with special handling for VT char(11)
                        if (m_lineHeight == TMP_Math.FLOAT_UNSET)
                        {
                            float lineOffsetDelta = 0 - m_maxLineDescender + lastVisibleAscender + (lineGap + m_lineSpacingDelta) * baseScale + m_lineSpacing * currentEmScale;
                            m_lineOffset += lineOffsetDelta;
                            m_IsDrivenLineSpacing = false;
                        }
                        else
                        {
                            m_lineOffset += m_lineHeight + m_lineSpacing * currentEmScale;
                            m_IsDrivenLineSpacing = true;
                        }

                        m_maxLineAscender = k_LargeNegativeFloat;
                        m_maxLineDescender = k_LargePositiveFloat;
                        m_startOfLineAscender = lastVisibleAscender;

                        m_xAdvance = 0;

                        SaveWordWrappingState(ref m_SavedWordWrapState, i, m_characterCount);
                        SaveWordWrappingState(ref m_SavedLastValidState, i, m_characterCount);

                        m_characterCount += 1;

                        k_HandleLineTerminationMarker.End();

                        continue;
                    }

                    // If End of Text
                    if (charCode == 0x03)
                        i = m_TextProcessingArray.Length;

                    k_HandleLineTerminationMarker.End();
                }
                #endregion Check for Linefeed or Last Character


                // Store Rectangle positions for each Character.
                #region Save CharacterInfo for the current character.
                k_SavePageInfoMarker.Begin();
                // Determine the bounds of the Mesh.
                if (m_textInfo.characterInfo[m_characterCount].isVisible)
                {
                    m_meshExtents.min.x = Mathf.Min(m_meshExtents.min.x, m_textInfo.characterInfo[m_characterCount].bottomLeft.x);
                    m_meshExtents.min.y = Mathf.Min(m_meshExtents.min.y, m_textInfo.characterInfo[m_characterCount].bottomLeft.y);

                    m_meshExtents.max.x = Mathf.Max(m_meshExtents.max.x, m_textInfo.characterInfo[m_characterCount].topRight.x);
                    m_meshExtents.max.y = Mathf.Max(m_meshExtents.max.y, m_textInfo.characterInfo[m_characterCount].topRight.y);
                }


                // Save pageInfo Data
                k_SavePageInfoMarker.End();
                #endregion Saving CharacterInfo


                // Save State of Mesh Creation for handling of Word Wrapping
                #region Save Word Wrapping State
                if (m_enableWordWrapping || m_overflowMode == TextOverflowModes.Truncate)
                {
                    k_SaveProcessingStatesMarker.Begin();

                    if ((isWhiteSpace || charCode == 0x200B || charCode == 0x2D || charCode == 0xAD) && charCode != 0xA0 && charCode != 0x2007 && charCode != 0x2011 && charCode != 0x202F && charCode != 0x2060)
                    {
                        // We store the state of numerous variables for the most recent Space, LineFeed or Carriage Return to enable them to be restored
                        // for Word Wrapping.
                        SaveWordWrappingState(ref m_SavedWordWrapState, i, m_characterCount);
                        isFirstWordOfLine = false;

                        // Reset soft line breaking point since we now have a valid hard break point.
                        m_SavedSoftLineBreakState.previous_WordBreak = -1;
                    }
                    // Handling for East Asian characters
                    else if (TMP_TextUtilities.IsChineseOrJapanese(charCode))
                    {
                        SaveWordWrappingState(ref m_SavedWordWrapState, i, m_characterCount);
                        isFirstWordOfLine = false;
                    }
                    else if (isFirstWordOfLine)
                    {
                        // Special handling for non-breaking space and soft line breaks
                        if (isWhiteSpace || (charCode == 0xAD && isSoftHyphenIgnored == false))
                            SaveWordWrappingState(ref m_SavedSoftLineBreakState, i, m_characterCount);

                        SaveWordWrappingState(ref m_SavedWordWrapState, i, m_characterCount);
                    }

                    k_SaveProcessingStatesMarker.End();
                }
                #endregion Save Word Wrapping State

                SaveWordWrappingState(ref m_SavedLastValidState, i, m_characterCount);

                m_characterCount += 1;
            }

            // Check Auto Sizing and increase font size to fill text container.
            #region Check Auto-Sizing (Upper Font Size Bounds)
            fontSizeDelta = m_maxFontSize - m_minFontSize;
            if (/* !m_isCharacterWrappingEnabled && */ m_enableAutoSizing && fontSizeDelta > 0.051f && m_fontSize < m_fontSizeMax && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
            {
                // Reset character width adjustment delta
                if (m_charWidthAdjDelta < m_charWidthMaxAdj / 100)
                    m_charWidthAdjDelta = 0;

                m_minFontSize = m_fontSize;

                float sizeDelta = Mathf.Max((m_maxFontSize - m_fontSize) / 2, 0.05f);
                m_fontSize += sizeDelta;
                m_fontSize = Mathf.Min((int)(m_fontSize * 20 + 0.5f) / 20f, m_fontSizeMax);

                //Debug.Log("[" + m_AutoSizeIterationCount + "] Increasing Point Size from [" + m_minFontSize.ToString("f3") + "] to [" + m_fontSize.ToString("f3") + "] with delta of [" + sizeDelta.ToString("f3") + "].");
                k_GenerateTextPhaseIMarker.End();
                k_GenerateTextMarker.End();
                return;
            }
            #endregion End Auto-sizing Check

            m_IsAutoSizePointSizeSet = true;

            if (m_AutoSizeIterationCount >= m_AutoSizeMaxIterationCount)
                Debug.Log("Auto Size Iteration Count: " + m_AutoSizeIterationCount + ". Final Point Size: " + m_fontSize);

            // If there are no visible characters or only character is End of Text (0x03)... no need to continue
            if (m_characterCount == 0 || (m_characterCount == 1 && charCode == 0x03))
            {
                ClearMesh(true);

                // Event indicating the text has been regenerated.
                k_GenerateTextPhaseIMarker.End();
                k_GenerateTextMarker.End();
                return;
            }

            // End Sampling of Phase I
            k_GenerateTextPhaseIMarker.End();

            // *** PHASE II of Text Generation ***
            k_GenerateTextPhaseIIMarker.Begin();

            // Partial clear of the vertices array to mark unused vertices as degenerate.
            m_textInfo.meshInfo[0].Clear(false);

            // Handle Text Alignment
            #region Text Vertical Alignment
            Vector2 anchorOffset = default;

            // Handle Vertical Text Alignment
            anchorOffset.x = m_RectTransformRect.x;
            var yMin = m_RectTransformRect.y;
            var yMax = m_RectTransformRect.yMax;
            anchorOffset.y = m_VerticalAlignment switch
            {
                VerticalAlignmentOptions.Top => yMax - m_maxTextAscender,
                VerticalAlignmentOptions.Middle => (yMin + yMax) / 2 - (m_maxTextAscender + maxVisibleDescender) / 2,
                VerticalAlignmentOptions.Bottom => yMin - maxVisibleDescender,
                VerticalAlignmentOptions.Baseline => (yMin + yMax) / 2,
                VerticalAlignmentOptions.Geometry => (yMin + yMax) / 2 + 0 - (m_meshExtents.max.y + m_meshExtents.min.y) / 2,
                VerticalAlignmentOptions.Capline => (yMin + yMax) / 2 + 0 - m_maxCapHeight / 2,
            };
            #endregion


            // Initialization for Second Pass
            int lastLine = 0;
            bool isStartOfWord = false;

            // Second Pass : Line Justification, UV Mapping, Character & Line Visibility & more.
            float lossyScale = m_previousLossyScaleY = this.transform.lossyScale.y;

            TMP_CharacterInfo[] characterInfos = m_textInfo.characterInfo;
            #region Handle Line Justification & UV Mapping & Character Visibility & More
            for (int i = 0; i < m_characterCount; i++)
            {
                char unicode = characterInfos[i].character;

                int currentLine = characterInfos[i].lineNumber;
                TMP_LineInfo lineInfo = m_textInfo.lineInfo[currentLine];

                // Process Line Justification
                var justificationOffset = TMP_TextUtilities.CalculateJustificationOffset(lineInfo, lineInfo.alignment);
                var offset = new Vector2(anchorOffset.x + justificationOffset, anchorOffset.y);

                // Handle UV2 mapping options and packing of scale information into UV2.
                #region Handling of UV2 mapping & Scale packing
                bool isCharacterVisible = characterInfos[i].isVisible;
                if (isCharacterVisible)
                {
                    // Pack UV's so that we can pass Xscale needed for Shader to maintain 1:1 ratio.
                    #region Pack Scale into UV2
                    var xScale = characterInfos[i].scale * Mathf.Abs(lossyScale) * (1 - m_charWidthAdjDelta);
                    if ((characterInfos[i].style & FontStyles.Bold) == FontStyles.Bold) xScale *= -1;

                    // Optimization to avoid having a vector2 returned from the Pack UV function.
                    characterInfos[i].vertex_BL.uv2 = new Vector2(PackUV(0, 0), xScale);
                    characterInfos[i].vertex_TL.uv2 = new Vector2(PackUV(0, 1), xScale);
                    characterInfos[i].vertex_TR.uv2 = new Vector2(PackUV(1, 1), xScale);
                    characterInfos[i].vertex_BR.uv2 = new Vector2(PackUV(1, 0), xScale);
                    #endregion

                    characterInfos[i].vertex_BL.position += offset;
                    characterInfos[i].vertex_TL.position += offset;
                    characterInfos[i].vertex_TR.position += offset;
                    characterInfos[i].vertex_BR.position += offset;


                    // Fill Vertex Buffers for the various types of element
                    FillCharacterVertexBuffers(i);
                }
                #endregion

                // Apply Alignment and Justification Offset
                m_textInfo.characterInfo[i].bottomLeft += offset;
                m_textInfo.characterInfo[i].topLeft += offset;
                m_textInfo.characterInfo[i].topRight += offset;
                m_textInfo.characterInfo[i].bottomRight += offset;

                m_textInfo.characterInfo[i].xAdvance += offset.x;

                // Need to recompute lineExtent to account for the offset from justification.
                #region Adjust lineExtents resulting from alignment offset
                if (currentLine != lastLine || i == m_characterCount - 1)
                {
                    // Update the previous line's extents
                    if (currentLine != lastLine)
                        m_textInfo.lineInfo[lastLine].maxAdvance += offset.x;

                    // Update the current line's extents
                    if (i == m_characterCount - 1)
                        m_textInfo.lineInfo[currentLine].maxAdvance += offset.x;
                }
                #endregion


                // Track Word Count per line and for the object
                #region Track Word Count
                if (char.IsLetterOrDigit(unicode) || unicode == 0x2D || unicode == 0xAD || unicode == 0x2010 || unicode == 0x2011)
                {
                    if (isStartOfWord == false)
                        isStartOfWord = true;
                }
                else if (isStartOfWord || i == 0 && (!char.IsPunctuation(unicode) || char.IsWhiteSpace(unicode) || unicode == 0x200B || i == m_characterCount - 1))
                {
                    if (i > 0 && i < characterInfos.Length - 1 && i < m_characterCount && (unicode == 39 || unicode == 8217) && char.IsLetterOrDigit(characterInfos[i - 1].character) && char.IsLetterOrDigit(characterInfos[i + 1].character))
                    {
                    }
                    else
                    {
                        isStartOfWord = false;
                    }
                }
                #endregion

                lastLine = currentLine;
            }
            #endregion

            // End Sampling of Phase II
            k_GenerateTextPhaseIIMarker.End();

            // Phase III - Update Mesh Vertex Data
            k_GenerateTextPhaseIIIMarker.Begin();

            if (IsActive())
            {
                // Upload Mesh Data
                m_mesh.MarkDynamic();
                m_mesh.vertices = m_textInfo.meshInfo[0].vertices;
                m_mesh.uv = m_textInfo.meshInfo[0].uvs0;
                m_mesh.uv2 = m_textInfo.meshInfo[0].uvs2;
                //m_mesh.uv4 = m_textInfo.meshInfo[0].uvs4;
                m_mesh.colors32 = m_textInfo.meshInfo[0].colors32;

                // Compute Bounds for the mesh. Manual computation is more efficient then using Mesh.RecalcualteBounds.
                m_mesh.RecalculateBounds();

                for (int i = 1; i < m_textInfo.materialCount; i++)
                {
                    // Clear unused vertices
                    m_textInfo.meshInfo[i].ClearUnusedVertices();

                    if (m_subTextObjects[i] == null) continue;

                    m_subTextObjects[i].mesh.vertices = m_textInfo.meshInfo[i].vertices;
                    m_subTextObjects[i].mesh.uv = m_textInfo.meshInfo[i].uvs0;
                    m_subTextObjects[i].mesh.uv2 = m_textInfo.meshInfo[i].uvs2;
                    m_subTextObjects[i].mesh.colors32 = m_textInfo.meshInfo[i].colors32;

                    m_subTextObjects[i].mesh.RecalculateBounds();
                }
            }

            // Event indicating the text has been regenerated.

            // End Sampling
            k_GenerateTextPhaseIIIMarker.End();
            k_GenerateTextMarker.End();
        }


        /// <summary>
        /// Method to Enable or Disable child SubMesh objects.
        /// </summary>
        /// <param name="state"></param>
        void SetActiveSubMeshes(bool state)
        {
            for (int i = 1; i < m_subTextObjects.Length && m_subTextObjects[i] != null; i++)
            {
                if (m_subTextObjects[i].enabled != state)
                    m_subTextObjects[i].enabled = state;
            }
        }

        void SetActiveSubTextObjectRenderers(bool state)
        {
            for (int i = 1; i < m_subTextObjects.Length && m_subTextObjects[i] != null; i++)
            {
                var subMeshRenderer = m_subTextObjects[i].renderer;
                if (subMeshRenderer != null && subMeshRenderer.enabled != state)
                    subMeshRenderer.enabled = state;
            }
        }


        /// <summary>
        ///
        /// </summary>
        internal void UpdateSubMeshSortingLayerID(int id)
        {
            for (int i = 1; i < m_subTextObjects.Length; i++)
            {
                TMP_SubMesh subMesh = m_subTextObjects[i];

                if (subMesh != null && subMesh.renderer != null)
                {
                    subMesh.renderer.sortingLayerID = id;
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        internal void UpdateSubMeshSortingOrder(int order)
        {
            for (int i = 1; i < m_subTextObjects.Length; i++)
            {
                TMP_SubMesh subMesh = m_subTextObjects[i];

                if (subMesh != null && subMesh.renderer != null)
                {
                    subMesh.renderer.sortingOrder = order;
                }
            }
        }

        /// <summary>
        /// Method to update the SDF Scale in UV2.
        /// </summary>
        /// <param name="scaleDelta"></param>
        void UpdateSDFScale(float scaleDelta)
        {
            if (scaleDelta is 0 or float.PositiveInfinity or float.NegativeInfinity)
            {
                m_havePropertiesChanged = true;
                OnPreRenderObject();
                return;
            }

            for (int materialIndex = 0; materialIndex < m_textInfo.materialCount; materialIndex++)
            {
                TMP_MeshInfo meshInfo = m_textInfo.meshInfo[materialIndex];

                for (int i = 0; i < meshInfo.uvs2.Length; i++)
                {
                    meshInfo.uvs2[i].y *= Mathf.Abs(scaleDelta);
                }
            }

            // Push the updated uv2 scale information to the meshes.
            for (int i = 0; i < m_textInfo.meshInfo.Length; i++)
            {
                if (i == 0)
                    m_mesh.uv2 = m_textInfo.meshInfo[0].uvs2;
                else
                    m_subTextObjects[i].mesh.uv2 = m_textInfo.meshInfo[i].uvs2;
            }
        }

    }
}
