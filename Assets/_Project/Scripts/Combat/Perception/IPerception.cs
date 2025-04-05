using System.Collections.Generic;
using UnityEngine;

public interface IPerception
{
    // Existing methods
    bool DetectTargets(float detectionRadius, LayerMask layers);
    List<Transform> GetPerceivedObjectsInLayer(int layerIndex);
    Transform GetClosestPerceivedObjectInLayer(int layerIndex);
    Transform GetBestTarget(System.Func<Transform, float> priorityFunction);
    LayerMask GetTargetLayers();
    
    // New recommended methods for completeness
    IReadOnlyList<Transform> VisibleTargets { get; }
    Transform CurrentTarget { get; }
    bool CanSeeTarget { get; }
    float DistanceToTarget { get; }
}