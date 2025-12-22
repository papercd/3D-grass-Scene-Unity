using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace WaveSpawner {
    public class WaveSpawner : MonoBehaviour {
        [Header("Wave Configuration")]
        [SerializeField] private Waves wavesData;

        [Header("Debug")]
        [SerializeField] private bool showSpawnGizmos;
        
        // Current State
        // Wave
        private bool _allWavesCleared;
        private int _totalWaves;
        private Wave _currentWave;
        private bool _isWaveRunning;

        // Enemy
        private List<EnemyStats> _currentEnemiesStats; // For test purposes only
        private int _currentEnemyCount;
        private int _enemySpawnedThisWave;
        private float _nextSpawnTime;

        // Spawning boundary fields
        private Collider _spawnProhibitedArea;
        private List<List<Vector3>> _sides;

        private void Awake() {
            _spawnProhibitedArea = GetComponentInChildren<BoxCollider>();
        }

        void Start() {
            ConfigureSpawnArea();

            _currentEnemiesStats = new();
            _totalWaves = wavesData.GetTotalWaves();
            _currentWave = wavesData.GetWave(0);
            Debug.Log($"[WaveSpawner] Total Number of Waves : {_totalWaves}");
        }

        void Update() {
            if (!_isWaveRunning) { return; }
            if (_enemySpawnedThisWave < _currentWave.quota) {
                if (Time.time >= _nextSpawnTime) {
                    SpawnWave();
                    _nextSpawnTime = Time.time + _currentWave.spawnInterval;
                } 
            } else {
                if (_currentEnemyCount <= 0) { OnWaveCompleted(); }
            }
        }

        public void StartNextWave() {
            _isWaveRunning = true;
            _currentEnemyCount = 0;
            _enemySpawnedThisWave = 0;
            Debug.Log($"Starting Wave : {_currentWave.waveNumber}");
        }
        
        private void SpawnWave() {
            // For each enemy group
            // Spawn Enemy if
            // 1. Concurrent Number is not exceeded
            // 2. Group quota is not met
            foreach (EnemyGroup group in _currentWave.enemyGroups) {
                for (int i = 0; i < group.groupSize; i++) {
                    if (_currentEnemyCount < _currentWave.maxConcurrent && group.totalGroupSpawns < group.groupQuota) {
                        SpawnEnemy(group.enemyPrefab);
                        group.totalGroupSpawns++;
                    }
                }
            }
        } 

        private void SpawnEnemy(GameObject enemyPrefab) {
            // Pick random spawn point
            Vector3 spawnPoint = GetRandomPosition();
            
            // Spawn Enemy
            GameObject enemy = Instantiate(enemyPrefab, spawnPoint, Quaternion.identity);
            EnemyStats stats = enemy.GetComponent<EnemyStats>();
            
            // Subscribe to enemy death
            if (stats != null) {
                stats.OnDeath += OnEnemyDead;
                _currentEnemiesStats.Add(stats);
            }

            _currentEnemyCount++;
            Debug.Log($"👻 Spawned {enemy.name} at {spawnPoint}. Total enemies: {_currentEnemyCount}");
        }
        
        private Vector3 GetRandomPosition() {
            List<Vector3> side = _sides[Random.Range(0, _sides.Count)];
            Vector3 position = Vector3.Lerp(side[0], side[1], Random.Range(0f, 1f));
            return position;
        }

        private void OnEnemyDead() {
            _currentEnemyCount--;
            Debug.Log($"💀 Enemy defeated. Remaining enemies: {_currentEnemyCount}");
        }
        
        /// <summary>
        /// Reset state related variables if next wave exists
        /// Set allWavesCompleted to true if no waves are available
        /// </summary>
        private void OnWaveCompleted() {
            Debug.Log($"Wave Number {_currentWave.waveNumber} Completed");
            _isWaveRunning = false;
            if (_currentWave.waveNumber <= _totalWaves) {
                _currentWave = wavesData.GetWave(_currentWave.waveNumber + 1);
                _nextSpawnTime = Time.time + _currentWave.spawnInterval;
                Debug.Log($"Next Wave Loaded : {_currentWave.waveNumber}");
            } else {
                Debug.Log($"All Waves Completed");
            }
        }

        private void ConfigureSpawnArea() {
            var maxX = _spawnProhibitedArea.bounds.max.x;
            var minX = _spawnProhibitedArea.bounds.min.x;
            var maxZ = _spawnProhibitedArea.bounds.max.z;
            var minZ = _spawnProhibitedArea.bounds.min.z;
            
            var topLeftCorner = new Vector3(minX, 1, maxZ);
            var topRightCorner = new Vector3(maxX, 1, maxZ);
            var bottomLeftCorner = new Vector3(minX, 1, minZ);
            var bottomRightCorner = new Vector3(maxX, 1, minZ);

            var top = new List<Vector3> {topLeftCorner, topRightCorner};
            var bottom = new List<Vector3> {bottomLeftCorner, bottomRightCorner};
            var left = new List<Vector3> {topLeftCorner, bottomLeftCorner};
            var right = new List<Vector3> {topRightCorner, bottomRightCorner};

            _sides =  new List<List<Vector3>> {
                top, bottom, left, right
            };
        }

        [ContextMenu("Start Wave")]
        public void StartNextWaveManual() {
            StartNextWave();
        }

        [ContextMenu("Kill Enemies")]
        public void KillEnemiesManual() {
            foreach (var enemyStat in _currentEnemiesStats) {
                enemyStat.TakeDamage(float.MaxValue);
            }
        }

        private void OnDrawGizmosSelected() {
            if (_spawnProhibitedArea == null) { return; }

            Gizmos.color = Color.crimson;
            Gizmos.DrawWireCube(_spawnProhibitedArea.bounds.center, _spawnProhibitedArea.bounds.size);
        }

        private void OnDisable() {
            foreach (var group in _currentWave.enemyGroups) {
                group.totalGroupSpawns = 0;
            }
        }
    }
}