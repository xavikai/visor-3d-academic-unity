using UnityEngine;

public class Evaluator : MonoBehaviour
{
    public RubricConfig rubricConfig;
    public OllamaClient ollamaClient;
    public float currentScore = 0f;

    public void Avaluar(int triangles, int vertices)
    {
        if (rubricConfig == null) return;

        int budget = rubricConfig.budgetEstablert;
        
        if (triangles <= budget)
        {
            currentScore = 3.0f;
        }
        else if (triangles > budget && triangles <= (budget * 1.1f))
        {
            currentScore = 2.0f;
        }
        else
        {
            currentScore = 0.0f;
        }

        Debug.Log($"Nota assignada: {currentScore} pts (Budget: {budget})");

        if (ollamaClient != null && AuthManager.IsTeacherMode)
        {
            ollamaClient.GenerateReport(triangles, vertices, budget, currentScore);
        }
    }
}
