using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages and tracks all active enemies in the scene, providing utility methods for finding and filtering enemies.
/// Works closely with EnemyFactory to maintain a registry of all spawned enemies.
/// </summary>
public class EnemyManager : MonoBehaviour
{
    #region Constants
    
    private const float c_CleanupInterval = 5f;
    
    #endregion
    
    #region Serialized Fields
    
    [Header("Configuration")]
    [SerializeField] private bool m_AutoRegisterWithFactory = true;
    [SerializeField] private bool m_ShowDebugInfo = false;
    
    [Header("Events")]
    public UnityEvent<BaseEnemy> OnEnemyRegistered = new();
    public UnityEvent<BaseEnemy> OnEnemyUnregistered = new();
    
    #endregion
    
    #region Private Fields
    
    private List<BaseEnemy> m_ActiveEnemies = new List<BaseEnemy>();
    private Dictionary<string, List<BaseEnemy>> m_EnemiesByType = new Dictionary<string, List<BaseEnemy>>();
    private float m_LastCleanupTime;
    
    #endregion
    
    #region Singleton Pattern
    
    private static EnemyManager s_Instance;
    
    public static EnemyManager Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = FindObjectOfType<EnemyManager>();
                
                if (s_Instance == null)
                {
                    GameObject managerObject = new GameObject("EnemyManager");
                    s_Instance = managerObject.AddComponent<EnemyManager>();
                }
            }
            
            return s_Instance;
        }
    }
    
    #endregion
    
    #region Properties
    
    /// <summary>
    /// Gets the total count of active enemies
    /// </summary>
    public int ActiveEnemyCount => m_ActiveEnemies.Count;
    
    /// <summary>
    /// Gets a read-only list of all active enemies
    /// </summary>
    public IReadOnlyList<BaseEnemy> ActiveEnemies => m_ActiveEnemies.AsReadOnly();
    
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
    }
    
    private void Start()
    {
        if (m_AutoRegisterWithFactory)
        {
            RegisterWithFactory();
        }
        
        // Find any existing enemies that might have been in the scene before manager was created
        FindAndRegisterExistingEnemies();
    }
    
    private void Update()
    {
        float currentTime = Time.time;
        
        // Periodically clean up destroyed or inactive enemies
        if (currentTime - m_LastCleanupTime > c_CleanupInterval)
        {
            CleanupInactiveEnemies();
            m_LastCleanupTime = currentTime;
        }
    }
    
    private void OnDestroy()
    {
        if (EnemyFactory.Instance != null)
        {
            EnemyFactory.Instance.OnEnemySpawned.RemoveListener(RegisterEnemy);
            EnemyFactory.Instance.OnEnemyDeath.RemoveListener(UnregisterEnemy);
        }
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Manually register with the EnemyFactory to receive spawn and death events
    /// </summary>
    public void RegisterWithFactory()
    {
        if (EnemyFactory.Instance != null)
        {
            EnemyFactory.Instance.OnEnemySpawned.AddListener(RegisterEnemy);
            EnemyFactory.Instance.OnEnemyDeath.AddListener(UnregisterEnemy);
            
            if (m_ShowDebugInfo)
            {
                Debug.Log("EnemyManager registered with EnemyFactory");
            }
        }
        else
        {
            Debug.LogWarning("EnemyFactory instance not found. EnemyManager will not track factory-spawned enemies.");
        }
    }
    
    /// <summary>
    /// Manually register an enemy with the manager
    /// </summary>
    /// <param name="_enemy">The enemy to register</param>
    public void RegisterEnemy(BaseEnemy _enemy)
    {
        if (_enemy == null) return;
        
        // Don't register the same enemy twice
        if (m_ActiveEnemies.Contains(_enemy)) return;
        
        // Add to active enemies list
        m_ActiveEnemies.Add(_enemy);
        
        // Register by type
        string enemyType = _enemy.GetType().Name;
        if (!m_EnemiesByType.ContainsKey(enemyType))
        {
            m_EnemiesByType[enemyType] = new List<BaseEnemy>();
        }
        m_EnemiesByType[enemyType].Add(_enemy);
        
        // Subscribe to death event
        _enemy.OnDeath += HandleEnemyDeath;
        
        // Trigger event
        OnEnemyRegistered?.Invoke(_enemy);
        
        if (m_ShowDebugInfo)
        {
            Debug.Log($"Registered enemy: {_enemy.name} (Total: {m_ActiveEnemies.Count})");
        }
    }
    
    /// <summary>
    /// Manually unregister an enemy from the manager
    /// </summary>
    /// <param name="_enemy">The enemy to unregister</param>
    public void UnregisterEnemy(BaseEnemy _enemy)
    {
        if (_enemy == null) return;
        
        // Unsubscribe from death event
        _enemy.OnDeath -= HandleEnemyDeath;
        
        // Remove from active enemies list
        m_ActiveEnemies.Remove(_enemy);
        
        // Remove from type dictionary
        string enemyType = _enemy.GetType().Name;
        if (m_EnemiesByType.ContainsKey(enemyType))
        {
            m_EnemiesByType[enemyType].Remove(_enemy);
        }
        
        // Trigger event
        OnEnemyUnregistered?.Invoke(_enemy);
        
        if (m_ShowDebugInfo)
        {
            Debug.Log($"Unregistered enemy: {_enemy.name} (Total: {m_ActiveEnemies.Count})");
        }
    }
    
    /// <summary>
    /// Find the nearest enemy to a position
    /// </summary>
    /// <param name="_position">The reference position</param>
    /// <returns>The nearest enemy or null if no enemies exist</returns>
    public BaseEnemy FindNearestEnemy(Vector3 _position)
    {
        if (m_ActiveEnemies.Count == 0) return null;
        
        return m_ActiveEnemies
            .Where(e => e != null && !e.IsDead)
            .OrderBy(e => Vector3.Distance(e.transform.position, _position))
            .FirstOrDefault();
    }
    
    /// <summary>
    /// Find all enemies within a radius of a position
    /// </summary>
    /// <param name="_position">The center position</param>
    /// <param name="_radius">The search radius</param>
    /// <returns>List of enemies within the radius</returns>
    public List<BaseEnemy> FindEnemiesInRadius(Vector3 _position, float _radius)
    {
        return m_ActiveEnemies
            .Where(e => e != null && !e.IsDead && 
                   Vector3.Distance(e.transform.position, _position) <= _radius)
            .ToList();
    }
    
    /// <summary>
    /// Find all enemies of a specific type
    /// </summary>
    /// <typeparam name="T">The enemy type to find</typeparam>
    /// <returns>List of enemies of the specified type</returns>
    public List<T> FindEnemiesByType<T>() where T : BaseEnemy
    {
        return m_ActiveEnemies
            .Where(e => e != null && !e.IsDead && e is T)
            .Cast<T>()
            .ToList();
    }
    
    /// <summary>
    /// Find all elite enemies
    /// </summary>
    /// <returns>List of elite enemies</returns>
    public List<BaseEnemy> FindEliteEnemies()
    {
        return m_ActiveEnemies
            .Where(e => e != null && !e.IsDead && e.IsElite)
            .ToList();
    }
    
    /// <summary>
    /// Gets a count of active enemies by type
    /// </summary>
    /// <param name="_enemyType">The type name (class name) of the enemy</param>
    /// <returns>Count of active enemies of that type</returns>
    public int GetEnemyCountByType(string _enemyType)
    {
        if (!m_EnemiesByType.ContainsKey(_enemyType))
        {
            return 0;
        }
        
        return m_EnemiesByType[_enemyType].Count(e => e != null && !e.IsDead);
    }
    
    /// <summary>
    /// Apply damage to all active enemies
    /// </summary>
    /// <param name="_damage">Amount of damage to apply</param>
    /// <param name="_excludeBosses">Whether to exclude boss enemies</param>
    public void DamageAllEnemies(float _damage, bool _excludeBosses = false)
    {
        CombatUtilities.DamageAllEnemies(_damage, _excludeBosses);
    }
    
    /// <summary>
    /// Pushes all enemies away from a position
    /// </summary>
    /// <param name="_position">The center position to push from</param>
    /// <param name="_radius">Radius of effect</param>
    public void PushEnemiesAway(Vector3 _position, float _radius)
    {
        Vector2 position2D = new Vector2(_position.x, _position.z);
        CombatUtilities.PushEnemiesAway(position2D, _radius);
    }
    
    /// <summary>
    /// Kills all active enemies of a specific type
    /// </summary>
    /// <param name="_enemyType">The type name of enemies to kill</param>
    /// <param name="_excludeBosses">Whether to exclude boss enemies</param>
    /// <returns>Number of enemies killed</returns>
    public int KillAllEnemiesByType(string _enemyType, bool _excludeBosses = true)
    {
        if (!m_EnemiesByType.ContainsKey(_enemyType))
        {
            return 0;
        }
        
        int killCount = 0;
        
        // Create a copy of the list to avoid modification during iteration
        List<BaseEnemy> enemiesToKill = new List<BaseEnemy>(m_EnemiesByType[_enemyType]);
        
        foreach (BaseEnemy enemy in enemiesToKill)
        {
            if (enemy == null || enemy.IsDead) continue;
            
            // Skip bosses if specified
            if (_excludeBosses && enemy.Data != null && enemy.Data.isBoss) continue;
            
            // Create a hit context with enough damage to kill
            HitContext hitContext = HitContext.Create(
                Vector3.zero,
                enemy.transform.position,
                enemy.MaxHealth * 2f, // Ensure it's enough to kill
                null
            );
            
            // Apply the fatal damage
            enemy.OnHitWithContext(hitContext);
            killCount++;
        }
        
        return killCount;
    }
    
    #endregion
    
    #region Private Methods
    
    private void FindAndRegisterExistingEnemies()
    {
        BaseEnemy[] existingEnemies = FindObjectsOfType<BaseEnemy>();
        
        foreach (BaseEnemy enemy in existingEnemies)
        {
            RegisterEnemy(enemy);
        }
        
        if (m_ShowDebugInfo && existingEnemies.Length > 0)
        {
            Debug.Log($"Found and registered {existingEnemies.Length} existing enemies");
        }
    }
    
    private void CleanupInactiveEnemies()
    {
        // Create a copy of the list to avoid modification during iteration
        List<BaseEnemy> enemiesToCheck = new List<BaseEnemy>(m_ActiveEnemies);
        
        int removedCount = 0;
        
        foreach (BaseEnemy enemy in enemiesToCheck)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy)
            {
                UnregisterEnemy(enemy);
                removedCount++;
            }
        }
        
        if (m_ShowDebugInfo && removedCount > 0)
        {
            Debug.Log($"Cleaned up {removedCount} inactive enemies");
        }
    }
    
    private void HandleEnemyDeath(IEnemy _enemy)
    {
        if (_enemy is BaseEnemy baseEnemy)
        {
            // No need to immediately unregister as the cleanup will handle it
            // However, if immediate tracking is needed, uncomment:
            // UnregisterEnemy(baseEnemy);
        }
    }
    
    #endregion
    
    #region Debug Tools
    
    [ContextMenu("Log Enemy Stats")]
    private void LogEnemyStats()
    {
        Debug.Log($"===== Enemy Manager Stats =====");
        Debug.Log($"Total Active Enemies: {m_ActiveEnemies.Count}");
        
        foreach (var kvp in m_EnemiesByType)
        {
            int liveCount = kvp.Value.Count(e => e != null && !e.IsDead);
            Debug.Log($"- {kvp.Key}: {liveCount} active");
        }
        
        int eliteCount = m_ActiveEnemies.Count(e => e != null && !e.IsDead && e.IsElite);
        Debug.Log($"Elite Enemies: {eliteCount}");
    }
    
    #endregion
} 