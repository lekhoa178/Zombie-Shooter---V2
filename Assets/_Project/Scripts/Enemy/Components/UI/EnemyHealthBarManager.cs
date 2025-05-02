using System.Collections.Generic;
using UnityEngine;
using Flyweight;
using UnityEngine.UI;
using UnityUtils;

/// <summary>
/// Manages health bar settings and initialization for enemies using the Flyweight pattern
/// </summary>
public class EnemyHealthBarManager : PersistentSingleton<EnemyHealthBarManager>
{
    public static EnemyHealthBarManager Instance { get; private set; }
    
    [Header("Health Bar Settings")]
    [SerializeField] private HealthBarFlyweightSettings m_DefaultHealthBarSettings;
    [SerializeField] private HealthBarFlyweightSettings m_BossHealthBarSettings;
    
    [Header("Default Settings")]
    [SerializeField] private bool m_ApplyToAllEnemies = true;
    [SerializeField] private bool m_AutoAddToNewEnemies = true;
    
    [Header("Boss Health Bar HUD")]
    [SerializeField] private RectTransform m_BossHealthBarContainer;
    [SerializeField] private int m_MaxVisibleBossHealthBars = 3;
    [SerializeField] private float m_BossBarSpacing = 10f;
    
    // Dictionary to track active boss health bars
    private Dictionary<EnemyHealthBarComponent, HealthBarFlyweight> m_ActiveBossHealthBars = new Dictionary<EnemyHealthBarComponent, HealthBarFlyweight>();
    
    protected override void Awake()
    {
        base.Awake();
        
        // Initialize the helper with our default settings
        if (m_DefaultHealthBarSettings != null)
        {
            HealthBarHelper.InitializeDefaultSettings(m_DefaultHealthBarSettings);
        }
        else
        {
            Debug.LogWarning("No default health bar settings assigned to EnemyHealthBarManager!");
        }
        
        // Validate boss health bar settings
        if (m_BossHealthBarSettings == null)
        {
            Debug.LogWarning("No boss health bar settings assigned. Using default settings for bosses.");
            m_BossHealthBarSettings = m_DefaultHealthBarSettings;
        }
        
        // Validate boss health bar container
        if (m_BossHealthBarContainer == null)
        {
            Debug.LogError("Boss health bar container is not assigned! Please assign a RectTransform in the inspector.");
        }
    }
    
    private void Start()
    {
        if (m_ApplyToAllEnemies)
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
        if (healthBarComponent == null && m_AutoAddToNewEnemies)
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
        
        return HealthBarHelper.GetHealthBar(enemy, m_DefaultHealthBarSettings, offset);
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
        ClearAllBossHealthBars();
    }
    
    /// <summary>
    /// Register a boss health bar to display on the HUD
    /// </summary>
    public void RegisterBossHealthBar(EnemyHealthBarComponent bossComponent)
    {
        if (bossComponent == null || !bossComponent.IsBossHealthBar) return;
        
        // If we already have this boss registered, return
        if (m_ActiveBossHealthBars.ContainsKey(bossComponent)) return;
        
        // Check if we're over the limit of visible boss bars
        if (m_ActiveBossHealthBars.Count >= m_MaxVisibleBossHealthBars)
        {
            Debug.LogWarning($"Maximum number of boss health bars ({m_MaxVisibleBossHealthBars}) reached. Not displaying health bar for {bossComponent.BossDisplayName}.");
            return;
        }
        
        // Create a boss health bar instance using the Flyweight pattern
        if (m_BossHealthBarContainer != null)
        {
            // Spawn a health bar using the boss settings
            HealthBarFlyweight bossBar = FlyweightFactory.Spawn(m_BossHealthBarSettings) as HealthBarFlyweight;
            
            if (bossBar != null)
            {
                // Place the health bar in the UI container
                bossBar.transform.SetParent(m_BossHealthBarContainer, false);
                
                // Configure transforms for UI display
                RectTransform rectTransform = bossBar.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchorMin = new Vector2(0, 0);
                    rectTransform.anchorMax = new Vector2(1, 0);
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    rectTransform.sizeDelta = new Vector2(0, 130);
                    rectTransform.localScale = Vector3.one;
                }
                
                // Add a layout element for proper sizing in the container
                LayoutElement layoutElement = bossBar.gameObject.AddComponent<LayoutElement>();
                layoutElement.minHeight = 130;
                layoutElement.flexibleWidth = 0.7f;
                
                // Initialize without setting parent to enemy transform
                bossBar.InitializeAsBossBar(bossComponent.Enemy, bossComponent.BossBarColor, bossComponent.BossDisplayName, bossComponent.BossIcon);
                
                // Store the reference
                m_ActiveBossHealthBars.Add(bossComponent, bossBar);
            }
            else
            {
                Debug.LogError("Failed to spawn boss health bar!");
            }
            
            // Update the layout of all boss bars
            UpdateBossHealthBarLayout();
        }
        else
        {
            Debug.LogError("Cannot add boss health bar: container is not assigned!");
        }
    }
    
    /// <summary>
    /// Unregister a boss health bar from the HUD
    /// </summary>
    public void UnregisterBossHealthBar(EnemyHealthBarComponent bossComponent)
    {
        if (bossComponent == null) return;
        
        if (m_ActiveBossHealthBars.TryGetValue(bossComponent, out HealthBarFlyweight bossBar))
        {
            // Return to pool
            FlyweightFactory.ReturnToPool(bossBar);
            m_ActiveBossHealthBars.Remove(bossComponent);
            
            // Update the layout
            UpdateBossHealthBarLayout();
        }
    }
    
    /// <summary>
    /// Clear all boss health bars from the HUD
    /// </summary>
    public void ClearAllBossHealthBars()
    {
        foreach (var bossBar in m_ActiveBossHealthBars.Values)
        {
            FlyweightFactory.ReturnToPool(bossBar);
        }
        
        m_ActiveBossHealthBars.Clear();
    }
    
    /// <summary>
    /// Updates the layout of boss health bars on the HUD
    /// </summary>
    private void UpdateBossHealthBarLayout()
    {
        if (m_BossHealthBarContainer)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_BossHealthBarContainer);
        }
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            ClearAllHealthBars();
        }
    }
} 