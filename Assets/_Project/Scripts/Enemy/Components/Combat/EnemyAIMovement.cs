// Implement AI movement component with NavMeshAgent

using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Flyweight;

public class EnemyAIMovement : MonoBehaviour, IAIMovement
{
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float stoppingDistance = 0.1f;
    [SerializeField] private bool updateRotation = true;
    [SerializeField] private float acceleration = 8f;
    [SerializeField] private float angularSpeed = 120f;
    
    private NavMeshAgent navAgent;
    private Animator animator;
    private bool isMoving;
    private float currentSpeed;
    [SerializeField] private BaseEnemy enemyComponent;
    private EnemyData enemyData;
    
    // For step VFX
    private float lastStepVfxTime;
    private bool isLeftFoot = true;
    
    // For start moving VFX
    private float lastStartMovingVfxTime;
    private bool wasMoving = false;
    
    public bool IsMoving => isMoving;
    public float CurrentMoveSpeed => currentSpeed;
    public float BaseMoveSpeed => moveSpeed;
    
    // Expose the NavMeshAgent
    public NavMeshAgent NavAgent => navAgent;
    
    // Property to get/set acceleration
    public float Acceleration
    {
        get => navAgent != null ? navAgent.acceleration : acceleration;
        set
        {
            acceleration = value;
            if (navAgent != null)
                navAgent.acceleration = value;
        }
    }
    
    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        enemyComponent = GetComponent<BaseEnemy>();
        
        // Get enemy data if available
        if (enemyComponent != null)
        {
            enemyData = enemyComponent.Data;
        }
        
        // Configure NavMeshAgent with default values
        if (navAgent != null)
        {
            navAgent.speed = moveSpeed;
            navAgent.stoppingDistance = stoppingDistance;
            navAgent.updateRotation = updateRotation;
            navAgent.acceleration = acceleration;
            navAgent.angularSpeed = angularSpeed;
        }
        
