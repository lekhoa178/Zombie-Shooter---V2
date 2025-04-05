// Core interface all enemies must implement

using System;
using System.Collections.Generic;
using UnityEngine;

public interface IEnemy
{
    // Identity & State
    EnemyData Data { get; }
    Transform Transform { get; }
    GameObject GameObject { get; }
    
    // Health & Combat States
    float CurrentHealth { get; }
    float MaxHealth { get; }
    bool IsDead { get; }
    bool IsStunned { get; }
    bool IsInvulnerable { get; }
    bool IsElite { get; }
    
    // Events for broadcasting state changes
    event Action<IEnemy> OnDeath;
    event Action<IHittable, float, Vector3> OnDamaged;
    event Action<IEnemy> OnStunBegin;
    event Action<IEnemy> OnStunEnd;
    event Action<IEnemy> OnPushbackStart;
    event Action<IEnemy> OnPushbackEnd;
    event Action<IEnemy, float> OnHealthChanged; 
    
    // Elite functionality
    void SetEliteStatus(bool elite);
}

// Interface for enemies that can be hit
public interface IHittable
{
    event Action<IHittable, float, Vector3> OnDamaged;

    // Called when the enemy gets hit
    void OnHit(Vector3 direction, float damage);
    
    void OnHitWithContext(HitContext hitContext);
    
    // For directional effects
    void OnHitFromDirection(Vector3 hitPosition, Vector3 hitDirection, float damage);
    
    // For specific hit reactions
    void ReactToHit(HitReactionType reactionType, Vector3 direction);
}

// Interface for enemies that can attack
public interface IAttacker
{
    bool CanAttack { get; }
    float AttackRange { get; }
    float AttackCooldown { get; }
    
    // Execute attack
    void PerformAttack(Transform target);
    
    // Check if can attack target
    bool IsTargetInAttackRange(Transform target);
    
    // Events
    event Action<IAttacker> OnAttackStart;
    event Action<IAttacker> OnAttackEnd;
}

// Interface for AI movement control
public interface IAIMovement
{
    void MoveTo(Vector3 position, float speed);
    void RotateTowards(Vector3 target, float rotationSpeed);
    void StopMovement();
    bool IsMoving { get; }
    float CurrentMoveSpeed { get; }
}