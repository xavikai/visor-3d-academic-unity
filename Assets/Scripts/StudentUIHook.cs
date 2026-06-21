using UnityEngine;
using UnityEngine.UI;

public class StudentUIHook : MonoBehaviour
{
    public ModelLoader modelLoader;
    public Toggle albedoToggle;
    public Toggle normalToggle;
    public Slider normalSlider;
    public Toggle metallicToggle;
    public Slider metallicSlider;
    public Slider smoothnessSlider;
    public Toggle wireframeToggle;
    public Toggle vertexColorToggle;
    public Toggle uvToggle;
    public Dropdown modelDropdown;
    public Text statsText;
    
    public Toggle emissionToggle;
    public Slider emissionSlider;
    
    public RawImage imgAlbedo;
    public RawImage imgNormal;
    public RawImage imgMetallic;
    public RawImage imgEmission;

    void Start()
    {
        if (albedoToggle != null) albedoToggle.onValueChanged.AddListener(OnAlbedoChanged);
        if (normalToggle != null) normalToggle.onValueChanged.AddListener(OnNormalChanged);
        if (metallicToggle != null) metallicToggle.onValueChanged.AddListener(OnMetallicChanged);
        if (wireframeToggle != null) wireframeToggle.onValueChanged.AddListener(OnWireframeChanged);
        if (vertexColorToggle != null) vertexColorToggle.onValueChanged.AddListener(OnVertexColorChanged);
        if (uvToggle != null) uvToggle.onValueChanged.AddListener(OnUvChanged);
        
        if (normalSlider != null) normalSlider.onValueChanged.AddListener(OnNormalIntensityChanged);
        if (metallicSlider != null) metallicSlider.onValueChanged.AddListener(OnMetallicIntensityChanged);
        if (smoothnessSlider != null) smoothnessSlider.onValueChanged.AddListener(OnSmoothnessChanged);
        if (emissionSlider != null) emissionSlider.onValueChanged.AddListener(OnEmissionIntensityChanged);
        if (emissionToggle != null) emissionToggle.onValueChanged.AddListener(OnEmissionChanged);
        
        if (modelDropdown != null) modelDropdown.onValueChanged.AddListener(OnModelSelected);
        
        UpdateStats();
        Invoke("UpdateTextureGallery", 0.5f); // Donem mig segon perquè s'inicialitzi el model actiu
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
    }

    private void SetTexture(RawImage img, Texture tex)
    {
        if (img == null) return;
        img.texture = tex;
        img.color = tex != null ? Color.white : new Color(0.15f, 0.15f, 0.15f, 1f);
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
        if (state && uvToggle != null && uvToggle.isOn)
        {
            uvToggle.SetIsOnWithoutNotify(false);
            if (modelLoader != null && modelLoader.materialViewer != null)
                modelLoader.materialViewer.ToggleUV(false);
        }

        if (modelLoader != null && modelLoader.materialViewer != null)
            modelLoader.materialViewer.ToggleVertexColor(state);
    }

    private void OnUvChanged(bool state)
    {
        if (state && vertexColorToggle != null && vertexColorToggle.isOn)
        {
            vertexColorToggle.SetIsOnWithoutNotify(false);
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

    private void OnModelSelected(int index)
    {
        if (modelLoader != null)
        {
            modelLoader.SetCurrentModel(index);
            UpdateStats();
            UpdateTextureGallery();
        }
    }
}
