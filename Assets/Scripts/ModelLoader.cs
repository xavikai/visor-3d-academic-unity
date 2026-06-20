using UnityEngine;
using System.Runtime.InteropServices;
using GLTFast;
using System.Threading.Tasks;
using System;

public class ModelLoader : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern void InitDragAndDrop();

    public PolygonCounter polygonCounter;
    private GameObject currentModel;

    void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        InitDragAndDrop();
#endif
    }

    public void CarregarModelFromWebBase64(string dataUrl)
    {
        int commaIndex = dataUrl.IndexOf(',');
        if (commaIndex >= 0)
        {
            string base64 = dataUrl.Substring(commaIndex + 1);
            byte[] bytes = Convert.FromBase64String(base64);
            LoadModelFromBytes(bytes);
        }
    }

    private async void LoadModelFromBytes(byte[] data)
    {
        if (currentModel != null)
        {
            Destroy(currentModel);
        }

        currentModel = new GameObject("LoadedModel");
        currentModel.transform.SetParent(this.transform, false);

        var gltf = new GltfImport();
        bool success = await gltf.LoadGltfBinary(data, new Uri(""));
        
        if (success)
        {
            success = await gltf.InstantiateMainSceneAsync(currentModel.transform);
            if (success && polygonCounter != null)
            {
                polygonCounter.AnalitzarMalla(currentModel);
            }
        }
        else
        {
            Debug.LogError("Error loading GLB.");
        }
    }
}
