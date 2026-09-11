using System;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Camera backgrounds are independent from the sky used for lighting and reflections.</summary>
[DisallowMultipleComponent]
public class ViewerEnvironment : MonoBehaviour
{
    public int Background { get; private set; } = 3;
    public bool HdriEnabled { get; private set; }
    public bool ShowHdri { get; private set; }
    public float Intensity { get; private set; } = 1;
    public float Rotation { get; private set; }
    public bool IsUpdating { get; private set; }
    public bool HasHdri => hdri != null;
    public string Error { get; private set; }
    public event Action Changed;

    private Camera viewerCamera;
    private Skybox cameraSky;
    private Material gradient, hdri, originalSky, originalCameraSky;
    private CameraClearFlags originalFlags;
    private Color originalBackground;
    private bool originalCameraSkyEnabled, createdCameraSky, initialized;
    private AmbientMode originalAmbientMode;
    private SphericalHarmonicsL2 originalProbe;
    private float originalAmbientIntensity, originalReflectionIntensity;
    private ReflectionProbe reflection;
    private float refreshAt;
    private int renderId = -1;
    private bool dirty;
    private bool originalRealtimeReflections;
    private float renderStarted;

    public void Initialize()
    {
        if (initialized) return;
        viewerCamera = Camera.main;
        if (viewerCamera == null) return;
        initialized = true;
        originalFlags = viewerCamera.clearFlags;
        originalBackground = viewerCamera.backgroundColor;
        cameraSky = viewerCamera.GetComponent<Skybox>();
        createdCameraSky = cameraSky == null;
        if (createdCameraSky) cameraSky = viewerCamera.gameObject.AddComponent<Skybox>();
        originalCameraSky = cameraSky.material;
        originalCameraSkyEnabled = cameraSky.enabled;
        originalSky = RenderSettings.skybox;
        originalAmbientMode = RenderSettings.ambientMode;
        originalAmbientIntensity = RenderSettings.ambientIntensity;
        originalReflectionIntensity = RenderSettings.reflectionIntensity;
        originalProbe = RenderSettings.ambientProbe;
        originalRealtimeReflections = QualitySettings.realtimeReflectionProbes;
        var gradientAsset = Resources.Load<Material>("Viewer/GradientBackground");
        var hdriAsset = Resources.Load<Material>("Viewer/StudioEnvironment");
        if (gradientAsset != null) gradient = new Material(gradientAsset);
        if (hdriAsset != null) hdri = new Material(hdriAsset);
        var probeObject = new GameObject("HDRI Reflections");
        probeObject.transform.SetParent(transform, false);
        reflection = probeObject.AddComponent<ReflectionProbe>();
        reflection.mode = ReflectionProbeMode.Realtime;
        reflection.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
        reflection.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
        reflection.clearFlags = ReflectionProbeClearFlags.Skybox;
        reflection.cullingMask = 0; // Capture the environment, never the inspected model.
        reflection.resolution = 128;
        reflection.hdr = true;
        reflection.size = Vector3.one * 1000000;
        reflection.importance = 1000;
        reflection.enabled = false;
        Apply();
    }

    public void Set(int background, bool enabledHdri, bool showHdri, float intensity, float rotation)
    {
        bool lightingChanged = HdriEnabled != (enabledHdri && HasHdri) ||
            !Mathf.Approximately(Intensity, intensity) || !Mathf.Approximately(Rotation, rotation);
        Background = Mathf.Clamp(background, 0, 4);
        HdriEnabled = enabledHdri && HasHdri;
        ShowHdri = showHdri;
        Intensity = Mathf.Clamp(intensity, 0, 3);
        Rotation = Mathf.Clamp(rotation, 0, 360);
        Apply(lightingChanged);
    }

    public void ResetDefaults() => Set(3, false, false, 1, 0);

