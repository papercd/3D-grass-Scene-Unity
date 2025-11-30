using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class CloudPlaneController : MonoBehaviour
{
    [Header("External Control")]
    [Tooltip("If true, allows MaterialPresetController to control cloudColor and cloudThreshold")]
    public bool allowExternalControl = true;

    [Header("Cloud Settings")]
    [Range(0.1f, 50f)]
    public float noiseScale = 5f;

    [Range(0f, 1f)]
    [Tooltip("Only used if allowExternalControl is false")]
    public float cloudDensity = 0.5f;

    [Range(0.1f, 10f)]
    public float cloudSharpness = 2f;

    [Header("Animation")]
    public Vector2 cloudSpeed = new Vector2(0.1f, 0.1f);
    public Vector2 secondaryCloudSpeed = new Vector2(0.05f, 0.05f);

    [Header("Opacity")]
    [Range(0f, 1f)]
    public float minOpacity = 0f;

    [Range(0f, 1f)]
    public float maxOpacity = 1f;

    [Header("Secondary Noise")]
    [Range(0.1f, 50f)]
    public float secondaryNoiseScale = 10f;

    [Range(0f, 1f)]
    public float secondaryNoiseStrength = 0.3f;

    [Header("Appearance")]
    [Tooltip("Only used if allowExternalControl is false")]
    public Color cloudColor = Color.white;

    [Range(0f, 1f)]
    public float edgeFade = 0.1f;

    // EXPOSED: Public access to the material instance
    [HideInInspector]
    public Material materialInstance;

    private MeshRenderer meshRenderer;

    void Awake()  // CHANGED: Use Awake instead of Start
    {
        meshRenderer = GetComponent<MeshRenderer>();

        // Get or create material instance
        if (meshRenderer.sharedMaterial != null)
        {
            materialInstance = new Material(meshRenderer.sharedMaterial);
            meshRenderer.material = materialInstance;

            Debug.Log($"CloudPlaneController: Created material instance '{materialInstance.name}' on {gameObject.name}");
        }

        UpdateMaterial();
    }

    void Update()
    {
        if (materialInstance != null)
        {
            UpdateMaterial();
        }
    }

    void UpdateMaterial()
    {
        // Always set these properties (not controlled by preset system)
        materialInstance.SetFloat("_NoiseScale", noiseScale);
        materialInstance.SetVector("_NoiseSpeed", cloudSpeed);
        materialInstance.SetFloat("_CloudSharpness", cloudSharpness);
        materialInstance.SetFloat("_OpacityMin", minOpacity);
        materialInstance.SetFloat("_OpacityMax", maxOpacity);
        materialInstance.SetFloat("_EdgeFade", edgeFade);
        materialInstance.SetFloat("_SecondaryNoiseScale", secondaryNoiseScale);
        materialInstance.SetVector("_SecondaryNoiseSpeed", secondaryCloudSpeed);
        materialInstance.SetFloat("_SecondaryNoiseStrength", secondaryNoiseStrength);

        // Only set these if NOT externally controlled
        if (!allowExternalControl)
        {
            materialInstance.SetFloat("_CloudDensity", cloudDensity);
            materialInstance.SetColor("_Color", cloudColor);

            // Also set threshold if it exists
            if (materialInstance.HasProperty("_CloudThreshold"))
            {
                materialInstance.SetFloat("_CloudThreshold", cloudDensity);
            }
        }
        // If externally controlled, MaterialPresetController will set _Color and _CloudThreshold
    }

    [ContextMenu("Create Cloud Plane")]
    void CreateCloudPlane()
    {
        // Create a simple plane if one doesn't exist
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = "Cloud Plane";
        plane.transform.position = transform.position;
        plane.transform.localScale = new Vector3(10, 1, 10); // Scale as needed

        // Apply the cloud material
        MeshRenderer renderer = plane.GetComponent<MeshRenderer>();
        Material mat = new Material(Shader.Find("Custom/CloudPlaneGodRays"));
        renderer.material = mat;

        // Add the controller
        CloudPlaneController controller = plane.AddComponent<CloudPlaneController>();
        controller.allowExternalControl = true; // Enable by default
    }
}