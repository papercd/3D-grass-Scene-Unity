using UnityEngine;
using System;

/// <summary>
/// Core controller for the Relic - the central object players must defend.
/// The Relic's HP influences the environment (grass density, light intensity).
/// </summary>
public class RelicController : MonoBehaviour
{
    [Header("Relic Stats")]
    [SerializeField] private float maxHP = 1000f;
    [SerializeField] private float currentHP;

    [Header("Environment Influence")]
    [SerializeField] private float healthyEnvironmentThreshold = 0.7f; // 70%+
    [SerializeField] private float criticalEnvironmentThreshold = 0.3f; // 30% or less

    [Header("Visual Effects")]
    [SerializeField] private Light relicLight;
    [SerializeField] private MeshRenderer relicMeshRenderer; // NEW: For material control
    [SerializeField] private ParticleSystem healingParticles;
    [SerializeField] private ParticleSystem damageParticles;
    [SerializeField] private float maxLightIntensity = 5f;
    [SerializeField] private float minLightIntensity = 1f;

    private Material relicMaterial; // Material instance

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioClip criticalWarningSound;
    [SerializeField] private AudioClip destructionSound;

    // Events for other systems to subscribe to
    public event Action<float> OnDamaged; // passes damage amount
    public event Action<float, float> OnHealthChanged; // current HP, max HP
    public event Action<float> OnHealthPercentChanged; // 0-1 normalized
    public event Action OnCriticalHealth; // triggered at 30%
    public event Action OnDestroyed;

    // Properties
    public float MaxHP => maxHP;
    public float CurrentHP => currentHP;
    public float HealthPercent => currentHP / maxHP;
    public bool IsAlive => currentHP > 0;
    public bool IsCritical => HealthPercent <= criticalEnvironmentThreshold;

    private bool hasTriggeredCriticalWarning = false;

    void Awake()
    {
        currentHP = maxHP;

        // Get components if not assigned
        if (relicLight == null)
            relicLight = GetComponentInChildren<Light>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Get material instance for color control
        if (relicMeshRenderer == null)
            relicMeshRenderer = GetComponentInChildren<MeshRenderer>();

        if (relicMeshRenderer != null)
        {
            relicMaterial = relicMeshRenderer.material; // Creates instance
            Debug.Log($"🎨 Relic material instance created: {relicMaterial.name}");
        }
    }

    void Start()
    {
        UpdateVisuals();
        Debug.Log($"🌟 Relic initialized with {maxHP} HP");
    }

