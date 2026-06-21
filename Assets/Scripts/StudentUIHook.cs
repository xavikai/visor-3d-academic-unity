using UnityEngine;
using UnityEngine.UI;

public class StudentUIHook : MonoBehaviour
{
    public ModelLoader modelLoader;
    public Toggle albedoToggle;
    public Toggle normalToggle;
    public Toggle metallicToggle;
    public Toggle wireframeToggle;
    public Toggle vertexColorToggle;
    public Text statsText;

    void Start()
    {
        if (albedoToggle != null) albedoToggle.onValueChanged.AddListener(OnAlbedoChanged);
        if (normalToggle != null) normalToggle.onValueChanged.AddListener(OnNormalChanged);
        if (metallicToggle != null) metallicToggle.onValueChanged.AddListener(OnMetallicChanged);
        if (wireframeToggle != null) wireframeToggle.onValueChanged.AddListener(OnWireframeChanged);
        if (vertexColorToggle != null) vertexColorToggle.onValueChanged.AddListener(OnVertexColorChanged);
        
        UpdateStats();
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

        statsText.text = $"<b>Highpoly:</b> {highTris:N0} tris | {highVerts:N0} verts\n" +
                         $"<b>Lowpoly:</b> {lowTris:N0} tris | {lowVerts:N0} verts";
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
        if (modelLoader != null && modelLoader.materialViewer != null)
            modelLoader.materialViewer.ToggleVertexColor(state);
    }
}
