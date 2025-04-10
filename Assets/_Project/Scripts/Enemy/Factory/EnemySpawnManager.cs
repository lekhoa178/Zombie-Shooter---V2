using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages multiple spawn points and provides central control over enemy spawning across a level.
/// </summary>
public class EnemySpawnManager : MonoBehaviour
{
    #region Serialized Fields
    [Header("Spawn Management")]
    [SerializeField] private bool m_AutoFindSpawners = true;
    [SerializeField] private List<EnemySpawner> m_ManagedSpawners = new List<EnemySpawner>();
    [SerializeField] private bool m_StartSpawningOnStart = false;
    
    [Header("Wave Settings")]
    [SerializeField] private bool m_UseWaveSystem = false;
    [SerializeField] private List<EnemyWave> m_Waves = new List<EnemyWave>();
    [SerializeField] private float m_TimeBetweenWaves = 10f;
    [SerializeField] private bool m_LoopWaves = false;
    
    [Header("Spawn Limits")]
    [SerializeField] private bool m_LimitTotalEnemies = false;
    [SerializeField] private int m_MaxConcurrentEnemies = 10;
    [SerializeField] private int m_MaxTotalEnemies = 50;
    
    [Header("Debug")]
    [SerializeField] private bool m_ShowDebugMessages = false;
    #endregion
    
    #region Public Events
    [Header("Events")]
    public UnityEvent OnAllWavesCompleted;
    public UnityEvent<int> OnWaveStarted;
    public UnityEvent<int> OnWaveCompleted;
    #endregion
    
    #region Private Fields
    private int m_CurrentWaveIndex = 0;
    private int m_TotalEnemiesSpawned = 0;
    private int m_CurrentActiveEnemies = 0;
    private bool m_IsSpawningActive = false;
    private List<BaseEnemy> m_TrackedEnemies = new List<BaseEnemy>();
    private Coroutine m_WaveCoroutine;
    #endregion
    
    #region Singleton Pattern
    private static EnemySpawnManager s_Instance;
    
    public static EnemySpawnManager Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = FindObjectOfType<EnemySpawnManager>();
                
