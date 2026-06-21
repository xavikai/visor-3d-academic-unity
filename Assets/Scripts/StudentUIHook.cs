using UnityEngine;
using UnityEngine.UI;

public class StudentUIHook : MonoBehaviour
{
    public ModelLoader modelLoader;
    public Toggle albedoToggle;
    public Toggle normalToggle;
    public Toggle metallicToggle;
    public Toggle wireframeToggle;

    void Start()
    {
        if (albedoToggle != null) albedoToggle.onValueChanged.AddListener(OnAlbedoChanged);
        if (normalToggle != null) normalToggle.onValueChanged.AddListener(OnNormalChanged);
        if (metallicToggle != null) metallicToggle.onValueChanged.AddListener(OnMetallicChanged);
        if (wireframeToggle != null) wireframeToggle.onValueChanged.AddListener(OnWireframeChanged);
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
}
