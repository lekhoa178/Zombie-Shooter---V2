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
        Vector3 center = HitDetectionHelper.GetHitCenter(context, config);
        // Draw main circle
        DebugUtilities.DrawWireSphere(center, config.Radius, config.DebugColor, config.DebugDuration);
        
        // Draw projectile detection circle if enabled
        if (context.DestroyProjectiles && context.ProjectileLayerMask != 0)
        {
            Color projectileDetectionColor = new Color(0.2f, 0.8f, 1f, 0.5f); // Cyan color
            float modifiedRadius = config.Radius * context.ProjectileDetectionRangeMultiplier;
            DebugUtilities.DrawWireSphere(center, modifiedRadius, projectileDetectionColor, config.DebugDuration);
        }
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
        
        // If projectile detection is enabled, draw a second outline with a different color
        if (context.DestroyProjectiles && context.ProjectileLayerMask != 0)
        {
            Color projectileDetectionColor = new Color(0.2f, 0.8f, 1f, 0.5f); // Cyan color
            float modifiedRadius = config.Radius * context.ProjectileDetectionRangeMultiplier;
            
            // Draw arc for projectile detection
            previousPoint = Vector3.zero;
            for (int i = 0; i <= segments; i++)
            {
                float angle = -config.ArcAngle / 2 + i * angleIncrement;
                Quaternion rotation = Quaternion.Euler(0, angle, 0);
                Vector3 direction = rotation * context.PlayerTransform.forward;
                Vector3 point = center + direction * modifiedRadius;
                
                if (i > 0)
                {
                    Debug.DrawLine(previousPoint, point, projectileDetectionColor, config.DebugDuration);
                }
                
                previousPoint = point;
            }
        }
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
        
        // Draw box wireframe using Debug.DrawLine
        DrawWireBox(center, config.BoxSize, context.PlayerTransform.rotation, config.DebugColor, config.DebugDuration);
        
        // Draw projectile detection box if enabled
        if (context.DestroyProjectiles && context.ProjectileLayerMask != 0)
        {
            Color projectileDetectionColor = new Color(0.2f, 0.8f, 1f, 0.5f); // Cyan color
            Vector3 modifiedSize = config.BoxSize * context.ProjectileDetectionRangeMultiplier;
            
            // Draw the projectile detection range
            DrawWireBox(center, modifiedSize, context.PlayerTransform.rotation, projectileDetectionColor, config.DebugDuration);
        }
    }
    
    // Helper method to draw a wire box using Debug.DrawLine
    private void DrawWireBox(Vector3 center, Vector3 size, Quaternion rotation, Color color, float duration)
    {
        // Calculate the 8 corners of the box
        Vector3 extents = size * 0.5f;
        
        Vector3[] corners = new Vector3[8];
        corners[0] = center + rotation * new Vector3(-extents.x, -extents.y, -extents.z);
        corners[1] = center + rotation * new Vector3(extents.x, -extents.y, -extents.z);
        corners[2] = center + rotation * new Vector3(extents.x, -extents.y, extents.z);
        corners[3] = center + rotation * new Vector3(-extents.x, -extents.y, extents.z);
        corners[4] = center + rotation * new Vector3(-extents.x, extents.y, -extents.z);
        corners[5] = center + rotation * new Vector3(extents.x, extents.y, -extents.z);
        corners[6] = center + rotation * new Vector3(extents.x, extents.y, extents.z);
        corners[7] = center + rotation * new Vector3(-extents.x, extents.y, extents.z);
        
        // Draw the 12 edges of the box
        // Bottom face
        Debug.DrawLine(corners[0], corners[1], color, duration);
        Debug.DrawLine(corners[1], corners[2], color, duration);
        Debug.DrawLine(corners[2], corners[3], color, duration);
        Debug.DrawLine(corners[3], corners[0], color, duration);
        
        // Top face
        Debug.DrawLine(corners[4], corners[5], color, duration);
        Debug.DrawLine(corners[5], corners[6], color, duration);
        Debug.DrawLine(corners[6], corners[7], color, duration);
        Debug.DrawLine(corners[7], corners[4], color, duration);
        
        // Vertical edges
        Debug.DrawLine(corners[0], corners[4], color, duration);
        Debug.DrawLine(corners[1], corners[5], color, duration);
        Debug.DrawLine(corners[2], corners[6], color, duration);
        Debug.DrawLine(corners[3], corners[7], color, duration);
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
        
        // Draw projectile detection cone if enabled
        if (context.DestroyProjectiles && context.ProjectileLayerMask != 0)
        {
            Color projectileDetectionColor = new Color(0.2f, 0.8f, 1f, 0.5f); // Cyan color
            float modifiedRadius = config.Radius * context.ProjectileDetectionRangeMultiplier;
            
            // Draw lines for the projectile detection cone
            for (int i = 0; i <= segments; i++)
            {
                float angle = -config.ArcAngle / 2 + i * angleIncrement;
                Quaternion rotation = Quaternion.Euler(0, angle, 0);
                Vector3 direction = rotation * context.PlayerTransform.forward;
                Vector3 point = center + direction * modifiedRadius;
                
                Debug.DrawLine(center, point, projectileDetectionColor, config.DebugDuration);
            }
            
            // Draw arc connecting the outer points
            Vector3 prevPoint = center + Quaternion.Euler(0, -config.ArcAngle / 2, 0) * context.PlayerTransform.forward * modifiedRadius;
            for (int i = 1; i <= segments; i++)
            {
                float angle = -config.ArcAngle / 2 + i * angleIncrement;
                Quaternion rotation = Quaternion.Euler(0, angle, 0);
                Vector3 direction = rotation * context.PlayerTransform.forward;
                Vector3 point = center + direction * modifiedRadius;
                
                Debug.DrawLine(prevPoint, point, projectileDetectionColor, config.DebugDuration);
                prevPoint = point;
            }
        }
    }
}