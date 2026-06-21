using UnityEngine;
using System.Collections.Generic;

public class MaterialViewer : MonoBehaviour
{
    private class OriginalMaterialData
    {
        public Texture baseMap;
        public Texture bumpMap;
        public Texture metallicGlossMap;
    }

    private Dictionary<Material, OriginalMaterialData> originalData = new Dictionary<Material, OriginalMaterialData>();
    private List<Material> allMaterials = new List<Material>();

    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    private Material vertexColorMaterial;
    private bool isVertexColorMode = false;

    private List<GameObject> wireframeObjects = new List<GameObject>();
    private Material wireframeMaterial;

    private List<GameObject> uvLayoutObjects = new List<GameObject>();
    private Material checkerboardMaterial;
    private bool isUvMode = false;

    public void Initialize()
    {
        originalData.Clear();
        allMaterials.Clear();
        originalMaterials.Clear();
        isVertexColorMode = false;
        isUvMode = false;
        
        foreach(var w in wireframeObjects) if(w!=null) Destroy(w);
        wireframeObjects.Clear();

        foreach(var u in uvLayoutObjects) if(u!=null) Destroy(u);
        uvLayoutObjects.Clear();

        wireframeMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        if (wireframeMaterial != null) wireframeMaterial.color = Color.cyan;

        checkerboardMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        checkerboardMaterial.SetTexture("_BaseMap", CreateCheckerboardTexture());

        // Intentem utilitzar Particles/Unlit que suporta Vertex Color de forma nativa a l'URP
        Shader vcShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (vcShader == null) vcShader = Shader.Find("UI/Default"); // Fallback
        vertexColorMaterial = new Material(vcShader);
        if (vertexColorMaterial.HasProperty("_Surface")) vertexColorMaterial.SetFloat("_Surface", 0); // Opac
        if (vertexColorMaterial.HasProperty("_Blend")) vertexColorMaterial.SetFloat("_Blend", 0);

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            if (r.name == "WireframeOverlay" || r.name.EndsWith("_UVLayout")) continue;

            if (r is MeshRenderer)
            {
                GenerateUVLayout(r.GetComponent<MeshFilter>());
            }
            else if (r is SkinnedMeshRenderer)
            {
                GenerateUVLayout(r as SkinnedMeshRenderer);
            }

            originalMaterials[r] = r.materials;

            foreach (Material m in r.materials)
            {
                if (!originalData.ContainsKey(m))
                {
                    OriginalMaterialData data = new OriginalMaterialData();
                    if (m.HasProperty("_BaseMap")) data.baseMap = m.GetTexture("_BaseMap");
                    if (m.HasProperty("_BumpMap")) data.bumpMap = m.GetTexture("_BumpMap");
                    if (m.HasProperty("_MetallicGlossMap")) data.metallicGlossMap = m.GetTexture("_MetallicGlossMap");

                    originalData[m] = data;
                    allMaterials.Add(m);
                }
            }
        }

        // Generar Wireframes
        MeshFilter[] filters = GetComponentsInChildren<MeshFilter>(true);
        foreach (MeshFilter mf in filters)
        {
            if (mf.gameObject.name == "WireframeOverlay" || mf.sharedMesh == null) continue;

            Mesh original = mf.sharedMesh;
            Mesh wireMesh = new Mesh();
            wireMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            wireMesh.vertices = original.vertices;
            
            int[] triangles = original.triangles;
            int[] lines = new int[triangles.Length * 2];
            int lineIndex = 0;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                lines[lineIndex++] = triangles[i];
                lines[lineIndex++] = triangles[i + 1];
                lines[lineIndex++] = triangles[i + 1];
                lines[lineIndex++] = triangles[i + 2];
                lines[lineIndex++] = triangles[i + 2];
                lines[lineIndex++] = triangles[i];
            }
            wireMesh.SetIndices(lines, MeshTopology.Lines, 0);

            GameObject wireObj = new GameObject("WireframeOverlay");
            wireObj.transform.SetParent(mf.transform, false);
            wireObj.transform.localPosition = Vector3.zero;
            wireObj.transform.localRotation = Quaternion.identity;
            wireObj.transform.localScale = Vector3.one;

            MeshFilter wireMf = wireObj.AddComponent<MeshFilter>();
            wireMf.sharedMesh = wireMesh;
            MeshRenderer wireMr = wireObj.AddComponent<MeshRenderer>();
            wireMr.material = wireframeMaterial;

            // Ajust fi per evitar z-fighting: movem l'objecte de wireframe una miqueta
            // cap a la càmera. Al ser un visor bàsic potser no cal, però ajuda.
            
