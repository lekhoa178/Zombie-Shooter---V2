using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Locomotion;
using UnityEngine;
using UnityUtils;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using Flyweight;

namespace LocomotionSystem
{
    public class PlayerCombatState : PlayerState
    {
        readonly CountdownTimer _attackIntervalTimer;

        private const float AttackInterval = 0.2f;

        protected bool IsAttackFrame = false;
        protected readonly HashSet<IHittable> HitEnemies;
        
        protected bool IsCombo = false;
        protected bool IsFinished = false;

        protected PlayerCombatState(PlayerMovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
        {
            _attackIntervalTimer = new CountdownTimer(AttackInterval);
            HitEnemies = new HashSet<IHittable>();
        }
        
        // Helper method to get the active ComboConfiguration
        protected ComboConfiguration GetActiveComboConfig()
        {
            // First check if there's a debug override in PlayerSO
            if (StateMachine.Player.Data.DebugComboConfigOverride != null)
            {
                if (StateMachine.Player.Animator.updateMode == AnimatorUpdateMode.Fixed)
                    Debug.Log("[Debug] Using ComboConfiguration override from PlayerSO");
                return StateMachine.Player.Data.DebugComboConfigOverride;
            }
            
            // If no override, use the WeaponManager's configuration
            return WeaponManager.Instance.GetActiveComboConfiguration(StateMachine.Player);
        }

        public override void Enter()
        {
            base.Enter();
            
            _attackIntervalTimer.Reset();

            // Handle weapon attachment at the start of the attack
            var comboConfig = GetActiveComboConfig();
            if (comboConfig != null)
            {
                var config = comboConfig.AttackConfigs[StateMachine.ReusableData.CurrentComboIndex];
                if (config)
                {
                    StateMachine.Player.SocketManager.HandleWeaponForAttack(config);
                }
                
                if (config && config.EnableAttackIFrames)
                {
                    IFrameManager.Instance.StartInvincibility(config.AttackIFrameDuration);
                }
            }
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();
            
            Float();

            if (!IsFinished) return;

            if (GetMovementInputDirection() != Vector3.zero)
            {
                OnMove();
            }
        }
        
        public override void Update()
        {
            base.Update();
            
            if (!IsAttackFrame) return;
            
            // _attackIntervalTimer.Tick(Time.deltaTime);
            //
            // if (_attackIntervalTimer.IsFinished)
            // {
            //     DetectAndHandleAttackHits();
            //     
            //     _attackIntervalTimer.Reset();
            //     _attackIntervalTimer.Start();
            // }
        }

        // ** Animation Event **
        public void GetCloseToTarget()
        {
            GameObject targetEnemy = StateMachine.Player.TargetDetector.GetCurrentTarget();

            if (targetEnemy == null) return;

            FaceThis(targetEnemy.transform.position);
            Vector3 finalPos = TargetOffset(targetEnemy.transform.position, 1f);
            finalPos.y = 0;
            StateMachine.Player.Rigidbody.MovePosition(finalPos);
        }
        
        // ** Animation Event **
        public void FaceTarget()
        {
            GameObject targetEnemy = StateMachine.Player.TargetDetector.GetCurrentTarget();

            if (targetEnemy == null) return;

            FaceThis(targetEnemy.transform.position);
        }

        private Vector3 TargetOffset(Vector3 target, float deltaDistance)
        {
            var position = target;
            return Vector3.MoveTowards(position, StateMachine.Player.transform.position, deltaDistance);
        }
        
        private void FaceThis(Vector3 target, float duration = 0.08f)
        {
            Quaternion lookAtRotation = Quaternion.LookRotation(target - StateMachine.Player.transform.position);
            lookAtRotation.x = 0;
            lookAtRotation.z = 0;
            StateMachine.Player.transform.DOLocalRotateQuaternion(lookAtRotation, duration);
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (stateInfo.normalizedTime < 0.95f) return;

            Debug.Log($"{stateInfo.normalizedTime} : {stateInfo.shortNameHash}");

            if (!IsFinished && IsCombo && !StateMachine.ReusableData.IsOnCooldown)
            {
                FinishAttack();
                return;
            }

            if (GetMovementInputDirection() == Vector3.zero)
            {
                ResetToIdle();
            }
        }
        
        private void Float()
        {
            var capsuleColliderCenterInWorldSpace = StateMachine.Player.ResizableCapsuleCollider.CapsuleColliderData.Collider.bounds.center;

            var downwardsRayFromCapsuleCenter = new Ray(capsuleColliderCenterInWorldSpace, Vector3.down);

            if (Physics.Raycast(downwardsRayFromCapsuleCenter, out RaycastHit hit, StateMachine.Player.ResizableCapsuleCollider.SlopeData.FloatRayDistance, StateMachine.Player.LayerData.GroundLayer, QueryTriggerInteraction.Ignore))
            {
                var groundAngle = Vector3.Angle(hit.normal, -downwardsRayFromCapsuleCenter.direction);

                var distanceToFloatingPoint = StateMachine.Player.ResizableCapsuleCollider.CapsuleColliderData.ColliderCenterInLocalSpace.y * StateMachine.Player.transform.localScale.y - hit.distance;

                if (distanceToFloatingPoint == 0f)
                {
                    return;
                }

                var amountToLift = distanceToFloatingPoint * StateMachine.Player.ResizableCapsuleCollider.SlopeData.StepReachForce - GetPlayerVerticalVelocity().y;

                var liftForce = new Vector3(0f, amountToLift, 0f);

                StateMachine.Player.Rigidbody.AddForce(liftForce, ForceMode.VelocityChange);
            }
        }
        
        protected bool CheckGroundStatus()
        {
            var startPoint = StateMachine.Player.transform.position + StateMachine.Player.transform.forward * 1f;
            
            var ray = new Ray(startPoint, Vector3.down);
            return Physics.Raycast(ray, out var _, StateMachine.Player.Data.GroundedData.GroundToFallRayDistance, StateMachine.Player.LayerData.GroundLayer);
        }

        private void DetectAndHandleAttackHits()
        {
            // Get the active ComboConfig
            var comboConfig = GetActiveComboConfig();
            
            // Fall back to default if ComboConfig is null
            if (comboConfig == null)
            {
                DefaultSphereDetection();
                return;
            }

            // Get current combo index and verify AttackConfigs exists
            var comboIndex = StateMachine.ReusableData.CurrentComboIndex;
            if (comboConfig.AttackConfigs == null)
            {
                DefaultSphereDetection();
                return;
            }

            // Verify comboIndex is within range
            if (comboIndex < 0 || comboIndex >= comboConfig.AttackConfigs.Length)
            {
                DefaultSphereDetection();
                return;
            }

            // Get the current attack configuration
            var attackConfig = comboConfig.AttackConfigs[comboIndex];

            // Fall back to default if config is null
            if (!attackConfig)
            {
                DefaultSphereDetection();
                return;
            }

            // Create detection context with only the necessary data
            var detectionContext = HitDetectionContext.CreateFromPlayer(StateMachine.Player, attackConfig);

            // Handle additional VFX that should spawn regardless of hits
            if (attackConfig.EnableAdditionalVfx && attackConfig.AdditionalVfxSettings && attackConfig.SpawnEvenWithoutHit)
            {
                SpawnAdditionalVFX(attackConfig, StateMachine.Player.transform.position, StateMachine.Player.transform.forward);
            }

            var hitColliders = HitDetectionManager.Instance.DetectHits(detectionContext, attackConfig);

            // If we have hits and additional VFX is enabled
            if (hitColliders.Length > 0 && attackConfig.EnableAdditionalVfx && attackConfig.AdditionalVfxSettings)
            {
                if (!attackConfig.SpawnEvenWithoutHit) // Only spawn if we haven't already spawned due to SpawnEvenWithoutHit
                {
                    if (attackConfig.SpawnPerCollision)
                    {
                        // Spawn for each collision
                        foreach (var collider in hitColliders)
                        {
                            Vector3 hitPosition = collider.bounds.center - StateMachine.Player.transform.forward * collider.bounds.extents.z 
                                                + Vector3.up * Random.Range(-0.1f, 0.1f);
                            SpawnAdditionalVFX(attackConfig, hitPosition, StateMachine.Player.transform.forward);
                        }
                    }
                    else
                    {
                        // Spawn once using the first hit position
                        Vector3 hitPosition = hitColliders[0].bounds.center - StateMachine.Player.transform.forward * hitColliders[0].bounds.extents.z 
                                            + Vector3.up * Random.Range(-0.1f, 0.1f);
                        SpawnAdditionalVFX(attackConfig, hitPosition, StateMachine.Player.transform.forward);
                    }
                }
            }

            // Process each hit collision
            foreach (var collider in hitColliders)
            {
                DebugUtilities.DrawWireSphere(
                    collider.bounds.center,
                    0.33f,
                    Color.yellow,
                    1f
                );

                // Get hit position
                Vector3 hitPosition = collider.bounds.center - StateMachine.Player.transform.forward * collider.bounds.extents.z 
                                    + Vector3.up * Random.Range(-0.1f, 0.1f) - Vector3.up * 0.2f;
                // Calculate hit direction
                var hitDirection = StateMachine.Player.transform.forward;
                
                // Use config VFX if available, otherwise use default
                VFXFlyweightHelper.SpawnVfxAtPosition(
                    attackConfig.HitVfxSettings ? attackConfig.HitVfxSettings : StateMachine.Player.Data.DefaultHitVfxSettings, 
                    hitPosition, 
                    Quaternion.LookRotation(hitDirection), 
                    attackConfig.GetHitVfxScale()
                );

                if (collider.gameObject.GetInterface<IHittable>() is not { } enemy || !HitEnemies.Add(enemy)) continue;
                
                // Process critical hit chance
                bool isCriticalHit = attackConfig.EnableCriticalHit && Random.Range(0f, 100f) <= attackConfig.CriticalHitChance;
                float finalDamage = isCriticalHit ? attackConfig.Damage * attackConfig.CriticalHitDamageMultiplier : attackConfig.Damage;
                
                // Create a hit context if available
                if (typeof(HitContext).IsClass)
                {
                    try
                    {
                        // Create a hit context with all necessary data
                        var hitContext = HitContext.FromConfiguration(
                            attackConfig, 
                            hitDirection, 
                            hitPosition, 
                            finalDamage, 
                            StateMachine.Player.gameObject
                        ).WithStun(attackConfig.EnableStun, attackConfig.StunThresholdDamage, attackConfig.StunDuration)
                         .WithPushBack(attackConfig.EnablePushBack, attackConfig.PushBackDistance, attackConfig.PushBackDuration, attackConfig.PushBackCurve, attackConfig.DeathPushBackMultiplier);
                        
                        // If it's a critical hit, mark it in the context
                        if (isCriticalHit)
                        {
                            hitContext.AsCritical();
                        }
                        
                        // Apply the hit using the context
                        if (enemy != null)
                        {
                            // Use the new TriggerOnHit method if available
                            try
                            {
                                // This method handles all combat event dispatching in one place
                                hitContext.TriggerOnHit(collider.gameObject);
                            }
                            catch (System.Exception ex)
                            {
                                Debug.LogWarning($"Error using TriggerOnHit: {ex.Message}. Falling back to direct method.");
                                
                                // Fallback to legacy methods
                                var methodInfo = enemy.GetType().GetMethod("OnHitWithContext");
                                if (methodInfo != null)
                                {
                                    // Call the context method if available
                                    methodInfo.Invoke(enemy, new object[] { hitContext });
                                }
                                else
                                {
                                    // Fallback to regular hit method
                                    enemy.OnHit(hitDirection, finalDamage);
                                }
                            }
                        }
                    }
                    catch (System.Exception)
                    {
                        // If anything goes wrong with the context approach, fall back to the direct method
                        enemy.OnHit(hitDirection, finalDamage);
                    }
                }
                else
                {
                    // If HitContext doesn't exist, use the regular method
                    enemy.OnHit(hitDirection, finalDamage);
                }
                
                // Show critical hit VFX if applicable
                if (isCriticalHit && attackConfig.CriticalHitVfxSettings)
                {
                    VFXFlyweightHelper.SpawnVfxAtPosition(attackConfig.CriticalHitVfxSettings, hitPosition, Quaternion.LookRotation(hitDirection), attackConfig.CriticalVfxScale);
                }
                
                // Apply hit stop with new configuration
                if (attackConfig.EnableHitStop)
                {
                    ApplyHitStop(attackConfig.HitStopDuration, attackConfig.HitStopTimeScale, attackConfig.HitStopAffects, enemy);
                }
                
                // Apply camera shake if enabled
                if (attackConfig.EnableCameraShake)
                {
                    CameraShakeManager.Instance.Shake(
                        attackConfig.CameraShakeIntensity, 
                        attackConfig.CameraShakeIntensity, 
                        attackConfig.CameraShakeDuration
                    );
                }
            }
        }

        private void SpawnAdditionalVFX(AttackHitConfiguration config, Vector3 hitPosition, Vector3 hitDirection)
        {
            // Determine spawn position
            Vector3 additionalVfxPosition;
            if (config.SpawnAtPlayerLocation)
            {
                additionalVfxPosition = StateMachine.Player.transform.position;
            }
            else
            {
                additionalVfxPosition = hitPosition;
            }
            
            // Apply additional offset
            additionalVfxPosition += config.AdditionalVfxOffset;

            // Calculate rotation
            Quaternion vfxRotation = Quaternion.identity;
            if (config.FaceHitDirection)
            {
                vfxRotation = Quaternion.LookRotation(hitDirection);
            }
            else if (config.InheritPlayerRotation)
            {
                vfxRotation = StateMachine.Player.transform.rotation;
            }
            
            // Apply additional rotation offset
            vfxRotation *= Quaternion.Euler(config.AdditionalVfxRotation);
            
            // Handle delay if specified
            if (config.AdditionalVfxDelay > 0)
            {
                StateMachine.Player.StartStateCoroutine(
                    VFXFlyweightHelper.SpawnDelayedVfx(
                        config.AdditionalVfxSettings, 
                        additionalVfxPosition,
                        config.AdditionalVfxScale,
                        config.AdditionalVfxDelay,
                        vfxRotation
                    )
                );
            }
            else
            {
                VFXFlyweightHelper.SpawnVfxAtPosition(
                    config.AdditionalVfxSettings,
                    additionalVfxPosition,
                    vfxRotation,
                    config.AdditionalVfxScale
                );
            }
        }

        // Helper method to apply hit stop based on configuration
        private void ApplyHitStop(float duration, float timeScale, AttackHitConfiguration.HitStopTarget target, IHittable hitEnemy = null)
        {
            switch (target)
            {
                case AttackHitConfiguration.HitStopTarget.Player:
                    HitStopManager.Instance.HitStopPlayer(duration, timeScale);
                    break;
                case AttackHitConfiguration.HitStopTarget.Enemy:
                    if (hitEnemy != null && hitEnemy is MonoBehaviour enemyMono)
                    {
                        HitStopManager.Instance.HitStopTarget(enemyMono.gameObject, duration, timeScale);
                    }
                    break;
                case AttackHitConfiguration.HitStopTarget.Both:
                default:
                    HitStopManager.Instance.HitStop(duration, timeScale);
                    break;
            }
        }

        // The original sphere detection as fallback
        private void DefaultSphereDetection()
        {
            var sphereCenter = StateMachine.Player.transform.position + StateMachine.Player.transform.forward * 1f;
            sphereCenter.y = StateMachine.Player.ResizableCapsuleCollider.CapsuleColliderData.Collider.bounds.center.y;

            var sphereRadius = 0.5f;

            var hitColliders = Physics.OverlapSphere(sphereCenter, sphereRadius, StateMachine.Player.LayerData.EnemyLayer);
        
            DebugUtilities.DrawWireSphere(
                sphereCenter,
                sphereRadius,
                Color.red,
                1f
            );
        
            foreach (var collider in hitColliders)
            {
                DebugUtilities.DrawWireSphere(
                    collider.bounds.center,
                    0.33f,
                    Color.yellow,
                    1f
                );
            
                var hitPosition = collider.bounds.center - StateMachine.Player.transform.forward * collider.bounds.extents.z + Vector3.up * Random.Range(-0.1f, 0.1f);
                
                VFXFlyweightHelper.SpawnVfxAtPosition(
                    StateMachine.Player.Data.DefaultHitVfxSettings,
                    hitPosition,
                    Quaternion.identity,
                    Vector3.one
                );

                if (collider.gameObject.GetInterface<IHittable>() is not { } enemy || !HitEnemies.Add(enemy)) continue;
            
                var hitDirection = StateMachine.Player.transform.forward;
                
                // Create a simple hit context
                var hitContext = HitContext.Create(
                    hitDirection, 
                    hitPosition, 
                    1.0f, // Default small damage
                    StateMachine.Player.gameObject
                )
                .WithPushBack(true, 0.5f, 0.2f)
                .WithStun(false, 5f, 0.1f);
                
                // Use the new event system instead of directly calling OnHit
                try
                {
                    hitContext.TriggerOnHit(collider.gameObject);
                }
                catch (System.Exception)
                {
                    // Fallback to direct method if there's an issue
                    enemy.OnHit(hitDirection, 0f);
                }
                
                // Use default hit stop values
                ApplyHitStop(0.1f, 0.1f, AttackHitConfiguration.HitStopTarget.Both);
                CameraShakeManager.Instance.Shake(0.1f, 0.1f, 0.1f);
            }
        }
        
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // Handle case animation event is not called while transitioning to this state
            StartComboFrame();
        }

