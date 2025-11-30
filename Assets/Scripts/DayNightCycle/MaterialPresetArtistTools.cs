using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Artist-friendly tools for capturing and saving material color presets
/// Now works with ScriptableObject for Play Mode safe saving!
/// </summary>
public class MaterialPresetArtistTools : MonoBehaviour
{
    [Header("References")]
    public MaterialPresetController presetController;

    [Header("Capture Settings")]
    [Tooltip("Which preset slot to save the captured colors to (0=Night, 1=Golden Hour, 2=Day, 3=Twilight)")]
    [Range(0, 3)]
    public int targetPresetIndex = 0;

    void Start()
    {
        if (presetController == null)
            presetController = FindObjectOfType<MaterialPresetController>();
    }

    [ContextMenu("📸 Capture Current Colors → Preset")]
    void CaptureCurrentColors()
    {
        if (presetController == null)
        {
            Debug.LogError("❌ MaterialPresetController not found!");
            return;
        }

        if (presetController.presetsAsset == null)
        {
            Debug.LogError("❌ Presets Asset not assigned in MaterialPresetController!");
            return;
        }

        if (targetPresetIndex >= presetController.presetsAsset.colorPresets.Length)
        {
            Debug.LogError($"❌ Invalid preset index {targetPresetIndex}!");
            return;
        }

        var preset = presetController.presetsAsset.colorPresets[targetPresetIndex];

        Debug.Log($"📸 Capturing current material colors → {preset.presetName} preset...");

        // Capture grass
        if (presetController.grassMaterial != null && presetController.grassMaterial.HasProperty("_Tint"))
        {
            preset.grassTint = presetController.grassMaterial.GetColor("_Tint");
            Debug.Log($"   Grass: {preset.grassTint}");
        }

        // Capture leaf
        if (presetController.leafMaterial != null && presetController.leafMaterial.HasProperty("_Tint"))
        {
            preset.leafTint = presetController.leafMaterial.GetColor("_Tint");
            Debug.Log($"   Leaf: {preset.leafTint}");
        }

        // Capture canopy (uses _BaseColor not _Tint)
        if (presetController.canopyMaterial != null && presetController.canopyMaterial.HasProperty("_BaseColor"))
        {
            preset.canopyTint = presetController.canopyMaterial.GetColor("_BaseColor");
            Debug.Log($"   Canopy: {preset.canopyTint}");
        }

        // Capture terrain
        if (presetController.terrainMaterial != null && presetController.terrainMaterial.HasProperty("_BaseColor"))
        {
            preset.terrainBaseColor = presetController.terrainMaterial.GetColor("_BaseColor");
            Debug.Log($"   Terrain: {preset.terrainBaseColor}");
        }

        // Capture trunk
        if (presetController.treeTrunkMaterial != null && presetController.treeTrunkMaterial.HasProperty("_BaseColor"))
        {
            preset.trunkBaseColor = presetController.treeTrunkMaterial.GetColor("_BaseColor");
            Debug.Log($"   Trunk: {preset.trunkBaseColor}");
        }

        // Capture clouds
        if (presetController.cloudMaterial != null && presetController.cloudMaterial.HasProperty("_Color"))
        {
            preset.cloudColor = presetController.cloudMaterial.GetColor("_Color");
            Debug.Log($"   Cloud: {preset.cloudColor}");
        }

#if UNITY_EDITOR
        // Mark the ScriptableObject as dirty
        EditorUtility.SetDirty(presetController.presetsAsset);

        // Save the asset (works even in Play Mode!)
        AssetDatabase.SaveAssets();

        Debug.Log($"✅ SAVED captured colors to {preset.presetName} preset! (Play Mode Safe)");
#else
        Debug.LogWarning("⚠️ Save only works in Editor!");
#endif
    }

