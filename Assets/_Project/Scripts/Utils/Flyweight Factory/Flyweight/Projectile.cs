using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityUtils;

namespace Flyweight
{
    public class Projectile : Flyweight
    {
        private new ProjectileSettings settings => (ProjectileSettings) base.settings;

        #region Properties

        [Header("Runtime Properties")]
        protected Vector3 direction;
        protected float damage;
        protected GameObject owner;
        protected Vector3 targetPosition;
        protected bool useParabolicTrajectory;
        protected float parabolicHeight;
        protected float speed;
        protected float journeyLength;
        protected float startTime;
        protected Vector3 startPosition;
        protected bool isInitialized = false;
        protected float distanceTraveled = 0f;
        protected Vector3 lastPosition;
        protected float maxDistance = 50f;
        protected bool flatTrajectory = false;
        protected float timeToTarget = 1.2f;
        protected float originalTimeToTarget = 1.2f; // Store the original time for reference

        // Parabolic movement tracking
        protected float parabolicElapsedTime = 0f;
        protected float parabolicNormalizedTime = 0f;
        protected Vector3 currentParabolicTarget;  // Target position that can be modified by homing
        protected float remainingDistance; // Distance left to travel
        
        // Bezier curve control points
        protected Vector3 bezierControlPoint;
        protected Vector3 previousBezierControlPoint;
        protected float bezierHeightMultiplier = 2.0f;
        protected float controlPointLerpSpeed = 0.25f;
        
        // Piercing variables
        protected int pierceCount = 0;
        protected int maxPierceCount = 0;
        protected HashSet<GameObject> hitObjects = new HashSet<GameObject>();
        
        // Homing variables
        protected bool isHoming = false;
        protected float homingStrength = 0f;
        protected float homingAngle = 45f;
        protected Transform homingTarget = null;
        protected float homingCheckInterval = 0.2f;
        protected float lastHomingCheck = 0f;
        protected LayerMask targetLayers = -1;
        protected bool isParabolicHoming = false;
        protected float parabolicHomingFactor = 0.8f;
        
        // Parabolic projectile area damage
        protected float areaRadius = 2f;
        protected int predictionZoneId = -1;
        protected Transform predictionZoneTarget;
        protected bool showPredictionZone = true;
        
        // Cutoff homing variables
        protected bool useCutoffHoming = false;
        protected float homingCutoffTime = 0.7f;
        protected float homingCutoffFade = 0.1f;
        protected bool pastCutoffPoint = false;
        protected Vector3 finalHomingTarget;    // The target position when cutoff occurred
        protected Vector3 finalControlPoint;    // The control point when cutoff occurred
        protected Vector3 finalTangent;         // The curve tangent at cutoff point
        
        [HideInInspector] public TrailRenderer[] trailRenderers;
        
        // Component references
        private Collider _collider;
        private Rigidbody _rigidbody;
        
        #endregion

        #region Lifecycle Methods

        private void OnEnable()
        {
            ResetState();
            ConfigureProjectile();
            StartCoroutine(DespawnAfterDelay(settings.despawnDelay));
        }
        
        private void Update()
        {
            if (!isInitialized) return;
            
            // Check for homing targets based on trajectory type
            if (isHoming && Time.time - lastHomingCheck > homingCheckInterval)
            {
                lastHomingCheck = Time.time;
                
                // Try to find a target if we don't have one or it's invalid
                if (homingTarget == null || !homingTarget.gameObject.activeInHierarchy)
                {
                    FindHomingTarget();
                }
            }
            
            // Update specific logic based on trajectory type
            if (!useParabolicTrajectory)
            {
                // Straight projectile specific updates
                if (isHoming && homingTarget != null) 
                    UpdateStraightHomingLogic();
                
                TrackDistance();
            }
        }

        private void FixedUpdate()
        {
            if (!isInitialized) return;
            
            if (useParabolicTrajectory)
            {
                UpdateParabolicMovement();
            }
            else
            {
                // Move straight projectiles
                transform.position += (direction * (speed * Time.deltaTime));
            }
        }
        
