using System;
using UnityEngine;

/// <summary>
/// Interface for entities that can be stunned
/// </summary>
public interface IStunnable
{
    /// <summary>
    /// Whether the entity is currently stunned
    /// </summary>
    bool IsStunned { get; }
    
    /// <summary>
    /// Whether the entity is in stunned recovery state
    /// </summary>
    bool IsInStunRecovery { get; }
    
    /// <summary>
    /// Apply a stun effect to the entity
    /// </summary>
    /// <param name="duration">Duration of the stun in seconds</param>
    /// <returns>True if stun was successfully applied, false if resisted</returns>
    bool ApplyStun(float duration);
    
    /// <summary>
    /// Forcibly end the stunned state
    /// </summary>
    void EndStun();
    
    /// <summary>
    /// Event fired when entity becomes stunned
    /// </summary>
    event Action<GameObject> OnStunBegin;
    
    /// <summary>
    /// Event fired when entity is no longer stunned
    /// </summary>
    event Action<GameObject> OnStunEnd;
    
    /// <summary>
    /// Whether entity can be stunned currently (not in recovery, not already stunned)
    /// </summary>
    bool CanBeStunned { get; }
    
    /// <summary>
    /// Handler for stun events from the combat event system
    /// </summary>
    /// <param name="context">Stun event context</param>
    void HandleStunEvent(CombatEventManager.StunEventContext context);
} 