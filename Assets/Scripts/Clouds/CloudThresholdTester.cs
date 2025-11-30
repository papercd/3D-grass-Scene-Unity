using UnityEditor;
using UnityEngine;

/// <summary>
/// Simple test to verify _CloudThreshold actually affects the cloud shader
/// Attach this to the cloud plane and manually adjust the slider
/// </summary>
public class CloudThresholdTester : MonoBehaviour
{
    [Header("Manual Test")]
    [Range(0f, 1f)]
    [Tooltip("Manually adjust this - you should see clouds change immediately")]
    public float testThreshold = 0.5f;

    [Header("Auto Animate Test")]
    public bool autoAnimate = false;
    public float animationSpeed = 0.5f;

    private Material cloudMaterial;
    private MeshRenderer meshRenderer;

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();

        if (meshRenderer != null && meshRenderer.material != null)
        {
            cloudMaterial = meshRenderer.material;
            Debug.Log($"CloudThresholdTester: Found material '{cloudMaterial.name}'");

            // Check all relevant properties
            Debug.Log($"  Has _CloudThreshold: {cloudMaterial.HasProperty("_CloudThreshold")}");
            Debug.Log($"  Has _CloudDensity: {cloudMaterial.HasProperty("_CloudDensity")}");
            Debug.Log($"  Has _CloudSharpness: {cloudMaterial.HasProperty("_CloudSharpness")}");
            Debug.Log($"  Has _NoiseScale: {cloudMaterial.HasProperty("_NoiseScale")}");

            if (cloudMaterial.HasProperty("_CloudThreshold"))
            {
                float current = cloudMaterial.GetFloat("_CloudThreshold");
                Debug.Log($"  Current _CloudThreshold value: {current}");
            }

            // List ALL properties on this material
            Debug.Log("=== ALL SHADER PROPERTIES ===");
            Shader shader = cloudMaterial.shader;
            int propertyCount = ShaderUtil.GetPropertyCount(shader);
            for (int i = 0; i < propertyCount; i++)
            {
                string propName = ShaderUtil.GetPropertyName(shader, i);
                ShaderUtil.ShaderPropertyType propType = ShaderUtil.GetPropertyType(shader, i);
                Debug.Log($"  [{i}] {propName} ({propType})");
            }
        }
        else
        {
            Debug.LogError("CloudThresholdTester: No MeshRenderer or material found!");
        }
    }

    void Update()
    {
        if (cloudMaterial == null) return;

        float targetThreshold = testThreshold;

        // Auto animate if enabled
        if (autoAnimate)
        {
            targetThreshold = Mathf.PingPong(Time.time * animationSpeed, 1f);
            testThreshold = targetThreshold;
        }

        // Set the threshold
        if (cloudMaterial.HasProperty("_CloudThreshold"))
        {
            cloudMaterial.SetFloat("_CloudThreshold", targetThreshold);
        }
    }

    void OnValidate()
    {
        // Update immediately when slider changes in inspector
        if (cloudMaterial != null && cloudMaterial.HasProperty("_CloudThreshold"))
        {
            cloudMaterial.SetFloat("_CloudThreshold", testThreshold);
        }
    }
}


