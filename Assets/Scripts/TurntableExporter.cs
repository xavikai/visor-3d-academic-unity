using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Media;
#endif

/// <summary>Deterministic, frame-by-frame turntable export using Unity's editor encoder.</summary>
[DisallowMultipleComponent]
public class TurntableExporter : MonoBehaviour
{
    public ModelLoader modelLoader;
    public bool IsRecording { get; private set; }
    public float Progress { get; private set; }
    public string Status { get; private set; } = "MP4 · 30 fps · sense interfície";
    public string LastOutputPath { get; private set; }
    public event Action Changed;
    public static bool IsSupported
    {
        get
        {
#if UNITY_EDITOR_WIN || UNITY_EDITOR_OSX
            return true;
#else
            return false;
#endif
        }
    }

#if UNITY_EDITOR
    private MediaEncoder encoder;
    private Camera captureCamera;
    private RenderTexture target;
    private Texture2D pixels;
    private Transform model;
    private Vector3 originalPosition, center;
    private Quaternion originalRotation;
    private OrbitCamera orbit;
    private bool orbitWasEnabled;
    private float previousTimeScale;
    private bool previousRunInBackground;
    private bool savedState;
    private int frame, totalFrames;
    private string partialPath, outputPath;

    public static float FrameAngle(int frameIndex, int frameCount) =>
        360f * frameIndex / frameCount;

