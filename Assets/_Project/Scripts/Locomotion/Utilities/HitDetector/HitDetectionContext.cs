using LocomotionSystem;
using UnityEngine;

public class HitDetectionContext
    {
        // Transform data
        public Transform PlayerTransform { get; private set; }
        
        // Collision data
        public Bounds ColliderBounds { get; private set; }
        public LayerMask TargetLayerMask { get; private set; }

        private HitDetectionContext(
            Transform playerTransform,
            Bounds colliderBounds, 
            LayerMask targetLayerMask)
        {
            PlayerTransform = playerTransform;
            ColliderBounds = colliderBounds;
            TargetLayerMask = targetLayerMask;
        }
        
        // Factory method to create from a player
        public static HitDetectionContext CreateFromPlayer(Player player)
        {
            return new HitDetectionContext(
                player.transform,
                player.ResizableCapsuleCollider.CapsuleColliderData.Collider.bounds,
                player.LayerData.EnemyLayer
            );
        }
    }