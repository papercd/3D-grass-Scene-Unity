using UnityEngine;

/// <summary>
/// Controls material colors based on time-of-day presets
/// FIXED: Now correctly finds CloudPlaneController's material instance
/// </summary>
public class MaterialPresetController : MonoBehaviour
{
    [System.Serializable]
    public class MaterialColorPreset
    {
        public string presetName = "Untitled";
        public float timeOfDay = 0f;

        [Header("Material Colors")]
        [ColorUsage(true, false)]
        public Color grassTint = new Color(0.52f, 0.87f, 0.38f);
        [ColorUsage(true, false)]
        public Color leafTint = new Color(0.52f, 0.87f, 0.38f);
        [ColorUsage(true, false)]
        public Color canopyTint = new Color(0.52f, 0.87f, 0.38f);
        [ColorUsage(true, false)]
        public Color terrainBaseColor = new Color(0.52f, 0.87f, 0.38f);
        [ColorUsage(true, false)]
        public Color trunkBaseColor = new Color(0.68f, 0.53f, 0.16f);
        [ColorUsage(true, false)]
        public Color cloudColor = Color.white;

        [Header("God Rays")]
        [ColorUsage(true, false)]
        public Color godRayColor = new Color(1f, 0.95f, 0.85f);
        [Range(0f, 10f)]
        public float godRayIntensity = 1f;

        [Header("Cloud Settings")]
        [Range(0f, 1f)]
        public float cloudThreshold = 0.5f;
    }

    [System.Serializable]
    public class CustomMaterialOverride
    {
        public Material material;
        public string propertyName = "_Tint";
        public Color nightColor = Color.white;
        public Color goldenHourColor = Color.white;
        public Color dayColor = Color.white;
        public Color twilightColor = Color.white;
    }

    [Header("Material References")]
    public Material grassMaterial;
    public Material leafMaterial;
    public Material canopyMaterial;
    public Material terrainMaterial;
    public Material treeTrunkMaterial;
    public Material cloudMaterial;

    [Header("God Rays")]
    [Tooltip("Volumetric god ray material (uses _Color and _MainLightIntensity)")]
    public Material godRayMaterial;

    [Header("Cloud Plane - AUTO FINDS INSTANCE")]
    [Tooltip("CloudPlaneController GameObject - will auto-find its material instance")]
    public CloudPlaneController cloudPlaneController;

    [Header("Runtime Material Instance Renderers")]
    [Tooltip("Leaf renderers - will use leafTint color")]
    public LeafInstanceRenderer[] leafInstanceRenderers;

    [Header("Presets")]
    [Tooltip("ScriptableObject asset that holds all color presets - can be saved in Play Mode!")]
    public MaterialColorPresetsAsset presetsAsset;

    [Header("Custom Materials")]
    public CustomMaterialOverride[] customMaterials;

    [Header("Blending")]
    public bool useSmoothBlending = true;

    [Header("Day-Night Cycle Integration")]
    public PresetBasedDayNightCycle dayNightCycle;
    public bool autoUpdateFromCycle = true;

    [Header("Debug")]
    public bool verboseLogging = false;

    private int lastPreset1 = -1;
    private int lastPreset2 = -1;
    private Material cloudPlaneMaterialInstance;  // Cache the instance