    public void Begin(int width = 1920, int height = 1080, int seconds = 10)
    {
        if (IsRecording) return;
        if (!IsSupported || !Application.isPlaying)
        {
            SetStatus("Exportació disponible en Play dins de Unity (Windows/macOS).");
            return;
        }
        if (width < 64 || height < 64 || width > 3840 || height > 2160 ||
            width % 2 != 0 || height % 2 != 0 || seconds < 1 || seconds > 60)
        {
            SetStatus("Resolució o durada no vàlides.");
            return;
        }
        var active = modelLoader != null ? modelLoader.GetActiveModel() : null;
        var source = Camera.main;
        var environment = modelLoader != null ? modelLoader.GetComponent<ViewerEnvironment>() : null;
        if (environment != null && (environment.IsUpdating || environment.Error != null))
        {
            SetStatus(environment.Error ?? "Espera que l'HDRI acabi d'actualitzar els reflexos.");
            return;
        }
        if (active == null || source == null)
        {
            SetStatus("Selecciona un model abans d'exportar.");
            return;
        }
        try
        {
            Bounds bounds = default;
            bool found = false;
            foreach (var renderer in active.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled || renderer.name == "WireframeOverlay") continue;
                if (found) bounds.Encapsulate(renderer.bounds);
                else bounds = renderer.bounds;
                found = true;
            }
            if (!found) throw new InvalidOperationException("El model no té geometria visible.");
            model = active.transform;
            originalPosition = model.position;
            originalRotation = model.rotation;
            center = bounds.center;
            orbit = source.GetComponent<OrbitCamera>();
            orbitWasEnabled = orbit != null && orbit.enabled;
            previousTimeScale = Time.timeScale;
            previousRunInBackground = Application.runInBackground;
            savedState = true;
            if (orbit != null) orbit.enabled = false;
            Time.timeScale = 0;
            Application.runInBackground = true;

            var cameraObject = new GameObject("Turntable Capture") { hideFlags = HideFlags.HideAndDontSave };
            captureCamera = cameraObject.AddComponent<Camera>();
            captureCamera.CopyFrom(source);
            if (environment != null && environment.isActiveAndEnabled) environment.ConfigureCamera(captureCamera);
            captureCamera.enabled = false;
            captureCamera.aspect = (float)width / height;
            var additionalData = captureCamera.GetUniversalAdditionalCameraData();
            var sourceData = source.GetUniversalAdditionalCameraData();
            additionalData.renderPostProcessing = sourceData.renderPostProcessing;
            additionalData.volumeLayerMask = sourceData.volumeLayerMask;
            additionalData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            // Sphere bounds retain every corner throughout the full rotation.
            float radius = Mathf.Max(0.01f, bounds.extents.magnitude);
            float vertical = source.fieldOfView * Mathf.Deg2Rad / 2;
            float horizontal = Mathf.Atan(Mathf.Tan(vertical) * captureCamera.aspect);
            float distance = radius / Mathf.Sin(Mathf.Min(vertical, horizontal)) * 1.12f;
            captureCamera.transform.rotation = source.transform.rotation;
            captureCamera.transform.position = center - captureCamera.transform.forward * distance;
            captureCamera.orthographicSize = radius * 1.12f / Mathf.Min(1f, captureCamera.aspect);
            captureCamera.nearClipPlane = Mathf.Max(0.001f, distance / 1000f);
            captureCamera.farClipPlane = Mathf.Max(100f, distance + radius * 4);
            target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            target.Create();
            captureCamera.targetTexture = target;
            pixels = new Texture2D(width, height, TextureFormat.RGBA32, false);

            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Recordings"));
            Directory.CreateDirectory(folder);
            string label = modelLoader.models[modelLoader.CurrentModelIndex].name;
            foreach (char invalid in Path.GetInvalidFileNameChars()) label = label.Replace(invalid, '_');
            if (label.Length > 64) label = label.Substring(0, 64);
            string variant = modelLoader.IsHighpolyActive ? "Highpoly" : "Lowpoly";
            string id = DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            outputPath = Path.Combine(folder, $"{label}_{variant}_360_{id}.mp4");
            partialPath = Path.Combine(folder, "." + Path.GetFileNameWithoutExtension(outputPath) + ".partial.mp4");
            var video = new VideoTrackAttributes
            {
                frameRate = new MediaRational(30),
                width = (uint)width,
                height = (uint)height,
                includeAlpha = false
            };
            encoder = new MediaEncoder(partialPath, video);
            frame = 0;
            totalFrames = seconds * 30;
            Progress = 0;
            IsRecording = true;
            SetStatus("Gravant la volta de 360°… 0%");
        }
        catch (Exception exception) { Finish(false, "No s'ha pogut exportar: " + exception.Message); }
    }

    private void LateUpdate()
    {
        if (!IsRecording) return;
        try
        {
            if (model == null || modelLoader.GetActiveModel() != model.gameObject)
                throw new InvalidOperationException("El model ha canviat durant la gravació.");
            var rotation = Quaternion.AngleAxis(FrameAngle(frame, totalFrames), Vector3.up);
            model.SetPositionAndRotation(center + rotation * (originalPosition - center), rotation * originalRotation);
            var previousTarget = RenderTexture.active;
            try
            {
                captureCamera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0, false);
                pixels.Apply(false);
                if (!encoder.AddFrame(pixels)) throw new IOException("El codificador ha rebutjat un fotograma.");
            }
            finally { RenderTexture.active = previousTarget; }
            frame++;
            Progress = (float)frame / totalFrames;
            if (frame == totalFrames) Finish(true, "Vídeo desat a Recordings.");
            else SetStatus($"Gravant la volta de 360°… {Progress:P0}");
        }
        catch (Exception exception) { Finish(false, "Exportació interrompuda: " + exception.Message); }
    }

    private void Finish(bool success, string message)
    {
        IsRecording = false;
        try
        {
            encoder?.Dispose();
            encoder = null;
            if (success)
            {
                File.Move(partialPath, outputPath);
                LastOutputPath = outputPath;
            }
        }
        catch (Exception exception) { success = false; message = "No s'ha pogut finalitzar el vídeo: " + exception.Message; }
        finally
        {
            encoder = null;
            if (model != null) model.SetPositionAndRotation(originalPosition, originalRotation);
            if (savedState)
            {
                if (orbit != null) orbit.enabled = orbitWasEnabled;
                Time.timeScale = previousTimeScale;
                Application.runInBackground = previousRunInBackground;
            }
            savedState = false;
            model = null;
            orbit = null;
            if (captureCamera != null) { captureCamera.targetTexture = null; Destroy(captureCamera.gameObject); }
            if (target != null) { target.Release(); Destroy(target); }
            if (pixels != null) Destroy(pixels);
            captureCamera = null;
            target = null;
            pixels = null;
            if (!success && !string.IsNullOrEmpty(partialPath))
            {
                try { if (File.Exists(partialPath)) File.Delete(partialPath); }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
                { message += " El fitxer parcial no s'ha pogut eliminar."; }
            }
            partialPath = null;
        }
        SetStatus(message);
    }

    public void Cancel()
    {
        if (IsRecording) Finish(false, "Gravació cancel·lada.");
    }

    public void RevealVideo()
    {
        if (!string.IsNullOrEmpty(LastOutputPath) && File.Exists(LastOutputPath))
            EditorUtility.RevealInFinder(LastOutputPath);
    }
    private void OnDisable() { Cancel(); }
#else
    public void Begin(int width = 1920, int height = 1080, int seconds = 10) =>
        SetStatus("Exportació disponible dins de l'editor Unity.");
    public void Cancel() { }
    public void RevealVideo() { }
#endif

    private void SetStatus(string message)
    {
        Status = message;
        Changed?.Invoke();
    }
}
