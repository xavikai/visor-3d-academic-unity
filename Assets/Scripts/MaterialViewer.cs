using UnityEngine;
using System.Collections.Generic;

public class MaterialViewer : MonoBehaviour
{
    public class OriginalMaterialData
    {
        public Texture baseMap;
        public Texture bumpMap;
        public Texture metallicGlossMap;
        public Texture emissionMap;
        public float bumpScale = 1f;
        public float metallic = 0f;
        public float smoothness = 0.5f;
        public Color emissionColor = Color.black;
    }

    private Dictionary<Material, OriginalMaterialData> originalData = new Dictionary<Material, OriginalMaterialData>();
    private List<Material> allMaterials = new List<Material>();
    private Dictionary<GameObject, Texture2D> modelUVs = new Dictionary<GameObject, Texture2D>();

    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    private Material vertexColorMaterial;
    private bool isVertexColorMode = false;

    private List<GameObject> wireframeObjects = new List<GameObject>();
    private Material wireframeMaterial;
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
            originalMaterials[r] = r.materials;

            foreach (Material m in r.materials)
            {
                if (!originalData.ContainsKey(m))
                {
                    OriginalMaterialData data = new OriginalMaterialData();
                    if (m.HasProperty("_BaseMap")) data.baseMap = m.GetTexture("_BaseMap");
                    else if (m.HasProperty("_MainTex")) data.baseMap = m.GetTexture("_MainTex");

                    if (m.HasProperty("_BumpMap")) data.bumpMap = m.GetTexture("_BumpMap");
                    if (m.HasProperty("_MetallicGlossMap")) data.metallicGlossMap = m.GetTexture("_MetallicGlossMap");
                    
                    if (m.HasProperty("_BumpScale")) data.bumpScale = m.GetFloat("_BumpScale");
                    if (m.HasProperty("_Metallic")) data.metallic = m.GetFloat("_Metallic");
                    if (m.HasProperty("_Smoothness")) data.smoothness = m.GetFloat("_Smoothness");
                    
                    if (m.HasProperty("_EmissionMap")) data.emissionMap = m.GetTexture("_EmissionMap");
                    if (m.HasProperty("_EmissionColor")) data.emissionColor = m.GetColor("_EmissionColor");

                    originalData[m] = data;
                    allMaterials.Add(m);
                }
            }
        }

        // Generar Wireframes
        MeshFilter[] filters = GetComponentsInChildren<MeshFilter>(true);
        foreach (MeshFilter mf in filters)
        {
            if (mf.gameObject.name == "WireframeOverlay" || mf.gameObject.name.EndsWith("_UVLayout") || mf.sharedMesh == null) continue;

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

    public void SetNormalIntensity(float value)
    {
        foreach (Material m in allMaterials)
        {
            if (m.HasProperty("_BumpScale"))
                m.SetFloat("_BumpScale", originalData[m].bumpScale * value);
        }
    }

    public void SetMetallic(float value)
    {
        foreach (Material m in allMaterials)
        {
            if (m.HasProperty("_Metallic"))
                m.SetFloat("_Metallic", value);
        }
    }

    public void SetSmoothness(float value)
    {
        foreach (Material m in allMaterials)
        {
            if (m.HasProperty("_Smoothness"))
                m.SetFloat("_Smoothness", value);
        }
    }

    public void ToggleEmission(bool state)
    {
        foreach (Material m in allMaterials)
        {
            if (m.HasProperty("_EmissionMap"))
                m.SetTexture("_EmissionMap", state ? originalData[m].emissionMap : null);
            
            if (state) m.EnableKeyword("_EMISSION");
            else m.DisableKeyword("_EMISSION");
        }
    }

    public void SetEmissionIntensity(float value)
    {
        foreach (Material m in allMaterials)
        {
            if (m.HasProperty("_EmissionColor"))
                m.SetColor("_EmissionColor", originalData[m].emissionColor * value);
        }
    }

    // Per la galeria de textures 2D
    public OriginalMaterialData GetFirstMaterialData()
    {
        if (allMaterials.Count > 0 && originalData.ContainsKey(allMaterials[0]))
            return originalData[allMaterials[0]];
        return null;
    }

    public OriginalMaterialData GetActiveMaterialData(GameObject activeModel)
    {
        if (activeModel != null)
        {
            Renderer[] renderers = activeModel.GetComponentsInChildren<Renderer>(false);
            if (renderers.Length > 0)
            {
                foreach (Material m in renderers[0].sharedMaterials)
                {
                    if (m != null && originalData.ContainsKey(m))
                        return originalData[m];
                }
            }
        }
        return GetFirstMaterialData();
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

    public Texture2D GetActiveModelUVs(GameObject activeModel)
    {
        if (activeModel == null) return null;
        if (modelUVs.ContainsKey(activeModel)) return modelUVs[activeModel];

        int size = 512;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        
        Color32[] pixels = new Color32[size * size];
        Color32 bgColor = new Color32(25, 25, 25, 255);
        for(int i=0; i<pixels.Length; i++) pixels[i] = bgColor;
        
        Color32 lineColor = new Color32(204, 204, 204, 255);

        MeshFilter[] filters = activeModel.GetComponentsInChildren<MeshFilter>(false);
        foreach(var mf in filters)
        {
            if (mf.sharedMesh != null) DrawMeshUVs(pixels, mf.sharedMesh, lineColor, size);
        }

        SkinnedMeshRenderer[] smrs = activeModel.GetComponentsInChildren<SkinnedMeshRenderer>(false);
        foreach(var smr in smrs)
        {
            if (smr.sharedMesh != null) DrawMeshUVs(pixels, smr.sharedMesh, lineColor, size);
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        modelUVs[activeModel] = tex;
        return tex;
    }

    private void DrawMeshUVs(Color32[] pixels, Mesh mesh, Color32 col, int size)
    {
        Vector2[] uvs = mesh.uv;
        int[] tris = mesh.triangles;
        if (uvs == null || uvs.Length == 0 || tris == null || tris.Length == 0) return;

        for (int i = 0; i < tris.Length; i += 3)
        {
            DrawLine(pixels, uvs[tris[i]], uvs[tris[i + 1]], col, size);
            DrawLine(pixels, uvs[tris[i + 1]], uvs[tris[i + 2]], col, size);
            DrawLine(pixels, uvs[tris[i + 2]], uvs[tris[i]], col, size);
        }
    }

    private void DrawLine(Color32[] pixels, Vector2 p1, Vector2 p2, Color32 col, int size)
    {
        int x0 = (int)(p1.x * size);
        int y0 = (int)(p1.y * size);
        int x1 = (int)(p2.x * size);
        int y1 = (int)(p2.y * size);

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            if (x0 >= 0 && x0 < size && y0 >= 0 && y0 < size)
                pixels[y0 * size + x0] = col;

            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }
}
