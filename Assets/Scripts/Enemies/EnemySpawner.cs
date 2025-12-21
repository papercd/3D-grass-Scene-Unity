using UnityEngine;

/// <summary>
/// Simple enemy spawner for testing - will be replaced with wave system later.
/// Spawns enemies at specified spawn points around the map.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private int maxEnemies = 10;

    [Header("Auto Spawn")]
    [SerializeField] private bool autoSpawn = true;
    [SerializeField] private bool spawnOnStart = true;

    [Header("Debug")]
    [SerializeField] private bool showSpawnGizmos = true;

    private float nextSpawnTime;
    private int currentEnemyCount = 0;

    void Start()
    {
        if (spawnOnStart)
        {
            SpawnEnemy();
        }

        nextSpawnTime = Time.time + spawnInterval;
    }

    void Update()
    {
        if (!autoSpawn) return;

        // Check if it's time to spawn and we haven't hit the limit
        if (Time.time >= nextSpawnTime && currentEnemyCount < maxEnemies)
        {
            SpawnEnemy();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    /// <summary>
    /// Spawn a single enemy at a random spawn point
    /// </summary>
    public void SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("❌ EnemySpawner: No enemy prefab assigned!");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("❌ EnemySpawner: No spawn points assigned!");
            return;
        }

        // Pick random spawn point
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        // Spawn enemy
        GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
        enemy.name = $"Shadow_Wisp_{currentEnemyCount + 1}";

        // Subscribe to enemy death to track count
        EnemyStats stats = enemy.GetComponent<EnemyStats>();
        if (stats != null)
        {
            stats.OnDeath += OnEnemyDied;
        }

        currentEnemyCount++;
        Debug.Log($"👻 Spawned {enemy.name} at {spawnPoint.name}. Total enemies: {currentEnemyCount}");
    }

    void OnEnemyDied()
    {
        currentEnemyCount--;
        Debug.Log($"💀 Enemy defeated. Remaining enemies: {currentEnemyCount}");
    }

    // Manual spawn method for testing
    [ContextMenu("Spawn Enemy Now")]
    public void SpawnEnemyManual()
    {
        SpawnEnemy();
    }

    [ContextMenu("Spawn 5 Enemies")]
    public void SpawnWave()
    {
        for (int i = 0; i < 5; i++)
        {
            SpawnEnemy();
        }
    }

    void OnDrawGizmos()
    {
        if (!showSpawnGizmos || spawnPoints == null) return;

        // Draw spawn points
        foreach (Transform spawnPoint in spawnPoints)
        {
            if (spawnPoint == null) continue;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(spawnPoint.position, 1f);

            // Draw arrow pointing up
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(spawnPoint.position, spawnPoint.position + Vector3.up * 2f);
        }
    }
}