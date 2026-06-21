using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.Events;
using UnityEngine.Events;

public class SceneSetup : EditorWindow
{
    [MenuItem("Visor 3D/1. Generar Escena Automàticament (Standalone)")]
    public static void GenerateScene()
    {
        // 1. Crear l'objecte ModelLoader
        GameObject modelLoaderObj = GameObject.Find("ModelLoader");
        if (modelLoaderObj == null) modelLoaderObj = new GameObject("ModelLoader");

        var modelLoader = GetOrAddComponent<ModelLoader>(modelLoaderObj);
        var polygonCounter = GetOrAddComponent<PolygonCounter>(modelLoaderObj);
        var evaluator = GetOrAddComponent<Evaluator>(modelLoaderObj);
        var rubricConfig = GetOrAddComponent<RubricConfig>(modelLoaderObj);
        var authManager = GetOrAddComponent<AuthManager>(modelLoaderObj);
        var ollamaClient = GetOrAddComponent<OllamaClient>(modelLoaderObj);

        modelLoader.polygonCounter = polygonCounter;
        polygonCounter.evaluator = evaluator;
        evaluator.rubricConfig = rubricConfig;
        evaluator.ollamaClient = ollamaClient;

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            var orbit = GetOrAddComponent<OrbitCamera>(mainCam.gameObject);
            orbit.target = modelLoaderObj.transform;
        }

        // 2. Crear Canvas
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
        else canvasObj = canvas.gameObject;

        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // Esborrar panells antics si existeixen per regenerar-los bé
        Transform oldLogin = canvasObj.transform.Find("LoginPanel");
        if (oldLogin != null) DestroyImmediate(oldLogin.gameObject);
        Transform oldTeacher = canvasObj.transform.Find("TeacherPanel");
        if (oldTeacher != null) DestroyImmediate(oldTeacher.gameObject);

        // 3. Crear LoginPanel
        GameObject loginPanel = CreatePanel(canvasObj.transform, "LoginPanel", new Color(0.2f, 0.2f, 0.2f, 0.95f), new Vector2(0.35f, 0.35f), new Vector2(0.65f, 0.65f));
        CreateText(loginPanel.transform, "Titol", "Mode Professor", new Vector2(0, 50), new Vector2(200, 30), TextAnchor.MiddleCenter);
        InputField passInput = CreateInputField(loginPanel.transform, "PasswordInput", new Vector2(0, 0));
        Button loginBtn = CreateButton(loginPanel.transform, "LoginBtn", "Entrar", new Vector2(0, -50));
        
        UnityEventTools.AddPersistentListener(loginBtn.onClick, new UnityAction(authManager.TryLoginTeacher));

        // 4. Crear TeacherPanel
        GameObject teacherPanel = CreatePanel(canvasObj.transform, "TeacherPanel", new Color(0.1f, 0.3f, 0.2f, 0.95f), new Vector2(0.2f, 0.1f), new Vector2(0.8f, 0.9f));
        CreateText(teacherPanel.transform, "Titol", "Avaluació Automàtica", new Vector2(0, 150), new Vector2(300, 30), TextAnchor.MiddleCenter);
        CreateText(teacherPanel.transform, "LabelBudget", "Pressupost Polígons:", new Vector2(-100, 100), new Vector2(200, 30), TextAnchor.MiddleRight);
        InputField budgetInput = CreateInputField(teacherPanel.transform, "BudgetInput", new Vector2(80, 100));
        budgetInput.text = "5000";
        Button evalBtn = CreateButton(teacherPanel.transform, "EvalBtn", "Avaluar", new Vector2(0, 50));
        Text reportText = CreateText(teacherPanel.transform, "ReportText", "L'informe d'Ollama apareixerà aquí...", new Vector2(0, -70), new Vector2(400, 200), TextAnchor.UpperLeft);

        UnityEventTools.AddPersistentListener(evalBtn.onClick, new UnityAction(polygonCounter.AnalitzarMalla));

        // Assignar referències
        authManager.loginPanel = loginPanel;
        authManager.teacherPanel = teacherPanel;
        authManager.passwordInput = passInput;
        rubricConfig.budgetInput = budgetInput;
        ollamaClient.reportTextUI = reportText;

        loginPanel.SetActive(false);
        teacherPanel.SetActive(false);

        Debug.Log("UI generada i connectada automàticament!");
    }

    private static T GetOrAddComponent<T>(GameObject obj) where T : Component
    {
        T comp = obj.GetComponent<T>();
        if (comp == null) comp = obj.AddComponent<T>();
        return comp;
    }

    private static GameObject CreatePanel(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        Image img = panel.AddComponent<Image>();
        img.color = color;
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return panel;
    }

    private static InputField CreateInputField(Transform parent, string name, Vector2 pos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var input = go.AddComponent<InputField>();
        
        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var text = textGo.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.color = Color.black;
        text.alignment = TextAnchor.MiddleLeft;
        
        input.textComponent = text;
        
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(120, 30);
        
        Image img = go.AddComponent<Image>();
        img.color = Color.white;
        input.targetGraphic = img;

        return input;
    }

    private static Button CreateButton(Transform parent, string name, string labelText, Vector2 pos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var btn = go.AddComponent<Button>();
        
        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var text = textGo.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = labelText;
        text.color = Color.black;
        text.alignment = TextAnchor.MiddleCenter;
        
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(120, 30);
        
        Image img = go.AddComponent<Image>();
        img.color = new Color(0.8f, 0.8f, 0.8f);
        btn.targetGraphic = img;

        return btn;
    }

    private static Text CreateText(Transform parent, string name, string labelText, Vector2 pos, Vector2 size, TextAnchor alignment)
    {
        GameObject textGo = new GameObject(name);
        textGo.transform.SetParent(parent, false);
        var text = textGo.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = labelText;
        text.color = Color.white;
        text.alignment = alignment;
        
        RectTransform rect = textGo.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        return text;
    }
}
