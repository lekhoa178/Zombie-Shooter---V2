using UnityEngine;

/// <summary>
/// Generic context class for transferring hit information between attackers and targets
/// without creating direct references between them.
/// </summary>
public class HitContext
{
    // Basic hit properties
    public Vector3 HitDirection { get; set; }
    public Vector3 HitPosition { get; set; }
    public float Damage { get; set; }
    public GameObject Instigator { get; set; }
    
    // Push back properties
    public bool EnablePushBack { get; set; }
    public float PushBackDistance { get; set; }
    public float PushBackDuration { get; set; }
    public AnimationCurve PushBackCurve { get; set; }
    public float DeathPushBackMultiplier { get; set; }
    
    // Stun properties
    public bool EnableStun { get; set; }
    public float StunThresholdDamage { get; set; }
    public float StunDuration { get; set; }
    
    // Special effects
    public bool IsCriticalHit { get; set; }
    
    /// <summary>
    /// Creates a hit context with basic information
    /// </summary>
    public static HitContext Create(Vector3 hitDirection, Vector3 hitPosition, float damage, GameObject instigator = null)
    {
        return new HitContext
        {
            HitDirection = hitDirection.normalized,
            HitPosition = hitPosition,
            Damage = damage,
            Instigator = instigator,
            
            // Default values
            EnablePushBack = false,
            PushBackDistance = 1f,
            PushBackDuration = 0.2f,
            PushBackCurve = AnimationCurve.EaseInOut(0, 0, 1, 1),
            DeathPushBackMultiplier = 1.5f,
            
            EnableStun = false,
            StunThresholdDamage = 5f,
            StunDuration = 1f,
            
            IsCriticalHit = false
        };
    }
    
    /// <summary>
    /// Configures push back settings for this hit context
    /// </summary>
    public HitContext WithPushBack(bool enable, float distance, float duration, AnimationCurve curve = null, float deathMultiplier = 1.5f)
    {
        EnablePushBack = enable;
        PushBackDistance = distance;
        PushBackDuration = duration;
        PushBackCurve = curve ?? AnimationCurve.EaseInOut(0, 0, 1, 1);
        DeathPushBackMultiplier = deathMultiplier;
        return this;
    }
    
    /// <summary>
    /// Configures stun settings for this hit context
    /// </summary>
    public HitContext WithStun(bool enable, float thresholdDamage, float duration)
    {
        EnableStun = enable;
        StunThresholdDamage = thresholdDamage;
        StunDuration = duration;
        return this;
    }
    
    /// <summary>
    /// Marks this hit as critical
    /// </summary>
    public HitContext AsCritical(bool isCritical = true)
    {
        IsCriticalHit = isCritical;
        return this;
    }
    
    /// <summary>
    /// Creates a hit context from an AttackHitConfiguration
    /// </summary>
    public static HitContext FromConfiguration(AttackHitConfiguration config, Vector3 hitDirection, Vector3 hitPosition, float damage, GameObject instigator = null)
    {
        var context = Create(hitDirection, hitPosition, damage, instigator);
        
        // Apply push back settings
        context.WithPushBack(
            config.EnablePushBack,
            config.PushBackDistance,
            config.PushBackDuration,
            config.PushBackCurve,
            config.DeathPushBackMultiplier
        );
        
        // Apply stun settings
        context.WithStun(
            config.EnableStun,
            config.StunThresholdDamage,
            config.StunDuration
        );
        
        return context;
    }
    
    /// <summary>
    /// Broadcasts this hit context to the combat event system
    /// </summary>
    public void BroadcastHit()
    {
        CombatEventManager.TriggerHit(
            Instigator, 
            null, // The target is unknown at this point
            HitDirection, 
            HitPosition, 
            Damage
        );
    }
    
    /// <summary>
    /// Broadcasts this hit context as a critical hit to the combat event system
    /// </summary>
    public void BroadcastCriticalHit()
    {
        IsCriticalHit = true;
        
        CombatEventManager.TriggerCriticalHit(
            Instigator,
            null, // The target is unknown at this point
            HitDirection,
            HitPosition,
            Damage
        );
    }
    
    /// <summary>
    /// Triggers a hit on a specific target using the unified combat event system
    /// </summary>
    /// <param name="target">The target GameObject that will receive the hit</param>
    public void TriggerOnHit(GameObject target)
    {
        if (target == null)
        {
            Debug.LogWarning("TriggerOnHit called with null target");
            return;
        }
        
        // Log debugging info if needed
        if (Debug.isDebugBuild)
        {
            LogDebugInfo($"TriggerOnHit to {target.name}");
        }
        
        // If this is a critical hit, also trigger the critical hit event
        if (IsCriticalHit)
        {
            CombatEventManager.TriggerCriticalHit(
                Instigator,
                target,
                HitDirection,
                HitPosition,
                Damage
            );
        }
        else
        {
            CombatEventManager.TriggerHit(
                Instigator,
                target,
                HitDirection,
                HitPosition,
                Damage
            );
        }
        
        // Handle knockback if enabled
        if (EnablePushBack)
        {
            if (target.GetComponent<IKnockbackable>() != null)
            {
                CombatEventManager.TriggerKnockback(
                    Instigator,
                    target,
                    HitDirection,
                    PushBackDistance,
                    PushBackDuration,
                    PushBackCurve
                );
            }
        }
        
        // Handle stun if enabled and damage is above threshold
        if (EnableStun && Damage >= StunThresholdDamage)
        {
            if (target.GetComponent<IStunnable>() != null)
            {
                CombatEventManager.TriggerStun(
                    Instigator,
                    target,
                    StunDuration,
                    Damage
                );
            }
        }
    }
    
    /// <summary>
    /// Logs debug information about this hit context
    /// </summary>
    public void LogDebugInfo(string caller)
    {
        Debug.Log($"<color=blue>HitContext Debug from {caller}:</color>\n" +
                  $"- EnablePushBack: {EnablePushBack}\n" +
                  $"- PushBackDistance: {PushBackDistance}\n" +
                  $"- PushBackDuration: {PushBackDuration}\n" +
                  $"- EnableStun: {EnableStun}\n" +
                  $"- StunThresholdDamage: {StunThresholdDamage}\n" +
                  $"- StunDuration: {StunDuration}\n" +
                  $"- Damage: {Damage}\n" +
                  $"- Instigator: {(Instigator != null ? Instigator.name : "null")}");
    }
} 