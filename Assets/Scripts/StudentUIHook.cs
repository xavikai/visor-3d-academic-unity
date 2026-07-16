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
    private Label statsText;
    
    private Toggle emissionToggle;
    private Slider emissionSlider;
    
    private Image imgAlbedo;
    private Image imgNormal;
    private Image imgMetallic;
    private Image imgEmission;
    private Image imgUv;

    private VisualElement zoomPanel;
    private Image imgZoom;
    private Label zoomTitle;
    private Button btnCloseZoom;

    private void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        var root = uiDocument.rootVisualElement;

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
        statsText = root.Q<Label>("StatsText");
        
        emissionToggle = root.Q<Toggle>("EmissionToggle");
        emissionSlider = root.Q<Slider>("EmissionSlider");
        
        imgAlbedo = root.Q<Image>("ImgAlbedo");
        imgNormal = root.Q<Image>("ImgNormal");
        imgMetallic = root.Q<Image>("ImgMetallic");
        imgEmission = root.Q<Image>("ImgEmission");
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
        
        if (modelDropdown != null) modelDropdown.RegisterValueChangedCallback(evt => OnModelSelected(evt.newValue));
        
        if (btnCloseZoom != null) btnCloseZoom.clicked += CloseZoom;
        
        SetupZoomButton(imgAlbedo, "Albedo");
        SetupZoomButton(imgNormal, "Normal Map");
        SetupZoomButton(imgMetallic, "Metallic / Smoothness");
        SetupZoomButton(imgEmission, "Emission");
        SetupZoomButton(imgUv, "UV Layout");

        UpdateStats();
        Invoke("UpdateTextureGallery", 0.5f); // Donem mig segon perquè s'inicialitzi el model actiu
    }

    private void Start()
    {
        if (modelDropdown != null && modelLoader != null)
        {
            modelDropdown.choices.Clear();
            if (modelLoader.lowpolyContainer != null)
            {
                foreach (Transform child in modelLoader.lowpolyContainer.transform)
                {
                    modelDropdown.choices.Add(child.name);
                }
            }
            
            if (modelDropdown.choices.Count > 0)
            {
                modelDropdown.SetValueWithoutNotify(modelDropdown.choices[0]);
            }
        }
    }

    private void OnDisable()
    {
        if (btnCloseZoom != null) btnCloseZoom.clicked -= CloseZoom;
    }

    public void UpdateTextureGallery()
    {
        if (modelLoader == null || modelLoader.materialViewer == null) return;

        var data = modelLoader.materialViewer.GetActiveMaterialData(modelLoader.GetActiveModel());
        if (data != null)
        {
            SetTexture(imgAlbedo, data.baseMap);
            SetTexture(imgNormal, data.bumpMap);
            SetTexture(imgMetallic, data.metallicGlossMap);
            SetTexture(imgEmission, data.emissionMap);
        }
        else
        {
            SetTexture(imgAlbedo, null);
            SetTexture(imgNormal, null);
            SetTexture(imgMetallic, null);
            SetTexture(imgEmission, null);
        }

        GameObject activeModel = modelLoader.GetActiveModel();
        SetTexture(imgUv, modelLoader.materialViewer.GetActiveModelUVs(activeModel));
    }

    private void SetTexture(Image img, Texture tex)
    {
        if (img == null) return;
        img.image = tex;
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
            
            // UI Toolkit no suporta l'assignació directa de Materials per desempaquetar Normal Maps a la Image d'aquesta forma
            // Si calgués, s'hauria de crear un shader unlit a mida o modificar l'estil amb `-unity-background-image-tint-color`.
            // Pel visor acadèmic, mostrem la textura crua.
        }
        
        zoomPanel.style.display = DisplayStyle.Flex;
    }

    private void CloseZoom()
    {
        if (zoomPanel != null) zoomPanel.style.display = DisplayStyle.None;
    }

    public void UpdateStats()
    {
        if (statsText == null || modelLoader == null || modelLoader.polygonCounter == null) return;

        int highTris = 0, highVerts = 0;
        if (modelLoader.highpolyContainer != null)
            modelLoader.polygonCounter.GetStats(modelLoader.highpolyContainer, out highTris, out highVerts);

        int lowTris = 0, lowVerts = 0;
        if (modelLoader.lowpolyContainer != null)
            modelLoader.polygonCounter.GetStats(modelLoader.lowpolyContainer, out lowTris, out lowVerts);

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
            UpdateStats();
            UpdateTextureGallery();
        }
    }

    private void OnMetallicChanged(bool state)
    {
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

    private void OnModelSelected(string selectedValue)
    {
        if (modelLoader != null && modelDropdown != null)
        {
            int index = modelDropdown.choices.IndexOf(selectedValue);
            if (index >= 0)
            {
                modelLoader.SetCurrentModel(index);
                UpdateStats();
                UpdateTextureGallery();
            }
        }
    }
}
