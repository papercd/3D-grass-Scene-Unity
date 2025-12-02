using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Controls background music playback with time-based scheduling and smooth fading
/// OPTIMIZED: Preloads and prewarms all audio to prevent playback hitches
/// </summary>
public class BackgroundMusicController : MonoBehaviour
{
    [System.Serializable]
    public class MusicTrack
    {
        [Header("Track Info")]
        public string trackName = "Untitled";

        [Tooltip("The music clip to play")]
        public AudioClip clip;

        [Header("Playback Schedule")]
        [Range(0f, 24f)]
        [Tooltip("Start playing at this time (24-hour format)")]
        public float startTime = 6f;

        [Range(0f, 24f)]
        [Tooltip("Stop playing at this time (24-hour format)")]
        public float endTime = 18f;

        [Header("Volume")]
        [Range(0f, 1f)]
        [Tooltip("Target volume when fully faded in")]
        public float targetVolume = 0.5f;

        [Header("Fade Settings")]
        [Range(0.1f, 30f)]
        [Tooltip("How long to fade in at start (seconds)")]
        public float fadeInDuration = 5f;

        [Range(0.1f, 30f)]
        [Tooltip("How long to fade out at end (seconds)")]
        public float fadeOutDuration = 5f;

        [Tooltip("Loop this track")]
        public bool loop = true;

        // Runtime state
        [HideInInspector] public AudioSource audioSource;
        [HideInInspector] public bool isPlaying;
        [HideInInspector] public float currentVolume;
        [HideInInspector] public bool isPreloaded;
    }

    [Header("References")]
    [Tooltip("Reference to day-night cycle for time tracking")]
    public PresetBasedDayNightCycle dayNightCycle;

    [Header("Music Tracks")]
    [Tooltip("Add your music tracks here - they'll play at their scheduled times")]
    public List<MusicTrack> tracks = new List<MusicTrack>();

    [Header("Global Settings")]
    [Range(0f, 1f)]
    [Tooltip("Master volume multiplier for all music")]
    public float masterVolume = 0.8f;

    [Tooltip("Use smooth fade curves (more natural sounding)")]
    public bool useSmoothFading = true;

    [Header("Optimization")]
    [Tooltip("Preload all music on startup to prevent hitches")]
    public bool preloadOnStart = true;

    [Tooltip("Start all tracks playing silently (eliminates first-play hitch)")]
    public bool prewarmAudioSources = true;

    [Header("Debug")]
    public bool verboseLogging = false;
    public bool showPlaybackStatus = true;
    public bool showPreloadProgress = true;

    private float previousTime = -1f;
    private bool isInitialized = false;

    void Start()
    {
        if (dayNightCycle == null)
        {
            dayNightCycle = FindObjectOfType<PresetBasedDayNightCycle>();
        }

        StartCoroutine(InitializeTracksAsync());
    }

    IEnumerator InitializeTracksAsync()
    {
        if (showPreloadProgress)
            Debug.Log("🎵 Initializing background music system...");

        foreach (var track in tracks)
        {
            if (track.clip == null)
            {
                Debug.LogWarning($"⚠️ Track '{track.trackName}' has no audio clip assigned!");
                continue;
            }

            // Create AudioSource for this track
            GameObject sourceObj = new GameObject($"Music_{track.trackName}");
            sourceObj.transform.SetParent(transform);

            AudioSource source = sourceObj.AddComponent<AudioSource>();
            source.clip = track.clip;
            source.loop = track.loop;
            source.playOnAwake = false;
            source.volume = 0f;
            source.spatialBlend = 0f; // 2D sound

            // CRITICAL: Set load type to preload in memory
            // (User should also set this in import settings)
            source.clip.LoadAudioData();

            track.audioSource = source;
            track.currentVolume = 0f;
            track.isPlaying = false;
            track.isPreloaded = false;

            if (showPreloadProgress)
                Debug.Log($"🎵 Created music track: {track.trackName} ({track.startTime:F1}h - {track.endTime:F1}h)");

            // Small yield to prevent single-frame hitch
            yield return null;
        }

        // Preload audio data
        if (preloadOnStart)
        {
            yield return StartCoroutine(PreloadAllAudio());
        }

        // Prewarm audio sources (play silently to initialize)
        if (prewarmAudioSources)
        {
            yield return StartCoroutine(PrewarmAudioSources());
        }

        isInitialized = true;

        if (showPreloadProgress)
            Debug.Log("✅ Background music system ready!");
    }

