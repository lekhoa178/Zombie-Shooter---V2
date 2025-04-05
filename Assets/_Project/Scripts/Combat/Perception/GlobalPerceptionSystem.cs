using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A static, centralized perception system that allows any actor to register
/// and query perceived entities in the game world.
/// </summary>
public static class GlobalPerceptionSystem
{
    // Cache for all perceived objects with distances, organized by perceiver and layer
    private static Dictionary<int, Dictionary<int, Dictionary<Transform, float>>> _perceptionCache 
        = new Dictionary<int, Dictionary<int, Dictionary<Transform, float>>>();
    
    // Track when each perceiver last updated its cache for each layer
    private static Dictionary<int, Dictionary<int, float>> _lastUpdateTimes 
        = new Dictionary<int, Dictionary<int, float>>();
    
    // Track when each perceiver last saw each object
    private static Dictionary<int, Dictionary<Transform, float>> _objectLastSeenTimes 
        = new Dictionary<int, Dictionary<Transform, float>>();
    
    // Track perceiver parameters
    private static Dictionary<int, PerceptionParameters> _perceiverParameters 
        = new Dictionary<int, PerceptionParameters>();

    // Add scene management support
    private static Dictionary<int, int> _perceiverSceneIds = new Dictionary<int, int>();

    // Add frame staggering for updates
    private static Dictionary<int, int> _updateFrameOffset = new Dictionary<int, int>();
    private static int _updateFrameCounter = 0;

    private static Dictionary<int, float> _perceiverImportance = new Dictionary<int, float>();

    /// <summary>
    /// Registers an actor with the global perception system
    /// </summary>
    public static void RegisterPerceiver(GameObject perceiver, PerceptionParameters parameters)
    {
        if (perceiver == null) return;
        
        int perceiverId = perceiver.GetInstanceID();
        
        // Initialize dictionaries for this perceiver
        if (!_perceptionCache.ContainsKey(perceiverId))
        {
            _perceptionCache[perceiverId] = new Dictionary<int, Dictionary<Transform, float>>();
            _lastUpdateTimes[perceiverId] = new Dictionary<int, float>();
            _objectLastSeenTimes[perceiverId] = new Dictionary<Transform, float>();
        }
        
        // Store perception parameters
        _perceiverParameters[perceiverId] = parameters;
        
        // Initialize dictionaries for each target layer
        for (int i = 0; i < 32; i++)
        {
            if (((1 << i) & parameters.TargetLayers.value) != 0)
            {
                if (!_perceptionCache[perceiverId].ContainsKey(i))
                {
                    _perceptionCache[perceiverId][i] = new Dictionary<Transform, float>();
                    _lastUpdateTimes[perceiverId][i] = -parameters.CacheUpdateFrequency; // Force initial update
                }
            }
        }

        // Track scene association for cleanup
        _perceiverSceneIds[perceiverId] = perceiver.scene.buildIndex;

        // Assign a frame offset for staggered updates
        _updateFrameOffset[perceiverId] = _updateFrameCounter % parameters.UpdateStaggerFrames;
        _updateFrameCounter++;
        
        // Set default importance of 1.0
        if (!_perceiverImportance.ContainsKey(perceiverId))
        {
            _perceiverImportance[perceiverId] = 1.0f;
        }
    }
    
    /// <summary>
    /// Unregisters an actor from the global perception system
    /// </summary>
    public static void UnregisterPerceiver(GameObject perceiver)
    {
        if (perceiver == null) return;
        
        int perceiverId = perceiver.GetInstanceID();
        
        _perceptionCache.Remove(perceiverId);
        _lastUpdateTimes.Remove(perceiverId);
        _objectLastSeenTimes.Remove(perceiverId);
        _perceiverParameters.Remove(perceiverId);
        _perceiverSceneIds.Remove(perceiverId);
        _updateFrameOffset.Remove(perceiverId);
        _perceiverImportance.Remove(perceiverId);
    }
    
