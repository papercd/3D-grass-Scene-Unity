using UnityEngine;
using UnityEngine.InputSystem;

public class CameraControllerOrbit : MonoBehaviour
{
    [Header("Pivot Settings")]
    public Vector3 pivotPoint = Vector3.zero;
    public float rotationSpeed = 100f;
    public float snapRotationSpeed = 5f;  // Speed for 90-degree snaps

    [Header("Zoom Settings")]
    public float zoomedSize = 10f;
    public float defaultSize = 15f;
    public float zoomSpeed = 5f;

    [Header("Audio Settings")]
    public AudioClip zoomInSound;
    public AudioClip zoomOutSound;

    private Camera cam;
    private AudioSource audioSource;
    private bool isZoomed = false;
    private float targetSize;
    private bool wasKPressed = false;

    // Snap rotation variables
    private bool isSnapping = false;
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    void Start()
    {
        cam = GetComponentInChildren<Camera>();
        if (cam == null)
        {
            Debug.LogError("No Camera found in children!");
        }
        else
        {
            targetSize = defaultSize;
            cam.orthographicSize = defaultSize;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;

        targetPosition = transform.position;
        targetRotation = transform.rotation;
    }

    void Update()
    {
        float yawInput = 0f;

        // Detect Q key press (add -90 degrees to target)
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            AddRotation(-45f);
        }

        // Detect E key press (add +90 degrees to target)
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            AddRotation(45f);
        }

        // Mouse input for free rotation (only when not snapping)
        /*
        if (Mouse.current.rightButton.isPressed && !isSnapping)
        {
            yawInput = Mouse.current.delta.x.ReadValue();

            if (yawInput != 0f)
            {
                // Rotate around pivot
                transform.RotateAround(pivotPoint, Vector3.up, yawInput * rotationSpeed * Time.deltaTime);
                transform.LookAt(pivotPoint);

                // Update targets to current transform
                targetPosition = transform.position;
                targetRotation = transform.rotation;
            }
        }*/

        // Smooth snap rotation - interpolate BOTH position and rotation
        if (isSnapping)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * snapRotationSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * snapRotationSpeed);

            // Check if we're close enough to target
            float positionDistance = Vector3.Distance(transform.position, targetPosition);
            float rotationAngle = Quaternion.Angle(transform.rotation, targetRotation);

            if (positionDistance < 0.01f && rotationAngle < 0.1f)
            {
                transform.position = targetPosition;
                transform.rotation = targetRotation;
                isSnapping = false;
            }
        }

        // Toggle zoom with K key
        if (Keyboard.current.kKey.isPressed && !wasKPressed)
        {
            isZoomed = !isZoomed;
            targetSize = isZoomed ? zoomedSize : defaultSize;

            if (isZoomed && zoomInSound != null)
            {
                audioSource.PlayOneShot(zoomInSound);
            }
            else if (!isZoomed && zoomOutSound != null)
            {
                audioSource.PlayOneShot(zoomOutSound);
            }
        }
        wasKPressed = Keyboard.current.kKey.isPressed;

        // Smoothly interpolate camera size
        if (cam != null)
        {
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, Time.deltaTime * zoomSpeed);
        }
    }

    void AddRotation(float degrees)
    {
        // Calculate new target position by rotating from CURRENT target position
        Vector3 directionFromPivot = targetPosition - pivotPoint;
        Vector3 newDirection = Quaternion.Euler(0, degrees, 0) * directionFromPivot;
        targetPosition = pivotPoint + newDirection;

        // Calculate new target rotation by adding to CURRENT target rotation
        targetRotation = Quaternion.Euler(0, degrees, 0) * targetRotation;

        isSnapping = true;
    }
}