using UnityEngine;

/// <summary>
/// Makes the Relic visual rotate and bob up/down for a magical floating effect
/// Attach this to the Relic's Visual child (the mesh), not the parent
/// </summary>
public class RelicFloatingAnimation : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField] private bool enableRotation = true;
    public float rotationSpeed = 30f; // Public so RelicController can modify
    [SerializeField] private Vector3 rotationAxis = Vector3.up; // Y-axis by default

    [Header("Bobbing")]
    [SerializeField] private bool enableBobbing = true;
    public float bobbingSpeed = 1f; // Public so RelicController can modify
    [SerializeField] private float bobbingHeight = 0.3f; // How high/low it moves
    [SerializeField] private Vector3 bobbingAxis = Vector3.up; // Y-axis by default

    [Header("Optional: Phase Offset")]
    [SerializeField] private float startPhaseOffset = 0f; // For randomizing start position

    private Vector3 startPosition;
    private float timeOffset;

    void Start()
    {
        // Store the starting local position
        startPosition = transform.localPosition;

        // Add random offset if desired (for multiple relics)
        timeOffset = startPhaseOffset + Random.Range(0f, 2f * Mathf.PI);

        Debug.Log($"✨ RelicFloatingAnimation started on {gameObject.name}");
    }

    void Update()
    {
        // Rotation around Y-axis
        if (enableRotation)
        {
            transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);
        }

        // Bobbing up and down
        if (enableBobbing)
        {
            float bobOffset = Mathf.Sin((Time.time + timeOffset) * bobbingSpeed * 2f * Mathf.PI) * bobbingHeight;
            transform.localPosition = startPosition + bobbingAxis.normalized * bobOffset;
        }
    }

    // Context menu helpers for testing
    [ContextMenu("Reset Position")]
    void ResetPosition()
    {
        transform.localPosition = startPosition;
        transform.localRotation = Quaternion.identity;
        Debug.Log("Reset floating animation to start position");
    }

    [ContextMenu("Rotate Fast (Test)")]
    void RotateFast()
    {
        rotationSpeed = 120f;
        Debug.Log("Rotation speed set to FAST");
    }

    [ContextMenu("Bob High (Test)")]
    void BobHigh()
    {
        bobbingHeight = 1f;
        Debug.Log("Bobbing height set to HIGH");
    }
}