    IEnumerator PreloadAllAudio()
    {
        if (showPreloadProgress)
            Debug.Log("⏳ Preloading audio data...");

        foreach (var track in tracks)
        {
            if (track.clip != null && !track.isPreloaded)
            {
                // Force load audio data into memory
                if (track.clip.loadState != AudioDataLoadState.Loaded)
                {
                    track.clip.LoadAudioData();

                    // Wait for load to complete
                    while (track.clip.loadState == AudioDataLoadState.Loading)
                    {
                        yield return null;
                    }

                    if (track.clip.loadState == AudioDataLoadState.Loaded)
                    {
                        track.isPreloaded = true;

                        if (showPreloadProgress)
                            Debug.Log($"  ✅ Preloaded: {track.trackName} ({track.clip.length:F1}s)");
                    }
                    else
                    {
                        Debug.LogWarning($"  ⚠️ Failed to preload: {track.trackName}");
                    }
                }
                else
                {
                    track.isPreloaded = true;

                    if (showPreloadProgress)
                        Debug.Log($"  ✅ Already loaded: {track.trackName}");
                }
            }

            yield return null;
        }
    }

    IEnumerator PrewarmAudioSources()
    {
        if (showPreloadProgress)
            Debug.Log("🔥 Prewarming audio sources...");

        // Play all sources at 0 volume briefly to initialize audio pipeline
        foreach (var track in tracks)
        {
            if (track.audioSource != null && track.audioSource.clip != null)
            {
                track.audioSource.volume = 0f;
                track.audioSource.Play();

                if (verboseLogging)
                    Debug.Log($"  🔥 Prewarmed: {track.trackName}");
            }
        }

        // Let them play for a few frames
        yield return new WaitForSeconds(0.1f);

        // Stop all (they'll be restarted properly when needed)
        foreach (var track in tracks)
        {
            if (track.audioSource != null)
            {
                track.audioSource.Stop();
            }
        }

        if (showPreloadProgress)
            Debug.Log("✅ Audio sources prewarmed!");
    }

    void Update()
    {
        if (!isInitialized || dayNightCycle == null) return;

        float currentTime = dayNightCycle.currentTime;

        // Update each track
        foreach (var track in tracks)
        {
            if (track.audioSource == null) continue;

            UpdateTrack(track, currentTime);
        }

        previousTime = currentTime;
    }

    void UpdateTrack(MusicTrack track, float currentTime)
    {
        bool shouldBePlaying = IsTimeInRange(currentTime, track.startTime, track.endTime);

        // Check for time wrap-around (midnight crossing)
        bool justWrapped = previousTime > 23f && currentTime < 1f;

        // Start playing if entering time window
        if (shouldBePlaying && !track.isPlaying)
        {
            StartTrack(track);
        }
        // Stop playing if leaving time window
        else if (!shouldBePlaying && track.isPlaying)
        {
            StopTrack(track);
        }

        // Update volume (fade in/out)
        // Always update volume — fade out happens even after isPlaying = false
        UpdateTrackVolume(track, currentTime);


        // Restart track if time wrapped and we're in range
        if (justWrapped && shouldBePlaying && track.loop)
        {
            if (verboseLogging)
                Debug.Log($"🔄 Day reset - ensuring {track.trackName} is playing");

            if (!track.audioSource.isPlaying)
                track.audioSource.Play();
        }
    }

    void StartTrack(MusicTrack track)
    {
        // Audio should start instantly without hitch (already preloaded)
        track.audioSource.Play();
        track.isPlaying = true;
        track.currentVolume = 0f;
        track.audioSource.volume = 0f;

        if (showPlaybackStatus)
            Debug.Log($"▶️ Started: {track.trackName}");
    }

    void StopTrack(MusicTrack track)
    {
        // Fade will handle the actual stop
        track.isPlaying = false;

        if (showPlaybackStatus)
            Debug.Log($"⏸️ Stopping: {track.trackName}");
    }

    void UpdateTrackVolume(MusicTrack track, float currentTime)
    {
        float targetVol = CalculateTargetVolume(track, currentTime);

        // Determine fade speed based on direction (in or out)
        float fadeDuration = (targetVol > track.currentVolume)
            ? track.fadeInDuration
            : track.fadeOutDuration;

        float volumeChangeSpeed = 1f / fadeDuration;

        track.currentVolume = Mathf.MoveTowards(
            track.currentVolume,
            targetVol,
            volumeChangeSpeed * Time.deltaTime
        );

        track.audioSource.volume = track.currentVolume * masterVolume;

        if (track.currentVolume <= 0.001f && !track.isPlaying)
            track.audioSource.Stop();
    }

