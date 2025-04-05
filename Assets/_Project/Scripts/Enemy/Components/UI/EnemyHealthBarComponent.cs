using UnityEngine;
using Flyweight;

/// <summary>
/// Add this component to enemies that need health bars
/// </summary>
[RequireComponent(typeof(BaseEnemy))]
public class EnemyHealthBarComponent : MonoBehaviour
{
    [Header("Health Bar Configuration")]
    [SerializeField] private bool useHealthBar = true;
    [SerializeField] private bool autoInitialize = true;
    [SerializeField] private Vector3 positionOffset = new Vector3(0, 2f, 0);
    [SerializeField] private HealthBarFlyweightSettings customSettings;
    
    private BaseEnemy _enemy;
    private HealthBarFlyweight _healthBar;
    
    private void Awake()
    {
        _enemy = GetComponent<BaseEnemy>();
    }
    
    private void Start()
    {
        if (autoInitialize && useHealthBar)
        {
            InitializeHealthBar();
        }
    }
    
    /// <summary>
    /// Initializes the health bar using the Flyweight pattern
    /// </summary>
    public void InitializeHealthBar()
    {
        if (!useHealthBar || _enemy == null) return;
        
        _healthBar = HealthBarHelper.GetHealthBar(_enemy, customSettings, positionOffset);
    }
    
    private void OnDestroy()
    {
        // Return the health bar to the pool when destroyed
        if (_healthBar != null)
        {
            HealthBarHelper.ReleaseHealthBar(_enemy);
            _healthBar = null;
        }
    }
    
    private void OnDisable()
    {
        // Return the health bar to the pool when disabled
        if (_healthBar != null)
        {
            HealthBarHelper.ReleaseHealthBar(_enemy);
            _healthBar = null;
        }
    }
    
    /// <summary>
    /// Updates the health bar position (useful for teleporting enemies)
    /// </summary>
    public void UpdatePosition()
    {
        if (_healthBar != null)
        {
            HealthBarHelper.UpdateHealthBarPosition(_enemy);
        }
    }
    
    /// <summary>
    /// Enable or disable the health bar
    /// </summary>
    public void SetActive(bool active)
    {
        useHealthBar = active;
        
        if (active && _healthBar == null && _enemy != null)
        {
            InitializeHealthBar();
        }
        else if (!active && _healthBar != null)
        {
            HealthBarHelper.ReleaseHealthBar(_enemy);
            _healthBar = null;
        }
    }
} 