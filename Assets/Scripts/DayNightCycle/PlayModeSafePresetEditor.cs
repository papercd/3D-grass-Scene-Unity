using UnityEngine;
using UnityEditor;

/// <summary>
/// Play Mode safe preset editor - can save to ScriptableObject even during Play Mode!
/// NOW INCLUDES: God rays, cloud threshold, and AMBIENT SOUNDS
/// </summary>
[ExecuteInEditMode]
public class PlayModeSafePresetEditor : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The ScriptableObject asset that holds all presets")]
    public MaterialColorPresetsAsset presetsAsset;

    [Tooltip("The controller that applies the presets")]
    public MaterialPresetController presetController;

    [Tooltip("The ambient sound controller")]
    public AmbientSoundController ambientSoundController;

    [Header("Editing Controls")]
    [Tooltip("Which preset slot you're currently editing (0=Night, 1=Golden Hour, 2=Day, 3=Twilight)")]
    [Range(0, 3)]
    public int currentPresetIndex = 0;

    [Tooltip("When checked, changes are instantly applied to materials")]
    public bool autoApplyChanges = true;

    [Header("Current Preset Being Edited")]
    public string presetName = "Night";

    [Range(0f, 24f)]
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
    [Tooltip("Cloud threshold - LOWER = more clouds, HIGHER = fewer clouds")]
    public float cloudThreshold = 0.5f;

    [Header("Ambient Sound")]
    [Tooltip("Ambient sound for this preset (e.g., crickets, birds, wind)")]
    public AudioClip ambientSound;

    [Range(0f, 1f)]
    [Tooltip("Volume for this ambient sound")]
    public float ambientVolume = 0.7f;

    [Header("Preview Settings")]
    public bool freezeTime = false;

    [Tooltip("Reference to the day-night cycle for preview")]
    public PresetBasedDayNightCycle dayNightCycle;

    // Cache cloud plane material instance
    private Material cloudPlaneMaterialInstance;

    void Start()
    {
        if (presetsAsset == null)
        {
            Debug.LogError("PlayModeSafePresetEditor: No presetsAsset assigned!");
            return;
        }

        if (presetController == null)
        {
            presetController = FindObjectOfType<MaterialPresetController>();
        }

        if (dayNightCycle == null)
        {
            dayNightCycle = FindObjectOfType<PresetBasedDayNightCycle>();
        }

        if (ambientSoundController == null)
        {
            ambientSoundController = FindObjectOfType<AmbientSoundController>();
        }

        // Get cloud plane material instance from controller
        if (presetController != null && presetController.cloudPlaneController != null)
        {
            cloudPlaneMaterialInstance = presetController.cloudPlaneController.materialInstance;
        }

        // Load the first preset by default
        LoadPresetToEditor(currentPresetIndex);
    }

    void Update()
    {
        // If auto-apply is on, continuously apply changes to materials
        if (autoApplyChanges && !freezeTime)
        {
            ApplyColorsToMaterials();
        }
    }

    void OnValidate()
    {
        // When values change in inspector, apply them if auto-apply is on
        if (autoApplyChanges && Application.isPlaying)
        {
            ApplyColorsToMaterials();
        }
    }

    /// <summary>
    /// Load a preset from the asset into the editor fields
    /// </summary>
    public void LoadPresetToEditor(int index)
    {
        if (presetsAsset == null || presetsAsset.colorPresets == null ||
            index < 0 || index >= presetsAsset.colorPresets.Length)
            return;

        var preset = presetsAsset.colorPresets[index];

        currentPresetIndex = index;
        presetName = preset.presetName;
        timeOfDay = preset.timeOfDay;
        grassTint = preset.grassTint;
        leafTint = preset.leafTint;
        canopyTint = preset.canopyTint;
        terrainBaseColor = preset.terrainBaseColor;
        trunkBaseColor = preset.trunkBaseColor;
        cloudColor = preset.cloudColor;
        godRayColor = preset.godRayColor;
        godRayIntensity = preset.godRayIntensity;
        cloudThreshold = preset.cloudThreshold;
        ambientSound = preset.ambientSound;  // NEW
        ambientVolume = preset.ambientVolume;  // NEW

        Debug.Log($"Loaded preset [{index}] {presetName} into editor");
    }

    /// <summary>
    /// Save current editor values back to the preset in the ScriptableObject
    /// WORKS IN PLAY MODE!
    /// </summary>
    public void SaveCurrentPreset()
    {
        if (presetsAsset == null || presetsAsset.colorPresets == null ||
            currentPresetIndex < 0 || currentPresetIndex >= presetsAsset.colorPresets.Length)
            return;

        var preset = presetsAsset.colorPresets[currentPresetIndex];

        preset.presetName = presetName;
        preset.timeOfDay = timeOfDay;
        preset.grassTint = grassTint;
        preset.leafTint = leafTint;
        preset.canopyTint = canopyTint;
        preset.terrainBaseColor = terrainBaseColor;
        preset.trunkBaseColor = trunkBaseColor;
        preset.cloudColor = cloudColor;
        preset.godRayColor = godRayColor;
        preset.godRayIntensity = godRayIntensity;
        preset.cloudThreshold = cloudThreshold;
        preset.ambientSound = ambientSound;  // NEW
        preset.ambientVolume = ambientVolume;  // NEW

#if UNITY_EDITOR
        EditorUtility.SetDirty(presetsAsset);
        AssetDatabase.SaveAssets();
#endif

        // Notify ambient sound controller to reinitialize
        if (ambientSoundController != null)
        {
            ambientSoundController.RestartAllAudio();
        }

        Debug.Log($"✅ SAVED preset [{currentPresetIndex}] {presetName} to ScriptableObject (Play Mode Safe!)");
    }

    /// <summary>
    /// Immediately apply current editor colors to all materials
    /// </summary>
    private void ApplyColorsToMaterials()
    {
        if (presetController == null) return;

        // Refresh cloud plane material instance if needed
        if (cloudPlaneMaterialInstance == null && presetController.cloudPlaneController != null)
        {
            cloudPlaneMaterialInstance = presetController.cloudPlaneController.materialInstance;
        }

        // Apply to standard materials
        SetMaterialColor(presetController.grassMaterial, "_Tint", grassTint);
        SetMaterialColor(presetController.leafMaterial, "_Tint", leafTint);
        SetMaterialColor(presetController.canopyMaterial, "_BaseColor", canopyTint);
        SetMaterialColor(presetController.terrainMaterial, "_BaseColor", terrainBaseColor);
        SetMaterialColor(presetController.treeTrunkMaterial, "_BaseColor", trunkBaseColor);
        SetMaterialColor(presetController.cloudMaterial, "_Color", cloudColor);

        // Apply god rays
        if (presetController.godRayMaterial != null)
        {
            if (presetController.godRayMaterial.HasProperty("_Color"))
                presetController.godRayMaterial.SetColor("_Color", godRayColor);

            if (presetController.godRayMaterial.HasProperty("_MainLightIntensity"))
                presetController.godRayMaterial.SetFloat("_MainLightIntensity", godRayIntensity);
        }

        // Apply cloud threshold to INSTANCE
        if (cloudPlaneMaterialInstance != null)
        {
            if (cloudPlaneMaterialInstance.HasProperty("_CloudThreshold"))
                cloudPlaneMaterialInstance.SetFloat("_CloudThreshold", cloudThreshold);
        }

        // Apply to leaf instances
        if (presetController.leafInstanceRenderers != null)
        {
            foreach (var renderer in presetController.leafInstanceRenderers)
            {
                if (renderer != null && renderer.materialInstance != null)
                {
                    if (renderer.materialInstance.HasProperty("_Tint"))
                        renderer.materialInstance.SetColor("_Tint", leafTint);
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

    // ========================================
    // CONTEXT MENU WORKFLOW
    // ========================================

    [ContextMenu("1️⃣ Edit Night Colors (00:00)")]
    void EditNightColors()
    {
        LoadPresetToEditor(0);
        autoApplyChanges = true;
        freezeTime = true;
        if (dayNightCycle != null) dayNightCycle.currentTime = 0f;
        Debug.Log("🌙 EDITING NIGHT PRESET - Adjust colors & sounds in Inspector, then Save");
    }

    [ContextMenu("2️⃣ Edit Golden Hour (06:00)")]
    void EditGoldenHourColors()
    {
        LoadPresetToEditor(1);
        autoApplyChanges = true;
        freezeTime = true;
        if (dayNightCycle != null) dayNightCycle.currentTime = 6f;
        Debug.Log("🌅 EDITING GOLDEN HOUR PRESET - Adjust colors & sounds in Inspector, then Save");
    }

    [ContextMenu("3️⃣ Edit Day Colors (12:00)")]
    void EditDayColors()
    {
        LoadPresetToEditor(2);
        autoApplyChanges = true;
        freezeTime = true;
        if (dayNightCycle != null) dayNightCycle.currentTime = 12f;
        Debug.Log("☀️ EDITING DAY PRESET - Adjust colors & sounds in Inspector, then Save");
    }

    [ContextMenu("4️⃣ Edit Twilight Colors (18:00)")]
    void EditTwilightColors()
    {
        LoadPresetToEditor(3);
        autoApplyChanges = true;
        freezeTime = true;
        if (dayNightCycle != null) dayNightCycle.currentTime = 18f;
        Debug.Log("🌆 EDITING TWILIGHT PRESET - Adjust colors & sounds in Inspector, then Save");
    }

    [ContextMenu("💾 Save This Preset (PLAY MODE SAFE)")]
    void SavePreset()
    {
        SaveCurrentPreset();
    }

    [ContextMenu("▶️ Preview Full Cycle (2 minutes)")]
    void PreviewCycle()
    {
        if (dayNightCycle != null)
        {
            freezeTime = false;
            dayNightCycle.autoAdvanceTime = true;
            dayNightCycle.dayDurationInMinutes = 2f;
            dayNightCycle.currentTime = 0f;

            if (presetController != null)
            {
                presetController.autoUpdateFromCycle = true;
            }

            Debug.Log("▶️ PREVIEW STARTED - Full 24hr cycle in 2 minutes (visuals + audio)");
        }
    }

    [ContextMenu("⏸️ Pause Preview")]
    void PausePreview()
    {
        freezeTime = true;
        if (dayNightCycle != null)
        {
            dayNightCycle.autoAdvanceTime = false;
        }
        Debug.Log("⏸️ PREVIEW PAUSED");
    }

    [ContextMenu("📋 Show Current Material Values")]
    void ShowCurrentMaterialValues()
    {
        if (presetController != null)
        {
            presetController.CaptureCurrentMaterialColors();
        }

        if (ambientSoundController != null)
        {
            ambientSoundController.ListCurrentVolumes();
        }
    }

    [ContextMenu("🔊 Preview Current Ambient Sound")]
    void PreviewAmbientSound()
    {
        if (ambientSound != null)
        {
            // Find or create a temporary audio source for preview
            AudioSource previewSource = GetComponent<AudioSource>();
            if (previewSource == null)
            {
                previewSource = gameObject.AddComponent<AudioSource>();
                previewSource.playOnAwake = false;
                previewSource.spatialBlend = 0f;
            }

            previewSource.clip = ambientSound;
            previewSource.volume = ambientVolume;
            previewSource.loop = true;
            previewSource.Play();

            Debug.Log($"🔊 Playing preview: {ambientSound.name} at volume {ambientVolume:F2}");
        }
        else
        {
            Debug.LogWarning("No ambient sound assigned to preview!");
        }
    }

    [ContextMenu("🔇 Stop Preview Sound")]
    void StopPreviewSound()
    {
        AudioSource previewSource = GetComponent<AudioSource>();
        if (previewSource != null)
        {
            previewSource.Stop();
            Debug.Log("🔇 Stopped preview sound");
        }
    }
}