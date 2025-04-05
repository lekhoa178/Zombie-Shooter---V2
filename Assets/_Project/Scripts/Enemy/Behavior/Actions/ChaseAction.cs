using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Chase", story: "[Agent] Chases [Target] Until Within [StoppingDistance]", category: "Action", id: "973f38bb0625d1a4de30034a7c46708a")]
public partial class ChaseAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<float> StoppingDistance;
    [SerializeReference] public BlackboardVariable<float> SpeedMultiplier;
    [SerializeReference] public BlackboardVariable<bool> LookAtTargetWhileChasing;
    [SerializeReference] public BlackboardVariable<Vector3> LastTargetPosition;
    [SerializeReference] public BlackboardVariable<bool> ContinuouslyUpdateDestination;
    [SerializeReference] public BlackboardVariable<bool> UseDamping;
    [SerializeReference] public BlackboardVariable<float> DampingFactor;

    private EnemyAIMovement movement;
    private float defaultStoppingDistance = 1.5f;
    private float defaultSpeedMultiplier = 1.2f;
    private bool isInitialized = false;
    private Vector3 currentTargetPosition;
    private bool isCurrentTargetPositionInitialized = false;

    protected override Status OnStart()
    {
        // Get the agent's movement component
        if (Agent?.Value == null)
        {
            Debug.LogError("Agent is null in Chase Action");
            return Status.Failure;
        }

        movement = Agent.Value.GetComponent<EnemyAIMovement>();
        if (movement == null)
        {
            Debug.LogError("Agent does not have EnemyAIMovement component");
            return Status.Failure;
        }

        // Check if target is valid
        if (Target?.Value == null)
        {
            Debug.LogWarning("No target provided for chase action");
            return Status.Failure;
        }

        // Initialize with default values if not set
        if (StoppingDistance?.Value == null && StoppingDistance != null)
        {
            StoppingDistance.Value = defaultStoppingDistance;
        }

        if (SpeedMultiplier?.Value == null && SpeedMultiplier != null)
        {
            SpeedMultiplier.Value = defaultSpeedMultiplier;
        }

        // Default to true for looking at target while chasing
        if (LookAtTargetWhileChasing?.Value == null && LookAtTargetWhileChasing != null)
        {
            LookAtTargetWhileChasing.Value = true;
        }

        // Initialize continuous update setting (default to true)
        if (ContinuouslyUpdateDestination?.Value == null && ContinuouslyUpdateDestination != null)
        {
            ContinuouslyUpdateDestination.Value = true;
        }

        // Initialize damping settings
        if (UseDamping?.Value == null && UseDamping != null)
        {
            UseDamping.Value = false;
        }
        
        if (DampingFactor?.Value == null && DampingFactor != null)
        {
            DampingFactor.Value = 0.5f; // Default damping - moderate smoothing
        }
        
        // Reset damping variables
        isCurrentTargetPositionInitialized = false;
        
        // Start chasing the target
        ChaseTarget();
        isInitialized = true;
        
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        // Check if target or agent became invalid
        if (Target?.Value == null || Agent?.Value == null || movement == null)
        {
            if (movement != null)
            {
                movement.StopMovement();
            }
            return Status.Failure;
        }

        // Store the last known position of the target
        if (LastTargetPosition != null)
        {
            LastTargetPosition.Value = Target.Value.transform.position;
        }

        // Get current distance to target
        float distanceToTarget = Vector3.Distance(Agent.Value.transform.position, Target.Value.transform.position);
        
        // Check if we're close enough to stop chasing
        float stoppingDist = StoppingDistance?.Value ?? defaultStoppingDistance;
        if (distanceToTarget <= stoppingDist)
        {
            // We've reached the target
            movement.StopMovement();
            
            // If we should look at the target, rotate to face it
            if (LookAtTargetWhileChasing?.Value ?? true)
            {
                movement.RotateTowards(Target.Value.transform.position, 5f);
            }
            
            return Status.Success;
        }

        // Check chase mode settings
        bool shouldContinuouslyUpdate = ContinuouslyUpdateDestination?.Value ?? true;
        bool shouldDamp = UseDamping?.Value ?? false;
        
        // Update chase destination based on configuration
        if (shouldContinuouslyUpdate)
        {
            if (shouldDamp)
            {
                // Apply damping for smoother movement
                UpdateDampedChasePosition();
            }
            else
            {
                // Direct chase with no damping
                ChaseTarget();
            }
        }
        else if (movement.HasReachedDestination() || !movement.IsMoving)
        {
            // Only update destination when we've reached previous one or stopped moving
            ChaseTarget();
        }
        
        // If we're looking at target while chasing, keep rotating
        if (LookAtTargetWhileChasing?.Value ?? true)
        {
            movement.RotateTowards(Target.Value.transform.position, 5f);
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        // Only stop movement if we were properly initialized
        if (isInitialized && movement != null)
        {
            // We don't always want to stop movement here, as the next action
            // (like Attack) might want to continue from our position
            // movement.StopMovement();
        }
    }

    private void UpdateDampedChasePosition()
    {
        if (Target?.Value == null || movement == null) return;
        
        Vector3 targetPosition = Target.Value.transform.position;
        
        // Initialize the current target position if needed
        if (!isCurrentTargetPositionInitialized)
        {
            currentTargetPosition = targetPosition;
            isCurrentTargetPositionInitialized = true;
        }
        
        // Get damping factor (higher values = more damping/smoother)
        float dampingFactor = Mathf.Clamp01(DampingFactor?.Value ?? 0.5f);
        
        // Calculate time-independent damping
        float smoothTime = dampingFactor * 1.0f; // 0-1 second range
        
        // Smoothly interpolate between the current chase position and the actual target position
        currentTargetPosition = Vector3.Lerp(
            currentTargetPosition, 
            targetPosition, 
            Time.deltaTime / (Time.deltaTime + smoothTime)
        );
        
        // Move to the damped position
        float speedMult = SpeedMultiplier?.Value ?? defaultSpeedMultiplier;
        movement.MoveToPositionWithMultiplier(currentTargetPosition, speedMult);
        
        // Debug visualization (optional)
        Debug.DrawLine(Agent.Value.transform.position, currentTargetPosition, Color.yellow);
        Debug.DrawLine(currentTargetPosition, targetPosition, Color.red);
    }

    private void ChaseTarget()
    {
        if (Target?.Value == null || movement == null) return;
        
        // For non-damped movement, update the current target position directly
        if (UseDamping?.Value ?? false)
        {
            currentTargetPosition = Target.Value.transform.position;
            isCurrentTargetPositionInitialized = true;
        }
        
        float speedMult = SpeedMultiplier?.Value ?? defaultSpeedMultiplier;
        movement.MoveToPositionWithMultiplier(Target.Value.transform.position, speedMult);
    }
}