        protected void OnDisable()
        {
            CleanupPredictionZone();
            
            if (trailRenderers != null)
            {
                foreach (var trail in trailRenderers)
                {
                    if (trail != null)
                    {
                        trail.enabled = false;
                        trail.Clear();
                    }
                }
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            HandleHit(other.gameObject, transform.position);
        }
        
        private void OnCollisionEnter(Collision collision)
        {
            HandleHit(collision.gameObject, collision.contacts[0].point);
        }
        
        #endregion

        #region Initialization
        
        private void ResetState()
        {
            // Reset basic state
            isInitialized = false;
            distanceTraveled = 0f;
            lastPosition = transform.position;
            parabolicElapsedTime = 0f;
            parabolicNormalizedTime = 0f;
            currentParabolicTarget = Vector3.zero;
            bezierControlPoint = Vector3.zero;
            previousBezierControlPoint = Vector3.zero;
            remainingDistance = 0f;
            
            // Reset cutoff homing variables
            pastCutoffPoint = false;
            finalHomingTarget = Vector3.zero;
            finalControlPoint = Vector3.zero;
            finalTangent = Vector3.zero;
            
            // Get settings values
            maxDistance = settings.maxTravelDistance > 0 ? settings.maxTravelDistance : 50f;
            flatTrajectory = settings.flatTrajectory;
            timeToTarget = settings.timeToTarget;
            originalTimeToTarget = timeToTarget;
            
            // Reset piercing state
            pierceCount = 0;
            maxPierceCount = settings.maxPierceCount;
            hitObjects.Clear();
            
            // Reset homing state
            isHoming = settings.isHoming;
            homingStrength = settings.homingStrength;
            homingAngle = settings.homingAngle;
            homingTarget = null;
            lastHomingCheck = 0f;
            isParabolicHoming = settings.enableHomingForParabolic;
            parabolicHomingFactor = settings.parabolicHomingFactor;
            useCutoffHoming = settings.useCutoffHoming;
            homingCutoffTime = settings.homingCutoffTime;
            homingCutoffFade = settings.homingCutoffFade;
            
            // Reset area damage settings
            areaRadius = settings.areaRadius;
            predictionZoneId = -1;
            predictionZoneTarget = null;
            showPredictionZone = settings && settings.predictionZoneSettings != null ? settings.showPredictionZone : false;
            
            if (trailRenderers != null)
            {
                foreach (var trail in trailRenderers)
                {
                    if (trail != null)
                    {
                        trail.enabled = true;
                    }
                }
            }
        }
        
        private void ConfigureProjectile()
        {
            // Apply scale settings
            if (settings.projectileScale != Vector3.one)
            {
                transform.localScale = settings.projectileScale;
            }
            
            // Configure targeting layers
            int projectileLayer = gameObject.layer;
            if (projectileLayer == LayerMask.NameToLayer("Projectile"))
            {
                targetLayers = LayerMask.GetMask("Player", "Enemy");
            }
            
            // Configure visual scale for area damage projectiles
            ConfigureVisualScale();
        }
        
        private void ConfigureVisualScale()
        {
            Transform modelTransform = transform.Find("Model");
            if (modelTransform == null) return;
            
            // Scale the projectile model based on area radius for parabolic area damage projectiles
            if (settings.useParabolicTrajectory && !settings.useDirectCollisionForParabolic)
            {
                // Hint at explosion size with visual scale
                float projectileModelScale = 0.5f + (areaRadius / 5f);
                projectileModelScale = Mathf.Min(projectileModelScale, 1.5f);
                
                // Apply the combined scale
                Vector3 baseScale = settings.projectileScale;
                modelTransform.localScale = new Vector3(
                    baseScale.x * projectileModelScale,
                    baseScale.y * projectileModelScale,
                    baseScale.z * projectileModelScale
                );
            }
            else
            {
                // For other projectiles, use the default scale
                modelTransform.localScale = Vector3.one;
            }
        }
        
        // Initialize for straight line movement
        public void Initialize(Vector3 dir, float dmg, GameObject projectileOwner)
        {
            // Apply flat trajectory if configured
            if (flatTrajectory)
            {
                dir.y = 0;
                dir.Normalize();
            }
            
            // Set basic properties
            direction = dir.normalized;
            damage = dmg;
            owner = projectileOwner;
            speed = settings.speed;
            startPosition = transform.position;
            lastPosition = startPosition;
            distanceTraveled = 0f;
            startTime = Time.time;
            
            // Configure projectile behavior
            isInitialized = true;
            useParabolicTrajectory = false;
            transform.rotation = Quaternion.LookRotation(direction);
            
            // Configure special behaviors
            isHoming = settings.isHoming;
            maxPierceCount = settings.maxPierceCount;
            isParabolicHoming = false; // Not applicable for straight projectiles

            // Configure physics components
            ConfigurePhysicsComponents();
        }
        
        // Initialize for parabolic trajectory
        public void InitializeParabolic(Vector3 start, Vector3 target, float dmg, GameObject projectileOwner, float arcHeight = -1f, float projectileSpeed = -1f)
        {
            // Set basic properties
            startPosition = start;
            lastPosition = startPosition;
            distanceTraveled = 0f;
            targetPosition = target;
            currentParabolicTarget = target; // Initialize current target to original target
            damage = dmg;
            owner = projectileOwner;
            useParabolicTrajectory = true;
            
            // Calculate initial direction
            direction = (target - start).normalized;
            
            // Determine parabolic height based on settings and parameters
            parabolicHeight = DetermineParabolicHeight(arcHeight);
            
            // Set up initial bezier control point (high point in the middle of the arc)
            SetupBezierControlPoint(start, target, parabolicHeight);
            
            // Calculate journey metrics
            journeyLength = CalculateBezierLength(startPosition, bezierControlPoint, targetPosition, 10);
            remainingDistance = journeyLength;
            
            // Determine time to target based on speed or settings
            timeToTarget = DetermineTimeToTarget(projectileSpeed);
            originalTimeToTarget = timeToTarget;
            
            // Configure projectile behavior
            isInitialized = true;
            
            // Configure homing for parabolic projectiles if enabled
            isHoming = settings.enableHomingForParabolic && settings.isHoming;
            isParabolicHoming = isHoming;
            homingStrength = settings.homingStrength;
            homingAngle = settings.homingAngle;
            parabolicHomingFactor = settings.parabolicHomingFactor;
            
            // Reset parabolic movement tracking
            parabolicElapsedTime = 0f;
            parabolicNormalizedTime = 0f;
            
            // Configure physics components
            ConfigurePhysicsComponents();
            
            // Create prediction zone if needed
            if (showPredictionZone && !settings.useDirectCollisionForParabolic)
            {
                CreatePredictionZone();
            }
        }
        
        private float DetermineParabolicHeight(float arcHeight)
        {
            if (arcHeight > 0)
            {
                return arcHeight; // Use provided height
            }
            else if (settings.useRandomHeight)
            {
                // Use random height within configured range
                float randomHeight = Random.Range(settings.minRandomHeight, settings.maxRandomHeight);
                
                #if UNITY_EDITOR
                if (settings.showDebugInfo)
                {
                    Debug.Log($"Using random parabolic height: {randomHeight}");
                }
                #endif
                
                return randomHeight;
            }
            else
            {
                return settings.parabolicHeight; // Use default height
            }
        }
        
        private float DetermineTimeToTarget(float projectileSpeed)
        {
            if (projectileSpeed > 0)
            {
                // Convert legacy speed parameter to time
                float calculatedTime = Mathf.Max(0.5f, journeyLength / projectileSpeed);
                
                #if UNITY_EDITOR
                if (settings.showDebugInfo)
                {
                    Debug.Log($"Parabolic projectile using provided speed ({projectileSpeed}) to calculate time: {calculatedTime}s");
                }
                #endif
                
                return calculatedTime;
            }
            else
            {
                // Use the configured time directly
                return settings.timeToTarget;
            }
        }
        
        private void ConfigurePhysicsComponents()
        {
            _collider = GetComponentInChildren<Collider>();
            if (_collider)
            {
                _collider.isTrigger = true;
            }

            _rigidbody = GetComponentInChildren<Rigidbody>();
            if (_rigidbody)
            {
                _rigidbody.isKinematic = true;
                _rigidbody.useGravity = false;
            }
        }
        
        private void SetupBezierControlPoint(Vector3 start, Vector3 end, float height)
        {
            // Calculate the midpoint between start and end
            Vector3 midPoint = (start + end) * 0.5f;
            
            // Calculate a point above the midpoint based on the parabolic height
            Vector3 up = Vector3.up;
            
            // Add some forward bias to the control point to make the arc more natural
            Vector3 arcDirection = (end - start).normalized;
            arcDirection.y = 0; // Keep it horizontal
            
            // Calculate the control point with forward bias and height
            bezierControlPoint = midPoint + up * (height * bezierHeightMultiplier) + arcDirection * (height * 0.5f);
            previousBezierControlPoint = bezierControlPoint;
        }
        
        #endregion

        #region Movement Methods
        
        // Update parabolic movement in FixedUpdate
        private void UpdateParabolicMovement()
        {
            // Update time progression
            parabolicElapsedTime += Time.fixedDeltaTime;
            parabolicNormalizedTime = Mathf.Clamp01(parabolicElapsedTime / timeToTarget);
            
            // Check if we've reached the cutoff point
            if (useCutoffHoming && isParabolicHoming && !pastCutoffPoint)
            {
                // If we're approaching the cutoff, capture the current trajectory
                if (parabolicNormalizedTime >= homingCutoffTime - homingCutoffFade)
                {
                    // Gradually reduce homing as we approach cutoff
                    float cutoffProgress = (parabolicNormalizedTime - (homingCutoffTime - homingCutoffFade)) / homingCutoffFade;
                    
                    // If past cutoff time, freeze trajectory
                    if (parabolicNormalizedTime >= homingCutoffTime)
                    {
                        // Freeze trajectory at this point
                        finalHomingTarget = currentParabolicTarget;
                        finalControlPoint = bezierControlPoint;
                        
                        // Calculate final tangent for rotation
                        finalTangent = CalculateBezierTangent(
                            startPosition,
                            finalControlPoint,
                            finalHomingTarget,
                            parabolicNormalizedTime
                        );
                        
                        // Mark as past cutoff
                        pastCutoffPoint = true;
                        
                        #if UNITY_EDITOR
                        if (settings.showDebugInfo)
                        {
                            Debug.Log($"Projectile cutoff reached at t={parabolicNormalizedTime}. Freezing trajectory.");
                        }
                        #endif
                    }
                    else
                    {
                        // Apply homing but with reduced effect as we approach cutoff
                        float homingScale = 1.0f - cutoffProgress;
                        ApplyHomingWithScale(homingScale);
                    }
                }
                else
                {
                    // Normal homing behavior before cutoff
                    if (isParabolicHoming && homingTarget != null && homingTarget.gameObject.activeInHierarchy)
                    {
                        UpdateParabolicHoming();
                    }
                }
            }
            else if (isParabolicHoming && homingTarget != null && homingTarget.gameObject.activeInHierarchy && !pastCutoffPoint)
            {
                // Standard homing behavior (no cutoff)
                UpdateParabolicHoming();
            }
            
            // When after the cutoff, use the frozen trajectory
            if (pastCutoffPoint)
            {
                // Use the frozen values for position calculation
                Vector3 currentPos = EvaluateBezierCurve(
                    startPosition, 
                    finalControlPoint, 
                    finalHomingTarget, 
                    parabolicNormalizedTime
                );
                
                // Update position
                transform.position = currentPos;
                
                // Update rotation using the frozen tangent direction
                Vector3 tangent = CalculateBezierTangent(
                    startPosition,
                    finalControlPoint,
                    finalHomingTarget,
                    parabolicNormalizedTime
                );
                
                if (tangent.magnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(tangent.normalized);
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        targetRotation,
                        Time.fixedDeltaTime * 12f
                    );
                }
            }
            else
            {
                // Only adjust time if not near completion and not past cutoff
                if (parabolicNormalizedTime < 0.85f)
                {
                    // Recalculate path length if control point changed significantly
                    if (Vector3.Distance(bezierControlPoint, previousBezierControlPoint) > 0.5f)
                    {
                        float newLength = CalculateBezierLength(transform.position, bezierControlPoint, currentParabolicTarget, 5);
                        
                        // Smoothly adjust the time to target based on the new path length
                        float remainingFraction = 1f - parabolicNormalizedTime;
                        if (remainingFraction > 0.1f)
                        {
                            float effectiveSpeed = journeyLength / originalTimeToTarget;
                            float idealRemainingTime = newLength / effectiveSpeed;
                            
                            // More aggressive time adjustment for homing projectiles
                            float targetTotalTime = parabolicElapsedTime + idealRemainingTime;
                            timeToTarget = Mathf.Lerp(timeToTarget, targetTotalTime, Time.fixedDeltaTime * 0.8f);
                        }
                        
                        // Update previous control point for next comparison
                        previousBezierControlPoint = bezierControlPoint;
                    }
                }
            
                // Update prediction zone if present
                if (predictionZoneId != -1)
                {
                    UpdatePredictionZone(parabolicNormalizedTime);
                    
                    // Update prediction zone target if homing
                    if (isParabolicHoming && homingTarget != null && predictionZoneTarget != null)
                    {
                        predictionZoneTarget.position = currentParabolicTarget;
                    }
                }
                
                // Remember old position for distance calculation
                Vector3 oldPosition = transform.position;
                
                // Calculate position using quadratic Bezier curve
                Vector3 currentPos = EvaluateBezierCurve(
                    startPosition, 
                    bezierControlPoint, 
                    currentParabolicTarget, 
                    parabolicNormalizedTime
                );
                
                // Update position
                transform.position = currentPos;
                
                // Track distance
                distanceTraveled += Vector3.Distance(oldPosition, currentPos);
                
                // Update rotation to follow the curve
                UpdateBezierRotation();
            }
            
            // Check if we've reached the target
            if (parabolicNormalizedTime >= 1.0f)
            {
                CompleteParabolicMovement();
            }
        }
        
