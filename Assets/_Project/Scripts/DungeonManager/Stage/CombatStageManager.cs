using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace DungeonGeneration
{
    /// <summary>
    /// Wave progression modes
    /// </summary>
    public enum WaveProgressionMode
    {
        Manual,         // Player must manually trigger the next wave
        TimeBased,      // Next wave spawns after a set time regardless of enemies alive
        EnemyBased      // Next wave spawns when all enemies are defeated
    }

    /// <summary>
    /// Manages a combat stage with wave-based enemy spawning
    /// </summary>
    public class CombatStageManager : StageBaseManager
    {
        #region Serialized Fields
        [Header("Wave Configuration")]
        [SerializeField] private List<WaveSet> m_WaveSets = new List<WaveSet>();
        [SerializeField] private float m_TimeBetweenWaves = 3f;
        [SerializeField] private float m_TimeBetweenSpawns = 0.5f;
        
        [Header("Wave Progression")]
        [SerializeField] private WaveProgressionMode m_ProgressionMode = WaveProgressionMode.EnemyBased;
        [SerializeField] private float m_TimeBasedWaveInterval = 30f;
        [Tooltip("Should the first wave start automatically?")]
        [SerializeField] private bool m_AutoStartFirstWave = true;
        
        [Header("Spawn Settings")]
        [SerializeField] private Transform[] m_SpawnPoints;
        [SerializeField] private float m_MinSpawnDistance = 5f;
        [SerializeField] private float m_MaxSpawnDistance = 15f;
        [SerializeField] private bool m_UseRandomSpawns = true;
        
        [Header("Wild Mode")]
        [SerializeField] private bool m_AllowWildWaves = false;
        [SerializeField] private List<GameObject> m_WildEnemyPool = new List<GameObject>();
        [SerializeField, Range(1, 10)] private int m_MinWildEnemies = 3;
        [SerializeField, Range(1, 20)] private int m_MaxWildEnemies = 8;
        
        [Header("Debug")]
        [SerializeField] private bool m_DebugMode = false;
        #endregion
        
        #region Events
        public event Action OnWaveCompleted;
        public event Action<int, int> OnWaveStarted; // WaveIndex, WaveSetIndex
        #endregion
        
        #region Private Fields
        private int m_CurrentWaveSetIndex = 0;
        private int m_CurrentWaveIndex = 0;
        private List<BaseEnemy> m_ActiveEnemies = new List<BaseEnemy>();
        private bool m_StageCleared = false;
        private bool m_StageActive = false;
        private GameObject m_Player;
        private Coroutine m_WaveCoroutine;
        private Coroutine m_TimeBasedWaveCoroutine;
        private bool m_WaitingForManualTrigger = false;
        private float m_TimeUntilNextWave = 0f;
        #endregion
        
        #region Properties
        /// <summary>
        /// Returns true if the current wave is complete and waiting for the next wave to be triggered
        /// </summary>
        public bool IsReadyForNextWave => m_StageActive && !m_StageCleared && m_ActiveEnemies.Count == 0 && m_WaveCoroutine == null;
        
        /// <summary>
        /// Returns true if stage is completely finished
        /// </summary>
        public bool IsStageCleared => m_StageCleared;
        
        /// <summary>
        /// Current wave progression mode
        /// </summary>
        public WaveProgressionMode ProgressionMode
        {
            get => m_ProgressionMode;
            set
            {
                // Clean up previous mode if needed
                if (m_TimeBasedWaveCoroutine != null && m_ProgressionMode == WaveProgressionMode.TimeBased && value != WaveProgressionMode.TimeBased)
                {
                    StopCoroutine(m_TimeBasedWaveCoroutine);
                    m_TimeBasedWaveCoroutine = null;
                }
                
                m_ProgressionMode = value;
                
                // Setup new mode if needed
                if (m_ProgressionMode == WaveProgressionMode.TimeBased && m_StageActive && !m_StageCleared)
                {
                    m_TimeBasedWaveCoroutine = StartCoroutine(TimeBasedWaveSpawner());
                }
            }
        }
        
        /// <summary>
        /// Time remaining until next automatic wave spawn in TimeBased mode
        /// </summary>
        public float TimeUntilNextWave => m_TimeUntilNextWave;
        #endregion
        
        #region Unity Lifecycle
        private void Start()
        {
            // Find player reference
            m_Player = GameObject.FindGameObjectWithTag("Player");
            if (m_Player == null)
            {
                Debug.LogError("No player found in scene! Make sure the player has the 'Player' tag.");
                return;
            }
            
            // Validate spawn points
            if (m_SpawnPoints.Length == 0 && !m_UseRandomSpawns)
            {
                Debug.LogWarning("No spawn points defined! Using random spawns instead.");
                m_UseRandomSpawns = true;
            }
            
            // Start combat after a delay
            if (m_AutoStartFirstWave)
            {
                StartCoroutine(StartCombatAfterDelay(1f));
            }
            else if (m_ProgressionMode == WaveProgressionMode.Manual)
            {
                m_StageActive = true;
                m_WaitingForManualTrigger = true;
            }
            
            if (EnemyManager.Instance)
            {
                EnemyManager.Instance.OnEnemyRegistered.AddListener((enemy) =>
                {
                    if (enemy)
                    {
                        m_ActiveEnemies.Add(enemy);
                    }
                });
                
                EnemyManager.Instance.OnEnemyUnregistered.AddListener((enemy) =>
                {
                    if (enemy)
                    {
                        m_ActiveEnemies.Remove(enemy);
                    }
                    
                    CheckWaveCompletion();
                });
            }
        }
        
        private void CheckWaveCompletion()
        {
            if (m_ActiveEnemies.Count == 0 && m_WaveCoroutine == null && !m_WaitingForManualTrigger && m_StageActive && !m_StageCleared)
            {
                OnWaveCompleted?.Invoke();
                
                if (m_ProgressionMode == WaveProgressionMode.EnemyBased)
                {
                    // Start next wave automatically when enemies are defeated
                    m_WaveCoroutine = StartCoroutine(StartNextWave());
                }
                else if (m_ProgressionMode == WaveProgressionMode.Manual)
                {
                    // Signal that we're waiting for manual triggering
                    m_WaitingForManualTrigger = true;
                    
                    if (m_DebugMode)
                    {
                        Debug.Log("Wave completed. Waiting for manual trigger to start next wave.");
                    }
                }
                // TimeBased mode handles its own wave spawning via coroutine
            }
        }
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Manually trigger the next wave (only works when in Manual mode or when no wave is active)
        /// </summary>
        public void TriggerNextWave()
        {
            if ((m_WaitingForManualTrigger || m_ActiveEnemies.Count == 0) && m_WaveCoroutine == null && !m_StageCleared)
            {
                m_WaitingForManualTrigger = false;
                m_WaveCoroutine = StartCoroutine(StartNextWave());
                
                if (m_DebugMode)
                {
                    Debug.Log("Manually triggering next wave.");
                }
            }
        }
        
        /// <summary>
        /// Set wave progression mode
        /// </summary>
        public void SetProgressionMode(WaveProgressionMode mode)
        {
            ProgressionMode = mode;
        }
        
        /// <summary>
        /// Set the time interval for time-based progression mode
        /// </summary>
        public void SetTimeBasedInterval(float seconds)
        {
            if (seconds > 0)
            {
                m_TimeBasedWaveInterval = seconds;
            }
        }
        #endregion
        
        #region Wave Management
        /// <summary>
        /// Start combat after a delay
        /// </summary>
        private IEnumerator StartCombatAfterDelay(float _delay)
        {
            yield return new WaitForSeconds(_delay);
            
            m_StageActive = true;
            
            // Start first wave
            m_WaveCoroutine = StartCoroutine(StartNextWave());
            
            // Start time-based wave spawner if needed
            if (m_ProgressionMode == WaveProgressionMode.TimeBased)
            {
                m_TimeBasedWaveCoroutine = StartCoroutine(TimeBasedWaveSpawner());
            }
        }
        
        /// <summary>
        /// Coroutine that handles time-based wave spawning
        /// </summary>
        private IEnumerator TimeBasedWaveSpawner()
        {
            m_TimeUntilNextWave = m_TimeBasedWaveInterval;
            
            while (m_StageActive && !m_StageCleared)
            {
                // Count down until next wave
                while (m_TimeUntilNextWave > 0)
                {
                    yield return new WaitForSeconds(0.1f);
                    m_TimeUntilNextWave -= 0.1f;
                }
                
                // Time's up, spawn next wave if not already spawning
                if (m_WaveCoroutine == null && !m_StageCleared)
                {
                    m_WaveCoroutine = StartCoroutine(StartNextWave());
                }
                
                // Reset timer
                m_TimeUntilNextWave = m_TimeBasedWaveInterval;
            }
        }
        
        /// <summary>
        /// Starts the next wave of enemies
        /// </summary>
        private IEnumerator StartNextWave()
        {
            // Wait before next wave
            if (m_CurrentWaveIndex > 0 || m_CurrentWaveSetIndex > 0)
            {
                yield return new WaitForSeconds(m_TimeBetweenWaves);
            }
            
            // Check if we've completed all wave sets
            if (m_CurrentWaveSetIndex >= m_WaveSets.Count)
            {
                OnCombatComplete();
                m_WaveCoroutine = null;
                yield break;
            }
            
            // Get current wave set
            WaveSet currentWaveSet = m_WaveSets[m_CurrentWaveSetIndex];
            
            // Check if we've completed all waves in the current set
            if (m_CurrentWaveIndex >= currentWaveSet.Waves.Count)
            {
                // Move to next wave set
                m_CurrentWaveSetIndex++;
                m_CurrentWaveIndex = 0;
                
                // Restart the coroutine
                m_WaveCoroutine = null;
                yield break;
            }
            
            // Get current wave
            Wave currentWave = currentWaveSet.Waves[m_CurrentWaveIndex];
            
            if (m_DebugMode)
            {
                Debug.Log($"Starting Wave {m_CurrentWaveIndex + 1} of WaveSet {m_CurrentWaveSetIndex + 1}");
            }
            
            // Notify that a wave is starting
            OnWaveStarted?.Invoke(m_CurrentWaveIndex, m_CurrentWaveSetIndex);
            
            // Handle wild mode
            if (currentWave.IsWildWave && m_AllowWildWaves)
            {
                yield return StartCoroutine(SpawnWildWave());
            }
            else
            {
                // Spawn all enemies in the wave
                for (int i = 0; i < currentWave.SpawnCount; i++)
                {
                    Vector3 spawnPosition = GetSpawnPosition();
                    
                    BaseEnemy enemy = SpawnEnemy(
                        currentWave.EnemyPrefab, 
                        spawnPosition, 
                        Quaternion.identity);
                        
                    if (enemy)
                    {
                        m_ActiveEnemies.Add(enemy);
                    }
                    
                    // Wait between spawns
                    yield return new WaitForSeconds(m_TimeBetweenSpawns);
                }
            }
            
            // Increment wave index
            m_CurrentWaveIndex++;
            
            // Clear coroutine reference
            m_WaveCoroutine = null;
        }
        
        /// <summary>
        /// Spawns a "wild" wave with random enemies from the pool
        /// </summary>
        private IEnumerator SpawnWildWave()
        {
            if (m_WildEnemyPool.Count == 0)
            {
                Debug.LogError("Cannot spawn wild wave: enemy pool is empty!");
                yield break;
            }
            
            // Determine random number of enemies
            int enemyCount = UnityEngine.Random.Range(m_MinWildEnemies, m_MaxWildEnemies + 1);
            
            for (int i = 0; i < enemyCount; i++)
            {
                // Pick random enemy from pool
                GameObject enemyPrefab = m_WildEnemyPool[UnityEngine.Random.Range(0, m_WildEnemyPool.Count)];
                
                // Get spawn position
                Vector3 spawnPosition = GetSpawnPosition();
                
                // Spawn the enemy
                BaseEnemy enemy = SpawnEnemy(
                    enemyPrefab, 
                    spawnPosition, 
                    Quaternion.identity);
                    
                if (enemy)
                {
                    m_ActiveEnemies.Add(enemy);
                }
                
                if (m_TimeBetweenSpawns > 0)
                {
                    yield return new WaitForSeconds(m_TimeBetweenSpawns);
                }
            }
        }
        
        /// <summary>
        /// Called when all waves have been completed
        /// </summary>
        private void OnCombatComplete()
        {
            m_StageCleared = true;
            
            if (m_DebugMode)
            {
                Debug.Log("Combat stage complete!");
            }
            
            // Clean up time-based coroutine if active
            if (m_TimeBasedWaveCoroutine != null)
            {
                StopCoroutine(m_TimeBasedWaveCoroutine);
                m_TimeBasedWaveCoroutine = null;
            }
            
            onStageCompleted?.Invoke();
        }
        #endregion
        
        #region Spawning
        /// <summary>
        /// Gets a position to spawn an enemy
        /// </summary>
        private Vector3 GetSpawnPosition()
        {
            if (!m_UseRandomSpawns && m_SpawnPoints.Length > 0)
            {
                // Use predefined spawn points
                int randomIndex = UnityEngine.Random.Range(0, m_SpawnPoints.Length);
                return m_SpawnPoints[randomIndex].position;
            }
            else
            {
                // Use NavMesh to find valid spawn position
                Vector3 playerPos = m_Player.transform.position;
                
                for (int i = 0; i < 30; i++) // Max attempts
                {
                    Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * m_MaxSpawnDistance;
                    randomDirection += playerPos;
                    
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(randomDirection, out hit, m_MaxSpawnDistance, NavMesh.AllAreas))
                    {
                        float distToPlayer = Vector3.Distance(hit.position, playerPos);
                        
                        // Ensure it's not too close to the player
                        if (distToPlayer >= m_MinSpawnDistance)
                        {
                            return hit.position;
                        }
                    }
                }
                
                // Fallback if no valid position found
                Debug.LogWarning("Could not find valid spawn position on NavMesh!");
                
                if (m_SpawnPoints.Length > 0)
                {
                    int randomIndex = UnityEngine.Random.Range(0, m_SpawnPoints.Length);
                    return m_SpawnPoints[randomIndex].position;
                }
                
                // Last resort - spawn at origin
                return Vector3.zero;
            }
        }
        
        /// <summary>
        /// Spawns an enemy with optional telegraph effect
        /// </summary>
        private BaseEnemy SpawnEnemy(GameObject enemyPrefab, Vector3 position, Quaternion rotation)
        {
            if (enemyPrefab) return enemyPrefab.SpawnEnemyAt(position);
            
            return null;
        }
        #endregion
    }
    
    /// <summary>
    /// Represents a set of waves to be spawned together
    /// </summary>
    [Serializable]
    public class WaveSet
    {
        public string Name;
        public List<Wave> Waves = new List<Wave>();
    }
    
    /// <summary>
    /// Represents a single wave of enemies
    /// </summary>
    [Serializable]
    public class Wave
    {
        public GameObject EnemyPrefab;
        public int SpawnCount = 1;
        public bool IsWildWave = false;
    }
}