    void Start()
    {
        // Find day-night cycle if not assigned
        if (dayNightCycle == null)
        {
            dayNightCycle = FindObjectOfType<PresetBasedDayNightCycle>();
        }

        // Find cloud plane controller if not assigned
        if (cloudPlaneController == null)
        {
            cloudPlaneController = FindObjectOfType<CloudPlaneController>();
        }

        // Get the cloud plane material INSTANCE (not the asset!)
        if (cloudPlaneController != null && cloudPlaneController.materialInstance != null)
        {
            cloudPlaneMaterialInstance = cloudPlaneController.materialInstance;
            Debug.Log($"✅ Found CloudPlaneController material instance: {cloudPlaneMaterialInstance.name}");
        }
        else if (cloudPlaneController != null)
        {
            Debug.LogWarning("⚠️ CloudPlaneController found but materialInstance is null! Make sure CloudPlaneController.Awake() runs first.");
        }
        else
        {
            Debug.LogWarning("⚠️ No CloudPlaneController found in scene!");
        }

        // Find ALL leaf instance renderers automatically if arrays are empty
        if (leafInstanceRenderers == null || leafInstanceRenderers.Length == 0)
        {
            var allRenderers = FindObjectsOfType<LeafInstanceRenderer>();

            if (allRenderers.Length > 0)
            {
                Debug.Log($"MaterialPresetController: Auto-found {allRenderers.Length} LeafInstanceRenderer(s)");
                leafInstanceRenderers = allRenderers;
            }
        }

        // Ensure all have material instances created
        EnsureAllMaterialInstances();

        // Log preset info
        if (presetsAsset != null && presetsAsset.colorPresets != null)
        {
            Debug.Log($"=== MATERIAL PRESETS LOADED ===");
            for (int i = 0; i < presetsAsset.colorPresets.Length; i++)
            {
                var p = presetsAsset.colorPresets[i];
                Debug.Log($"  [{i}] {p.presetName} @ {p.timeOfDay:F1} hours");
            }
        }
    }

    void Update()
    {
        if (!autoUpdateFromCycle || dayNightCycle == null || presetsAsset == null)
            return;

        // Get current time from day-night cycle
        float currentTime = dayNightCycle.currentTime;

        // Find which two presets we're between
        int preset1Index = -1;
        int preset2Index = -1;
        float blendFactor = 0f;

        FindPresetsForTime(currentTime, out preset1Index, out preset2Index, out blendFactor);

        // Log when presets change
        if (preset1Index != lastPreset1 || preset2Index != lastPreset2)
        {
            if (preset1Index >= 0 && preset1Index < presetsAsset.colorPresets.Length &&
                preset2Index >= 0 && preset2Index < presetsAsset.colorPresets.Length)
            {
                Debug.Log($"⏰ TIME {currentTime:F2} → Blending [{preset1Index}] {presetsAsset.colorPresets[preset1Index].presetName} → [{preset2Index}] {presetsAsset.colorPresets[preset2Index].presetName}");
            }
            lastPreset1 = preset1Index;
            lastPreset2 = preset2Index;
        }

        if (preset1Index >= 0 && preset2Index >= 0)
        {
            ApplyBlendedPreset(preset1Index, preset2Index, blendFactor);
        }
    }

    void FindPresetsForTime(float currentTime, out int preset1, out int preset2, out float blend)
    {
        preset1 = 0;
        preset2 = 1;
        blend = 0f;

        if (presetsAsset == null || presetsAsset.colorPresets == null || presetsAsset.colorPresets.Length < 2)
            return;

        // Find which two presets we're between
        for (int i = 0; i < presetsAsset.colorPresets.Length; i++)
        {
            if (currentTime >= presetsAsset.colorPresets[i].timeOfDay)
            {
                preset1 = i;
                preset2 = (i + 1) % presetsAsset.colorPresets.Length;
            }
        }

        // Calculate blend factor
        float startTime = presetsAsset.colorPresets[preset1].timeOfDay;
        float endTime = presetsAsset.colorPresets[preset2].timeOfDay;

        // Handle wrap around midnight
        if (endTime < startTime)
            endTime += 24f;

        float adjustedCurrentTime = currentTime;
        if (adjustedCurrentTime < startTime)
            adjustedCurrentTime += 24f;

        float duration = endTime - startTime;
        float elapsed = adjustedCurrentTime - startTime;

        blend = Mathf.Clamp01(elapsed / duration);
    }

    void EnsureAllMaterialInstances()
    {
        if (leafInstanceRenderers != null)
        {
            foreach (var renderer in leafInstanceRenderers)
            {
                if (renderer != null && renderer.leafMaterial != null && renderer.materialInstance == null)
                {
                    renderer.materialInstance = new Material(renderer.leafMaterial);
                    Debug.Log($"Created material instance for {renderer.gameObject.name}");
                }
            }
        }
    }