        // Apply homing effect with a scaling factor (used for cutoff fading)
        private void ApplyHomingWithScale(float scale)
        {
            // Save current values to provide as context for UpdateParabolicHoming
            float savedHomingFactor = parabolicHomingFactor;
            
            // Scale the homing factor temporarily
            parabolicHomingFactor *= scale;
            
            // Apply normal homing logic with scaled factor
            UpdateParabolicHoming();
            
            // Restore original values
            parabolicHomingFactor = savedHomingFactor;
        }
        
        private void UpdateParabolicHoming()
        {
            // Get the current homingTarget position - this is where we want to go
            Vector3 targetPos = homingTarget.position;
            
            // Save previous target and control point for comparison
            Vector3 previousTarget = currentParabolicTarget;
            
            // Calculate how much homing effect to apply based on trajectory progress
            // Create a curve that maintains strong homing through most of the trajectory
            float homingCurve;
            if (parabolicNormalizedTime < 0.85f)
            {
                // Strong homing for most of the trajectory
                homingCurve = 1f - Mathf.Pow(parabolicNormalizedTime - 0.5f, 2) * 0.5f;
            }
            else
            {
                // Gradual falloff at the end
                homingCurve = (1f - parabolicNormalizedTime) * 6.67f; // Scale from 1.0 at 0.85 to 0 at 1.0
            }
            
            // Apply homing factor
            float homingStrengthFactor = parabolicHomingFactor * homingCurve;
            homingStrengthFactor = Mathf.Clamp01(homingStrengthFactor);
            
            // Calculate a frame-specific adjustment amount
            float frameAdjustment = homingStrengthFactor * Time.fixedDeltaTime * 2.5f;
            frameAdjustment = Mathf.Clamp(frameAdjustment, 0.01f, 0.2f);
            
            // Update target position with increased tracking for long-range targets
            float distanceToTarget = Vector3.Distance(transform.position, targetPos);
            
            // Determine target influence based on distance and trajectory
            float targetInfluence = frameAdjustment;
            
            // Boost influence for distant targets to increase long-range homing
            if (distanceToTarget > journeyLength * 0.5f)
            {
                targetInfluence *= 1.5f;
            }
            
            // Calculate new target position with smoothing
            Vector3 newTarget = Vector3.Lerp(currentParabolicTarget, targetPos, targetInfluence);
            
            // Limit maximum deviation from original path (60% of journey length)
            float maxDeviation = journeyLength * 0.6f;
            float deviation = Vector3.Distance(newTarget, targetPosition);
            
            if (deviation > maxDeviation)
            {
                // Apply softer limiting to allow more natural arcs
                float limitFactor = maxDeviation / deviation;
                limitFactor = Mathf.Pow(limitFactor, 0.7f); // Soften the limit
                
                // Apply limit by blending between current target and unlimited new target
                newTarget = Vector3.Lerp(targetPosition, newTarget, limitFactor);
            }
            
            // Apply the target change with smoothing
            currentParabolicTarget = Vector3.Lerp(currentParabolicTarget, newTarget, 0.4f);
            
            // Update bezier control point to create a smooth arc to the new target
            UpdateBezierControlPoint();
            
            #if UNITY_EDITOR
            if (settings.showDebugInfo)
            {
                Debug.DrawLine(transform.position, currentParabolicTarget, Color.cyan, 0.1f);
                Debug.DrawLine(targetPosition, currentParabolicTarget, Color.yellow, 0.1f);
                Debug.DrawLine(homingTarget.position, currentParabolicTarget, Color.green, 0.1f);
                
                // Draw the bezier path
                DrawBezierPath(startPosition, bezierControlPoint, currentParabolicTarget, 20, Color.magenta);
                
                // Visualize control point
                Debug.DrawLine(startPosition, bezierControlPoint, Color.red, 0.1f);
                Debug.DrawLine(bezierControlPoint, currentParabolicTarget, Color.red, 0.1f);
                
                // If cutoff is enabled, draw the cutoff point
                if (useCutoffHoming)
                {
                    Vector3 cutoffPos = EvaluateBezierCurve(
                        startPosition, 
                        bezierControlPoint, 
                        currentParabolicTarget, 
                        homingCutoffTime
                    );
                    
                    // Draw a sphere at cutoff point
                    Debug.DrawLine(cutoffPos, cutoffPos + Vector3.up * 0.5f, Color.yellow, 0.1f);
                    Debug.DrawLine(cutoffPos, cutoffPos + Vector3.right * 0.5f, Color.yellow, 0.1f);
                    Debug.DrawLine(cutoffPos, cutoffPos + Vector3.forward * 0.5f, Color.yellow, 0.1f);
                    Debug.DrawLine(cutoffPos, cutoffPos - Vector3.up * 0.5f, Color.yellow, 0.1f);
                    Debug.DrawLine(cutoffPos, cutoffPos - Vector3.right * 0.5f, Color.yellow, 0.1f);
                    Debug.DrawLine(cutoffPos, cutoffPos - Vector3.forward * 0.5f, Color.yellow, 0.1f);
                }
            }
            #endif
        }
        
