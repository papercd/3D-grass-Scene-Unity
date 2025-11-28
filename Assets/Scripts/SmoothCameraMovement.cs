using UnityEngine;

public class SmoothThirdPersonCamera : MonoBehaviour
{
    public Transform target; // Your character's head position
    public Vector3 offset = new Vector3(0, 0.2f, -0.5f);
    
    public float mouseSensitivity = 2f;
    public float smoothSpeed = 10f;
    public float rotationSmoothSpeed = 5f;
    
    private float yaw = 0f;
    private float pitch = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Get mouse input for camera rotation
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -30f, 60f); // Limit vertical rotation

        // Calculate rotation
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        
        // Calculate position behind head
        Vector3 desiredPosition = target.position - (rotation * Vector3.forward * Mathf.Abs(offset.z)) 
                                                    + (Vector3.up * offset.y);
        
        // Smooth movement
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotationSmoothSpeed * Time.deltaTime);
    }
}