    [ContextMenu("📋 Print All Presets")]
    void PrintAllPresets()
    {
        if (presetController == null || presetController.presetsAsset == null)
        {
            Debug.LogError("❌ Preset controller or asset not found!");
            return;
        }

        Debug.Log("=== ALL PRESETS ===");

        for (int i = 0; i < presetController.presetsAsset.colorPresets.Length; i++)
        {
            var preset = presetController.presetsAsset.colorPresets[i];
            Debug.Log($"\n{i}. {preset.presetName} (Time: {preset.timeOfDay:00}:00)");
            Debug.Log($"   Grass: {preset.grassTint}");
            Debug.Log($"   Leaf: {preset.leafTint}");
            Debug.Log($"   Canopy: {preset.canopyTint}");
            Debug.Log($"   Terrain: {preset.terrainBaseColor}");
            Debug.Log($"   Trunk: {preset.trunkBaseColor}");
            Debug.Log($"   Cloud: {preset.cloudColor}");
        }
    }

    [ContextMenu("🎨 Apply Preset to Materials")]
    void ApplyPresetToMaterials()
    {
        if (presetController == null)
        {
            Debug.LogError("❌ MaterialPresetController not found!");
            return;
        }

        presetController.ApplyCurrentPreset(targetPresetIndex);
        Debug.Log($"✅ Applied preset {targetPresetIndex} to all materials");
    }

    [ContextMenu("🔄 Reset Preset to Defaults")]
    void ResetPresetToDefaults()
    {
        if (presetController == null || presetController.presetsAsset == null)
        {
            Debug.LogError("❌ Preset controller or asset not found!");
            return;
        }

        if (targetPresetIndex >= presetController.presetsAsset.colorPresets.Length)
        {
            Debug.LogError($"❌ Invalid preset index {targetPresetIndex}!");
            return;
        }

        var preset = presetController.presetsAsset.colorPresets[targetPresetIndex];

        // Reset to sensible defaults based on time of day
        switch (targetPresetIndex)
        {
            case 0: // Night
                preset.grassTint = new Color(0.22f, 0.38f, 0.18f);
                preset.leafTint = new Color(0.22f, 0.38f, 0.18f);
                preset.canopyTint = new Color(0.22f, 0.38f, 0.18f);
                preset.terrainBaseColor = new Color(0.22f, 0.38f, 0.18f);
                preset.trunkBaseColor = new Color(0.34f, 0.26f, 0.08f);
                preset.cloudColor = new Color(0.4f, 0.4f, 0.5f);
                break;

            case 1: // Golden Hour
                preset.grassTint = new Color(0.6f, 0.78f, 0.36f);
                preset.leafTint = new Color(0.6f, 0.78f, 0.36f);
                preset.canopyTint = new Color(0.6f, 0.78f, 0.36f);
                preset.terrainBaseColor = new Color(0.6f, 0.78f, 0.36f);
                preset.trunkBaseColor = new Color(0.68f, 0.53f, 0.16f);
                preset.cloudColor = new Color(1f, 0.85f, 0.6f);
                break;

            case 2: // Day
                preset.grassTint = new Color(0.52f, 0.87f, 0.38f);
                preset.leafTint = new Color(0.52f, 0.87f, 0.38f);
                preset.canopyTint = new Color(0.52f, 0.87f, 0.38f);
                preset.terrainBaseColor = new Color(0.52f, 0.87f, 0.38f);
                preset.trunkBaseColor = new Color(0.68f, 0.53f, 0.16f);
                preset.cloudColor = Color.white;
                break;

            case 3: // Twilight
                preset.grassTint = new Color(0.46f, 0.68f, 0.31f);
                preset.leafTint = new Color(0.46f, 0.68f, 0.31f);
                preset.canopyTint = new Color(0.46f, 0.68f, 0.31f);
                preset.terrainBaseColor = new Color(0.46f, 0.68f, 0.31f);
                preset.trunkBaseColor = new Color(0.54f, 0.42f, 0.13f);
                preset.cloudColor = new Color(0.9f, 0.6f, 0.5f);
                break;
        }

#if UNITY_EDITOR
        EditorUtility.SetDirty(presetController.presetsAsset);
        AssetDatabase.SaveAssets();
        Debug.Log($"✅ Reset {preset.presetName} to default values");
#endif
    }
}