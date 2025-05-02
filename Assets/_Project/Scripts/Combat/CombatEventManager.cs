using System;
using UnityEngine;
using System.Collections.Generic;
using UnityUtils;

/// <summary>
/// Centralized event system for combat-related events.
/// Decouples systems by allowing components to broadcast and listen for events
/// without direct references to each other.
/// </summary>
public class CombatEventManager : PersistentSingleton<CombatEventManager>
{
    // Define event context classes to carry more detailed information
    
    #region Event Context Classes
    /// <summary>
    /// Base context for all combat events
    /// </summary>
    public class CombatEventContext
    {
        public GameObject Source { get; set; }
        public GameObject Target { get; set; }
        
        public CombatEventContext(GameObject source, GameObject target)
        {
            Source = source;
            Target = target;
        }
        
        /// <summary>
        /// Get the component of specified type from the target
        /// </summary>
        public T GetTargetComponent<T>() where T : class
        {
            return Target?.GetComponent<T>();
        }
        
        /// <summary>
        /// Get the component of specified type from the source
        /// </summary>
        public T GetSourceComponent<T>() where T : class
        {
            return Source?.GetComponent<T>();
        }
    }
    
    /// <summary>
    /// Context for stun-related events
    /// </summary>
    public class StunEventContext : CombatEventContext
    {
        public float Duration { get; set; }
        public float Damage { get; set; }
        public bool EnableResistance { get; set; }
        
        public StunEventContext(GameObject source, GameObject target, float duration, float damage) 
            : base(source, target)
        {
            Duration = duration;
            Damage = damage;
            EnableResistance = true;
        }
    }
    
    /// <summary>
    /// Context for knockback-related events
    /// </summary>
    public class KnockbackEventContext : CombatEventContext
    {
        public Vector3 Direction { get; set; }
        public float Distance { get; set; }
        public float Duration { get; set; }
        public AnimationCurve Curve { get; set; }
        public bool IsDeath { get; set; }
        
        public KnockbackEventContext(GameObject source, GameObject target, Vector3 direction, 
                                    float distance, float duration, AnimationCurve curve = null) 
            : base(source, target)
        {
            Direction = direction;
            Distance = distance;
            Duration = duration;
            Curve = curve ?? AnimationCurve.EaseInOut(0, 0, 1, 1);
            IsDeath = false;
        }
    }
    
    /// <summary>
    /// Context for hit-related events
    /// </summary>
    public class HitEventContext : CombatEventContext
    {
        public Vector3 HitDirection { get; set; }
        public Vector3 HitPosition { get; set; }
        public float Damage { get; set; }
        public bool IsCritical { get; set; }
        
        public HitEventContext(GameObject source, GameObject target, Vector3 hitDirection, 
                              Vector3 hitPosition, float damage) 
            : base(source, target)
        {
            HitDirection = hitDirection;
            HitPosition = hitPosition;
            Damage = damage;
            IsCritical = false;
        }
    }
    
    /// <summary>
    /// Context for recovery from status effects
    /// </summary>
    public class RecoveryEventContext : CombatEventContext
    {
        public string EffectType { get; set; }
        public float RecoveryDuration { get; set; }
        
        public RecoveryEventContext(GameObject source, GameObject target, string effectType, float recoveryDuration) 
            : base(source, target)
        {
            EffectType = effectType;
            RecoveryDuration = recoveryDuration;
        }
    }
    #endregion
    
    #region Events
    // Hit Events
    public static event Action<HitEventContext> OnHit;
    public static event Action<HitEventContext> OnHitBlocked;
    public static event Action<HitEventContext> OnCriticalHit;
    
    // Stun Events
    public static event Action<StunEventContext> OnStunApplied;
    public static event Action<StunEventContext> OnStunResisted;
    public static event Action<CombatEventContext> OnStunEnded;
    
    // Knockback Events
    public static event Action<KnockbackEventContext> OnKnockbackApplied;
    public static event Action<CombatEventContext> OnKnockbackEnded;
    
    // Recovery Events
    public static event Action<RecoveryEventContext> OnRecoveryStarted;
    public static event Action<RecoveryEventContext> OnRecoveryEnded;
    
    // Death Events
    public static event Action<CombatEventContext> OnDeath;
    #endregion
    
    #region Event Trigger Methods
    // Hit Trigger Methods
    public static void TriggerHit(GameObject source, GameObject target, Vector3 direction, Vector3 position, float damage)
    {
        if (!instance)
        {
            Debug.LogError("CombatEventManager instance is null!");
            return;
        }

        // Prevent duplicate hit processing in the same frame
        string hitKey = $"{source?.GetInstanceID()}_{target?.GetInstanceID()}_{Time.frameCount}";
        
        if (!instance._processedHitsThisFrame.Add(hitKey))
        {
            Debug.LogWarning($"Prevented duplicate hit event in the same frame: {hitKey}");
            return;
        }

        var context = new HitEventContext(source, target, direction, position, damage);

        Debug.Log($"<color=cyan>Triggering Hit Event</color> - " +
                  $"Source: {source?.name}, " +
                  $"Target: {target?.name}, " +
                  $"Frame: {Time.frameCount}");
        
        HandleHit(context);

        OnHit?.Invoke(context);
    }
    
