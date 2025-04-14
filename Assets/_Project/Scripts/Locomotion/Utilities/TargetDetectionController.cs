using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace LocomotionSystem
{
    /// <summary>
    /// Generic target detection and selection system that can work with any type of targetable object
    /// </summary>
    public class TargetDetectionController : MonoBehaviour
    {
        [Header("Components")] 
        public PlayerInput Input { get; private set; }

        [Header("Target Lists")]
        public List<Transform> availableTargets = new List<Transform>();

        [Space] [Header("Target Detection Settings")] 
        [Tooltip("Layer mask for detecting targets")]
        public LayerMask targetLayers;

        [Tooltip("Should detector scan for new targets continuously")]
        public bool autoScanForTargets = true;

        [Tooltip("Can the target be changed while one is already selected")]
        public bool canChangeTarget = true;

        [Tooltip("Detection Range: \n Maximum range for detecting potential targets.")] 
        [Range(0f, 30f)]
        public float detectionRange = 10f;

        [Tooltip(
            "Dot Product Threshold \nHigher Values: More strict alignment required \nLower Values: Allows for broader targeting")]
        [Range(0f, 1f)]
        public float dotProductThreshold = 0.15f;

        [Tooltip("How often to update target detection (in seconds)")]
        [Range(0.05f, 0.5f)]
        public float detectionInterval = 0.1f;
        
        [Tooltip("Maximum targets to consider during detection")]
        public int maxTargetsToConsider = 100;

        [Space] [Header("Debug")] 
        public bool debug;
        public Transform checkPos;

        private GameObject currentTarget;
        private Camera mainCamera;
        private Coroutine detectionRoutine;
        
        // Event that fires when target changes
        public event Action<GameObject> OnTargetChanged;

        #region Unity Lifecycle

        private void Awake()
        {
            mainCamera = Camera.main;
            
            Input = GetComponent<PlayerInput>();
            if (Input == null)
            {
            Input = FindAnyObjectByType<PlayerInput>();
            }

            // Subscribe to EnemyFactory events
            if (EnemyFactory.Instance != null)
            {
                EnemyFactory.Instance.OnEnemySpawned.AddListener(HandleNewEnemySpawned);
            }
        }

        private void Start()
        {
            if (autoScanForTargets)
            {
                ScanForTargets();
                StartDetectionRoutine();
            }
        }
        
        private void OnEnable()
        {
            // Subscribe to death events for targets that support it
            SubscribeToTargetEvents();
            
            if (detectionRoutine == null && autoScanForTargets)
            {
                StartDetectionRoutine();
            }

            // Subscribe to EnemyFactory events
            if (EnemyFactory.Instance != null)
            {
                EnemyFactory.Instance.OnEnemySpawned.AddListener(HandleNewEnemySpawned);
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from all events
            UnsubscribeFromTargetEvents();
            
            if (detectionRoutine != null)
            {
                StopCoroutine(detectionRoutine);
                detectionRoutine = null;
            }

            if (EnemyFactory.Instance != null && EnemyFactory.Instance.OnEnemySpawned != null)
            {
                EnemyFactory.Instance.OnEnemySpawned.RemoveListener(HandleNewEnemySpawned);
            }
        }

        #endregion

        #region Target Detection and Management

        /// <summary>
        /// Handle newly spawned enemies from EnemyFactory
        /// </summary>
        private void HandleNewEnemySpawned(BaseEnemy _enemy)
        {
            if (_enemy != null && IsInLayerMask(_enemy.gameObject.layer, targetLayers))
            {
                RegisterTarget(_enemy.transform);
            }
        }

        /// <summary>
        /// Scan the scene for available targets using the targetLayers
        /// </summary>
        public void ScanForTargets()
        {
            // Clear existing targets to prevent duplicates
            availableTargets.Clear();
            
            // Find all GameObjects on the target layers
            List<GameObject> targetObjects = new List<GameObject>();
            
            // Get all colliders on the target layers
            Collider[] targetColliders = Physics.OverlapSphere(transform.position, maxTargetsToConsider, targetLayers);
            
            // Extract unique GameObjects from colliders
            HashSet<GameObject> uniqueTargets = new HashSet<GameObject>();
            foreach (var collider in targetColliders)
            {
                uniqueTargets.Add(collider.gameObject);
            }
            
            // Convert to list for processing
            targetObjects.AddRange(uniqueTargets);

            // Output the number of targets found
            if (debug)
                Debug.Log("Number of targets found: " + targetObjects.Count);

            // Add each target transform to the list if it's valid
            foreach (var obj in targetObjects)
            {
                if (obj != null && obj.activeInHierarchy)
                {
                    availableTargets.Add(obj.transform);
                    
                    // Try to subscribe to target events
                    TrySubscribeToTargetEvents(obj);
                }
            }
        }
        
        private void StartDetectionRoutine()
        {
            if (detectionRoutine != null)
            {
                StopCoroutine(detectionRoutine);
            }
            
            detectionRoutine = StartCoroutine(DetectionRoutine());
        }

        private IEnumerator DetectionRoutine()
        {
            while (true)
            {
                ScanForTargetsInRange();
                
                ValidateCurrentTarget();
                
                if (currentTarget == null || canChangeTarget)
                {
                    GetTargetInInputDirection();
                }
                
                yield return new WaitForSeconds(detectionInterval);
            }
        }
        
        /// <summary>
        /// Scan for targets within detection range around the player
        /// </summary>
        private void ScanForTargetsInRange()
        {
            // Get all colliders on the target layers within detection range
            Collider[] targetColliders = Physics.OverlapSphere(transform.position, detectionRange, targetLayers);
            
            // Create a set of current targets to track which ones are still valid
            HashSet<Transform> currentValidTargets = new HashSet<Transform>();
            
            // Process each collider
            foreach (var collider in targetColliders)
            {
                if (collider == null || !collider.gameObject.activeInHierarchy)
                    continue;
                    
                GameObject targetObject = collider.gameObject;
                Transform targetTransform = targetObject.transform;
                
                // Add to our set of current valid targets
                currentValidTargets.Add(targetTransform);
                
                // If not already in our available targets, add it
                if (!availableTargets.Contains(targetTransform))
                {
                    availableTargets.Add(targetTransform);
                    TrySubscribeToTargetEvents(targetObject);
                    
                    if (debug)
                    {
                        Debug.Log($"New target detected and added: {targetObject.name}");
                    }
                }
            }
            
            // Create a list of targets to remove (ones that are no longer in range or active)
            List<Transform> targetsToRemove = new List<Transform>();
            
            // Check which existing targets are no longer valid
            foreach (var existingTarget in availableTargets)
            {
                if (existingTarget == null || 
                    !existingTarget.gameObject.activeInHierarchy ||
                    !currentValidTargets.Contains(existingTarget))
                {
                    targetsToRemove.Add(existingTarget);
                }
            }
            
            // Remove invalid targets
            foreach (var targetToRemove in targetsToRemove)
            {
                availableTargets.Remove(targetToRemove);
                
                // If it was the current target, clear it
                if (currentTarget != null && currentTarget.transform == targetToRemove)
                {
                    SetTarget(null);
                }
                
                if (debug && targetToRemove != null)
                {
                    Debug.Log($"Target removed: {targetToRemove.name}");
                }
            }
        }
        
        private void ValidateCurrentTarget()
        {
            // Check if current target is still valid
            if (currentTarget != null)
            {
                // If target is inactive, too far, or not on the right layer, clear it
                if (!currentTarget.activeInHierarchy || 
                    Vector3.Distance(currentTarget.transform.position, transform.position) > detectionRange ||
                    !IsInLayerMask(currentTarget.layer, targetLayers))
                {
                    SetTarget(null);
                }
            }
        }
        
        private bool IsInLayerMask(int layer, LayerMask layerMask)
        {
            return ((1 << layer) & layerMask) != 0;
        }
        
        /// <summary>
        /// Get the currently selected target
        /// </summary>
        /// <returns>The current target GameObject or null if none is selected</returns>
        public GameObject GetCurrentTarget()
        {
            // First validate our target is still good
            ValidateCurrentTarget();
            
            return currentTarget;
        }
        
        /// <summary>
        /// Manually set the current target
        /// </summary>
        /// <param name="target">Target to select</param>
        /// <returns>True if the target was set successfully</returns>
        public bool SetTarget(GameObject target)
        {
            // If the target is the same, don't do anything
            if (target == currentTarget)
                return true;
                
            // Validate the target
            if (target != null)
            {
                // Check if it's a valid target (in range and on the right layer)
                if (Vector3.Distance(target.transform.position, transform.position) > detectionRange ||
                    !IsInLayerMask(target.layer, targetLayers))
                {
                    return false;
                }
                
                // Make sure it's in our available targets
                if (!availableTargets.Contains(target.transform))
                {
                    availableTargets.Add(target.transform);
                    TrySubscribeToTargetEvents(target);
                }
            }
            
            // Set the target
            GameObject previousTarget = currentTarget;
            currentTarget = target;
            
            // Notify listeners
            if (previousTarget != currentTarget)
            {
                OnTargetChanged?.Invoke(currentTarget);
            }
            
            return true;
        }
        
        /// <summary>
        /// Clear the current target
        /// </summary>
        public void ClearTarget()
        {
            SetTarget(null);
        }
        
        #endregion

        #region Target Selection Logic

        private void GetTargetInInputDirection()
        {
            if (!canChangeTarget) return;
            if (!Input || Input.PlayerActions.IsUnityNull() || Input.PlayerActions.Movement == null) return;

            var moveDirection = Input.PlayerActions.Movement.ReadValue<Vector2>();
            
            // If we don't have input but we have no current target, try to find the closest target
            if (moveDirection.magnitude < 0.1f)
            {
                // Only fall back to closest target if we don't already have one
                if (currentTarget == null)
                {
                    GameObject closestTarget = GetClosestTarget()?.gameObject;
                    if (closestTarget != null)
                    {
                        SetTarget(closestTarget);
                    }
                }
                return;
            }
            
            var inputDirection = new Vector3(moveDirection.x, 0, moveDirection.y);

            if (mainCamera != null)
            {
            inputDirection = mainCamera.transform.TransformDirection(inputDirection);
            inputDirection.y = 0;
            inputDirection.Normalize();

                var closestTarget = GetClosestTargetInDirection(inputDirection);

                if (closestTarget &&
                    (Vector3.Distance(transform.position, closestTarget.position)) <= detectionRange)
                {
                    SetTarget(closestTarget.gameObject);

                    if (debug)
                    {
                        Debug.Log(
                            $"Found target: {currentTarget.name} at distance {Vector3.Distance(transform.position, closestTarget.position)}");
                    }
                }
            }
            else if (debug)
            {
                Debug.LogWarning("Main camera is null, cannot transform input direction");
            }
        }

        private Transform GetClosestTargetInDirection(Vector3 inputDirection)
        {
            Transform closestTarget = null;
            var minDistance = float.MaxValue;

            // Create a temporary list to avoid issues with collection being modified
            List<Transform> validTargets = new List<Transform>(availableTargets);
            
            foreach (var target in validTargets)
            {
                // Skip null or inactive targets
                if (!target || !target.gameObject.activeInHierarchy) 
                    continue;
                
                // Skip targets not on the target layers
                if (!IsInLayerMask(target.gameObject.layer, targetLayers))
                    continue;
                
                var distanceToTarget = Vector3.Distance(transform.position, target.position);
                
                // Skip targets outside detection range
                if (distanceToTarget > detectionRange)
                    continue;
                    
                var targetDirection = (target.position - transform.position).normalized;
                var dotProduct = Vector3.Dot(inputDirection, targetDirection);
                
                if (dotProduct > dotProductThreshold && distanceToTarget < minDistance)
                {
                    minDistance = distanceToTarget;
                    closestTarget = target;
                }
            }

            return closestTarget;
        }
        
        /// <summary>
        /// Get the closest target regardless of direction
        /// </summary>
        /// <returns>Transform of the closest target or null if none found</returns>
        public Transform GetClosestTarget()
        {
            Transform closestTarget = null;
            var minDistance = float.MaxValue;
            
            foreach (var target in availableTargets)
            {
                if (target == null || !target.gameObject.activeInHierarchy)
                    continue;
                
                // Skip targets not on the target layers    
                if (!IsInLayerMask(target.gameObject.layer, targetLayers))
                    continue;
                    
                var distance = Vector3.Distance(transform.position, target.position);
                
                if (distance < minDistance && distance <= detectionRange)
                {
                    minDistance = distance;
                    closestTarget = target;
                }
            }

            return closestTarget;
        }

        #endregion
        
        #region Configuration Methods
        
        /// <summary>
        /// Set detection parameters dynamically
        /// </summary>
        /// <param name="range">Detection range</param>
        /// <param name="threshold">Dot product threshold</param>
        public void SetDetectionParameters(float range, float threshold)
        {
            detectionRange = range;
            dotProductThreshold = threshold;
            
            if (debug)
            {
                Debug.Log($"Target detection parameters updated - Range: {range}, Threshold: {threshold}");
            }
        }
        
        /// <summary>
        /// Reset detection parameters to defaults
        /// </summary>
        /// <param name="defaultRange">Default detection range</param>
        /// <param name="defaultThreshold">Default dot product threshold</param>
        public void ResetToDefaultParameters(float defaultRange = 10f, float defaultThreshold = 0.15f)
        {
            detectionRange = defaultRange;
            dotProductThreshold = defaultThreshold;
        }
        
        /// <summary>
        /// Set the update interval for target detection
        /// </summary>
        /// <param name="interval">Interval in seconds</param>
        public void SetDetectionInterval(float interval)
        {
            detectionInterval = Mathf.Clamp(interval, 0.05f, 0.5f);
            
            // Restart detection routine with new interval
            if (detectionRoutine != null)
            {
                StartDetectionRoutine();
            }
        }

        #endregion
        
        #region Target Event Handling

        private void SubscribeToTargetEvents()
        {
            foreach (var target in availableTargets)
            {
                if (target != null)
                {
                    TrySubscribeToTargetEvents(target.gameObject);
                }
            }
        }
        
        private void UnsubscribeFromTargetEvents()
        {
            // Unsubscribe from all death events for BaseEnemy components
            foreach (var enemy in FindObjectsOfType<BaseEnemy>())
            {
                if (IsInLayerMask(enemy.gameObject.layer, targetLayers))
                {
                    enemy.OnDeath -= HandleTargetDeath;
                }
            }
            
            // Add more unsubscribe logic here for other target types
        }
        
        private void TrySubscribeToTargetEvents(GameObject target)
        {
            // Try to subscribe to BaseEnemy death events
            BaseEnemy enemy = target.GetComponent<BaseEnemy>();
            if (enemy != null)
            {
                enemy.OnDeath += HandleTargetDeath;
            }
            
            // Add more event subscription logic here for other target types
            // Example: IDestructible, IInteractable, etc.
        }

        private void HandleTargetDeath(IEnemy deadEnemy)
        {
            HandleTargetDestroyed(deadEnemy.Transform);
        }
        
        private void HandleTargetDestroyed(Transform targetTransform)
        {
            // Remove the target from our list
            if (availableTargets.Contains(targetTransform))
            {
                availableTargets.Remove(targetTransform);
            }
            
            // Clear current target if it was the one destroyed
            if (currentTarget != null && currentTarget.transform == targetTransform)
            {
                SetTarget(null);
            }
        }

        #endregion
        
        #region Public Target Registration
        
        /// <summary>
        /// Register a new target with the detection system
        /// </summary>
        /// <param name="targetTransform">Transform of the target to register</param>
        public void RegisterTarget(Transform targetTransform)
        {
            // Only register if it's on the target layer
            if (IsInLayerMask(targetTransform.gameObject.layer, targetLayers) && 
                !availableTargets.Contains(targetTransform))
            {
                availableTargets.Add(targetTransform);
                
                // Subscribe to events
                TrySubscribeToTargetEvents(targetTransform.gameObject);
            }
        }
        
        /// <summary>
        /// Unregister a target from the detection system
        /// </summary>
        /// <param name="targetTransform">Transform of the target to unregister</param>
        public void UnregisterTarget(Transform targetTransform)
        {
            if (availableTargets.Contains(targetTransform))
            {
                availableTargets.Remove(targetTransform);
                
                // If it was the current target, clear it
                if (currentTarget != null && currentTarget.transform == targetTransform)
                {
                    SetTarget(null);
                }
            }
        }
        
        #endregion

        #region Gizmos Debug Visualization

        private void OnDrawGizmosSelected()
        {
            if (!debug) return;
            
            // Draw the detection range as a wire sphere
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // Show the current target with a line
            if (currentTarget != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, currentTarget.transform.position);
                
                // Draw sphere on the target
                Gizmos.DrawWireSphere(currentTarget.transform.position, 0.5f);
            }
            
            // Calculate the angle from the dot product threshold
            // dot = cos(angle) => angle = acos(dot)
            float angleInRadians = Mathf.Acos(dotProductThreshold);
            float angleInDegrees = angleInRadians * Mathf.Rad2Deg;
            
            // Draw the directional cone representing the dot product threshold
            DrawDirectionalCone(angleInDegrees);
            
            // Draw lines to potential targets within detection range
            if (Application.isPlaying)
            {
                Vector3 inputDirection = Vector3.zero;
                
                // Get input direction if available
                if (Input != null)
                {
                    var moveDirection = Input.PlayerActions.Movement.ReadValue<Vector2>();
                    inputDirection = new Vector3(moveDirection.x, 0, moveDirection.y);
                    
                    if (inputDirection != Vector3.zero && mainCamera != null)
                    {
                        inputDirection = mainCamera.transform.TransformDirection(inputDirection);
                        inputDirection.y = 0;
                        inputDirection.Normalize();
                        
                        // Draw the input direction
                        Gizmos.color = Color.blue;
                        Gizmos.DrawRay(transform.position, inputDirection * 3f);
                    }
                }
                
                // Draw lines to all targets and highlight valid ones
                foreach (var target in availableTargets)
                {
                    if (target == null) continue;
                    
                    // Skip if not on target layer
                    if (!IsInLayerMask(target.gameObject.layer, targetLayers))
                        continue;
                    
                    Vector3 directionToTarget = (target.position - transform.position).normalized;
                    float distance = Vector3.Distance(transform.position, target.position);
                    
                    if (distance <= detectionRange)
                    {
                        float dotProduct = Vector3.Dot(inputDirection, directionToTarget);
                        
                        // Color based on whether it's within the dot product threshold
                        if (dotProduct > dotProductThreshold && inputDirection != Vector3.zero)
                        {
                            Gizmos.color = new Color(0, 1, 0, 0.5f); // Green for valid targets
                        }
                        else
                        {
                            Gizmos.color = new Color(1, 0.5f, 0, 0.3f); // Orange for in-range but wrong angle
                        }
                        
                        Gizmos.DrawLine(transform.position, target.position);
                    }
                    else
                    {
                        // Out of range targets
                        Gizmos.color = new Color(1, 0, 0, 0.1f); // Light red for out-of-range
                        Gizmos.DrawLine(transform.position, target.position);
                    }
                }
            }
        }

        private void DrawDirectionalCone(float angle)
        {
            if (angle > 175f) 
            {
                // Don't draw the cone if angle is too large (almost a full sphere)
                return;
            }
            
            Vector3 inputDirection = Vector3.forward;
            Camera cam = Camera.current ?? Camera.main;
            
            if (Application.isPlaying && Input != null && mainCamera != null)
            {
                var moveDirection = Input.PlayerActions.Movement.ReadValue<Vector2>();
                if (moveDirection != Vector2.zero)
                {
                    inputDirection = new Vector3(moveDirection.x, 0, moveDirection.y);
                    inputDirection = mainCamera.transform.TransformDirection(inputDirection);
                    inputDirection.y = 0;
                    inputDirection.Normalize();
                }
            }
            
            // Transform direction to be relative to the camera in Editor mode
            if (!Application.isPlaying && cam != null)
            {
                inputDirection = cam.transform.forward;
                inputDirection.y = 0;
                inputDirection.Normalize();
            }
            
            Color coneColor = new Color(1, 1, 0, 0.2f); // Transparent yellow
            
            // Draw the cone
            DrawWireCone(transform.position, inputDirection, angle, detectionRange, coneColor);
        }

        private void DrawWireCone(Vector3 position, Vector3 direction, float angle, float distance, Color color)
        {
            Gizmos.color = color;
            
            // Calculate the number of segments based on the angle
            int segments = Mathf.Max(12, Mathf.FloorToInt(angle / 5f));
            
            // Calculate the radius of the cone at the farthest point
            float radius = distance * Mathf.Sin(angle * Mathf.Deg2Rad);
            
            // Draw the cone outline
            Vector3 forwardOffset = direction * distance;
            Vector3 prevPoint = Vector3.zero;
            
            // Draw edges of the cone
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float currentAngle = t * angle * 2f * Mathf.PI / 360f;
                
                // Create rotation for the current segment
                Quaternion rotation = Quaternion.AngleAxis(currentAngle * Mathf.Rad2Deg, Vector3.up);
                Vector3 rotatedDir = rotation * direction;
                
                // Calculate perpendicular direction to create the circle
                Vector3 perpendicularDir = Vector3.Cross(rotatedDir, Vector3.up).normalized;
                if (perpendicularDir.magnitude < 0.001f)
                {
                    perpendicularDir = Vector3.Cross(rotatedDir, Vector3.right).normalized;
                }
                
                // Rotate the perpendicular direction to create the cone circle
                Vector3 circleDir = Quaternion.AngleAxis(90, rotatedDir) * perpendicularDir;
                Vector3 pointOnCone = position + forwardOffset + circleDir * radius;
                
                // Draw the cone edge
                Gizmos.DrawLine(position, pointOnCone);
                
                // Connect the points to form the cone's outer circumference
                if (i > 0)
                {
                    Gizmos.DrawLine(prevPoint, pointOnCone);
                }
                
                prevPoint = pointOnCone;
            }
        }

        #endregion
    }
}