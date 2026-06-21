using UnityEngine;
using UnityEngine.InputSystem;

public class OrbitCamera : MonoBehaviour
{
    public Transform target;
    public float distance = 5.0f;
    public float sensitivity = 0.3f;
    public float zoomSensitivity = 1.0f;

    private float x = 0.0f;
    private float y = 0.0f;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;

        if (target == null)
        {
            GameObject loader = GameObject.Find("ModelLoader");
            if (loader != null) target = loader.transform;
        }
    }

    void LateUpdate()
    {
        if (target != null && Mouse.current != null)
        {
            // Rotació amb el clic esquerre o dret
            if (Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                x += delta.x * sensitivity;
                y -= delta.y * sensitivity;
                y = Mathf.Clamp(y, -80f, 80f);
            }

            // Zoom amb la rodeta del ratolí
            Vector2 scroll = Mouse.current.scroll.ReadValue();
            if (scroll.y != 0)
            {
                distance -= scroll.y * zoomSensitivity * 0.01f;
                distance = Mathf.Clamp(distance, 0.5f, 50f);
            }

            // Aplicar transformació
            Quaternion rotation = Quaternion.Euler(y, x, 0);
            Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
            Vector3 position = rotation * negDistance + target.position;

            transform.rotation = rotation;
            transform.position = position;
        }
    }
}