    public void ApplyBlendedPreset(int presetIndex1, int presetIndex2, float t)
    {
        if (presetsAsset == null || presetsAsset.colorPresets == null ||
            presetIndex1 >= presetsAsset.colorPresets.Length || presetIndex2 >= presetsAsset.colorPresets.Length)
            return;

        var preset1 = presetsAsset.colorPresets[presetIndex1];
        var preset2 = presetsAsset.colorPresets[presetIndex2];

        if (useSmoothBlending)
            t = Mathf.SmoothStep(0, 1, t);

        // Blend and apply colors
        Color grassColor = Color.Lerp(preset1.grassTint, preset2.grassTint, t);
        Color leafColor = Color.Lerp(preset1.leafTint, preset2.leafTint, t);
        Color canopyColor = Color.Lerp(preset1.canopyTint, preset2.canopyTint, t);
        Color terrainColor = Color.Lerp(preset1.terrainBaseColor, preset2.terrainBaseColor, t);
        Color trunkColor = Color.Lerp(preset1.trunkBaseColor, preset2.trunkBaseColor, t);
        Color cloudColor = Color.Lerp(preset1.cloudColor, preset2.cloudColor, t);

        // Blend god ray properties
        Color godRayColor = Color.Lerp(preset1.godRayColor, preset2.godRayColor, t);
        float godRayIntensity = Mathf.Lerp(preset1.godRayIntensity, preset2.godRayIntensity, t);

        // Blend cloud threshold
        float cloudThreshold = Mathf.Lerp(preset1.cloudThreshold, preset2.cloudThreshold, t);

        // Apply to standard materials
        SetMaterialColor(grassMaterial, "_Tint", grassColor);
        SetMaterialColor(leafMaterial, "_Tint", leafColor);
        SetMaterialColor(canopyMaterial, "_BaseColor", canopyColor);
        SetMaterialColor(terrainMaterial, "_BaseColor", terrainColor);
        SetMaterialColor(treeTrunkMaterial, "_BaseColor", trunkColor);
        SetMaterialColor(cloudMaterial, "_Color", cloudColor);

        // Apply god ray settings
        if (godRayMaterial != null)
        {
            if (godRayMaterial.HasProperty("_Color"))
                godRayMaterial.SetColor("_Color", godRayColor);

            if (godRayMaterial.HasProperty("_MainLightIntensity"))
                godRayMaterial.SetFloat("_MainLightIntensity", godRayIntensity);

            if (verboseLogging)
                Debug.Log($"🌟 God Rays: Color={godRayColor}, Intensity={godRayIntensity:F2}");
        }

        // Apply cloud threshold to MATERIAL INSTANCE (not asset!)
        if (cloudPlaneMaterialInstance != null)
        {
            if (cloudPlaneMaterialInstance.HasProperty("_CloudThreshold"))
            {
                cloudPlaneMaterialInstance.SetFloat("_CloudThreshold", cloudThreshold);

                if (verboseLogging)
                    Debug.Log($"☁️ Cloud Threshold: {cloudThreshold:F2} (applied to INSTANCE)");
            }
        }

        // Apply to runtime instances (leaves only)
        ApplyToLeafInstances(leafColor);

        // Apply to custom materials
        if (customMaterials != null)
        {
            foreach (var custom in customMaterials)
            {
                if (custom.material != null)
                {
                    Color color1 = GetCustomPresetColor(custom, presetIndex1);
                    Color color2 = GetCustomPresetColor(custom, presetIndex2);
                    Color blendedColor = Color.Lerp(color1, color2, t);
                    custom.material.SetColor(custom.propertyName, blendedColor);
                }
            }
        }
    }

    private void ApplyToLeafInstances(Color leafColor)
    {
        if (leafInstanceRenderers == null) return;

        foreach (var renderer in leafInstanceRenderers)
        {
            if (renderer != null && renderer.materialInstance != null)
            {
                if (renderer.materialInstance.HasProperty("_Tint"))
                {
                    renderer.materialInstance.SetColor("_Tint", leafColor);
                }
            }
        }
    }

