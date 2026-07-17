using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ModelLoader : MonoBehaviour
{
    public PolygonCounter polygonCounter;
    
    public GameObject modelsContainer;

    [System.Serializable]
    public class ModelEntry
    {
        public string name;
        public List<GameObject> lowpolyParts = new List<GameObject>();
        public List<GameObject> highpolyParts = new List<GameObject>();
        public GameObject rootFolder;
    }

    [HideInInspector]
    public List<ModelEntry> models = new List<ModelEntry>();

    [HideInInspector]
    public MaterialViewer materialViewer;

    private bool isHighpolyActive = false;
    private int currentModelIndex = 0;

    void Awake()
    {
        if (polygonCounter == null)
        {
            polygonCounter = GetComponent<PolygonCounter>();
            if (polygonCounter == null) polygonCounter = gameObject.AddComponent<PolygonCounter>();
        }

        if (modelsContainer == null)
        {
            Transform t = transform.Find("ModelsContainer");
            if (t != null) modelsContainer = t.gameObject;
        }

        if (modelsContainer != null)
        {
            Dictionary<string, ModelEntry> dict = new Dictionary<string, ModelEntry>();

            foreach (Transform child in modelsContainer.transform)
            {
                string lowerName = child.name.ToLower();
                bool hasKeyword = lowerName.Contains("low") || lowerName.Contains("high") || lowerName.Contains("baixa") || lowerName.Contains("alta");
                
                string baseName = child.name;
                if (hasKeyword)
                {
                    // Netejar el sufix per trobar el nom base (ex: "escut_lowpoly" -> "escut")
                    baseName = System.Text.RegularExpressions.Regex.Replace(baseName, @"(?i)[_\-\s]*(low|high|baixa|alta)(poly)?.*$", "");
                }
                
                if (string.IsNullOrEmpty(baseName)) baseName = "Model";

                if (!dict.ContainsKey(baseName))
                {
                    dict[baseName] = new ModelEntry();
                    dict[baseName].name = baseName;
                    dict[baseName].rootFolder = null;
                }

                ModelEntry entry = dict[baseName];

                if (hasKeyword)
                {
                    if (lowerName.Contains("high") || lowerName.Contains("alta"))
                        entry.highpolyParts.Add(child.gameObject);
                    else
                        entry.lowpolyParts.Add(child.gameObject);
                }
                else
                {
                    // Comprovar si actua com una carpeta (té fills i algun fill té la paraula clau)
                    bool childHasKeyword = false;
                    foreach (Transform sub in child)
                    {
                        string subL = sub.name.ToLower();
                        if (subL.Contains("low") || subL.Contains("high") || subL.Contains("baixa") || subL.Contains("alta"))
                        {
                            childHasKeyword = true;
                            break;
                        }
                    }

                    if (child.childCount > 0 && childHasKeyword)
                    {
                        entry.rootFolder = child.gameObject;
                        foreach (Transform subChild in child)
                        {
                            string subL = subChild.name.ToLower();
                            if (subL.Contains("high") || subL.Contains("alta")) 
                                entry.highpolyParts.Add(subChild.gameObject);
                            else if (subL.Contains("low") || subL.Contains("baixa")) 
                                entry.lowpolyParts.Add(subChild.gameObject);
                        }
                        
                        // Assignar peces orfes dins la carpeta
                        foreach (Transform subChild in child)
                        {
                            if (!entry.highpolyParts.Contains(subChild.gameObject) && !entry.lowpolyParts.Contains(subChild.gameObject))
                            {
                                if (entry.lowpolyParts.Count > 0 && entry.highpolyParts.Count == 0)
                                    entry.highpolyParts.Add(subChild.gameObject);
                                else if (entry.highpolyParts.Count > 0 && entry.lowpolyParts.Count == 0)
                                    entry.lowpolyParts.Add(subChild.gameObject);
                                else if (entry.lowpolyParts.Count == 0)
                                    entry.lowpolyParts.Add(subChild.gameObject);
                                else
                                    entry.highpolyParts.Add(subChild.gameObject);
                            }
                        }
                    }
                    else
                    {
                        // És un model normal sense nom especial, l'assumim com a lowpoly per defecte
                        entry.lowpolyParts.Add(child.gameObject);
                    }
                }
            }

            models = new List<ModelEntry>(dict.Values);
        }
    }

    void Start()
    {
        materialViewer = gameObject.AddComponent<MaterialViewer>();
        
        SetHighpolyActive(false);

        StartCoroutine(DelayedInit());
    }

    private IEnumerator DelayedInit()
    {
        yield return new WaitForEndOfFrame();
        
        if (materialViewer != null)
        {
            materialViewer.Initialize();
        }

        UpdateActiveModels();
        AutoFitModel();
    }

    public void SetHighpolyActive(bool active)
    {
        isHighpolyActive = active;

        // Mantenim els contenidors actius i apaguem/encenem els fills per separat
        if (modelsContainer != null) modelsContainer.SetActive(true);

        UpdateActiveModels();
    }

    public void SetCurrentModel(int index)
    {
        currentModelIndex = index;
        UpdateActiveModels();
        AutoFitModel();
    }

    private void UpdateActiveModels()
    {
        // Apagar-ho tot
        foreach (var entry in models)
        {
            if (entry.rootFolder != null) entry.rootFolder.SetActive(false);
            foreach (var p in entry.lowpolyParts) if (p != null) p.SetActive(false);
            foreach (var p in entry.highpolyParts) if (p != null) p.SetActive(false);
        }

        // Encendre el model actual en la versió corresponent
        if (currentModelIndex >= 0 && currentModelIndex < models.Count)
        {
            ModelEntry current = models[currentModelIndex];
            if (current.rootFolder != null) current.rootFolder.SetActive(true);

            if (isHighpolyActive)
            {
                foreach (var p in current.highpolyParts) if (p != null) p.SetActive(true);
            }
            else
            {
                foreach (var p in current.lowpolyParts) if (p != null) p.SetActive(true);
            }
        }

        UpdatePolygonCounter();
    }
    
    // Per enllaçar amb el botó o Toggle
    public void ToggleHighpoly(bool state)
    {
        SetHighpolyActive(state);
    }
    
    public void ToggleLowpoly(bool state)
    {
        SetHighpolyActive(!state);
    }

    private void UpdatePolygonCounter()
    {
        if (polygonCounter != null)
        {
            GameObject target = GetActiveModel();
            if (target == null) target = gameObject;
            polygonCounter.SetModel(target);
        }
    }

    public GameObject GetActiveModel()
    {
        if (currentModelIndex >= 0 && currentModelIndex < models.Count)
        {
            ModelEntry current = models[currentModelIndex];
            if (current.rootFolder != null) return current.rootFolder;
            
            // Fallback: retornar una de les peces actives perquè els scripts puguin trobar la malla o materials
            if (isHighpolyActive && current.highpolyParts.Count > 0)
                return current.highpolyParts[0];
            else if (!isHighpolyActive && current.lowpolyParts.Count > 0)
                return current.lowpolyParts[0];
            else if (current.lowpolyParts.Count > 0)
                return current.lowpolyParts[0]; // Últim recurs
        }
        return null;
    }

    private void AutoFitModel()
    {
        // Només volem ajustar la càmera a l'objecte ACTIU actual, no a tots
        Renderer[] renderers = GetComponentsInChildren<Renderer>(false); // Només els actius
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
        {
            if (r.gameObject.activeInHierarchy && r.name != "WireframeOverlay" && !r.name.EndsWith("_UVLayout"))
                bounds.Encapsulate(r.bounds);
        }

        // Calculem l'offset necessari per moure només els contenidors globals perquè el centre ACTIU sigui 0,0,0
        // Wait, si movem els contenidors globals per cada model, es pot desquadrar tot.
        // Millor demanar a la càmera que orbiti al voltant del nou centre!
        Vector3 currentCenter = bounds.center;
        
        // Calculem quina hauria de ser la distància de la càmera segons la mida de l'objecte
        float maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        float requiredDistance = maxDimension * 1.5f; 
        
        if (requiredDistance < 1f) requiredDistance = 1f;

        // Actualitzem l'OrbitCamera per mirar al centre de l'objecte actual
        OrbitCamera cam = Camera.main != null ? Camera.main.GetComponent<OrbitCamera>() : null;
        if (cam != null)
        {
            // Update OrbitCamera to support targeting a specific world point instead of a transform, or just use the center
            // Since OrbitCamera targets modelLoaderObj.transform, we can just move modelLoaderObj so the center is 0,0,0
            Vector3 offset = -bounds.center + transform.position;
            if (modelsContainer != null) modelsContainer.transform.position += offset;

            cam.ResetView(requiredDistance);
        }
        
        Debug.Log($"Model auto-centrat. Mida màxima: {maxDimension}, Distància ajustada a: {requiredDistance}");
    }
}
