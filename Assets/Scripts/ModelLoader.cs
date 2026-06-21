using UnityEngine;
using System.Collections;

public class ModelLoader : MonoBehaviour
{
    public PolygonCounter polygonCounter;
    private GameObject loadedModel;

    void Start()
    {
        // L'alumne col·loca el seu model 3D com a fill del ModelLoader a l'editor.
        // Al iniciar l'executable, detectem el model i l'assignem per quan el professor vulgui avaluar.
        StartCoroutine(FindAndAssignModel());
    }

    private IEnumerator FindAndAssignModel()
    {
        // Esperem un frame perquè l'escena acabi de carregar
        yield return new WaitForEndOfFrame();

        if (transform.childCount > 0)
        {
            loadedModel = transform.GetChild(0).gameObject;
            Debug.Log("Model detectat correctament: " + loadedModel.name);
            
            // Passem la referència al comptador de polígons però NO l'avaluem encara.
            // L'avaluació es farà quan el professor premi el botó des del Panell Ocult.
            if (polygonCounter != null)
            {
                polygonCounter.SetModel(loadedModel);
            }
        }
        else
        {
            Debug.LogWarning("No s'ha trobat cap model. Has de posar el teu .glb com a fill de l'objecte ModelLoader.");
        }
    }
}