        // ** Animation Event ** //
        public virtual void StartComboFrame()
        {
            StateMachine.ReusableData.IsComboFrame = true;
        }

        // ** Animation Event ** //
        public virtual void EndComboFrame()
        {
            StateMachine.ReusableData.IsComboFrame = false;
        }
        
        // ** Animation Event ** //
        public void StartAttackFrame()
        {
            IsAttackFrame = true;
        } 

        // ** Animation Event ** //
        public void EndAttackFrame()
        {
            HitEnemies.Clear();

            IsAttackFrame = false;
            
            DetectAndHandleAttackHits();
        }
        
        // ** Animation Event ** //
        public virtual void FinishAttack()
        {
            if (IsFinished) return;
            
            IsFinished = true;
            
            // Handle any area effects at the end of the attack
            HandleAreaEffect();
            
            // Get the active ComboConfiguration
            ComboConfiguration comboConfig = GetActiveComboConfig();
            
            // Clear weapons if configured to do so
            if (comboConfig != null && StateMachine.ReusableData.CurrentComboIndex < comboConfig.AttackConfigs.Length)
            {
                var config = comboConfig.AttackConfigs[StateMachine.ReusableData.CurrentComboIndex];
                if (config != null && config.RemoveWeaponAfterAttack)
                {
                    StateMachine.Player.SocketManager.ClearAllWeapons();
                }
            }
            
            Debug.Log("Finish Attack");
            
            StateMachine.ReusableData.ResetComboTimer.Reset();
            StateMachine.ReusableData.ResetComboTimer.Start();
        }
        
