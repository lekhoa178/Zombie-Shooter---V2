using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Attack", story: "[Agent] Melee Attacks [Target]", category: "Action", id: "7da4a5255a6bc5dd4f976ed89e5abb5c")]
public partial class MeleeAttackAction : Action
{
    // Core references
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    
    // Attack configuration
    [SerializeReference] public BlackboardVariable<bool> FaceTargetBeforeAttack;
    
    // Output variables
    [SerializeReference] public BlackboardVariable<bool> AttackSuccessful;
    [SerializeReference] public BlackboardVariable<bool> TargetInRange;
    [SerializeReference] public BlackboardVariable<bool> IsAttacking;
    
    // Private state
    private IAttacker attacker;
    private BaseEnemy enemyComponent;
    private bool isWaitingForAttackToComplete = false;
    private bool attackStarted = false;
    private bool attackCompleted = false;
    
    // Event handlers
    private Action<IAttacker> attackStartHandler;
    private Action<IAttacker> attackEndHandler;

    // Add these fields to the class
    [SerializeReference] public BlackboardVariable<float> AttackTimeout;
    private float attackStartTime;
    private float defaultAttackTimeout = 2f;

    protected override Status OnStart()
    {
        // Validate agent
        if (Agent?.Value == null)
        {
            Debug.LogError("Agent is null in Attack Action");
            return Status.Failure;
        }

        // Get required components
        attacker = Agent.Value.GetComponent<IAttacker>();
        enemyComponent = Agent.Value.GetComponent<BaseEnemy>();
        
        if (attacker == null)
        {
            Debug.LogError("Agent does not implement IAttacker interface");
            return Status.Failure;
        }
        
        if (enemyComponent == null)
        {
            Debug.LogWarning("Agent does not inherit from BaseEnemy");
        }
        
        if (Target?.Value == null)
        {
            Debug.LogWarning("No target provided for attack action");
            return Status.Failure;
        }
        
        if (FaceTargetBeforeAttack?.Value == null && FaceTargetBeforeAttack != null)
        {
            FaceTargetBeforeAttack.Value = true;
        }
        
        if (IsAttacking != null)
        {
            IsAttacking.Value = false;
        }
        
        if (TargetInRange != null)
        {
            TargetInRange.Value = attacker.IsTargetInAttackRange(Target.Value.transform);
        }
        
        attackStartHandler = OnAttackStarted;
        attackEndHandler = OnAttackEnded;
        
        attacker.OnAttackStart += attackStartHandler;
        attacker.OnAttackEnd += attackEndHandler;
        
        attackStarted = false;
        isWaitingForAttackToComplete = false;
        
        if (AttackTimeout?.Value == null && AttackTimeout != null)
        {
            AttackTimeout.Value = defaultAttackTimeout;
        }
        
        attackStartTime = -1f;
        
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Agent?.Value == null || attacker == null || Target?.Value == null)
        {
            CleanupAttack(false);
            return Status.Success;
        }
        
        Debug.Log($"IsStunned: {enemyComponent.IsStunned}, IsDead: {enemyComponent.IsDead}, IsCanAttack: {attacker.CanAttack}, IsTargetInRange: {attacker.IsTargetInAttackRange(Target.Value.transform)}");
        
        if (enemyComponent != null && (enemyComponent.IsDead || enemyComponent.IsStunned))
        {
            CleanupAttack(false);
            return Status.Failure;
        }
        
        // Check if attack was completed via event callback
        if (attackCompleted)
        {
            CleanupAttack(true);
            return Status.Success;
        }
        
        // Check for attack timeout
        if (isWaitingForAttackToComplete && attackStartTime > 0)
        {
            var timeout = AttackTimeout?.Value ?? defaultAttackTimeout;
            if (Time.time - attackStartTime > timeout)
            {
                Debug.LogWarning($"Attack timed out for {Agent.Value.name}");
                CleanupAttack(false);
                return Status.Failure;
            }
        }
        
        var inRange = attacker.IsTargetInAttackRange(Target.Value.transform);
        if (TargetInRange != null)
        {
            TargetInRange.Value = inRange;
        }
        
        if (isWaitingForAttackToComplete)
        {
            return Status.Running;
        }
        
        // if (!inRange)
        // {
        //     CleanupAttack(false);
        //     return Status.Failure;
        // }
        
        if (!attacker.CanAttack)
        {
            CleanupAttack(true);
            return Status.Success;
        }
        
        if (FaceTargetBeforeAttack?.Value ?? true)
        {
            FaceTarget();
        }
        
        if (attackStarted) return Status.Running;
        
        if (IsAttacking != null)
        {
            IsAttacking.Value = true;
        }
            
        attackStartTime = Time.time;
            
        attacker.PerformAttack(Target.Value.transform);
            
        attackStarted = true;
        isWaitingForAttackToComplete = true;
            
        return Status.Running;

    }

    protected override void OnEnd()
    {
        CleanupAttack(false);
    }
    
    private void CleanupAttack(bool wasSuccessful)
    {
        // Update state variables
        if (IsAttacking != null)
        {
            IsAttacking.Value = false;
        }
        
        if (AttackSuccessful != null)
        {
            AttackSuccessful.Value = wasSuccessful;
        }
        
        // Unsubscribe from events if we have an attacker
        if (attacker != null)
        {
            attacker.OnAttackStart -= attackStartHandler;
            attacker.OnAttackEnd -= attackEndHandler;
        }
        
        // Reset all state flags
        attackStarted = false;
        isWaitingForAttackToComplete = false;
        attackCompleted = false;
        attackStartTime = -1f;
    }
    
    private void OnAttackStarted(IAttacker source)
    {
        // Attack has officially started
        Debug.Log($"Attack started by {Agent?.Value?.name}");
    }
    
    private void OnAttackEnded(IAttacker source)
    {
        attackCompleted = true;
    }
    
    private void FaceTarget()
    {
        if (Agent?.Value == null || Target?.Value == null) return;
        
        Vector3 direction = Target.Value.transform.position - Agent.Value.transform.position;
        direction.y = 0;
        
        if (direction != Vector3.zero)
        {
            Agent.Value.transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}

