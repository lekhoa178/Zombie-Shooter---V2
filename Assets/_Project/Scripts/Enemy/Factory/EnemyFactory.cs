using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Flyweight;
using UnityEngine.Events;
using UnityUtils;

/// <summary>
/// Factory class for spawning enemies with telegraph effects and handling death events.
/// </summary>
public class EnemyFactory : PersistentSingleton<EnemyFactory>
{
    #region Serialized Fields
    [Header("Telegraph Settings")]
    [SerializeField] private VFXFlyweightSettings m_DefaultTelegraphVfx;
    [SerializeField, Range(0.5f, 5f)] private float m_DefaultTelegraphDuration = 2f;
    [SerializeField] private Vector3 m_TelegraphVfxOffset = new Vector3(0, 0.5f, 0);
    [SerializeField] private AudioClip m_TelegraphSound;
    
    [Header("Spawning Settings")]
    [SerializeField] private bool m_UseObjectPool = true;
    [SerializeField] private int m_DefaultPoolSize = 10;
    [SerializeField] private bool m_ExpandPoolIfNeeded = true;
    
    [Header("Debug")]
    [SerializeField] private bool m_ShowDebugMessages = false;
    #endregion
    
    #region Events
    [Header("Events")]
    // Events for notifications about enemy lifecycle
    public UnityEvent<BaseEnemy> OnEnemySpawnStarted;
    public UnityEvent<BaseEnemy> OnEnemySpawned;
    public UnityEvent<BaseEnemy> OnEnemyDeath;
    #endregion
    
    #region Private Fields
    // Dictionary to store pools of different enemy types
    private Dictionary<string, List<BaseEnemy>> m_EnemyPools = new Dictionary<string, List<BaseEnemy>>();
    // Dictionary to store prefab references
    private Dictionary<string, GameObject> m_EnemyPrefabs = new Dictionary<string, GameObject>();
    // Dictionary to track active spawning coroutines
    private Dictionary<Vector3, Coroutine> m_ActiveTelegraphs = new Dictionary<Vector3, Coroutine>();
    // Dictionary to track custom telegraph configurations
    private Dictionary<string, EnemyTelegraphConfig> m_CustomTelegraphConfigs = new Dictionary<string, EnemyTelegraphConfig>();
    #endregion
    
    #region Public API
    /// <summary>
    /// Register an enemy prefab for use with the factory
    /// </summary>
    /// <param name="_prefab">The enemy prefab to register</param>
    public void RegisterEnemyPrefab(GameObject _prefab)
    {
        if (_prefab == null) return;
        
        string enemyName = _prefab.name;
        
        if (!m_EnemyPrefabs.ContainsKey(enemyName))
        {
            m_EnemyPrefabs.Add(enemyName, _prefab);
            
            if (m_UseObjectPool)
            {
                // Initialize the pool for this enemy type
                InitializePool(enemyName, _prefab, m_DefaultPoolSize);
            }
            
            if (m_ShowDebugMessages)
            {
                Debug.Log($"Registered enemy prefab: {enemyName}");
            }
        }
    }
    
    /// <summary>
    /// Register a custom telegraph configuration for an enemy type
    /// </summary>
    public void RegisterCustomTelegraph(string _enemyName, EnemyTelegraphConfig _config)
    {
        if (_config == null || string.IsNullOrEmpty(_enemyName)) return;
        
        m_CustomTelegraphConfigs[_enemyName] = _config;
        
        if (m_ShowDebugMessages)
        {
            Debug.Log($"Registered custom telegraph for: {_enemyName}");
        }
    }
    
