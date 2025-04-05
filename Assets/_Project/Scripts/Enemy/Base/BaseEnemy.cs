// Base enemy class that implements the interfaces

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;
using Flyweight;
using MoreMountains.Feedbacks;

[RequireComponent(typeof(EnemyKnockbackHandler))]
[RequireComponent(typeof(EnemyStunHandler))]
public class BaseEnemy : MonoBehaviour, IEnemy, IHittable
{
    [SerializeField] protected EnemyData enemyData;
    
    [Header("Feedbacks")]
    [Tooltip("Configuration for squash and stretch effects when hit")]
    public MMF_Player hitFeedback;
    
    // Components
    protected Rigidbody Rb;
    protected Animator Animator;
    protected BehaviorGraphAgent BehaviorGraphAgent;
    protected IAIMovement AIMovement;
    protected EnemyHealthBarComponent HealthBarComponent;
    protected Collider Collider;
    
    // State
    protected float currentHealth;
    protected bool isDead;
    protected bool isInvulnerable;
    
    // Events
    public event Action<IEnemy> OnDeath;
    public event Action<IHittable, float, Vector3> OnDamaged;
    public event Action<IEnemy> OnStunBegin;
    public event Action<IEnemy> OnStunEnd;
    public event Action<IEnemy> OnPushbackStart;
    public event Action<IEnemy> OnPushbackEnd;
    public event Action<IEnemy, float> OnHealthChanged;
    
    // Interface properties
    public EnemyData Data => enemyData;
    public Transform Transform => transform;
    public GameObject GameObject => gameObject;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => enemyData.maxHealth;
    public bool IsDead => isDead;
    public bool IsStunned => stunHandler != null && stunHandler.IsStunned;
    public bool IsInvulnerable => isInvulnerable;
    
    // Add the component references:
    [Header("Combat Handlers")]
    [SerializeField] private EnemyKnockbackHandler knockbackHandler;
    [SerializeField] private EnemyStunHandler stunHandler;
    
    [HideInInspector] 
    public Vector3 lastHitDirection = Vector3.zero;
    
    private bool _isProcessingHit = false;
    
    protected virtual void Awake()
    {
        Rb = GetComponent<Rigidbody>();
        Animator = GetComponent<Animator>();
        BehaviorGraphAgent = GetComponent<BehaviorGraphAgent>();
        AIMovement = GetComponent<IAIMovement>();
        HealthBarComponent = GetComponent<EnemyHealthBarComponent>();
        Collider = GetComponent<Collider>();
        
        currentHealth = enemyData.maxHealth;
        
        RegisterBehaviorMethods();
        
        if (knockbackHandler == null)
            knockbackHandler = gameObject.GetComponent<EnemyKnockbackHandler>();
        
        if (stunHandler == null)
            stunHandler = gameObject.GetComponent<EnemyStunHandler>();
        
        knockbackHandler.Initialize(this, enemyData, Rb, Animator, AIMovement);
        stunHandler.Initialize(this, enemyData, Animator, AIMovement);
        
        knockbackHandler.OnEnemyPushbackStart += (enemy) => OnPushbackStart?.Invoke(this);
        knockbackHandler.OnEnemyPushbackEnd += (enemy) => OnPushbackEnd?.Invoke(this);
        
        stunHandler.OnStunBegin += (enemy) => OnStunBegin?.Invoke(this);
        stunHandler.OnStunEnd += (enemy) => OnStunEnd?.Invoke(this);
        
        Debug.Log($"<color=blue>{gameObject.name}: BaseEnemy Awake completed, events registered</color>");
    }
    
    protected virtual void RegisterBehaviorMethods()
    {
        if (BehaviorGraphAgent != null)
        {
            // Use the safe method that won't crash
            SafeSetVariable("HealthPercentage", currentHealth / enemyData.maxHealth);
            SafeSetVariable("IsDead", isDead);
        }
    }
    
