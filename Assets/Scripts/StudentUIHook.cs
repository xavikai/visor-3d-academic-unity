using UnityEngine;
using UnityEngine.UIElements;

public class StudentUIHook : MonoBehaviour
{
    public ModelLoader modelLoader;

    private UIDocument uiDocument;

    private Toggle albedoToggle;
    private Toggle normalToggle;
    private Slider normalSlider;
    private Toggle metallicToggle;
    private Slider metallicSlider;
    private Slider smoothnessSlider;
    private Toggle highpolyToggle;
    private Toggle wireframeToggle;
    private Toggle vertexColorToggle;
    private Toggle uvToggle;
    private DropdownField modelDropdown;
    private DropdownField materialDropdown;
    private Label statsText;
    
    private Toggle emissionToggle;
    private Slider emissionSlider;
    
    private Toggle occlusionToggle;
    private Slider occlusionSlider;
    private Toggle heightToggle;
    private Slider heightSlider;
    
    private Image imgAlbedo;
    private Image imgNormal;
    private Image imgMetallic;
    private Image imgEmission;
    private Image imgOcclusion;
    private Image imgHeight;
    private Image imgUv;

    private VisualElement zoomPanel;
    private Image imgZoom;
    private Label zoomTitle;
    private Button btnCloseZoom;

    private System.Collections.Generic.List<RenderTexture> activeRenderTextures = new System.Collections.Generic.List<RenderTexture>();

    private void BindUI()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        var root = uiDocument.rootVisualElement;
        var environment = GetComponent<ViewerEnvironmentUI>() ?? gameObject.AddComponent<ViewerEnvironmentUI>();
        environment.Bind(root, modelLoader);
        var turntable = GetComponent<TurntableUI>() ?? gameObject.AddComponent<TurntableUI>();
        turntable.Bind(root, modelLoader);
        var lighting = GetComponent<StudioLightingUI>() ?? gameObject.AddComponent<StudioLightingUI>();
        lighting.Bind(root, modelLoader);

        // Cercar elements
        albedoToggle = root.Q<Toggle>("AlbedoToggle");
        normalToggle = root.Q<Toggle>("NormalToggle");
        normalSlider = root.Q<Slider>("NormalSlider");
        metallicToggle = root.Q<Toggle>("MetallicToggle");
        metallicSlider = root.Q<Slider>("MetallicSlider");
        smoothnessSlider = root.Q<Slider>("SmoothnessSlider");
        highpolyToggle = root.Q<Toggle>("HighpolyToggle");
        wireframeToggle = root.Q<Toggle>("WireframeToggle");
        vertexColorToggle = root.Q<Toggle>("VertexColorToggle");
        uvToggle = root.Q<Toggle>("UvToggle");
        modelDropdown = root.Q<DropdownField>("ModelDropdown");
        materialDropdown = root.Q<DropdownField>("MaterialDropdown");
        statsText = root.Q<Label>("StatsText");
        
        emissionToggle = root.Q<Toggle>("EmissionToggle");
        emissionSlider = root.Q<Slider>("EmissionSlider");
        
        occlusionToggle = root.Q<Toggle>("OcclusionToggle");
        occlusionSlider = root.Q<Slider>("OcclusionSlider");
        heightToggle = root.Q<Toggle>("HeightToggle");
        heightSlider = root.Q<Slider>("HeightSlider");
        
        imgAlbedo = root.Q<Image>("ImgAlbedo");
        imgNormal = root.Q<Image>("ImgNormal");
        imgMetallic = root.Q<Image>("ImgMetallic");
        imgEmission = root.Q<Image>("ImgEmission");
        imgOcclusion = root.Q<Image>("ImgOcclusion");
        imgHeight = root.Q<Image>("ImgHeight");
        imgUv = root.Q<Image>("ImgUv");

        zoomPanel = root.Q<VisualElement>("ZoomPanel");
        imgZoom = root.Q<Image>("ImgZoom");
        zoomTitle = root.Q<Label>("ZoomTitle");
        btnCloseZoom = root.Q<Button>("BtnCloseZoom");

