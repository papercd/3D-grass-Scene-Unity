using System;
using System.Collections.Generic;
using UnityEngine;

namespace WaveSpawner {
    [CreateAssetMenu(fileName = "Waves",  menuName = "WaveSpawner/Waves")]
    public class Waves : ScriptableObject {
        public List<Wave> waves = new List<Wave>();

        public int GetTotalWaves() { return waves.Count; }
        public Wave GetWave(int waveNumber) { return waves.Find(w => w.waveNumber == waveNumber); }
    }
    
    [Serializable]
    public class Wave {
        public int waveNumber;                 // Wave number
        public List<EnemyGroup> enemyGroups;   // Enemy groups in this wave
        public int quota;                      // Total number of enemies to spawn in this wave
        public int maxConcurrent;              // Maximum number of concurrent enemeis in a wave
        public float spawnInterval;            // Spawn check interval
    }

    [Serializable]
    public class EnemyGroup {
        public string enemyName;       // Enemny name
        public int groupSize;          // Number of enemies to instantiate per "Spawn"
        public int groupQuota;         // Maximum number of this enemy
        public int totalGroupSpawns;   // Counter
        public GameObject enemyPrefab;
    }
}