        moveSpeed = enemyData != null ? enemyData.moveSpeed : moveSpeed;
    }
    
    public void MoveTo(Vector3 position, float speed, float accel = -1)
    {
        if (!navAgent) return;
        
        bool wasStoppedBefore = navAgent.isStopped || !isMoving;
        
        navAgent.SetDestination(position);
        navAgent.speed = speed;
        
        if (accel > 0)
            navAgent.acceleration = accel;
            
        currentSpeed = speed;
        isMoving = true;
        
        navAgent.isStopped = false;
        
        if (animator)
        {
            animator.SetFloat("Moving", 2); // 2: Move
            animator.SetFloat("MoveSpeed", speed / moveSpeed);
        }
        
        // Check if this is the start of movement
        if (wasStoppedBefore && enemyData && speed >= enemyData.minSpeedForStartMovingVfx)
        {
            SpawnStartMovingVfx();
        }
    }
    
    public void MoveTo(Vector3 position, float speed)
    {
        if (!navAgent) return;
        
        var wasStoppedBefore = navAgent.isStopped || !isMoving;
        
        navAgent.SetDestination(position);
        navAgent.speed = speed;
        currentSpeed = speed;
        isMoving = true;
        
        navAgent.isStopped = false;
        
        if (animator)
        {
            animator.SetFloat("Moving", 2);
            animator.SetFloat("MoveSpeed", speed / moveSpeed); // Normalized speed
        }
        
        // Check if this is the start of movement
        if (wasStoppedBefore && enemyData && speed >= enemyData.minSpeedForStartMovingVfx)
        {
            SpawnStartMovingVfx();
        }
    }

    public void RotateTowards(Vector3 target, float rotSpeed)
    {
        if (navAgent == null) return;

        if (navAgent.updateRotation) return;
        
        var direction = (target - transform.position).normalized;
        direction.y = 0;

        if (direction == Vector3.zero) return;
            
        var lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotSpeed);
    }

    public void StopMovement()
    {
        if (!navAgent || !navAgent.isOnNavMesh) return;
        
        navAgent.isStopped = true;
        isMoving = false;
        
        if (animator && !enemyComponent.IsDead)
        {
            animator.SetFloat("Moving", 0);
        }
    }
    
    private void Update()
    {
        // Check if we've reached the destination
        if (navAgent && navAgent.isOnNavMesh && !navAgent.isStopped && navAgent.remainingDistance <= navAgent.stoppingDistance)
        {
            // We've reached our destination
            if (isMoving)
            {
                StopMovement();
            }
        }

        if (!animator || !navAgent || enemyComponent.IsDead) return;
        
        var currentVelocity = navAgent.velocity.magnitude;
        animator.SetFloat("MoveSpeed", currentVelocity / moveSpeed);
            
        var isCurrentlyMoving = currentVelocity > 0.1f;
        animator.SetFloat("Moving", isCurrentlyMoving ? 2 : 0); // 2: Move, 0: Idle
            
        // Check for movement state change for start moving VFX
        if (enemyData && enemyData.enableStartMovingVfx && 
            !wasMoving && isCurrentlyMoving && 
            currentVelocity >= enemyData.minSpeedForStartMovingVfx &&
            Time.time > lastStartMovingVfxTime + enemyData.startMovingVfxCooldown)
        {
            SpawnStartMovingVfx();
        }
            
        wasMoving = isCurrentlyMoving;
            
        // Handle step VFX
        if (enemyData != null && enemyData.enableStepVfx && enemyData.stepVfxSettings != null &&
            isCurrentlyMoving)
        {
            UpdateStepVfx(currentVelocity);
        }
    }
    
    public void MoveToPosition(Vector3 position)
    {
        MoveTo(position, moveSpeed);
    }
    
    public void MoveToPositionWithParams(Vector3 position, float speed, float accel)
    {
        MoveTo(position, speed, accel);
    }
    
    // Method to change movement parameters at runtime
    public void SetMovementParams(float speed, float accel)
    {
        moveSpeed = speed;
        acceleration = accel;
        
        if (navAgent)
        {
            navAgent.speed = speed;
            navAgent.acceleration = accel;
        }
    }
    
    public bool HasPath()
    {
        return navAgent != null && navAgent.hasPath;
    }
    
    public bool HasReachedDestination()
    {
        return navAgent != null && !navAgent.pathPending && 
               navAgent.remainingDistance <= navAgent.stoppingDistance;
    }
    
    // Flee away from a target
    public void FleeFrom(GameObject target, float fleeDistance = 10f)
    {
        if (navAgent == null || target == null) return;
        
        // Calculate direction away from target
        Vector3 directionFromTarget = (transform.position - target.transform.position).normalized;
        
        // Find a point in that direction
        Vector3 fleePosition = transform.position + directionFromTarget * fleeDistance;
        
        // Sample NavMesh for a valid position
        NavMeshHit hit;
        if (NavMesh.SamplePosition(fleePosition, out hit, fleeDistance, NavMesh.AllAreas))
        {
            // Move to the valid flee position with aggressive parameters for quick escape
            MoveTo(hit.position, moveSpeed * 1.5f, acceleration * 2f);
        }
        else
        {
            // Fallback: Try to find any valid flee position
            Vector3[] directions = new Vector3[]
            {
                directionFromTarget,
                Quaternion.Euler(0, 45, 0) * directionFromTarget,
                Quaternion.Euler(0, -45, 0) * directionFromTarget,
                Quaternion.Euler(0, 90, 0) * directionFromTarget,
                Quaternion.Euler(0, -90, 0) * directionFromTarget
            };
            
            foreach (Vector3 direction in directions)
            {
                fleePosition = transform.position + direction * fleeDistance;
                if (NavMesh.SamplePosition(fleePosition, out hit, fleeDistance, NavMesh.AllAreas))
                {
                    MoveTo(hit.position, moveSpeed * 1.5f, acceleration * 2f);
                    return;
                }
            }
            
            // Last resort: Just move in the raw direction regardless of NavMesh
            MoveTo(transform.position + directionFromTarget * 5f, moveSpeed * 1.5f);
        }
    }
    
    // Circle around a target at a certain distance
    public void CircleAroundTarget(GameObject target, float circleRadius = 5f, bool clockwise = true)
    {
        if (navAgent == null || target == null) return;
        
        Vector3 toTarget = target.transform.position - transform.position;
        toTarget.y = 0;
        
        float currentAngle = Mathf.Atan2(toTarget.z, toTarget.x) * Mathf.Rad2Deg;
        
        float rotateAmount = clockwise ? -45 : 45;
        float targetAngle = currentAngle + rotateAmount;
        
        float x = target.transform.position.x + circleRadius * Mathf.Cos(targetAngle * Mathf.Deg2Rad);
        float z = target.transform.position.z + circleRadius * Mathf.Sin(targetAngle * Mathf.Deg2Rad);
        Vector3 targetPosition = new Vector3(x, transform.position.y, z);
        
        if (NavMesh.SamplePosition(targetPosition, out var hit, circleRadius, NavMesh.AllAreas))
        {
            MoveTo(hit.position, moveSpeed * 0.8f);
        }
    }
    
    // Set NavMesh area mask (useful for different types of terrain)
    public void SetAreaMask(int areaMask)
    {
        if (navAgent != null)
        {
            navAgent.areaMask = areaMask;
        }
    }
    
    // Find a random position in range that's reachable
    public Vector3 GetRandomReachablePosition(float radius)
    {
        if (navAgent == null) return transform.position;
        
        // Try several times to find a valid point
        for (int i = 0; i < 30; i++)
        {
            Vector3 randomDirection = Random.insideUnitSphere * radius;
            randomDirection.y = 0;
            Vector3 randomPoint = transform.position + randomDirection;
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, radius, NavMesh.AllAreas))
            {
                NavMeshPath path = new NavMeshPath();
                if (navAgent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    return hit.position;
                }
            }
        }
        
        // Fallback to current position if no valid point found
        return transform.position;
    }
    
    // Add this method to apply a speed multiplier to the base speed
    public void MoveToPositionWithMultiplier(Vector3 position, float speedMultiplier = 1.0f)
    {
        var actualSpeed = moveSpeed * speedMultiplier;

        var actualAccel = speedMultiplier switch
        {
            > 1.2f => acceleration * (speedMultiplier * 0.8f),
            < 0.8f => acceleration * (speedMultiplier * 1.2f),
            _ => acceleration
        };

        MoveTo(position, actualSpeed, actualAccel);
    }
    
    // Similarly, add multiplier versions of other movement methods
    public void ChaseTargetWithMultiplier(GameObject target, float speedMultiplier = 1.0f)
    {
        if (target != null)
        {
            MoveToPositionWithMultiplier(target.transform.position, speedMultiplier);
        }
    }
    
    public void FleeWithMultiplier(GameObject target, float fleeDistance = 10f, float speedMultiplier = 1.5f)
    {
        if (navAgent == null || target == null) return;
        
        // Calculate direction away from target
        Vector3 directionFromTarget = (transform.position - target.transform.position).normalized;
        
        // Find a point in that direction
        Vector3 fleePosition = transform.position + directionFromTarget * fleeDistance;
        
        // Sample NavMesh for a valid position
        NavMeshHit hit;
        if (NavMesh.SamplePosition(fleePosition, out hit, fleeDistance, NavMesh.AllAreas))
        {
            // Move to the valid flee position with the multiplied speed
            MoveToPositionWithMultiplier(hit.position, speedMultiplier);
        }
        else
        {
            // Use fallback approaches from the original method...
            // Just use multiplier version at the end
            MoveToPositionWithMultiplier(transform.position + directionFromTarget * 5f, speedMultiplier);
        }
    }
    
    // New method to spawn the start moving VFX
    private void SpawnStartMovingVfx()
    {
        if (enemyData == null || !enemyData.enableStartMovingVfx || 
            enemyData.startMovingVfxSettings == null) return;
        
        // Track last time we spawned this VFX
        lastStartMovingVfxTime = Time.time;
        
        // Calculate world position for the VFX
        Vector3 position = transform.position + transform.rotation * enemyData.startMovingVfxOffset;
        
        // Calculate rotation for the VFX
        Quaternion rotation = Quaternion.Euler(enemyData.startMovingVfxRotation) * transform.rotation;
        
        // Spawn the start moving VFX
        VFXFlyweightHelper.SpawnVfxAtPosition(
            enemyData.startMovingVfxSettings,
            position,
            rotation,
            enemyData.GetStartMovingVfxScale()
        );
    }
    
    // Existing method to handle step VFX
    private void UpdateStepVfx(float currentVelocity)
    {
        // Only spawn step VFX if we're moving and above minimum speed
        if (isMoving && currentVelocity >= enemyData.minSpeedForStepVfx)
        {
            // Calculate step interval based on speed if configured
            float stepInterval = enemyData.stepVfxInterval;
            if (enemyData.scaleStepVfxWithSpeed)
            {
                // Faster movement = faster steps
                float speedRatio = currentVelocity / moveSpeed;
                stepInterval = Mathf.Lerp(
                    enemyData.stepVfxInterval * 1.5f, // Slower interval at minimum speed
                    enemyData.stepVfxInterval * 0.5f, // Faster interval at maximum speed
                    Mathf.Clamp01(speedRatio - 0.5f)  // Offset to start scaling at half speed
                );
            }
            
            // Check if it's time to spawn a new step VFX
            if (Time.time >= lastStepVfxTime + stepInterval)
            {
                SpawnStepVfx();
                lastStepVfxTime = Time.time;
                
                // Toggle foot for next step if alternating
                if (enemyData.alternateStepVfx)
                {
                    isLeftFoot = !isLeftFoot;
                }
            }
        }
    }
    
    private void SpawnStepVfx()
    {
        // Determine position based on foot configuration
        Vector3 footOffset;
        if (enemyData.alternateStepVfx)
        {
            footOffset = isLeftFoot ? enemyData.leftFootOffset : enemyData.rightFootOffset;
        }
        else
        {
            footOffset = enemyData.stepVfxOffset;
        }
        
        // Calculate world position for the VFX
        Vector3 position = transform.position + transform.rotation * footOffset;
        
        // Calculate rotation for the VFX
        Quaternion rotation = Quaternion.Euler(enemyData.stepVfxRotation) * transform.rotation;
        
        // Spawn the step VFX
        VFXFlyweightHelper.SpawnVfxAtPosition(
            enemyData.stepVfxSettings,
            position,
            rotation,
            enemyData.GetStepVfxScale()
        );
    }
}