    /// <summary>
    /// Spawn an enemy at the specified position after a telegraph effect
    /// </summary>
    /// <param name="_enemyPrefab">The enemy prefab to spawn</param>
    /// <param name="_position">Position to spawn at</param>
    /// <param name="_rotation">Rotation to spawn with</param>
    /// <param name="_telegraphDuration">How long to show telegraph before spawning (optional)</param>
    /// <returns>A reference to the spawned enemy (will be null until telegraph completes)</returns>
    public BaseEnemy SpawnEnemy(GameObject _enemyPrefab, Vector3 _position, Quaternion _rotation, float _telegraphDuration = 0f)
    {
        if (_enemyPrefab == null)
        {
            Debug.LogError("Enemy prefab is null. Cannot spawn enemy.");
            return null;
        }
        
        string enemyName = _enemyPrefab.name;
        
        // Register the prefab if it's not already registered
        if (!m_EnemyPrefabs.ContainsKey(enemyName))
        {
            RegisterEnemyPrefab(_enemyPrefab);
        }
        
        // Use either the provided duration or the default
        float telegraphDuration = _telegraphDuration > 0 ? _telegraphDuration : m_DefaultTelegraphDuration;
        
        // Start the spawning process
        StartCoroutine(SpawnEnemyWithTelegraph(enemyName, _position, _rotation, telegraphDuration));
        
        // Return null since the actual enemy isn't spawned yet
        return null;
    }
    
    /// <summary>
    /// Spawn an enemy by name (must be registered first)
    /// </summary>
    /// <param name="_enemyName">Name of the registered enemy prefab</param>
    /// <param name="_position">Position to spawn at</param>
    /// <param name="_rotation">Rotation to spawn with</param>
    /// <param name="_telegraphDuration">How long to show telegraph before spawning (optional)</param>
    /// <returns>A reference to the spawned enemy (will be null until telegraph completes)</returns>
    public BaseEnemy SpawnEnemyByName(string _enemyName, Vector3 _position, Quaternion _rotation, float _telegraphDuration = 0f)
    {
        if (string.IsNullOrEmpty(_enemyName) || !m_EnemyPrefabs.ContainsKey(_enemyName))
        {
            Debug.LogError($"Enemy prefab '{_enemyName}' is not registered. Call RegisterEnemyPrefab first.");
            return null;
        }
        
        // Use either the provided duration or the default
        float telegraphDuration = _telegraphDuration > 0 ? _telegraphDuration : m_DefaultTelegraphDuration;
        
        // Start the spawning process
        StartCoroutine(SpawnEnemyWithTelegraph(_enemyName, _position, _rotation, telegraphDuration));
        
        // Return null since the actual enemy isn't spawned yet
        return null;
    }
    
    /// <summary>
    /// Spawn an enemy immediately without telegraph effect
    /// </summary>
    /// <param name="_enemyPrefab">The enemy prefab to spawn</param>
    /// <param name="_position">Position to spawn at</param>
    /// <param name="_rotation">Rotation to spawn with</param>
    /// <returns>The spawned enemy</returns>
    public BaseEnemy SpawnEnemyImmediate(GameObject _enemyPrefab, Vector3 _position, Quaternion _rotation)
    {
        if (_enemyPrefab == null)
        {
            Debug.LogError("Enemy prefab is null. Cannot spawn enemy.");
            return null;
        }
        
        string enemyName = _enemyPrefab.name;
        
        // Register the prefab if not already registered
        if (!m_EnemyPrefabs.ContainsKey(enemyName))
        {
            RegisterEnemyPrefab(_enemyPrefab);
        }
        
        // Spawn the enemy directly
        BaseEnemy spawnedEnemy = CreateEnemy(enemyName, _position, _rotation);
        
        if (spawnedEnemy != null)
        {
            OnEnemySpawned?.Invoke(spawnedEnemy);
        }
        
        return spawnedEnemy;
    }
    
    /// <summary>
    /// Cancel any active telegraph at the specified position
    /// </summary>
    public void CancelTelegraphAtPosition(Vector3 _position)
    {
        if (m_ActiveTelegraphs.TryGetValue(_position, out Coroutine telegraphCoroutine))
        {
            if (telegraphCoroutine != null)
            {
                StopCoroutine(telegraphCoroutine);
            }
            
            m_ActiveTelegraphs.Remove(_position);
        }
    }
    
    /// <summary>
    /// Cancel all active telegraphs
    /// </summary>
    public void CancelAllTelegraphs()
    {
        foreach (var telegraphCoroutine in m_ActiveTelegraphs.Values)
        {
            if (telegraphCoroutine != null)
            {
                StopCoroutine(telegraphCoroutine);
            }
        }
        
        m_ActiveTelegraphs.Clear();
    }
    #endregion
    
