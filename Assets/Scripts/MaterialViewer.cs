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
        public Texture occlusionMap;
        public float occlusionStrength = 1f;
        public Texture parallaxMap;
        public float parallaxScale = 0.02f;

        // Per-material UI States
        public bool albedoToggle = true;
        public bool normalToggle = true;
        public float normalSlider = 1f;
        public bool metallicToggle = true;
        public float metallicSlider = 0f;
        public float smoothnessSlider = 0.5f;
        public bool emissionToggle = true;
        public float emissionSlider = 1f;
        public bool occlusionToggle = true;
        public float occlusionSlider = 1f;
        public bool heightToggle = true;
        public float heightSlider = 0.02f;
    }

    private Dictionary<Material, OriginalMaterialData> originalData = new Dictionary<Material, OriginalMaterialData>();
    private List<Material> allMaterials = new List<Material>();
    private Dictionary<GameObject, Texture2D> modelUVs = new Dictionary<GameObject, Texture2D>();

    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    public Material activeMaterial { get; private set; }
    private Material vertexColorMaterial;
    private bool isVertexColorMode = false;

    private List<GameObject> wireframeObjects = new List<GameObject>();
    private Material wireframeMaterial;
    private Material checkerboardMaterial;
    private bool isUvMode = false;
    
    private bool initialized;
    private readonly List<Mesh> generatedMeshes = new List<Mesh>();
    private Texture2D checkerTexture;

    public void Initialize()
    {
        if (initialized) return;
        initialized = true;
        originalData.Clear();
        allMaterials.Clear();
        originalMaterials.Clear();
        isVertexColorMode = false;
        isUvMode = false;
        activeMaterial = null;
        
        foreach(var w in wireframeObjects) if(w!=null) Destroy(w);
        wireframeObjects.Clear();

        wireframeMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        if (wireframeMaterial != null) wireframeMaterial.color = Color.cyan;

        checkerboardMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        checkerTexture = CreateCheckerboardTexture();
        checkerboardMaterial.SetTexture("_BaseMap", checkerTexture);

        

        Shader vcShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (vcShader == null) vcShader = Shader.Find("UI/Default");
        vertexColorMaterial = new Material(vcShader);
        if (vertexColorMaterial.HasProperty("_Surface")) vertexColorMaterial.SetFloat("_Surface", 0);
        if (vertexColorMaterial.HasProperty("_Blend")) vertexColorMaterial.SetFloat("_Blend", 0);

        Dictionary<Material, Material> clonedMaterials = new Dictionary<Material, Material>();

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            if (r.name == "WireframeOverlay") continue;
            Material[] shared = r.sharedMaterials;
            Material[] instanced = new Material[shared.Length];

            for (int i = 0; i < shared.Length; i++)
            {
                Material original = shared[i];
                if (original == null) continue;

                if (!clonedMaterials.ContainsKey(original))
                {
                    Material clone = new Material(original);
                    clone.name = original.name; // Keep name clean
                    clonedMaterials[original] = clone;
                    allMaterials.Add(clone);

                    OriginalMaterialData data = new OriginalMaterialData();
                    // We don't store material in OriginalMaterialData as it's not defined

                    if (clone.HasProperty("_BaseMap")) data.baseMap = clone.GetTexture("_BaseMap");
                    else if (clone.HasProperty("_MainTex")) data.baseMap = clone.GetTexture("_MainTex");

                    if (clone.HasProperty("_BumpMap")) data.bumpMap = clone.GetTexture("_BumpMap");
                    if (clone.HasProperty("_MetallicGlossMap")) data.metallicGlossMap = clone.GetTexture("_MetallicGlossMap");
                    
                    if (clone.HasProperty("_Metallic")) data.metallic = clone.GetFloat("_Metallic");
                    if (clone.HasProperty("_Smoothness")) data.smoothness = clone.GetFloat("_Smoothness");
                    else if (clone.HasProperty("_Glossiness")) data.smoothness = clone.GetFloat("_Glossiness");

                    if (clone.HasProperty("_EmissionMap")) data.emissionMap = clone.GetTexture("_EmissionMap");
                    if (clone.HasProperty("_EmissionColor")) data.emissionColor = clone.GetColor("_EmissionColor");
                    if (clone.HasProperty("_OcclusionMap")) data.occlusionMap = clone.GetTexture("_OcclusionMap");
                    if (clone.HasProperty("_ParallaxMap")) data.parallaxMap = clone.GetTexture("_ParallaxMap");

                    if (clone.HasProperty("_BumpScale")) data.bumpScale = clone.GetFloat("_BumpScale");
                    if (clone.HasProperty("_OcclusionStrength")) data.occlusionStrength = clone.GetFloat("_OcclusionStrength");
                    if (clone.HasProperty("_Parallax")) data.parallaxScale = clone.GetFloat("_Parallax");
                    // UI States
                    data.metallicSlider = data.metallic;
                    data.smoothnessSlider = data.smoothness;
                    data.heightSlider = data.parallaxScale;

                    originalData[clone] = data;
                }

                instanced[i] = clonedMaterials[original];
            }

            r.sharedMaterials = instanced;
            originalMaterials[r] = instanced;
        }

        if (allMaterials.Count > 0) activeMaterial = allMaterials[0];

        // Generar Wireframes
        MeshFilter[] filters = GetComponentsInChildren<MeshFilter>(true);
        foreach (MeshFilter mf in filters)
        {
            if (mf.gameObject.name == "WireframeOverlay" || mf.gameObject.name.EndsWith("_UVLayout") || mf.sharedMesh == null) continue;

            Mesh original = mf.sharedMesh;
            if (!original.isReadable) continue;
            Mesh wireMesh = new Mesh();
            wireMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            wireMesh.vertices = original.vertices;
            
            int[] triangles = TriangleIndices(original);
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
            generatedMeshes.Add(wireMesh);

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
        if (activeMaterial == null || !originalData.ContainsKey(activeMaterial)) 
        {
            return;
        }

        originalData[activeMaterial].albedoToggle = state;
        
        if (activeMaterial.HasProperty("_BaseMap"))
        {
            activeMaterial.SetTexture("_BaseMap", state ? originalData[activeMaterial].baseMap : null);
        }
        else if (activeMaterial.HasProperty("_MainTex"))
        {
            activeMaterial.SetTexture("_MainTex", state ? originalData[activeMaterial].baseMap : null);
        }
        else
        {
        }
    }

    public void ToggleNormal(bool state)
    {
        if (activeMaterial == null || !originalData.ContainsKey(activeMaterial)) return;
        originalData[activeMaterial].normalToggle = state;

        if (activeMaterial.HasProperty("_BumpMap"))
            activeMaterial.SetTexture("_BumpMap", state ? originalData[activeMaterial].bumpMap : null);
        
        if (state && originalData[activeMaterial].bumpMap != null) activeMaterial.EnableKeyword("_NORMALMAP");
        else activeMaterial.DisableKeyword("_NORMALMAP");
    }

    public void ToggleMetallic(bool state)
    {
        if (activeMaterial == null || !originalData.ContainsKey(activeMaterial)) return;
        originalData[activeMaterial].metallicToggle = state;

        if (activeMaterial.HasProperty("_MetallicGlossMap"))
            activeMaterial.SetTexture("_MetallicGlossMap", state ? originalData[activeMaterial].metallicGlossMap : null);
        
        if (state && originalData[activeMaterial].metallicGlossMap != null) activeMaterial.EnableKeyword("_METALLICSPECGLOSSMAP");
        else activeMaterial.DisableKeyword("_METALLICSPECGLOSSMAP");
        
        if (activeMaterial.HasProperty("_Metallic"))
        {
            activeMaterial.SetFloat("_Metallic", state ? originalData[activeMaterial].metallicSlider : 0f);
        }
    }

    public void SetNormalIntensity(float value)
    {
        if (activeMaterial == null || !originalData.ContainsKey(activeMaterial)) return;
        originalData[activeMaterial].normalSlider = value;
        
        if (activeMaterial.HasProperty("_BumpScale"))
            activeMaterial.SetFloat("_BumpScale", originalData[activeMaterial].bumpScale * value);
    }

    public void SetMetallic(float value)
    {
        if (activeMaterial == null || !originalData.ContainsKey(activeMaterial)) return;
        originalData[activeMaterial].metallicSlider = value;
        
        if (activeMaterial.HasProperty("_Metallic"))
            activeMaterial.SetFloat("_Metallic", value);
    }

    public void SetSmoothness(float value)
    {
        if (activeMaterial == null || !originalData.ContainsKey(activeMaterial)) return;
        originalData[activeMaterial].smoothnessSlider = value;
        
        if (activeMaterial.HasProperty("_Smoothness"))
            activeMaterial.SetFloat("_Smoothness", value);
        else if (activeMaterial.HasProperty("_Glossiness"))
            activeMaterial.SetFloat("_Glossiness", value);
    }

    public void ToggleEmission(bool state)
    {
        if (activeMaterial == null || !originalData.ContainsKey(activeMaterial)) return;
        originalData[activeMaterial].emissionToggle = state;

        if (activeMaterial.HasProperty("_EmissionMap"))
            activeMaterial.SetTexture("_EmissionMap", state ? originalData[activeMaterial].emissionMap : null);
        
        if (state) activeMaterial.EnableKeyword("_EMISSION");
        else activeMaterial.DisableKeyword("_EMISSION");
    }

    public void SetEmissionIntensity(float value)
    {
        if (activeMaterial == null || !originalData.ContainsKey(activeMaterial)) return;
        originalData[activeMaterial].emissionSlider = value;

        if (activeMaterial.HasProperty("_EmissionColor"))
            activeMaterial.SetColor("_EmissionColor", originalData[activeMaterial].emissionColor * value);
    }

    public void ToggleOcclusion(bool state)
    {
        if (activeMaterial == null || !originalData.ContainsKey(activeMaterial)) return;
        originalData[activeMaterial].occlusionToggle = state;

        if (activeMaterial.HasProperty("_OcclusionMap"))
            activeMaterial.SetTexture("_OcclusionMap", state ? originalData[activeMaterial].occlusionMap : null);
    }

    public void SetOcclusionIntensity(float value)
    {
        if (activeMaterial == null || !originalData.ContainsKey(activeMaterial)) return;
        originalData[activeMaterial].occlusionSlider = value;

        if (activeMaterial.HasProperty("_OcclusionStrength"))
            activeMaterial.SetFloat("_OcclusionStrength", originalData[activeMaterial].occlusionStrength * value);
    }

    public void ToggleHeight(bool state)
    {
        if (activeMaterial == null || !originalData.ContainsKey(activeMaterial)) return;
        originalData[activeMaterial].heightToggle = state;

        if (activeMaterial.HasProperty("_ParallaxMap"))
            activeMaterial.SetTexture("_ParallaxMap", state ? originalData[activeMaterial].parallaxMap : null);
        
        if (state && originalData[activeMaterial].parallaxMap != null) activeMaterial.EnableKeyword("_PARALLAXMAP");
        else activeMaterial.DisableKeyword("_PARALLAXMAP");
    }

    public void SetHeightScale(float value)
    {
        if (activeMaterial == null || !originalData.ContainsKey(activeMaterial)) return;
        originalData[activeMaterial].heightSlider = value;

        if (activeMaterial.HasProperty("_Parallax"))
            activeMaterial.SetFloat("_Parallax", value);
    }

    // Per la galeria de textures 2D
    public OriginalMaterialData GetFirstMaterialData()
    {
        if (activeMaterial != null && originalData.ContainsKey(activeMaterial))
            return originalData[activeMaterial];
        if (allMaterials.Count > 0 && originalData.ContainsKey(allMaterials[0]))
            return originalData[allMaterials[0]];
        return null;
    }

    public void SetActiveMaterial(Material mat)
    {
        if (mat == null) { activeMaterial = null; return; }
        if (originalData.ContainsKey(mat))
        {
            activeMaterial = mat;
        }
    }

    public List<Material> GetMaterialsForModel(GameObject activeModel)
    {
        List<Material> list = new List<Material>();
        if (activeModel != null)
        {
            Renderer[] renderers = activeModel.GetComponentsInChildren<Renderer>(false);
            foreach (Renderer r in renderers)
            {
                if (!originalMaterials.TryGetValue(r, out var materials)) continue;
                foreach (Material m in materials)
                {
                    if (m != null && !list.Contains(m) && originalData.ContainsKey(m))
                    {
                        list.Add(m);
                    }
                }
            }
        }
        return list;
    }

    public OriginalMaterialData GetActiveMaterialData(GameObject activeModel)
    {
        var materials = GetMaterialsForModel(activeModel);
        if (activeMaterial != null && materials.Contains(activeMaterial)) return originalData[activeMaterial];
        activeMaterial = materials.Count > 0 ? materials[0] : null;
        return activeMaterial != null ? originalData[activeMaterial] : null;
    }

    private void OnDestroy()
    {
        foreach (var material in allMaterials) if (material != null) Destroy(material);
        foreach (var texture in modelUVs.Values) if (texture != null) Destroy(texture);
        foreach (var mesh in generatedMeshes) if (mesh != null) Destroy(mesh);
        if (wireframeMaterial != null) Destroy(wireframeMaterial);
        if (checkerboardMaterial != null) Destroy(checkerboardMaterial);
        if (vertexColorMaterial != null) Destroy(vertexColorMaterial);
        if (checkerTexture != null) Destroy(checkerTexture);
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
                Material[] uvMats = new Material[kvp.Value.Length];
                for (int i = 0; i < uvMats.Length; i++) uvMats[i] = checkerboardMaterial;
                r.sharedMaterials = uvMats;
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
            if (mf.name != "WireframeOverlay" && mf.sharedMesh != null) DrawMeshUVs(pixels, mf.sharedMesh, lineColor, size);
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

    private static int[] TriangleIndices(Mesh mesh)
    {
        var indices = new List<int>();
        for (int sub = 0; sub < mesh.subMeshCount; sub++)
            if (mesh.GetTopology(sub) == MeshTopology.Triangles) indices.AddRange(mesh.GetTriangles(sub));
        return indices.ToArray();
    }
    private void DrawMeshUVs(Color32[] pixels, Mesh mesh, Color32 col, int size)
    {
        if (!mesh.isReadable) return;
        Vector2[] uvs = mesh.uv;
        int[] tris = TriangleIndices(mesh);
        if (uvs == null || uvs.Length == 0 || tris == null || tris.Length == 0) return;

        for (int i = 0; i < tris.Length; i += 3)
        {
            if (tris[i] >= uvs.Length || tris[i + 1] >= uvs.Length || tris[i + 2] >= uvs.Length) continue;
            DrawLine(pixels, uvs[tris[i]], uvs[tris[i + 1]], col, size);
            DrawLine(pixels, uvs[tris[i + 1]], uvs[tris[i + 2]], col, size);
            DrawLine(pixels, uvs[tris[i + 2]], uvs[tris[i]], col, size);
        }
    }

    private void DrawLine(Color32[] pixels, Vector2 p1, Vector2 p2, Color32 col, int size)
    {
        if (float.IsNaN(p1.x) || float.IsNaN(p1.y) || float.IsNaN(p2.x) || float.IsNaN(p2.y)) return;
        p1 = Vector2.Min(Vector2.one, Vector2.Max(Vector2.zero, p1));
        p2 = Vector2.Min(Vector2.one, Vector2.Max(Vector2.zero, p2));
        int x0 = (int)(p1.x * (size - 1));
        int y0 = (int)(p1.y * (size - 1));
        int x1 = (int)(p2.x * (size - 1));
        int y1 = (int)(p2.y * (size - 1));

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
