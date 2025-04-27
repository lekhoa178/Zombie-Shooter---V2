// Base enemy class that implements the interfaces

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;
using Flyweight;
using MoreMountains.Feedbacks;
using Random = UnityEngine.Random;

[RequireComponent(typeof(EnemyKnockbackHandler))]
[RequireComponent(typeof(EnemyStunHandler))]
public class BaseEnemy : MonoBehaviour, IEnemy, IHittable
{
    [SerializeField] protected EnemyData enemyData;
    
    [Header("Feedbacks")]
    [Tooltip("Configuration for squash and stretch efFfects when hit")]
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
    protected bool isElite = false;
    protected VFXFlyweight elitePersistentVfx;
    protected Material[] originalMaterials;
    protected Vector3 originalScale;
    
    // Events
    public event Action<IEnemy> OnDeath;
    public event Action<IHittable, float, Vector3> OnDamaged;
    public event Action<IEnemy, float> OnHealthChanged;
    
    // Interface properties
    public EnemyData Data => enemyData;
    public Transform Transform => transform;
    public GameObject GameObject => gameObject;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => isElite ? enemyData.GetEffectiveMaxHealth(true) : enemyData.maxHealth;
    public float HealthPercentage => currentHealth / MaxHealth;
    public bool IsDead => isDead;
    public bool IsStunned => stunHandler != null && stunHandler.IsStunned && !stunHandler.IsInStunRecovery;
    public bool IsInvulnerable => isInvulnerable;
    public bool IsElite => isElite;
    
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
        
        // Store original scale for reference
        originalScale = transform.localScale;
        
        // Check if this enemy can be elite and roll the chance
        if (enemyData.canBeElite && Random.value <= enemyData.eliteSpawnChance)
        {
            SetEliteStatus(true);
        }
        
        // Set initial health based on elite status
        currentHealth = MaxHealth;
        
        RegisterBehaviorMethods();
        
        if (knockbackHandler == null)
            knockbackHandler = gameObject.GetComponent<EnemyKnockbackHandler>();
        
        if (stunHandler == null)
            stunHandler = gameObject.GetComponent<EnemyStunHandler>();
        
        knockbackHandler.Initialize(this, enemyData, Rb, Animator, AIMovement);
        stunHandler.Initialize(this, enemyData, Animator, AIMovement);
        