    #region Private Methods
    private IEnumerator SpawnEnemyWithTelegraph(string _enemyName, Vector3 _position, Quaternion _rotation, float _telegraphDuration)
    {
        // Raise event that spawning has started (for any listeners that might want to know)
        OnEnemySpawnStarted?.Invoke(null);
        
        // Get or create the telegraph config for this enemy type
        EnemyTelegraphConfig telegraphConfig = GetTelegraphConfig(_enemyName);
        
        // Spawn the telegraph VFX
        VFXFlyweight telegraphVfx = VFXFlyweightHelper.SpawnVfxAtPosition(
            telegraphConfig.telegraphVfx,
            _position + telegraphConfig.vfxOffset,
            _rotation,
            telegraphConfig.vfxScale
        );
        
        // Play telegraph sound if available
        if (telegraphConfig.telegraphSound != null)
        {
            AudioSource.PlayClipAtPoint(telegraphConfig.telegraphSound, _position);
        }
        
        // Store the coroutine for possible cancellation
        m_ActiveTelegraphs[_position] = StartCoroutine(SpawnEnemyAfterTelegraph(_enemyName, _position, _rotation, _telegraphDuration, telegraphVfx));
        
        yield return null;
    }
    
    private IEnumerator SpawnEnemyAfterTelegraph(string _enemyName, Vector3 _position, Quaternion _rotation, float _telegraphDuration, VFXFlyweight _telegraphVfx)
    {
        // Wait for the telegraph duration
        yield return new WaitForSeconds(_telegraphDuration);
        
        // The telegraph is complete, remove from active telegraphs
        m_ActiveTelegraphs.Remove(_position);
        
        // Spawn the actual enemy
        BaseEnemy spawnedEnemy = CreateEnemy(_enemyName, _position, _rotation);
        
        // Notify about the spawned enemy
        if (spawnedEnemy != null)
        {
            OnEnemySpawned?.Invoke(spawnedEnemy);
            
            // Subscribe to the enemy's death event
            spawnedEnemy.OnDeath += HandleEnemyDeath;
        }
    }
    
    private BaseEnemy CreateEnemy(string enemyName, Vector3 position, Quaternion rotation)
    {
        BaseEnemy enemy = null;
        
        if (m_UseObjectPool)
        {
            // Try to get an inactive enemy from the pool
            enemy = GetEnemyFromPool(enemyName);
            
            if (enemy == null && m_ExpandPoolIfNeeded)
            {
                // If the pool is empty and we can expand, create a new enemy
                enemy = InstantiateNewEnemy(enemyName, position, rotation);
                
                if (enemy != null)
                {
                    // Add to the pool
                    if (!m_EnemyPools.ContainsKey(enemyName))
                    {
                        m_EnemyPools[enemyName] = new List<BaseEnemy>();
                    }
                    
                    m_EnemyPools[enemyName].Add(enemy);
                }
            }
            
            if (enemy != null)
            {
                // Reset the enemy's position, rotation, and activate it
                enemy.transform.position = position;
                enemy.transform.rotation = rotation;
                enemy.gameObject.SetActive(true);
            }
        }
        else
        {
            // If not using object pooling, just instantiate a new enemy
            enemy = InstantiateNewEnemy(enemyName, position, rotation);
        }
        
        return enemy;
    }
    
    private BaseEnemy GetEnemyFromPool(string _enemyName)
    {
        if (!m_EnemyPools.ContainsKey(_enemyName))
        {
            return null;
        }
        
        // Find an inactive enemy in the pool
        foreach (BaseEnemy enemy in m_EnemyPools[_enemyName])
        {
            if (enemy != null && !enemy.gameObject.activeInHierarchy)
            {
                return enemy;
            }
        }
        
        return null;
    }
    
