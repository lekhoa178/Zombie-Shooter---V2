using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Flee", story: "[Agent] Flees from [Target] for [FleeDuration] seconds", category: "Action", id: "bf1aa4e316a7c7190b06c667c18371d3")]
public partial class FleeAction : Action
{
    // Core references
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    
    // Flee configuration
    [SerializeReference] public BlackboardVariable<float> FleeDuration;
    [SerializeReference] public BlackboardVariable<float> SpeedMultiplier;
    
    // Facing options - simplified to a single variable
    [SerializeReference] public BlackboardVariable<bool> RunBackwards;
    
    // Movement pattern options
    [SerializeReference] public BlackboardVariable<bool> StrafeRandomly;
    [SerializeReference] public BlackboardVariable<bool> RandomizeBackwardsMovement;
    [SerializeReference] public BlackboardVariable<float> RandomnessAmount;
    
    // Output variables
    [SerializeReference] public BlackboardVariable<bool> IsFleeing;
    [SerializeReference] public BlackboardVariable<float> RemainingFleeTime;
    
    // State tracking
    private EnemyAIMovement movement;
    private float fleeEndTime;
    private Vector3 fleeDirection;
    private Vector3 currentMoveDirection;
    private float strafeSign = 1f;
    private float nextDirectionChangeTime;
    private Vector3 lastRandomDirection;
    
    // Default values
    private float defaultFleeDuration = 3f;
    private float defaultSpeedMultiplier = 1.2f;
    private float defaultRandomnessAmount = 0.3f;
    
    protected override Status OnStart()
    {
        // Validate agent
        if (Agent?.Value == null)
        {
            Debug.LogError("Agent is null in Flee Action");
            return Status.Failure;
        }

        // Get the movement component
        movement = Agent.Value.GetComponent<EnemyAIMovement>();
        if (movement == null)
        {
            Debug.LogError("Agent does not have EnemyAIMovement component");
            return Status.Failure;
        }
        
        // Check if target is valid
        if (Target?.Value == null)
        {
            Debug.LogWarning("No target provided for flee action");
            return Status.Failure;
        }
        
        // Initialize with default values if not set
        if (FleeDuration?.Value == null && FleeDuration != null)
        {
            FleeDuration.Value = defaultFleeDuration;
        }
        
        if (SpeedMultiplier?.Value == null && SpeedMultiplier != null)
        {
            SpeedMultiplier.Value = defaultSpeedMultiplier;
        }
        
        // Initialize RunBackwards option (default to true)
        if (RunBackwards?.Value == null && RunBackwards != null)
        {
            RunBackwards.Value = true;
        }
        
        // Initialize movement pattern options
        if (StrafeRandomly?.Value == null && StrafeRandomly != null)
        {
            StrafeRandomly.Value = false;
        }
        
        if (RandomizeBackwardsMovement?.Value == null && RandomizeBackwardsMovement != null)
        {
            RandomizeBackwardsMovement.Value = false;
        }
        
        if (RandomnessAmount?.Value == null && RandomnessAmount != null)
        {
            RandomnessAmount.Value = defaultRandomnessAmount;
        }
        
        // Set flee end time
        float duration = FleeDuration?.Value ?? defaultFleeDuration;
        fleeEndTime = Time.time + duration;
        
        // Initialize random direction values
        strafeSign = UnityEngine.Random.value > 0.5f ? 1f : -1f;
        nextDirectionChangeTime = Time.time + UnityEngine.Random.Range(0.5f, 1.5f);
        lastRandomDirection = UnityEngine.Random.insideUnitSphere * 0.5f;
        lastRandomDirection.y = 0;
        
        // Update output variables
        if (IsFleeing != null)
        {
            IsFleeing.Value = true;
        }
        
        if (RemainingFleeTime != null)
        {
            RemainingFleeTime.Value = duration;
        }
        
        // Disable NavMeshAgent's automatic rotation if we're controlling rotation
        if (movement.NavAgent != null)
        {
            movement.NavAgent.updateRotation = false;
        }
        
        // Calculate flee direction and start fleeing
        CalculateFleeDirection();
        StartFleeing();
        
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        // Check if agent or target became invalid
        if (Agent?.Value == null || Target?.Value == null || movement == null)
        {
            StopFleeing();
            return Status.Failure;
        }
        
        // Check if flee time is up
        float remainingTime = fleeEndTime - Time.time;
        if (remainingTime <= 0)
        {
            StopFleeing();
            return Status.Success;
        }
        
        // Update remaining time in blackboard
        if (RemainingFleeTime != null)
        {
            RemainingFleeTime.Value = remainingTime;
        }
        
        // Update facing based on options
        UpdateFacing();
        
        // Check if we should recalculate flee direction (periodic update)
        if (Time.time >= nextDirectionChangeTime)
        {
            // Update direction change schedule
            nextDirectionChangeTime = Time.time + UnityEngine.Random.Range(0.3f, 1.0f);
            
            // Update strafe direction if enabled
            if (StrafeRandomly?.Value ?? false)
            {
                strafeSign = -strafeSign; // Reverse strafe direction
            }
            
            // Generate new random direction for randomized backwards movement
            if (RandomizeBackwardsMovement?.Value ?? false)
            {
                lastRandomDirection = UnityEngine.Random.insideUnitSphere * 0.5f;
                lastRandomDirection.y = 0;
            }
            
            // Recalculate flee direction with updated randomization
            CalculateFleeDirection();
            
            // Update movement
            UpdateFleeMovement();
        }
        
        return Status.Running;
    }

