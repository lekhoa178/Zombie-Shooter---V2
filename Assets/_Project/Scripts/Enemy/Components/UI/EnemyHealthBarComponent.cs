using UnityEngine;
using Flyweight;

/// <summary>
/// Add this component to enemies that need health bars
/// </summary>
[RequireComponent(typeof(BaseEnemy))]
public class EnemyHealthBarComponent : MonoBehaviour
{
    #region Serialized Fields
    [Header("Health Bar Configuration")]
    [SerializeField] private bool m_UseHealthBar = true;
    [SerializeField] private bool m_AutoInitialize = true;
    [SerializeField] private Vector3 m_PositionOffset = new Vector3(0, 2f, 0);
    [SerializeField] private HealthBarFlyweightSettings m_CustomSettings;
    
    [Header("Boss Health Bar")]
    [SerializeField] private bool m_IsBossHealthBar = false;
    [SerializeField] private Color m_BossBarColor = Color.red;
    [SerializeField] private string m_BossDisplayName = "";
    [SerializeField] private Sprite m_BossIcon;
    #endregion
    
    #region Private Fields
    private BaseEnemy m_Enemy;
    private HealthBarFlyweight m_HealthBar;
    private bool m_RegisteredWithHUD = false;
    #endregion
    
    #region Properties
    /// <summary>
    /// Gets whether this enemy uses a boss health bar
    /// </summary>
    public bool IsBossHealthBar => m_IsBossHealthBar;
    
    /// <summary>
    /// Gets the boss display name (if empty, uses enemy name)
    /// </summary>
    public string BossDisplayName => !string.IsNullOrEmpty(m_BossDisplayName) ? m_BossDisplayName : m_Enemy?.name ?? gameObject.name;
    
    /// <summary>
    /// Gets the boss icon
    /// </summary>
    public Sprite BossIcon => m_BossIcon;
    
    /// <summary>
    /// Gets the boss bar color
    /// </summary>
    public Color BossBarColor => m_BossBarColor;
    
    /// <summary>
    /// Gets the associated enemy
    /// </summary>
    public BaseEnemy Enemy => m_Enemy;
    
    /// <summary>
    /// Gets or sets the position offset for world-space health bars
    /// </summary>
    public Vector3 PositionOffset
    {
        get => m_PositionOffset;
        set => m_PositionOffset = value;
    }
    #endregion
    
    #region Unity Lifecycle
    private void Awake()
    {
        m_Enemy = GetComponent<BaseEnemy>();
    }
    
    private void Start()
    {
        if (m_AutoInitialize && m_UseHealthBar)
        {
            InitializeHealthBar();
        }
    }
    
    private void OnDestroy()
    {
        // Return the health bar to the pool when destroyed
        CleanupHealthBar();
    }
    
    // private void OnDisable()
    // {
    //     // Return the health bar to the pool when disabled
    //     CleanupHealthBar();
    // }
    #endregion
    
    #region Public Methods
    /// <summary>
    /// Initializes the health bar
    /// </summary>
    public void InitializeHealthBar()
    {
        if (!m_UseHealthBar || m_Enemy == null) return;
        
        // Handle boss health bars
        if (m_IsBossHealthBar)
        {
            // Register with the manager which will handle the UI display
            if (!m_RegisteredWithHUD)
            {
                EnemyHealthBarManager.Instance?.RegisterBossHealthBar(this);
                m_RegisteredWithHUD = true;
            }
        }
        else
        {
            // Regular world-space health bar
            if (m_HealthBar == null)
            {
                m_HealthBar = HealthBarHelper.GetHealthBar(m_Enemy, m_CustomSettings, m_PositionOffset);
            }
            else
            {
                // Bar already exists, make sure it's positioned correctly
                UpdatePosition();
            }
        }
    }
    
    /// <summary>
    /// Updates the health bar position (useful for teleporting enemies)
    /// </summary>
    public void UpdatePosition()
    {
        if (m_IsBossHealthBar)
        {
            // Boss health bars are on the HUD, no need to update position
            return;
        }
        
        if (m_HealthBar != null)
        {
            m_HealthBar.UpdatePosition();
        }
        else
        {
            HealthBarHelper.UpdateHealthBarPosition(m_Enemy);
        }
    }
    
    /// <summary>
    /// Enable or disable the health bar
    /// </summary>
    public void SetActive(bool active)
    {
        m_UseHealthBar = active;
        
        if (active && m_Enemy != null)
        {
            if (m_IsBossHealthBar)
            {
                if (!m_RegisteredWithHUD)
                {
                    EnemyHealthBarManager.Instance?.RegisterBossHealthBar(this);
                    m_RegisteredWithHUD = true;
                }
            }
            else if (m_HealthBar == null)
            {
                // Regular health bar initialization
                InitializeHealthBar();
            }
        }
        else
        {
            CleanupHealthBar();
        }
    }
    
    /// <summary>
    /// Toggle between boss and regular health bar
    /// </summary>
    public void SetBossHealthBar(bool isBoss)
    {
        if (m_IsBossHealthBar == isBoss) return;
        
        // Clean up existing health bar
        CleanupHealthBar();
        
        // Set new state
        m_IsBossHealthBar = isBoss;
        
        // Re-initialize with new type
        if (m_UseHealthBar && m_Enemy != null)
        {
            InitializeHealthBar();
        }
    }
    
    /// <summary>
    /// Get current health information for UI display
    /// </summary>
    public (float currentHealth, float maxHealth, float healthPercent) GetHealthInfo()
    {
        if (m_Enemy == null) return (0, 0, 0);
        
        float current = m_Enemy.CurrentHealth;
        float max = m_Enemy.MaxHealth;
        float percent = max > 0 ? current / max : 0;
        
        return (current, max, percent);
    }
    #endregion
    
    #region Private Methods
    /// <summary>
    /// Cleanup health bar resources
    /// </summary>
    private void CleanupHealthBar()
    {
        if (m_IsBossHealthBar && m_RegisteredWithHUD)
        {
            // Unregister from HUD
            EnemyHealthBarManager.Instance?.UnregisterBossHealthBar(this);
            m_RegisteredWithHUD = false;
        }
        else if (m_HealthBar != null)
        {
            // Return world-space health bar to pool
            m_HealthBar.transform.parent = null; // Detach from enemy
            HealthBarHelper.ReleaseHealthBar(m_Enemy);
            m_HealthBar = null;
        }
    }
    #endregion
} 