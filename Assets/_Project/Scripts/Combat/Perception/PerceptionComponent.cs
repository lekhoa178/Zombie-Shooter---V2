using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Component that connects an entity to the GlobalPerceptionSystem
/// </summary>
public class PerceptionComponent : MonoBehaviour, IPerception
{
    [Header("Perception Settings")]
    [SerializeField][Range(1, 7)]private int perceptionPriority = 1;
    [SerializeField] private float viewDistance = 10f;
    [SerializeField] private float viewAngle = 120f;
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private LayerMask obstacleLayers;
    [SerializeField] private float memoryDuration = 3f;
    [SerializeField] private float cacheUpdateFrequency = 0.2f;
    [SerializeField] private bool useProximityDetection = false;
    [SerializeField] private float proximityDetectionRadius = 3f;
    [SerializeField] private bool proximityIgnoresObstacles = true;
    [SerializeField] private int updateStaggerFrames = 3;
    
    private Transform currentTarget;
    private bool canSeeTarget;
    private float lastTargetSeenTime;
    private List<Transform> visibleTargets = new List<Transform>();
    
    public Transform CurrentTarget => currentTarget;
    public bool CanSeeTarget => canSeeTarget;
    public float DistanceToTarget => currentTarget ? 
        GlobalPerceptionSystem.GetDistanceToObject(gameObject, currentTarget) : float.MaxValue;
    public IReadOnlyList<Transform> VisibleTargets => visibleTargets;
    
    private void Awake()
    {
        var parameters = CreateParameters();
        GlobalPerceptionSystem.RegisterPerceiver(gameObject, parameters);
        
        // Apply importance based on priority field
        GlobalPerceptionSystem.SetPerceiverImportance(gameObject, perceptionPriority);
    }

    private void OnEnable()
    {
        // Register for scene unloaded instead of loaded
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }
    
    private void OnDisable()
    {
        // Unregister from scene events
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }
    
    private void OnDestroy()
    {
        GlobalPerceptionSystem.UnregisterPerceiver(gameObject);
    }
    
    private void Update()
    {
        GlobalPerceptionSystem.UpdatePerception(gameObject);
        UpdateVisibleTargets();
    }
    
    // Create a complete parameter object with all settings
    public PerceptionParameters CreateParameters()
    {
        return new PerceptionParameters
        {
            ViewDistance = viewDistance,
            ViewAngle = viewAngle,
            TargetLayers = targetLayers,
            ObstacleLayers = obstacleLayers,
            MemoryDuration = memoryDuration,
            CacheUpdateFrequency = cacheUpdateFrequency,
            UseProximityDetection = useProximityDetection,
            ProximityDetectionRadius = proximityDetectionRadius,
            ProximityIgnoresObstacles = proximityIgnoresObstacles,
            UpdateStaggerFrames = updateStaggerFrames
        };
    }
    
    private void UpdateVisibleTargets()
    {
        visibleTargets.Clear();
        
        // Get all perceived objects in target layers
        visibleTargets = GlobalPerceptionSystem.GetPerceivedObjectsByDistance(
            gameObject, targetLayers, viewDistance);
        
        // First check if current target is still visible
        if (currentTarget != null)
        {
            if (GlobalPerceptionSystem.IsObjectPerceivedBy(gameObject, currentTarget))
            {
                canSeeTarget = true;
                lastTargetSeenTime = Time.time;
            }
            else if (Time.time - lastTargetSeenTime > memoryDuration)
            {
                // Target lost from memory after duration expires
                currentTarget = null;
                canSeeTarget = false;
            }
            else
            {
                // Target not visible but still in memory
                canSeeTarget = false;
            }
        }
        // If no current target, try to find a new one
        else if (visibleTargets.Count > 0)
        {
            currentTarget = GetBestTarget(target =>
            {
                float distance = -Vector3.Distance(transform.position, target.position);
                float facing = Vector3.Dot(transform.forward,
                    (target.position - transform.position).normalized);
                return distance * 0.5f + facing * 0.5f;
            });
            
            if (currentTarget != null)
            {
                canSeeTarget = true;
                lastTargetSeenTime = Time.time;
            }
        }
    }
    
    // Implement IPerception interface
    public bool DetectTargets(float detectionRadius, LayerMask layers)
    {
        visibleTargets = GlobalPerceptionSystem.GetPerceivedObjectsByDistance(
            gameObject, layers, detectionRadius);
        
        return visibleTargets.Count > 0;
    }
    
    public List<Transform> GetPerceivedObjectsInLayer(int layerIndex)
    {
        return GlobalPerceptionSystem.GetPerceivedObjectsInLayer(gameObject, layerIndex);
    }
    
    public Transform GetClosestPerceivedObjectInLayer(int layerIndex)
    {
        return GlobalPerceptionSystem.GetClosestPerceivedObject(gameObject, layerIndex);
    }

    public Transform GetBestTarget(System.Func<Transform, float> priorityFunction)
    {
        return GlobalPerceptionSystem.GetBestTarget(gameObject, targetLayers, priorityFunction);
    }
    
    public LayerMask GetTargetLayers()
    {
        return targetLayers;
    }

    private void OnValidate()
    {
        if (Application.isPlaying && gameObject != null)
        {
            GlobalPerceptionSystem.UpdatePerceiverParameters(gameObject, CreateParameters());
            GlobalPerceptionSystem.SetPerceiverImportance(gameObject, perceptionPriority);
        }
    }
    
    private void OnSceneUnloaded(Scene scene)
    {
        // Now we're only cleaning up scenes that have actually been unloaded
        GlobalPerceptionSystem.CleanupScene(scene.buildIndex);
    }
    
    // Visualization for debugging
    private void OnDrawGizmosSelected()
    {
        // Draw view distance
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, viewDistance);
        
        // Draw proximity detection radius if enabled
        if (useProximityDetection)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // Orange with transparency
            Gizmos.DrawSphere(transform.position, proximityDetectionRadius);
            
            // Draw wireframe for clarity
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, proximityDetectionRadius);
        }
        
        // Draw view angle
        Vector3 viewAngleA = DirFromAngle(-viewAngle / 2, false);
        Vector3 viewAngleB = DirFromAngle(viewAngle / 2, false);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + viewAngleA * viewDistance);
        Gizmos.DrawLine(transform.position, transform.position + viewAngleB * viewDistance);
        
        // Draw lines to visible targets
        if (visibleTargets != null)
        {
            foreach (var target in visibleTargets)
            {
                if (target != null)
                {
                    Gizmos.color = canSeeTarget && target == currentTarget ? Color.green : Color.yellow;
                    Gizmos.DrawLine(transform.position, target.position);
                }
            }
        }
    }
    
    private Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
}
