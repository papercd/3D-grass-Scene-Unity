using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Controls ambient sound playback with smooth crossfading between presets
/// Works in conjunction with MaterialPresetController
/// </summary>
public class AmbientSoundController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The preset asset containing audio clips for each time of day")]
    public MaterialColorPresetsAsset presetsAsset;

    [Tooltip("Reference to day-night cycle for time tracking")]
    public PresetBasedDayNightCycle dayNightCycle;

    [Header("Audio Settings")]
    [Range(0f, 1f)]
    [Tooltip("Master volume multiplier for all ambient sounds")]
    public float masterVolume = 1f;

    [Range(0.1f, 10f)]
    [Tooltip("Crossfade duration in seconds")]
    public float crossfadeDuration = 2f;

    [Header("Audio Sources (Auto-Created)")]
    [Tooltip("These are created automatically - one per preset")]
    public AudioSource[] audioSources;

    [Header("Debug")]
    public bool verboseLogging = false;
    public bool showCurrentlyPlaying = true;

    // Track current state
    private int currentPreset1Index = -1;
    private int currentPreset2Index = -1;
    private float currentBlendFactor = 0f;

    // For smooth volume transitions
    private Dictionary<int, float> targetVolumes = new Dictionary<int, float>();
    private Dictionary<int, float> currentVolumes = new Dictionary<int, float>();

    void Start()
    {
        if (dayNightCycle == null)
        {
            dayNightCycle = FindObjectOfType<PresetBasedDayNightCycle>();
        }

        if (presetsAsset == null)
        {
            Debug.LogError("AmbientSoundController: No presetsAsset assigned!");
            return;
        }

        InitializeAudioSources();
    }

    void InitializeAudioSources()
    {
        if (presetsAsset.colorPresets == null || presetsAsset.colorPresets.Length == 0)
            return;

        // Create one AudioSource for each preset
        audioSources = new AudioSource[presetsAsset.colorPresets.Length];

        for (int i = 0; i < presetsAsset.colorPresets.Length; i++)
        {
            var preset = presetsAsset.colorPresets[i];

            // Create AudioSource
            GameObject sourceObj = new GameObject($"AmbientSound_{preset.presetName}");
            sourceObj.transform.SetParent(transform);

            AudioSource source = sourceObj.AddComponent<AudioSource>();
            source.clip = preset.ambientSound;
            source.loop = true;
            source.playOnAwake = false;
            source.volume = 0f; // Start silent
            source.spatialBlend = 0f; // 2D sound

            audioSources[i] = source;

            // Initialize volume tracking
            targetVolumes[i] = 0f;
            currentVolumes[i] = 0f;

            if (preset.ambientSound != null)
            {
                Debug.Log($"🔊 Created AudioSource for [{i}] {preset.presetName}: {preset.ambientSound.name}");
            }
            else
            {
                Debug.LogWarning($"⚠️ No audio clip assigned for preset [{i}] {preset.presetName}");
            }
        }

        // Start all sources playing (but at 0 volume)
        foreach (var source in audioSources)
        {
            if (source != null && source.clip != null)
            {
                source.Play();
            }
        }

        Debug.Log($"✅ Initialized {audioSources.Length} ambient sound sources");
    }

    void Update()
    {
        if (dayNightCycle == null || presetsAsset == null || audioSources == null)
            return;

        float currentTime = dayNightCycle.currentTime;

        // Find which two presets we're blending between
        int preset1Index, preset2Index;
        float blendFactor;

        FindPresetsForTime(currentTime, out preset1Index, out preset2Index, out blendFactor);

        // Update target volumes
        UpdateTargetVolumes(preset1Index, preset2Index, blendFactor);

        // Smoothly transition current volumes toward target volumes
        UpdateCurrentVolumes();

        // Apply volumes to audio sources
        ApplyVolumesToSources();

        // Debug display
        if (showCurrentlyPlaying && (preset1Index != currentPreset1Index || preset2Index != currentPreset2Index))
        {
            string preset1Name = presetsAsset.colorPresets[preset1Index].presetName;
            string preset2Name = presetsAsset.colorPresets[preset2Index].presetName;
            Debug.Log($"🎵 Audio Blend: {preset1Name} ({(1f - blendFactor) * 100f:F0}%) → {preset2Name} ({blendFactor * 100f:F0}%)");
        }

        currentPreset1Index = preset1Index;
        currentPreset2Index = preset2Index;
        currentBlendFactor = blendFactor;
    }

    void FindPresetsForTime(float currentTime, out int preset1, out int preset2, out float blend)
    {
        preset1 = 0;
        preset2 = 1;
        blend = 0f;

        if (presetsAsset.colorPresets == null || presetsAsset.colorPresets.Length < 2)
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

    void UpdateTargetVolumes(int preset1Index, int preset2Index, float blendFactor)
    {
        // Set all targets to 0
        for (int i = 0; i < audioSources.Length; i++)
        {
            targetVolumes[i] = 0f;
        }

        // Set target volumes for the two active presets
        if (preset1Index >= 0 && preset1Index < presetsAsset.colorPresets.Length)
        {
            float vol1 = presetsAsset.colorPresets[preset1Index].ambientVolume * (1f - blendFactor) * masterVolume;
            targetVolumes[preset1Index] = vol1;
        }

        if (preset2Index >= 0 && preset2Index < presetsAsset.colorPresets.Length)
        {
            float vol2 = presetsAsset.colorPresets[preset2Index].ambientVolume * blendFactor * masterVolume;
            targetVolumes[preset2Index] = vol2;
        }
    }

    void UpdateCurrentVolumes()
    {
        float transitionSpeed = 1f / crossfadeDuration;

        for (int i = 0; i < audioSources.Length; i++)
        {
            if (!currentVolumes.ContainsKey(i))
                currentVolumes[i] = 0f;

            float target = targetVolumes.ContainsKey(i) ? targetVolumes[i] : 0f;
            float current = currentVolumes[i];

            // Smooth transition
            current = Mathf.MoveTowards(current, target, transitionSpeed * Time.deltaTime);
            currentVolumes[i] = current;
        }
    }

    void ApplyVolumesToSources()
    {
        for (int i = 0; i < audioSources.Length; i++)
        {
            if (audioSources[i] != null)
            {
                float volume = currentVolumes.ContainsKey(i) ? currentVolumes[i] : 0f;
                audioSources[i].volume = volume;

                if (verboseLogging && volume > 0.01f)
                {
                    Debug.Log($"🔊 [{i}] {presetsAsset.colorPresets[i].presetName}: Volume = {volume:F3}");
                }
            }
        }
    }

    [ContextMenu("List Current Volumes")]
    public void ListCurrentVolumes()
    {
        Debug.Log("=== CURRENT AMBIENT SOUND VOLUMES ===");
        for (int i = 0; i < audioSources.Length; i++)
        {
            if (audioSources[i] != null)
            {
                string clipName = audioSources[i].clip != null ? audioSources[i].clip.name : "No Clip";
                Debug.Log($"[{i}] {presetsAsset.colorPresets[i].presetName}: {clipName} - Volume: {audioSources[i].volume:F3}");
            }
        }
    }

    [ContextMenu("Restart All Audio")]
    public void RestartAllAudio()
    {
        foreach (var source in audioSources)
        {
            if (source != null && source.clip != null)
            {
                source.Stop();
                source.Play();
            }
        }
        Debug.Log("🔄 Restarted all ambient sounds");
    }
}