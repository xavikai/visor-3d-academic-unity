using UnityEngine;
using System.Collections;

public class ModelLoader : MonoBehaviour
{
    public PolygonCounter polygonCounter;
    
    public GameObject highpolyContainer;
    public GameObject lowpolyContainer;

    [HideInInspector]
    public MaterialViewer materialViewer;

    private bool isHighpolyActive = false;

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

        UpdatePolygonCounter();
        AutoFitModel();
    }

    public void SetHighpolyActive(bool active)
    {
        isHighpolyActive = active;

        if (highpolyContainer != null) highpolyContainer.SetActive(active);
        if (lowpolyContainer != null) lowpolyContainer.SetActive(!active);

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
            // Enviem al PolygonCounter el contenidor actiu
            GameObject target = isHighpolyActive ? highpolyContainer : lowpolyContainer;
            if (target == null) target = gameObject;
            polygonCounter.SetModel(target);
        }
    }

    private void AutoFitModel()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
        {
            bounds.Encapsulate(r.bounds);
        }

        // Movem els contenidors en direcció contrària al centre de la caixa englobant
        // per forçar que el centre de l'objecte global sigui (0,0,0)
        Vector3 offset = -bounds.center;
        
        if (highpolyContainer != null) highpolyContainer.transform.position += offset;
        if (lowpolyContainer != null) lowpolyContainer.transform.position += offset;

        // Calculem quina hauria de ser la distància de la càmera segons la mida de l'objecte
        float maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        float requiredDistance = maxDimension * 1.5f; 
        
        if (requiredDistance < 1f) requiredDistance = 1f;

        // Actualitzem l'OrbitCamera
        OrbitCamera cam = Camera.main != null ? Camera.main.GetComponent<OrbitCamera>() : null;
        if (cam != null)
        {
            cam.ResetView(requiredDistance);
        }
        
        Debug.Log($"Model auto-centrat. Mida màxima: {maxDimension}, Distància ajustada a: {requiredDistance}");
    }
}
