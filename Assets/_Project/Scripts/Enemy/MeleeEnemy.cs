using System;
using System.Collections;
using LocomotionSystem;
using UnityEngine;
using Flyweight;
using MoreMountains.Feedbacks;

public class MeleeEnemy : BaseEnemy, IAttacker
{
    private float lastAttackTime;
    private IPerception perception;
    private bool isAttackingOrTelegraphing = false;
    
    [Header("Telegraph Feedback")]
    [Tooltip("Optional feedback player for telegraph effects")]
    [SerializeField] private MMF_Player telegraphFeedback;
    
    // IAttacker implementation
    public bool CanAttack => (Time.time > lastAttackTime + GetEffectiveAttackCooldown()) && !isDead;
    public float AttackRange => isElite ? enemyData.GetEffectiveAttackRange(true) : enemyData.attackRange;
    public float AttackCooldown => GetEffectiveAttackCooldown();
    
    public event Action<IAttacker> OnAttackStart;
    public event Action<IAttacker> OnAttackEnd;
    public event Action<IAttacker> OnAttackTelegraphStart;
    public event Action<IAttacker> OnAttackTelegraphEnd;
    
    protected override void Awake()
    {
        base.Awake();
        perception = GetComponent<IPerception>();
    }
    
    public void PerformAttack(Transform target)
    {
        if (!CanAttack || target == null) return;
        
        StartCoroutine(PerformTelegraphAttack(target));

        // ExecuteAttack(target);
    }
    
    private IEnumerator PerformTelegraphAttack(Transform target)
    {
        if (isAttackingOrTelegraphing) yield break;
        
        isAttackingOrTelegraphing = true;

        if (telegraphFeedback)
        {
            telegraphFeedback.PlayFeedbacks();
        }
        
        if (enemyData.telegraphVfxSettings)
        {
            var telegraph = VFXFlyweightHelper.SpawnVfxAtPosition(
                enemyData.telegraphVfxSettings,
                transform.position + Vector3.up,
                Quaternion.identity
            );
            
            telegraph.transform.SetParent(transform, true);
        }
        
        OnAttackTelegraphStart?.Invoke(this);
        
        // Wait for telegraph duration
        yield return new WaitForSeconds(enemyData.telegraphDuration);
        
        OnAttackTelegraphEnd?.Invoke(this);
        
        isAttackingOrTelegraphing = false;
        
        ExecuteAttack(target);
    }
    
    private void ExecuteAttack(Transform target)
    {
        Debug.Log($"Attacking {target.name}");
        
        // Face target
        var direction = (target.position - transform.position).normalized;
        transform.forward = direction;
        
        // Trigger attack animation
        if (Animator && !isDead)
        {
            Animator.SetTrigger("Attack");
        }
        else
        {
            // If no animator, immediately call attack end (important!)
            OnAttackFinished();
        }
        
        // Set attack cooldown
        lastAttackTime = Time.time;
        
        // Raise event
        OnAttackStart?.Invoke(this);
    }
    
    // Helper methods to get elite-adjusted values
    protected float GetEffectiveAttackCooldown()
    {
        return isElite ? enemyData.GetEffectiveAttackCooldown(true) : enemyData.attackCooldown;
    }

    protected float GetEffectiveAttackDamage()
    {
        return isElite ? enemyData.GetEffectiveAttackDamage(true) : enemyData.attackDamage;
    }

    protected float GetEffectiveMoveSpeed()
    {
        return isElite ? enemyData.GetEffectiveMoveSpeed(true) : enemyData.moveSpeed;
    }

    // Called from animation event
    public void OnAttackHit()
    {
        if (perception == null) return;
        
        if (perception.CurrentTarget == null) return;

        if (!IsTargetInAttackRange(perception.CurrentTarget)) return;
        
        var player = perception.CurrentTarget.GetComponent<Player>();
        if (player == null) return;
        
        var hitDirection = (player.transform.position - transform.position).normalized;
        var hitPosition = player.transform.position;
                
        // Create hit context with all the necessary attack data
        var hitContext = HitContext.Create(
            hitDirection,
            hitPosition,
            GetEffectiveAttackDamage(), // Use effective damage that considers elite status
            gameObject
        )
        .WithPushBack(
            enemyData.attackCanPushBack,
            enemyData.attackPushbackDistance,
            enemyData.attackPushbackDuration
        )
        .WithStun(
            enemyData.attackCanStun,
            enemyData.attackStunThreshold,
            enemyData.attackStunDuration
        );
        
        hitContext.TriggerOnHit(player.gameObject);

        // Play elite attack sound if applicable
        if (isElite && enemyData.eliteAttackSounds != null && enemyData.eliteAttackSounds.Length > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, enemyData.eliteAttackSounds.Length);
            AudioSource.PlayClipAtPoint(enemyData.eliteAttackSounds[randomIndex], transform.position);
        }

        // Use appropriate VFX settings based on elite status
        if (isElite && enemyData.elitePersistentVfxSettings != null && hitPosition != Vector3.zero)
        {
            // Spawn an additional hit effect for elite enemies
            VFXFlyweightHelper.SpawnVfxAtPosition(
                enemyData.elitePersistentVfxSettings,
                hitPosition,
                Quaternion.LookRotation(hitDirection),
                enemyData.GetElitePersistentVfxScale()
            );
        }

        if (enemyData.hitVfxSettings != null)
        {
            VFXFlyweightHelper.SpawnVfxAtPosition(
                enemyData.hitVfxSettings,
                hitPosition,
                Quaternion.LookRotation(hitDirection),
                enemyData.GetHitVfxScale()
            );
        }

        if (enemyData.enableAdditionalVfx && enemyData.additionalVfxSettings != null)
        {
            StartCoroutine(SpawnAdditionalAttackVfx(hitPosition, hitDirection));
        }
    }
    
    // For enemy attacks
    private IEnumerator SpawnAdditionalAttackVfx(Vector3 hitPosition, Vector3 hitDirection)
    {
        if (enemyData.additionalVfxDelay > 0)
        {
            yield return new WaitForSeconds(enemyData.additionalVfxDelay);
        }

        // Determine spawn position
        Vector3 spawnPosition;
        if (enemyData.spawnAtHitLocation)
        {
            spawnPosition = hitPosition;
        }
        else if (enemyData.spawnAtEnemyLocation)
        {
            spawnPosition = transform.position;
        }
        else
        {
            spawnPosition = hitPosition;
        }

        // Apply offset
        spawnPosition += enemyData.additionalVfxOffset;

        var rotation = enemyData.faceHitDirection ? Quaternion.LookRotation(hitDirection) : Quaternion.Euler(enemyData.additionalVfxRotation);

        if (enemyData.additionalVfxDelay > 0)
        {
            yield return StartCoroutine(
                VFXFlyweightHelper.SpawnDelayedVfx(
                    enemyData.additionalVfxSettings,
                    spawnPosition,
                    enemyData.GetAdditionalVfxScale(),
                    enemyData.additionalVfxDelay,
                    rotation
                )
            );
        }
        else
        {
            VFXFlyweightHelper.SpawnVfxAtPosition(
                enemyData.additionalVfxSettings,
                spawnPosition,
                rotation,
                enemyData.GetAdditionalVfxScale()
            );
        }
    }
    
    // Called from animation event
    public void OnAttackFinished()
    {
        isAttackingOrTelegraphing = false;
        
        OnAttackEnd?.Invoke(this);
    }
    
    public bool IsTargetInAttackRange(Transform target)
    {
        if (target == null) return false;
        
        var distance = Vector3.Distance(transform.position, target.position);
        return distance <= enemyData.attackRange;
    }
    
}