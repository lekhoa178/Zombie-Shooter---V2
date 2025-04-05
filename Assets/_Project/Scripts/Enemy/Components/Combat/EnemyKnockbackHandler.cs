using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Handles knockback and pushback effects for enemies
/// </summary>
[DisallowMultipleComponent]
public class EnemyKnockbackHandler : MonoBehaviour, IKnockbackable
{
    // Dependencies (will be set by BaseEnemy)
    private BaseEnemy enemy;
    private EnemyData enemyData;
    private Rigidbody rb;
    private Animator animator;
    private IAIMovement aiMovement;
    
    // State tracking
    private bool isPushedBack = false;
    
    // Events - original events
    public event Action<BaseEnemy> OnEnemyPushbackStart;
    public event Action<BaseEnemy> OnEnemyPushbackEnd;
    
    // IKnockbackable interface events
    public event Action<GameObject> OnKnockbackStart;
    public event Action<GameObject> OnKnockbackEnd;
    
    // Public properties
    public bool IsPushedBack => isPushedBack;
    
    // Initialize the handler with required references
    public void Initialize(BaseEnemy enemy, EnemyData data, Rigidbody rb, Animator animator, IAIMovement movement)
    {
        this.enemy = enemy;
        this.enemyData = data;
        this.rb = rb;
        this.animator = animator;
        this.aiMovement = movement;
    }
    
    private void OnEnable()
    {
        CombatEventManager.OnKnockbackApplied += HandleKnockbackEvent;
    }

    private void OnDisable()
    {
        CombatEventManager.OnKnockbackApplied -= HandleKnockbackEvent;
    }

    /// <summary>
    /// Handle knockback events from the combat event system
    /// </summary>
    public void HandleKnockbackEvent(CombatEventManager.KnockbackEventContext context)
    {
        // Only process if this is the target
        if (context.Target != gameObject)
        {
            Debug.Log($"<color=gray>Knockback ignored - wrong target: {context.Target.name} vs {gameObject.name}</color>");
            return;
        }
        
        Debug.Log($"<color=cyan>{gameObject.name} received knockback event with distance {context.Distance}</color>");
        
        if (enemy.IsDead)
        {
            // Apply death knockback with additional force
            ApplyDeathKnockback(context.Direction, context.Distance, context.Duration, context.Curve);
        }
        else
        {
            // Apply normal knockback with resistance
            ApplyKnockback(context.Direction, context.Distance, context.Duration, context.Curve);
        }
    }
    
    /// <summary>
    /// Apply knockback to the enemy
    /// </summary>
    public bool ApplyKnockback(Vector3 direction, float distance, float duration, AnimationCurve curve = null)
    {
        // Apply resistance to knockback
        float resistedDistance = distance * (1f - enemyData.pushBackResistance);
        
        Debug.Log($"<color=cyan>{gameObject.name} normal knockback: {resistedDistance} (base: {distance} × {1f - enemyData.pushBackResistance})</color>");
        
        if (isPushedBack)
        {
            Debug.Log($"<color=gray>{gameObject.name} already being pushed back</color>");
            return false;
        }
        
        enemy.StartCoroutine(ApplyPushback(direction, resistedDistance, duration, curve));
        return true;
    }
    
    /// <summary>
    /// Apply death knockback with enhanced effect
    /// </summary>
    public bool ApplyDeathKnockback(Vector3 direction, float distance, float duration, AnimationCurve curve = null)
    {
        // Apply death knockback with multiplier
        float deathMultiplier = enemyData.applyExtraDeathPushBack ? enemyData.deathPushBackMultiplier : 1f;
        float finalDistance = distance * deathMultiplier;
        
        Debug.Log($"<color=red>{gameObject.name} death knockback: {finalDistance} (base: {distance} × {deathMultiplier})</color>");
        
        if (isPushedBack)
        {
            Debug.Log($"<color=gray>{gameObject.name} already being pushed back</color>");
            return false;
        }
        
        enemy.StartCoroutine(ApplyPushback(direction, finalDistance, duration, curve));
        return true;
    }
    
