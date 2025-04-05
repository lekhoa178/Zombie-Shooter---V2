using System.Collections.Generic;
using UnityEngine;
using Flyweight;

/// <summary>
/// Manages health bar settings and initialization for enemies using the Flyweight pattern
/// </summary>
public class EnemyHealthBarManager : MonoBehaviour
{
    public static EnemyHealthBarManager Instance { get; private set; }
    
    [Header("Health Bar Settings")]
    [SerializeField] private HealthBarFlyweightSettings defaultHealthBarSettings;
    
    [Header("Default Settings")]
    [SerializeField] private bool applyToAllEnemies = true;
    [SerializeField] private bool autoAddToNewEnemies = true;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Initialize the helper with our default settings
        if (defaultHealthBarSettings != null)
        {
            HealthBarHelper.InitializeDefaultSettings(defaultHealthBarSettings);
        }
        else
        {
            Debug.LogWarning("No default health bar settings assigned to EnemyHealthBarManager!");
        }
    }
    
    private void Start()
    {
        if (applyToAllEnemies)
        {
            // Find all existing enemies and ensure they have health bars
            BaseEnemy[] allEnemies = FindObjectsOfType<BaseEnemy>();
            
            foreach (var enemy in allEnemies)
            {
                AddHealthBarToEnemy(enemy);
            }
        }
    }
    
    /// <summary>
    /// Adds a health bar component to an enemy if needed
    /// </summary>
    public void AddHealthBarToEnemy(BaseEnemy enemy)
    {
        if (enemy == null) return;
        
        EnemyHealthBarComponent healthBarComponent = enemy.GetComponent<EnemyHealthBarComponent>();
        
        // Create component if it doesn't exist
        if (healthBarComponent == null && autoAddToNewEnemies)
        {
            healthBarComponent = enemy.gameObject.AddComponent<EnemyHealthBarComponent>();
        }
        
        // Initialize if the component exists
        if (healthBarComponent != null)
        {
            healthBarComponent.InitializeHealthBar();
        }
    }
    
    /// <summary>
    /// Gets a health bar for an enemy (creates one if needed)
    /// </summary>
    public HealthBarFlyweight GetHealthBar(BaseEnemy enemy, Vector3 offset = default)
    {
        if (enemy == null) return null;
        
        return HealthBarHelper.GetHealthBar(enemy, defaultHealthBarSettings, offset);
    }
    
    /// <summary>
    /// Return a health bar to the pool
    /// </summary>
    public void ReleaseHealthBar(BaseEnemy enemy)
    {
        HealthBarHelper.ReleaseHealthBar(enemy);
    }
    
    /// <summary>
    /// Clear all active health bars (useful for scene transitions)
    /// </summary>
    public void ClearAllHealthBars()
    {
        HealthBarHelper.ReleaseAllHealthBars();
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            ClearAllHealthBars();
            Instance = null;
        }
    }
} 