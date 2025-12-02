using UnityEngine;

/// <summary>
/// ScriptableObject that stores all color/sound presets for different times of day
/// Can be edited and saved during Play Mode!
/// NOW INCLUDES: God rays, cloud threshold, and AMBIENT SOUNDS
/// </summary>
[CreateAssetMenu(fileName = "MaterialColorPresets", menuName = "Environment/Material Color Presets")]
public class MaterialColorPresetsAsset : ScriptableObject
{
    [System.Serializable]
    public class ColorPreset
    {
        [Header("Preset Info")]
        public string presetName = "Untitled";
        
        [Range(0f, 24f)]
        [Tooltip("Time of day in 24-hour format (0 = midnight, 12 = noon)")]
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
        
        [Header("Ambient Sounds")]
        [Tooltip("Primary ambient sound for this time of day (e.g., crickets, birds, wind)")]
        public AudioClip ambientSound;
        
        [Range(0f, 1f)]
        [Tooltip("Volume for the ambient sound")]
        public float ambientVolume = 0.7f;
    }
    
    [Header("Time-Based Presets")]
    [Tooltip("Must be in chronological order by timeOfDay")]
    public ColorPreset[] colorPresets = new ColorPreset[]
    {
        new ColorPreset 
        { 
            presetName = "Night", 
            timeOfDay = 0f,
            grassTint = new Color(0.2f, 0.35f, 0.15f),
            leafTint = new Color(0.2f, 0.35f, 0.15f),
            canopyTint = new Color(0.2f, 0.35f, 0.15f),
            terrainBaseColor = new Color(0.2f, 0.35f, 0.15f),
            trunkBaseColor = new Color(0.3f, 0.25f, 0.1f),
            cloudColor = new Color(0.3f, 0.3f, 0.4f),
            godRayColor = new Color(0.6f, 0.7f, 0.9f),
            godRayIntensity = 0.3f,
            cloudThreshold = 0.4f,
            ambientVolume = 0.8f
        },
        new ColorPreset 
        { 
            presetName = "Golden Hour", 
            timeOfDay = 6f,
            grassTint = new Color(0.7f, 0.6f, 0.3f),
            leafTint = new Color(0.7f, 0.6f, 0.3f),
            canopyTint = new Color(0.7f, 0.6f, 0.3f),
            terrainBaseColor = new Color(0.7f, 0.6f, 0.3f),
            trunkBaseColor = new Color(0.68f, 0.53f, 0.16f),
            cloudColor = new Color(1f, 0.7f, 0.4f),
            godRayColor = new Color(1f, 0.8f, 0.5f),
            godRayIntensity = 2.5f,
            cloudThreshold = 0.35f,
            ambientVolume = 0.7f
        },
        new ColorPreset 
        { 
            presetName = "Day", 
            timeOfDay = 12f,
            grassTint = new Color(0.52f, 0.87f, 0.38f),
            leafTint = new Color(0.52f, 0.87f, 0.38f),
            canopyTint = new Color(0.52f, 0.87f, 0.38f),
            terrainBaseColor = new Color(0.52f, 0.87f, 0.38f),
            trunkBaseColor = new Color(0.68f, 0.53f, 0.16f),
            cloudColor = Color.white,
            godRayColor = new Color(1f, 0.95f, 0.85f),
            godRayIntensity = 1.5f,
            cloudThreshold = 0.55f,
            ambientVolume = 0.6f
        },
        new ColorPreset 
        { 
            presetName = "Twilight", 
            timeOfDay = 18f,
            grassTint = new Color(0.6f, 0.5f, 0.4f),
            leafTint = new Color(0.6f, 0.5f, 0.4f),
            canopyTint = new Color(0.6f, 0.5f, 0.4f),
            terrainBaseColor = new Color(0.6f, 0.5f, 0.4f),
            trunkBaseColor = new Color(0.5f, 0.4f, 0.3f),
            cloudColor = new Color(0.9f, 0.5f, 0.3f),
            godRayColor = new Color(1f, 0.6f, 0.3f),
            godRayIntensity = 3.0f,
            cloudThreshold = 0.3f,
            ambientVolume = 0.75f
        }
    };
    
    [Header("Audio Transition")]
    [Range(0.1f, 10f)]
    [Tooltip("How long audio crossfades take in seconds")]
    public float audioTransitionDuration = 2f;
}