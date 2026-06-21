using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor.Events;
#endif

public class SceneSetup : EditorWindow
{
    [MenuItem("Visor 3D/1. Generar Escena Automàticament (Standalone)")]
    public static void GenerateScene()
    {
        // 0. Forçar Read/Write a tots els models carregats a l'escena perquè es pugui avaluar la malla
        MeshFilter[] allFilters = Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude);
        bool modelsReimported = false;
        foreach (var mf in allFilters)
        {
            if (mf.sharedMesh != null && !mf.sharedMesh.isReadable)
            {
                string path = AssetDatabase.GetAssetPath(mf.sharedMesh);
                if (!string.IsNullOrEmpty(path))
                {
                    ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                    if (importer != null && !importer.isReadable)
                    {
                        importer.isReadable = true;
                        importer.SaveAndReimport();
                        modelsReimported = true;
                    }
                }
            }
        }
        if (modelsReimported) Debug.Log("S'ha activat 'Read/Write' automàticament als models per poder llegir-los.");

        // 1. Crear l'objecte ModelLoader i contenidors
        GameObject modelLoaderObj = GameObject.Find("ModelLoader");
        if (modelLoaderObj == null) modelLoaderObj = new GameObject("ModelLoader");

        GameObject highpolyObj = GameObject.Find("HighpolyContainer");
        if (highpolyObj == null) 
        {
            highpolyObj = new GameObject("HighpolyContainer");
            highpolyObj.transform.SetParent(modelLoaderObj.transform);
        }
        
        GameObject lowpolyObj = GameObject.Find("LowpolyContainer");
        if (lowpolyObj == null) 
        {
            lowpolyObj = new GameObject("LowpolyContainer");
            lowpolyObj.transform.SetParent(modelLoaderObj.transform);
        }

        var modelLoader = GetOrAddComponent<ModelLoader>(modelLoaderObj);
        modelLoader.highpolyContainer = highpolyObj;
        modelLoader.lowpolyContainer = lowpolyObj;

        var polygonCounter = GetOrAddComponent<PolygonCounter>(modelLoaderObj);
        var evaluator = GetOrAddComponent<Evaluator>(modelLoaderObj);
        var rubricConfig = GetOrAddComponent<RubricConfig>(modelLoaderObj);
        var authManager = GetOrAddComponent<AuthManager>(modelLoaderObj);
        var ollamaClient = GetOrAddComponent<OllamaClient>(modelLoaderObj);

        modelLoader.polygonCounter = polygonCounter;
        polygonCounter.evaluator = evaluator;
        evaluator.rubricConfig = rubricConfig;
        evaluator.ollamaClient = ollamaClient;
        ollamaClient.rubricConfig = rubricConfig;

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            var orbit = GetOrAddComponent<OrbitCamera>(mainCam.gameObject);
            orbit.target = modelLoaderObj.transform;

            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f); // Gris fosc neutral

#if UNITY_EDITOR
            // 2.2 Entorn HDRI (Per reflexos fotorealistes)
            string hdriPath = "Assets/Resources/studio_small_09_1k.hdr";
            TextureImporter importer = AssetImporter.GetAtPath(hdriPath) as TextureImporter;
            if (importer != null && importer.textureShape != TextureImporterShape.TextureCube)
            {
                importer.textureShape = TextureImporterShape.TextureCube;
                importer.SaveAndReimport();
            }

            Cubemap hdriCubemap = AssetDatabase.LoadAssetAtPath<Cubemap>(hdriPath);
            if (hdriCubemap != null)
            {
                Material skyboxMat = new Material(Shader.Find("Skybox/Cubemap"));
                skyboxMat.name = "HDRI_Skybox";
                skyboxMat.SetTexture("_Tex", hdriCubemap);
                RenderSettings.skybox = skyboxMat;
                DynamicGI.UpdateEnvironment();
            }

            // Add Volume for Post-Processing
            GameObject volumeObj = GameObject.Find("GlobalVolume");
            if (volumeObj == null)
            {
                volumeObj = new GameObject("GlobalVolume");
                var volume = volumeObj.AddComponent<Volume>();
                volume.isGlobal = true;

                VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/ViewerProfile.asset");
                if (profile == null)
                {
                    if (!System.IO.Directory.Exists("Assets/Settings")) System.IO.Directory.CreateDirectory("Assets/Settings");
                    profile = ScriptableObject.CreateInstance<VolumeProfile>();
                    AssetDatabase.CreateAsset(profile, "Assets/Settings/ViewerProfile.asset");

                    var bloom = profile.Add<UnityEngine.Rendering.Universal.Bloom>(true);
                    bloom.intensity.Override(0.5f);
                    bloom.threshold.Override(1.2f);

                    var tonemapping = profile.Add<UnityEngine.Rendering.Universal.Tonemapping>(true);
                    tonemapping.mode.Override(UnityEngine.Rendering.Universal.TonemappingMode.ACES);

                    var vignette = profile.Add<UnityEngine.Rendering.Universal.Vignette>(true);
                    vignette.intensity.Override(0.3f);
                    vignette.smoothness.Override(0.8f);
                    
                    AssetDatabase.SaveAssets();
                }
                volume.profile = profile;
            }

            var camData = mainCam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (camData == null) camData = mainCam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
