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

    public void Initialize()
    {
        originalData.Clear();
        allMaterials.Clear();

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
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
}
