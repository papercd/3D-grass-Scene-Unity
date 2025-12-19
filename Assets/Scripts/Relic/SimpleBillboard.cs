using UnityEngine;

/// <summary>
/// Simple billboard - just drag the rotating transform (CameraController) into the Inspector
/// Much simpler than auto-detection!
/// </summary>
public class DirectBillboard : MonoBehaviour
{
    [Header("Assign This")]
    [Tooltip("Drag your CameraController (or whatever rotates) here")]
    public Transform targetTransform;

    [Header("Options")]
    [SerializeField] private bool autoFindCamera = true;
    [SerializeField] private bool showDebugLogs = false;

    void Start()
    {
        // Auto-find if not assigned
        if (targetTransform == null && autoFindCamera)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                // Try parent first, then camera itself
                targetTransform = cam.transform.parent != null ? cam.transform.parent : cam.transform;
                Debug.Log($"✅ DirectBillboard: Auto-found '{targetTransform.name}'");
            }
            else
            {
                Debug.LogError("❌ DirectBillboard: No camera found and no transform assigned!");
            }
        }

        if (targetTransform != null)
        {
            Debug.Log($"✅ DirectBillboard: Facing '{targetTransform.name}'");
        }
    }

    void LateUpdate()
    {
        if (targetTransform == null) return;

        // Simple - just copy the rotation
        transform.rotation = targetTransform.rotation;

        // Debug
        if (showDebugLogs && Time.frameCount % 60 == 0)
        {
            Debug.Log($"Billboard: {targetTransform.name} rotation Y = {targetTransform.eulerAngles.y:F1}°");
        }
    }
}