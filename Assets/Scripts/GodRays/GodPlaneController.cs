using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GodRaysPlaneController : MonoBehaviour
{
    [Header("References")]
    public Light mainLight;
    public Material godRayMaterial;

    [Header("Base Line Settings")]
    [Tooltip("Center point of the base line on the origin plane")]
    public Vector3 baseLineCenter = new Vector3(0f, 0f, 50f);

    [Tooltip("Half-width of the base line (distance from center to each endpoint)")]
    [Range(10f, 200f)]
    public float lineHalfWidth = 50f;

    [Header("Extension Settings")]
    [Range(50f, 500f)]
    public float extensionLength = 200f;

    [Header("Gizmos")]
    public bool showGizmos = true;

    private Camera mainCamera;
    private Vector3 point1, point2, point3, point4;

    void Start()
    {
        mainCamera = Camera.main;

        if (mainLight == null)
        {
            mainLight = RenderSettings.sun;
        }
    }

    void Update()
    {
        if (mainCamera == null || mainLight == null || godRayMaterial == null)
            return;

        CalculateAndUpdatePlane();
    }

    void CalculateAndUpdatePlane()
    {
        // Get camera forward projected onto XZ plane
        Vector3 camForwardFlat = new Vector3(mainCamera.transform.forward.x, 0, mainCamera.transform.forward.z).normalized;

        // Perpendicular vector (rotate 90° around Y axis)
        Vector3 perpendicular = new Vector3(-camForwardFlat.z, 0, camForwardFlat.x);

        // Create base line perpendicular to camera view, centered at baseLineCenter
        point1 = baseLineCenter + perpendicular * lineHalfWidth;
        point2 = baseLineCenter - perpendicular * lineHalfWidth;

        // Ensure they're on the origin plane
        point1.y = 0;
        point2.y = 0;

        // Get light direction and extrude toward light
        Vector3 lightDir = -mainLight.transform.forward;
        point3 = point1 + lightDir * extensionLength;
        point4 = point2 + lightDir * extensionLength;

        // Calculate plane origin and normal
        Vector3 planeOrigin = (point1 + point2 + point3 + point4) / 4f;
        Vector3 planeNormal = mainLight.transform.forward;
        //Vector3 planeNormal = Vector3.Cross(point2 - point1, point3 - point1).normalized;

        // Pass to shader
        godRayMaterial.SetVector("_PlaneOrigin", planeOrigin);
        godRayMaterial.SetVector("_PlaneNormal", planeNormal);
    }

    void OnDrawGizmos()
    {
        if (!showGizmos)
            return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainLight == null)
            mainLight = RenderSettings.sun;

        if (mainCamera == null || mainLight == null)
            return;

        // Recalculate for gizmos
        Vector3 camForwardFlat = new Vector3(mainCamera.transform.forward.x, 0, mainCamera.transform.forward.z).normalized;
        Vector3 perpendicular = new Vector3(-camForwardFlat.z, 0, camForwardFlat.x);

        Vector3 p1 = baseLineCenter + perpendicular * lineHalfWidth;
        Vector3 p2 = baseLineCenter - perpendicular * lineHalfWidth;
        p1.y = 0;
        p2.y = 0;

        Vector3 lightDir = -mainLight.transform.forward;
        Vector3 p3 = p1 + lightDir * extensionLength;
        Vector3 p4 = p2 + lightDir * extensionLength;

        // Draw the quad
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p3, p4);
        Gizmos.DrawLine(p1, p3);
        Gizmos.DrawLine(p2, p4);

        // Draw base points
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(p1, 2f);
        Gizmos.DrawSphere(p2, 2f);

        // Draw extended points
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(p3, 2f);
        Gizmos.DrawSphere(p4, 2f);

        // Draw center point
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(baseLineCenter, 2f);

        // Draw camera forward direction on XZ plane
        Gizmos.color = Color.green;
        Gizmos.DrawRay(baseLineCenter, camForwardFlat * 20f);

        // Draw perpendicular direction
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(baseLineCenter, perpendicular * 20f);
    }
}