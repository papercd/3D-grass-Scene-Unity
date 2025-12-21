using UnityEngine;
using System;

/// <summary>
/// Handles enemy health, damage output, and movement speed.
/// Tracks death and notifies other systems when enemy is destroyed.
/// </summary>
public class EnemyStats : MonoBehaviour
{
    [Header("Enemy Stats")]
    [SerializeField] private float maxHP = 50f;
    [SerializeField] private float currentHP;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float attackCooldown = 1.5f; // Time between attacks

    [Header("Visual Feedback")]
    [SerializeField] private Renderer enemyRenderer;
    [SerializeField] private Color normalColor = new Color(0.2f, 0.1f, 0.3f); // Dark purple
    [SerializeField] private Color damageFlashColor = Color.red;
    [SerializeField] private float damageFlashDuration = 0.1f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioClip deathSound;

    // Properties
    public float MaxHP => maxHP;
    public float CurrentHP => currentHP;
    public float Damage => damage;
    public float MoveSpeed => moveSpeed;
    public float AttackCooldown => attackCooldown;
    public bool IsAlive => currentHP > 0;
    public float HealthPercent => currentHP / maxHP;

    // Events
    public event Action<float> OnDamaged; // passes damage amount
    public event Action OnDeath;

    private Material enemyMaterial;
    private bool isFlashing = false;
    private float lastAttackTime = 0f;

    void Awake()
    {
        currentHP = maxHP;

        // Get components
        if (enemyRenderer == null)
            enemyRenderer = GetComponentInChildren<Renderer>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Create material instance for color changes
        if (enemyRenderer != null)
        {
            enemyMaterial = enemyRenderer.material;
            enemyMaterial.color = normalColor;
        }
    }

    void Start()
    {
        Debug.Log($"👻 {gameObject.name} spawned with {maxHP} HP");
    }

    /// <summary>
    /// Apply damage to this enemy
    /// </summary>
    public void TakeDamage(float damageAmount)
    {
        if (!IsAlive) return;

        currentHP = Mathf.Max(0, currentHP - damageAmount);

        Debug.Log($"💥 {gameObject.name} took {damageAmount} damage! HP: {currentHP}/{maxHP}");

        // Visual feedback
        FlashDamageColor();

        // Audio feedback
        if (audioSource != null && damageSound != null)
            audioSource.PlayOneShot(damageSound);

        // Notify listeners
        OnDamaged?.Invoke(damageAmount);

        // Check death
        if (currentHP <= 0 && IsAlive)
        {
            Die();
        }
    }

    /// <summary>
    /// Check if enough time has passed since last attack
    /// </summary>
    public bool CanAttack()
    {
        return Time.time >= lastAttackTime + attackCooldown;
    }

    /// <summary>
    /// Mark that an attack was performed (resets cooldown)
    /// </summary>
    public void RegisterAttack()
    {
        lastAttackTime = Time.time;
    }

    void FlashDamageColor()
    {
        if (isFlashing || enemyMaterial == null) return;

        StartCoroutine(DamageFlashCoroutine());
    }

    System.Collections.IEnumerator DamageFlashCoroutine()
    {
        isFlashing = true;

        // Flash red
        enemyMaterial.color = damageFlashColor;

        yield return new WaitForSeconds(damageFlashDuration);

        // Return to normal
        if (enemyMaterial != null)
            enemyMaterial.color = normalColor;

        isFlashing = false;
    }

    void Die()
    {
        Debug.Log($"💀 {gameObject.name} defeated!");

        // Audio
        if (audioSource != null && deathSound != null)
            audioSource.PlayOneShot(deathSound);

        // Notify listeners
        OnDeath?.Invoke();

        // TODO: Death particles/effects
        // TODO: Drop loot/resources
        // TODO: Update enemy counter in game manager

        // Destroy after brief delay (to allow death sound to play)
        Destroy(gameObject, 0.2f);
    }

    void OnDestroy()
    {
        // Clean up material instance
        if (enemyMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(enemyMaterial);
            else
                DestroyImmediate(enemyMaterial);
        }
    }

    // Debug methods
    [ContextMenu("Take 25 Damage")]
    void Debug_TakeDamage()
    {
        TakeDamage(25f);
    }

    [ContextMenu("Kill Enemy")]
    void Debug_Kill()
    {
        TakeDamage(currentHP);
    }

    void OnDrawGizmosSelected()
    {
        // Visualize attack range/detection
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 2f); // Attack range

        // Health indicator
        Gizmos.color = Color.Lerp(Color.red, Color.green, HealthPercent);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.3f);
    }
}