#endif
        }

        // 2. Crear Canvas
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        GameObject canvasObj;
        if (canvas == null)
        {
            canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

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
        Transform oldStudent = canvasObj.transform.Find("StudentPanel");
        if (oldStudent != null) DestroyImmediate(oldStudent.gameObject);
        Transform oldLogin = canvasObj.transform.Find("LoginPanel");
        if (oldLogin != null) DestroyImmediate(oldLogin.gameObject);
        Transform oldTeacher = canvasObj.transform.Find("TeacherPanel");
        if (oldTeacher != null) DestroyImmediate(oldTeacher.gameObject);

        // 3. Crear StudentPanel (Visor Sketchfab)
        GameObject studentPanel = CreatePanel(canvasObj.transform, "StudentPanel", new Color(0, 0, 0, 0), new Vector2(0, 0), new Vector2(1, 1));
        
        // Panell de controls inferior/lateral
        GameObject controlsPanel = CreatePanel(studentPanel.transform, "Controls", new Color(0.1f, 0.1f, 0.1f, 0.8f), new Vector2(0, 0), new Vector2(0, 1));
        RectTransform ctrlRect = controlsPanel.GetComponent<RectTransform>();
        ctrlRect.pivot = new Vector2(0, 0.5f);
        ctrlRect.anchoredPosition = new Vector2(0, 0);
        ctrlRect.sizeDelta = new Vector2(350, 0); // Ample real i ben ancorat

        CreateText(controlsPanel.transform, "Title", "Visor de l'Alumne", new Vector2(0, 400), new Vector2(350, 40), TextAnchor.MiddleCenter, 32);

        // Toggles Model
        CreateText(controlsPanel.transform, "LblModel", "Model:", new Vector2(-100, 330), new Vector2(100, 40), TextAnchor.MiddleLeft, 24);
        Dropdown modelDropdown = CreateDropdown(controlsPanel.transform, "ModelDropdown", new Vector2(70, 330));
        
        Toggle highpolyToggle = CreateToggle(controlsPanel.transform, "HighpolyToggle", "Mostrar Highpoly", new Vector2(0, 280));
        highpolyToggle.isOn = false;
        UnityEventTools.AddPersistentListener(highpolyToggle.onValueChanged, new UnityAction<bool>(modelLoader.ToggleHighpoly));

        // Toggles Materials
        CreateText(controlsPanel.transform, "LblMat", "Canals de Material:", new Vector2(20, 200), new Vector2(300, 40), TextAnchor.MiddleLeft, 24);
        Toggle albedoToggle = CreateToggle(controlsPanel.transform, "AlbedoToggle", "Color (Albedo)", new Vector2(0, 150));
        
        Toggle normalToggle = CreateToggle(controlsPanel.transform, "NormalToggle", "Relleu (Normal Map)", new Vector2(0, 100));
        CreateText(controlsPanel.transform, "LblNormalInt", "Intensitat:", new Vector2(0, 75), new Vector2(100, 20), TextAnchor.MiddleLeft, 14);
        Slider normalSlider = CreateSlider(controlsPanel.transform, "NormalSlider", new Vector2(90, 75), 0f, 2f, 1f);
        
        Toggle metallicToggle = CreateToggle(controlsPanel.transform, "MetallicToggle", "Metall/Rugositat", new Vector2(0, 20));
        CreateText(controlsPanel.transform, "LblMetallic", "Metàl·lic:", new Vector2(0, -5), new Vector2(100, 20), TextAnchor.MiddleLeft, 14);
        Slider metallicSlider = CreateSlider(controlsPanel.transform, "MetallicSlider", new Vector2(90, -5), 0f, 1f, 0f);
        CreateText(controlsPanel.transform, "LblSmooth", "Rugositat:", new Vector2(0, -30), new Vector2(100, 20), TextAnchor.MiddleLeft, 14);
        Slider smoothnessSlider = CreateSlider(controlsPanel.transform, "SmoothnessSlider", new Vector2(90, -30), 0f, 1f, 0.5f);
        
        Toggle emissionToggle = CreateToggle(controlsPanel.transform, "EmissionToggle", "Llum (Emissió)", new Vector2(0, -70));
        CreateText(controlsPanel.transform, "LblEmission", "Intensitat:", new Vector2(0, -95), new Vector2(100, 20), TextAnchor.MiddleLeft, 14);
        Slider emissionSlider = CreateSlider(controlsPanel.transform, "EmissionSlider", new Vector2(90, -95), 0f, 5f, 1f);
        
        Toggle wireframeToggle = CreateToggle(controlsPanel.transform, "WireframeToggle", "Malla (Wireframe)", new Vector2(0, -140));
        wireframeToggle.isOn = false;
        Toggle vertexColorToggle = CreateToggle(controlsPanel.transform, "VertexColorToggle", "Vertex Colors", new Vector2(0, -190));
        vertexColorToggle.isOn = false;
        Toggle uvToggle = CreateToggle(controlsPanel.transform, "UvToggle", "UV Checker", new Vector2(0, -240));
        uvToggle.isOn = false;

        // Panell de Textures (Galeria 2D) a la dreta
        GameObject textureGalleryPanel = CreatePanel(studentPanel.transform, "TextureGalleryPanel", new Color(0.1f, 0.1f, 0.1f, 0.9f), new Vector2(1, 0), new Vector2(1, 1));
        RectTransform tgRect = textureGalleryPanel.GetComponent<RectTransform>();
        tgRect.pivot = new Vector2(1, 0.5f);
        tgRect.anchoredPosition = new Vector2(0, 0);
        tgRect.sizeDelta = new Vector2(140, 0); // 140 width, full height
        
        CreateText(textureGalleryPanel.transform, "LblGallery", "Textures 2D", new Vector2(0, 480), new Vector2(140, 30), TextAnchor.UpperCenter, 18);
        
        CreateText(textureGalleryPanel.transform, "LblUv", "UV Layout", new Vector2(0, 440), new Vector2(120, 20), TextAnchor.MiddleCenter, 14);
        RawImage imgUv = CreateRawImage(textureGalleryPanel.transform, "ImgUv", new Vector2(0, 370));

        CreateText(textureGalleryPanel.transform, "LblAlb", "Albedo", new Vector2(0, 280), new Vector2(120, 20), TextAnchor.MiddleCenter, 14);
        RawImage imgAlbedo = CreateRawImage(textureGalleryPanel.transform, "ImgAlbedo", new Vector2(0, 210));
        
        CreateText(textureGalleryPanel.transform, "LblNrm", "Normal", new Vector2(0, 120), new Vector2(120, 20), TextAnchor.MiddleCenter, 14);
        RawImage imgNormal = CreateRawImage(textureGalleryPanel.transform, "ImgNormal", new Vector2(0, 50));
        
        CreateText(textureGalleryPanel.transform, "LblMet", "Metallic/Smooth", new Vector2(0, -40), new Vector2(120, 20), TextAnchor.MiddleCenter, 14);
        RawImage imgMetallic = CreateRawImage(textureGalleryPanel.transform, "ImgMetallic", new Vector2(0, -110));
        
        CreateText(textureGalleryPanel.transform, "LblEmi", "Emission", new Vector2(0, -200), new Vector2(120, 20), TextAnchor.MiddleCenter, 14);
        RawImage imgEmission = CreateRawImage(textureGalleryPanel.transform, "ImgEmission", new Vector2(0, -270));

        // Assignar Shader desempaquetador al Normal Map
        Shader normalShader = Shader.Find("UI/NormalUnpack");
        if (normalShader != null)
        {
            Material normalMat = new Material(normalShader);
            imgNormal.material = normalMat;
        }

        // Panell de Zoom per textures
        GameObject zoomPanel = CreatePanel(canvasObj.transform, "TextureZoomPanel", new Color(0, 0, 0, 0.95f), new Vector2(0, 0), new Vector2(1, 1));
        Text zoomTitle = CreateText(zoomPanel.transform, "ZoomTitle", "Nom Textura", new Vector2(0, 450), new Vector2(800, 50), TextAnchor.MiddleCenter, 32);
        RawImage imgZoom = CreateRawImage(zoomPanel.transform, "ImgZoom", new Vector2(0, -20));
        imgZoom.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 800);
        Button btnCloseZoom = zoomPanel.AddComponent<Button>();
        zoomPanel.SetActive(false);

        // Estadístiques
        CreateText(controlsPanel.transform, "LblStats", "Estadístiques:", new Vector2(20, -300), new Vector2(360, 40), TextAnchor.MiddleLeft, 24);
        Text statsText = CreateText(controlsPanel.transform, "StatsText", "Calculant...", new Vector2(20, -460), new Vector2(360, 250), TextAnchor.UpperLeft, 18);
        
        // Aquests es connectaran per codi durant el Start perquè el MaterialViewer es crea dinàmicament
        var hook = GetOrAddComponent<StudentUIHook>(studentPanel);
        hook.modelLoader = modelLoader;
        hook.modelDropdown = modelDropdown;
        hook.albedoToggle = albedoToggle;
        hook.normalToggle = normalToggle;
        hook.normalSlider = normalSlider;
        hook.metallicToggle = metallicToggle;
        hook.metallicSlider = metallicSlider;
        hook.smoothnessSlider = smoothnessSlider;
        hook.emissionToggle = emissionToggle;
        hook.emissionSlider = emissionSlider;
        hook.wireframeToggle = wireframeToggle;
        hook.vertexColorToggle = vertexColorToggle;
        hook.uvToggle = uvToggle;
        hook.statsText = statsText;
        hook.imgAlbedo = imgAlbedo;
        hook.imgNormal = imgNormal;
        hook.imgMetallic = imgMetallic;
        hook.imgEmission = imgEmission;
        hook.imgUv = imgUv;
        
        hook.zoomPanel = zoomPanel;
        hook.imgZoom = imgZoom;
        hook.zoomTitle = zoomTitle;
        hook.btnCloseZoom = btnCloseZoom;

        // 4. Crear LoginPanel
        GameObject loginPanel = CreatePanel(canvasObj.transform, "LoginPanel", new Color(0.2f, 0.2f, 0.2f, 0.95f), new Vector2(0.35f, 0.35f), new Vector2(0.65f, 0.65f));
        CreateText(loginPanel.transform, "Titol", "Mode Professor", new Vector2(0, 50), new Vector2(200, 30), TextAnchor.MiddleCenter);
        InputField passInput = CreateInputField(loginPanel.transform, "PasswordInput", new Vector2(0, 0));
        Button loginBtn = CreateButton(loginPanel.transform, "LoginBtn", "Entrar", new Vector2(0, -50));
        
        UnityEventTools.AddPersistentListener(loginBtn.onClick, new UnityAction(authManager.TryLoginTeacher));

        // 5. Crear TeacherPanel
        GameObject teacherPanel = CreatePanel(canvasObj.transform, "TeacherPanel", new Color(0.1f, 0.3f, 0.2f, 0.95f), new Vector2(0.2f, 0.1f), new Vector2(0.8f, 0.9f));
        CreateText(teacherPanel.transform, "Titol", "Avaluació Automàtica", new Vector2(0, 150), new Vector2(300, 30), TextAnchor.MiddleCenter);
        CreateText(teacherPanel.transform, "LabelBudget", "Pressupost Polígons:", new Vector2(-100, 100), new Vector2(200, 30), TextAnchor.MiddleRight);
        InputField budgetInput = CreateInputField(teacherPanel.transform, "BudgetInput", new Vector2(80, 100));
        budgetInput.text = "5000";
        Button evalBtn = CreateButton(teacherPanel.transform, "EvalBtn", "Avaluar", new Vector2(0, 50));
        Text reportText = CreateText(teacherPanel.transform, "ReportText", "L'informe d'Ollama apareixerà aquí...", new Vector2(0, -70), new Vector2(400, 200), TextAnchor.UpperLeft);

        UnityEventTools.AddPersistentListener(evalBtn.onClick, new UnityAction(polygonCounter.AnalitzarMalla));

        // Assignar referències
        authManager.studentPanel = studentPanel;
        authManager.loginPanel = loginPanel;
        authManager.teacherPanel = teacherPanel;
        authManager.passwordInput = passInput;
        rubricConfig.budgetInput = budgetInput;
        ollamaClient.reportTextUI = reportText;

        loginPanel.SetActive(false);
        teacherPanel.SetActive(false);
        studentPanel.SetActive(true);

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
        GameObject panel = new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        Image img = panel.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = color.a > 0f;
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return panel;
    }

    private static RawImage CreateRawImage(Transform parent, string name, Vector2 pos)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RawImage img = go.AddComponent<RawImage>();
        img.color = new Color(0.15f, 0.15f, 0.15f, 1f); // Gris fosc de fons si no hi ha textura
        img.raycastTarget = true;
        go.AddComponent<Button>(); // Botó per ampliar
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(120, 120);
        return img;
    }

    private static InputField CreateInputField(Transform parent, string name, Vector2 pos)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var input = go.AddComponent<InputField>();
        
        GameObject textGo = new GameObject("Text", typeof(RectTransform));
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
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var btn = go.AddComponent<Button>();
        
        GameObject textGo = new GameObject("Text", typeof(RectTransform));
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

    private static Toggle CreateToggle(Transform parent, string name, string labelText, Vector2 pos, int fontSize = 20)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var toggle = go.AddComponent<Toggle>();
        
        GameObject bgGo = new GameObject("Background", typeof(RectTransform));
        bgGo.transform.SetParent(go.transform, false);
        var bgImage = bgGo.AddComponent<Image>();
        bgImage.color = Color.white;
        RectTransform bgRect = bgGo.GetComponent<RectTransform>();
        bgRect.anchoredPosition = new Vector2(-150, 0); // Desplaçat cap a l'esquerra tenint en compte que l'amplada és 350
        bgRect.sizeDelta = new Vector2(28, 28);
        
        GameObject checkGo = new GameObject("Checkmark", typeof(RectTransform));
        checkGo.transform.SetParent(bgGo.transform, false);
        var checkImage = checkGo.AddComponent<Image>();
        checkImage.color = Color.black;
        RectTransform checkRect = checkGo.GetComponent<RectTransform>();
        checkRect.anchoredPosition = Vector2.zero;
        checkRect.sizeDelta = new Vector2(20, 20);
        
        GameObject textGo = new GameObject("Label", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var text = textGo.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = labelText;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        text.fontSize = fontSize;
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchoredPosition = new Vector2(20, 0); // El text comença a la dreta del centre
        textRect.sizeDelta = new Vector2(280, 40);
        
        toggle.targetGraphic = bgImage;
        toggle.graphic = checkImage;
        toggle.isOn = true;
        
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(350, 40);

        return toggle;
    }

