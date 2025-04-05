using System;
using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Perception", story: "Check If [Agent] Perceive Layer [TargetLayer] and Set [TargetDetected] Flag, with [MemoryDuration] Memory Duration", category: "Action", id: "67b392df0a3d1323eb94e8ff07783eee")]
public partial class PerceptionAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<bool> TargetDetected;
    [SerializeReference] public BlackboardVariable<string> TargetLayer;
    [SerializeReference] public BlackboardVariable<float> MemoryDuration = new BlackboardVariable<float>();
    [SerializeReference] public BlackboardVariable<GameObject> DetectedTarget;
    [SerializeReference] public BlackboardVariable<Vector3> LastSeenPosition;
    [SerializeReference] public BlackboardVariable<List<GameObject>> AllDetectedTargets;
    [SerializeReference] public BlackboardVariable<GameObject> ClosestTarget;
    [SerializeReference] public BlackboardVariable<float> DistanceToClosestTarget;

    private IPerception perception;
    private LayerMask targetLayerMask;
    private int targetLayerIndex = -1;
    
    private float memoryStartTime;
    private bool isMemoryActive = false;
    
    protected override Status OnStart()
    {
        // Validate agent
        if (Agent?.Value == null)
        {
            Debug.LogError("Agent is null in Perception Action");
            return Status.Failure;
        }

        // Get the perception component through the interface
        perception = Agent.Value.GetComponent<IPerception>();
        if (perception == null)
        {
            Debug.LogError("Agent does not have a component implementing IPerception interface");
            return Status.Failure;
        }
        
        // Get target layer
        if (TargetLayer?.Value != null)
        {
            targetLayerMask = LayerMask.GetMask(TargetLayer.Value);
            if (targetLayerMask.value == 0)
            {
                Debug.LogWarning($"Layer '{TargetLayer.Value}' not found. Using default target layers from perception component.");
                targetLayerMask = perception.GetTargetLayers();
            }
            
            // Store the layer index for quick lookups
            targetLayerIndex = LayerMask.NameToLayer(TargetLayer.Value);
        }
        else
        {
            // Use default target layers from perception
            targetLayerMask = perception.GetTargetLayers();
        }
        
        // Initialize TargetDetected if needed
        if (TargetDetected != null && TargetDetected.Value == null)
        {
            TargetDetected.Value = false;
        }
        
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (perception == null)
        {
            return Status.Failure;
        }
        
        // Get detected targets from cache
        List<Transform> detectedTargets = new List<Transform>();
        Transform closestTarget = null;
        float distanceToClosest = float.MaxValue;
        
        // If we have a specific layer, look only in that layer
        if (targetLayerIndex >= 0)
        {
            // Just get all objects in the layer using the perception's built-in methods
            detectedTargets = perception.GetPerceivedObjectsInLayer(targetLayerIndex);
            
            // Find closest if we have any targets
            if (detectedTargets.Count > 0)
            {
                closestTarget = perception.GetClosestPerceivedObjectInLayer(targetLayerIndex);
                if (closestTarget != null && perception is MonoBehaviour mb) {
                    distanceToClosest = Vector3.Distance(mb.transform.position, closestTarget.position);
                }
            }
        }
        else
        {
            // Handle multiple layers by running detection with the specified detection radius
            // This is the most significant change - we now use DetectTargets which works with the IPerception interface
            bool targetDetected = perception.DetectTargets(GetDetectionDistance(), targetLayerMask);
            
            // If detection successful, the perception component will have populated its visible targets
            if (targetDetected && perception is PerceptionComponent perceptionComponent)
            {
                detectedTargets = new List<Transform>(perceptionComponent.VisibleTargets);
                closestTarget = perceptionComponent.CurrentTarget;
                distanceToClosest = perceptionComponent.DistanceToTarget;
            }
        }
        
        // Determine if we detected anything
        bool detected = detectedTargets.Count > 0 || isMemoryActive;
        
        // Update TargetDetected
        if (TargetDetected != null)
        {
            TargetDetected.Value = detected;
        }
        
        // Update the ClosestTarget variable
        if (ClosestTarget != null)
        {
            ClosestTarget.Value = closestTarget != null ? closestTarget.gameObject : null;
        }
        
        // Update the DistanceToClosestTarget variable
        if (DistanceToClosestTarget != null)
        {
            DistanceToClosestTarget.Value = closestTarget != null ? distanceToClosest : float.MaxValue;
        }
        
        // Update All Detected Targets list if needed
        if (AllDetectedTargets != null)
        {
            // Convert Transform list to GameObject list
            List<GameObject> gameObjects = new List<GameObject>();
            foreach (Transform t in detectedTargets)
            {
                if (t != null)
                {
                    gameObjects.Add(t.gameObject);
                }
            }
            AllDetectedTargets.Value = gameObjects;
        }
        
        // Update DetectedTarget with the closest target
        if (DetectedTarget != null && closestTarget != null)
        {
            DetectedTarget.Value = closestTarget.gameObject;
            
            // Update last seen position
            if (LastSeenPosition != null)
            {
                LastSeenPosition.Value = closestTarget.position;
            }
            
            isMemoryActive = false;
        }
        else if (DetectedTarget != null)
        {
            if (MemoryDuration is { Value: > 0 })
            {
                // If we have a memory duration, keep the last seen position
                if (isMemoryActive)
                {
                    if (Time.time - memoryStartTime > MemoryDuration.Value)
                    {
                        DetectedTarget.Value = null;
                        isMemoryActive = false;
                    }
                }
                else if (DetectedTarget?.Value != null)
                {
                    // Start memory duration
                    memoryStartTime = Time.time;
                    isMemoryActive = true;
                }
            }
            else
            {
                // No memory duration, clear the detected target
                DetectedTarget.Value = null;
            }
        }
        
        // Check if a specific target was requested by name
        // if (Target?.Value != null && detected)
        // {
        //     // See if any of the detected targets match the requested target name
        //     bool foundNamedTarget = false;
        //     GameObject namedTarget = null;
        //     
        //     foreach (Transform target in detectedTargets)
        //     {
        //         if (target != null && (target.name.Contains(Target.Value) || target.gameObject.CompareTag(Target.Value)))
        //         {
        //             foundNamedTarget = true;
        //             namedTarget = target.gameObject;
        //             break;
        //         }
        //     }
        //     
        //     if (TargetDetected != null)
        //     {
        //         TargetDetected.Value = foundNamedTarget;
        //     }
        //     
        //     // If we found a specific named target, override the closest one
        //     if (foundNamedTarget && DetectedTarget != null)
        //     {
        //         DetectedTarget.Value = namedTarget;
        //         
        //         // Update last seen position
        //         if (LastSeenPosition != null && namedTarget != null)
        //         {
        //             LastSeenPosition.Value = namedTarget.transform.position;
        //         }
        //     }
        // }
        
        return Status.Running;
    }

    // Helper method to get a reasonable detection distance
    private float GetDetectionDistance()
    {
        // If we can access the PerceptionComponent directly, use its viewDistance
        if (perception is PerceptionComponent perceptionComponent)
        {
            // Use reflection to access private viewDistance field
            var field = typeof(PerceptionComponent).GetField("viewDistance", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field != null)
            {
                return (float)field.GetValue(perceptionComponent);
            }
            return 10f; // Fallback
        }
        return 10f; // Default fallback value
    }

    protected override void OnEnd()
    {
        // Nothing specific to clean up
    }
}