        Debug.Log($"<color=blue>{gameObject.name}: BaseEnemy Awake completed, events registered</color>");
    }
    
    protected virtual void Start()
    {
        // Apply Elite effects after all components are initialized
        if (isElite)
        {
            ApplyEliteEffects();
        }
    }
    
    public void SetEliteStatus(bool elite)
    {
        isElite = elite;
        
        if (isElite)
        {
            // Store original materials for possible material changes
            StoreOriginalMaterials();
            
            // Update behavior graph
            if (BehaviorGraphAgent != null)
            {
                SafeSetVariable("IsElite", true);
            }
        }
        
        // Update current health based on new max health
        currentHealth = MaxHealth;
    }
    
    protected virtual void StoreOriginalMaterials()
    {
        // Store original materials for reference
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        List<Material> materials = new List<Material>();
        
        foreach (Renderer renderer in renderers)
        {
            foreach (Material material in renderer.materials)
            {
                materials.Add(new Material(material));
            }
        }
        
        originalMaterials = materials.ToArray();
    }
    
    protected virtual void ApplyEliteEffects()
    {
        // Scale up the enemy if it's elite
        if (enemyData.eliteScaleMultiplier > 1.0f)
        {
            transform.localScale = originalScale * enemyData.eliteScaleMultiplier;
        }

        // Spawn persistent VFX if specified
        if (enemyData.elitePersistentVfxSettings != null)
        {
            SpawnElitePersistentVFX();
        }
        
        // Spawn one-time spawn VFX if specified
        if (enemyData.eliteSpawnVfxSettings != null)
        {
            VFXFlyweightHelper.SpawnVfxAtPosition(
                enemyData.eliteSpawnVfxSettings,
                transform.position,
                Quaternion.identity,
                enemyData.GetElitePersistentVfxScale()
            );
        }
        
        // Play elite spawn sound if specified
        if (enemyData.eliteSpawnSound != null)
        {
            AudioSource.PlayClipAtPoint(enemyData.eliteSpawnSound, transform.position);
        }
        
        // Log that we created an elite enemy
        Debug.Log($"<color=#FFD700>Elite {enemyData.enemyName} spawned at {transform.position}</color>");
    }

    protected virtual void SpawnElitePersistentVFX()
    {
        if (elitePersistentVfx != null) return;
        
        VFXFlyweight vfxInstance = VFXFlyweightHelper.SpawnVfxAtPosition(
            enemyData.elitePersistentVfxSettings,
            transform.position + enemyData.elitePersistentVfxOffset,
            Quaternion.identity,
            enemyData.GetElitePersistentVfxScale()
        );
        
        // Parent and save reference to destroy it later
        if (vfxInstance != null)
        {
            vfxInstance.transform.SetParent(transform);
            elitePersistentVfx = vfxInstance;
        }
    }
    
    protected virtual void RegisterBehaviorMethods()
    {
        if (BehaviorGraphAgent != null)
        {
            // Use the safe method that won't crash
            SafeSetVariable("HealthPercentage", currentHealth / MaxHealth);
            SafeSetVariable("IsDead", isDead);
            SafeSetVariable("IsElite", isElite);
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
                SafeSetVariable("HealthPercentage", currentHealth / MaxHealth);
                SafeSetVariable("currentHealth", currentHealth);
            }
            
            if (!Mathf.Approximately(oldHealth, currentHealth))
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
                
                if (isElite && enemyData.eliteHitSounds != null && enemyData.eliteHitSounds.Length > 0)
                {
                    int randomIndex = UnityEngine.Random.Range(0, enemyData.eliteHitSounds.Length);
                    AudioSource.PlayClipAtPoint(enemyData.eliteHitSounds[randomIndex], transform.position);
                }
                
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
        
        // Handle elite death effects if applicable
        if (isElite && enemyData.eliteDeathVfxSettings != null)
        {
            VFXFlyweightHelper.SpawnVfxAtPosition(
                enemyData.eliteDeathVfxSettings,
                transform.position,
                Quaternion.identity
            );
            
            // Play elite death sound if specified
            if (enemyData.eliteDeathSounds != null && enemyData.eliteDeathSounds.Length > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, enemyData.eliteDeathSounds.Length);
                AudioSource.PlayClipAtPoint(enemyData.eliteDeathSounds[randomIndex], transform.position);
            }
        }
        // Handle normal death visuals for non-elite
        else if (enemyData.deathVFX)
        {
            Instantiate(enemyData.deathVFX, transform.position, Quaternion.identity);
            
            // Play normal death sound
            if (enemyData.deathSounds != null && enemyData.deathSounds.Length > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, enemyData.deathSounds.Length);
                AudioSource.PlayClipAtPoint(enemyData.deathSounds[randomIndex], transform.position);
            }
        }
        
        // Destroy elite persistent VFX if it exists
        if (elitePersistentVfx != null)
        {
            Destroy(elitePersistentVfx);
            elitePersistentVfx = null;
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

    protected void HandleHit(CombatEventManager.HitEventContext context)
    {
        if (context.Target != gameObject) return;
        
        Debug.Log($"<color=orange>{gameObject.name} received hit from event system ({context.Damage} damage)</color>");
    }

    protected void HandleDeath(CombatEventManager.CombatEventContext context)
    {
        if (context.Target != gameObject) return;
        
        // We should already be dead when we receive this event, but just in case
        if (!isDead) Die();
    }
}