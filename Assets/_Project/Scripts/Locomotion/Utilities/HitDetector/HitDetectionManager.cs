using System;
using System.Collections.Generic;
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
            
        return strategy.DetectHits(context, config);

    }
}