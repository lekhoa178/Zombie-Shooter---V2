using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace DungeonGeneration
{
    /// <summary>
    /// Reusable component for spawning waves of enemies
    /// </summary>
    public class WaveSpawner : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Wave Configuration")]
        [SerializeField] private List<WaveCombination> m_WaveCombinations = new List<WaveCombination>();
        [SerializeField] private float m_TimeBetweenSpawns = 0.25f;
        
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
        public event Action<int, int> OnWaveStarted; // WaveSetIndex, CombinationIndex
        public event Action OnAllEnemyDefeated;
        
        // Delegate for custom spawn position handling
        public delegate Vector3 GetCustomSpawnPositionDelegate(int waveSetIndex, int waveIndex, int enemyIndex, GameObject enemyPrefab);
        public GetCustomSpawnPositionDelegate GetCustomSpawnPosition;
        #endregion
        
        #region Private Fields
        private int m_CurrentWaveSetIndex = 0;
        private WaveCombination m_SelectedCombination;
        private List<BaseEnemy> m_ActiveEnemies = new List<BaseEnemy>();
        private bool m_IsActive = false;
        private GameObject m_Player;
        private Coroutine m_WaveCoroutine;
        private Coroutine m_TimeBasedWaveCoroutine;
        private bool m_WaitingForManualTrigger = false;
        private float m_TimeUntilNextWave = 0f;
        private int m_CurrentWaveIndex = 0;
        private int m_CurrentEnemyIndex = 0;
        #endregion
        
        #region Properties
        /// <summary>
        /// Returns true if the current wave is complete and waiting for the next wave to be triggered
        /// </summary>
        public bool IsReadyForNextWave => m_IsActive && m_ActiveEnemies.Count == 0 && m_WaveCoroutine == null;
        
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
                if (m_ProgressionMode == WaveProgressionMode.TimeBased && m_IsActive)
                {
                    m_TimeBasedWaveCoroutine = StartCoroutine(TimeBasedWaveSpawner());
                }
            }
        }
        
        /// <summary>
        /// Time remaining until next automatic wave spawn in TimeBased mode
        /// </summary>
        public float TimeUntilNextWave => m_TimeUntilNextWave;

        /// <summary>
        /// Gets or sets the currently selected wave combination
        /// </summary>
        public WaveCombination SelectedCombination
        {
            get => m_SelectedCombination;
            set => m_SelectedCombination = value;
        }
        #endregion
        
        #region Unity Lifecycle
        private void Awake()
        {
            // Find player reference
            m_Player = GameObject.FindGameObjectWithTag("Player");
            if (m_Player == null)
            {
                Debug.LogError("No player found in scene! Make sure the player has the 'Player' tag.");
            }
            
            // Validate spawn points
            if (m_SpawnPoints.Length == 0 && !m_UseRandomSpawns)
            {
                Debug.LogWarning("No spawn points defined! Using random spawns instead.");
                m_UseRandomSpawns = true;
            }
            
            // Register with the enemy manager for tracking active enemies
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
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Start spawning waves
        /// </summary>
        public void StartSpawning()
        {
            if (m_IsActive) return;
            
            m_IsActive = true;
            m_CurrentWaveSetIndex = 0;
            
            // Select a random wave combination if none is set
            if (m_SelectedCombination == null)
            {
                SelectRandomCombination();
            }
            
            // Start first wave set
            if (m_AutoStartFirstWave)
            {
                StartCoroutine(StartCombatAfterDelay(1f));
            }
            else if (m_ProgressionMode == WaveProgressionMode.Manual)
            {
                m_WaitingForManualTrigger = true;
            }
            
            // Start time-based wave spawner if needed
            if (m_ProgressionMode == WaveProgressionMode.TimeBased)
            {
                m_TimeBasedWaveCoroutine = StartCoroutine(TimeBasedWaveSpawner());
            }
        }
        
        /// <summary>
        /// Stop all spawning
        /// </summary>
        public void StopSpawning()
        {
            m_IsActive = false;
            
            // Stop coroutines
            if (m_WaveCoroutine != null)
            {
                StopCoroutine(m_WaveCoroutine);
                m_WaveCoroutine = null;
            }
            
            if (m_TimeBasedWaveCoroutine != null)
            {
                StopCoroutine(m_TimeBasedWaveCoroutine);
                m_TimeBasedWaveCoroutine = null;
            }
        }
        
        /// <summary>
        /// Manually trigger the next wave (only works when in Manual mode or when no wave is active)
        /// </summary>
        public void TriggerNextWave()
        {
            if ((m_WaitingForManualTrigger || m_ActiveEnemies.Count == 0) && m_WaveCoroutine == null && m_IsActive)
            {
                m_WaitingForManualTrigger = false;
                m_WaveCoroutine = StartCoroutine(StartNextWaveSet());
                
                if (m_DebugMode)
                {
                    Debug.Log("Manually triggering next wave set.");
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
        
        /// <summary>
        /// Set a specific wave combination to use (overrides random selection)
        /// </summary>
        public void SetWaveCombination(WaveCombination combination)
        {
            m_SelectedCombination = combination;
        }
        
        /// <summary>
        /// Set a specific wave combination to use by index
        /// </summary>
        public void SetWaveCombinationByIndex(int index)
        {
            if (index >= 0 && index < m_WaveCombinations.Count)
            {
                m_SelectedCombination = m_WaveCombinations[index];
            }
        }
        #endregion
        
        #region Wave Management
        /// <summary>
        /// Selects a random wave combination to use
        /// </summary>
        private void SelectRandomCombination()
        {
            if (m_WaveCombinations.Count == 0)
            {
                Debug.LogError("No wave combinations defined!");
                return;
            }
            
            int randomIndex = UnityEngine.Random.Range(0, m_WaveCombinations.Count);
            m_SelectedCombination = m_WaveCombinations[randomIndex];
            
            if (m_DebugMode)
            {
                Debug.Log($"Selected wave combination: {m_SelectedCombination.Name} (index: {randomIndex})");
            }
            
            // Reset wave set index
            m_CurrentWaveSetIndex = 0;
        }
        
        /// <summary>
        /// Start combat after a delay
        /// </summary>
        private IEnumerator StartCombatAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            // Start first wave set
            m_WaveCoroutine = StartCoroutine(StartNextWaveSet());
        }
        
        /// <summary>
        /// Coroutine that handles time-based wave spawning
        /// </summary>
        private IEnumerator TimeBasedWaveSpawner()
        {
            m_TimeUntilNextWave = m_TimeBasedWaveInterval;
            
            while (m_IsActive)
            {
                // Count down until next wave
                while (m_TimeUntilNextWave > 0 && m_IsActive)
                {
                    yield return new WaitForSeconds(0.1f);
                    m_TimeUntilNextWave -= 0.1f;
                }
                
                // Time's up, spawn next wave if not already spawning
                if (m_WaveCoroutine == null && m_IsActive)
                {
                    m_WaveCoroutine = StartCoroutine(StartNextWaveSet());
                }
                
                // Reset timer
                m_TimeUntilNextWave = m_TimeBasedWaveInterval;
            }
        }
        
        /// <summary>
        /// Called when all enemies in a wave have been defeated
        /// </summary>
        private void CheckWaveCompletion()
        {
            if (m_ActiveEnemies.Count == 0 && m_WaveCoroutine == null && !m_WaitingForManualTrigger && m_IsActive)
            {
                OnWaveCompleted?.Invoke();
                
                // Check if this was the last wave set
                if (m_SelectedCombination != null && m_CurrentWaveSetIndex >= m_SelectedCombination.WaveSets.Count)
                {
                    OnAllEnemyDefeated?.Invoke();
                    return;
                }
                
                if (m_ProgressionMode == WaveProgressionMode.EnemyBased)
                {
                    // Start next wave automatically when enemies are defeated
                    m_WaveCoroutine = StartCoroutine(StartNextWaveSet());
                }
                else if (m_ProgressionMode == WaveProgressionMode.Manual)
                {
                    // Signal that we're waiting for manual triggering
                    m_WaitingForManualTrigger = true;
                    
                    if (m_DebugMode)
                    {
                        Debug.Log("Wave set completed. Waiting for manual trigger to start next wave set.");
                    }
                }
                // TimeBased mode handles its own wave spawning via coroutine
            }
        }
        
        /// <summary>
        /// Starts the next wave set of enemies
        /// </summary>
        private IEnumerator StartNextWaveSet()
        {
            if (m_SelectedCombination == null || m_SelectedCombination.WaveSets.Count == 0)
            {
                Debug.LogError("No wave sets defined in the selected combination!");
                m_WaveCoroutine = null;
                yield break;
            }
            
            // Check if we've completed all wave sets in the combination
            if (m_CurrentWaveSetIndex >= m_SelectedCombination.WaveSets.Count)
            {
                // No more wave sets to spawn, but wait for all enemies to be defeated
                // before calling OnCombatComplete (handled in CheckWaveCompletion)
                m_WaveCoroutine = null;
                yield break;
            }
            
            // Get current wave set
            var currentWaveSet = m_SelectedCombination.WaveSets[m_CurrentWaveSetIndex];
            
            if (m_DebugMode)
            {
                Debug.Log($"Starting WaveSet {m_CurrentWaveSetIndex + 1} of Combination {m_SelectedCombination.Name}");
            }
            
            // Notify that a wave set is starting
            OnWaveStarted?.Invoke(m_CurrentWaveSetIndex, m_WaveCombinations.IndexOf(m_SelectedCombination));
            
            // Spawn all waves in this wave set
            m_CurrentWaveIndex = 0;
            foreach (Wave wave in currentWaveSet.Waves)
            {
                if (wave.IsWildWave && m_AllowWildWaves)
                {
                    yield return StartCoroutine(SpawnWildWave());
                }
                else
                {
                    // Spawn all enemies in the wave
                    m_CurrentEnemyIndex = 0;
                    for (int i = 0; i < wave.SpawnCount; i++)
                    {
                        // Get custom or default spawn position
                        Vector3 spawnPosition;
                        if (GetCustomSpawnPosition != null)
                        {
                            spawnPosition = GetCustomSpawnPosition(m_CurrentWaveSetIndex, m_CurrentWaveIndex, m_CurrentEnemyIndex, wave.EnemyPrefab);
                            
                            // If the custom handler returns Vector3.zero, fall back to default spawning
                            if (spawnPosition == Vector3.zero)
                            {
                                spawnPosition = GetSpawnPosition();
                            }
                        }
                        else
                        {
                            spawnPosition = GetSpawnPosition();
                        }
                        
                        BaseEnemy enemy = SpawnEnemy(
                            wave.EnemyPrefab, 
                            spawnPosition, 
                            Quaternion.identity);
                            
                        // Wait between spawns
                        if (m_TimeBetweenSpawns > 0)
                            yield return new WaitForSeconds(m_TimeBetweenSpawns);
                            
                        m_CurrentEnemyIndex++;
                    }
                }
                
                // Short wait between waves in the same set
                if (m_TimeBetweenSpawns > 0)
                    yield return new WaitForSeconds(m_TimeBetweenSpawns);
                    
                m_CurrentWaveIndex++;
            }
            
            // Reset indices after wave set completion
            m_CurrentWaveIndex = 0;
            m_CurrentEnemyIndex = 0;
            
            // Increment wave set index for next time
            m_CurrentWaveSetIndex++;
            
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
                SpawnEnemy(
                    enemyPrefab, 
                    spawnPosition, 
                    Quaternion.identity);
                    
                if (m_TimeBetweenSpawns > 0)
                {
                    yield return new WaitForSeconds(m_TimeBetweenSpawns);
                }
            }
        }
        #endregion
        
        #region Spawning
        /// <summary>
        /// Gets a position to spawn an enemy
        /// </summary>
        private Vector3 GetSpawnPosition()
        {
            // Check if a custom spawn position handler is available
            if (GetCustomSpawnPosition != null)
            {
                Vector3 customPosition = GetCustomSpawnPosition(m_CurrentWaveSetIndex, m_CurrentWaveIndex, m_CurrentEnemyIndex, null);
                if (customPosition != Vector3.zero)
                {
                    return customPosition;
                }
            }
            
            if (!m_UseRandomSpawns && m_SpawnPoints.Length > 0)
            {
                // Use predefined spawn points
                int randomIndex = UnityEngine.Random.Range(0, m_SpawnPoints.Length);
                return m_SpawnPoints[randomIndex].position;
            }
            else
            {
                // Use NavMesh to find valid spawn position
                if (m_Player == null)
                {
                    m_Player = GameObject.FindGameObjectWithTag("Player");
                    if (m_Player == null)
                    {
                        Debug.LogError("No player found in scene!");
                        return Vector3.zero;
                    }
                }
                
                var playerPos = m_Player.transform.position;
                
                for (var i = 0; i < 30; i++) // Max attempts
                {
                    var randomDirection = UnityEngine.Random.insideUnitSphere * m_MaxSpawnDistance;
                    randomDirection += playerPos;

                    if (!NavMesh.SamplePosition(randomDirection, out var hit, m_MaxSpawnDistance, NavMesh.AllAreas))
                        continue;
                    
                    var distToPlayer = Vector3.Distance(hit.position, playerPos);
                        
                    // Ensure it's not too close to the player
                    if (distToPlayer >= m_MinSpawnDistance)
                    {
                        return hit.position;
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
    /// Wave progression modes
    /// </summary>
    public enum WaveProgressionMode
    {
        Manual,         // Player must manually trigger the next wave
        TimeBased,      // Next wave spawns after a set time regardless of enemies alive
        EnemyBased      // Next wave spawns when all enemies are defeated
    }
    
    /// <summary>
    /// Represents a combination of wave sets that can be selected randomly
    /// </summary>
    [Serializable]
    public class WaveCombination
    {
        public string Name;
        public List<WaveSet> WaveSets = new List<WaveSet>();
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