        // ** Animation Event ** //
        public virtual void ResetToIdle()
        {
            StateMachine.ChangeState(StateMachine.IdlingState);
        }

        private void HandleAreaEffect()
        {
            // Get current combo index and its configuration
            var comboIndex = StateMachine.ReusableData.CurrentComboIndex;
            var comboConfig = GetActiveComboConfig();
            
            if (comboConfig == null) return;
            
            var currentAttackConfig = comboConfig.AttackConfigs[comboIndex];
            
            if (!currentAttackConfig || !currentAttackConfig.EnableAreaEffect || !currentAttackConfig.AreaEffectVfxSettings) return;
            
            // Determine the position for the area effect
            Vector3 areaEffectPosition = StateMachine.Player.transform.position + 
                                         StateMachine.Player.transform.forward * currentAttackConfig.Offset.z;
            
            // Draw debug sphere to visualize area effect
            Debug.DrawLine(StateMachine.Player.transform.position, areaEffectPosition, Color.yellow, 2f);
            DebugUtilities.DrawWireSphere(areaEffectPosition, currentAttackConfig.AreaEffectRadius, Color.red, 2f);
            
            // Spawn the area effect VFX
            var vfxObj = VFXFlyweightHelper.SpawnVfxAtPosition(currentAttackConfig.AreaEffectVfxSettings, areaEffectPosition, Quaternion.identity, currentAttackConfig.GetAreaEffectVfxScale());
            vfxObj.transform.localScale *= currentAttackConfig.AreaEffectRadius;

            // Apply area damage to enemies within radius
            var colliders = Physics.OverlapSphere(areaEffectPosition, currentAttackConfig.AreaEffectRadius, StateMachine.Player.LayerData.EnemyLayer);
            
            foreach (var collider in colliders)
            {
                if (collider.gameObject.GetInterface<IHittable>() is { } enemy)
                {
                    var hitDirection = StateMachine.Player.transform.forward;
                    enemy.OnHit(hitDirection, currentAttackConfig.AreaEffectDamage);
                }
            }
            
            // Apply area hit stop if enabled
            if (currentAttackConfig.EnableAreaHitStop)
            {
                ApplyHitStop(currentAttackConfig.AreaHitStopDuration, currentAttackConfig.AreaHitStopTimeScale, currentAttackConfig.AreaHitStopAffects);
            }
            
            // Apply area camera shake if enabled
            if (currentAttackConfig.EnableAreaCameraShake)
            {
                CameraShakeManager.Instance.Shake(
                    currentAttackConfig.AreaCameraShakeIntensity,
                    currentAttackConfig.AreaCameraShakeIntensity,
                    currentAttackConfig.AreaCameraShakeDuration
                );
            }
        }

