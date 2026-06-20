using UnityEngine;
using TMPro;

public class RubricConfig : MonoBehaviour
{
    [Header("Configuració de la Rúbrica")]
    public int budgetEstablert = 10000;
    
    [TextArea(3, 5)]
    public string promptQualitatiu = "Ets un professor avaluant el model 3D d'un alumne. Analitza els polígons donats el pressupost.";

    public TMP_InputField budgetInput;
    public TMP_InputField promptInput;

    public void UpdateRubricFromUI()
    {
        if (budgetInput != null && int.TryParse(budgetInput.text, out int b))
        {
            budgetEstablert = b;
        }
        if (promptInput != null)
        {
            promptQualitatiu = promptInput.text;
        }
    }
}
