using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class StudioLighting : MonoBehaviour
{
    [Serializable]
    public class LampSettings
    {
        public bool enabled = true;
        [Range(-180, 180)] public float azimuth;
        [Range(-75, 75)] public float elevation;
        [Range(1.5f, 6)] public float distance = 2.5f;
        [Range(0, 5)] public float intensity;
        public Color color = Color.white;
    }

    public LampSettings[] settings;
    public event Action Changed;
    public Vector3 Center { get; private set; }
    public float Radius { get; private set; } = 1;
    public Light GetLight(int index) => lamps[index];
    private readonly Light[] lamps = new Light[3];
    private readonly Dictionary<Light, bool> previousLights = new Dictionary<Light, bool>();
    private ModelLoader loader;
    private GameObject rig;
    private bool initialized, subscribed;
    private float baseAzimuth;

    public void Initialize(ModelLoader modelLoader)
    {
        if (initialized) return;
        initialized = true;
        loader = modelLoader;
        var camera = Camera.main;
        Vector3 facing = camera != null ? -camera.transform.forward : Vector3.forward;
        baseAzimuth = Mathf.Atan2(facing.x, facing.z) * Mathf.Rad2Deg;
        rig = new GameObject("Three Point Lighting");
        SceneManager.MoveGameObjectToScene(rig, gameObject.scene);
        rig.transform.SetParent(transform, false);
        var names = new[] { "Key - Principal", "Fill - Farciment", "Rim - Contorn" };
        for (int i = 0; i < lamps.Length; i++)
        {
            var lampObject = new GameObject(names[i]);
            lampObject.transform.SetParent(rig.transform, false);
            var lamp = lampObject.AddComponent<Light>();
            lamp.type = LightType.Spot;
            lamp.spotAngle = 90;
            lamp.innerSpotAngle = 65;
            lamp.shadows = i == 1 ? LightShadows.None : LightShadows.Soft;
            lamp.renderMode = LightRenderMode.ForcePixel;
            lamp.shadowBias = 0.03f;
            lamp.shadowNormalBias = 0.2f;
            lamps[i] = lamp;
        }
        if (settings == null || settings.Length != 3 || Array.Exists(settings, item => item == null))
            ResetDefaults();
        Activate();
    }

    private void OnEnable() { if (initialized) Activate(); }

    private void Activate()
    {
        if (subscribed) return;
        // Only replace lighting belonging to this viewer's scene.
        previousLights.Clear();
        foreach (var root in gameObject.scene.GetRootGameObjects())
        foreach (var lamp in root.GetComponentsInChildren<Light>(true))
        {
            if (lamp.transform.IsChildOf(rig.transform)) continue;
            previousLights[lamp] = lamp.enabled;
            lamp.enabled = false;
        }
        rig.SetActive(true);
        loader.ModelChanged += FitToModel;
        subscribed = true;
        FitToModel();
    }

    public void ResetDefaults()
    {
        settings = new[]
        {
            new LampSettings { azimuth = -35, elevation = 35, intensity = 2.8f, color = new Color(1, 0.94f, 0.85f) },
            new LampSettings { azimuth = 45, elevation = 15, intensity = 1.0f, color = new Color(0.83f, 0.91f, 1) },
            new LampSettings { azimuth = 155, elevation = 40, intensity = 2.2f }
        };
        Apply();
    }

    public void FitToModel()
    {
        var active = loader != null ? loader.GetActiveModel() : null;
        bool found = false;
        Bounds bounds = default;
        if (active != null)
        foreach (var renderer in active.GetComponentsInChildren<Renderer>())
        {
            if (!renderer.enabled || renderer.name == "WireframeOverlay") continue;
            if (found) bounds.Encapsulate(renderer.bounds);
            else bounds = renderer.bounds;
            found = true;
        }
        if (found)
        {
            Center = bounds.center;
            Radius = Mathf.Max(0.01f, bounds.extents.magnitude);
        }
        if (rig != null) rig.SetActive(found && isActiveAndEnabled);
        Apply();
    }

    public void SetLamp(int index, bool enabled, float azimuth, float elevation, float distance, float intensity, Color color)
    {
        if (index < 0 || index >= 3) return;
        var state = settings[index];
        state.enabled = enabled;
        state.azimuth = Mathf.Clamp(azimuth, -180, 180);
        state.elevation = Mathf.Clamp(elevation, -75, 75);
        state.distance = Mathf.Clamp(distance, 1.5f, 6);
        state.intensity = Mathf.Clamp(intensity, 0, 5);
        state.color = color;
        Apply();
    }

    private void Apply()
    {
        if (settings == null) return;
        for (int i = 0; i < lamps.Length; i++)
        {
            var lamp = lamps[i];
            if (lamp == null) continue;
            var state = settings[i];
            float yaw = (baseAzimuth + state.azimuth) * Mathf.Deg2Rad;
            float pitch = state.elevation * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Sin(yaw) * Mathf.Cos(pitch), Mathf.Sin(pitch), Mathf.Cos(yaw) * Mathf.Cos(pitch));
            lamp.transform.position = Center + direction * (Radius * state.distance);
            lamp.transform.LookAt(Center);
            lamp.range = Radius * 12;
            lamp.shadowNearPlane = Mathf.Max(0.001f, Radius * 0.01f);
            // Compensate for import units; cap radiance to avoid half-float overflow in URP.
            lamp.intensity = Mathf.Min(60000f, state.intensity * Radius * Radius * 4);
            lamp.color = state.color;
            lamp.enabled = state.enabled;
        }
        Changed?.Invoke();
    }

    private void OnDisable()
    {
        if (subscribed && loader != null) loader.ModelChanged -= FitToModel;
        subscribed = false;
        if (rig != null) rig.SetActive(false);
        foreach (var pair in previousLights) if (pair.Key != null) pair.Key.enabled = pair.Value;
        previousLights.Clear();
    }

    private void OnDestroy() { if (rig != null) Destroy(rig); }
}
