using UnityEngine;
using UnityEngine.InputSystem;

public class CameraControllerOrbit : MonoBehaviour
{
    [Header("Pivot Settings")]
    public Vector3 pivotPoint = Vector3.zero; // usually origin
    public float rotationSpeed = 100f;

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

        // Get or add AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
    }

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

        // Toggle zoom with K key
        if (Keyboard.current.kKey.isPressed && !wasKPressed)
        {
            isZoomed = !isZoomed;
            targetSize = isZoomed ? zoomedSize : defaultSize;

            // Play appropriate sound
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
}