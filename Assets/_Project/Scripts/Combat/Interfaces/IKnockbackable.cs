using System;
using UnityEngine;

/// <summary>
/// Interface for entities that can be knocked back
/// </summary>
public interface IKnockbackable
{
    /// <summary>
    /// Whether the entity is currently being pushed back
    /// </summary>
    bool IsPushedBack { get; }
    
    /// <summary>
    /// Apply knockback to the entity
    /// </summary>
    /// <param name="direction">Direction of the knockback</param>
    /// <param name="distance">Distance to knock back</param>
    /// <param name="duration">Duration of the knockback movement</param>
    /// <param name="curve">Optional animation curve for the knockback movement</param>
    /// <returns>True if knockback successfully applied</returns>
    bool ApplyKnockback(Vector3 direction, float distance, float duration, AnimationCurve curve = null);
    
    /// <summary>
    /// Apply death knockback with enhanced effect
    /// </summary>
    /// <param name="direction">Direction of the knockback</param>
    /// <param name="distance">Base distance to knock back</param>
    /// <param name="duration">Duration of the knockback movement</param>
    /// <param name="curve">Optional animation curve for the knockback movement</param>
    /// <returns>True if knockback successfully applied</returns>
    bool ApplyDeathKnockback(Vector3 direction, float distance, float duration, AnimationCurve curve = null);
    
    /// <summary>
    /// Event fired when knockback begins
    /// </summary>
    event Action<GameObject> OnKnockbackStart;
    
    /// <summary>
    /// Event fired when knockback ends
    /// </summary>
    event Action<GameObject> OnKnockbackEnd;
    
    /// <summary>
    /// Handler for knockback events from the combat event system
    /// </summary>
    /// <param name="context">Knockback event context</param>
    void HandleKnockbackEvent(CombatEventManager.KnockbackEventContext context);
} 