        // Registrar esdeveniments
        if (albedoToggle != null) albedoToggle.RegisterValueChangedCallback(evt => OnAlbedoChanged(evt.newValue));
        if (normalToggle != null) normalToggle.RegisterValueChangedCallback(evt => OnNormalChanged(evt.newValue));
        if (metallicToggle != null) metallicToggle.RegisterValueChangedCallback(evt => OnMetallicChanged(evt.newValue));
        if (highpolyToggle != null) highpolyToggle.RegisterValueChangedCallback(evt => OnHighpolyChanged(evt.newValue));
        if (wireframeToggle != null) wireframeToggle.RegisterValueChangedCallback(evt => OnWireframeChanged(evt.newValue));
        if (vertexColorToggle != null) vertexColorToggle.RegisterValueChangedCallback(evt => OnVertexColorChanged(evt.newValue));
        if (uvToggle != null) uvToggle.RegisterValueChangedCallback(evt => OnUvChanged(evt.newValue));
        
        if (normalSlider != null) normalSlider.RegisterValueChangedCallback(evt => OnNormalIntensityChanged(evt.newValue));
        if (metallicSlider != null) metallicSlider.RegisterValueChangedCallback(evt => OnMetallicIntensityChanged(evt.newValue));
        if (smoothnessSlider != null) smoothnessSlider.RegisterValueChangedCallback(evt => OnSmoothnessChanged(evt.newValue));
        if (emissionSlider != null) emissionSlider.RegisterValueChangedCallback(evt => OnEmissionIntensityChanged(evt.newValue));
        if (emissionToggle != null) emissionToggle.RegisterValueChangedCallback(evt => OnEmissionChanged(evt.newValue));
        
        if (occlusionToggle != null) occlusionToggle.RegisterValueChangedCallback(evt => OnOcclusionChanged(evt.newValue));
        if (occlusionSlider != null) occlusionSlider.RegisterValueChangedCallback(evt => OnOcclusionIntensityChanged(evt.newValue));
        if (heightToggle != null) heightToggle.RegisterValueChangedCallback(evt => OnHeightChanged(evt.newValue));
        if (heightSlider != null) heightSlider.RegisterValueChangedCallback(evt => OnHeightScaleChanged(evt.newValue));
        
        if (modelDropdown != null) modelDropdown.RegisterValueChangedCallback(evt => OnModelSelected(evt.newValue));
        if (materialDropdown != null) materialDropdown.RegisterValueChangedCallback(evt => OnMaterialSelected(evt.newValue));
        
        if (btnCloseZoom != null) btnCloseZoom.clicked += CloseZoom;
        
        SetupZoomButton(imgAlbedo, "Albedo");
        SetupZoomButton(imgNormal, "Normal Map");
        SetupZoomButton(imgMetallic, "Metallic / Smoothness");
        SetupZoomButton(imgEmission, "Emission");
        SetupZoomButton(imgOcclusion, "Occlusion Map");
        SetupZoomButton(imgHeight, "Height Map");
        SetupZoomButton(imgUv, "UV Layout");

        if (metallicSlider != null && metallicToggle != null)
        {
            metallicSlider.SetEnabled(!metallicToggle.value);
        }
        
        if (occlusionSlider != null && occlusionToggle != null) occlusionSlider.SetEnabled(occlusionToggle.value);
        if (heightSlider != null && heightToggle != null) heightSlider.SetEnabled(heightToggle.value);

