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
}
