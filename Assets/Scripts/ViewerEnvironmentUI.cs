using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public class ViewerEnvironmentUI : MonoBehaviour
{
    private ViewerEnvironment environment;
    private VisualElement boundRoot;
    private DropdownField background;
    private Toggle enabledHdri, showHdri;
    private Slider intensity, rotation;
    private Label status;

    public void Bind(VisualElement root, ModelLoader loader)
    {
        if (root == boundRoot || loader == null) return;
        if (environment != null) environment.Changed -= Refresh;
        boundRoot = root;
        environment = loader.GetComponent<ViewerEnvironment>() ?? loader.gameObject.AddComponent<ViewerEnvironment>();
        environment.Initialize();
        background = root.Q<DropdownField>("BackgroundChoice");
        if (background == null) return;
        enabledHdri = root.Q<Toggle>("HdriEnabled");
        showHdri = root.Q<Toggle>("ShowHdriBackground");
        intensity = root.Q<Slider>("HdriIntensity");
        rotation = root.Q<Slider>("HdriRotation");
        status = root.Q<Label>("EnvironmentStatus");
        background.choices = new List<string> { "Negre", "Gris", "Clar", "Degradat gris", "Degradat blau" };
        background.RegisterValueChangedCallback(_ => Apply());
        enabledHdri.RegisterValueChangedCallback(_ => Apply());
        showHdri.RegisterValueChangedCallback(_ => Apply());
        intensity.RegisterValueChangedCallback(_ => Apply());
        rotation.RegisterValueChangedCallback(_ => Apply());
        root.Q<Button>("ResetEnvironment").clicked += environment.ResetDefaults;
        environment.Changed += Refresh;
        Refresh();
    }
    private void Apply() => environment.Set(background.index, enabledHdri.value, showHdri.value, intensity.value, rotation.value);
    private void Refresh()
    {
        if (background == null) return;
        background.SetValueWithoutNotify(background.choices[environment.Background]);
        enabledHdri.SetValueWithoutNotify(environment.HdriEnabled);
        enabledHdri.SetEnabled(environment.HasHdri);
        showHdri.SetValueWithoutNotify(environment.ShowHdri);
        intensity.SetValueWithoutNotify(environment.Intensity);
        rotation.SetValueWithoutNotify(environment.Rotation);
        showHdri.SetEnabled(environment.HdriEnabled);
        intensity.SetEnabled(environment.HdriEnabled);
        rotation.SetEnabled(environment.HdriEnabled);
        background.SetEnabled(!environment.HdriEnabled || !environment.ShowHdri);
        status.text = !environment.HasHdri ? "No s'ha trobat l'HDRI d'estudi." : environment.Error != null ? environment.Error : environment.IsUpdating ? "Actualitzant reflexos…" :
            environment.HdriEnabled ? $"HDRI d'estudi · Intensitat {environment.Intensity:0.0}\nRotació {environment.Rotation:0}°" : "HDRI desactivat · Fons independent de les llums";
    }
    private void OnDestroy() { if (environment != null) environment.Changed -= Refresh; }
}