    /// <summary>
    /// Apply damage to the Relic
    /// </summary>
    public void TakeDamage(float damageAmount)
    {
        if (!IsAlive) return;

        currentHP = Mathf.Max(0, currentHP - damageAmount);

        Debug.Log($"⚠️ Relic took {damageAmount} damage! HP: {currentHP}/{maxHP} ({HealthPercent:P0})");

        // Trigger events
        OnDamaged?.Invoke(damageAmount);
        OnHealthChanged?.Invoke(currentHP, maxHP);
        OnHealthPercentChanged?.Invoke(HealthPercent);

        // Visual & audio feedback
        UpdateVisuals();
        PlayDamageEffects();

        // Check for critical health warning
        if (IsCritical && !hasTriggeredCriticalWarning)
        {
            hasTriggeredCriticalWarning = true;
            OnCriticalHealth?.Invoke();
            PlayCriticalWarning();
            Debug.LogWarning("🚨 RELIC CRITICAL! HP below 30%!");
        }

        // Check for death
        if (currentHP <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Heal the Relic (for potential power-ups or spells)
    /// </summary>
    public void Heal(float healAmount)
    {
        if (!IsAlive) return;

        float previousHP = currentHP;
        currentHP = Mathf.Min(maxHP, currentHP + healAmount);

        Debug.Log($"💚 Relic healed {healAmount}! HP: {currentHP}/{maxHP}");

        // Trigger events
        OnHealthChanged?.Invoke(currentHP, maxHP);
        OnHealthPercentChanged?.Invoke(HealthPercent);

        // Visual feedback
        UpdateVisuals();
        if (healingParticles != null)
            healingParticles.Play();

        // Reset critical warning if healed above threshold
        if (previousHP < criticalEnvironmentThreshold * maxHP && currentHP >= criticalEnvironmentThreshold * maxHP)
        {
            hasTriggeredCriticalWarning = false;
        }
    }

    /// <summary>
    /// Update visual effects based on current health
    /// </summary>
    void UpdateVisuals()
    {
        // Update Point Light
        if (relicLight != null)
        {
            // Light intensity based on HP
            relicLight.intensity = Mathf.Lerp(minLightIntensity, maxLightIntensity, HealthPercent);

            // Color changes based on health
            if (IsCritical)
            {
                // Purple-blue tint when critical (warning color that still fits blue theme)
                relicLight.color = Color.Lerp(
                    new Color(0.8f, 0.3f, 1f), // Purple (very low)
                    new Color(0.6f, 0.5f, 0.9f), // Purple-blue (critical)
                    (HealthPercent / criticalEnvironmentThreshold)
                );
            }
            else
            {
                // Healthy cyan-blue glow
                relicLight.color = Color.Lerp(
                    new Color(0.5f, 0.75f, 1f), // Light cyan (moderate HP)
                    new Color(0.6f, 0.85f, 1f), // Bright cyan-white (full HP)
                    (HealthPercent - criticalEnvironmentThreshold) / (1f - criticalEnvironmentThreshold)
                );
            }
        }

        // Update Material Colors
        if (relicMaterial != null)
        {
            // Emission intensity based on HP (dims when damaged)
            float emissionIntensity = Mathf.Lerp(2f, 3f, HealthPercent);
            relicMaterial.SetFloat("_EmissionIntensity", emissionIntensity);

            if (IsCritical)
            {
                // Critical: Purple-blue warning (transitions from blue to purple as HP drops)
                Color criticalBase = Color.Lerp(
                    new Color(0.8f, 0.3f, 1f), // Purple (very low HP)
                    new Color(0.6f, 0.4f, 0.8f), // Purple-blue (critical but not dead)
                    HealthPercent / criticalEnvironmentThreshold
                );

                relicMaterial.SetColor("_BaseColor", criticalBase);
                relicMaterial.SetColor("_EmissionColor", criticalBase * 2.5f); // HDR purple/magenta glow (balanced)
                relicMaterial.SetColor("_RimColor", criticalBase * 1.5f);

                // Faster pulse when critical
                relicMaterial.SetFloat("_PulseSpeed", 3f);
                relicMaterial.SetFloat("_PulseAmount", 0.5f);
            }
            else
            {
                // Healthy: Crystal blue glow
                float healthFactor = (HealthPercent - criticalEnvironmentThreshold) / (1f - criticalEnvironmentThreshold);

                Color healthyBase = Color.Lerp(
                    new Color(0.5f, 0.6f, 0.9f), // Slightly darker blue (moderate HP)
                    new Color(0.4f, 0.7f, 1f), // Bright crystal blue (full HP)
                    healthFactor
                );

                Color healthyEmission = Color.Lerp(
                    new Color(0.0f, 0.0f, 0.0f), // Moderate blue glow
                    new Color(0.0f, 0.0f, 0.0f), // Bright crystal blue glow (balanced!)
                    healthFactor
                );

                relicMaterial.SetColor("_BaseColor", healthyBase);
                relicMaterial.SetColor("_EmissionColor", healthyEmission);
                relicMaterial.SetColor("_RimColor", new Color(0.5f, 0.8f, 1f)); // Cyan rim

                // Normal pulse when healthy (more visible now)
                relicMaterial.SetFloat("_PulseSpeed", 2f);
                relicMaterial.SetFloat("_PulseAmount", 0.6f);
            }
        }

        // TODO: Trigger environment controller to update grass density
    }

    void PlayDamageEffects()
    {
        // Visual
        if (damageParticles != null)
            damageParticles.Play();

        // Audio
        if (audioSource != null && damageSound != null)
            audioSource.PlayOneShot(damageSound);

        // Screen shake (if you have a camera shake system)
        // CameraShake.Instance?.Shake(0.2f, 0.3f);
    }

    void PlayCriticalWarning()
    {
        if (audioSource != null && criticalWarningSound != null)
            audioSource.PlayOneShot(criticalWarningSound);

        // TODO: Screen flash effect
        // TODO: UI warning popup
    }

    void Die()
    {
        Debug.LogError("💀 RELIC DESTROYED! GAME OVER!");

        OnDestroyed?.Invoke();

        // Visual effects
        if (relicLight != null)
            relicLight.enabled = false;

        // Fade out material
        if (relicMaterial != null)
        {
            relicMaterial.SetFloat("_EmissionIntensity", 0f);
            relicMaterial.SetColor("_BaseColor", Color.black);
        }

        // Audio
        if (audioSource != null && destructionSound != null)
            audioSource.PlayOneShot(destructionSound);

        // TODO: Death particles/explosion
        // TODO: Trigger Game Over screen
    }

    void OnDestroy()
    {
        // Clean up material instance to prevent memory leak
        if (relicMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(relicMaterial);
            else
                DestroyImmediate(relicMaterial);
        }
    }

    /// <summary>
    /// Get the environment influence value (0-1) for external systems
    /// Used by grass spawners, fog density, etc.
    /// </summary>
    public float GetEnvironmentInfluence()
    {
        return HealthPercent;
    }

    // Editor Debug Methods
    [ContextMenu("Take 100 Damage")]
    void Debug_TakeDamage()
    {
        TakeDamage(100f);
    }

    [ContextMenu("Heal 200 HP")]
    void Debug_Heal()
    {
        Heal(200f);
    }

    [ContextMenu("Set Critical HP")]
    void Debug_SetCritical()
    {
        currentHP = maxHP * 0.25f; // 25% HP
        UpdateVisuals();
        OnHealthChanged?.Invoke(currentHP, maxHP);
        OnHealthPercentChanged?.Invoke(HealthPercent);
    }

    void OnDrawGizmos()
    {
        // Visualize environment influence radius
        Gizmos.color = IsAlive ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, 10f); // Environment radius

        // Health indicator
        Gizmos.color = Color.Lerp(Color.red, Color.green, HealthPercent);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.5f);
    }
}