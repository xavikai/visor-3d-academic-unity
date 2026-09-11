using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class ModelLoader : MonoBehaviour
{
    public PolygonCounter polygonCounter;
    public GameObject modelsContainer;
    public ModelCatalog catalog;
    [Serializable]
    public class ModelEntry
    {
        public string name;
        public List<GameObject> lowpolyParts = new List<GameObject>();
        public List<GameObject> highpolyParts = new List<GameObject>();
        public GameObject rootFolder;
    }
    [HideInInspector] public List<ModelEntry> models = new List<ModelEntry>();
    [HideInInspector] public MaterialViewer materialViewer;
    public event Action ModelChanged;
    public int CurrentModelIndex { get; private set; } = -1;
    public bool IsHighpolyActive { get; private set; }
    public bool HasBothVariants => CurrentModelIndex >= 0 &&
        models[CurrentModelIndex].lowpolyParts.Count > 0 && models[CurrentModelIndex].highpolyParts.Count > 0;
    public bool IsReady { get; private set; }

    private void Awake()
    {
        polygonCounter = GetComponent<PolygonCounter>() ?? gameObject.AddComponent<PolygonCounter>();
        if (modelsContainer == null)
        {
            var existing = transform.Find("ModelsContainer");
            modelsContainer = existing != null ? existing.gameObject : new GameObject("ModelsContainer");
            modelsContainer.transform.SetParent(transform, false);
        }
        models.Clear();
        if (catalog != null)
        {
            // Preview copies are isolated from the stage and its shared materials.
            foreach (Transform child in modelsContainer.transform) child.gameObject.SetActive(false);
            var seen = new HashSet<AcademicModel>();
            foreach (var prefab in catalog.prefabs)
            {
                if (prefab == null || !seen.Add(prefab)) continue;
                if (!prefab.IsValid) { Debug.LogWarning($"Model invàlid al catàleg: {prefab.name}", prefab); continue; }
                var copy = Instantiate(prefab, modelsContainer.transform, false);
                copy.name = prefab.DisplayName;
                copy.transform.localPosition = Vector3.zero;
                copy.transform.localRotation = Quaternion.identity;
                AddModel(copy);
            }
        }
        else
        {
            foreach (Transform child in modelsContainer.transform)
            {
                var model = child.GetComponent<AcademicModel>();
                if (model != null && model.IsValid) AddModel(model);
                else
                {
                    // Explicit folders also work without a catalog/component.
                    var entry = new ModelEntry { name = child.name, rootFolder = child.gameObject };
                    var low = child.Find("Lowpoly");
                    var high = child.Find("Highpoly");
                    if (low != null) entry.lowpolyParts.Add(low.gameObject);
                    if (high != null) entry.highpolyParts.Add(high.gameObject);
                    if (low == null && high == null) entry.lowpolyParts.Add(child.gameObject);
                    models.Add(entry);
                }
            }
        }
        models.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.name, b.name));
        var labels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in models)
        {
            string label = entry.name;
            int suffix = 2;
            while (!labels.Add(entry.name)) entry.name = label + " (" + suffix++ + ")";
            entry.rootFolder.SetActive(false);
        }
    }

    private void AddModel(AcademicModel model)
    {
        var entry = new ModelEntry { name = model.DisplayName, rootFolder = model.gameObject };
        if (model.lowpoly != null) entry.lowpolyParts.Add(model.lowpoly);
        if (model.highpoly != null) entry.highpolyParts.Add(model.highpoly);
        models.Add(entry);
    }

    private void Start()
    {
        var environment = GetComponent<ViewerEnvironment>() ?? gameObject.AddComponent<ViewerEnvironment>();
        environment.Initialize();
        var studio = GetComponent<StudioLighting>() ?? gameObject.AddComponent<StudioLighting>();
        studio.Initialize(this);
        materialViewer = GetComponent<MaterialViewer>() ?? gameObject.AddComponent<MaterialViewer>();
        materialViewer.Initialize();
        IsReady = true;
        if (models.Count > 0) SetCurrentModel(0);
        else ModelChanged?.Invoke();
    }

    public void SetCurrentModel(int index)
    {
        if (index < 0 || index >= models.Count) return;
        CurrentModelIndex = index;
        UpdateActiveModels();
    }

    public void SetHighpolyActive(bool active) { IsHighpolyActive = active; UpdateActiveModels(); }
    public void ToggleHighpoly(bool state) => SetHighpolyActive(state);
    public void ToggleLowpoly(bool state) => SetHighpolyActive(!state);

    private void UpdateActiveModels()
    {
        foreach (var entry in models)
        {
            foreach (var part in entry.lowpolyParts) part.SetActive(false);
            foreach (var part in entry.highpolyParts) part.SetActive(false);
            entry.rootFolder.SetActive(false);
        }
        if (CurrentModelIndex >= 0)
        {
            var entry = models[CurrentModelIndex];
            if (entry.highpolyParts.Count == 0) IsHighpolyActive = false;
            else if (entry.lowpolyParts.Count == 0) IsHighpolyActive = true;
            entry.rootFolder.SetActive(true);
            foreach (var part in IsHighpolyActive ? entry.highpolyParts : entry.lowpolyParts) part.SetActive(true);
        }
        polygonCounter.SetModel(GetActiveModel());
        AutoFitModel();
        ModelChanged?.Invoke();
    }

    public GameObject GetActiveModel()
    {
        if (CurrentModelIndex < 0 || CurrentModelIndex >= models.Count) return null;
        var entry = models[CurrentModelIndex];
        var parts = IsHighpolyActive ? entry.highpolyParts : entry.lowpolyParts;
        return parts.Count == 1 ? parts[0] : entry.rootFolder;
    }

    public void AutoFitModel()
    {
        var active = GetActiveModel();
        var camera = Camera.main;
        if (active == null || camera == null) return;
        Bounds bounds = default;
        bool found = false;
        foreach (var renderer in active.GetComponentsInChildren<Renderer>())
        {
            if (!renderer.enabled || renderer.name == "WireframeOverlay") continue;
            if (!found) bounds = renderer.bounds;
            else bounds.Encapsulate(renderer.bounds);
            found = true;
        }
        if (!found) return;
        float radius = Mathf.Max(bounds.extents.magnitude, 0.01f);
        float vertical = camera.fieldOfView * Mathf.Deg2Rad / 2;
        float horizontal = Mathf.Atan(Mathf.Tan(vertical) * camera.aspect);
        float distance = radius / Mathf.Sin(Mathf.Min(vertical, horizontal)) * 1.2f;
        camera.nearClipPlane = Mathf.Max(0.001f, distance / 1000f);
        camera.farClipPlane = Mathf.Max(100f, distance + radius * 4);
        var orbit = camera.GetComponent<OrbitCamera>();
        if (orbit != null) orbit.Focus(bounds.center, distance);
    }
}