        // ** Animation Event **
        public void MoveForward(float distance = 1f, float duration = 0.2f)
        {
            if (distance <= 0f || duration <= 0f) return;
            
            Vector3 startPosition = StateMachine.Player.transform.position;
            Vector3 targetPosition = startPosition + StateMachine.Player.transform.forward * distance;
            
            // Make sure we don't move into the ground
            targetPosition.y = startPosition.y;
            
            // Start a coroutine to move over time
            StateMachine.Player.StartStateCoroutine(MoveForwardCoroutine(startPosition, targetPosition, duration));
            
            Debug.Log($"MoveForward animation event: moving {distance} units over {duration} seconds");
        }

        private IEnumerator MoveForwardCoroutine(Vector3 start, Vector3 target, float duration)
        {
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                
                // You can use different easing functions for smoother movement
                // This is linear movement
                Vector3 newPosition = Vector3.Lerp(start, target, t);
                
                // Use the rigidbody for movement to respect physics
                StateMachine.Player.Rigidbody.MovePosition(newPosition);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Ensure we end exactly at the target position
            StateMachine.Player.Rigidbody.MovePosition(target);
        }

        // ** Animation Event **
        public void MoveTowardsTarget(float maxDistance = 1.5f, float duration = 0.2f)
        {
            if (maxDistance <= 0f || duration <= 0f) return;
            
            GameObject targetEnemy = StateMachine.Player.TargetDetector.GetCurrentTarget();
            Vector3 startPosition = StateMachine.Player.transform.position;
            Vector3 targetPosition;
            
            if (targetEnemy != null)
            {
                // Face the target enemy
                FaceThis(targetEnemy.transform.position);
                
                // Calculate direction to target
                Vector3 directionToTarget = targetEnemy.transform.position - startPosition;
                directionToTarget.y = 0; // Keep movement on the horizontal plane
                
                float distanceToTarget = directionToTarget.magnitude;
                
                // Get the offset position, which is closer to the player by a small margin
                float actualDistance = Mathf.Min(distanceToTarget - 1f, maxDistance);
                
                // If we're already close enough, just face the target but don't move
                if (actualDistance <= 0.1f)
                {
                    Debug.Log("Already close to target, just facing target");
                    return;
                }
                
                // Calculate the new position, moving towards target up to maxDistance
                targetPosition = startPosition + directionToTarget.normalized * actualDistance;
            }
            else
            {
                // No target, just move forward
                targetPosition = startPosition + StateMachine.Player.transform.forward * maxDistance;
            }
            
            // Make sure we don't move into the ground
            targetPosition.y = startPosition.y;
            
            // Start a coroutine to move over time
            StateMachine.Player.StartStateCoroutine(MoveTowardsTargetCoroutine(startPosition, targetPosition, duration));
            
            if (targetEnemy != null)
            {
                Debug.Log($"Moving towards target enemy, distance: {Vector3.Distance(startPosition, targetPosition):F2} over {duration} seconds");
            }
            else
            {
                Debug.Log($"No target enemy found, moving forward {maxDistance} units over {duration} seconds");
            }
        }

        private IEnumerator MoveTowardsTargetCoroutine(Vector3 start, Vector3 target, float duration)
        {
            float elapsed = 0f;
            
            // Use a smoother motion curve than linear
            AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                
                // Use the easing curve for smoother motion
                float curvedT = movementCurve.Evaluate(t);
                
                Vector3 newPosition = Vector3.Lerp(start, target, curvedT);
                
                // Use the rigidbody for movement to respect physics
                StateMachine.Player.Rigidbody.MovePosition(newPosition);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Ensure we end exactly at the target position
            StateMachine.Player.Rigidbody.MovePosition(target);
        }

    }
}