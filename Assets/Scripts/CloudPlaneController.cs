using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class CloudPlaneController : MonoBehaviour
{
    [Header("Cloud Settings")]
    [Range(0.1f, 50f)]
    public float noiseScale = 5f;

    [Range(0f, 1f)]
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
    public Color cloudColor = Color.white;

    [Range(0f, 1f)]
    public float edgeFade = 0.1f;

    private Material cloudMaterial;
    private MeshRenderer meshRenderer;

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();

        // Get or create material instance
        if (meshRenderer.sharedMaterial != null)
        {
            cloudMaterial = new Material(meshRenderer.sharedMaterial);
            meshRenderer.material = cloudMaterial;
        }

        UpdateMaterial();
    }

    void Update()
    {
        if (cloudMaterial != null)
        {
            UpdateMaterial();
        }
    }

    void UpdateMaterial()
    {
        cloudMaterial.SetFloat("_NoiseScale", noiseScale);
        cloudMaterial.SetVector("_NoiseSpeed", cloudSpeed);
        cloudMaterial.SetFloat("_CloudDensity", cloudDensity);
        cloudMaterial.SetFloat("_CloudSharpness", cloudSharpness);
        cloudMaterial.SetFloat("_OpacityMin", minOpacity);
        cloudMaterial.SetFloat("_OpacityMax", maxOpacity);
        cloudMaterial.SetColor("_Color", cloudColor);
        cloudMaterial.SetFloat("_EdgeFade", edgeFade);

        cloudMaterial.SetFloat("_SecondaryNoiseScale", secondaryNoiseScale);
        cloudMaterial.SetVector("_SecondaryNoiseSpeed", secondaryCloudSpeed);
        cloudMaterial.SetFloat("_SecondaryNoiseStrength", secondaryNoiseStrength);
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
        plane.AddComponent<CloudPlaneController>();
    }
}