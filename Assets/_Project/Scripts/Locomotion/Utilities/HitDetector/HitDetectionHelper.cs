using LocomotionSystem;
using UnityEngine;

public static class HitDetectionHelper
{
    // Helper method to calculate hit center based on configuration
    public static Vector3 GetHitCenter(HitDetectionContext context, AttackHitConfiguration config)
    {
        Vector3 basePosition;
        
        if (config.OffsetFromColliderCenter)
        {
            basePosition = context.ColliderBounds.center;
        }
        else
        {
            basePosition = context.PlayerTransform.position;
        }
        
        // Apply forward/up offset in world space
        Vector3 forwardOffset = context.PlayerTransform.forward * config.Offset.z;
        Vector3 upOffset = Vector3.up * config.Offset.y;
        Vector3 rightOffset = context.PlayerTransform.right * config.Offset.x;
        
        return basePosition + forwardOffset + upOffset + rightOffset;
    }
}