        private void UpdateBezierControlPoint()
        {
            // Get the midpoint between current position and target
            Vector3 midPoint = (transform.position + currentParabolicTarget) * 0.5f;
            
            // Calculate ideal height at current point in trajectory
            float heightMultiplier = Mathf.Sin((parabolicNormalizedTime * 0.8f + 0.1f) * Mathf.PI);
            float currentHeight = parabolicHeight * heightMultiplier * bezierHeightMultiplier;
            
            // Add forward bias for more natural arcs
            Vector3 arcDirection = (currentParabolicTarget - transform.position).normalized;
            Vector3 idealControlPoint = midPoint + Vector3.up * currentHeight + arcDirection * (currentHeight * 0.3f);
            
            // Smoothly adjust control point
            bezierControlPoint = Vector3.Lerp(
                bezierControlPoint, 
                idealControlPoint, 
                Time.fixedDeltaTime * controlPointLerpSpeed * 5f
            );
        }
        
        private void UpdateBezierRotation()
        {
            // Calculate tangent to the bezier curve at the current point
            Vector3 tangent = CalculateBezierTangent(
                startPosition,
                bezierControlPoint,
                currentParabolicTarget,
                parabolicNormalizedTime
            );
            
            if (tangent.magnitude > 0.01f)
            {
                // Create a rotation that points along the tangent
                Quaternion targetRotation = Quaternion.LookRotation(tangent.normalized);
                
                // Apply smooth rotation
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    Time.fixedDeltaTime * 12f
                );
            }
        }
        