    public static void TriggerCriticalHit(GameObject source, GameObject target, Vector3 hitDirection, Vector3 hitPosition, float damage)
    {
        var context = new HitEventContext(source, target, hitDirection, hitPosition, damage) { IsCritical = true };
        
        Debug.Log($"<color=cyan>Triggering Critical Hit Event</color> - " +
                  $"Source: {source?.name}, " +
                  $"Target: {target?.name}, " +
                  $"Frame: {Time.frameCount}");
        
        HandleHit(context);

        OnCriticalHit?.Invoke(context);
        OnHit?.Invoke(context);
    }
    
    // Stun Trigger Methods
    public static void TriggerStun(GameObject source, GameObject target, float duration, float damage)
    {
        var context = new StunEventContext(source, target, duration, damage);
        OnStunApplied?.Invoke(context);
        
        // Auto-forward to the IStunnable interface if implemented
        if (target != null)
        {
            IStunnable stunnable = target.GetComponent<IStunnable>();
            stunnable?.HandleStunEvent(context);
        }
    }
    
    public static void TriggerStunResisted(GameObject source, GameObject target, float duration, float damage)
    {
        var context = new StunEventContext(source, target, duration, damage);
        OnStunResisted?.Invoke(context);
    }
    
    public static void TriggerStunEnded(GameObject source, GameObject target)
    {
        var context = new CombatEventContext(source, target);
        OnStunEnded?.Invoke(context);
    }
    
    // Knockback Trigger Methods
    public static void TriggerKnockback(GameObject source, GameObject target, Vector3 direction, float distance, float duration, AnimationCurve curve = null)
    {
        var context = new KnockbackEventContext(source, target, direction, distance, duration, curve);
        OnKnockbackApplied?.Invoke(context);
        
        // Auto-forward to the IKnockbackable interface if implemented
        if (target != null)
        {
            IKnockbackable knockbackable = target.GetComponent<IKnockbackable>();
            knockbackable?.HandleKnockbackEvent(context);
        }
    }
    
    public static void TriggerDeathKnockback(GameObject source, GameObject target, Vector3 direction, float distance, float duration, AnimationCurve curve = null)
    {
        var context = new KnockbackEventContext(source, target, direction, distance, duration, curve) { IsDeath = true };
        OnKnockbackApplied?.Invoke(context);
        
        // Auto-forward to the IKnockbackable interface if implemented
        if (target != null)
        {
            IKnockbackable knockbackable = target.GetComponent<IKnockbackable>();
            knockbackable?.HandleKnockbackEvent(context);
        }
    }
    
    public static void TriggerKnockbackEnded(GameObject source, GameObject target)
    {
        var context = new CombatEventContext(source, target);
        OnKnockbackEnded?.Invoke(context);
    }
    
    // Recovery Trigger Methods
    public static void TriggerRecoveryStarted(GameObject source, GameObject target, string effectType, float recoveryDuration)
    {
        var context = new RecoveryEventContext(source, target, effectType, recoveryDuration);
        OnRecoveryStarted?.Invoke(context);
    }
    
    public static void TriggerRecoveryEnded(GameObject source, GameObject target, string effectType)
    {
        var context = new RecoveryEventContext(source, target, effectType, 0);
        OnRecoveryEnded?.Invoke(context);
    }
    
    public static void TriggerDeath(GameObject source, GameObject target)
    {
        var context = new CombatEventContext(source, target);
        OnDeath?.Invoke(context);
    }
    #endregion

    private readonly HashSet<string> _processedHitsThisFrame = new HashSet<string>();

    private void LateUpdate()
    {
        _processedHitsThisFrame.Clear();
    }

    [ContextMenu("Debug Event Listeners")]
    private void DebugEventListeners()
    {
        if (OnHit != null)
        {
            Debug.Log("<color=yellow>Hit Event Listeners:</color>");
            foreach (var d in OnHit.GetInvocationList())
            {
                Debug.Log($"- {d.Target?.GetType().Name}.{d.Method.Name}");
            }
        }
        
        if (OnStunApplied != null)
        {
            Debug.Log("<color=yellow>Stun Event Listeners:</color>");
            foreach (var d in OnStunApplied.GetInvocationList())
            {
                Debug.Log($"- {d.Target?.GetType().Name}.{d.Method.Name}");
            }
        }
        
        if (OnKnockbackApplied != null)
        {
            Debug.Log("<color=yellow>Knockback Event Listeners:</color>");
            foreach (var d in OnKnockbackApplied.GetInvocationList())
            {
                Debug.Log($"- {d.Target?.GetType().Name}.{d.Method.Name}");
            }
        }
    }

    private static void HandleHit(CombatEventManager.HitEventContext context)
    {
        // We only care about hits that target this enemy
        if (context.Target == context.Source) return;
        
        // Create a HitContext from the event context
        var hitContext = HitContext.Create(
            context.HitDirection,
            context.HitPosition,
            context.Damage,
            context.Source
        );
        
        // Apply additional properties if context contains them
        if (context.IsCritical)
        {
            hitContext.AsCritical();
        }
        
        // Call the context method to apply damage and effects
        context.Target.GetInterface<IHittable>()?.OnHitWithContext(hitContext);
    }
} 