using System;
using System.Collections.Generic;
using Flyweight;
using LocomotionSystem;
using UnityEngine;

public class HitDetectionManager
{
    public static HitDetectionManager Instance { get; } = new HitDetectionManager();
    
    private readonly Dictionary<AttackHitConfiguration.DetectionShape, IHitDetectionStrategy> _strategies = new()
    {
        { AttackHitConfiguration.DetectionShape.Circle, new CircleHitDetection() },
        { AttackHitConfiguration.DetectionShape.HalfCircle, new HalfCircleHitDetection() },
        { AttackHitConfiguration.DetectionShape.Cone, new ConeHitDetection() },
        { AttackHitConfiguration.DetectionShape.Box, new BoxHitDetection() }
    };

    public Collider[] DetectHits(HitDetectionContext context, AttackHitConfiguration config)
    {
        if (!_strategies.TryGetValue(config.Shape, out var strategy)) return Array.Empty<Collider>();
        if (config.DebugEnabled)
        {
            strategy.DrawDebug(context, config);
        }
            
        Collider[] hitColliders = strategy.DetectHits(context, config);
        
        // Process projectile destruction if enabled
        if (context.DestroyProjectiles && context.ProjectileLayerMask != 0)
        {
            DetectAndProcessProjectiles(context, config, strategy);
        }
        
        return hitColliders;
    }
    
    private void DetectAndProcessProjectiles(HitDetectionContext context, AttackHitConfiguration config, IHitDetectionStrategy strategy)
    {
        // Use the factory method to create a projectile-specific context
        var projectileContext = HitDetectionContext.CreateProjectileContext(context);
        
        // Apply range multiplier to the config for projectile detection
        var projectileConfig = ScriptableObject.Instantiate(config);
        
        // Adjust detection range based on multiplier
        projectileConfig.Radius *= context.ProjectileDetectionRangeMultiplier;
        projectileConfig.BoxSize *= context.ProjectileDetectionRangeMultiplier;
        
        // Detect projectiles using the detection strategy
        Collider[] projectileColliders = strategy.DetectHits(projectileContext, projectileConfig);
        
        if (projectileColliders.Length > 0)
        {
            ProcessProjectiles(projectileColliders, context);
        }
    }
    
    private void ProcessProjectiles(Collider[] projectileColliders, HitDetectionContext context)
    {
        int processedCount = 0;
        
        foreach (var collider in projectileColliders)
        {
            // Respect the maximum count if set
            if (!context.UnlimitedProjectileDestruction && processedCount >= context.MaxProjectilesToDestroy)
                break;
            
            // Try to get projectile component
            var projectile = collider.GetComponent<Flyweight.Projectile>();
            if (projectile != null)
            {
                // Handle based on configured effect
                if (context.ProjectileEffect == AttackHitConfiguration.ProjectileEffect.Destroy)
                {
                    // Destroy/return to pool
                    DestroyProjectile(projectile, collider.transform.position, context);
                    processedCount++;
                }
                else if (context.ProjectileEffect == AttackHitConfiguration.ProjectileEffect.Reflect)
                {
                    // Reflect projectile
                    ReflectProjectile(projectile, context);
                    processedCount++;
                }
            }
        }
    }
    
    private void DestroyProjectile(Flyweight.Projectile projectile, Vector3 position, HitDetectionContext context)
    {
        // Spawn VFX at projectile position if configured
        if (context.ProjectileDestructionVfx != null)
        {
            VFXFlyweightHelper.SpawnVfxAtPosition(
                context.ProjectileDestructionVfx,
                position,
                Quaternion.identity
            );
        }
        
        // Return projectile to pool
        FlyweightFactory.ReturnToPool(projectile);
    }
    
    private void ReflectProjectile(Flyweight.Projectile projectile, HitDetectionContext context)
    {
        try
        {
            // Calculate reflection direction based on player's forward
            Vector3 reflectionDir = context.ReflectionDirectionTransform.TransformDirection(
                new Vector3(0, 0, 1)
            ).normalized;
            
            // Try to access properties via reflection to ensure compatibility
            // with different Projectile implementations
            var projectileType = projectile.GetType();
            
            // Set the new direction (using transform.forward)
            projectile.transform.forward = reflectionDir;
            
            // Try to change the owner - likely a property or field called Owner
            var ownerProp = projectileType.GetProperty("Owner");
            if (ownerProp != null)
            {
                ownerProp.SetValue(projectile, context.PlayerTransform.gameObject);
            }
            
            // For damage, try to find a property called Damage
            if (context.OverrideProjectileDamage)
            {
                var damageProp = projectileType.GetProperty("Damage");
                if (damageProp != null)
                {
                    damageProp.SetValue(projectile, context.ProjectileDamage);
                }
            }
            
            // For speed, try to find a property called Speed
            var speedProp = projectileType.GetProperty("Speed");
            if (speedProp != null && speedProp.CanRead && speedProp.CanWrite)
            {
                float currentSpeed = (float)speedProp.GetValue(projectile);
                speedProp.SetValue(projectile, currentSpeed * context.ReflectionSpeedMultiplier);
            }
            
            // If there's a Reflect method, try to call it
            var reflectMethod = projectileType.GetMethod("Reflect");
            if (reflectMethod != null)
            {
                reflectMethod.Invoke(projectile, null);
            }
            
            // Make sure the projectile is active
            projectile.gameObject.SetActive(true);
            
            // Debug log
            Debug.Log($"Reflected projectile with direction {reflectionDir}, speed multiplier {context.ReflectionSpeedMultiplier}");
        }
        catch (Exception ex)
        {
            // Log any errors encountered during reflection
            Debug.LogError($"Error reflecting projectile: {ex.Message}");
            
            // Fall back to basic approach - activate and redirect
            projectile.transform.forward = context.PlayerTransform.forward;
            projectile.gameObject.SetActive(true);
        }
    }
}