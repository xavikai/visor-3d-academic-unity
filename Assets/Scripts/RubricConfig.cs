using UnityEngine;
using UnityEngine.UI;

public class RubricConfig : MonoBehaviour
{
    [Header("Configuració de Correcció")]
    [Tooltip("El pressupost poligonal que l'alumne no hauria de superar.")]
    public int maxPolygonBudget = 5000;
    
    [TextArea(3, 5)]
    public string promptQualitatiu = "Ets un professor avaluant el model 3D d'un alumne. Analitza els polígons donats el pressupost.";

    public InputField budgetInput;
    public InputField promptInput;

    public int GetCurrentBudget()
    {
        if (budgetInput != null && int.TryParse(budgetInput.text, out int parsed))
        {
            maxPolygonBudget = parsed;
        }
        return maxPolygonBudget;
    }

    public void UpdateRubricFromUI()
    {
        if (budgetInput != null && int.TryParse(budgetInput.text, out int b))
        {
            maxPolygonBudget = b;
        }
        if (promptInput != null)
        {
            promptQualitatiu = promptInput.text;
        }
    }
}
