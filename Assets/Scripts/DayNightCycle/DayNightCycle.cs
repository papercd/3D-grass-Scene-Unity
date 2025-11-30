using UnityEngine;

/// <summary>
/// Day-Night cycle using material presets for full artistic control
/// Artists define exact colors for each time of day, system blends between them
/// </summary>
public class PresetBasedDayNightCycle : MonoBehaviour
{
    [System.Serializable]
    public class TimeOfDayPreset
    {
        public string presetName;
        [Range(0f, 24f)]
        public float timeOfDay;
        
        [Header("Lighting")]
        public float sunAngle = 50f; // Vertical angle
        public float sunRotation = 0f; // Horizontal rotation
        public float lightIntensity = 1f;
        public Color lightColor = Color.white;
        public float shadowStrength = 1f;
        
        [Header("Ambient")]
        public Color ambientColor = new Color(0.5f, 0.5f, 0.5f);
        public float ambientIntensity = 1f;
        
        [Header("Fog")]
        public Color fogColor = Color.gray;
        public float fogDensity = 0.01f;
        
        [Header("Skybox")]
        public float skyboxExposure = 1f;
    }
    
    [Header("Time Settings")]
    [Range(0f, 24f)]
    public float currentTime = 12f;
    public float dayDurationInMinutes = 10f;
    public bool autoAdvanceTime = true;
    
    [Header("Time Presets")]
    public TimeOfDayPreset[] timePresets = new TimeOfDayPreset[4];
    
    [Header("Light Reference")]
    public Light directionalLight;
    
    [Header("Skybox")]
    public Material skyboxMaterial;
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    
    private int currentPresetIndex = 0;
    private int nextPresetIndex = 1;
    private float blendFactor = 0f;
    
    void Start()
    {
        if (directionalLight == null)
            directionalLight = RenderSettings.sun;
        
        SetupDefaultPresets();
        UpdateEnvironment();
    }
    
    void Update()
    {
        if (autoAdvanceTime)
        {
            float timeIncrement = (24f / (dayDurationInMinutes * 60f)) * Time.deltaTime;
            currentTime += timeIncrement;
            
            if (currentTime >= 24f)
                currentTime = 0f;
        }
        
        UpdateEnvironment();
    }
    
    void UpdateEnvironment()
    {
        FindCurrentPresets();
        CalculateBlendFactor();
        BlendPresets();
    }
    
    void FindCurrentPresets()
    {
        if (timePresets == null || timePresets.Length < 2)
        {
            currentPresetIndex = 0;
            nextPresetIndex = 0;
            return;
        }
        
        // Sort presets by time (if not already sorted)
        System.Array.Sort(timePresets, (a, b) => a.timeOfDay.CompareTo(b.timeOfDay));
        
        // Find which two presets we're between
        for (int i = 0; i < timePresets.Length; i++)
        {
            if (currentTime >= timePresets[i].timeOfDay)
            {
                currentPresetIndex = i;
                nextPresetIndex = (i + 1) % timePresets.Length;
            }
        }
    }
    
    void CalculateBlendFactor()
    {
        TimeOfDayPreset current = timePresets[currentPresetIndex];
        TimeOfDayPreset next = timePresets[nextPresetIndex];
        
        float startTime = current.timeOfDay;
        float endTime = next.timeOfDay;
        
        // Handle wrap around midnight
        if (endTime < startTime)
            endTime += 24f;
        
        float adjustedCurrentTime = currentTime;
        if (adjustedCurrentTime < startTime)
            adjustedCurrentTime += 24f;
        
        float duration = endTime - startTime;
        float elapsed = adjustedCurrentTime - startTime;
        
        blendFactor = Mathf.Clamp01(elapsed / duration);
    }
    
    void BlendPresets()
    {
        TimeOfDayPreset current = timePresets[currentPresetIndex];
        TimeOfDayPreset next = timePresets[nextPresetIndex];
        
        // Blend lighting
        if (directionalLight != null)
        {
            // Blend sun angle and rotation
            float sunAngle = Mathf.Lerp(current.sunAngle, next.sunAngle, blendFactor);
            float sunRotation = Mathf.Lerp(current.sunRotation, next.sunRotation, blendFactor);
            directionalLight.transform.rotation = Quaternion.Euler(sunAngle, sunRotation, 0f);
            
            // Blend intensity and color
            directionalLight.intensity = Mathf.Lerp(current.lightIntensity, next.lightIntensity, blendFactor);
            directionalLight.color = Color.Lerp(current.lightColor, next.lightColor, blendFactor);
            directionalLight.shadowStrength = Mathf.Lerp(current.shadowStrength, next.shadowStrength, blendFactor);
        }
        
        // Blend ambient
        RenderSettings.ambientLight = Color.Lerp(current.ambientColor, next.ambientColor, blendFactor);
        RenderSettings.ambientIntensity = Mathf.Lerp(current.ambientIntensity, next.ambientIntensity, blendFactor);
        
        // Blend fog
        RenderSettings.fog = true;
        RenderSettings.fogColor = Color.Lerp(current.fogColor, next.fogColor, blendFactor);
        RenderSettings.fogDensity = Mathf.Lerp(current.fogDensity, next.fogDensity, blendFactor);
        
        // Blend skybox
        if (skyboxMaterial != null)
        {
            float exposure = Mathf.Lerp(current.skyboxExposure, next.skyboxExposure, blendFactor);
            skyboxMaterial.SetFloat("_Exposure", exposure);
        }
    }
    
