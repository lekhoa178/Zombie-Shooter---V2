using Flyweight;
using LocomotionSystem;
using UnityEngine;

public class HitDetectionContext
    {
        // Transform data
        public Transform PlayerTransform { get; private set; }
        
        // Collision data
        public Bounds ColliderBounds { get; private set; }
        public LayerMask TargetLayerMask { get; private set; }
        
        // Projectile destruction data
        public bool DestroyProjectiles { get; private set; }
        public LayerMask ProjectileLayerMask { get; private set; }
        public float ProjectileDetectionRangeMultiplier { get; private set; }
        public AttackHitConfiguration.ProjectileEffect ProjectileEffect { get; private set; }
        public int MaxProjectilesToDestroy { get; private set; }
        public bool UnlimitedProjectileDestruction => MaxProjectilesToDestroy <= 0;
        public Transform ReflectionDirectionTransform => PlayerTransform;
        public float ReflectionSpeedMultiplier { get; private set; }
        public VFXFlyweightSettings ProjectileDestructionVfx { get; private set; }
        public float ProjectileDamage { get; private set; }
        public bool OverrideProjectileDamage { get; private set; }

        protected HitDetectionContext(
            Transform playerTransform,
            Bounds colliderBounds, 
            LayerMask targetLayerMask,
            bool destroyProjectiles,
            LayerMask projectileLayerMask,
            float projectileDetectionRangeMultiplier = 1.0f,
            AttackHitConfiguration.ProjectileEffect projectileEffect = AttackHitConfiguration.ProjectileEffect.Destroy,
            int maxProjectilesToDestroy = 0,
            float reflectionSpeedMultiplier = 1.0f,
            VFXFlyweightSettings projectileDestructionVfx = null,
            float projectileDamage = 10f,
            bool overrideProjectileDamage = true)
        {
            PlayerTransform = playerTransform;
            ColliderBounds = colliderBounds;
            TargetLayerMask = targetLayerMask;
            DestroyProjectiles = destroyProjectiles;
            ProjectileLayerMask = projectileLayerMask;
            ProjectileDetectionRangeMultiplier = projectileDetectionRangeMultiplier;
            ProjectileEffect = projectileEffect;
            MaxProjectilesToDestroy = maxProjectilesToDestroy;
            ReflectionSpeedMultiplier = reflectionSpeedMultiplier;
            ProjectileDestructionVfx = projectileDestructionVfx;
            ProjectileDamage = projectileDamage;
            OverrideProjectileDamage = overrideProjectileDamage;
        }
        
        // Factory method to create from a player
        public static HitDetectionContext CreateFromPlayer(Player player, AttackHitConfiguration config = null)
        {
            bool destroyProjectiles = config != null && config.DestroyProjectiles;
            LayerMask projectileLayerMask = config != null ? config.ProjectileLayerMask : 0;
            float projectileDetectionRangeMultiplier = config != null ? config.ProjectileDetectionRangeMultiplier : 1.0f;
            var projectileEffect = config != null ? config.ProjectileDestructionEffect : AttackHitConfiguration.ProjectileEffect.Destroy;
            int maxProjectilesToDestroy = config != null ? config.MaxProjectilesToDestroy : 0;
            float reflectionSpeedMultiplier = config != null ? config.ReflectionSpeedMultiplier : 1.0f;
            VFXFlyweightSettings projectileDestructionVfx = config != null ? config.ProjectileDestructionVfx : null;
            float projectileDamage = config != null ? config.ProjectileDamage : 10f;
            bool overrideProjectileDamage = config != null && config.OverrideProjectileDamage;
            
            return new HitDetectionContext(
                player.transform,
                player.ResizableCapsuleCollider.CapsuleColliderData.Collider.bounds,
                player.LayerData.EnemyLayer,
                destroyProjectiles,
                projectileLayerMask,
                projectileDetectionRangeMultiplier,
                projectileEffect,
                maxProjectilesToDestroy,
                reflectionSpeedMultiplier,
                projectileDestructionVfx,
                projectileDamage,
                overrideProjectileDamage
            );
        }
        
        // Factory method to create a projectile-specific context from an existing context
        public static HitDetectionContext CreateProjectileContext(HitDetectionContext baseContext)
        {
            return new HitDetectionContext(
                baseContext.PlayerTransform,
                baseContext.ColliderBounds,
                baseContext.ProjectileLayerMask, // Use projectile layer as target
                baseContext.DestroyProjectiles,
                baseContext.ProjectileLayerMask,
                baseContext.ProjectileDetectionRangeMultiplier,
                baseContext.ProjectileEffect,
                baseContext.MaxProjectilesToDestroy,
                baseContext.ReflectionSpeedMultiplier,
                baseContext.ProjectileDestructionVfx,
                baseContext.ProjectileDamage,
                baseContext.OverrideProjectileDamage
            );
        }
    }