                if (s_Instance == null)
                {
                    GameObject managerObject = new GameObject("EnemySpawnManager");
                    s_Instance = managerObject.AddComponent<EnemySpawnManager>();
                }
            }
            
            return s_Instance;
        }
    }
    #endregion
    
    #region Properties
    public bool IsSpawningActive => m_IsSpawningActive;
    public int CurrentWaveIndex => m_CurrentWaveIndex;
    public int TotalEnemiesSpawned => m_TotalEnemiesSpawned;
    public int CurrentActiveEnemies => m_CurrentActiveEnemies;
    #endregion
    
    #region Unity Lifecycle
    private void Awake()
    {
        if (s_Instance != null && s_Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        s_Instance = this;
        
        if (transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }
        
        // Subscribe to factory events
        EnemyFactory.Instance.OnEnemySpawned.AddListener(HandleEnemySpawned);
        EnemyFactory.Instance.OnEnemyDeath.AddListener(HandleEnemyDeath);
        
        if (m_AutoFindSpawners)
        {
            FindAllSpawners();
        }
    }
    
    private void Start()
    {
        if (m_StartSpawningOnStart)
        {
            StartSpawning();
        }
    }
    
    private void OnDestroy()
    {
        // Clean up event subscriptions
        if (EnemyFactory.Instance != null)
        {
            EnemyFactory.Instance.OnEnemySpawned.RemoveListener(HandleEnemySpawned);
            EnemyFactory.Instance.OnEnemyDeath.RemoveListener(HandleEnemyDeath);
        }
    }
    #endregion
    
    #region Public Methods
    /// <summary>
    /// Start spawning based on configured settings
    /// </summary>
    public void StartSpawning()
    {
        if (m_IsSpawningActive) return;
        
        m_IsSpawningActive = true;
        
        if (m_UseWaveSystem)
        {
            // Start wave-based spawning
            StartWaveSystem();
        }
        else
        {
            // Start all spawners
            foreach (EnemySpawner spawner in m_ManagedSpawners)
            {
                if (spawner != null)
                {
                    spawner.StartSpawning();
                }
            }
        }
    }
    
    /// <summary>
    /// Stop all enemy spawning
    /// </summary>
    public void StopSpawning()
    {
        m_IsSpawningActive = false;
        
        // Stop wave coroutine if active
        if (m_WaveCoroutine != null)
        {
            StopCoroutine(m_WaveCoroutine);
            m_WaveCoroutine = null;
        }
        
        // Stop all spawners
        foreach (EnemySpawner spawner in m_ManagedSpawners)
        {
            if (spawner != null)
            {
                spawner.StopSpawning();
            }
        }
    }
    
    /// <summary>
    /// Add a spawner to the managed list
    /// </summary>
    public void AddSpawner(EnemySpawner spawner)
    {
        if (spawner != null && !m_ManagedSpawners.Contains(spawner))
        {
            m_ManagedSpawners.Add(spawner);
        }
    }
    
    /// <summary>
    /// Remove a spawner from the managed list
    /// </summary>
    public void RemoveSpawner(EnemySpawner spawner)
    {
        if (spawner != null)
        {
            m_ManagedSpawners.Remove(spawner);
        }
    }
    
    /// <summary>
    /// Manually start a specific wave
    /// </summary>
    public void StartWave(int waveIndex)
    {
        if (!m_UseWaveSystem || waveIndex < 0 || waveIndex >= m_Waves.Count)
        {
            Debug.LogWarning($"Invalid wave index: {waveIndex}. Wave system is {(m_UseWaveSystem ? "enabled" : "disabled")}. Valid range: 0-{m_Waves.Count - 1}");
            return;
        }
        
        m_CurrentWaveIndex = waveIndex;
        
        if (m_WaveCoroutine != null)
        {
            StopCoroutine(m_WaveCoroutine);
        }
        
        m_WaveCoroutine = StartCoroutine(SpawnWave(m_CurrentWaveIndex));
    }
    
    /// <summary>
    /// Skip to the next wave
    /// </summary>
    public void SkipToNextWave()
    {
        if (!m_UseWaveSystem || m_Waves.Count == 0) return;
        
        // Stop current wave if active
        if (m_WaveCoroutine != null)
        {
            StopCoroutine(m_WaveCoroutine);
            m_WaveCoroutine = null;
        }
        
        // Move to next wave with looping if enabled
        m_CurrentWaveIndex++;
        if (m_CurrentWaveIndex >= m_Waves.Count)
        {
            if (m_LoopWaves)
            {
                m_CurrentWaveIndex = 0;
            }
            else
            {
                m_CurrentWaveIndex = m_Waves.Count - 1;
                OnAllWavesCompleted?.Invoke();
                return;
            }
        }
        
        // Start the next wave
        m_WaveCoroutine = StartCoroutine(SpawnWave(m_CurrentWaveIndex));
    }
    
    /// <summary>
    /// Reset all spawn counters and wave state
    /// </summary>
    public void ResetSpawnSystem()
    {
        StopSpawning();
        
        m_CurrentWaveIndex = 0;
        m_TotalEnemiesSpawned = 0;
        m_CurrentActiveEnemies = 0;
        
        // Clear tracked enemies
        m_TrackedEnemies.Clear();
    }
    
    /// <summary>
    /// Find all spawners in the scene and add them to the managed list
    /// </summary>
    public void FindAllSpawners()
    {
        m_ManagedSpawners.Clear();
        EnemySpawner[] spawners = FindObjectsOfType<EnemySpawner>();
        
        if (spawners != null && spawners.Length > 0)
        {
            m_ManagedSpawners.AddRange(spawners);
            
            if (m_ShowDebugMessages)
            {
                Debug.Log($"Found {spawners.Length} enemy spawners in scene");
            }
        }
    }
    #endregion
    
    #region Private Methods
    private void StartWaveSystem()
    {
        if (m_Waves.Count == 0)
        {
            Debug.LogWarning("Wave system is enabled but no waves are configured.");
            return;
        }
        
        m_CurrentWaveIndex = 0;
        m_WaveCoroutine = StartCoroutine(SpawnWave(m_CurrentWaveIndex));
    }
    
    private IEnumerator SpawnWave(int waveIndex)
    {
        if (waveIndex < 0 || waveIndex >= m_Waves.Count) yield break;
        
        EnemyWave currentWave = m_Waves[waveIndex];
        
        // Notify listeners that wave has started
        OnWaveStarted?.Invoke(waveIndex);
        
        if (m_ShowDebugMessages)
        {
            Debug.Log($"Starting wave {waveIndex + 1}/{m_Waves.Count}");
        }
        
        // Wait for initial delay
        if (currentWave.initialDelay > 0)
        {
            yield return new WaitForSeconds(currentWave.initialDelay);
        }
        
        // Track enemies spawned in this wave
        int waveEnemiesSpawned = 0;
        int totalEnemiesInWave = currentWave.GetTotalEnemiesCount();
        
        // Spawn enemies in batches
        while (waveEnemiesSpawned < totalEnemiesInWave && m_IsSpawningActive)
        {
            // Check if we've hit the concurrent limit
            if (m_LimitTotalEnemies && m_CurrentActiveEnemies >= m_MaxConcurrentEnemies)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }
            
            // Check if we've hit the total limit
            if (m_LimitTotalEnemies && m_TotalEnemiesSpawned >= m_MaxTotalEnemies)
            {
                if (m_ShowDebugMessages)
                {
                    Debug.Log("Reached maximum total enemies limit. Stopping spawn system.");
                }
                
                m_IsSpawningActive = false;
                yield break;
            }
            
            // Determine how many enemies to spawn in this batch
            int batchSize = Mathf.Min(
                currentWave.batchSize, 
                totalEnemiesInWave - waveEnemiesSpawned,
                m_LimitTotalEnemies ? m_MaxConcurrentEnemies - m_CurrentActiveEnemies : int.MaxValue
            );
            
            if (batchSize <= 0)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }
            
            // Spawn the batch
            for (int i = 0; i < batchSize; i++)
            {
                if (!m_IsSpawningActive) break;
                
                // Get a random enemy prefab and spawner based on wave configuration
                EnemyWaveItem enemyToSpawn = currentWave.GetRandomEnemy();
                EnemySpawner spawner = GetRandomSpawner(currentWave.useSpecificSpawners ? currentWave.specificSpawners : null);
                
                if (enemyToSpawn.enemyPrefab != null && spawner != null)
                {
                    // Temporarily set the spawner's prefab and telegraph duration
                    spawner.SetEnemyPrefab(enemyToSpawn.enemyPrefab);
                    spawner.SpawnSingleEnemy();
                    
                    waveEnemiesSpawned++;
                    
                    // Add small delay between spawns within batch
                    if (currentWave.delayBetweenSpawns > 0)
                    {
                        yield return new WaitForSeconds(currentWave.delayBetweenSpawns);
                    }
                }
            }
            
            // Wait for delay between batches
            if (currentWave.delayBetweenBatches > 0)
            {
                yield return new WaitForSeconds(currentWave.delayBetweenBatches);
            }
        }
        
        // Wait for enemies to be defeated if required
        if (currentWave.waitForAllEnemiesDefeated)
        {
            while (m_CurrentActiveEnemies > 0)
            {
                yield return new WaitForSeconds(1f);
            }
        }
        
        // Notify listeners that wave has completed
        OnWaveCompleted?.Invoke(waveIndex);
        
        if (m_ShowDebugMessages)
        {
            Debug.Log($"Wave {waveIndex + 1} completed");
        }
        
        // Move to next wave if there are more
        if (m_IsSpawningActive)
        {
            if (waveIndex < m_Waves.Count - 1)
            {
                m_CurrentWaveIndex++;
                
                // Wait between waves
                if (m_TimeBetweenWaves > 0)
                {
                    yield return new WaitForSeconds(m_TimeBetweenWaves);
                }
                
                // Start next wave
                m_WaveCoroutine = StartCoroutine(SpawnWave(m_CurrentWaveIndex));
            }
            else if (m_LoopWaves)
            {
                m_CurrentWaveIndex = 0;
                
                // Wait between waves
                if (m_TimeBetweenWaves > 0)
                {
                    yield return new WaitForSeconds(m_TimeBetweenWaves);
                }
                
                // Start from first wave again
                m_WaveCoroutine = StartCoroutine(SpawnWave(m_CurrentWaveIndex));
            }
            else
            {
                // All waves completed
                OnAllWavesCompleted?.Invoke();
                
                if (m_ShowDebugMessages)
                {
                    Debug.Log("All waves completed");
                }
            }
        }
    }
    
    private EnemySpawner GetRandomSpawner(List<EnemySpawner> specificSpawners = null)
    {
        // Use specific spawners if provided, otherwise use all managed spawners
        List<EnemySpawner> spawners = specificSpawners != null && specificSpawners.Count > 0 ? specificSpawners : m_ManagedSpawners;
        
        if (spawners.Count == 0)
        {
            Debug.LogWarning("No spawners available");
            return null;
        }
        
        return spawners[Random.Range(0, spawners.Count)];
    }
    
    private void HandleEnemySpawned(BaseEnemy enemy)
    {
        if (enemy == null) return;
        
        m_TotalEnemiesSpawned++;
        m_CurrentActiveEnemies++;
        m_TrackedEnemies.Add(enemy);
    }
    
    private void HandleEnemyDeath(BaseEnemy enemy)
    {
        if (enemy == null) return;
        
        m_CurrentActiveEnemies--;
        m_TrackedEnemies.Remove(enemy);
    }
    #endregion
}

