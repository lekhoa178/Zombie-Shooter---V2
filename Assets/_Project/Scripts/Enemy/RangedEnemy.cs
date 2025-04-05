using System;
using System.Collections;
using UnityEngine;
using Flyweight;
using MoreMountains.Feedbacks;
using Random = UnityEngine.Random;

public class RangedEnemy : BaseEnemy, IAttacker
{
    [Header("Ranged Attack Settings")]
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private MMF_Player telegraphFeedback;
    
    [Header("Debug Options")]
    [SerializeField] private bool showDebugTrajectories = false;
    [SerializeField] private float debugLineDuration = 2f;
    
    private float lastAttackTime;
    private IPerception perception;
    private bool isAttackingOrTelegraphing = false;
    private bool isInAttackSequence = false;
    private Transform currentTarget;
    private RangedEnemyData rangedData => enemyData as RangedEnemyData;
    private Coroutine currentFiringCoroutine;
    
    // IAttacker implementation
    public bool CanAttack => Time.time > lastAttackTime + GetEffectiveAttackCooldown();
    public float AttackRange => isElite ? enemyData.GetEffectiveAttackRange(true) : rangedData.attackRange;
    public float AttackCooldown => GetEffectiveAttackCooldown();
    
    public event Action<IAttacker> OnAttackStart;
    public event Action<IAttacker> OnAttackEnd;
    public event Action<IAttacker> OnAttackTelegraphStart;
    public event Action<IAttacker> OnAttackTelegraphEnd;
    
    // Helper methods to get elite-adjusted values
    protected float GetEffectiveAttackCooldown()
    {
        return isElite ? enemyData.GetEffectiveAttackCooldown(true) : rangedData.attackCooldown;
    }

    protected float GetEffectiveAttackDamage()
    {
        return isElite ? enemyData.GetEffectiveAttackDamage(true) : rangedData.attackDamage;
    }

    protected float GetEffectiveMoveSpeed()
    {
        return isElite ? enemyData.GetEffectiveMoveSpeed(true) : rangedData.moveSpeed;
    }
    
    protected override void Awake()
    {
        base.Awake();
        
        // Validate that we have the right type of data
        if (!(enemyData is RangedEnemyData))
        {
            Debug.LogError($"RangedEnemy requires RangedEnemyData, but was provided with {enemyData.GetType().Name}");
        }
        
        perception = GetComponent<IPerception>();
        
        if (projectileSpawnPoint == null)
        {
            projectileSpawnPoint = transform;
        }
    }

    private void OnEnable()
    {
        // Set debug visualization state
        ProjectileFlyweightHelper.SetDebugVisualization(showDebugTrajectories, debugLineDuration);
        
        // Register for combat events
        CombatEventManager.OnHit += HandleHit;
        CombatEventManager.OnDeath += HandleDeath;
    }

    private void OnValidate()
    {
        // Update debug visualization when values change in inspector
        if (Application.isPlaying)
        {
            ProjectileFlyweightHelper.SetDebugVisualization(showDebugTrajectories, debugLineDuration);
        }
    }
    
    public void PerformAttack(Transform target)
    {
        if (!CanAttack || target == null) return;
        
        currentTarget = target;
        StartCoroutine(PerformTelegraphAttack(target));
    }
    
    private IEnumerator PerformTelegraphAttack(Transform target)
    {
        if (isAttackingOrTelegraphing) yield break;
        
        isAttackingOrTelegraphing = true;

        if (telegraphFeedback != null)
        {
            telegraphFeedback.PlayFeedbacks();
        }
        
        if (rangedData.telegraphVfxSettings != null)
        {
            var telegraph = VFXFlyweightHelper.SpawnVfxAtPosition(
                rangedData.telegraphVfxSettings,
                transform.position + Vector3.up,
                Quaternion.identity
            );
            
            telegraph.transform.SetParent(transform, true);
        }
        
        OnAttackTelegraphStart?.Invoke(this);
        
        // Wait for telegraph duration
        yield return new WaitForSeconds(rangedData.telegraphDuration);
        
        // Stop telegraph feedback
        if (telegraphFeedback != null)
        {
            telegraphFeedback.StopFeedbacks();
        }
        
        OnAttackTelegraphEnd?.Invoke(this);
        
        isAttackingOrTelegraphing = false;
        
        ExecuteAttack(target);
    }
    
    private void ExecuteAttack(Transform target)
    {
        // Face target
        Vector3 direction = (target.position - transform.position).normalized;
        transform.forward = direction;
        
        // Trigger attack animation
        if (Animator)
        {
            Animator.SetTrigger("Attack");
        }
        else
        {
            // If no animator, immediately fire projectile and call attack end
            FireProjectilePattern();
            OnAttackFinished();
        }
        
        // Set attack cooldown
        lastAttackTime = Time.time;
        
        // Raise event
        OnAttackStart?.Invoke(this);
    }
    
