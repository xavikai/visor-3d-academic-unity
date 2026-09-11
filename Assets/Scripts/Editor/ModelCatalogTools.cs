using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ModelCatalogTools
{
    public const string CatalogPath = "Assets/Models/StageCatalog.asset";

    [MenuItem("Visor 3D/Crear grup de model (Lowpoly + Highpoly)")]
    public static void CreateGroup()
    {
        var root = new GameObject("NouModel");
        Undo.RegisterCreatedObjectUndo(root, "Crear model");
        var model = root.AddComponent<AcademicModel>();
        model.lowpoly = new GameObject("Lowpoly");
        model.lowpoly.transform.SetParent(root.transform, false);
        model.highpoly = new GameObject("Highpoly");
        model.highpoly.transform.SetParent(root.transform, false);
        model.highpoly.SetActive(false);
        Selection.activeGameObject = root;
    }

    [MenuItem("Visor 3D/Actualitzar catàleg des de l'escena activa")]
    public static void UpdateFromActiveScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Atura Play abans d'actualitzar el catàleg.");
        var scene = SceneManager.GetActiveScene();
        if (scene.GetRootGameObjects().Any(root => root.GetComponentInChildren<ModelLoader>(true) != null))
            throw new InvalidOperationException("Obre l'escena de l'escenari, no la del visor.");
        if (string.IsNullOrEmpty(scene.path))
            throw new InvalidOperationException("Desa primer l'escena de l'escenari.");
        if (scene.isDirty)
            throw new InvalidOperationException("Desa els canvis de l'escenari abans d'actualitzar el catàleg.");
        var entries = Collect(scene);
        System.IO.Directory.CreateDirectory("Assets/Models");
        AssetDatabase.Refresh();
        var catalog = AssetDatabase.LoadAssetAtPath<ModelCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<ModelCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }
        Undo.RecordObject(catalog, "Actualitzar catàleg");
        catalog.prefabs = entries;
        catalog.sourceScenePath = scene.path;
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        Debug.Log($"Catàleg actualitzat: {entries.Count} models únics de {scene.name}.");
    }

    public static List<AcademicModel> Collect(Scene scene)
    {
        var unique = new HashSet<AcademicModel>();
        foreach (var root in scene.GetRootGameObjects())
        foreach (var instance in root.GetComponentsInChildren<AcademicModel>(true))
        {
            if (instance.transform.parent != null &&
                instance.transform.parent.GetComponentInParent<AcademicModel>(true) != null)
                throw new InvalidOperationException($"No es poden niar grups AcademicModel: {instance.name}.");
            // Closest source preserves prefab variants as distinct models.
            var prefab = PrefabUtility.GetCorrespondingObjectFromSource(instance);
            if (prefab == null || !prefab.IsValid)
                throw new InvalidOperationException($"Desa {instance.name} com a prefab amb variants vàlides abans d'actualitzar.");
            if (PrefabUtility.HasPrefabInstanceAnyOverrides(instance.gameObject, false))
                throw new InvalidOperationException($"Aplica els canvis de {instance.name} al prefab o crea una Prefab Variant. El visor utilitza el prefab.");
            unique.Add(prefab);
        }
        return unique.OrderBy(model => model.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
