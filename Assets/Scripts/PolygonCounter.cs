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

        GetStats(currentModel, out totalTriangles, out totalVertices);

        Debug.Log($"Vèrtexs: {totalVertices}, Triangles: {totalTriangles}");

        if (evaluator != null)
        {
            evaluator.Avaluar(totalTriangles, totalVertices);
        }
    }

    public void GetStats(GameObject model, out int tris, out int verts)
    {
        tris = 0; verts = 0;
        if (model == null) return;

        MeshFilter[] filters = model.GetComponentsInChildren<MeshFilter>(true);
        foreach (MeshFilter mf in filters)
        {
            if (mf.gameObject.name == "WireframeOverlay") continue;

            if (mf.sharedMesh != null)
            {
                // Utilitzem GetIndexCount en comptes de triangles.Length per evitar errors si Read/Write està desactivat accidentalment
                tris += (int)(mf.sharedMesh.GetIndexCount(0) / 3);
                verts += mf.sharedMesh.vertexCount;
            }
        }
    }
}
