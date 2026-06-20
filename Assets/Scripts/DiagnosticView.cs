using UnityEngine;

public class DiagnosticView : MonoBehaviour
{
    public void SetLitMode(GameObject model)
    {
        ChangeShader(model, "Universal Render Pipeline/Lit");
    }

    public void SetUnlitMode(GameObject model)
    {
        ChangeShader(model, "Universal Render Pipeline/Unlit");
    }

    private void ChangeShader(GameObject obj, string shaderName)
    {
        if (obj == null) return;
        
        Shader newShader = Shader.Find(shaderName);
        if (newShader == null) return;

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            foreach (Material m in r.materials)
            {
                m.shader = newShader;
            }
        }
    }
}
