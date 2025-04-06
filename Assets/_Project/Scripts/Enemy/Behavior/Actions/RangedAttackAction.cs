using System;
using Flyweight;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "RangedAttack", story: "[Agent] Ranged Attacks [Target] with configurable bullet patterns", category: "Action", id: "5e6c6d8ebfa245782fe69e97552357d9")]
public partial class RangedAttackAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    
    [Header("Override Settings")]
    [Tooltip("When enabled, values from this action override RangedEnemyData values. When disabled, uses values directly from RangedEnemyData.")]
    [SerializeReference] public BlackboardVariable<bool> UseBlackboardOverrides;
    
    [Header("Fire Pattern")]
    [SerializeReference] public BlackboardVariable<int> ConsecutiveCount;
    [SerializeReference] public BlackboardVariable<float> ConsecutiveDelay;
    [SerializeReference] public BlackboardVariable<int> SimultaneousCount;
    [SerializeReference] public BlackboardVariable<float> SpreadAngle;
    [SerializeReference] public BlackboardVariable<bool> UseRandomSpread;
    [SerializeReference] public BlackboardVariable<float> RandomSpreadFactor;
    
    [Header("Aim Settings")]
    [SerializeReference] public BlackboardVariable<bool> LeadTarget;
    [SerializeReference] public BlackboardVariable<float> LeadFactor;
    [SerializeReference] public BlackboardVariable<float> DirectionVariance;

    [Header("Projectile Settings")] [SerializeReference]
    public BlackboardVariable<ProjectileSettings> projectileSettings;
    
    [Header("Attack Timeout")]
    [SerializeReference] public BlackboardVariable<float> AttackTimeout;
    
    // Internal state tracking
    private RangedEnemy _rangedEnemy;
    private bool _isAttacking = false;
    private bool _isCompleted = false;
    private float _attackStartTime = -1f;
    private const float _defaultAttackTimeout = 3f;
    
    // Store original values to restore later
    private int _savedConsecutiveCount;
    private int _savedSimultaneousCount;
    private bool _savedUseRandomSpread;
    private float _savedSpreadAngle;
    private bool _savedLeadTarget;
    private float _savedLeadFactor;
    private float _savedDirectionVariance;
    private ProjectileSettings _savedProjectileSettings;
    private bool _valuesOverridden = false;
    
    protected override Status OnStart()
    {
        // Validate required variables
        if (Agent?.Value == null)
        {
            Debug.LogError("RangedAttackAction requires an Agent");
            return Status.Failure;
        }
        
        if (Target?.Value == null)
        {
            Debug.LogError("RangedAttackAction requires a Target");
            return Status.Failure;
        }
        
        // Get the ranged enemy component
        _rangedEnemy = Agent.Value.GetComponent<RangedEnemy>();
        if (_rangedEnemy == null)
        {
            Debug.LogError("Agent does not have a RangedEnemy component");
            return Status.Failure;
        }

        if (!_rangedEnemy.CanAttack)
        {
            return Status.Success;
        }
        
        // Initialize defaults for unset variables
        if (UseBlackboardOverrides?.Value == null) UseBlackboardOverrides.Value = false;
        if (ConsecutiveCount?.Value == null) ConsecutiveCount.Value = 1;
        if (ConsecutiveDelay?.Value == null) ConsecutiveDelay.Value = 0.2f;
        if (SimultaneousCount?.Value == null) SimultaneousCount.Value = 1;
        if (SpreadAngle?.Value == null) SpreadAngle.Value = 15f;
        if (UseRandomSpread?.Value == null) UseRandomSpread.Value = false;
        if (RandomSpreadFactor?.Value == null) RandomSpreadFactor.Value = 0.5f;
        if (LeadTarget?.Value == null) LeadTarget.Value = true;
        if (LeadFactor?.Value == null) LeadFactor.Value = 0.5f;
        if (DirectionVariance?.Value == null) DirectionVariance.Value = 0f;
        if (AttackTimeout?.Value == null) AttackTimeout.Value = _defaultAttackTimeout;
        
        // Reset state
        _isAttacking = false;
        _isCompleted = false;
        _attackStartTime = -1f;
        _valuesOverridden = false;
        
        // Subscribe to ranged enemy events
        _rangedEnemy.OnAttackStart += OnRangedEnemyAttackStart;
        _rangedEnemy.OnAttackEnd += OnRangedEnemyAttackEnd;
        
        // Only override RangedEnemyData if UseBlackboardOverrides is enabled
        bool useOverrides = UseBlackboardOverrides.Value;
        
        // Set the attack parameters in RangedEnemyData if overrides are enabled
        RangedEnemyData rangedData = _rangedEnemy.Data as RangedEnemyData;
        if (rangedData != null && useOverrides)
        {
            // Save original values
            _savedConsecutiveCount = rangedData.consecutiveCount;
            _savedSimultaneousCount = rangedData.simultaneousCount;
            _savedUseRandomSpread = rangedData.useRandomSpread;
            _savedSpreadAngle = rangedData.spreadAngle;
            _savedLeadTarget = rangedData.leadTarget;
            _savedLeadFactor = rangedData.leadFactor;
            _savedDirectionVariance = rangedData.directionVariance;
            _savedProjectileSettings = rangedData.projectileSettings;
            _valuesOverridden = true;
            
            // Apply new values
            rangedData.consecutiveCount = ConsecutiveCount.Value;
            rangedData.simultaneousCount = SimultaneousCount.Value;
            rangedData.useRandomSpread = UseRandomSpread.Value;
            rangedData.spreadAngle = SpreadAngle.Value;
            rangedData.leadTarget = LeadTarget.Value;
            rangedData.leadFactor = LeadFactor.Value;
            rangedData.directionVariance = DirectionVariance.Value;
            if (projectileSettings?.Value != null)
                rangedData.projectileSettings = projectileSettings.Value;
            
            // Start the attack sequence
            _rangedEnemy.PerformAttack(Target.Value.transform);
            
            // Values will be restored when the action completes
        }
        else
        {
            _rangedEnemy.PerformAttack(Target.Value.transform);
        }
        
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        // Check if the attack has completed
        if (_isCompleted)
        {
            CleanupAttack(true);
            return Status.Success;
        }
        
        // Check for attack timeout
        if (_isAttacking && _attackStartTime > 0)
        {
            float timeout = AttackTimeout?.Value ?? _defaultAttackTimeout;
            if (Time.time - _attackStartTime > timeout)
            {
                Debug.LogWarning($"Ranged attack timed out for {Agent.Value.name}");
                CleanupAttack(false);
                return Status.Failure;
            }
        }
        
        return Status.Running;
    }

    protected override void OnEnd()
    {
        // Unsubscribe from events
        CleanupAttack(false);
    }
    
    private void CleanupAttack(bool wasSuccessful)
    {
        // Unsubscribe from events
        if (_rangedEnemy != null)
        {
            _rangedEnemy.OnAttackStart -= OnRangedEnemyAttackStart;
            _rangedEnemy.OnAttackEnd -= OnRangedEnemyAttackEnd;
            
            // Restore original values if they were overridden
            if (_valuesOverridden)
            {
                RangedEnemyData rangedData = _rangedEnemy.Data as RangedEnemyData;
                if (rangedData != null)
                {
                    rangedData.consecutiveCount = _savedConsecutiveCount;
                    rangedData.simultaneousCount = _savedSimultaneousCount;
                    rangedData.useRandomSpread = _savedUseRandomSpread;
                    rangedData.spreadAngle = _savedSpreadAngle;
                    rangedData.leadTarget = _savedLeadTarget;
                    rangedData.leadFactor = _savedLeadFactor;
                    rangedData.directionVariance = _savedDirectionVariance;
                    rangedData.projectileSettings = _savedProjectileSettings;
                }
                _valuesOverridden = false;
            }
        }
        
        // Reset state
        _isAttacking = false;
        _isCompleted = false;
        _attackStartTime = -1f;
    }
    
    private void OnRangedEnemyAttackStart(IAttacker attacker)
    {
        _isAttacking = true;
        _attackStartTime = Time.time;
        
        // Only use direct overrides if:
        // 1. UseBlackboardOverrides is true
        // 2. AND we couldn't modify the data in OnStart (not a RangedEnemyData)
        if (UseBlackboardOverrides.Value && _rangedEnemy.Data is not RangedEnemyData)
        {
            // Override the firing pattern directly
            _rangedEnemy.FireProjectilePattern(
                ConsecutiveCount.Value,
                SimultaneousCount.Value,
                UseRandomSpread.Value,
                SpreadAngle.Value
            );
        }
    }
    
    private void OnRangedEnemyAttackEnd(IAttacker attacker)
    {
        _isAttacking = false;
        _isCompleted = true;
    }
}

