using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public class TurntableUI : MonoBehaviour
{
    private TurntableExporter exporter;
    private ModelLoader loader;
    private VisualElement boundRoot;
    private Button exportButton, cancelButton, revealButton;
    private DropdownField resolution, duration;
    private Label status;
    private bool locked;
    private readonly Dictionary<VisualElement, bool> controlStates = new Dictionary<VisualElement, bool>();

    public void Bind(VisualElement root, ModelLoader modelLoader)
    {
        if (boundRoot == root) return;
        foreach (var pair in controlStates) pair.Key.SetEnabled(pair.Value);
        controlStates.Clear();
        locked = false;
        if (exporter != null) exporter.Changed -= Refresh;
        if (loader != null) loader.ModelChanged -= Refresh;
        boundRoot = root;
        loader = modelLoader;
        exporter = GetComponent<TurntableExporter>() ?? gameObject.AddComponent<TurntableExporter>();
        exporter.modelLoader = loader;
        exportButton = root.Q<Button>("ExportTurntable");
        cancelButton = root.Q<Button>("CancelTurntable");
        revealButton = root.Q<Button>("RevealTurntable");
        resolution = root.Q<DropdownField>("TurntableResolution");
        duration = root.Q<DropdownField>("TurntableDuration");
        status = root.Q<Label>("TurntableStatus");
        if (exportButton == null) return;
        resolution.choices = new List<string> { "1080p", "720p" };
        resolution.SetValueWithoutNotify("1080p");
        duration.choices = new List<string> { "10 s", "5 s", "15 s", "20 s" };
        duration.SetValueWithoutNotify("10 s");
        exportButton.clicked += () => exporter.Begin(resolution.index == 1 ? 1280 : 1920,
            resolution.index == 1 ? 720 : 1080, int.Parse(duration.value.Split(' ')[0]));
        cancelButton.clicked += exporter.Cancel;
        revealButton.clicked += exporter.RevealVideo;
        exporter.Changed += Refresh;
        if (loader != null) loader.ModelChanged += Refresh;
        Refresh();
    }

    private void Refresh()
    {
        if (exportButton == null || exporter == null) return;
        bool recording = exporter.IsRecording;
        if (recording && !locked)
        {
            controlStates.Clear();
            boundRoot.Query<DropdownField>().ForEach(Lock);
            boundRoot.Query<Toggle>().ForEach(Lock);
            boundRoot.Query<Slider>().ForEach(Lock);
            var lightingControls = boundRoot.Q<VisualElement>("LightingControls");
            if (lightingControls != null) Lock(lightingControls);
            var environmentControls = boundRoot.Q<VisualElement>("EnvironmentControls");
            if (environmentControls != null) Lock(environmentControls);
            locked = true;
        }
        else if (!recording && locked)
        {
            foreach (var pair in controlStates) pair.Key.SetEnabled(pair.Value);
            controlStates.Clear();
            locked = false;
        }
        exportButton.SetEnabled(!recording && TurntableExporter.IsSupported && loader != null && loader.GetActiveModel() != null);
        cancelButton.style.display = recording ? DisplayStyle.Flex : DisplayStyle.None;
        revealButton.style.display = !recording && !string.IsNullOrEmpty(exporter.LastOutputPath) ? DisplayStyle.Flex : DisplayStyle.None;
        status.text = TurntableExporter.IsSupported ? exporter.Status : "Exportació disponible a Unity (Windows/macOS).";
    }

    private void Lock(VisualElement control)
    {
        controlStates[control] = control.enabledSelf;
        control.SetEnabled(false);
    }

    private void OnDestroy()
    {
        if (exporter != null) exporter.Changed -= Refresh;
        if (loader != null) loader.ModelChanged -= Refresh;
    }
}
