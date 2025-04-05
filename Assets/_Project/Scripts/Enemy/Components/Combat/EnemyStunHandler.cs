using System;
using System.Collections;
using UnityEngine;
using Flyweight;

/// <summary>
/// Handles stun and stun recovery mechanics for enemies
/// </summary>
[DisallowMultipleComponent]
public class EnemyStunHandler : MonoBehaviour, IStunnable
{
    // Dependencies (will be set by BaseEnemy)
    private BaseEnemy enemy;
    private EnemyData enemyData;
    private Animator animator;
    private IAIMovement aiMovement;
    
    private bool isStunned = false;
    private bool isInStunRecovery = false;
    private float stunStressLevel = 0f;
    private float lastStunTime = 0f;
    private float stunRecoveryEndTime = 0f;
    private Coroutine stunDecayCoroutine;
    private bool isAccumulatingStress = false;
    
    public event Action<BaseEnemy> OnEnemyStunBegin;
    public event Action<BaseEnemy> OnEnemyStunEnd;
    public event Action<BaseEnemy> OnEnemyRecoveryBegin;
    public event Action<BaseEnemy> OnEnemyRecoveryEnd;
    
    // IStunnable implementation
    public event Action<GameObject> OnStunBegin;
    public event Action<GameObject> OnStunEnd;
    
    public bool IsStunned => isStunned;
    public bool IsInStunRecovery => isInStunRecovery;
    public float StunStressLevel => stunStressLevel;
    public bool CanBeStunned => !isStunned && !isInStunRecovery && !enemy.IsDead;
    
    private Camera mainCamera;
    