#region Support Classes
[System.Serializable]
public class EnemyWave
{
    [Header("Wave Settings")]
    public string waveName = "Wave";
    public float initialDelay = 2f;
    
    [Header("Enemies")]
    public List<EnemyWaveItem> enemies = new List<EnemyWaveItem>();
    
    [Header("Spawn Control")]
    public int batchSize = 3;
    public float delayBetweenSpawns = 0.5f;
    public float delayBetweenBatches = 5f;
    
    [Header("Spawner Selection")]
    public bool useSpecificSpawners = false;
    public List<EnemySpawner> specificSpawners = new List<EnemySpawner>();
    
    [Header("Wave Completion")]
    public bool waitForAllEnemiesDefeated = true;
    
    public EnemyWaveItem GetRandomEnemy()
    {
        if (enemies.Count == 0)
        {
            Debug.LogWarning($"Wave '{waveName}' has no enemy prefabs configured.");
            return null;
        }
        
        // Calculate total weight
        float totalWeight = 0f;
        foreach (var enemy in enemies)
        {
            totalWeight += enemy.spawnWeight;
        }
        
        // Select an enemy based on weight
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;
        
        foreach (var enemy in enemies)
        {
            currentWeight += enemy.spawnWeight;
            if (randomValue <= currentWeight)
            {
                return enemy;
            }
        }
        
        // Fallback to first enemy
        return enemies[0];
    }
    
    public int GetTotalEnemiesCount()
    {
        int total = 0;
        foreach (var enemy in enemies)
        {
            total += enemy.count;
        }
        return total;
    }
}

[System.Serializable]
public class EnemyWaveItem
{
    public GameObject enemyPrefab;
    public int count = 10;
    public float spawnWeight = 1f;
}
#endregion 