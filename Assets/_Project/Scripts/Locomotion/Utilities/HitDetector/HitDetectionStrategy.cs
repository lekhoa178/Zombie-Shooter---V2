using System.Collections.Generic;
using Locomotion;
using LocomotionSystem;
using UnityEngine;

public interface IHitDetectionStrategy
{
    Collider[] DetectHits(HitDetectionContext context, AttackHitConfiguration config);
    void DrawDebug(HitDetectionContext context, AttackHitConfiguration config);
}

public class CircleHitDetection : IHitDetectionStrategy
{
    public Collider[] DetectHits(HitDetectionContext context, AttackHitConfiguration config)
    {
        Vector3 sphereCenter = HitDetectionHelper.GetHitCenter(context, config);
        return Physics.OverlapSphere(sphereCenter, config.Radius, context.TargetLayerMask);
    }

    public void DrawDebug(HitDetectionContext context, AttackHitConfiguration config)
    {
        Vector3 sphereCenter = HitDetectionHelper.GetHitCenter(context, config);
        DebugUtilities.DrawWireSphere(sphereCenter, config.Radius, config.DebugColor, config.DebugDuration);
    }
}

public class HalfCircleHitDetection : IHitDetectionStrategy
{
    public Collider[] DetectHits(HitDetectionContext context, AttackHitConfiguration config)
    {
        Vector3 center = HitDetectionHelper.GetHitCenter(context, config);
        
        // Get all potential hits in sphere
        var potentialHits = Physics.OverlapSphere(center, config.Radius, context.TargetLayerMask);
        var validHits = new List<Collider>();
        
        foreach (var hit in potentialHits)
        {
            Vector3 dirToTarget = hit.bounds.center - context.PlayerTransform.position;
            dirToTarget.y = 0; // Ignore vertical difference
            
            float angle = Vector3.Angle(context.PlayerTransform.forward, dirToTarget);
            if (angle <= config.ArcAngle / 2)
            {
                validHits.Add(hit);
            }
        }
        
        return validHits.ToArray();
    }

    public void DrawDebug(HitDetectionContext context, AttackHitConfiguration config)
    {
        Vector3 center = HitDetectionHelper.GetHitCenter(context, config);
        
        // Draw arc visualization
        int segments = 20;
        float angleIncrement = config.ArcAngle / segments;
        Vector3 previousPoint = Vector3.zero;
        
        for (int i = 0; i <= segments; i++)
        {
            float angle = -config.ArcAngle / 2 + i * angleIncrement;
            Quaternion rotation = Quaternion.Euler(0, angle, 0);
            Vector3 direction = rotation * context.PlayerTransform.forward;
            Vector3 point = center + direction * config.Radius;
            
            if (i > 0)
            {
                Debug.DrawLine(previousPoint, point, config.DebugColor, config.DebugDuration);
            }
            
            // Draw radius line for first and last segment
            if (i == 0 || i == segments)
            {
                Debug.DrawLine(center, point, config.DebugColor, config.DebugDuration);
            }
            
            previousPoint = point;
        }
        
        // Draw arc base
        Debug.DrawLine(
            center + Quaternion.Euler(0, -config.ArcAngle / 2, 0) * context.PlayerTransform.forward * config.Radius,
            center + Quaternion.Euler(0, config.ArcAngle / 2, 0) * context.PlayerTransform.forward * config.Radius,
            config.DebugColor,
            config.DebugDuration
        );
    }
}

public class BoxHitDetection : IHitDetectionStrategy
{
    public Collider[] DetectHits(HitDetectionContext context, AttackHitConfiguration config)
    {
        Vector3 center = HitDetectionHelper.GetHitCenter(context, config);
        
        return Physics.OverlapBox(
            center,
            config.BoxSize / 2, // Half extents
            context.PlayerTransform.rotation,
            context.TargetLayerMask
        );
    }

    public void DrawDebug(HitDetectionContext context, AttackHitConfiguration config)
    {
        Vector3 center = HitDetectionHelper.GetHitCenter(context, config);
        
        // Draw box wireframe
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(
            center,
            context.PlayerTransform.rotation,
            config.BoxSize
        );
        
        DebugUtilities.DrawWireCube(Vector3.zero, Vector3.one, rotationMatrix, config.DebugColor, config.DebugDuration);
    }
}

public class ConeHitDetection : IHitDetectionStrategy
{
    public Collider[] DetectHits(HitDetectionContext context, AttackHitConfiguration config)
    {
        // Similar to half circle but with distance-based angle calculation
        Vector3 center = HitDetectionHelper.GetHitCenter(context, config);
        
        var potentialHits = Physics.OverlapSphere(center, config.Radius, context.TargetLayerMask);
        var validHits = new List<Collider>();
        
        foreach (var hit in potentialHits)
        {
            Vector3 dirToTarget = hit.bounds.center - context.PlayerTransform.position;
            dirToTarget.y = 0;
            
            float distanceRatio = dirToTarget.magnitude / config.Radius;
            float maxAngle = config.ArcAngle / 2 * distanceRatio; // Scales with distance
            
            float angle = Vector3.Angle(context.PlayerTransform.forward, dirToTarget);
            if (angle <= maxAngle)
            {
                validHits.Add(hit);
            }
        }
        
        return validHits.ToArray();
    }

    public void DrawDebug(HitDetectionContext context, AttackHitConfiguration config)
    {
        // Cone visualization (simplified)
        Vector3 center = HitDetectionHelper.GetHitCenter(context, config);
        
        // Similar to half-circle but with outer radius
        int segments = 20;
        float angleIncrement = config.ArcAngle / segments;
        
        for (int i = 0; i <= segments; i++)
        {
            float angle = -config.ArcAngle / 2 + i * angleIncrement;
            Quaternion rotation = Quaternion.Euler(0, angle, 0);
            Vector3 direction = rotation * context.PlayerTransform.forward;
            Vector3 point = center + direction * config.Radius;
            
            Debug.DrawLine(center, point, config.DebugColor, config.DebugDuration);
        }
    }
}