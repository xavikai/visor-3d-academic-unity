using UnityEngine;

public class PolygonCounter : MonoBehaviour
{
    public Evaluator evaluator;
    public int totalTriangles = 0;
    public int totalVertices = 0;

    public void AnalitzarMalla(GameObject modelImportat)
    {
        totalTriangles = 0;
        totalVertices = 0;

        MeshFilter[] filters = modelImportat.GetComponentsInChildren<MeshFilter>();
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
