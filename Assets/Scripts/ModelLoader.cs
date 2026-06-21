using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ModelLoader : MonoBehaviour
{
    public PolygonCounter polygonCounter;
    
    public GameObject highpolyContainer;
    public GameObject lowpolyContainer;

    [HideInInspector]
    public List<GameObject> highpolyModels = new List<GameObject>();
    [HideInInspector]
    public List<GameObject> lowpolyModels = new List<GameObject>();

    [HideInInspector]
    public MaterialViewer materialViewer;

    private bool isHighpolyActive = false;
    private int currentModelIndex = 0;

    void Start()
    {
        if (highpolyContainer == null)
        {
            Transform t = transform.Find("HighpolyContainer");
            if (t != null) highpolyContainer = t.gameObject;
        }
        if (lowpolyContainer == null)
        {
            Transform t = transform.Find("LowpolyContainer");
            if (t != null) lowpolyContainer = t.gameObject;
        }

        if (highpolyContainer != null)
        {
            foreach (Transform child in highpolyContainer.transform) highpolyModels.Add(child.gameObject);
        }
        if (lowpolyContainer != null)
        {
            foreach (Transform child in lowpolyContainer.transform) lowpolyModels.Add(child.gameObject);
        }

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
    }

    public void SetHighpolyActive(bool active)
    {
        isHighpolyActive = active;

        // Mantenim els contenidors actius i apaguem/encenem els fills per separat
        if (highpolyContainer != null) highpolyContainer.SetActive(true);
        if (lowpolyContainer != null) lowpolyContainer.SetActive(true);

        UpdateActiveModels();
    }

    public void SetCurrentModel(int index)
    {
        currentModelIndex = index;
        UpdateActiveModels();
    }

    private void UpdateActiveModels()
    {
        // Apagar-ho tot
        foreach (var m in highpolyModels) if (m != null) m.SetActive(false);
        foreach (var m in lowpolyModels) if (m != null) m.SetActive(false);

        // Encendre el model actual en la versió corresponent
        if (isHighpolyActive)
        {
            if (currentModelIndex >= 0 && currentModelIndex < highpolyModels.Count)
                highpolyModels[currentModelIndex].SetActive(true);
        }
        else
        {
            if (currentModelIndex >= 0 && currentModelIndex < lowpolyModels.Count)
                lowpolyModels[currentModelIndex].SetActive(true);
        }

        UpdatePolygonCounter();
        AutoFitModel();
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
            GameObject target = null;
            if (isHighpolyActive && currentModelIndex >= 0 && currentModelIndex < highpolyModels.Count)
                target = highpolyModels[currentModelIndex];
            else if (!isHighpolyActive && currentModelIndex >= 0 && currentModelIndex < lowpolyModels.Count)
                target = lowpolyModels[currentModelIndex];
                
            if (target == null) target = gameObject;
            polygonCounter.SetModel(target);
        }
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
            if (highpolyContainer != null) highpolyContainer.transform.position += offset;
            if (lowpolyContainer != null) lowpolyContainer.transform.position += offset;

            cam.ResetView(requiredDistance);
        }
        
        Debug.Log($"Model auto-centrat. Mida màxima: {maxDimension}, Distància ajustada a: {requiredDistance}");
    }
}