    private BaseEnemy InstantiateNewEnemy(string _enemyName, Vector3 _position, Quaternion _rotation)
    {
        if (!m_EnemyPrefabs.ContainsKey(_enemyName))
        {
            Debug.LogError($"Enemy prefab '{_enemyName}' is not registered.");
            return null;
        }
        
        GameObject enemyPrefab = m_EnemyPrefabs[_enemyName];
        GameObject enemyInstance = Instantiate(enemyPrefab, _position, _rotation);
        BaseEnemy baseEnemy = enemyInstance.GetComponent<BaseEnemy>();
        
        if (baseEnemy == null)
        {
            Debug.LogError($"Prefab '{_enemyName}' does not have a BaseEnemy component.");
            Destroy(enemyInstance);
            return null;
        }
        
        return baseEnemy;
    }
    
    private void HandleEnemyDeath(IEnemy _enemy)
    {
        if (_enemy is BaseEnemy baseEnemy)
        {
            // Unsubscribe from the death event
            baseEnemy.OnDeath -= HandleEnemyDeath;
            
            // Notify any listeners about the enemy's death
            OnEnemyDeath?.Invoke(baseEnemy);
            
            if (m_UseObjectPool)
            {
                // Instead of destroying, return to pool after delay
                StartCoroutine(ReturnToPoolAfterDelay(baseEnemy, 3f));
            }
        }
    }
    
    private IEnumerator ReturnToPoolAfterDelay(BaseEnemy _enemy, float _delay)
    {
        // Wait for the delay (allows death animations/effects to finish)
        yield return new WaitForSeconds(_delay);
        
        if (_enemy != null)
        {
            // Deactivate the enemy to return it to the pool
            _enemy.gameObject.SetActive(false);
        }
    }
    
    private void InitializePool(string _enemyName, GameObject _prefab, int _poolSize)
    {
        if (!m_EnemyPools.ContainsKey(_enemyName))
        {
            m_EnemyPools[_enemyName] = new List<BaseEnemy>();
            
            // Create a parent object for this enemy type's pool
            GameObject poolParent = new GameObject($"{_enemyName}Pool");
            poolParent.transform.SetParent(transform);
            
            // Pre-instantiate enemies for the pool
            for (int i = 0; i < _poolSize; i++)
            {
                GameObject enemyInstance = Instantiate(_prefab, Vector3.zero, Quaternion.identity);
                BaseEnemy baseEnemy = enemyInstance.GetComponent<BaseEnemy>();
                
                if (baseEnemy != null)
                {
                    // Add to the pool and deactivate
                    m_EnemyPools[_enemyName].Add(baseEnemy);
                    enemyInstance.transform.SetParent(poolParent.transform);
                    enemyInstance.SetActive(false);
                    
                    // Add unique ID to the object name
                    enemyInstance.name = $"{_enemyName}_Pool_{i:D4}";
                }
                else
                {
                    Debug.LogError($"Prefab '{_enemyName}' does not have a BaseEnemy component.");
                    Destroy(enemyInstance);
                }
            }
            
            if (m_ShowDebugMessages)
            {
                Debug.Log($"Initialized pool for {_enemyName} with {_poolSize} instances");
            }
        }
    }
    
    private EnemyTelegraphConfig GetTelegraphConfig(string _enemyName)
    {
        // Try to get custom config for this enemy type
        if (m_CustomTelegraphConfigs.TryGetValue(_enemyName, out EnemyTelegraphConfig config))
        {
            return config;
        }
        
        // Fall back to default config
        return new EnemyTelegraphConfig(
            m_DefaultTelegraphVfx,
            m_TelegraphVfxOffset,
            Vector3.one,
            m_TelegraphSound
        );
    }
    #endregion
}

/// <summary>
/// Configuration class for enemy telegraph effects
/// </summary>
[System.Serializable]
public class EnemyTelegraphConfig
{
    public VFXFlyweightSettings telegraphVfx;
    public Vector3 vfxOffset;
    public Vector3 vfxScale;
    public AudioClip telegraphSound;
    
    public EnemyTelegraphConfig(VFXFlyweightSettings vfx, Vector3 offset, Vector3 scale, AudioClip sound)
    {
        telegraphVfx = vfx;
        vfxOffset = offset;
        vfxScale = scale;
        telegraphSound = sound;
    }
} 