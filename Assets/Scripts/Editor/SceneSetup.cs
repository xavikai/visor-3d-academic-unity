using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneSetup
{
    public const string ViewerPath = "Assets/Scenes/Viewer.unity";
    public const string StagePath = "Assets/Scenes/Escenari.unity";

    [MenuItem("Visor 3D/Preparar les dues escenes (primera vegada)")]
    public static void GenerateScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Atura Play abans de preparar les escenes.");
        if (File.Exists(ViewerPath) || File.Exists(StagePath) ||
            File.Exists("Assets/Models/Escut.prefab") || File.Exists(ModelCatalogTools.CatalogPath))
            throw new InvalidOperationException("Les escenes ja existeixen. No se sobreescriuen.");
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("Desa els canvis de les escenes obertes abans de continuar.");
        Directory.CreateDirectory("Assets/Models");
        AssetDatabase.Refresh();
        AssetDatabase.CopyAsset("Assets/Scenes/SampleScene.unity", ViewerPath);
        var viewer = EditorSceneManager.OpenScene(ViewerPath, OpenSceneMode.Single);
        var loader = UnityEngine.Object.FindAnyObjectByType<ModelLoader>();
        var container = loader.transform.Find("ModelsContainer");
        loader.modelsContainer = container.gameObject;
        ConfigureLighting();
        var group = new GameObject("Escut");
        group.transform.SetParent(container, false);
        var academic = group.AddComponent<AcademicModel>();
        academic.displayName = "Escut";
        foreach (var child in container.Cast<Transform>().ToArray())
        {
            if (child == group.transform) continue;
            child.SetParent(group.transform, false);
            // The sample FBX shield lies on XZ. Present its face towards the viewer.
            child.localRotation = Quaternion.Euler(90, 0, 0);
            if (child.name.IndexOf("high", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                child.name = "Highpoly";
                academic.highpoly = child.gameObject;
                child.gameObject.SetActive(false);
            }
            else { child.name = "Lowpoly"; academic.lowpoly = child.gameObject; }
        }
        if (!academic.IsValid) throw new InvalidOperationException("No s'ha pogut agrupar l'escut original.");
        var prefab = PrefabUtility.SaveAsPrefabAsset(group, "Assets/Models/Escut.prefab").GetComponent<AcademicModel>();
        UnityEngine.Object.DestroyImmediate(group);
        var catalog = ScriptableObject.CreateInstance<ModelCatalog>();
        catalog.prefabs.Add(prefab);
        catalog.sourceScenePath = StagePath;
        AssetDatabase.CreateAsset(catalog, ModelCatalogTools.CatalogPath);
        loader.catalog = catalog;
        EditorSceneManager.SaveScene(viewer);
        AssetDatabase.SaveAssets();
        CreateStage();
    }

    public static void CreateStage()
    {
        if (File.Exists(StagePath)) throw new InvalidOperationException("L'escenari ja existeix.");
        var stage = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Escut.prefab");
        var objects = new GameObject("Models de l'escenari");
        for (int i = 0; i < 3; i++)
        {
            var copy = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            copy.transform.SetParent(objects.transform, false);
            copy.transform.position = new Vector3((i - 1) * 120, 0, 0);
        }
        var camera = Camera.main;
        camera.transform.position = new Vector3(0, 65, -350);
        camera.transform.LookAt(Vector3.zero);
        camera.farClipPlane = 1500;
        camera.backgroundColor = new Color(0.13f, 0.15f, 0.18f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        EditorSceneManager.SaveScene(stage, StagePath);
        ModelCatalogTools.UpdateFromActiveScene();
        var otherScenes = EditorBuildSettings.scenes.Where(s => s.path != ViewerPath && s.path != StagePath &&
            s.path != "Assets/Scenes/SampleScene.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ViewerPath, true),
            new EditorBuildSettingsScene(StagePath, true) }.Concat(otherScenes).ToArray();
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(ViewerPath, OpenSceneMode.Single);
        Debug.Log("Visor i escenari preparats: tres instàncies de l'escut, una entrada al catàleg.");
    }

    public static void ConfigureLighting()
    {
        var light = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
            .FirstOrDefault(item => item.type == LightType.Directional);
        if (light == null) return;
        light.transform.rotation = Quaternion.Euler(35, 150, 0);
        light.intensity = 2;
    }
}