    public void SafeSetVariable<T>(string variableName, T value)
    {
        if (!BehaviorGraphAgent || !BehaviorGraphAgent.Graph) return;
        
        try {
            BehaviorGraphAgent.SetVariableValue(variableName, value);
        }
        catch (InvalidOperationException) {
            Debug.LogWarning($"Variable '{variableName}' not found in behavior graph for {gameObject.name}. Add it to your Behavior Graph's blackboard.");
        }
        catch (Exception e) {
            Debug.LogError($"Error setting variable '{variableName}' in behavior graph: {e.Message}");
        }
    }
    
    
    public virtual void OnHit(Vector3 direction, float damage)
    {
        var context = HitContext.Create(direction, transform.position, damage, gameObject);
        OnHitWithContext(context);
    }
    
    
    public virtual void OnHitWithContext(HitContext hitContext)
    {
        if (_isProcessingHit)
        {
            Debug.LogWarning($"<color=red>Prevented recursive hit on {gameObject.name}</color>");
            return;
        }

        if (isDead || isInvulnerable) return;

        try
        {
            _isProcessingHit = true;
            
            Debug.Log($"<color=yellow>Processing hit on {gameObject.name} from {hitContext.Instigator?.name}</color>");
            
            lastHitDirection = hitContext.HitDirection;
            
            // Store previous health to detect changes
            float oldHealth = currentHealth;
            
            // Apply damage
            currentHealth -= hitContext.Damage;
            
            // Update health percentage in behavior graph
            if (BehaviorGraphAgent)
            {
                SafeSetVariable("HealthPercentage", currentHealth / enemyData.maxHealth);
                SafeSetVariable("currentHealth", currentHealth);
            }
            
            // Notify health changed (for UI updates)
            if (oldHealth != currentHealth)
            {
                OnHealthChanged?.Invoke(this, currentHealth);
                OnDamaged?.Invoke(this, hitContext.Damage, hitContext.HitDirection);
            }
            
            var willDie = currentHealth <= 0;
            
            // Handle death or normal hit
            if (willDie)
            {
                if (knockbackHandler)
                {
                    knockbackHandler.ApplyKnockbackFromHit(hitContext, true);
                }
                
                CombatEventManager.TriggerDeath(hitContext.Instigator, gameObject);
                Die();
            }
            else
            {
                // Use TriggerOnHit to centralize event dispatching instead of separate calls
                hitContext.TriggerOnHit(gameObject);
                
                // Still trigger feedback effects locally
                if (hitFeedback)
                {
                    hitFeedback.PlayFeedbacks();
                }
                
                if (Animator != null && !IsDead)
                {
                    Animator.SetTrigger("Damage");
                }
                
                // Spawn hit reaction VFX at hit position using enemyData configuration
                if (enemyData.hitReactionVfxSettings)
                {
                    Vector3 hitPosition = transform.position;
                
                    VFXFlyweightHelper.SpawnVfxAtPosition(
                        enemyData.hitReactionVfxSettings,
                        hitPosition,
                        Quaternion.LookRotation(hitContext.HitDirection),
                        enemyData.GetHitReactionVfxScale()
                    );
                }

                // Handle additional hit reaction VFX if enabled
                if (enemyData.enableAdditionalHitReactionVfx && 
                    enemyData.additionalHitReactionVfxSettings && 
                    enemyData.spawnHitReactionPerHit)
                {
                    StartCoroutine(SpawnAdditionalHitReactionVfx(transform.position, hitContext.HitDirection));
                }
            }
        }
        finally
        {
            _isProcessingHit = false;
        }
    }
    
    
    public virtual void OnHitFromDirection(Vector3 hitPosition, Vector3 hitDirection, float damage)
    {
        OnHit(hitDirection, damage);
    }
    
    
    public virtual void ReactToHit(HitReactionType reactionType, Vector3 direction)
    {
        if (isDead) return;
        
        switch (reactionType)
        {
            case HitReactionType.Small:
                Animator.SetTrigger("Damage");
                break;
            case HitReactionType.Medium:
                Animator.SetTrigger("Damage");
                break;
            case HitReactionType.Knockback:
                ApplyKnockback(direction, 5f);
                Animator.SetTrigger("Damage");
                break;
        }
    }
    
    protected virtual void ApplyKnockback(Vector3 direction, float force)
    {
        if (Rb != null)
        {
            Rb.AddForce(direction.normalized * force, ForceMode.Impulse);
        }
    }
    
    
    protected virtual void Die()
    {
        isDead = true;
        
        if (BehaviorGraphAgent && BehaviorGraphAgent.GetVariableID("IsDead", out _))
        {
            SafeSetVariable("IsDead", true);
            PauseBehaviorGraph();
        }
        
        Animator.SetTrigger("Die");
        Animator.SetFloat("Moving", 1); // Set to 1 to ensure the death animation plays correctly
        
        Collider.enabled = false;
        
        OnDeath?.Invoke(this);
        
        // Handle death visuals
        if (enemyData.deathVFX)
        {
            Instantiate(enemyData.deathVFX, transform.position, Quaternion.identity);
        }
        
        // Play random death sound
        if (enemyData.deathSounds != null && enemyData.deathSounds.Length > 0)
        {
            // Play sound logic
        }
        
        // Handle game object (destroy or pool)
        Destroy(gameObject, 3f); // Consider object pooling for better performance
    }
    