        private void CompleteParabolicMovement()
        {
            // For area damage, trigger explosion at landing point
            if (!settings.useDirectCollisionForParabolic)
            {
                TriggerAreaDamage();
            }
            
            // Return to pool
            ReturnToPool();
        }
        
        // Track distance and timeout for straight projectiles
        private void TrackDistance()
        {
            float frameDist = Vector3.Distance(transform.position, lastPosition);
            distanceTraveled += frameDist;
            lastPosition = transform.position;
            
            // Check if we've exceeded max travel distance
            if (distanceTraveled >= maxDistance)
            {
                #if UNITY_EDITOR
                if (settings.showDebugInfo)
                {
                    Debug.Log($"Projectile exceeded max distance ({maxDistance}m), returning to pool");
                }
                #endif
                ReturnToPool();
                return;
            }
            
            // Check for timeout
            float timeout = (maxDistance / speed) * 1.5f;
            if (Time.time - startTime > timeout)
            {
                #if UNITY_EDITOR
                if (settings.showDebugInfo)
                {
                    Debug.Log($"Projectile exceeded timeout ({timeout}s), returning to pool");
                }
                #endif
                ReturnToPool();
            }
        }
        
        #endregion

        #region Hit Detection and Damage
        
        protected virtual void HandleHit(GameObject hitObject, Vector3 hitPoint)
        {
            // Skip invalid hits
            if (hitObject == owner) return;
            if (hitObjects.Contains(hitObject)) return;
            
            // For parabolic projectiles with area damage, skip direct hit processing
            if (useParabolicTrajectory && !settings.useDirectCollisionForParabolic)
            {
                return;
            }
            
            // Add to hit objects list to prevent hitting the same target twice
            hitObjects.Add(hitObject);
            
            // Calculate hit direction
            Vector3 hitDirection = CalculateHitDirection(hitObject);
            
            // Create hit context and apply damage
            var hitContext = HitContext.Create(hitDirection, hitPoint, damage, owner);
            hitContext.TriggerOnHit(hitObject);
            
            // Spawn impact effects
            SpawnHitEffects(hitPoint, hitDirection);
            
            // Handle projectile lifecycle after hit
            HandlePostHitBehavior();
        }
        