    // Coroutine for firing consecutive salvos with delays
    private IEnumerator FireConsecutiveProjectiles(int count, int simultaneousCount, bool useRandomSpread, float spreadAngle)
    {
        isInAttackSequence = true;
        
        try
        {
            for (int i = 0; i < count; i++)
            {
                if (currentTarget == null) break;
                
                // Skip the rest of the sequence if attack was interrupted and is interruptible
                if (rangedData.attackInterruptible && !isInAttackSequence) break;
                
                // Set the Attack trigger for each consecutive batch
                if (Animator)
                {
                    Animator.SetTrigger("Attack");
                }
                
                FireSimultaneousProjectiles(
                    simultaneousCount,
                    useRandomSpread,
                    spreadAngle
                );
                
                // Wait before firing next salvo
                if (i < count - 1)
                {
                    float timeElapsed = 0f;
                    float totalDelay = rangedData.consecutiveDelay;
                    
                    // Reset the trigger to ensure proper animation triggering for the next batch
                    if (Animator)
                    {
                        Animator.ResetTrigger("Attack");
                    }
                    
                    // Use a while loop instead of yield return new WaitForSeconds
                    // This allows us to check for interruption during the delay
                    while (timeElapsed < totalDelay)
                    {
                        if (rangedData.attackInterruptible && !isInAttackSequence) break; // Allow early exit if interrupted
                        
                        timeElapsed += Time.deltaTime;
                        yield return null;
                    }
                    
                    // If we broke out of the loop due to interruption, exit the entire coroutine
                    if (rangedData.attackInterruptible && !isInAttackSequence) break;
                }
            }
        }
        finally
        {
            // Ensure we always clean up the attack state even if something went wrong
            EndAttack();
        }
    }
    
    // Called from animation event
    public void OnAttackHit()
    {
        // Only fire directly if we're not using the consecutive coroutine
        // (for consecutive firing, the coroutine handles it)
        if (currentFiringCoroutine == null)
        {
            FireProjectilePattern();
        }
    }
    
    // Main firing method that uses data to determine pattern
    public void FireProjectilePattern(int? overrideConsecutiveCount = null, 
                                      int? overrideSimultaneousCount = null,
                                      bool? overrideUseRandomSpread = null,
                                      float? overrideSpreadAngle = null)
    {
        if (currentTarget == null) return;
        
        // Set parameters using overrides or defaults from data
        int consecutiveCount = overrideConsecutiveCount ?? rangedData.consecutiveCount;
        int simultaneousCount = overrideSimultaneousCount ?? rangedData.simultaneousCount;
        bool useRandomSpread = overrideUseRandomSpread ?? rangedData.useRandomSpread;
        float spreadAngle = overrideSpreadAngle ?? rangedData.spreadAngle;
        
        // Cancel any ongoing firing routine
        if (currentFiringCoroutine != null)
        {
            StopCoroutine(currentFiringCoroutine);
            currentFiringCoroutine = null;
            isInAttackSequence = false;
        }
        
        // If firing multiple consecutive shots, use a coroutine
        if (consecutiveCount > 1)
        {
            currentFiringCoroutine = StartCoroutine(FireConsecutiveProjectiles(
                consecutiveCount, 
                simultaneousCount,
                useRandomSpread,
                spreadAngle
            ));
        }
        else
        {
            // Otherwise just fire simultaneously
            isInAttackSequence = true;
            
            FireSimultaneousProjectiles(
                simultaneousCount,
                useRandomSpread,
                spreadAngle
            );
            
            // For single batch, end the attack here
            // Don't call this immediately to give time for the animation to play
            StartCoroutine(EndAttackAfterDelay(0.5f));
        }
    }
    
