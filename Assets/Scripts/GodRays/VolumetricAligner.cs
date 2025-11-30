using UnityEngine;

[ExecuteInEditMode]
public class VolumetricAligner : MonoBehaviour
{
    [Header("References")]
    public Light sunLight;
    public Transform mainCamera;

    [Header("Alignment")]
    public float distanceFromCamera = 15f;
    public float rotationOffset = 90f;

    public Vector3 distanceOffsetFromInitialPos;

    void Start()
    {
        // Position in front of camera
        distanceOffsetFromInitialPos = transform.position;
        transform.position = mainCamera.position + (mainCamera.forward * distanceFromCamera) + distanceOffsetFromInitialPos;
    }

    void Update()
    {
        if (sunLight == null) sunLight = RenderSettings.sun;
        if (mainCamera == null) mainCamera = Camera.main?.transform;
        if (sunLight == null || mainCamera == null) return;



        // Align planes parallel to light direction
        Quaternion lookAtSun = Quaternion.LookRotation(-sunLight.transform.forward);
        transform.rotation = lookAtSun * Quaternion.Euler(rotationOffset, 0, 0);
    }
}