#if UNITY_EDITOR
    private static Slider CreateSlider(Transform parent, string name, Vector2 pos, float min = 0f, float max = 1f, float def = 1f)
    {
        DefaultControls.Resources uiResources = new DefaultControls.Resources();
        uiResources.standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        uiResources.background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        uiResources.knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        GameObject go = DefaultControls.CreateSlider(uiResources);
        go.name = name;
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(150, 20);
        
        Slider slider = go.GetComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = def;
        return slider;
    }

    private static Dropdown CreateDropdown(Transform parent, string name, Vector2 pos)
    {
        DefaultControls.Resources uiResources = new DefaultControls.Resources();
        uiResources.standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        uiResources.background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        uiResources.dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd");
        uiResources.mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd");
        uiResources.checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");

        GameObject go = DefaultControls.CreateDropdown(uiResources);
        go.name = name;
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(180, 30);
        
        Text[] texts = go.GetComponentsInChildren<Text>(true);
        foreach (var text in texts) text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        
        return go.GetComponent<Dropdown>();
    }
#endif

    private static Text CreateText(Transform parent, string name, string labelText, Vector2 pos, Vector2 size, TextAnchor alignment, int fontSize = 20)
    {
        GameObject textGo = new GameObject(name, typeof(RectTransform));
        textGo.transform.SetParent(parent, false);
        var text = textGo.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = labelText;
        text.color = Color.white;
        text.alignment = alignment;
        text.fontSize = fontSize;
        text.raycastTarget = false;
        
        RectTransform rect = textGo.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        return text;
    }
}