            wireObj.SetActive(false);
            wireframeObjects.Add(wireObj);
        }
    }

    public void ToggleAlbedo(bool state)
    {
        foreach (Material m in allMaterials)
        {
            if (m.HasProperty("_BaseMap"))
                m.SetTexture("_BaseMap", state ? originalData[m].baseMap : null);
        }
    }

    public void ToggleNormal(bool state)
    {
        foreach (Material m in allMaterials)
        {
            if (m.HasProperty("_BumpMap"))
                m.SetTexture("_BumpMap", state ? originalData[m].bumpMap : null);
        }
    }

    public void ToggleMetallic(bool state)
    {
        foreach (Material m in allMaterials)
        {
            if (m.HasProperty("_MetallicGlossMap"))
                m.SetTexture("_MetallicGlossMap", state ? originalData[m].metallicGlossMap : null);
        }
    }

    public void ToggleWireframe(bool state)
    {
        foreach (var w in wireframeObjects)
        {
            if (w != null) w.SetActive(state);
        }
    }

    public void ToggleVertexColor(bool state)
    {
        isVertexColorMode = state;
        UpdateMaterials();
    }

    public void ToggleUV(bool state)
    {
        isUvMode = state;
        UpdateMaterials();

        foreach (var u in uvLayoutObjects)
        {
            if (u != null) u.SetActive(state);
        }
    }

    private void UpdateMaterials()
    {
        foreach (var kvp in originalMaterials)
        {
            Renderer r = kvp.Key;
            if (r == null) continue;

            if (isUvMode)
            {
                Material[] vMats = new Material[kvp.Value.Length];
                for (int i = 0; i < vMats.Length; i++) vMats[i] = checkerboardMaterial;
                r.materials = vMats;
            }
            else if (isVertexColorMode)
            {
                Material[] vMats = new Material[kvp.Value.Length];
                for (int i = 0; i < vMats.Length; i++) vMats[i] = vertexColorMaterial;
                r.materials = vMats;
            }
            else
            {
                r.materials = kvp.Value;
            }
        }
    }

    private Texture2D CreateCheckerboardTexture()
    {
        int size = 512;
        int squares = 16;
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Point;
        Color[] pixels = new Color[size * size];
        int squareSize = size / squares;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isWhite = ((x / squareSize) % 2) == ((y / squareSize) % 2);
                pixels[y * size + x] = isWhite ? Color.white : Color.gray;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private void GenerateUVLayout(MeshFilter mf)
    {
        if (mf == null || mf.sharedMesh == null || !mf.sharedMesh.isReadable) return;
        CreateUVLayoutObject(mf.sharedMesh, mf.gameObject);
    }

    private void GenerateUVLayout(SkinnedMeshRenderer smr)
    {
        if (smr == null || smr.sharedMesh == null || !smr.sharedMesh.isReadable) return;
        CreateUVLayoutObject(smr.sharedMesh, smr.gameObject);
    }

    private void CreateUVLayoutObject(Mesh originalMesh, GameObject sourceObject)
    {
        Vector2[] uvs = originalMesh.uv;
        if (uvs == null || uvs.Length == 0) return;

        Mesh uvMesh = new Mesh();
        uvMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        Vector3[] newVertices = new Vector3[uvs.Length];
        for (int i = 0; i < uvs.Length; i++)
        {
            // UV [0,1] -> [-0.5, 0.5] escalat x2 -> mida física 1 metre a prop del model
            newVertices[i] = new Vector3((uvs[i].x - 0.5f) * 2f, (uvs[i].y - 0.5f) * 2f, 0); 
        }
        uvMesh.vertices = newVertices;

        int[] originalTriangles = originalMesh.triangles;
        int[] lineIndices = new int[originalTriangles.Length * 2];
        int lineIdx = 0;
        for (int i = 0; i < originalTriangles.Length; i += 3)
        {
            lineIndices[lineIdx++] = originalTriangles[i];
            lineIndices[lineIdx++] = originalTriangles[i + 1];
            lineIndices[lineIdx++] = originalTriangles[i + 1];
            lineIndices[lineIdx++] = originalTriangles[i + 2];
            lineIndices[lineIdx++] = originalTriangles[i + 2];
            lineIndices[lineIdx++] = originalTriangles[i];
        }
        uvMesh.SetIndices(lineIndices, MeshTopology.Lines, 0);

        GameObject layoutObj = new GameObject(sourceObject.name + "_UVLayout");
        layoutObj.transform.SetParent(this.transform);
        
        // Col·loquem el mapa UV a la dreta (+X)
        layoutObj.transform.localPosition = new Vector3(2f, 0, 0); 
        layoutObj.transform.localRotation = Quaternion.identity;

        MeshFilter newMf = layoutObj.AddComponent<MeshFilter>();
        newMf.sharedMesh = uvMesh;
        MeshRenderer newMr = layoutObj.AddComponent<MeshRenderer>();
        newMr.material = wireframeMaterial;

        layoutObj.SetActive(false);
        uvLayoutObjects.Add(layoutObj);
    }
}