    // Initialize the handler with required references
    public void Initialize(BaseEnemy enemy, EnemyData data, Animator animator, IAIMovement movement)
    {
        this.enemy = enemy;
        this.enemyData = data;
        this.animator = animator;
        this.aiMovement = movement;
    }

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        // Subscribe to events from the generic CombatEventManager
        CombatEventManager.OnStunApplied += HandleStunEvent;
        CombatEventManager.OnRecoveryStarted += HandleRecoveryStarted;
    }
    
    private void OnDisable()
    {
        // Unsubscribe from events
        CombatEventManager.OnStunApplied -= HandleStunEvent;
        CombatEventManager.OnRecoveryStarted -= HandleRecoveryStarted;
    }
    
    /// <summary>
    /// Process stun application from a hit context, checking if the enemy should be stunned
    /// </summary>
    public void ProcessStunFromHit(HitContext hitContext)
    {
        if (!hitContext.EnableStun || isStunned || isInStunRecovery || enemy.IsDead) 
        {
            Debug.Log($"<color=gray>{gameObject.name} stun rejected: EnableStun={hitContext.EnableStun}, isStunned={isStunned}, isInRecovery={isInStunRecovery}</color>");
            return;
        }
        
        if (hitContext.Damage >= hitContext.StunThresholdDamage)
        {
            var resistedDuration = hitContext.StunDuration * (1f - enemyData.stunDurationResistance);
            Debug.Log($"<color=yellow>{gameObject.name} will be stunned for {resistedDuration}s</color>");
            
            // Start the stun coroutine
            ApplyStun(resistedDuration);
        }
        else
        {
            Debug.Log($"<color=gray>{gameObject.name} damage {hitContext.Damage} below stun threshold {hitContext.StunThresholdDamage}</color>");
        }
    }
    
    /// <summary>
    /// Apply stun effect to the enemy for a specific duration
    /// </summary>
    public bool ApplyStun(float duration)
    {
        // If in recovery or already stunned, can't be stunned again
        if (isInStunRecovery || isStunned || enemy.IsDead)
        {
            Debug.LogWarning($"[{gameObject.name}] Stun rejected: already stunned={isStunned}, in recovery={isInStunRecovery}, dead={enemy.IsDead}");
            
            // Broadcast the stun resistance event
            CombatEventManager.TriggerStunResisted(gameObject, gameObject, duration, 0);
            return false;
        }
        
        StartCoroutine(ApplyStunCoroutine(duration));
        return true;
    }
    
    /// <summary>
    /// Handle stun events from the combat event system
    /// </summary>
    public void HandleStunEvent(CombatEventManager.StunEventContext context)
    {
        // Only process if this is the target
        if (context.Target != gameObject) return;
        
        Debug.Log($"<color=yellow>{gameObject.name} received stun event with duration {context.Duration}s</color>");
        
        bool wasApplied = ApplyStun(context.Duration);
        
        if (!wasApplied)
        {
            // Stun was resisted
            CombatEventManager.TriggerStunResisted(context.Source, gameObject, context.Duration, context.Damage);
        }
    }
    
    private void HandleRecoveryStarted(CombatEventManager.RecoveryEventContext context)
    {
        // Only process if this is the target and it's a stun recovery
        if (context.Target != gameObject || context.EffectType != "Stun") return;
        
        // This event is usually triggered by our own code, but we could add additional effects here
    }
    
    /// <summary>
    /// Internal coroutine to apply the stun effect
    /// </summary>
    private IEnumerator ApplyStunCoroutine(float duration)
    {
        Debug.Log($"[{gameObject.name}] Applying stun for {duration}s");
        
        lastStunTime = Time.time;
        isStunned = true;
        
        // Force start stress accumulation regardless of settings
        // This ensures we're always building stress for the debug visualization
        if (stunDecayCoroutine != null)
        {
            StopCoroutine(stunDecayCoroutine);
            stunDecayCoroutine = null;
        }
        
        // IMPORTANT: Always add a minimum amount of stress immediately
        // This ensures we see something in the debug bar
        stunStressLevel += 1.0f;
        
        if (!isInStunRecovery) StartCoroutine(AccumulateStunStress(duration));
        
        enemy.SafeSetVariable("IsStunned", true);
        
        enemy.PauseBehaviorGraph();
        
        // Fire both the old and new event systems
        OnEnemyStunBegin?.Invoke(enemy);
        OnStunBegin?.Invoke(gameObject);
        
        aiMovement?.StopMovement();
        
        if (enemyData.rotateToFaceHitDirection && 
            (enemyData.onlyRotateWhenStunned || (!enemyData.onlyRotateWhenStunned && !enemyData.onlyRotateWhenPushedBack)))
        {
            if (enemy.lastHitDirection != Vector3.zero)
            {
                Vector3 facingDirection = new Vector3(-enemy.lastHitDirection.x, 0, -enemy.lastHitDirection.z).normalized;
                transform.forward = facingDirection;
            }
        }
        
        if (animator)
        {
            animator.SetFloat("Moving", 0);
        }
        
        yield return new WaitForSeconds(duration);
        
        if (enemy.IsDead) yield break;
        
        if (stunStressLevel >= enemyData.stunResistanceThreshold && enemyData.enableStunResistance)
        {
            CombatEventManager.TriggerStunEnded(gameObject, gameObject);
            
            CombatEventManager.TriggerRecoveryStarted(gameObject, gameObject, "Stun", enemyData.stunRecoveryDuration);
            
            StartStunRecovery();
        }
        else
        {
            CombatEventManager.TriggerStunEnded(gameObject, gameObject);
            
            EndStun();
        }
    }
    
    /// <summary>
    /// End the stunned state
    /// </summary>
    public void EndStun()
    {
        if (!isStunned || enemy.IsDead) return;
        
        isStunned = false;
        
        enemy.SafeSetVariable("IsStunned", false);
        
        enemy.ResumeBehaviorGraph();
        
        // Fire both the old and new event systems
        OnEnemyStunEnd?.Invoke(enemy);
        OnStunEnd?.Invoke(gameObject);
        
        if (animator)
        {
            // animator.SetBool("Stunned", false);
        }
    }
    
    /// <summary>
    /// Accumulate stun stress while the enemy is stunned
    /// </summary>
    private IEnumerator AccumulateStunStress(float stunDuration)
    {
        var timer = 0f;
        isAccumulatingStress = true;
        
        Debug.Log($"[{gameObject.name}] Starting stress accumulation: current={stunStressLevel}, rate={enemyData.stunStressAccumulationRate}/sec, threshold={enemyData.stunResistanceThreshold}");
        
        try
        {
            while (timer < stunDuration && isStunned)
            {
                var stressToAdd = Mathf.Max(1.0f, enemyData.stunStressAccumulationRate) * Time.deltaTime;
                stunStressLevel += stressToAdd;
                timer += Time.deltaTime;
                
                // Force update ratio for debugging purposes
                // var currentRatio = stunStressLevel / enemyData.stunResistanceThreshold;
                
                // Log more frequently
                // if (Mathf.FloorToInt(timer * 4) > Mathf.FloorToInt((timer - Time.deltaTime) * 4))
                // {
                //     Debug.Log($"[{gameObject.name}] Stress: {stunStressLevel:F2}/{enemyData.stunResistanceThreshold} ({currentRatio:P0}) after {timer:F1}s");
                // }

                yield return null;
            }
        }
        finally
        {
            isAccumulatingStress = false;
        }
        
        // Don't start decay immediately, give time to see the accumulated value
        yield return new WaitForSeconds(0.5f);
        
        // Start decaying stun stress when not stunned
        stunDecayCoroutine = StartCoroutine(DecayStunStress());
    }
    
    /// <summary>
    /// Decay stun stress when the enemy is not stunned
    /// </summary>
    private IEnumerator DecayStunStress()
    {
        // Only decay if below threshold and not in recovery
        while (stunStressLevel > 0 && !isStunned && !isInStunRecovery && 
               Time.time - lastStunTime <= enemyData.stunStressResetDelay)
        {
            stunStressLevel -= enemyData.stunStressDecayRate * Time.deltaTime;
            stunStressLevel = Mathf.Max(0, stunStressLevel);
            
            yield return null;
        }
    }
    
    /// <summary>
    /// Enter stun recovery mode (temporarily immune to stuns)
    /// </summary>
    private void StartStunRecovery()
    {
        EndStun();
        
        isInStunRecovery = true;
        stunRecoveryEndTime = Time.time + enemyData.stunRecoveryDuration;
        
        // enemy.SafeSetVariable("IsInStunRecovery", true);
        
        OnEnemyRecoveryBegin?.Invoke(enemy);
        
        if (enemyData.empowerDuringRecovery)
        {
            if (aiMovement != null)
            {
                EnemyAIMovement enemyMovement = aiMovement as EnemyAIMovement;
                if (enemyMovement)
                {
                    enemyMovement.MoveToPositionWithMultiplier(transform.position, enemyData.recoverySpeedMultiplier);
                }
                else
                {
                    // Fallback for non-EnemyAIMovement implementations or older versions
                    aiMovement.MoveTo(transform.position, enemyData.moveSpeed * enemyData.recoverySpeedMultiplier);
                }
            }
        }
        
        // Visual feedback - animation
        if (animator)
        {
            animator.SetTrigger("StunRecover");
        }
        
        // Play the recovery VFX if configured
        if (enemyData.stunRecoveryVfxSettings)
        {
            VFXFlyweightHelper.SpawnVfxAtPosition(
                enemyData.stunRecoveryVfxSettings,
                transform.position + Vector3.up * 0.5f,
                Quaternion.identity,
                Vector3.one
            );
        }
        
        Debug.Log($"{gameObject.name}: Entered stun recovery mode for {enemyData.stunRecoveryDuration} seconds");
    }
    
    /// <summary>
    /// End stun recovery mode
    /// </summary>
    public void EndStunRecovery()
    {
        if (!isInStunRecovery) return;
        
        isInStunRecovery = false;
        stunStressLevel = 0f;
        
        // Update behavior graph variables
        enemy.SafeSetVariable("IsInStunRecovery", false);
        enemy.SafeSetVariable("StunStressLevel", stunStressLevel);
        
        // Notify recovery has ended
        OnEnemyRecoveryEnd?.Invoke(enemy);
        
        // Reset any empowerment effects
        if (enemyData.empowerDuringRecovery && aiMovement != null)
        {
            // Try to reset movement parameters if available
            EnemyAIMovement enemyMovement = aiMovement as EnemyAIMovement;
            if (enemyMovement)
            {
                enemyMovement.SetMovementParams(enemyData.moveSpeed, enemyData.moveSpeed);
            }
            else
            {
                // Fallback - just move at normal speed
                aiMovement.MoveTo(transform.position, enemyData.moveSpeed);
            }
        }
        
        // Broadcast recovery ended event
        CombatEventManager.TriggerRecoveryEnded(gameObject, gameObject, "Stun");
        
        Debug.Log($"{gameObject.name}: Stun recovery ended, can be stunned again");
    }
    
    /// <summary>
    /// Handle updates for the stun system
    /// </summary>
    public void UpdateStunSystem()
    {
        // Handle stun resistance decay when not stunned
        if (!isStunned && !isInStunRecovery && stunStressLevel > 0 && 
            Time.time - lastStunTime > enemyData.stunStressResetDelay)
        {
            // After the delay, fully reset stun stress
            stunStressLevel = 0;
            enemy.SafeSetVariable("StunStressLevel", stunStressLevel);
        }
        
        // Handle recovery period ending
        if (isInStunRecovery && Time.time > stunRecoveryEndTime)
        {
            EndStunRecovery();
        }
    }
    
    /// <summary>
    /// Draw debug visualization for stun stress
    /// </summary>
    public void DrawStunStressBar()
    {
        // Early exit checks but with more diagnostics
        if (enemyData == null) 
        {
            Debug.LogError($"[{gameObject.name}] DrawStunStressBar: enemyData is null!");
            return;
        }
        
        // Calculate fill amount (ensure it's valid)
        float fillRatio = 0f;
        
        if (enemyData.stunResistanceThreshold <= 0)
        {
            Debug.LogError($"[{gameObject.name}] Invalid stunResistanceThreshold: {enemyData.stunResistanceThreshold}");
            
            // Force a minimum threshold for debugging
            enemyData.stunResistanceThreshold = 5f;
        }
        
        fillRatio = Mathf.Clamp01(stunStressLevel / enemyData.stunResistanceThreshold);
        
        // More frequent diagnostics when values change
        // if (Time.frameCount % 60 == 0 || isStunned || isAccumulatingStress) 
        // {
        //     Debug.Log($"[{gameObject.name}] Stun DEBUG - Stress: {stunStressLevel:F2}, " +
        //              $"Threshold: {enemyData.stunResistanceThreshold:F2}, " +
        //              $"Ratio: {fillRatio:P0}, " +
        //              $"isStunned: {isStunned}, " +
        //              $"inRecovery: {isInStunRecovery}, " +
        //              $"isAccumulating: {isAccumulatingStress}");
        // }
        
        // Make sure we have a camera
        if (!mainCamera) return;
        
        // Calculate screen position - check if the enemy is in front of camera
        Vector3 worldPos = transform.position + Vector3.up * 2f;
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
        
        // Check if behind camera (z is negative in screen space when behind camera)
        if (screenPos.z < 0) return;
        
        // Check if off screen
        if (screenPos.x < 0 || screenPos.x > Screen.width || 
            screenPos.y < 0 || screenPos.y > Screen.height) return;
        
        // Convert Y coordinate to GUI space (GUI uses top-left origin)
        float guiY = Screen.height - screenPos.y;
        
        // Create a rect for the background bar
        Rect rect = new Rect(screenPos.x - 50, guiY - 5, 100, 10);
        
        // Draw background bar
        Color originalColor = GUI.color;  // Store original color
        
        // Draw background (gray)
        GUI.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);  // Semi-transparent gray
        GUI.Box(rect, "");
        
        // Only draw fill if there's something to show
        if (fillRatio > 0.01f)
        {
            // Set fill color based on state
            if (isInStunRecovery)
            {
                GUI.color = new Color(1f, 0.9f, 0f, 0.8f);  // Bright yellow, semi-transparent
            }
            else
            {
                GUI.color = new Color(1f, 0.2f, 0.2f, 0.8f);  // Bright red, semi-transparent
            }
            
            // Draw the fill rect
            Rect fillRect = new Rect(rect.x, rect.y, rect.width * fillRatio, rect.height);
            GUI.Box(fillRect, "");
            
            // Add a text label showing the exact value for debugging
            GUI.color = Color.white;
            string stressText = $"{stunStressLevel:F1}/{enemyData.stunResistanceThreshold:F1}";
            GUI.Label(new Rect(rect.x, rect.y - 15, 100, 20), stressText);
        }
        
        // Add an indicator if in recovery mode
        if (isInStunRecovery)
        {
            GUI.color = Color.yellow;
            GUI.Label(new Rect(rect.x + rect.width + 5, rect.y, 100, 20), "RECOVERY");
        }
        
        // Reset color when done
        GUI.color = originalColor;
    }

    // Add a method to force stress for testing
    public void ForceStress(float amount)
    {
        stunStressLevel = amount;
        Debug.Log($"[{gameObject.name}] Forced stress to {amount}");
    }

    // Add this method to help with testing
    [ContextMenu("Force Add Stress")]
    public void AddTestStress()
    {
        // Add a significant amount of stress to make it visible
        stunStressLevel += enemyData.stunResistanceThreshold * 0.25f;
        
        // Clamp to avoid overflow
        stunStressLevel = Mathf.Min(stunStressLevel, enemyData.stunResistanceThreshold * 1.5f);
        
        Debug.Log($"[{gameObject.name}] Added test stress. Now at {stunStressLevel:F2}/{enemyData.stunResistanceThreshold:F2} ({stunStressLevel/enemyData.stunResistanceThreshold:P0})");
    }

    // Add a method to reset all stun-related state for testing
    [ContextMenu("Reset Stun System")]
    public void ResetStunSystem()
    {
        if (stunDecayCoroutine != null)
        {
            StopCoroutine(stunDecayCoroutine);
            stunDecayCoroutine = null;
        }
        
        isStunned = false;
        isInStunRecovery = false;
        stunStressLevel = 0f;
        lastStunTime = 0f;
        stunRecoveryEndTime = 0f;
        isAccumulatingStress = false;
        
        // Update behavior graph variables
        enemy.SafeSetVariable("IsStunned", false);
        enemy.SafeSetVariable("IsInStunRecovery", false);
        enemy.SafeSetVariable("StunStressLevel", 0f);
        
        Debug.Log($"[{gameObject.name}] Stun system reset completely.");
    }
} 