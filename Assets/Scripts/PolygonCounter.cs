using UnityEngine;

public class PolygonCounter : MonoBehaviour
{
    public int totalTriangles = 0;
    public int totalVertices = 0;
    
    private GameObject currentModel;

    public void SetModel(GameObject model)
    {
        currentModel = model;
        GetStats(model, out totalTriangles, out totalVertices);
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
                tris += CountTriangles(mf.sharedMesh);
                verts += mf.sharedMesh.vertexCount;
            }
        }

        SkinnedMeshRenderer[] skinFilters = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (SkinnedMeshRenderer smr in skinFilters)
        {
            if (smr.sharedMesh != null)
            {
                tris += CountTriangles(smr.sharedMesh);
                verts += smr.sharedMesh.vertexCount;
            }
        }
    }

    private static int CountTriangles(Mesh mesh)
    {
        int count = 0;
        for (int sub = 0; sub < mesh.subMeshCount; sub++)
            if (mesh.GetTopology(sub) == MeshTopology.Triangles)
                count += (int)(mesh.GetIndexCount(sub) / 3);
        return count;
    }
}
