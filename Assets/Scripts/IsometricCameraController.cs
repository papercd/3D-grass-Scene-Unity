using UnityEngine;
using UnityEngine.InputSystem;

public class CameraControllerOrbit : MonoBehaviour
{
    [Header("Pivot Settings")]
    public Vector3 pivotPoint = Vector3.zero; // usually origin
    public float rotationSpeed = 100f;

    void Update()
    {
        float yawInput = 0f;

        // Mouse input (new Input System)
        if (Mouse.current.rightButton.isPressed)
            yawInput = Mouse.current.delta.x.ReadValue();

        // Optional keyboard input
        if (Keyboard.current.qKey.isPressed) yawInput = -1f;
        if (Keyboard.current.eKey.isPressed) yawInput = 1f;

        if (yawInput != 0f)
        {
            // Rotate CameraController around the pivot point (Y axis)
            transform.RotateAround(pivotPoint, Vector3.up, yawInput * rotationSpeed * Time.deltaTime);

            // Make the CameraController look at the pivot (origin)
            transform.LookAt(pivotPoint);
        }
    }
}
