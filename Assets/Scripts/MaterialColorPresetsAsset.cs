using UnityEngine;

/// <summary>
/// ScriptableObject that holds all color presets - can be saved even in Play Mode!
/// NOW INCLUDES GOD RAY SETTINGS AND CLOUD THRESHOLD
/// </summary>
[CreateAssetMenu(fileName = "MaterialColorPresets", menuName = "Day/Night/Material Color Presets")]
public class MaterialColorPresetsAsset : ScriptableObject
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
        
        [Header("Cloud Settings (NEW)")]
        [Range(0f, 1f)]
        [Tooltip("Cloud coverage - higher = more clouds")]
        public float cloudThreshold = 0.5f;
    }
    
    public MaterialColorPreset[] colorPresets = new MaterialColorPreset[]
    {
        new MaterialColorPreset { 
            presetName = "Night", 
            timeOfDay = 0f,
            grassTint = new Color(0.22f, 0.38f, 0.18f),
            leafTint = new Color(0.22f, 0.38f, 0.18f),
            canopyTint = new Color(0.22f, 0.38f, 0.18f),
            terrainBaseColor = new Color(0.22f, 0.38f, 0.18f),
            trunkBaseColor = new Color(0.34f, 0.26f, 0.08f),
            cloudColor = new Color(0.4f, 0.4f, 0.5f),
            godRayColor = new Color(0.3f, 0.35f, 0.5f),
            godRayIntensity = 0.2f,
            cloudThreshold = 0.6f  // More clouds at night (darker, moodier)
        },
        new MaterialColorPreset { 
            presetName = "Golden Hour", 
            timeOfDay = 6f,
            grassTint = new Color(0.6f, 0.78f, 0.36f),
            leafTint = new Color(0.6f, 0.78f, 0.36f),
            canopyTint = new Color(0.6f, 0.78f, 0.36f),
            terrainBaseColor = new Color(0.6f, 0.78f, 0.36f),
            trunkBaseColor = new Color(0.68f, 0.53f, 0.16f),
            cloudColor = new Color(1f, 0.85f, 0.6f),
            godRayColor = new Color(1f, 0.75f, 0.5f),
            godRayIntensity = 1.5f,
            cloudThreshold = 0.45f  // Moderate clouds for dramatic sunrise
        },
        new MaterialColorPreset { 
            presetName = "Day", 
            timeOfDay = 12f,
            grassTint = new Color(0.52f, 0.87f, 0.38f),
            leafTint = new Color(0.52f, 0.87f, 0.38f),
            canopyTint = new Color(0.52f, 0.87f, 0.38f),
            terrainBaseColor = new Color(0.52f, 0.87f, 0.38f),
            trunkBaseColor = new Color(0.68f, 0.53f, 0.16f),
            cloudColor = Color.white,
            godRayColor = new Color(1f, 0.98f, 0.9f),
            godRayIntensity = 1.0f,
            cloudThreshold = 0.55f  // Clear day, fewer clouds
        },
        new MaterialColorPreset { 
            presetName = "Twilight", 
            timeOfDay = 18f,
            grassTint = new Color(0.46f, 0.68f, 0.31f),
            leafTint = new Color(0.46f, 0.68f, 0.31f),
            canopyTint = new Color(0.46f, 0.68f, 0.31f),
            terrainBaseColor = new Color(0.46f, 0.68f, 0.31f),
            trunkBaseColor = new Color(0.54f, 0.42f, 0.13f),
            cloudColor = new Color(0.9f, 0.6f, 0.5f),
            godRayColor = new Color(1f, 0.6f, 0.4f),
            godRayIntensity = 1.2f,
            cloudThreshold = 0.4f  // More clouds for dramatic sunset
        }
    };
}