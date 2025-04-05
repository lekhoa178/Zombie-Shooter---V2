using UnityEngine;
using Flyweight;
using System.Collections.Generic;

/// <summary>
/// Provides helper methods for spawning and managing health bars using the Flyweight pattern
/// </summary>
public static class HealthBarHelper
{
    private static HealthBarFlyweightSettings _defaultSettings;
    private static Dictionary<BaseEnemy, HealthBarFlyweight> _activeHealthBars = new Dictionary<BaseEnemy, HealthBarFlyweight>();
    
    /// <summary>
    /// Initializes the default settings for health bars
    /// </summary>
    /// <param name="settings">The ScriptableObject settings to use</param>
    public static void InitializeDefaultSettings(HealthBarFlyweightSettings settings)
    {
        _defaultSettings = settings;
    }
    
    /// <summary>
    /// Gets or creates a health bar for an enemy
    /// </summary>
    /// <param name="enemy">The enemy to attach the health bar to</param>
    /// <param name="settings">Optional custom settings (uses default if null)</param>
    /// <param name="offset">Optional position offset from the enemy's transform</param>
    /// <returns>The health bar instance</returns>
    public static HealthBarFlyweight GetHealthBar(
        BaseEnemy enemy, 
        HealthBarFlyweightSettings settings = null, 
        Vector3 offset = default)
    {
        if (enemy == null)
        {
            Debug.LogError("Cannot create health bar for null enemy");
            return null;
        }
        
        // Use provided settings or fall back to default
        HealthBarFlyweightSettings healthBarSettings = settings ?? _defaultSettings;
        
        if (healthBarSettings == null)
        {
            Debug.LogError("No health bar settings provided and no default set");
            return null;
        }
        
        // If offset is default (zero), use a standard up vector
        if (offset == default)
        {
            offset = Vector3.up * 2f;
        }
        
        // Check if enemy already has a health bar
        if (_activeHealthBars.TryGetValue(enemy, out var existingHealthBar))
        {
            return existingHealthBar;
        }
        
        // Get a health bar from the pool
        var healthBar = FlyweightFactory.Spawn(healthBarSettings) as HealthBarFlyweight;
        
        if (healthBar == null)
        {
            Debug.LogError($"Failed to spawn health bar with settings {healthBarSettings.name}");
            return null;
        }
        
        // Initialize the health bar
        healthBar.Initialize(enemy, offset);
        
        // Track active health bar
        _activeHealthBars[enemy] = healthBar;
        
        return healthBar;
    }
    
    /// <summary>
    /// Returns a health bar to the pool
    /// </summary>
    /// <param name="enemy">The enemy whose health bar should be released</param>
    public static void ReleaseHealthBar(BaseEnemy enemy)
    {
        if (enemy == null) return;
        
        if (_activeHealthBars.TryGetValue(enemy, out var healthBar))
        {
            // Remove from active dictionary
            _activeHealthBars.Remove(enemy);
            
            // Return to pool
            FlyweightFactory.ReturnToPool(healthBar);
        }
    }
    
    /// <summary>
    /// Releases all active health bars
    /// </summary>
    public static void ReleaseAllHealthBars()
    {
        // Make a copy of all health bars to avoid modifying during enumeration
        var healthBars = new List<HealthBarFlyweight>(_activeHealthBars.Values);
        
        foreach (var healthBar in healthBars)
        {
            FlyweightFactory.ReturnToPool(healthBar);
        }
        
        _activeHealthBars.Clear();
    }
    
    /// <summary>
    /// Updates the position of an enemy's health bar
    /// </summary>
    /// <param name="enemy">The enemy whose health bar should be updated</param>
    public static void UpdateHealthBarPosition(BaseEnemy enemy)
    {
        if (enemy == null) return;
        
        if (_activeHealthBars.TryGetValue(enemy, out var healthBar))
        {
            // Position will be updated automatically in the next frame by the health bar's update routine
        }
    }
} 