        UpdateStats();
    }

    private bool started;
    private VisualElement boundRoot;

    private void OnEnable()
    {
        if (modelLoader != null) modelLoader.ModelChanged += RefreshSelection;
        if (started) EnsureUI();
    }

    private void Start()
    {
        started = true;
        EnsureUI();
    }

    private void EnsureUI()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;
        if (boundRoot != uiDocument.rootVisualElement)
        {
            boundRoot = uiDocument.rootVisualElement;
            BindUI();
        }
        RefreshSelection();
    }

    private void RefreshSelection()
    {
        if (modelDropdown == null || modelLoader == null || !modelLoader.IsReady) return;
        modelDropdown.choices = modelLoader.models.ConvertAll(entry => entry.name);
        modelDropdown.SetEnabled(modelDropdown.choices.Count > 0);
        modelDropdown.SetValueWithoutNotify(modelLoader.CurrentModelIndex >= 0
            ? modelDropdown.choices[modelLoader.CurrentModelIndex] : "Cap model al catàleg");
        if (highpolyToggle != null)
        {
            highpolyToggle.SetValueWithoutNotify(modelLoader.IsHighpolyActive);
            highpolyToggle.SetEnabled(modelLoader.HasBothVariants);
            highpolyToggle.tooltip = modelLoader.HasBothVariants ? "Alternar lowpoly / highpoly" : "Només hi ha una variant disponible";
        }
        CloseZoom();
        UpdateStats();
        PopulateMaterialDropdown();
    }

    private void OnDisable()
    {
        if (modelLoader != null) modelLoader.ModelChanged -= RefreshSelection;
        CancelInvoke();
        CloseZoom();
        ClearRenderTextures();
    }
    public void UpdateTextureGallery()
    {
        if (modelLoader == null || modelLoader.materialViewer == null) return;

        CloseZoom();
        ClearRenderTextures();

        var data = modelLoader.materialViewer.GetActiveMaterialData(modelLoader.GetActiveModel());
        if (data != null)
        {
            SetTexture(imgAlbedo, data.baseMap);
            SetTexture(imgNormal, data.bumpMap, true);
            SetTexture(imgMetallic, data.metallicGlossMap);
            SetTexture(imgEmission, data.emissionMap);
            SetTexture(imgOcclusion, data.occlusionMap);
            SetTexture(imgHeight, data.parallaxMap);
        }
        else
        {
            SetTexture(imgAlbedo, null);
            SetTexture(imgNormal, null);
            SetTexture(imgMetallic, null);
            SetTexture(imgEmission, null);
            SetTexture(imgOcclusion, null);
            SetTexture(imgHeight, null);
        }

        GameObject activeModel = modelLoader.GetActiveModel();
        SetTexture(imgUv, modelLoader.materialViewer.GetActiveModelUVs(activeModel));
    }

    private void ClearRenderTextures()
    {
        foreach (var rt in activeRenderTextures)
        {
            if (rt != null) RenderTexture.ReleaseTemporary(rt);
        }
        activeRenderTextures.Clear();
    }

    private void SetTexture(Image img, Texture tex, bool isNormalMap = false)
    {
        if (img == null) return;
        
        if (tex != null && isNormalMap)
        {
            Shader unpackShader = Shader.Find("Hidden/UnpackNormalUI");
            if (unpackShader != null)
            {
                Material unpackMat = new Material(unpackShader);
                RenderTexture rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(tex, rt, unpackMat);
                img.image = rt;
                activeRenderTextures.Add(rt);
                
                if (Application.isPlaying) Destroy(unpackMat);
                else DestroyImmediate(unpackMat);
            }
            else
            {
                img.image = tex;
            }
        }
        else
        {
            img.image = tex;
        }

        img.tintColor = tex != null ? Color.white : new Color(0.15f, 0.15f, 0.15f, 1f);
    }

    private void SetupZoomButton(Image img, string title)
    {
        if (img != null)
        {
            img.RegisterCallback<PointerDownEvent>(evt => 
            {
                if (evt.button == 0) // Click esquerre
                {
                    OpenZoom(img.image, title);
                }
            });
        }
    }

    private void OpenZoom(Texture tex, string title)
    {
        if (tex == null || zoomPanel == null) return;
        
        if (zoomTitle != null) zoomTitle.text = title;
        if (imgZoom != null) 
        {
            imgZoom.image = tex;
            imgZoom.tintColor = Color.white;
        }
        
        zoomPanel.style.display = DisplayStyle.Flex;
    }

    private void CloseZoom()
    {
        if (zoomPanel != null) zoomPanel.style.display = DisplayStyle.None;
        if (imgZoom != null) imgZoom.image = null;
    }

    public void UpdateStats()
    {
        if (statsText == null || modelLoader == null || modelLoader.polygonCounter == null) return;

        int highTris = 0, highVerts = 0;
        int lowTris = 0, lowVerts = 0;

        if (modelDropdown != null && modelLoader.models != null)
        {
            int index = modelDropdown.choices.IndexOf(modelDropdown.value);
            if (index >= 0 && index < modelLoader.models.Count)
            {
                var entry = modelLoader.models[index];
                int tempT, tempV;
                foreach (var p in entry.highpolyParts)
                {
                    if (p != null) {
                        modelLoader.polygonCounter.GetStats(p, out tempT, out tempV);
                        highTris += tempT; highVerts += tempV;
                    }
                }
                foreach (var p in entry.lowpolyParts)
                {
                    if (p != null) {
                        modelLoader.polygonCounter.GetStats(p, out tempT, out tempV);
                        lowTris += tempT; lowVerts += tempV;
                    }
                }
            }
        }

        statsText.text = $"<b>Highpoly:</b>\n{highTris:N0} tris | {highVerts:N0} verts\n\n" +
                         $"<b>Lowpoly:</b>\n{lowTris:N0} tris | {lowVerts:N0} verts";
    }

    private void OnAlbedoChanged(bool state)
    {
        if (modelLoader != null && modelLoader.materialViewer != null)
            modelLoader.materialViewer.ToggleAlbedo(state);
    }

    private void OnNormalChanged(bool state)
    {
        if (modelLoader != null && modelLoader.materialViewer != null)
            modelLoader.materialViewer.ToggleNormal(state);
    }

    private void OnHighpolyChanged(bool state)
    {
        if (modelLoader != null)
        {
            modelLoader.ToggleHighpoly(state);
        }
    }

    private void OnMetallicChanged(bool state)
    {
        if (metallicSlider != null)
        {
            metallicSlider.SetEnabled(!state);
        }

        if (modelLoader != null && modelLoader.materialViewer != null)
            modelLoader.materialViewer.ToggleMetallic(state);
    }

    private void OnWireframeChanged(bool state)
    {
        if (modelLoader != null && modelLoader.materialViewer != null)
            modelLoader.materialViewer.ToggleWireframe(state);
    }

    private void OnVertexColorChanged(bool state)
    {
        if (state && uvToggle != null && uvToggle.value)
        {
            uvToggle.SetValueWithoutNotify(false);
            if (modelLoader != null && modelLoader.materialViewer != null)
                modelLoader.materialViewer.ToggleUV(false);
        }

        if (modelLoader != null && modelLoader.materialViewer != null)
            modelLoader.materialViewer.ToggleVertexColor(state);
    }

    private void OnUvChanged(bool state)
    {
        if (state && vertexColorToggle != null && vertexColorToggle.value)
        {
            vertexColorToggle.SetValueWithoutNotify(false);
            if (modelLoader != null && modelLoader.materialViewer != null)
                modelLoader.materialViewer.ToggleVertexColor(false);
        }

        if (modelLoader != null && modelLoader.materialViewer != null)
            modelLoader.materialViewer.ToggleUV(state);
    }

    private void OnNormalIntensityChanged(float value)
    {
        if (modelLoader != null && modelLoader.materialViewer != null)
            modelLoader.materialViewer.SetNormalIntensity(value);
    }

    private void OnMetallicIntensityChanged(float value)
    {
        if (modelLoader != null && modelLoader.materialViewer != null)
            modelLoader.materialViewer.SetMetallic(value);
    }

    private void OnSmoothnessChanged(float value)
    {
        if (modelLoader != null && modelLoader.materialViewer != null)
            modelLoader.materialViewer.SetSmoothness(value);
    }

    private void OnEmissionChanged(bool state)
    {
        if (modelLoader != null && modelLoader.materialViewer != null)
            modelLoader.materialViewer.ToggleEmission(state);
    }

    private void OnEmissionIntensityChanged(float value)
    {
        if (modelLoader != null && modelLoader.materialViewer != null)
            modelLoader.materialViewer.SetEmissionIntensity(value);
    }

    private void OnOcclusionChanged(bool state)
    {
        if (occlusionSlider != null) occlusionSlider.SetEnabled(state);

        if (modelLoader != null && modelLoader.materialViewer != null)
            modelLoader.materialViewer.ToggleOcclusion(state);
    }

    private void OnOcclusionIntensityChanged(float value)
    {
        if (modelLoader != null && modelLoader.materialViewer != null)
            modelLoader.materialViewer.SetOcclusionIntensity(value);
    }

    private void OnHeightChanged(bool state)
    {
        if (heightSlider != null) heightSlider.SetEnabled(state);

        if (modelLoader != null && modelLoader.materialViewer != null)
            modelLoader.materialViewer.ToggleHeight(state);
    }

    private void OnHeightScaleChanged(float value)
    {
        if (modelLoader != null && modelLoader.materialViewer != null)
            modelLoader.materialViewer.SetHeightScale(value);
    }

    private void OnModelSelected(string selectedValue)
    {
        if (modelLoader != null && modelDropdown != null)
        {
            int index = modelDropdown.choices.IndexOf(selectedValue);
            if (index >= 0)
            {
                modelLoader.SetCurrentModel(index);
            }
        }
    }

    private System.Collections.Generic.List<Material> currentMaterials = new System.Collections.Generic.List<Material>();

    private void PopulateMaterialDropdown()
    {
        if (materialDropdown == null || modelLoader == null || modelLoader.materialViewer == null) return;

        GameObject activeModel = modelLoader.GetActiveModel();
        currentMaterials = modelLoader.materialViewer.GetMaterialsForModel(activeModel);

        materialDropdown.choices.Clear();
        materialDropdown.SetValueWithoutNotify("");

        for (int i = 0; i < currentMaterials.Count; i++)
        {
            string matName = currentMaterials[i].name;
            if (matName.EndsWith(" (Instance)")) matName = matName.Substring(0, matName.Length - 11);
            materialDropdown.choices.Add($"{i + 1}. {matName}");
        }

        if (materialDropdown.choices.Count > 0)
        {
            materialDropdown.SetValueWithoutNotify(materialDropdown.choices[0]);
            OnMaterialSelected(materialDropdown.choices[0]);
        }
        else
        {
            UpdateTextureGallery();
        }
    }

    private void OnMaterialSelected(string selectedValue)
    {
        if (materialDropdown == null || modelLoader == null || modelLoader.materialViewer == null) return;
        
        int index = materialDropdown.choices.IndexOf(selectedValue);
        if (index >= 0 && index < currentMaterials.Count)
        {
            modelLoader.materialViewer.SetActiveMaterial(currentMaterials[index]);
            SyncUIWithActiveMaterial();
            UpdateTextureGallery();
        }
    }

    private void SyncUIWithActiveMaterial()
    {
        if (modelLoader == null || modelLoader.materialViewer == null) return;
        
        var data = modelLoader.materialViewer.GetActiveMaterialData(modelLoader.GetActiveModel());
        if (data == null) return;

        if (albedoToggle != null) albedoToggle.SetValueWithoutNotify(data.albedoToggle);
        if (normalToggle != null) normalToggle.SetValueWithoutNotify(data.normalToggle);
        if (normalSlider != null) normalSlider.SetValueWithoutNotify(data.normalSlider);
        
        if (metallicToggle != null) metallicToggle.SetValueWithoutNotify(data.metallicToggle);
        if (metallicSlider != null) 
        {
            metallicSlider.SetValueWithoutNotify(data.metallicSlider);
            metallicSlider.SetEnabled(!data.metallicToggle);
        }
        
        if (smoothnessSlider != null) smoothnessSlider.SetValueWithoutNotify(data.smoothnessSlider);
        
        if (emissionToggle != null) emissionToggle.SetValueWithoutNotify(data.emissionToggle);
        if (emissionSlider != null) emissionSlider.SetValueWithoutNotify(data.emissionSlider);
        
        if (occlusionToggle != null) occlusionToggle.SetValueWithoutNotify(data.occlusionToggle);
        if (occlusionSlider != null) 
        {
            occlusionSlider.SetValueWithoutNotify(data.occlusionSlider);
            occlusionSlider.SetEnabled(data.occlusionToggle);
        }
        
        if (heightToggle != null) heightToggle.SetValueWithoutNotify(data.heightToggle);
        if (heightSlider != null) 
        {
            heightSlider.SetValueWithoutNotify(data.heightSlider);
            heightSlider.SetEnabled(data.heightToggle);
        }
    }
}