    // Helper method to end attack after a short delay
    private IEnumerator EndAttackAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        EndAttack();
    }
    
    // Calculate target position with leading and bias
    private Vector3 GetTargetPosition()
    {
        Vector3 targetPosition = currentTarget.position;
        
        // Apply leading if enabled
        if (rangedData.leadTarget)
        {
            Rigidbody targetRb = currentTarget.GetComponent<Rigidbody>();
            if (targetRb != null)
            {
                targetPosition += targetRb.linearVelocity * rangedData.leadFactor;
            }
        }
        
        // Note: directionVariance is now applied at the individual projectile level
        // in ProjectileFlyweightHelper, not here at the target position level
        
        return targetPosition;
    }
    
    // Calculate random spread offset
    private Vector3 GetRandomSpreadOffset(float spreadAngle)
    {
        float randomAngleH = UnityEngine.Random.Range(-spreadAngle, spreadAngle) * 0.5f;
        float randomAngleV = UnityEngine.Random.Range(-spreadAngle, spreadAngle) * 0.5f;
        
        Quaternion randomRot = Quaternion.Euler(randomAngleV, randomAngleH, 0);
        return randomRot * Vector3.forward * rangedData.randomSpreadFactor;
    }
    
    // Calculate arc spread offset
    private Vector3 GetArcSpreadOffset(float angle)
    {
        // Create a rotation around the up axis
        Quaternion rotation = Quaternion.AngleAxis(angle, projectileSpawnPoint.up);
        
        // Apply rotation to forward direction
        return rotation * Vector3.forward;
    }
    
    // Fire multiple projectiles in a pattern
    private void FireSimultaneousProjectiles(int count, bool useRandomSpread, float spreadAngle)
    {
        if (currentTarget == null) return;
        
        // Calculate target position with leading if enabled
        Vector3 targetPosition = GetTargetPosition();
        
        // Draw a direct line to target for debugging
        if (showDebugTrajectories)
        {
            Debug.DrawLine(projectileSpawnPoint.position, targetPosition, Color.yellow, debugLineDuration);
        }
        
        // Let the ProjectileSettings control parabolic behavior and height
        if (rangedData.projectileSettings != null)
        {
            // Play elite attack sound if applicable
            if (isElite && enemyData.eliteAttackSounds != null && enemyData.eliteAttackSounds.Length > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, enemyData.eliteAttackSounds.Length);
                AudioSource.PlayClipAtPoint(enemyData.eliteAttackSounds[randomIndex], transform.position);
            }
            
            // Use elite VFX for projectiles if applicable
            if (isElite && count > 0)
            {
                // Spawn an elite attack indicator VFX
                if (enemyData.elitePersistentVfxSettings != null)
                {
                    VFXFlyweightHelper.SpawnVfxAtPosition(
                        enemyData.elitePersistentVfxSettings,
                        projectileSpawnPoint.position,
                        Quaternion.identity,
                        enemyData.GetElitePersistentVfxScale()
                    );
                }
            }
            
            // Get effective damage value considering elite status
            float effectiveDamage = GetEffectiveAttackDamage();
            
            // Spawn the projectile pattern with proper scaling for damage
            ProjectileFlyweightHelper.SpawnProjectilePattern(
                rangedData.projectileSettings,
                projectileSpawnPoint.position,
                targetPosition,
                effectiveDamage, // Use scaled damage
                gameObject,
                count,
                spreadAngle,
                useRandomSpread,
                rangedData.randomSpreadFactor,
                rangedData.directionVariance
            );
        }
    }
    
    // Called from animation event - no longer needed as we now handle this in code
    public void OnAttackFinished()
    {
        // This is kept for backward compatibility, but we're now handling attack end in code
        // We'll let the coroutines call AttackEnd() directly
    }

    public override void OnHitWithContext(HitContext hitContext)
    {
        base.OnHitWithContext(hitContext);
        
                if (rangedData.attackInterruptible)
                {
                    isInAttackSequence = false;
                }
    }

    public void EndAttack()
    {
        isAttackingOrTelegraphing = false;
        isInAttackSequence = false;
        currentTarget = null;
        
        // Cancel any ongoing firing routine
        if (currentFiringCoroutine != null)
        {
            StopCoroutine(currentFiringCoroutine);
            currentFiringCoroutine = null;
        }
        
        OnAttackEnd?.Invoke(this);
    }
    
    public bool IsTargetInAttackRange(Transform target)
    {
        if (target == null) return false;
        
        float distance = Vector3.Distance(transform.position, target.position);
        return distance <= rangedData.attackRange && distance > rangedData.minAttackDistance;
    }
    
    // Public getters for use with RangedAttackAction
    public Transform GetProjectileSpawnPoint() => projectileSpawnPoint;
    public float GetEliteMultiplier(float baseValue, float multiplier) => isElite ? baseValue * multiplier : baseValue;
    
    // Cancel any ongoing firing routines on disable
    private void OnDisable()
    {
        // Unregister from combat events
        CombatEventManager.OnHit -= HandleHit;
        CombatEventManager.OnDeath -= HandleDeath;
        
        // Cancel any ongoing firing routines
        if (currentFiringCoroutine != null)
        {
            StopCoroutine(currentFiringCoroutine);
            currentFiringCoroutine = null;
        }
    }
}