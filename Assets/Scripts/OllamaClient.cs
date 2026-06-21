using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Text;
using System.Collections;

public class OllamaClient : MonoBehaviour
{
    private string ollamaUrl = "http://localhost:11434/api/generate";
    private string modelName = "llama3"; // Podeu canviar el model (ex: mistral, gemma)

    public RubricConfig rubricConfig;
    public Text reportTextUI; 

    public void GenerateReport(int triangles, int vertices, int budget, float score)
    {
        StartCoroutine(SendOllamaRequest(triangles, vertices, budget, score));
    }

    private IEnumerator SendOllamaRequest(int triangles, int vertices, int budget, float score)
    {
        if (reportTextUI != null) reportTextUI.text = "Generant informe...";

        string prompt = $"{rubricConfig.promptQualitatiu}\n" +
                        $"Dades tècniques:\n" +
                        $"- Vèrtexs: {vertices}\n" +
                        $"- Triangles: {triangles}\n" +
                        $"- Pressupost establert: {budget}\n" +
                        $"- Nota automàtica assignada: {score} pts.\n\n" +
                        $"Escriu un informe raonat per a l'alumne.";

        prompt = prompt.Replace("\"", "\\\"");
        prompt = prompt.Replace("\n", "\\n");

        string jsonPayload = $"{{\"model\": \"{modelName}\", \"prompt\": \"{prompt}\", \"stream\": false}}";

        using (UnityWebRequest request = new UnityWebRequest(ollamaUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error Ollama: " + request.error);
                if (reportTextUI != null) reportTextUI.text = "Error connectant amb Ollama: " + request.error;
            }
            else
            {
                string responseText = request.downloadHandler.text;
                string result = ExtractResponseFromJson(responseText);
                if (reportTextUI != null) reportTextUI.text = result;
                else Debug.Log("Resposta Ollama: " + result);
            }
        }
    }

    private string ExtractResponseFromJson(string json)
    {
        try {
            OllamaResponse parsed = JsonUtility.FromJson<OllamaResponse>(json);
            return parsed.response;
        } catch {
            return "Error parsejant la resposta.";
        }
    }

    [System.Serializable]
    private class OllamaResponse
    {
        public string model;
        public string created_at;
        public string response;
        public bool done;
    }
}
