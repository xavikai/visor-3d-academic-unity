using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class AuthManager : MonoBehaviour
{
    public static bool IsTeacherMode = false;

    public GameObject loginPanel; // Abans anomenat studentPanel
    public GameObject teacherPanel;
    public TMP_InputField passwordInput;
    
    private string correctPassword = "admin";

    void Start()
    {
        // En mode Standalone l'alumne no veu cap UI (només el seu model 3D)
        IsTeacherMode = false;
        if (loginPanel != null) loginPanel.SetActive(false);
        if (teacherPanel != null) teacherPanel.SetActive(false);
    }

    void Update()
    {
        // Drecera oculta per obrir el Login del professor usant el Nou Input System
        if (Keyboard.current != null)
        {
            if (Keyboard.current.ctrlKey.isPressed && 
                Keyboard.current.shiftKey.isPressed && 
                Keyboard.current.tKey.wasPressedThisFrame)
            {
                if (!IsTeacherMode)
                {
                    bool isLoginActive = loginPanel != null && loginPanel.activeSelf;
                    if (loginPanel != null) loginPanel.SetActive(!isLoginActive);
                }
                else
                {
                    // Si ja som professor, amaguem/mostrem el panell per poder veure el model
                    bool isTeacherActive = teacherPanel != null && teacherPanel.activeSelf;
                    if (teacherPanel != null) teacherPanel.SetActive(!isTeacherActive);
                }
            }
        }
    }

    public void TryLoginTeacher()
    {
        if (passwordInput != null && passwordInput.text == correctPassword)
        {
            IsTeacherMode = true;
            if (loginPanel != null) loginPanel.SetActive(false);
            if (teacherPanel != null) teacherPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Contrasenya incorrecta.");
        }
    }
}
