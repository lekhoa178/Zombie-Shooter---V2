using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Static utility class that provides combat-related functions for manipulating enemies and projectiles
/// </summary>
public static class CombatUtilities
{
    #region Constants
    
    private const float c_KnockbackForceMultiplier = 10f;
    private const float c_DefaultAreaDamageKnockbackStrength = 5f;
    private const float c_DefaultPushStrength = 8f;
    private const int c_MaxProjectilesToConsider = 50;
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Gets active enemies from the EnemyManager or falls back to FindObjectsOfType if the manager isn't available
    /// </summary>
    private static IEnumerable<BaseEnemy> GetActiveEnemies()
    {
        return EnemyManager.Instance ? EnemyManager.Instance.ActiveEnemies : Object.FindObjectsByType<BaseEnemy>(FindObjectsSortMode.None);
    }
    
    #endregion
    
    #region Area Damage
    
    /// <summary>
    /// Applies damage to all enemies within a radius of a position and knocks them back
    /// </summary>
    /// <param name="position">Center position for the area damage</param>
    /// <param name="radius">Radius of the area effect</param>
    /// <param name="damage">Amount of damage to apply</param>
    public static void ApplyAreaDamage(Vector2 position, float radius, float damage)
    {
        // Convert Vector2 to Vector3 for 3D space
        Vector3 damageCenter = new Vector3(position.x, position.y, 0f);
        
        // Get active enemies from manager
        IEnumerable<BaseEnemy> enemies = GetActiveEnemies();
        
        // Track affected enemies for debugging
        int affectedEnemies = 0;
        
        foreach (BaseEnemy enemy in enemies)
        {
            if (enemy == null || enemy.IsDead || enemy.IsInvulnerable)
                continue;
                
            // Check if enemy is within the radius
            float distance = Vector3.Distance(enemy.transform.position, damageCenter);
            if (distance <= radius)
            {
                Vector3 hitDirection = (enemy.transform.position - damageCenter).normalized;
                
                // Create hit context with damage and knockback
                HitContext hitContext = HitContext.Create(
                    hitDirection,
                    enemy.transform.position,
                    damage,
                    null // No instigator
                ).WithPushBack(true, c_DefaultAreaDamageKnockbackStrength, 0.2f);
                
                // Apply damage with context
                enemy.OnHitWithContext(hitContext);
                affectedEnemies++;
            }
        }
        
        // Debug information
        if (Debug.isDebugBuild)
        {
            Debug.Log($"Applied area damage: {damage} at position {position} with radius {radius}. Affected {affectedEnemies} enemies.");
        }
    }
    
    #endregion
    
    #region Push Enemies
    
    /// <summary>
    /// Pushes all enemies away from a central position without dealing damage
    /// </summary>
    /// <param name="position">Center position to push enemies away from</param>
    /// <param name="range">Range of the push effect</param>
    public static void PushEnemiesAway(Vector3 position, float range)
    {
        Vector3 pushCenter = position;
        
        IEnumerable<BaseEnemy> enemies = GetActiveEnemies();
        
        var affectedEnemies = 0;
        
        foreach (BaseEnemy enemy in enemies)
        {
            if (!enemy || enemy.IsDead || enemy.IsInvulnerable)
                continue;
                
            float distance = Vector3.Distance(enemy.transform.position, pushCenter);
            if (distance <= range)
            {
                Vector3 pushDirection = (enemy.transform.position - pushCenter).normalized;
                
                EnemyKnockbackHandler knockbackHandler = enemy.GetComponent<EnemyKnockbackHandler>();
                if (knockbackHandler != null)
                {
                    var hitContext = HitContext.Create(
                        pushDirection,
                        enemy.transform.position,
                        0f
                    ).WithPushBack(true, c_DefaultPushStrength * c_KnockbackForceMultiplier, 0.2f);
                    
                    // Apply knockback without damage
                    knockbackHandler.ApplyKnockbackFromHit(hitContext, false);
                    affectedEnemies++;
                }
                else
                {
                    // Fallback if no knockback handler exists
                    Rigidbody rb = enemy.GetComponent<Rigidbody>();
                    if (rb)
                    {
                        rb.AddForce(pushDirection * (c_DefaultPushStrength * c_KnockbackForceMultiplier), ForceMode.Impulse);
                        affectedEnemies++;
                    }
                }
            }
        }
    }
    
    #endregion
    
    #region Global Damage
    
    /// <summary>
    /// Damages all enemies in the scene by a percentage of their max health
    /// </summary>
    /// <param name="damagePercent">Percentage of max health to deal as damage (0.0-1.0)</param>
    /// <param name="excludeBosses">Whether to exclude enemies marked as bosses</param>
    public static void DamageAllEnemies(float damagePercent, bool excludeBosses)
    {
        // Clamp damage percent to valid range
        damagePercent = Mathf.Clamp01(damagePercent);
        
        IEnumerable<BaseEnemy> enemies = GetActiveEnemies();
        
        int affectedEnemies = 0;
        
        foreach (BaseEnemy enemy in enemies)
        {
            if (!enemy || enemy.IsDead || enemy.IsInvulnerable)
                continue;
                
            if (excludeBosses && enemy.Data && enemy.Data.isBoss)
                continue;
                
            float damage = enemy.MaxHealth * damagePercent;
            
            HitContext hitContext = HitContext.Create(
                Vector3.zero,
                enemy.transform.position,
                damage,
                null
            );
            
            // Apply damage
            enemy.OnHitWithContext(hitContext);
            affectedEnemies++;
        }
    }
    
    #endregion
    
    #region Projectile Negation
    
    /// <summary>
    /// Negates (destroys) nearby projectiles
    /// </summary>
    /// <param name="position">Center position to search for projectiles</param>
    /// <param name="count">Maximum number of projectiles to negate</param>
    public static void NegateNearbyBullets(Vector3 position, int count)
    {
        var center = position;
        
        var colliders = Physics.OverlapSphere(center, 5f, LayerMask.GetMask("Projectile"));
        
        // Limit the number of projectiles we'll process
        int projectilesToDestroy = Mathf.Min(count, colliders.Length);
        int destroyedCount = 0;
        
        System.Array.Sort(colliders, (a, b) => 
            Vector3.Distance(a.transform.position, center)
            .CompareTo(Vector3.Distance(b.transform.position, center)));
        
        // Destroy the closest projectiles up to the count
        for (int i = 0; i < colliders.Length && destroyedCount < projectilesToDestroy; i++)
        {
            GameObject projectile = colliders[i].gameObject;
            
            if (projectile.layer == LayerMask.NameToLayer("Projectile"))
            {
                Object.Destroy(projectile);
                
                destroyedCount++;
            }
        }
    }
    
    #endregion
} 