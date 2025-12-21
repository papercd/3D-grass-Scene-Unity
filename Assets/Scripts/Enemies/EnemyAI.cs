using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// AI controller for enemies - navigates toward Relic, attacks on contact.
/// Can be distracted by player when they get close.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyStats))]
public class EnemyAI : MonoBehaviour
{
    [Header("Target References")]
    [SerializeField] private Transform relicTarget;
    [SerializeField] private Transform playerTarget;

    [Header("AI Behavior")]
    [SerializeField] private float playerDetectionRange = 8f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float stoppingDistance = 1.5f;

    [Header("Auto-Find Targets")]
    [SerializeField] private bool autoFindRelic = true;
    [SerializeField] private bool autoFindPlayer = true;

    private NavMeshAgent agent;
    private EnemyStats stats;
    private Transform currentTarget;
    private bool isAttacking = false;

    // Target types
    private enum TargetType { None, Relic, Player }
    private TargetType currentTargetType = TargetType.None;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        stats = GetComponent<EnemyStats>();
    }

    void Start()
    {
        // Auto-find targets if not assigned
        if (autoFindRelic && relicTarget == null)
        {
            GameObject relic = GameObject.FindGameObjectWithTag("Relic");
            if (relic != null)
            {
                relicTarget = relic.transform;
                Debug.Log($"🎯 {gameObject.name} found Relic target");
            }
            else
            {
                Debug.LogWarning($"⚠️ {gameObject.name} could not find Relic! Make sure Relic has 'Relic' tag.");
            }
        }

        if (autoFindPlayer && playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTarget = player.transform;
                Debug.Log($"🎯 {gameObject.name} found Player target");
            }
        }

        // Configure NavMesh agent
        if (agent != null && stats != null)
        {
            agent.speed = stats.MoveSpeed;
            agent.stoppingDistance = stoppingDistance;
        }

        // Start targeting the Relic
        SetTarget(relicTarget, TargetType.Relic);
    }

    void Update()
    {
        if (!stats.IsAlive) return;

        // Update targeting logic
        UpdateTargeting();

        // Move toward current target
        MoveToTarget();

        // Check for attack opportunities
        CheckAttack();
    }

    void UpdateTargeting()
    {
        // Priority 1: Player is nearby (distraction mechanic)
        if (playerTarget != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

            if (distanceToPlayer <= playerDetectionRange)
            {
                // Switch to player if we're targeting the Relic
                if (currentTargetType != TargetType.Player)
                {
                    SetTarget(playerTarget, TargetType.Player);
                    Debug.Log($"👀 {gameObject.name} noticed the Player!");
                }
            }
            else
            {
                // Player is out of range, go back to Relic
                if (currentTargetType == TargetType.Player)
                {
                    SetTarget(relicTarget, TargetType.Relic);
                    Debug.Log($"🎯 {gameObject.name} returning to Relic");
                }
            }
        }
    }

    void MoveToTarget()
    {
        if (currentTarget == null || agent == null) return;

        // Set NavMesh destination
        if (!isAttacking && agent.enabled)
        {
            agent.SetDestination(currentTarget.position);
        }
    }

    void CheckAttack()
    {
        if (currentTarget == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

        // In attack range
        if (distanceToTarget <= attackRange)
        {
            if (stats.CanAttack())
            {
                PerformAttack();
            }
        }
    }

    void PerformAttack()
    {
        stats.RegisterAttack();

        Debug.Log($"⚔️ {gameObject.name} attacks {currentTargetType}!");

        // Apply damage based on target type
        if (currentTargetType == TargetType.Relic)
        {
            RelicController relic = currentTarget.GetComponent<RelicController>();
            if (relic != null)
            {
                relic.TakeDamage(stats.Damage);
            }
        }
        else if (currentTargetType == TargetType.Player)
        {
            // TODO: Implement player health system
            Debug.Log($"💥 Player takes {stats.Damage} damage!");
            // PlayerHealth playerHealth = currentTarget.GetComponent<PlayerHealth>();
            // if (playerHealth != null)
            //     playerHealth.TakeDamage(stats.Damage);
        }

        // TODO: Play attack animation
        // TODO: Attack visual effects
    }

    void SetTarget(Transform newTarget, TargetType targetType)
    {
        currentTarget = newTarget;
        currentTargetType = targetType;

        if (agent != null && currentTarget != null)
        {
            agent.SetDestination(currentTarget.position);
        }
    }

    // Public method to set custom target (for future mechanics)
    public void SetCustomTarget(Transform target)
    {
        SetTarget(target, TargetType.None);
    }

    // Disable AI (for death, stun, etc.)
    public void DisableAI()
    {
        if (agent != null)
            agent.enabled = false;

        enabled = false;
    }

    void OnDrawGizmosSelected()
    {
        // Draw detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, playerDetectionRange);

        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Draw line to current target
        if (currentTarget != null)
        {
            Gizmos.color = currentTargetType == TargetType.Player ? Color.blue : Color.magenta;
            Gizmos.DrawLine(transform.position + Vector3.up, currentTarget.position + Vector3.up);
        }

        // Draw stopping distance
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);
    }
}