        private Vector3 CalculateHitDirection(GameObject hitObject)
        {
            // For parabolic projectiles with direct collision, calculate direction to target
            if (useParabolicTrajectory && settings.useDirectCollisionForParabolic)
            {
                return (hitObject.transform.position - transform.position).normalized;
            }
            
            // For straight projectiles, use the current direction
            return direction;
        }
        
        private void SpawnHitEffects(Vector3 hitPoint, Vector3 hitDirection)
        {
            // Spawn impact VFX
            if (settings.hitVFXSettings != null)
            {
                float impactScale = 1.0f;
                Vector3 vfxScale = Vector3.one * impactScale;
                
                // Adjust scale based on projectile scale
                if (settings.projectileScale != Vector3.one)
                {
                    float avgCustomScale = (settings.projectileScale.x + settings.projectileScale.y + settings.projectileScale.z) / 3f;
                    vfxScale *= avgCustomScale;
                }
                
                // Spawn and configure VFX
                VFXFlyweight vfx = VFXFlyweightHelper.SpawnVfxAtPosition(
                    settings.hitVFXSettings,
                    hitPoint,
                    Quaternion.LookRotation(hitDirection)
                );
                
                if (vfx != null)
                {
                    vfx.transform.localScale = vfxScale;
                }
            }
            
            // Play hit sound
            if (settings.hitSound != null)
            {
                AudioSource.PlayClipAtPoint(settings.hitSound, hitPoint, 0.5f);
            }
        }
        
        private void HandlePostHitBehavior()
        {
            // For parabolic projectiles with direct collision, always return to pool
            if (useParabolicTrajectory && settings.useDirectCollisionForParabolic)
            {
                ReturnToPool();
                return;
            }
            
            // For piercing projectiles, check pierce count
            if (settings.canPierceTargets)
            {
                pierceCount++;
                
                if (pierceCount >= maxPierceCount)
                {
                    ReturnToPool();
                }
                // Otherwise continue (piercing projectile)
            }
            else
            {
                // Non-piercing projectile, return to pool after first hit
                ReturnToPool();
            }
        }
        
        // Trigger area damage at the landing point (for parabolic projectiles)
        private void TriggerAreaDamage()
        {
            // Find all colliders within the area radius
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, areaRadius, settings.hitLayers);
            
            foreach (var hitCollider in hitColliders)
            {
                GameObject hitObject = hitCollider.gameObject;
                
                // Skip invalid targets
                if (hitObject == owner) continue;
                if (hitObjects.Contains(hitObject)) continue;
                
                // Add to hit list
                hitObjects.Add(hitObject);
                
                // Calculate damage with distance falloff
                float distance = Vector3.Distance(transform.position, hitCollider.transform.position);
                float damageMultiplier = 1f - Mathf.Clamp01(distance / areaRadius);
                float actualDamage = damage * damageMultiplier;
                
                // Apply damage with radial direction
                Vector3 hitDirection = (hitCollider.transform.position - transform.position).normalized;
                var hitContext = HitContext.Create(hitDirection, hitCollider.transform.position, actualDamage, owner);
                hitContext.TriggerOnHit(hitObject);
            }
            