    /// <summary>
    /// Updates perception for a specific perceiver
    /// </summary>
    public static void UpdatePerception(GameObject perceiver)
    {
        if (perceiver == null) return;
        
        int perceiverId = perceiver.GetInstanceID();
        
        // If parameters are missing, try to re-register from the component
        if (!_perceiverParameters.ContainsKey(perceiverId))
        {
            var perceptionComponent = perceiver.GetComponent<PerceptionComponent>();
            if (perceptionComponent != null)
            {
                Debug.LogWarning($"Re-registering perceiver {perceiver.name} that was missing from the registry");
                // Use reflection to access private CreateParameters method
                var method = typeof(PerceptionComponent).GetMethod("CreateParameters", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (method != null)
                {
                    if (method.Invoke(perceptionComponent, null) is PerceptionParameters param)
                    {
                        RegisterPerceiver(perceiver, param);
                        return;
                    }
                }
            }
            return;
        }
        
        // Get importance (defaulting to 1.0)
        float importance = 1.0f;
        _perceiverImportance.TryGetValue(perceiverId, out importance);
        
        var parameters = _perceiverParameters[perceiverId];
        
        // Skip update based on frame staggering and importance
        int updateFrequency = Mathf.Max(1, Mathf.RoundToInt(parameters.UpdateStaggerFrames / importance));
        if (Time.frameCount % updateFrequency != _updateFrameOffset[perceiverId])
            return;
        
        // Update detection for each layer individually
        for (int i = 0; i < 32; i++)
        {
            if (((1 << i) & parameters.TargetLayers.value) != 0)
            {
                if (!_lastUpdateTimes[perceiverId].ContainsKey(i))
                {
                    _lastUpdateTimes[perceiverId][i] = -parameters.CacheUpdateFrequency; // Force initial update
                }
                
                // Only update this layer's cache if enough time has passed
                if (Time.time - _lastUpdateTimes[perceiverId][i] >= parameters.CacheUpdateFrequency)
                {
                    DetectTargetsInLayer(perceiver, parameters.ViewDistance, 1 << i);
                    _lastUpdateTimes[perceiverId][i] = Time.time;
                }
            }
        }
        
        // Clean up old memory objects
        CleanUpObjectMemory(perceiverId, parameters.MemoryDuration);
    }

    /// <summary>
    /// Updates the parameters for an existing perceiver
    /// </summary>
    public static void UpdatePerceiverParameters(GameObject perceiver, PerceptionParameters parameters)
    {
        if (perceiver == null) return;
        
        int perceiverId = perceiver.GetInstanceID();
        
        if (_perceiverParameters.ContainsKey(perceiverId))
        {
            _perceiverParameters[perceiverId] = parameters;
            
            // Re-initialize layer dictionaries if layers changed
            for (int i = 0; i < 32; i++)
            {
                if (((1 << i) & parameters.TargetLayers.value) != 0)
                {
                    if (!_perceptionCache[perceiverId].ContainsKey(i))
                    {
                        _perceptionCache[perceiverId][i] = new Dictionary<Transform, float>();
                        _lastUpdateTimes[perceiverId][i] = -parameters.CacheUpdateFrequency; // Force update
                    }
                }
            }
        }
    }

    /// <summary>
    /// Sets the importance of a perceiver - higher values will update more frequently
    /// </summary>
    public static void SetPerceiverImportance(GameObject perceiver, float importance)
    {
        if (perceiver == null) return;
        
        int perceiverId = perceiver.GetInstanceID();
        _perceiverImportance[perceiverId] = Mathf.Max(0.1f, importance);
    }
    
    private static void DetectTargetsInLayer(GameObject perceiver, float detectionRadius, LayerMask layerMask)
    {
        int perceiverId = perceiver.GetInstanceID();
        int layerIndex = GetLayerFromMask(layerMask);
        if (layerIndex == -1) return;
        
        if (!_perceiverParameters.ContainsKey(perceiverId))
            return;
            
        var parameters = _perceiverParameters[perceiverId];
        
        // Ensure layer dictionary exists
        if (!_perceptionCache[perceiverId].ContainsKey(layerIndex))
        {
            _perceptionCache[perceiverId][layerIndex] = new Dictionary<Transform, float>();
        }
        else
        {
            // Clear the dictionary for this layer
            _perceptionCache[perceiverId][layerIndex].Clear();
        }
        
        Collider[] targetsInViewRadius = Physics.OverlapSphere(perceiver.transform.position, detectionRadius, layerMask);
        
        foreach (Collider targetCollider in targetsInViewRadius)
        {
            if (targetCollider == null) continue;
            
            Transform target = targetCollider.transform;
            
            // Skip already destroyed objects or null references
            if (target == null) continue;
            
            // Skip dead enemies
            IEnemy enemyComponent = target.GetComponent<IEnemy>();
            if (enemyComponent != null && enemyComponent.IsDead) continue;
            
            Vector3 dirToTarget = (target.position - perceiver.transform.position).normalized;
            float distToTarget = Vector3.Distance(perceiver.transform.position, target.position);
            
            bool detected = false;
            
            // Check if target is within proximity detection range (if enabled)
            if (parameters.UseProximityDetection && distToTarget <= parameters.ProximityDetectionRadius)
            {
                // For proximity detection, we can optionally ignore obstacles
                if (parameters.ProximityIgnoresObstacles)
                {
                    detected = true;
                }
                else
                {
                    // Check for obstacles even in proximity detection
                    detected = !Physics.Raycast(perceiver.transform.position, dirToTarget, 
                        distToTarget, parameters.ObstacleLayers);
                }
            }
            // Regular view cone detection
            else if (Vector3.Angle(perceiver.transform.forward, dirToTarget) < parameters.ViewAngle / 2)
            {
                // Check for obstacles between
                detected = !Physics.Raycast(perceiver.transform.position, dirToTarget, 
                    distToTarget, parameters.ObstacleLayers);
            }
            
            // If detected by either method, add to perception cache
            if (detected)
            {
                // Add to the layer-specific dictionary with distance
                _perceptionCache[perceiverId][layerIndex][target] = distToTarget;
                
                // Update last seen time
                _objectLastSeenTimes[perceiverId][target] = Time.time;
            }
        }
    }
    
    private static void CleanUpObjectMemory(int perceiverId, float memoryDuration)
    {
        if (!_objectLastSeenTimes.ContainsKey(perceiverId))
            return;
            
        List<Transform> objectsToRemove = new List<Transform>();
        
        foreach (var kvp in _objectLastSeenTimes[perceiverId])
        {
            // If object hasn't been seen for memory duration, remove it
            if (Time.time - kvp.Value > memoryDuration)
            {
                objectsToRemove.Add(kvp.Key);
            }
        }
        
        foreach (Transform obj in objectsToRemove)
        {
            _objectLastSeenTimes[perceiverId].Remove(obj);
            
            // Also remove from layer caches
            if (_perceptionCache.TryGetValue(perceiverId, out var layerDict))
            {
                foreach (var layerCache in layerDict.Values)
                {
                    layerCache.Remove(obj);
                }
            }
        }
    }

    /// <summary>
    /// Cleans up all data for perceivers in a specific scene
    /// </summary>
    public static void CleanupScene(int sceneIndex)
    {
        var perceiverIds = new List<int>();
        
        // Find all perceivers in this scene
        foreach(var pair in _perceiverSceneIds)
        {
            if(pair.Value == sceneIndex)
                perceiverIds.Add(pair.Key);
        }
        
        // Clean up all related data
        foreach(var id in perceiverIds)
        {
            _perceptionCache.Remove(id);
            _lastUpdateTimes.Remove(id);
            _objectLastSeenTimes.Remove(id);
            _perceiverParameters.Remove(id);
            _perceiverSceneIds.Remove(id);
            _updateFrameOffset.Remove(id);
            _perceiverImportance.Remove(id);
        }
    }
    
    /// <summary>
    /// Gets all perceived objects for a specific perceiver in a layer
    /// </summary>
    public static List<Transform> GetPerceivedObjectsInLayer(GameObject perceiver, int layerIndex)
    {
        List<Transform> result = new List<Transform>();
        
        if (perceiver == null) return result;
        
        int perceiverId = perceiver.GetInstanceID();
        
        if (_perceptionCache.TryGetValue(perceiverId, out var perceiverCache) &&
            perceiverCache.TryGetValue(layerIndex, out var objects))
        {
            foreach (var entry in objects)
            {
                if (entry.Key != null) // Avoid null references
                    result.Add(entry.Key);
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Gets the closest perceived object for a specific perceiver in a layer
    /// </summary>
    public static Transform GetClosestPerceivedObject(GameObject perceiver, int layerIndex)
    {
        if (perceiver == null) return null;
        
        int perceiverId = perceiver.GetInstanceID();
        Transform closest = null;
        float minDistance = float.MaxValue;
        
        if (_perceptionCache.TryGetValue(perceiverId, out var perceiverCache) &&
            perceiverCache.TryGetValue(layerIndex, out var objects))
        {
            foreach (var entry in objects)
            {
                if (entry.Key != null && entry.Value < minDistance)
                {
                    minDistance = entry.Value;
                    closest = entry.Key;
                }
            }
        }
        
        return closest;
    }
    
    /// <summary>
    /// Gets all perceived objects for a specific perceiver within a distance range
    /// </summary>
    public static List<Transform> GetPerceivedObjectsByDistance(
        GameObject perceiver, LayerMask layerMask, float maxDistance, float minDistance = 0f)
    {
        List<Transform> result = new List<Transform>();
        
        if (perceiver == null) return result;
        
        int perceiverId = perceiver.GetInstanceID();
        
        if (!_perceptionCache.TryGetValue(perceiverId, out var perceiverCache))
            return result;
        
        // Get objects from all relevant layers
        for (int i = 0; i < 32; i++)
        {
            if (((1 << i) & layerMask.value) != 0 && perceiverCache.TryGetValue(i, out var objects))
            {
                foreach (var entry in objects)
                {
                    if (entry.Key != null && entry.Value >= minDistance && entry.Value <= maxDistance)
                    {
                        result.Add(entry.Key);
                    }
                }
            }
        }
        
        // Sort by distance (closest first)
        result.Sort((a, b) => {
            float distA = _perceptionCache[perceiverId][a.gameObject.layer][a];
            float distB = _perceptionCache[perceiverId][b.gameObject.layer][b];
            return distA.CompareTo(distB);
        });
        
        return result;
    }

    /// <summary>
    /// Gets the best target based on a provided scoring function
    /// </summary>
    public static Transform GetBestTarget(GameObject perceiver, LayerMask layerMask, 
                                        System.Func<Transform, float> priorityFunction)
    {
        if (perceiver == null || priorityFunction == null) return null;
        
        int perceiverId = perceiver.GetInstanceID();
        Transform bestTarget = null;
        float bestScore = float.MinValue;
        
        if (!_perceptionCache.TryGetValue(perceiverId, out var perceiverCache))
            return null;
        
        // Score each candidate directly without creating a temporary list
        for (int i = 0; i < 32; i++)
        {
            if (((1 << i) & layerMask.value) != 0 && perceiverCache.TryGetValue(i, out var objects))
            {
                foreach (var entry in objects)
                {
                    if (entry.Key == null) continue;
                    
                    float score = priorityFunction(entry.Key);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestTarget = entry.Key;
                    }
                }
            }
        }
        
        return bestTarget;
    }
    
    /// <summary>
    /// Checks if an object is perceived by a specific perceiver
    /// </summary>
    public static bool IsObjectPerceivedBy(GameObject perceiver, Transform obj)
    {
        if (perceiver == null || obj == null) return false;
        
        int perceiverId = perceiver.GetInstanceID();
        int layerIndex = obj.gameObject.layer;
        
        return _perceptionCache.TryGetValue(perceiverId, out var perceiverCache) &&
               perceiverCache.TryGetValue(layerIndex, out var objects) &&
               objects.ContainsKey(obj);
    }
    
    /// <summary>
    /// Gets the distance to an object if it's perceived by a specific perceiver
    /// </summary>
    public static float GetDistanceToObject(GameObject perceiver, Transform obj)
    {
        if (perceiver == null || obj == null) return float.MaxValue;
        
        int perceiverId = perceiver.GetInstanceID();
        int layerIndex = obj.gameObject.layer;
        
        if (_perceptionCache.TryGetValue(perceiverId, out var perceiverCache) &&
            perceiverCache.TryGetValue(layerIndex, out var objects) &&
            objects.TryGetValue(obj, out float distance))
        {
            return distance;
        }
        
        return float.MaxValue; // Not perceived
    }
    
    /// <summary>
    /// Gets when an object was last seen by a specific perceiver
    /// </summary>
    public static float GetLastSeenTime(GameObject perceiver, Transform obj)
    {
        if (perceiver == null || obj == null) return -1f;
        
        int perceiverId = perceiver.GetInstanceID();
        
        if (_objectLastSeenTimes.TryGetValue(perceiverId, out var lastSeenTimes) &&
            lastSeenTimes.TryGetValue(obj, out float time))
        {
            return time;
        }
        return -1f; // Never seen
    }
    
    // Utility method to get layer index from mask
    private static int GetLayerFromMask(LayerMask layerMask)
    {
        int bitmask = layerMask.value;
        for (int i = 0; i < 32; i++)
        {
            if ((bitmask & (1 << i)) != 0)
            {
                return i;
            }
        }
        return -1;
    }
    
    /// <summary>
    /// Reset system for debugging or testing
    /// </summary>
    public static void Reset()
    {
        _perceptionCache.Clear();
        _lastUpdateTimes.Clear();
        _objectLastSeenTimes.Clear();
        _perceiverParameters.Clear();
        _perceiverSceneIds.Clear();
        _updateFrameOffset.Clear();
        _perceiverImportance.Clear();
        _updateFrameCounter = 0;
    }
}

/// <summary>
/// Stores perception parameters for an entity
/// </summary>
public class PerceptionParameters
{
    public float ViewDistance = 10f;
    public float ViewAngle = 120f;
    public LayerMask TargetLayers;
    public LayerMask ObstacleLayers;
    public float MemoryDuration = 3f;
    public float CacheUpdateFrequency = 0.2f;
    public bool UseProximityDetection = false;
    public float ProximityDetectionRadius = 3f;
    public bool ProximityIgnoresObstacles = true;
    public int UpdateStaggerFrames = 3;
}