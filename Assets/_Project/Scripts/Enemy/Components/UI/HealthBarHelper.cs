using UnityEngine;
using Flyweight;
using System.Collections.Generic;

/// <summary>
/// Provides helper methods for spawning and managing health bars using the Flyweight pattern
/// </summary>
public static class HealthBarHelper
{
    private static HealthBarFlyweightSettings s_DefaultSettings;
    private static Dictionary<BaseEnemy, HealthBarFlyweight> s_ActiveHealthBars = new Dictionary<BaseEnemy, HealthBarFlyweight>();
    
    /// <summary>
    /// Initializes the default settings for health bars
    /// </summary>
    /// <param name="settings">The ScriptableObject settings to use</param>
    public static void InitializeDefaultSettings(HealthBarFlyweightSettings settings)
    {
        s_DefaultSettings = settings;
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
        HealthBarFlyweightSettings healthBarSettings = settings ?? s_DefaultSettings;
        
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
        if (s_ActiveHealthBars.TryGetValue(enemy, out var existingHealthBar))
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
        
        // Initialize the health bar (regular world-space mode)
        healthBar.Initialize(enemy, offset);
        
        // Track active health bar
        s_ActiveHealthBars[enemy] = healthBar;
        
        return healthBar;
    }
    
    /// <summary>
    /// Returns a health bar to the pool
    /// </summary>
    /// <param name="enemy">The enemy whose health bar should be released</param>
    public static void ReleaseHealthBar(BaseEnemy enemy)
    {
        if (enemy == null) return;
        
        if (s_ActiveHealthBars.TryGetValue(enemy, out var healthBar))
        {
            // Remove from active dictionary
            s_ActiveHealthBars.Remove(enemy);
            
            // Return to pool
            FlyweightFactory.ReturnToPool(healthBar);
        }
    }
    
    /// <summary>
    /// Updates the position of an enemy's health bar
    /// </summary>
    public static void UpdateHealthBarPosition(BaseEnemy enemy)
    {
        if (enemy == null) return;
        
        if (s_ActiveHealthBars.TryGetValue(enemy, out var healthBar))
        {
            healthBar.UpdatePosition();
        }
    }
    
    /// <summary>
    /// Releases all active health bars
    /// </summary>
    public static void ReleaseAllHealthBars()
    {
        foreach (var healthBar in s_ActiveHealthBars.Values)
        {
            FlyweightFactory.ReturnToPool(healthBar);
        }
        
        s_ActiveHealthBars.Clear();
    }
    
    /// <summary>
    /// Gets an existing health bar for an enemy if one exists
    /// </summary>
    public static HealthBarFlyweight GetExistingHealthBar(BaseEnemy enemy)
    {
        if (enemy == null) return null;
        
        if (s_ActiveHealthBars.TryGetValue(enemy, out var healthBar))
        {
            return healthBar;
        }
        
        return null;
    }
} 