    float CalculateTargetVolume(MusicTrack track, float currentTime)
    {
        // If not playing anymore, volume should fade OUT toward 0
        if (!track.isPlaying)
            return 0f;


        float fadeInStart = track.startTime;
        float fadeInEnd = track.startTime + (track.fadeInDuration / 3600f); // Convert seconds to hours

        float fadeOutStart = track.endTime - (track.fadeOutDuration / 3600f);
        float fadeOutEnd = track.endTime;

        // Handle wrap-around (midnight crossing)
        bool wrapsAroundMidnight = track.endTime < track.startTime;

        if (wrapsAroundMidnight)
        {
            // Adjust times for wrap-around
            if (currentTime < track.startTime)
                currentTime += 24f;

            fadeOutStart = track.endTime + 24f - (track.fadeOutDuration / 3600f);
            fadeOutEnd = track.endTime + 24f;
        }

        // Fade in
        if (currentTime < fadeInEnd)
        {
            float fadeProgress = Mathf.InverseLerp(fadeInStart, fadeInEnd, currentTime);

            if (useSmoothFading)
                fadeProgress = Mathf.SmoothStep(0f, 1f, fadeProgress);

            return track.targetVolume * fadeProgress;
        }
        // Fade out
        else if (currentTime > fadeOutStart)
        {
            float fadeProgress = Mathf.InverseLerp(fadeOutEnd, fadeOutStart, currentTime);

            if (useSmoothFading)
                fadeProgress = Mathf.SmoothStep(0f, 1f, fadeProgress);

            return track.targetVolume * fadeProgress;
        }
        // Full volume
        else
        {
            return track.targetVolume;
        }
    }

    bool IsTimeInRange(float currentTime, float startTime, float endTime)
    {
        // Handle wrap-around (e.g., 22:00 - 06:00)
        if (endTime < startTime)
        {
            return currentTime >= startTime || currentTime <= endTime;
        }
        else
        {
            return currentTime >= startTime && currentTime <= endTime;
        }
    }

    // ========================================
    // CONTEXT MENU TOOLS
    // ========================================

    [ContextMenu("📊 Show All Track Status")]
    public void ShowAllTrackStatus()
    {
        Debug.Log("=== BACKGROUND MUSIC STATUS ===");

        float currentTime = dayNightCycle != null ? dayNightCycle.currentTime : 0f;
        Debug.Log($"Current Time: {currentTime:F2}h");
        Debug.Log($"Master Volume: {masterVolume:F2}");
        Debug.Log($"System Initialized: {isInitialized}");
        Debug.Log("");

        foreach (var track in tracks)
        {
            string status = track.isPlaying ? "▶️ PLAYING" : "⏸️ STOPPED";
            string inRange = IsTimeInRange(currentTime, track.startTime, track.endTime) ? "✅" : "❌";
            string preloaded = track.isPreloaded ? "✅ LOADED" : "⚠️ NOT LOADED";

            Debug.Log($"{status} {inRange} {preloaded} [{track.trackName}]");
            Debug.Log($"  Time Window: {track.startTime:F1}h - {track.endTime:F1}h");
            Debug.Log($"  Volume: {track.currentVolume:F3} / {track.targetVolume:F2}");
            Debug.Log($"  AudioSource Playing: {track.audioSource?.isPlaying}");
            if (track.clip != null)
                Debug.Log($"  Clip Load State: {track.clip.loadState}");
            Debug.Log("");
        }
    }

    [ContextMenu("🔄 Restart All Music")]
    public void RestartAllMusic()
    {
        foreach (var track in tracks)
        {
            if (track.audioSource != null)
            {
                track.audioSource.Stop();
                track.isPlaying = false;
                track.currentVolume = 0f;
            }
        }

        Debug.Log("🔄 Restarted all music tracks");
    }

    [ContextMenu("🔥 Force Preload Now")]
    public void ForcePreloadNow()
    {
        StartCoroutine(PreloadAllAudio());
    }

    [ContextMenu("▶️ Force Play All (For Testing)")]
    public void ForcePlayAll()
    {
        foreach (var track in tracks)
        {
            if (track.audioSource != null && !track.audioSource.isPlaying)
            {
                track.audioSource.Play();
                track.audioSource.volume = track.targetVolume * masterVolume;
                Debug.Log($"▶️ Force playing: {track.trackName}");
            }
        }
    }

    [ContextMenu("⏹️ Stop All Music")]
    public void StopAllMusic()
    {
        foreach (var track in tracks)
        {
            if (track.audioSource != null)
            {
                track.audioSource.Stop();
                track.isPlaying = false;
                track.currentVolume = 0f;
            }
        }

        Debug.Log("⏹️ Stopped all music");
    }
}