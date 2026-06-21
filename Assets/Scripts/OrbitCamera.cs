using UnityEngine;
using UnityEngine.InputSystem;

public class OrbitCamera : MonoBehaviour
{
    public Transform target;
    public float distance = 5.0f;
    public float rotationSensitivity = 0.3f;
    public float panSensitivity = 0.01f;
    public float zoomSensitivity = 1.0f;
    
    // Suavitzat (Damping)
    public float damping = 10f;

    private float currentX = 0.0f;
    private float currentY = 0.0f;
    private float targetX = 0.0f;
    private float targetY = 0.0f;
    
    private float currentDistance;
    private float targetDistance;
    private float defaultDistance = 5.0f;
    
    private Vector3 currentPan;
    private Vector3 targetPan;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        targetX = currentX = angles.y;
        targetY = currentY = angles.x;
        targetDistance = currentDistance = distance;

        if (target == null)
        {
            GameObject loader = GameObject.Find("ModelLoader");
            if (loader != null) target = loader.transform;
        }

        if (target != null)
        {
            currentPan = targetPan = target.position;
        }
    }

    void LateUpdate()
    {
        if (target != null && Mouse.current != null)
        {
            // Tecla F per centrar (Focus)
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                targetPan = Vector3.zero;
                targetDistance = defaultDistance;
            }

            Vector2 delta = Mouse.current.delta.ReadValue();

            // Rotació (Clic esquerre)
            if (Mouse.current.leftButton.isPressed)
            {
                targetX += delta.x * rotationSensitivity;
                targetY -= delta.y * rotationSensitivity;
                targetY = Mathf.Clamp(targetY, -85f, 85f);
            }
            
            // Pan (Clic dret o central)
            if (Mouse.current.rightButton.isPressed || Mouse.current.middleButton.isPressed)
            {
                Vector3 right = transform.right;
                Vector3 up = transform.up;
                
                // Pan 1:1 matemàticament perfecte
                float fov = Camera.main != null ? Camera.main.fieldOfView : 60f;
                float heightAtDistance = 2.0f * currentDistance * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
                float panSens = heightAtDistance / Screen.height;
                
                targetPan -= (right * delta.x + up * delta.y) * panSens;
            }

            // Zoom (Rodeta)
            Vector2 scroll = Mouse.current.scroll.ReadValue();
            if (scroll.y != 0)
            {
                // El zoom s'escala logarítmicament segons la distància perquè sigui precís en objectes minúsculs i ràpid en gegants
                float adjustedZoomSens = zoomSensitivity * currentDistance * 0.005f;
                targetDistance -= scroll.y * adjustedZoomSens;
                targetDistance = Mathf.Clamp(targetDistance, 0.01f, 5000f);
            }

            // Aplicar inèrcia (Damping)
            currentX = Mathf.Lerp(currentX, targetX, Time.deltaTime * damping);
            currentY = Mathf.Lerp(currentY, targetY, Time.deltaTime * damping);
            currentDistance = Mathf.Lerp(currentDistance, targetDistance, Time.deltaTime * damping);
            currentPan = Vector3.Lerp(currentPan, targetPan, Time.deltaTime * damping);

            // Calcular i aplicar transformació
            Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
            Vector3 negDistance = new Vector3(0.0f, 0.0f, -currentDistance);
            Vector3 position = rotation * negDistance + currentPan;

            transform.rotation = rotation;
            transform.position = position;
        }
    }

    public void ResetView(float newDistance)
    {
        targetDistance = Mathf.Clamp(newDistance, 0.01f, 5000f);
        currentDistance = targetDistance; // Aplicar a l'instant
        targetPan = Vector3.zero;
        currentPan = Vector3.zero; // Aplicar a l'instant
        defaultDistance = targetDistance;
    }
}
