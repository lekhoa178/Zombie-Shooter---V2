using System;
using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using Random = UnityEngine.Random;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Patrol", story: "[Agent] Patrols Around [Points]", category: "Action", id: "3a57fd045a8824d8e2c8f3a98db03524")]
public partial class PatrolAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<List<GameObject>> Points;
    [SerializeReference] public BlackboardVariable<float> WaitTime;
    [SerializeReference] public BlackboardVariable<int> CurrentPointIndex;
    [SerializeReference] public BlackboardVariable<bool> Loop;
    
    // Random patrol configuration
    [SerializeReference] public BlackboardVariable<bool> UseRandomPoints;
    [SerializeReference] public BlackboardVariable<float> RandomPatrolRadius;
    [SerializeReference] public BlackboardVariable<int> RandomPointCount;
    [SerializeReference] public BlackboardVariable<float> MinDistanceBetweenPoints;

    // Add these fields at the top of the class
    [SerializeReference] public BlackboardVariable<bool> DrawDebugPoints;
    [SerializeReference] public BlackboardVariable<Color> PointColor;
    [SerializeReference] public BlackboardVariable<Color> PathColor;
    private bool lastDebugDrawState = false;

    // Add this field with the other parameters
    [SerializeReference] public BlackboardVariable<float> SpeedMultiplier;

    private EnemyAIMovement movement;
    private float waitTimer;
    private bool isWaiting;
    private List<Vector3> randomPatrolPoints;

    protected override Status OnStart()
    {
        // Get the agent's movement component
        if (Agent?.Value == null)
        {
            Debug.LogError("Agent is null in Patrol Action");
            return Status.Failure;
        }

        movement = Agent.Value.GetComponent<EnemyAIMovement>();
        if (movement == null)
        {
            Debug.LogError("Agent does not have EnemyAIMovement component");
            return Status.Failure;
        }

        // Check if we're using random points
        bool useRandom = UseRandomPoints?.Value ?? false;
        
        if (useRandom)
        {
            // Generate random patrol points
            if (!GenerateRandomPatrolPoints())
            {
                Debug.LogError("Failed to generate random patrol points");
                return Status.Failure;
            }
        }
        else
        {
            // Validate fixed patrol points
            if (Points?.Value == null || Points.Value.Count == 0)
            {
                Debug.LogError("No patrol points provided");
                return Status.Failure;
            }
        }

        // Initialize or use existing current point index
        if (CurrentPointIndex?.Value == null)
        {
            // Default to 0 if not set
            if (CurrentPointIndex != null)
                CurrentPointIndex.Value = 0;
        }

        // Default wait time if not set
        if (WaitTime?.Value == null && WaitTime != null)
        {
            WaitTime.Value = 1.0f;
        }

        // Default loop setting if not set
        if (Loop?.Value == null && Loop != null)
        {
            Loop.Value = true;
        }

        isWaiting = false;
        waitTimer = 0;

        // Initialize speed multiplier if needed
        if (SpeedMultiplier?.Value == null && SpeedMultiplier != null)
        {
            SpeedMultiplier.Value = 1.0f;
        }

        // Begin moving to the first patrol point
        MoveToCurrentPoint();
        
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        // Check if agent has become invalid
        if (Agent?.Value == null || movement == null)
        {
            return Status.Failure;
        }
        
        bool useRandom = UseRandomPoints?.Value ?? false;
        
        // Check if we need valid points
        if ((!useRandom && (Points?.Value == null || Points.Value.Count == 0)) ||
            (useRandom && (randomPatrolPoints == null || randomPatrolPoints.Count == 0)))
        {
            return Status.Failure;
        }

        // Check if we're waiting at a point
        if (isWaiting)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= (WaitTime?.Value ?? 1.0f))
            {
                // Waiting time is over, move to next point
                isWaiting = false;
                MoveToNextPoint();
            }
            return Status.Running;
        }
        
        // Check if we've reached the current patrol point
        if (movement.HasReachedDestination())
        {
            // Start waiting
            isWaiting = true;
            waitTimer = 0;
            return Status.Running;
        }

        // Still moving to the current point
        return Status.Running;
    }

    protected override void OnEnd()
    {
        // Stop movement if the behavior is interrupted
        if (movement != null)
        {
            movement.StopMovement();
        }
    }

    private bool GenerateRandomPatrolPoints()
    {
        // Set defaults for random generation if not provided
        float radius = RandomPatrolRadius?.Value ?? 20f;
        int pointCount = RandomPointCount?.Value ?? 5;
        float minDistance = MinDistanceBetweenPoints?.Value ?? (movement.NavAgent.stoppingDistance * 2f);
        
        randomPatrolPoints = new List<Vector3>();
        Vector3 agentPosition = Agent.Value.transform.position;
        
        // Try to generate the requested number of points
        int maxAttempts = pointCount * 10; // To prevent infinite loops
        int attempts = 0;
        
        while (randomPatrolPoints.Count < pointCount && attempts < maxAttempts)
        {
            attempts++;
            
            // Generate a random point within the radius
            Vector3 randomDirection = Random.insideUnitSphere * radius;
            randomDirection.y = 0;
            Vector3 randomPoint = agentPosition + randomDirection;
            
            // Try to find a walkable point near the random point
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, radius, NavMesh.AllAreas))
            {
                // Check if this point is too close to existing points
                bool tooClose = false;
                foreach (Vector3 existingPoint in randomPatrolPoints)
                {
                    if (Vector3.Distance(hit.position, existingPoint) < minDistance)
                    {
                        tooClose = true;
                        break;
                    }
                }
                
                // Also check if it's too close to the agent
                if (!tooClose && Vector3.Distance(hit.position, agentPosition) < minDistance)
                {
                    tooClose = true;
                }
                
                // If this point is valid, add it
                if (!tooClose)
                {
                    // Check if there's a valid path to this point
                    NavMeshPath path = new NavMeshPath();
                    if (NavMesh.CalculatePath(agentPosition, hit.position, NavMesh.AllAreas, path) && 
                        path.status == NavMeshPathStatus.PathComplete)
                    {
                        randomPatrolPoints.Add(hit.position);
                        Debug.Log($"Generated random patrol point {randomPatrolPoints.Count} at {hit.position}");
                    }
                }
            }
        }
        
        // Return true if we generated at least 2 points (minimum for patrol)
        if (randomPatrolPoints.Count >= 2)
        {
            Debug.Log($"Successfully generated {randomPatrolPoints.Count} random patrol points");
            
            // Draw debug visualization immediately after generation
            if (DrawDebugPoints?.Value ?? false)
            {
                DrawDebugPatrolPath();
            }
            
            return true;
        }
        else
        {
            Debug.LogWarning($"Failed to generate enough patrol points. Only found {randomPatrolPoints.Count}");
            return false;
        }
    }

    private void MoveToCurrentPoint()
    {
        int currentIndex = CurrentPointIndex?.Value ?? 0;
        bool useRandom = UseRandomPoints?.Value ?? false;
        
        // Get speed multiplier (default to 1.0 if not set)
        float speedMult = SpeedMultiplier?.Value ?? 1.0f;
        
        if (useRandom)
        {
            // Use random points
            if (randomPatrolPoints == null || randomPatrolPoints.Count == 0)
            {
                Debug.LogError("No random patrol points available");
                return;
            }
            
            // Validate index
            if (currentIndex < 0 || currentIndex >= randomPatrolPoints.Count)
            {
                currentIndex = 0;
                if (CurrentPointIndex != null)
                    CurrentPointIndex.Value = currentIndex;
            }
            
            Vector3 targetPosition = randomPatrolPoints[currentIndex];
            Debug.Log($"Moving to random patrol point {currentIndex} at {targetPosition} with speed multiplier {speedMult}");
            
            // Move to the current random patrol point with the multiplied speed
            if (speedMult != 1.0f)
            {
                // Use the version with explicit speed parameter
                movement.MoveTo(targetPosition, movement.BaseMoveSpeed * speedMult);
            }
            else
            {
                // Use the default speed method
                movement.MoveToPosition(targetPosition);
            }
        }
        else
        {
            // Use fixed points
            if (Points?.Value == null || Points.Value.Count == 0)
            {
                Debug.LogError("No patrol points provided");
                return;
            }
            
            // Validate index
            if (currentIndex < 0 || currentIndex >= Points.Value.Count)
            {
                currentIndex = 0;
                if (CurrentPointIndex != null)
                    CurrentPointIndex.Value = currentIndex;
            }
            
            // Get current patrol point
            GameObject currentPoint = Points.Value[currentIndex];
            if (currentPoint != null)
            {
                Debug.Log($"Moving to patrol point {currentIndex} at {currentPoint.transform.position} with speed multiplier {speedMult}");
                
                // Move to the current patrol point with the multiplied speed
                if (speedMult != 1.0f)
                {
                    // Use the version with explicit speed parameter
                    movement.MoveTo(currentPoint.transform.position, movement.CurrentMoveSpeed * speedMult);
                }
                else
                {
                    // Use the default speed method
                    movement.MoveToPosition(currentPoint.transform.position);
                }
            }
        }
    }

    private void MoveToNextPoint()
    {
        int currentIndex = CurrentPointIndex?.Value ?? 0;
        bool shouldLoop = Loop?.Value ?? true;
        bool useRandom = UseRandomPoints?.Value ?? false;
        Vector3 currentPosition = Agent.Value.transform.position;
        float minRequiredDistance = movement.NavAgent.stoppingDistance + 0.1f;
        
        int pointCount = useRandom ? randomPatrolPoints.Count : Points.Value.Count;
        
        // Try to find a valid next point that's not too close
        int startingIndex = currentIndex;
        bool foundValidPoint = false;
        
        // Loop to find next valid point
        do {
            // Increment index
            currentIndex++;
            
            // Check if we've reached the end of the patrol points
            if (currentIndex >= pointCount)
            {
                if (shouldLoop)
                {
                    // Loop back to the beginning
                    currentIndex = 0;
                }
                else
                {
                    // Don't loop, just stay at the last point
                    currentIndex = pointCount - 1;
                    break; // Exit the loop if we can't loop
                }
            }
            
            // Check if we've checked all points and came back to where we started
            if (currentIndex == startingIndex)
            {
                Debug.LogWarning("All patrol points are too close to each other!");
                break;
            }
            
            // Check if this point is far enough from current position
            Vector3 nextPointPosition;
            
            if (useRandom)
            {
                nextPointPosition = randomPatrolPoints[currentIndex];
            }
            else
            {
                GameObject nextPoint = Points.Value[currentIndex];
                if (nextPoint == null) continue;
                nextPointPosition = nextPoint.transform.position;
            }
            
            float distanceToNext = Vector3.Distance(currentPosition, nextPointPosition);
            if (distanceToNext > minRequiredDistance)
            {
                foundValidPoint = true;
                break;
            }
            else
            {
                Debug.LogWarning($"Patrol point {currentIndex} is too close (distance: {distanceToNext}), skipping it");
            }
            
        } while (!foundValidPoint);
        
        // Update the current index
        if (CurrentPointIndex != null)
            CurrentPointIndex.Value = currentIndex;
        
        // Move to the next point
        MoveToCurrentPoint();
    }

    private void DrawDebugPatrolPath()
    {
        bool shouldDraw = DrawDebugPoints?.Value ?? false;
        
        // Check if we need to draw debug information
        if (!shouldDraw)
        {
            // Clear previous debug if we just turned off drawing
            if (lastDebugDrawState)
            {
                Debug.ClearDeveloperConsole();
            }
            lastDebugDrawState = false;
            return;
        }
        
        lastDebugDrawState = true;
        
        // Set default colors if not provided
        Color pointCol = PointColor?.Value ?? Color.yellow;
        Color pathCol = PathColor?.Value ?? Color.green;
        
        // Get the points we're working with
        bool useRandom = UseRandomPoints?.Value ?? false;
        List<Vector3> points = new List<Vector3>();
        
        if (useRandom && randomPatrolPoints != null && randomPatrolPoints.Count > 0)
        {
            // Use generated random points
            points.AddRange(randomPatrolPoints);
        }
        else if (!useRandom && Points?.Value != null)
        {
            // Use provided points
            foreach (GameObject point in Points.Value)
            {
                if (point != null)
                {
                    points.Add(point.transform.position);
                }
            }
        }
        
        // Draw the points and paths
        if (points.Count > 0)
        {
            int currentIdx = CurrentPointIndex?.Value ?? 0;
            
            // Draw each point
            for (int i = 0; i < points.Count; i++)
            {
                // Draw sphere at the point - bigger for current point
                float pointSize = (i == currentIdx) ? 0.5f : 0.3f;
                Color pointColor = (i == currentIdx) ? Color.red : pointCol;
                
                // Draw the point
                Debug.DrawLine(points[i] + Vector3.up * pointSize, points[i] - Vector3.up * pointSize, pointColor, 0f, false);
                Debug.DrawLine(points[i] + Vector3.right * pointSize, points[i] - Vector3.right * pointSize, pointColor, 0f, false);
                Debug.DrawLine(points[i] + Vector3.forward * pointSize, points[i] - Vector3.forward * pointSize, pointColor, 0f, false);
                
                // Add number label
                Debug.DrawLine(points[i], points[i] + Vector3.up * 1f, pointColor, 0f, false);
            }
            
            // Draw the path connecting the points
            bool shouldLoop = Loop?.Value ?? true;
            for (int i = 0; i < points.Count; i++)
            {
                int nextIdx = (i + 1) % points.Count;
                
                // If not looping, don't draw line from last to first
                if (!shouldLoop && i == points.Count - 1)
                    continue;
                
                // Draw path line
                Debug.DrawLine(points[i], points[nextIdx], pathCol, 0f, false);
                
                // Calculate midpoint for direction indicator
                Vector3 midPoint = (points[i] + points[nextIdx]) / 2f;
                Vector3 direction = (points[nextIdx] - points[i]).normalized;
                
                // Draw arrow at midpoint
                Vector3 right = Vector3.Cross(Vector3.up, direction).normalized * 0.2f;
                Debug.DrawLine(midPoint, midPoint - direction * 0.3f + right, pathCol, 0f, false);
                Debug.DrawLine(midPoint, midPoint - direction * 0.3f - right, pathCol, 0f, false);
            }
            
            // Draw current path from agent to current point
            if (Agent?.Value != null && points.Count > currentIdx)
            {
                Debug.DrawLine(Agent.Value.transform.position, points[currentIdx], Color.white, 0f, false);
            }
        }
    }

    // Add OnDrawGizmos for editor visualization
    public void OnDrawGizmos()
    {
        // We'll use this to show patrol points in the editor even when not playing
        if (Application.isPlaying) return; // Already handled in Update
        
        bool shouldDraw = DrawDebugPoints?.Value ?? false;
        if (!shouldDraw) return;
        
        bool useRandom = UseRandomPoints?.Value ?? false;
        if (useRandom) return; // Can't visualize random points until they're generated
        
        // Set default colors if not provided
        Color pointCol = PointColor?.Value ?? Color.yellow;
        Color pathCol = PathColor?.Value ?? new Color(0, 0.8f, 0, 0.5f);
        
        // Draw points from Points list
        if (Points?.Value != null)
        {
            List<Vector3> positions = new List<Vector3>();
            
            foreach (GameObject point in Points.Value)
            {
                if (point != null)
                {
                    positions.Add(point.transform.position);
                    Gizmos.color = pointCol;
                    Gizmos.DrawSphere(point.transform.position, 0.3f);
                }
            }
            
            // Draw connecting lines
            if (positions.Count > 1)
            {
                Gizmos.color = pathCol;
                bool shouldLoop = Loop?.Value ?? true;
                
                for (int i = 0; i < positions.Count - 1; i++)
                {
                    Gizmos.DrawLine(positions[i], positions[i + 1]);
                }
                
                // Draw line from last to first if looping
                if (shouldLoop && positions.Count > 1)
                {
                    Gizmos.DrawLine(positions[positions.Count - 1], positions[0]);
                }
            }
        }
    }
}