            // Spawn area impact VFX
            SpawnAreaImpactEffects();
        }
        
        private void SpawnAreaImpactEffects()
        {
            // Spawn area impact VFX if available
            if (settings.hitVFXSettings != null)
            {
                // Calculate VFX scale based on area radius and modifiers
                float effectScale = areaRadius * settings.vfxScaleModifier;
                Vector3 vfxScale = Vector3.one * effectScale;
                
                // Adjust for custom projectile scale
                if (settings.projectileScale != Vector3.one)
                {
                    float avgCustomScale = (settings.projectileScale.x + settings.projectileScale.y + settings.projectileScale.z) / 3f;
                    vfxScale *= avgCustomScale;
                }
                
                // Spawn VFX
                VFXFlyweight vfx = VFXFlyweightHelper.SpawnVfxAtPosition(
                    settings.hitVFXSettings,
                    transform.position,
                    Quaternion.identity  // For area explosions, use identity rotation
                );
                
                // Apply scale
                if (vfx != null)
                {
                    vfx.transform.localScale = vfxScale;
                }
            }
            
            // Play hit sound
            if (settings.hitSound != null)
            {
                AudioSource.PlayClipAtPoint(settings.hitSound, transform.position, 0.5f);
            }
        }
        
        #endregion

        #region Homing Logic
        
        private void UpdateHomingLogic()
        {
            if (useParabolicTrajectory)
            {
                // Parabolic homing is handled in UpdateParabolicMovement
                return;
            }
            else
            {
                UpdateStraightHomingLogic();
            }
        }
        
        private void UpdateStraightHomingLogic()
        {
            // Apply homing behavior if we have a valid target
            if (homingTarget != null && homingTarget.gameObject.activeInHierarchy)
            {
                ApplyHomingBehavior();
            }
        }
        
        private void ApplyHomingBehavior()
        {
            // Calculate direction to target
            Vector3 targetDirection = (homingTarget.position - transform.position).normalized;
            
            // For flat trajectory, ignore height
            if (flatTrajectory)
            {
                targetDirection.y = 0;
                targetDirection.Normalize();
            }
            
            // Check if target is within our homing angle
            float angleToTarget = Vector3.Angle(direction, targetDirection);
            if (angleToTarget <= homingAngle)
            {
                // Gradually rotate towards the target
                float rotationSpeed = homingStrength * 180f * Time.deltaTime;
                float maxRotationAngle = Mathf.Min(angleToTarget, rotationSpeed);
                
                direction = Vector3.RotateTowards(direction, targetDirection, maxRotationAngle * Mathf.Deg2Rad, 0f);
                
                // Maintain flat trajectory if needed
                if (flatTrajectory)
                {
                    direction.y = 0;
                    direction.Normalize();
                }
                
                // Update rotation to match new direction
                transform.rotation = Quaternion.LookRotation(direction);
            }
            else
            {
                // Target moved outside of our homing angle
                homingTarget = null;
            }
        }
        
        private void FindHomingTarget()
        {
            // Find potential targets in range
            Collider[] potentialTargets = Physics.OverlapSphere(transform.position, 20f, targetLayers);
            
            Transform bestTarget = null;
            float closestAngle = homingAngle;
            float closestDistance = float.MaxValue;
            
            foreach (var potential in potentialTargets)
            {
                // Skip invalid targets
                if (potential.gameObject == owner) continue;
                if (hitObjects.Contains(potential.gameObject)) continue;
                
                // Calculate direction to this target
                Vector3 directionToTarget = (potential.transform.position - transform.position).normalized;
                float distanceToTarget = Vector3.Distance(transform.position, potential.transform.position);
                
                // For flat trajectory, ignore height
                if (flatTrajectory && !useParabolicTrajectory)
                {
                    directionToTarget.y = 0;
                    directionToTarget.Normalize();
                }
                
                // Different selection logic for parabolic vs straight projectiles
                if (useParabolicTrajectory)
                {
                    // For parabolic, prioritize closer targets
                    if (distanceToTarget < closestDistance)
                    {
                        bestTarget = potential.transform;
                        closestDistance = distanceToTarget;
                    }
                }
                else
                {
                    // Calculate angle to this target for straight projectiles
                    float angle = Vector3.Angle(direction, directionToTarget);
                    
                    // If within homing angle and better than current best, select it
                    if (angle <= homingAngle && angle < closestAngle)
                    {
                        bestTarget = potential.transform;
                        closestAngle = angle;
                    }
                }
            }
            
            homingTarget = bestTarget;
            
            // If we found a target, update prediction zone for parabolic projectiles
            if (homingTarget != null && useParabolicTrajectory && isParabolicHoming && predictionZoneTarget != null)
            {
                predictionZoneTarget.position = homingTarget.position;
            }
        }
        
        #endregion

        #region Prediction Zone Management
        
        // Create a prediction zone for the parabolic projectile's landing spot
        private void CreatePredictionZone()
        {
            // Skip creating prediction zone if settings are missing
            if (settings.predictionZoneSettings == null || HitPredictionZoneManager.Instance == null)
            {
                #if UNITY_EDITOR
                if (settings.showDebugInfo)
                {
                    Debug.LogWarning("Cannot create prediction zone - predictionZoneSettings is null or HitPredictionZoneManager not available");
                }
                #endif
                return;
            }
            
            try
            {
                // Create a target object for the prediction zone
                var targetObj = new GameObject("PredictionTarget");
                targetObj.transform.position = targetPosition;
                predictionZoneTarget = targetObj.transform;
                
                // Create the prediction zone
                predictionZoneId = HitPredictionZoneManager.Instance.CreateOrUpdateZone(
                    -1,
                    PredictionType.Circle,
                    predictionZoneTarget,
                    new Vector3(0, 0.01f, 0),
                    new Vector3(areaRadius, 0.1f, areaRadius),
                    true,
                    (progress) => {},
                    settings.predictionZoneSettings
                );
                
                // Clean up if zone creation failed
                if (predictionZoneId == -1 && predictionZoneTarget != null)
                {
                    Destroy(predictionZoneTarget.gameObject);
                    predictionZoneTarget = null;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error creating prediction zone: {e.Message}");
                
                // Clean up resources
                if (predictionZoneTarget != null)
                {
                    Destroy(predictionZoneTarget.gameObject);
                    predictionZoneTarget = null;
                }
                
                predictionZoneId = -1;
            }
        }
        
        // Update the prediction zone progression
        private void UpdatePredictionZone(float fractionOfJourney)
        {
            if (predictionZoneId != -1 && HitPredictionZoneManager.Instance != null)
            {
                HitPredictionZoneManager.Instance.UpdateZoneProgression(predictionZoneId, fractionOfJourney);
            }
        }
        
        // Clean up prediction zone resources
        private void CleanupPredictionZone()
        {
            try
            {
                // Remove the zone
                if (predictionZoneId != -1 && HitPredictionZoneManager.Instance != null)
                {
                    HitPredictionZoneManager.Instance.RemoveZone(predictionZoneId);
                    predictionZoneId = -1;
                }
                
                // Destroy the target object
                if (predictionZoneTarget != null)
                {
                    Destroy(predictionZoneTarget.gameObject);
                    predictionZoneTarget = null;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error cleaning up prediction zone: {e.Message}");
                
                // Reset state
                predictionZoneId = -1;
                predictionZoneTarget = null;
            }
        }
        
        #endregion

        #region Bezier Curve Utilities

        // Evaluate point on a quadratic Bezier curve at time t
        private Vector3 EvaluateBezierCurve(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;
            
            // B(t) = (1-t)²P₀ + 2(1-t)tP₁ + t²P₂
            return uu * p0 + 2f * u * t * p1 + tt * p2;
        }

        // Calculate tangent (first derivative) of the Bezier curve at time t
        private Vector3 CalculateBezierTangent(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            // B'(t) = 2(1-t)(P₁-P₀) + 2t(P₂-P₁)
            return 2f * (1f - t) * (p1 - p0) + 2f * t * (p2 - p1);
        }

        // Calculate the approximate length of a bezier curve using linear segments
        private float CalculateBezierLength(Vector3 p0, Vector3 p1, Vector3 p2, int segments)
        {
            float length = 0f;
            Vector3 prevPoint = p0;
            
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector3 currentPoint = EvaluateBezierCurve(p0, p1, p2, t);
                length += Vector3.Distance(prevPoint, currentPoint);
                prevPoint = currentPoint;
            }
            
            return length;
        }

        #if UNITY_EDITOR
        // Draw the bezier path in the editor for debugging
        private void DrawBezierPath(Vector3 p0, Vector3 p1, Vector3 p2, int segments, Color color)
        {
            Vector3 prevPoint = p0;
            
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector3 currentPoint = EvaluateBezierCurve(p0, p1, p2, t);
                Debug.DrawLine(prevPoint, currentPoint, color, 0.1f);
                prevPoint = currentPoint;
            }
        }
        #endif

        #endregion

        #region Utility Methods
        
        // Set custom speed for the projectile
        public void SetSpeed(float newSpeed)
        {
            speed = newSpeed;
        }

        // Set custom max distance
        public void SetMaxDistance(float distance)
        {
            maxDistance = distance;
        }
        
        // Set whether to use flat trajectory
        public void SetFlatTrajectory(bool useFlatTrajectory)
        {
            flatTrajectory = useFlatTrajectory;
            
            // Update direction if already initialized as a straight projectile
            if (isInitialized && !useParabolicTrajectory && flatTrajectory)
            {
                direction.y = 0;
                direction = direction.normalized;
            }
        }
        
        // Set whether to enable homing
        public void SetHoming(bool enableHoming, float strength = -1f, float angle = -1f)
        {
            isHoming = enableHoming;
            
            // Update parabolic homing setting if we're a parabolic projectile
            if (useParabolicTrajectory)
            {
                isParabolicHoming = enableHoming;
            }
            
            // Apply custom strength and angle if provided
            if (strength > 0) homingStrength = strength;
            if (angle > 0) homingAngle = angle;
        }
        
        // Automatic despawn after a delay
        private IEnumerator DespawnAfterDelay(float delay)
        {
            yield return Helpers.GetWaitForSeconds(delay);
            
            // Only auto-despawn if still active
            if (isInitialized)
            {
                #if UNITY_EDITOR
                if (settings.showDebugInfo)
                {
                    Debug.Log($"Projectile despawned after timeout ({delay}s)");
                }
                #endif
                ReturnToPool();
            }
        }
        
        // Return to object pool
        protected void ReturnToPool()
        {
            // Only return to pool if initialized
            if (!isInitialized) return;
            
            // Stop all coroutines
            StopAllCoroutines();
            
            // Clean up prediction zone
            CleanupPredictionZone();
            
            // Reset state
            isInitialized = false;
            distanceTraveled = 0f;
            
            // Return to factory pool
            FlyweightFactory.ReturnToPool(this);
        }
        
        #endregion
    }
}