    private void SetMaterialColor(Material mat, string propertyName, Color color)
    {
        if (mat != null && mat.HasProperty(propertyName))
        {
            mat.SetColor(propertyName, color);
        }
    }

    private Color GetCustomPresetColor(CustomMaterialOverride custom, int presetIndex)
    {
        switch (presetIndex)
        {
            case 0: return custom.nightColor;
            case 1: return custom.goldenHourColor;
            case 2: return custom.dayColor;
            case 3: return custom.twilightColor;
            default: return Color.white;
        }
    }

    public void CaptureCurrentMaterialColors()
    {
        Debug.Log("Capturing current material colors...");

        if (grassMaterial != null && grassMaterial.HasProperty("_Tint"))
            Debug.Log($"Grass: {grassMaterial.GetColor("_Tint")}");

        if (leafMaterial != null && leafMaterial.HasProperty("_Tint"))
            Debug.Log($"Leaf: {leafMaterial.GetColor("_Tint")}");

        if (canopyMaterial != null && canopyMaterial.HasProperty("_BaseColor"))
            Debug.Log($"Canopy: {canopyMaterial.GetColor("_BaseColor")}");

        if (terrainMaterial != null && terrainMaterial.HasProperty("_BaseColor"))
            Debug.Log($"Terrain: {terrainMaterial.GetColor("_BaseColor")}");

        if (treeTrunkMaterial != null && treeTrunkMaterial.HasProperty("_BaseColor"))
            Debug.Log($"Trunk: {treeTrunkMaterial.GetColor("_BaseColor")}");

        if (cloudMaterial != null && cloudMaterial.HasProperty("_Color"))
            Debug.Log($"Cloud: {cloudMaterial.GetColor("_Color")}");

        if (godRayMaterial != null)
        {
            if (godRayMaterial.HasProperty("_Color"))
                Debug.Log($"God Ray Color: {godRayMaterial.GetColor("_Color")}");
            if (godRayMaterial.HasProperty("_MainLightIntensity"))
                Debug.Log($"God Ray Intensity: {godRayMaterial.GetFloat("_MainLightIntensity")}");
        }

        if (cloudPlaneMaterialInstance != null && cloudPlaneMaterialInstance.HasProperty("_CloudThreshold"))
            Debug.Log($"Cloud Threshold (INSTANCE): {cloudPlaneMaterialInstance.GetFloat("_CloudThreshold")}");
    }

    public void ApplyCurrentPreset(int presetIndex)
    {
        if (presetsAsset == null || presetsAsset.colorPresets == null || presetIndex >= presetsAsset.colorPresets.Length)
            return;

        var preset = presetsAsset.colorPresets[presetIndex];

        SetMaterialColor(grassMaterial, "_Tint", preset.grassTint);
        SetMaterialColor(leafMaterial, "_Tint", preset.leafTint);
        SetMaterialColor(canopyMaterial, "_BaseColor", preset.canopyTint);
        SetMaterialColor(terrainMaterial, "_BaseColor", preset.terrainBaseColor);
        SetMaterialColor(treeTrunkMaterial, "_BaseColor", preset.trunkBaseColor);
        SetMaterialColor(cloudMaterial, "_Color", preset.cloudColor);

        // Apply god rays
        if (godRayMaterial != null)
        {
            if (godRayMaterial.HasProperty("_Color"))
                godRayMaterial.SetColor("_Color", preset.godRayColor);
            if (godRayMaterial.HasProperty("_MainLightIntensity"))
                godRayMaterial.SetFloat("_MainLightIntensity", preset.godRayIntensity);
        }

        // Apply cloud threshold to INSTANCE
        if (cloudPlaneMaterialInstance != null && cloudPlaneMaterialInstance.HasProperty("_CloudThreshold"))
            cloudPlaneMaterialInstance.SetFloat("_CloudThreshold", preset.cloudThreshold);

        // Apply to instances
        ApplyToLeafInstances(preset.leafTint);

        Debug.Log($"Applied preset: {preset.presetName}");
    }
}