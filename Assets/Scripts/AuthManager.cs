using UnityEngine;
using TMPro;

public class AuthManager : MonoBehaviour
{
    public static bool IsTeacherMode = false;

    public GameObject teacherPanel;
    public GameObject studentPanel;
    public TMP_InputField passwordInput;
    
    private string correctPassword = "admin";

    void Start()
    {
        SetStudentMode();
    }

    public void SetStudentMode()
    {
        IsTeacherMode = false;
        if (teacherPanel != null) teacherPanel.SetActive(false);
        if (studentPanel != null) studentPanel.SetActive(true);
    }

    public void TryLoginTeacher()
    {
        if (passwordInput != null && passwordInput.text == correctPassword)
        {
            IsTeacherMode = true;
            if (teacherPanel != null) teacherPanel.SetActive(true);
            if (studentPanel != null) studentPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Contrasenya incorrecta.");
        }
    }
}
