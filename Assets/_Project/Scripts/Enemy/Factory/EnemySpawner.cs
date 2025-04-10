using System.Collections;
using System.Collections.Generic;
using Flyweight;
using UnityEngine;

/// <summary>
/// Component that uses EnemyFactory to spawn enemies at specified locations with telegraph effects.
/// Place this on an empty GameObject to create an enemy spawn point.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    #region Serialized Fields
    [Header("Enemy Settings")]
    [SerializeField] private GameObject m_EnemyPrefab;
    [SerializeField] private bool m_SpawnOnStart = false;
    [SerializeField] private bool m_SpawnRepeating = false;
    
    [Header("Telegraph Settings")]
    [SerializeField] private float m_TelegraphDuration = 2f;
    [SerializeField] private VFXFlyweightSettings m_CustomTelegraphVfx;
    [SerializeField] private Vector3 m_TelegraphVfxOffset = new Vector3(0, 0.5f, 0);
    [SerializeField] private Vector3 m_TelegraphVfxScale = Vector3.one;
    [SerializeField] private AudioClip m_TelegraphSound;
    [SerializeField] private bool m_UseCustomTelegraph = false;
    
    [Header("Spawn Settings")]
    [SerializeField] private bool m_RandomizeRotation = false;
    [SerializeField] private Vector2 m_RandomPositionRadius = Vector2.zero;
    [SerializeField] private float m_SpawnHeight = 0f;
    
    [Header("Repeat Settings")]
    [SerializeField, Min(0.1f)] private float m_MinSpawnInterval = 5f;
    [SerializeField, Min(0.1f)] private float m_MaxSpawnInterval = 10f;
    [SerializeField] private int m_MaxSpawnCount = 0; // 0 = infinite
    [SerializeField] private bool m_WaitForPreviousEnemyDeath = false;
    
    [Header("Debug")]
    [SerializeField] private bool m_ShowSpawnGizmo = true;
    [SerializeField] private Color m_GizmoColor = Color.red;
    #endregion
    
    #region Private Fields
    private int m_SpawnCount = 0;
    private bool m_SpawningActive = false;
    private BaseEnemy m_LastSpawnedEnemy;
    private Coroutine m_SpawnRoutine;
    private EnemyTelegraphConfig m_TelegraphConfig;
    #endregion
    
    #region Unity Lifecycle
    private void Awake()
    {
        ValidateSettings();
        
        // Create telegraph config if using custom settings
        if (m_UseCustomTelegraph && m_CustomTelegraphVfx != null)
        {
            m_TelegraphConfig = new EnemyTelegraphConfig(
                m_CustomTelegraphVfx,
                m_TelegraphVfxOffset,
                m_TelegraphVfxScale,
                m_TelegraphSound
            );
        }
    }
    
    private void Start()
    {
        // Register any custom telegraph configuration
        if (m_UseCustomTelegraph && m_TelegraphConfig != null && m_EnemyPrefab != null)
        {
            EnemyFactory.Instance.RegisterCustomTelegraph(m_EnemyPrefab.name, m_TelegraphConfig);
        }
        
        // Register the enemy prefab with the factory
        if (m_EnemyPrefab != null)
        {
            EnemyFactory.Instance.RegisterEnemyPrefab(m_EnemyPrefab);
        }
        
        // Start spawning if configured to do so
        if (m_SpawnOnStart)
        {
            StartSpawning();
        }
    }
    
    private void OnDisable()
    {
        StopSpawning();
    }
    
    private void OnDrawGizmos()
    {
        if (!m_ShowSpawnGizmo) return;
        
        Gizmos.color = m_GizmoColor;
        
        // Draw spawn position
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        
        // Draw random spawn radius if used
        if (m_RandomPositionRadius != Vector2.zero)
        {
            // Draw ellipse for the random spawn area
            DrawEllipse(transform.position, m_RandomPositionRadius.x, m_RandomPositionRadius.y, 32);
            
            // Draw spawn height line
            if (m_SpawnHeight != 0)
            {
                Vector3 heightOffset = new Vector3(0, m_SpawnHeight, 0);
                Gizmos.DrawLine(transform.position, transform.position + heightOffset);
                Gizmos.DrawWireSphere(transform.position + heightOffset, 0.2f);
            }
        }
    }
    #endregion
    
    #region Public Methods
    /// <summary>
    /// Starts the spawning process based on the configured settings
    /// </summary>
    public void StartSpawning()
    {
        if (m_SpawningActive || m_EnemyPrefab == null) return;
        
        m_SpawningActive = true;
        
        if (m_SpawnRepeating)
        {
            m_SpawnRoutine = StartCoroutine(SpawnRepeatingCoroutine());
        }
        else
        {
            SpawnSingleEnemy();
        }
    }
    
    /// <summary>
    /// Stops the spawning process
    /// </summary>
    public void StopSpawning()
    {
        m_SpawningActive = false;
        
        if (m_SpawnRoutine != null)
        {
            StopCoroutine(m_SpawnRoutine);
            m_SpawnRoutine = null;
        }
    }
    
    /// <summary>
    /// Spawn a single enemy immediately
    /// </summary>
    public void SpawnSingleEnemy()
    {
        if (m_EnemyPrefab == null) return;
        
        // Get spawn position with optional randomization
        Vector3 spawnPosition = GetSpawnPosition();
        
        // Get spawn rotation with optional randomization
        Quaternion spawnRotation = GetSpawnRotation();
        
        // Use the factory to spawn the enemy with telegraph
        m_LastSpawnedEnemy = EnemyFactory.Instance.SpawnEnemy(
            m_EnemyPrefab,
            spawnPosition,
            spawnRotation,
            m_TelegraphDuration
        );
        
        m_SpawnCount++;
    }
    
    /// <summary>
    /// Change the enemy prefab to spawn
    /// </summary>
    public void SetEnemyPrefab(GameObject prefab)
    {
        if (prefab != null && prefab.GetComponent<BaseEnemy>() != null)
        {
            m_EnemyPrefab = prefab;
            
            // Register the new prefab
            EnemyFactory.Instance.RegisterEnemyPrefab(m_EnemyPrefab);
            
            // Register custom telegraph if needed
            if (m_UseCustomTelegraph && m_TelegraphConfig != null)
            {
                EnemyFactory.Instance.RegisterCustomTelegraph(m_EnemyPrefab.name, m_TelegraphConfig);
            }
        }
        else
        {
            Debug.LogError("Invalid enemy prefab. Must contain a BaseEnemy component.");
        }
    }
    #endregion
    
    #region Private Methods
    private IEnumerator SpawnRepeatingCoroutine()
    {
        while (m_SpawningActive && (m_MaxSpawnCount == 0 || m_SpawnCount < m_MaxSpawnCount))
        {
            // Wait for the previous enemy to die if configured to do so
            if (m_WaitForPreviousEnemyDeath && m_LastSpawnedEnemy != null)
            {
                while (m_LastSpawnedEnemy != null && !m_LastSpawnedEnemy.IsDead && m_LastSpawnedEnemy.gameObject.activeInHierarchy)
                {
                    yield return new WaitForSeconds(0.5f);
                }
            }
            
            // Random delay between spawns
            float spawnInterval = Random.Range(m_MinSpawnInterval, m_MaxSpawnInterval);
            yield return new WaitForSeconds(spawnInterval);
            
            if (!m_SpawningActive) break;
            
            SpawnSingleEnemy();
        }
        
        m_SpawningActive = false;
        m_SpawnRoutine = null;
    }
    
    private Vector3 GetSpawnPosition()
    {
        Vector3 position = transform.position;
        
        // Apply random position offset if configured
        if (m_RandomPositionRadius != Vector2.zero)
        {
            // Generate random point in ellipse
            float angle = Random.Range(0f, Mathf.PI * 2);
            float radius = Random.Range(0f, 1f);
            
            // Convert from polar to Cartesian coordinates
            float x = Mathf.Cos(angle) * m_RandomPositionRadius.x * radius;
            float z = Mathf.Sin(angle) * m_RandomPositionRadius.y * radius;
            
            position += new Vector3(x, 0, z);
        }
        
        // Apply height offset
        position.y += m_SpawnHeight;
        
        return position;
    }
    
    private Quaternion GetSpawnRotation()
    {
        if (m_RandomizeRotation)
        {
            return Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        }
        
        return transform.rotation;
    }
    
    private void DrawEllipse(Vector3 center, float radiusX, float radiusZ, int segments)
    {
        float angle = 0f;
        float angleStep = 2 * Mathf.PI / segments;
        
        Vector3 prevPoint = center + new Vector3(Mathf.Cos(angle) * radiusX, 0, Mathf.Sin(angle) * radiusZ);
        
        for (int i = 0; i < segments + 1; i++)
        {
            angle += angleStep;
            Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * radiusX, 0, Mathf.Sin(angle) * radiusZ);
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }
    
    private void ValidateSettings()
    {
        if (m_EnemyPrefab != null && m_EnemyPrefab.GetComponent<BaseEnemy>() == null)
        {
            Debug.LogError($"Enemy prefab '{m_EnemyPrefab.name}' doesn't have a BaseEnemy component!", this);
            m_EnemyPrefab = null;
        }
        
        if (m_UseCustomTelegraph && m_CustomTelegraphVfx == null)
        {
            Debug.LogWarning("Custom telegraph VFX is enabled but no VFX is assigned, using default instead.", this);
            m_UseCustomTelegraph = false;
        }
        
        if (m_MaxSpawnInterval < m_MinSpawnInterval)
        {
            m_MaxSpawnInterval = m_MinSpawnInterval;
        }
    }
    #endregion
} 