using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ViewerValidation
{
    private static int checks;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        checks++;
    }

    [MenuItem("Visor 3D/Validar projecte i proves de regressió")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Atura Play abans de validar.");
        checks = 0;
        var catalog = AssetDatabase.LoadAssetAtPath<ModelCatalog>(ModelCatalogTools.CatalogPath);
        Check(catalog != null && catalog.prefabs.Count > 0, "Catàleg buit o inexistent.");
        Check(catalog.prefabs.Distinct().Count() == catalog.prefabs.Count, "Catàleg duplicat.");
        foreach (var prefab in catalog.prefabs)
        {
            Check(prefab != null && prefab.IsValid, "Variants de prefab invàlides.");
            Check(PrefabUtility.IsPartOfPrefabAsset(prefab), "El catàleg ha de referenciar prefabs.");
        }
        var scene = SceneManager.GetSceneByPath(catalog.sourceScenePath);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(catalog.sourceScenePath, OpenSceneMode.Additive);
        try
        {
            var collected = ModelCatalogTools.Collect(scene);
            Check(collected.Count == catalog.prefabs.Count && !collected.Except(catalog.prefabs).Any(),
                "Catàleg desactualitzat: actualitza'l des de l'escenari.");
            foreach (var root in scene.GetRootGameObjects())
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0,
                    "Script perdut a " + transform.name);
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        var viewer = SceneManager.GetSceneByPath(SceneSetup.ViewerPath);
        bool openedViewer = !viewer.isLoaded;
        if (openedViewer) viewer = EditorSceneManager.OpenScene(SceneSetup.ViewerPath, OpenSceneMode.Additive);
        try
        {
            var loaders = viewer.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ModelLoader>(true)).ToArray();
            Check(loaders.Length == 1, "El visor ha de tenir exactament un ModelLoader.");
            Check(loaders[0].catalog == catalog, "Assigna StageCatalog al visor.");
            Check(loaders[0].modelsContainer != null, "Assigna ModelsContainer al visor.");
            foreach (var root in viewer.GetRootGameObjects())
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0,
                    "Script perdut al visor: " + transform.name);
        }
        finally { if (openedViewer) EditorSceneManager.CloseScene(viewer, true); }
        var enabledScenes = EditorBuildSettings.scenes.Where(s => s.enabled).ToArray();
        Check(enabledScenes.Length >= 2 && enabledScenes[0].path == SceneSetup.ViewerPath &&
            enabledScenes[1].path == SceneSetup.StagePath, "Viewer i Escenari han de ser les dues primeres escenes.");
        var test = new GameObject("Temporary validation");
        var mesh = new Mesh();
        try
        {
            mesh.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up, Vector3.forward };
            mesh.subMeshCount = 2;
            mesh.SetTriangles(new[] { 0, 1, 2 }, 0);
            mesh.SetTriangles(new[] { 0, 2, 3 }, 1);
            test.AddComponent<MeshFilter>().sharedMesh = mesh;
            var counter = test.AddComponent<PolygonCounter>();
            counter.GetStats(test, out int tris, out int verts);
            Check(tris == 2 && verts == 4, "El recompte ha d'incloure tots els submeshes.");
            mesh.UploadMeshData(true);
            counter.GetStats(test, out tris, out verts);
            Check(tris == 2 && verts == 4, "El recompte ha de funcionar sense Read/Write.");
            counter.GetStats(null, out tris, out verts);
            Check(tris == 0 && verts == 0, "Un model nul ha de tenir recompte zero.");
            var group = test.AddComponent<AcademicModel>();
            group.lowpoly = test;
            Check(!group.IsValid, "Una variant no pot ser l'arrel.");
            var child = new GameObject("Lowpoly");
            child.transform.SetParent(test.transform);
            group.lowpoly = child;
            Check(group.IsValid, "S'ha de permetre una única variant.");
            group.highpoly = child;
            Check(!group.IsValid, "Les variants no poden coincidir.");
        }
        finally { UnityEngine.Object.DestroyImmediate(test); UnityEngine.Object.DestroyImmediate(mesh); }
        System.IO.Directory.CreateDirectory("Logs");
        System.IO.File.WriteAllText("Logs/academic-validation.txt", $"PASS: {checks} comprovacions. {DateTime.Now:O}");
        Debug.Log($"Validació correcta: {checks} comprovacions.");
    }
}