    protected virtual void Update()
    {
        if (stunHandler)
        {
            stunHandler.UpdateStunSystem();
        }
    }

    // For OnGUI debugging:
    private void OnGUI()
    {
        if (stunHandler)
        {
            stunHandler.DrawStunStressBar();
        }
    }

    public void PauseBehaviorGraph()
    {
        if (!BehaviorGraphAgent || !BehaviorGraphAgent.Graph) return;
        
        SafeSetVariable("IsPaused", true);
            
        // Debug.Log($"{gameObject.name}: Behavior Graph Paused");
    }

    public void ResumeBehaviorGraph()
    {
        if (!BehaviorGraphAgent || !BehaviorGraphAgent.Graph) return;
        
        SafeSetVariable("IsPaused", false);
            
        // Debug.Log($"{gameObject.name}: Behavior Graph Resumed");
    }
    
    // For when enemy gets hit by player
    private IEnumerator SpawnAdditionalHitReactionVfx(Vector3 hitPosition, Vector3 hitDirection)
    {
        if (enemyData.additionalHitReactionVfxDelay > 0)
        {
            yield return new WaitForSeconds(enemyData.additionalHitReactionVfxDelay);
        }

        // Determine spawn position
        Vector3 spawnPosition;
        if (enemyData.spawnHitReactionAtHitLocation)
        {
            spawnPosition = hitPosition;
        }
        else
        {
            spawnPosition = transform.position;
        }

        // Apply offset
        spawnPosition += enemyData.additionalHitReactionVfxOffset;

        // Calculate rotation
        Quaternion rotation;
        if (enemyData.hitReactionFaceHitDirection)
        {
            rotation = Quaternion.LookRotation(hitDirection);
        }
        else
        {
            rotation = Quaternion.Euler(enemyData.additionalHitReactionVfxRotation);
        }

        // Spawn the additional hit reaction VFX using the helper
        if (enemyData.additionalHitReactionVfxDelay > 0)
        {
            // Using StartCoroutine directly since we're already in a coroutine
            yield return StartCoroutine(
                VFXFlyweightHelper.SpawnDelayedVfx(
                    enemyData.additionalHitReactionVfxSettings,
                    spawnPosition,
                    enemyData.GetAdditionalHitReactionVfxScale(),
                    enemyData.additionalHitReactionVfxDelay,
                    rotation
                )
            );
        }
        else
        {
            VFXFlyweightHelper.SpawnVfxAtPosition(
                enemyData.additionalHitReactionVfxSettings,
                spawnPosition,
                rotation,
                enemyData.GetAdditionalHitReactionVfxScale()
            );
        }
    }
    
    [ContextMenu("Debug Enemy Data")]
    public void DebugEnemyData()
    {
        if (enemyData == null)
        {
            Debug.LogError("No EnemyData assigned!");
            return;
        }
        
        Debug.Log($"Enemy Data Debug for {gameObject.name}:\n" +
                  $"- stunResistanceThreshold: {enemyData.stunResistanceThreshold}\n" +
                  $"- stunStressAccumulationRate: {enemyData.stunStressAccumulationRate}\n" +
                  $"- enableStunResistance: {enemyData.enableStunResistance}\n" +
                  $"- stunRecoveryDuration: {enemyData.stunRecoveryDuration}\n" +
                  $"- stunDurationResistance: {enemyData.stunDurationResistance}");
    }

    private void OnEnable()
    {
        CombatEventManager.OnHit += HandleHit;
        CombatEventManager.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        CombatEventManager.OnHit -= HandleHit;
        CombatEventManager.OnDeath -= HandleDeath;
    }

    private void HandleHit(CombatEventManager.HitEventContext context)
    {
        // We only care about hits that target this enemy
        if (context.Target != gameObject) return;
        
        // Log info about hit from the event system
        Debug.Log($"<color=orange>{gameObject.name} received hit from event system ({context.Damage} damage)</color>");
        
        // We don't need to apply damage again since the context already comes from an OnHit call
        // This is just for handling VFX and other effects
    }

    private void HandleDeath(CombatEventManager.CombatEventContext context)
    {
        if (context.Target != gameObject) return;
        
        // We should already be dead when we receive this event, but just in case
        if (!isDead) Die();
    }
}