    protected override void OnEnd()
    {
        StopFleeing();
    }
    
    private void StopFleeing()
    {
        if (movement != null)
        {
            // Re-enable NavMeshAgent's rotation when we stop fleeing
            if (movement.NavAgent != null)
            {
                movement.NavAgent.updateRotation = true;
            }
            
            movement.StopMovement();
        }
        
        if (IsFleeing != null)
        {
            IsFleeing.Value = false;
        }
    }
    
    private void UpdateFacing()
    {
        if (Agent?.Value == null || Target?.Value == null) return;
        
        bool runBackwards = RunBackwards?.Value ?? true;
        
        if (runBackwards)
        {
            // When running backwards, face toward the target
            Vector3 lookDirection = Target.Value.transform.position - Agent.Value.transform.position;
            lookDirection.y = 0;
            
            if (lookDirection != Vector3.zero)
            {
                float rotationSpeed = 10f;
                Agent.Value.transform.rotation = Quaternion.Slerp(
                    Agent.Value.transform.rotation,
                    Quaternion.LookRotation(lookDirection),
                    Time.deltaTime * rotationSpeed
                );
            }
        }
        else
        {
            // When not running backwards, face in the direction of movement
            if (currentMoveDirection != Vector3.zero)
            {
                float rotationSpeed = 8f;
                Agent.Value.transform.rotation = Quaternion.Slerp(
                    Agent.Value.transform.rotation,
                    Quaternion.LookRotation(currentMoveDirection),
                    Time.deltaTime * rotationSpeed
                );
            }
        }
    }
    
    private void CalculateFleeDirection()
    {
        if (Agent?.Value == null || Target?.Value == null) return;
        
        // Get the base flee direction (away from target)
        Vector3 baseDirection = (Agent.Value.transform.position - Target.Value.transform.position).normalized;
        baseDirection.y = 0; // Keep on same Y level
        
        // Apply strafing if enabled
        if (StrafeRandomly?.Value ?? false)
        {
            // Get perpendicular direction (cross product with up vector)
            Vector3 perpendicularDir = Vector3.Cross(baseDirection, Vector3.up).normalized;
            
            // Combine with base direction (70% backwards, 30% sideways)
            fleeDirection = (baseDirection * 0.7f + perpendicularDir * 0.3f * strafeSign).normalized;
        }
        else
        {
            fleeDirection = baseDirection;
        }
        
        // Apply randomized backwards movement if enabled
        if (RandomizeBackwardsMovement?.Value ?? false)
        {
            float randomAmount = RandomnessAmount?.Value ?? defaultRandomnessAmount;
            
            // Add random direction influence while preserving the main backwards component
            Vector3 randomizedDirection = Vector3.Lerp(
                fleeDirection, 
                fleeDirection + lastRandomDirection,
                randomAmount
            ).normalized;
            
            // Ensure it's still mostly moving away from target
            float dotProduct = Vector3.Dot(randomizedDirection, baseDirection);
            
            // If the randomized direction isn't at least somewhat away from the target, adjust it
            if (dotProduct < 0.5f)
            {
                // Blend more with the base direction to ensure proper retreat
                randomizedDirection = Vector3.Lerp(randomizedDirection, baseDirection, 0.5f).normalized;
            }
            
            fleeDirection = randomizedDirection;
        }
        
        // Store current move direction for rotation purposes
        currentMoveDirection = fleeDirection;
    }
    
    private void StartFleeing()
    {
        if (movement == null) return;
        
        UpdateFleeMovement();
    }
    
    private void UpdateFleeMovement()
    {
        if (Agent?.Value == null || movement == null || Target?.Value == null) return;
        
        float speedMult = SpeedMultiplier?.Value ?? defaultSpeedMultiplier;
        
        // Calculate flee destination using the current flee direction
        Vector3 fleePosition = Agent.Value.transform.position + fleeDirection * 10f;
        
        // Use custom movement to handle the flee behavior
        if (movement.NavAgent != null)
        {
            // First check if the direction is walkable
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(fleePosition, out hit, 10f, UnityEngine.AI.NavMesh.AllAreas))
            {
                // Use the sampled position as the flee destination
                movement.MoveToPositionWithMultiplier(hit.position, speedMult);
            }
            else
            {
                // If we can't find a valid position in our flee direction,
                // fall back to the standard flee method which will try to find a valid path
                movement.FleeWithMultiplier(Target.Value, speedMult);
            }
        }
        else
        {
            // Fallback for non-NavMesh agents
            movement.MoveToPositionWithMultiplier(fleePosition, speedMult);
        }
        
        // Debug visualization
        Debug.DrawRay(Agent.Value.transform.position, fleeDirection * 3f, Color.red);
    }
}

