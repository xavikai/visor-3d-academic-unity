using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public class StudioLightingUI : MonoBehaviour
{
    private StudioLighting lighting;
    private VisualElement boundRoot;
    private DropdownField selected, color;
    private Toggle enabledToggle;
    private Slider azimuth, elevation, distance, intensity;
    private Label summary;
    private readonly Color[] colors = { Color.white, new Color(1, 0.94f, 0.85f),
        new Color(0.83f, 0.91f, 1), new Color(1, 0.2f, 0.1f), new Color(0.2f, 0.5f, 1), new Color(0.3f, 1, 0.4f) };

    public void Bind(VisualElement root, ModelLoader loader)
    {
        if (boundRoot == root || loader == null) return;
        if (lighting != null) lighting.Changed -= Refresh;
        boundRoot = root;
        lighting = loader.GetComponent<StudioLighting>() ?? loader.gameObject.AddComponent<StudioLighting>();
        lighting.Initialize(loader);
        selected = root.Q<DropdownField>("SelectedLight");
        if (selected == null) return;
        color = root.Q<DropdownField>("LightColor");
        enabledToggle = root.Q<Toggle>("LightEnabled");
        azimuth = root.Q<Slider>("LightAzimuth");
        elevation = root.Q<Slider>("LightElevation");
        distance = root.Q<Slider>("LightDistance");
        intensity = root.Q<Slider>("LightIntensity");
        summary = root.Q<Label>("LightSummary");
        selected.choices = new List<string> { "Principal", "Farciment", "Contorn" };
        selected.SetValueWithoutNotify("Principal");
        color.choices = new List<string> { "Blanc", "Càlid", "Fred", "Vermell", "Blau", "Verd" };
        selected.RegisterValueChangedCallback(_ => Refresh());
        enabledToggle.RegisterValueChangedCallback(_ => Apply());
        azimuth.RegisterValueChangedCallback(_ => Apply());
        elevation.RegisterValueChangedCallback(_ => Apply());
        distance.RegisterValueChangedCallback(_ => Apply());
        intensity.RegisterValueChangedCallback(_ => Apply());
        color.RegisterValueChangedCallback(_ => Apply());
        root.Q<Button>("ResetLighting").clicked += lighting.ResetDefaults;
        lighting.Changed += Refresh;
        Refresh();
    }

    private void Apply()
    {
        if (lighting == null || selected.index < 0) return;
        lighting.SetLamp(selected.index, enabledToggle.value, azimuth.value, elevation.value,
            distance.value, intensity.value, colors[Mathf.Max(0, color.index)]);
    }

    private void Refresh()
    {
        if (lighting == null || selected == null || selected.index < 0) return;
        var state = lighting.settings[selected.index];
        enabledToggle.SetValueWithoutNotify(state.enabled);
        azimuth.SetValueWithoutNotify(state.azimuth);
        elevation.SetValueWithoutNotify(state.elevation);
        distance.SetValueWithoutNotify(state.distance);
        intensity.SetValueWithoutNotify(state.intensity);
        int closest = 0;
        float difference = float.MaxValue;
        for (int i = 0; i < colors.Length; i++)
        {
            float delta = Vector4.Distance(colors[i], state.color);
            if (delta < difference) { closest = i; difference = delta; }
        }
        color.SetValueWithoutNotify(color.choices[closest]);
        summary.text = $"Angle {state.azimuth:0}° · Altura {state.elevation:0}°\nDistància {state.distance:0.0}× · Intensitat {state.intensity:0.0}";
    }

    private void OnDestroy() { if (lighting != null) lighting.Changed -= Refresh; }
}