    void SetupDefaultPresets()
    {
        if (timePresets == null || timePresets.Length == 0)
        {
            timePresets = new TimeOfDayPreset[4];
        }
        
        // Night
        if (timePresets[0] == null)
        {
            timePresets[0] = new TimeOfDayPreset
            {
                presetName = "Night",
                timeOfDay = 0f,
                sunAngle = -20f,
                sunRotation = 0f,
                lightIntensity = 0.1f,
                lightColor = new Color(0.5f, 0.6f, 0.8f),
                shadowStrength = 0.3f,
                ambientColor = new Color(0.15f, 0.2f, 0.35f),
                ambientIntensity = 0.3f,
                fogColor = new Color(0.1f, 0.15f, 0.25f),
                fogDensity = 0.015f,
                skyboxExposure = 0.3f
            };
        }
        
        // Dawn / Golden Hour
        if (timePresets.Length > 1 && timePresets[1] == null)
        {
            timePresets[1] = new TimeOfDayPreset
            {
                presetName = "Dawn/Golden Hour",
                timeOfDay = 6f,
                sunAngle = 10f,
                sunRotation = 90f,
                lightIntensity = 0.9f,
                lightColor = new Color(1f, 0.7f, 0.5f),
                shadowStrength = 0.7f,
                ambientColor = new Color(0.6f, 0.5f, 0.6f),
                ambientIntensity = 0.6f,
                fogColor = new Color(0.7f, 0.6f, 0.7f),
                fogDensity = 0.02f,
                skyboxExposure = 0.8f
            };
        }
        
        // Day
        if (timePresets.Length > 2 && timePresets[2] == null)
        {
            timePresets[2] = new TimeOfDayPreset
            {
                presetName = "Day",
                timeOfDay = 12f,
                sunAngle = 60f,
                sunRotation = 180f,
                lightIntensity = 1.5f,
                lightColor = new Color(1f, 0.98f, 0.95f),
                shadowStrength = 1f,
                ambientColor = new Color(0.7f, 0.75f, 0.8f),
                ambientIntensity = 1f,
                fogColor = new Color(0.85f, 0.9f, 0.95f),
                fogDensity = 0.008f,
                skyboxExposure = 1.2f
            };
        }
        
        // Dusk / Twilight
        if (timePresets.Length > 3 && timePresets[3] == null)
        {
            timePresets[3] = new TimeOfDayPreset
            {
                presetName = "Dusk/Twilight",
                timeOfDay = 18f,
                sunAngle = 5f,
                sunRotation = 270f,
                lightIntensity = 0.8f,
                lightColor = new Color(1f, 0.6f, 0.4f),
                shadowStrength = 0.6f,
                ambientColor = new Color(0.65f, 0.45f, 0.5f),
                ambientIntensity = 0.5f,
                fogColor = new Color(0.8f, 0.6f, 0.5f),
                fogDensity = 0.02f,
                skyboxExposure = 0.7f
            };
        }
    }
    
    public TimeOfDayPreset GetCurrentPreset()
    {
        return timePresets[currentPresetIndex];
    }
    
    public TimeOfDayPreset GetNextPreset()
    {
        return timePresets[nextPresetIndex];
    }
    
    public float GetBlendFactor()
    {
        return blendFactor;
    }
    
    void OnGUI()
    {
        if (!showDebugInfo) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 350, 150));
        GUILayout.Box("Preset-Based Day-Night Cycle");
        GUILayout.Label($"Time: {GetTimeString()}");
        
        if (timePresets != null && timePresets.Length > currentPresetIndex && timePresets.Length > nextPresetIndex)
        {
            GUILayout.Label($"Blending: {timePresets[currentPresetIndex].presetName} → {timePresets[nextPresetIndex].presetName}");
            GUILayout.Label($"Blend Factor: {blendFactor:F2}");
        }
        
        if (directionalLight != null)
        {
            GUILayout.Label($"Sun Angle: {directionalLight.transform.eulerAngles.x:F1}°");
            GUILayout.Label($"Light Intensity: {directionalLight.intensity:F2}");
        }
        GUILayout.EndArea();
    }
    
    string GetTimeString()
    {
        int hours = Mathf.FloorToInt(currentTime);
        int minutes = Mathf.FloorToInt((currentTime - hours) * 60f);
        return $"{hours:00}:{minutes:00}";
    }
    
    public void SetTime(float hour)
    {
        currentTime = Mathf.Clamp(hour, 0f, 24f);
        UpdateEnvironment();
    }
}