    private void Apply(bool updateLighting = true)
    {
        if (!initialized || !isActiveAndEnabled) return;
        if (gradient != null)
        {
            gradient.SetColor("_Top", Background == 4 ? new Color(.08f, .15f, .27f) : new Color(.24f, .26f, .29f));
            gradient.SetColor("_Bottom", Background == 4 ? new Color(.015f, .03f, .07f) : new Color(.035f, .04f, .05f));
        }
        if (hdri != null)
        {
            hdri.SetFloat("_Exposure", Intensity);
            hdri.SetFloat("_Rotation", Rotation);
        }
        ConfigureCamera(viewerCamera);
        if (updateLighting)
        {
            Error = null;
            if (HdriEnabled)
            {
                QualitySettings.realtimeReflectionProbes = true;
                RenderSettings.skybox = hdri;
                RenderSettings.ambientMode = AmbientMode.Skybox;
                RenderSettings.ambientIntensity = 1;
                RenderSettings.reflectionIntensity = 1;
                reflection.enabled = true;
                dirty = IsUpdating = true;
                refreshAt = Time.realtimeSinceStartup + .2f;
            }
            else
            {
                reflection.enabled = false;
                dirty = IsUpdating = false;
                RestoreLighting();
            }
        }
        Changed?.Invoke();
    }

    // CopyFrom does not copy the Skybox component. Also call this for the video camera.
    public void ConfigureCamera(Camera target)
    {
        if (target == null) return;
        Material visibleSky = HdriEnabled && ShowHdri ? hdri : Background >= 3 ? gradient : null;
        var sky = target.GetComponent<Skybox>();
        if (sky == null) sky = target.gameObject.AddComponent<Skybox>();
        sky.material = visibleSky;
        sky.enabled = visibleSky != null;
        target.clearFlags = visibleSky != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
        target.backgroundColor = Background == 0 ? Color.black : Background == 2 ? new Color(.8f,.8f,.8f) : new Color(.12f,.12f,.12f);
    }

    private void Update()
    {
        if (!HdriEnabled || !initialized) return;
        if (dirty && Time.realtimeSinceStartup >= refreshAt)
        {
            dirty = false;
            DynamicGI.UpdateEnvironment();
            renderId = reflection.RenderProbe();
            renderStarted = Time.realtimeSinceStartup;
            if (renderId < 0) FailReflection();
        }
        if (IsUpdating && !dirty && renderId >= 0 && reflection.IsFinishedRendering(renderId))
        {
            IsUpdating = false;
            Changed?.Invoke();
        }
        else if (IsUpdating && !dirty && Time.realtimeSinceStartup - renderStarted > 10) FailReflection();
    }

    private void FailReflection()
    {
        IsUpdating = false;
        Error = "No s'han pogut generar els reflexos. Desactiva i torna a activar l'HDRI.";
        Changed?.Invoke();
    }

    private void RestoreLighting()
    {
        RenderSettings.skybox = originalSky;
        RenderSettings.ambientMode = originalAmbientMode;
        RenderSettings.ambientIntensity = originalAmbientIntensity;
        RenderSettings.reflectionIntensity = originalReflectionIntensity;
        RenderSettings.ambientProbe = originalProbe;
        QualitySettings.realtimeReflectionProbes = originalRealtimeReflections;
    }

    private void OnEnable() { if (initialized) Apply(); }
    private void OnDisable()
    {
        if (!initialized) return;
        RestoreLighting();
        if (reflection != null) reflection.enabled = false;
        IsUpdating = dirty = false;
        if (viewerCamera != null)
        {
            viewerCamera.clearFlags = originalFlags;
            viewerCamera.backgroundColor = originalBackground;
            cameraSky.material = originalCameraSky;
            cameraSky.enabled = originalCameraSkyEnabled;
        }
    }
    private void OnDestroy()
    {
        if (gradient != null) Destroy(gradient);
        if (hdri != null) Destroy(hdri);
        if (reflection != null) Destroy(reflection.gameObject);
        if (createdCameraSky && cameraSky != null) Destroy(cameraSky);
    }
}
