using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using UnityEngine.AI;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "CircularAround", story: "[Agent] Move Circular Around [Target]", category: "Action", id: "5c506d85065d494cc2ef0bd42161c63a")]
public partial class CircularAroundAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    
    // Add parameters for the circle movement
    [SerializeReference] public BlackboardVariable<float> CircleRadius = new BlackboardVariable<float>(5f);
    [SerializeReference] public BlackboardVariable<bool> Clockwise = new BlackboardVariable<bool>(true);
    [SerializeReference] public BlackboardVariable<float> Duration = new BlackboardVariable<float>(3f);
    [SerializeReference] public BlackboardVariable<float> SpeedMultiplier = new BlackboardVariable<float>(0.4f);
    [SerializeReference] public BlackboardVariable<float> RadiusThreshold = new BlackboardVariable<float>(0.5f);
    
    // Track the execution state
    private EnemyAIMovement _agentMovement;
    private float _startTime;
    private bool _initialized;
    private bool _atDesiredRadius = false;
    private Transform _agentTransform;
    private Vector3 _lastCirclePosition; // Track last circle position

    protected override Status OnStart()
    {
        // Reset state
        _initialized = false;
        _startTime = Time.time;
        _atDesiredRadius = false;
        
        // Get the agent GameObject
        GameObject agent = Agent?.Value;
        if (agent == null)
        {
            Debug.LogWarning("CircularAroundAction: Agent is null");
            return Status.Failure;
        }
        
        _agentTransform = agent.transform;
        
        // Get movement components
        _agentMovement = agent.GetComponent<EnemyAIMovement>();
        
        if (_agentMovement == null)
        {
            Debug.LogWarning("CircularAroundAction: Agent does not have IAIMovement component");
            return Status.Failure;
        }
        
        _initialized = true;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        // Check if initialization failed
        if (!_initialized)
            return Status.Failure;
            
        // Check for expired duration
        if (Duration.Value > 0 && (Time.time - _startTime) >= Duration.Value)
            return Status.Success;
            
        // Get target
        GameObject target = Target?.Value;
        if (target == null)
        {
            Debug.LogWarning("CircularAroundAction: Target is null");
            return Status.Failure;
        }
        
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
        
        // Calculate current distance to target
        float currentDistance = Vector3.Distance(_agentTransform.position, target.transform.position);
        
        // Always calculate the circular movement destination
        Vector3 circleDestination = CalculateCirclePosition(target, CircleRadius.Value, Clockwise.Value);
        _lastCirclePosition = circleDestination;
        
        // If we're not at the desired radius, decide if we need to adjust our approach
        if (!_atDesiredRadius)
        {
            // Check if we've reached the desired distance
            if (Mathf.Abs(currentDistance - CircleRadius.Value) <= RadiusThreshold.Value)
            {
                // We've reached the desired radius, just continue with circular movement
                _atDesiredRadius = true;
                Debug.Log($"CircularAroundAction: Reached desired radius ({currentDistance:F2})");
                
                // Move to circle position at normal speed
                _agentMovement.MoveTo(circleDestination, _agentMovement.BaseMoveSpeed);
            }
            else 
            {
                // We need to adjust our distance while circling
                // Determine if we need to approach or retreat
                bool needToRetreat = currentDistance < CircleRadius.Value;
                
                if (needToRetreat)
                {
                    // Calculate a position that's both circular and further away
                    Vector3 directionToTarget = target.transform.position - _agentTransform.position;
                    directionToTarget.y = 0;
                    directionToTarget.Normalize();
                    
                    // Calculate retreat destination (in the same circular direction but pushed out)
                    Vector3 retreatDestination = circleDestination - directionToTarget * 
                        (CircleRadius.Value - currentDistance + 1f); // Add extra distance to ensure we move out
                    
                    // Use NavMesh to find a valid point
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(retreatDestination, out hit, 5f, NavMesh.AllAreas))
                    {
                        // Move with slightly faster speed when retreating
                        _agentMovement.MoveTo(hit.position, _agentMovement.BaseMoveSpeed * SpeedMultiplier);
                    }
                    else
                    {
                        // Fallback to direct circle position if we can't find valid retreat position
                        _agentMovement.MoveTo(circleDestination, _agentMovement.BaseMoveSpeed * SpeedMultiplier);
                    }
                }
                else // We're too far out and need to approach
                {
                    // When we're too far, move more directly toward the circle position
                    _agentMovement.MoveTo(circleDestination, _agentMovement.BaseMoveSpeed * (SpeedMultiplier + 0.2f));
                }
            }
        }
        else
        {
            // We're at the right radius, just do normal circular movement
            _agentMovement.MoveTo(circleDestination, _agentMovement.BaseMoveSpeed);
        }
        
        // Always face the target while moving
        _agentMovement.RotateTowards(target.transform.position, 5f);
        
        return Status.Running;
    }
    
    // Calculate the next position to move to for circular movement
    private Vector3 CalculateCirclePosition(GameObject target, float radius, bool clockwise)
    {
        Vector3 toTarget = target.transform.position - _agentTransform.position;
        toTarget.y = 0;
        
        float currentAngle = Mathf.Atan2(toTarget.z, toTarget.x) * Mathf.Rad2Deg;
        
        // Larger angle change when we're not at the right radius to help us spiral in/out
        float rotateAmount = _atDesiredRadius ? 
            (clockwise ? -45 : 45) : 
            (clockwise ? -60 : 60);
            
        float targetAngle = currentAngle + rotateAmount;
        
        float x = target.transform.position.x + radius * Mathf.Cos(targetAngle * Mathf.Deg2Rad);
        float z = target.transform.position.z + radius * Mathf.Sin(targetAngle * Mathf.Deg2Rad);
        Vector3 targetPosition = new Vector3(x, _agentTransform.position.y, z);
        
        // Try to find a valid NavMesh position
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPosition, out hit, radius, NavMesh.AllAreas))
        {
            return hit.position;
        }
        
        // Fallback if no valid NavMesh position is found
        return targetPosition;
    }

    protected override void OnEnd()
    {
        // Stop movement when action completes
        if (_initialized && _agentMovement != null)
        {
            _agentMovement.StopMovement();
        }
    }
    
    // Optional: Draw gizmos for debugging
    public void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || Target?.Value == null) return;
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(Target.Value.transform.position, CircleRadius.Value);
        
        if (_lastCirclePosition != Vector3.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(_lastCirclePosition, 0.3f);
        }
    }
}