    /// <summary>
    /// Apply pushback to the enemy in a specific direction
    /// </summary>
    public IEnumerator ApplyPushback(Vector3 direction, float distance, float duration, AnimationCurve curve = null)
    {
        // Default curve if none provided
        curve ??= AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        // Ensure this can't be interrupted
        isPushedBack = true;
        enemy.SafeSetVariable("IsPushedBack", true);
        
        // Notify listeners
        OnEnemyPushbackStart?.Invoke(enemy);
        OnKnockbackStart?.Invoke(gameObject);
        
        // Stop any AI movement during pushback
        aiMovement?.StopMovement();
        
        // Handle facing hit direction if configured
        if (enemyData.rotateToFaceHitDirection && 
            (enemyData.onlyRotateWhenPushedBack || (!enemyData.onlyRotateWhenStunned && !enemyData.onlyRotateWhenPushedBack)))
        {
            // Get a normalized direction that ignores Y
            Vector3 facingDirection = new Vector3(-direction.x, 0, -direction.z).normalized;
            
            // Only rotate if we have a valid direction
            if (facingDirection != Vector3.zero)
            {
                // Immediately face away from the hit direction
                transform.forward = facingDirection;
            }
        }
        
        // Play hit reaction animation
        if (animator && !enemy.IsDead)
        {
            animator.SetTrigger("Damage");
        }
        
        // Calculate the end position
        var startPosition = transform.position;
        var normalizedDirection = new Vector3(direction.x, 0, direction.z).normalized;
        var targetPosition = startPosition + normalizedDirection * distance;
        
        // Make sure the target position is valid (on NavMesh, not inside obstacles)
        if (NavMesh.SamplePosition(targetPosition, out var hit, distance, NavMesh.AllAreas))
        {
            targetPosition = hit.position;
        }
        
        // Store the actual push distance (might be less than requested if obstacles are in the way)
        var actualDistance = Vector3.Distance(startPosition, targetPosition);
        
        // If we can't push back significantly, just skip the movement
        if (actualDistance < 0.2f)
        {
            isPushedBack = false;
            enemy.SafeSetVariable("IsPushedBack", false);
            OnEnemyPushbackEnd?.Invoke(enemy);
            OnKnockbackEnd?.Invoke(gameObject);
            yield break;
        }
        
        var stunHandler = enemy.GetComponent<EnemyStunHandler>();
        var wasPaused = stunHandler?.IsStunned ?? false;
        
        // Save original rigidbody settings
        var wasKinematic = false;
        if (rb)
        {
            wasKinematic = rb.isKinematic;
            rb.isKinematic = true;
        }
        
        // Execute the pushback movement
        float elapsedTime = 0;
        while (elapsedTime < duration)
        {
            var normalizedTime = elapsedTime / duration;
            var curveValue = curve.Evaluate(normalizedTime);
            
            // Move directly without physics
            transform.position = Vector3.Lerp(startPosition, targetPosition, curveValue);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Ensure we end at exactly the target position
        transform.position = targetPosition;
        
        // Restore original rigidbody settings
        if (rb)
        {
            rb.isKinematic = wasKinematic;
        }
        
        // Resume the behavior graph if we paused it (and we're not stunned)
        if (!wasPaused && stunHandler && !stunHandler.IsStunned && !enemy.IsDead)
        {
            enemy.ResumeBehaviorGraph();
        }
        
        // Mark as no longer being pushed back
        isPushedBack = false;
        enemy.SafeSetVariable("IsPushedBack", false);
        
        // Broadcast the knockback ended event
        CombatEventManager.TriggerKnockbackEnded(gameObject, gameObject);
        
        // Notify that pushback has ended
        OnEnemyPushbackEnd?.Invoke(enemy);
        OnKnockbackEnd?.Invoke(gameObject);
    }
    
    /// <summary>
    /// Apply a knockback based on hit context, respecting enemy resistances
    /// </summary>
    public void ApplyKnockbackFromHit(HitContext hitContext, bool isDying = false)
    {
        if (!hitContext.EnablePushBack || isPushedBack) return;
        
        if (isDying && enemyData.applyExtraDeathPushBack)
        {
            float enhancedDistance = hitContext.PushBackDistance * enemyData.deathPushBackMultiplier;
            enemy.StartCoroutine(ApplyPushback(
                hitContext.HitDirection,
                enhancedDistance,
                hitContext.PushBackDuration,
                hitContext.PushBackCurve
            ));
        }
        else
        {
            float resistedDistance = hitContext.PushBackDistance * (1f - enemyData.pushBackResistance);
            enemy.StartCoroutine(ApplyPushback(
                hitContext.HitDirection,
                resistedDistance,
                hitContext.PushBackDuration,
                hitContext.PushBackCurve
            ));
        }
    }
} 