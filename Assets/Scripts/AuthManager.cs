using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class AuthManager : MonoBehaviour
{
    public static bool IsTeacherMode = false;

    public GameObject studentPanel;
    public GameObject loginPanel;
    public GameObject teacherPanel;
    public InputField passwordInput;
    
    private string correctPassword = "admin";

    void Start()
    {
        IsTeacherMode = false;
        if (loginPanel != null) loginPanel.SetActive(false);
        if (teacherPanel != null) teacherPanel.SetActive(false);
        if (studentPanel != null) studentPanel.SetActive(true);
    }

    void Update()
    {
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
            if (studentPanel != null) studentPanel.SetActive(false); // Amaguem els controls de l'alumne
            
            // Buidem el camp per si es torna a mostrar
            passwordInput.text = "";
        }
        else
        {
            Debug.LogWarning("Contrasenya incorrecta");
        }
    }
}
