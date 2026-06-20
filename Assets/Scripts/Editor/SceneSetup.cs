using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class SceneSetup : EditorWindow
{
    [MenuItem("Visor 3D/1. Generar Escena Automàticament")]
    public static void GenerateScene()
    {
        // 1. Crear l'objecte ModelLoader
        GameObject modelLoaderObj = GameObject.Find("ModelLoader");
        if (modelLoaderObj == null) 
        {
            modelLoaderObj = new GameObject("ModelLoader");
        }

        // Afegir components si no existeixen
        var modelLoader = GetOrAddComponent<ModelLoader>(modelLoaderObj);
        var polygonCounter = GetOrAddComponent<PolygonCounter>(modelLoaderObj);
        var evaluator = GetOrAddComponent<Evaluator>(modelLoaderObj);
        var rubricConfig = GetOrAddComponent<RubricConfig>(modelLoaderObj);
        var authManager = GetOrAddComponent<AuthManager>(modelLoaderObj);
        var ollamaClient = GetOrAddComponent<OllamaClient>(modelLoaderObj);
        var diagnosticView = GetOrAddComponent<DiagnosticView>(modelLoaderObj);

        // Enllaçar les dependències internes automàticament
        modelLoader.polygonCounter = polygonCounter;
        polygonCounter.evaluator = evaluator;
        evaluator.rubricConfig = rubricConfig;
        evaluator.ollamaClient = ollamaClient;
        ollamaClient.rubricConfig = rubricConfig;

        // 2. Crear Canvas i EventSystem
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        GameObject canvasObj;
        if (canvas == null)
        {
            canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        else
        {
            canvasObj = canvas.gameObject;
        }

        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // 3. Crear Panells de la UI
        GameObject studentPanel = CreatePanel(canvasObj.transform, "StudentPanel", new Color(0.15f, 0.15f, 0.15f, 1f));
        GameObject teacherPanel = CreatePanel(canvasObj.transform, "TeacherPanel", new Color(0.1f, 0.25f, 0.1f, 1f));
        teacherPanel.SetActive(false);

        // Assignar els panells a l'AuthManager automàticament
        authManager.studentPanel = studentPanel;
        authManager.teacherPanel = teacherPanel;

        Debug.Log("Escena base i dependències creades amb èxit! El ModelLoader i el Canvas estan configurats.");
    }

    private static T GetOrAddComponent<T>(GameObject obj) where T : Component
    {
        T comp = obj.GetComponent<T>();
        if (comp == null) comp = obj.AddComponent<T>();
        return comp;
    }

    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return existing.gameObject;

        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        Image img = panel.AddComponent<Image>();
        img.color = color;
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return panel;
    }
}
