using UnityEngine;

public class PolygonCounter : MonoBehaviour
{
    public Evaluator evaluator;
    public int totalTriangles = 0;
    public int totalVertices = 0;
    
    private GameObject currentModel;

    public void SetModel(GameObject model)
    {
        currentModel = model;
    }

    public void AnalitzarMalla()
    {
        if (currentModel == null)
        {
            Debug.LogError("No hi ha cap model assignat per avaluar. L'alumne no ha afegit res.");
            return;
        }

        totalTriangles = 0;
        totalVertices = 0;

        MeshFilter[] filters = currentModel.GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter mf in filters)
        {
            if (mf.sharedMesh != null)
            {
                totalTriangles += mf.sharedMesh.triangles.Length / 3;
                totalVertices += mf.sharedMesh.vertexCount;
            }
        }

        Debug.Log($"Vèrtexs: {totalVertices}, Triangles: {totalTriangles}");

        if (evaluator != null)
        {
            evaluator.Avaluar(totalTriangles, totalVertices);
        }
    }
}
