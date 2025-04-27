using System.Collections.Generic;
using System.Linq;
using LocomotionSystem;
using UnityEngine;

namespace PowerTree
{
    #region Attack Branch Nodes

    /// <summary>
    /// 1. Wild Strength: Increase damage when above 50% health
    /// </summary>
    [CreateAssetMenu(fileName = "WildStrength", menuName = "PowerTree/Nodes/Attack/WildStrength")]
    public class WildStrengthNode : PowerNode
    {
        [SerializeField] private float[] damageBoost = new float[] { 0.1f, 0.2f, 0.3f }; // 10%, 20%, 30%
        [SerializeField] private float healthThreshold = 0.5f; // 50% of max health
        
        private float _currentBoost;
        private bool _boostApplied;
        
        private void OnValidate()
        {
            descriptions = new[]
            {
                "Increase damage by {damageBoost[0]} when above {healthThreshold} health",
                "Increases to {damageBoost[1]}",
                "Increases to {damageBoost[2]}"
            };
        }
        
        public override void ApplyEffect(PlayerStats player, int level)
        {
            if (level <= 0 || level > damageBoost.Length) return;
            
            _currentBoost = damageBoost[level - 1];
            
            // Save node-specific configuration
            player.SetNodeProperty(nodeID, "healthThreshold", healthThreshold);
            
            // Check and apply initial state
            CheckAndApplyDamageIfNeeded(player);
            
            // Subscribe to heart changes to update damage boost
            player.OnRedHeartLost.AddListener((_) => CheckAndApplyDamageIfNeeded(player));
            player.OnRedHeartGained.AddListener((_) => CheckAndApplyDamageIfNeeded(player));
        }
        
        public override void RemoveEffect(PlayerStats player)
        {
            player.OnRedHeartLost.RemoveListener((_) => CheckAndApplyDamageIfNeeded(player));
            player.OnRedHeartGained.RemoveListener((_) => CheckAndApplyDamageIfNeeded(player));
            
            // Remove boost if applied
            if (_boostApplied)
            {
                player.ModifyDamageMultiplier(-_currentBoost);
                _boostApplied = false;
            }
            
            ResetState();
        }
        
        private void CheckAndApplyDamageIfNeeded(PlayerStats player)
        {
            // Get the configured threshold for this node
            float threshold = player.GetNodeProperty(nodeID, "healthThreshold", healthThreshold);
            bool isHighHealth = player.IsHighHealth(threshold);
            
            if (isHighHealth && !_boostApplied)
            {
                // Apply damage boost
                player.ModifyDamageMultiplier(_currentBoost);
                _boostApplied = true;
            }
            else if (!isHighHealth && _boostApplied)
            {
                // Remove damage boost
                player.ModifyDamageMultiplier(-_currentBoost);
                _boostApplied = false;
            }
        }

        public override void ResetState()
        {
            _currentBoost = 0f;
            _boostApplied = false;
        }
    }

    /// <summary>
    /// 2. Rage Destruction: Damage boost when losing red hearts
    /// </summary>
    [CreateAssetMenu(fileName = "RageDestruction", menuName = "PowerTree/Nodes/Attack/RageDestruction")]
    public class RageDestructionNode : PowerNode
    {
        [SerializeField] private float[] damageBoost = new float[] { 0.15f, 0.25f, 0.35f }; // 15%, 25%, 35%
        [SerializeField] private float[] effectDuration = new float[] { 5f, 5f, 7f }; // 5s, 5s, 7s
        
        private float _currentBoost;
        private float _currentDuration;
        
        private void OnValidate()
        {
            descriptions = new[]
            {
                "When losing a red heart, increase damage by {damageBoost[0]} for {effectDuration[0]} seconds",
                "Increases to {damageBoost[1]}",
                "Increases to {damageBoost[2]} and lasts for {effectDuration[2]} seconds"
            };
        }
        
        public override void ApplyEffect(PlayerStats player, int level)
        {
            if (level <= 0 || level > damageBoost.Length) return;
            
            _currentBoost = damageBoost[level - 1];
            _currentDuration = effectDuration[level - 1];
            
            // Subscribe to red heart lost event
            player.OnRedHeartLost.AddListener(OnRedHeartLost);
        }
        
        public override void RemoveEffect(PlayerStats player)
        {
            player.OnRedHeartLost.RemoveListener(OnRedHeartLost);
            
            ResetState();
        }

        public override void ResetState()
        {
            _currentBoost = 0f;
            _currentDuration = 0f;
        }
        
        private void OnRedHeartLost(float amount)
        {
            var player = FindAnyObjectByType<PlayerStats>();
            if (player != null)
            {
                // Apply temporary damage boost
                player.ModifyDamageMultiplier(_currentBoost, _currentDuration);
            }
        }
    }

    /// <summary>
    /// 3. Lightning Strike: Increase attack speed and chance for double damage
    /// </summary>
    [CreateAssetMenu(fileName = "LightningStrike", menuName = "PowerTree/Nodes/Attack/LightningStrike")]
    public class LightningStrikeNode : PowerNode
    {
        [SerializeField] private float[] attackSpeedBoost = new float[] { 0.1f, 0.2f, 0.3f }; // 10%, 20%, 30%
        [SerializeField] private float doubleDamageChance = 0.05f; // 5% for level 3
        
        private float _currentSpeedBoost;
        private bool _hasDoubleDamageChance;
        
        private void OnValidate()
        {
            descriptions = new[]
            {
                "Attack speed increases by {attackSpeedBoost[0]}",
                "Increases to {attackSpeedBoost[1]}",
                "Increases to {attackSpeedBoost[2]}"
                // "Increases to {attackSpeedBoost[2]} and {doubleDamageChance} chance for attacks to deal double damage"
            };
        }
        
        public override void ApplyEffect(PlayerStats player, int level)
        {
            if (level <= 0 || level > attackSpeedBoost.Length) return;
            
            // Remove previous effect if any
            if (_currentSpeedBoost > 0)
            {
                player.ModifyAttackSpeedMultiplier(-_currentSpeedBoost);
            }
            
            // Apply new effect
            _currentSpeedBoost = attackSpeedBoost[level - 1];
            _hasDoubleDamageChance = (level == 3);
            
            player.ModifyAttackSpeedMultiplier(_currentSpeedBoost);
            
            // if (hasDoubleDamageChance)
            // {
            //     WeaponSystem.AddDoubleDamageChance(doubleDamageChance);
            // }
        }
        
        public override void RemoveEffect(PlayerStats player)
        {
            player.ModifyAttackSpeedMultiplier(-_currentSpeedBoost);
            
            // if (hasDoubleDamageChance)
            // {
            //     WeaponSystem.RemoveDoubleDamageChance(doubleDamageChance);
            // }
            
            ResetState();
        }

        public override void ResetState()
        {
            _currentSpeedBoost = 0f;
            _hasDoubleDamageChance = false;
        }
    }

    /// <summary>
    /// 4. Berserk: Chance to increase attack speed when hitting enemies
    /// </summary>
    [CreateAssetMenu(fileName = "Berserk", menuName = "PowerTree/Nodes/Attack/Berserk")]
public class BerserkNode : PowerNode
{
    [SerializeField] private float[] procChance = new float[] { 0.05f, 0.1f, 0.15f }; // 5%, 10%, 15%
    [SerializeField] private float[] speedBoost = new float[] { 0.2f, 0.3f, 0.4f }; // 20%, 30%, 40%
    [SerializeField] private float effectDuration = 3f; // 3 seconds
    
    private float _currentProcChance;
    private float _currentSpeedBoost;
    private int _currentMaxStacks = 1;
    
    private readonly List<Timer> _activeStackTimers = new();
    private int _stackCounter;
    
    private void OnValidate()
    {
        descriptions = new[]
        {
            "{procChance[0]} chance to increase attack speed by {speedBoost[0]} for {effectDuration} seconds when hitting",
            "Increases to {procChance[1]} chance, {speedBoost[1]} attack speed",
            "Increases to {procChance[2]} chance, {speedBoost[2]} attack speed, can stack twice"
        };
    }
    
    public override void ApplyEffect(PlayerStats player, int level)
    {
        if (level <= 0 || level > procChance.Length) return;
        
        _currentProcChance = procChance[level - 1];
        _currentSpeedBoost = speedBoost[level - 1];
        _currentMaxStacks = (level == 3) ? 2 : 1; // Level 3 can stack twice
        
        CombatEventManager.OnHit += OnDamaged;
    }

    public override void RemoveEffect(PlayerStats player)
    {
        CombatEventManager.OnHit -= OnDamaged;
        
        // Clean up all active timers
        foreach (var timer in _activeStackTimers.ToList())
        {
            timer.Reset();
        }
        
        _activeStackTimers.Clear();
        
        ResetState();
    }

    public override void ResetState()
    {
        _currentMaxStacks = 1;
        _currentProcChance = 0f;
        _currentSpeedBoost = 0f;
        _activeStackTimers.Clear();
        _stackCounter = 0;
    }
    
    private void OnDamaged(CombatEventManager.HitEventContext obj)
    {
        var target = obj.Target;
        var source = obj.Source;

        var enemy = target.GetComponent<BaseEnemy>();
        
        if (!enemy) return;

        var player = source.GetComponent<PlayerStats>();
        if (!player) return;

        if (_activeStackTimers.Count >= _currentMaxStacks) return;

        if (Random.value < _currentProcChance)
        {
            // Apply the speed boost
            player.ModifyAttackSpeedMultiplier(_currentSpeedBoost, effectDuration, canStack: (_currentMaxStacks > 1));
            
            _stackCounter++;
            var stackTimer = player.GetTimer(nodeID, $"stackTimer{_stackCounter}", effectDuration);
            
            var timerRef = stackTimer;
            
            stackTimer.OnTimerCompleted += () => 
            {
                _activeStackTimers.Remove(timerRef);
                _stackCounter = Mathf.Max(0, _stackCounter - 1);
            };
            
            _activeStackTimers.Add(stackTimer);
            stackTimer.Start();
        }
    }
}

    /// <summary>
    /// 5. Bloodthirsty Combat: Combat buffs for killing enemies quickly
    /// </summary>
    [CreateAssetMenu(fileName = "BloodthirstyCombat", menuName = "PowerTree/Nodes/Attack/BloodthirstyCombat")]
    public class BloodthirstyCombatNode : PowerNode
    {
        [SerializeField] private float[] attackSpeedBoost = new float[] { 0.2f, 0.25f, 0.3f }; // 20%, 25%, 30%
        [SerializeField] private float[] damageBoost = new float[] { 0f, 0.05f, 0.1f }; // 0%, 5%, 10%
        [SerializeField] private float buffDuration = 5f; // 5 seconds
        [SerializeField] private float killWindowDuration = 5f; // Time window for consecutive kills
        [SerializeField] private int requiredKills = 3; // 3 kills within time window
        [SerializeField] private int maxStacks = 1; // Level 3 can stack twice (2)
        
        private float _currentAttackSpeedBoost;
        private float _currentDamageBoost;
        private int _currentMaxStacks = 1;
        private Counter _killCounter;
        private Timer _killTimer;
        
        private void OnValidate()
        {
            descriptions = new[]
            {
                "When defeating {requiredKills} enemies within {killWindowDuration} seconds, gain {attackSpeedBoost[0]} attack speed for {buffDuration} seconds",
                "Buff increases to {attackSpeedBoost[1]} attack speed and {damageBoost[1]} damage",
                "Buff can stack twice, up to {attackSpeedBoost[2]} attack speed and {damageBoost[2]} damage"
            };
        }
        
        public override void ApplyEffect(PlayerStats player, int level)
        {
            if (level <= 0 || level > attackSpeedBoost.Length) return;
            
            _currentAttackSpeedBoost = attackSpeedBoost[level - 1];
            _currentDamageBoost = damageBoost[level - 1];
            _currentMaxStacks = (level == 3) ? 2 : 1;
            
            _killCounter = player.GetCounter(nodeID, "killCounter");
            _killCounter.MaxValue = requiredKills;
            _killCounter.OnMaxValueReached += () => ApplyBuffs(player);
            
            _killTimer = player.GetTimer(nodeID, "killTimer", killWindowDuration);
            _killTimer.OnTimerCompleted += () => _killCounter.Reset();
            
            CombatEventManager.OnDeath += OnEnemyKilled;
        }

        public override void RemoveEffect(PlayerStats player)
        {
            CombatEventManager.OnDeath -= OnEnemyKilled;
            
            ResetState();
        }

        public override void ResetState()
        {
            _currentMaxStacks = 1;
            _currentAttackSpeedBoost = 0f;
            _currentDamageBoost = 0f;
            _killCounter?.Reset();
            _killTimer?.Reset();
        }
        
        private void OnEnemyKilled(CombatEventManager.CombatEventContext obj)
        {
            var target = obj.Target;
            var source = obj.Source;

            var enemy = target.GetComponent<BaseEnemy>();
            
            if (!enemy) return;

            var player = source.GetComponent<PlayerStats>();
            if (!player) return;

            if (enemy.IsDead)
            {
                OnEnemyKilled();
            }
        }
        
        private void OnEnemyKilled()
        {
            _killCounter.Increment();
            
            if (!_killTimer.IsRunning)
            {
                _killTimer.Start();
            }
            else
            {
                _killTimer.Reset();
            }
        }
        
        private void ApplyBuffs(PlayerStats player)
        {
            player.ModifyAttackSpeedMultiplier(_currentAttackSpeedBoost, buffDuration, canStack: (_currentMaxStacks > 1));
            
            if (_currentDamageBoost > 0)
            {
                player.ModifyDamageMultiplier(_currentDamageBoost, buffDuration);
            }
        }
    }

    /// <summary>
    /// 6. Piercing Damage: Deal more damage to high-health enemies
    /// </summary>
    [CreateAssetMenu(fileName = "PiercingDamage", menuName = "PowerTree/Nodes/Attack/PiercingDamage")]
    public class PiercingDamageNode : PowerNode
    {
        [SerializeField] private float[] damageBoost = new float[] { 0.25f, 0.35f, 0.5f }; // 25%, 35%, 50%
        [SerializeField] private float healthThreshold = 0.75f; // 75% enemy health
        
        private float _currentDamageBoost;
        
        private void OnValidate()
        {
            descriptions = new[]
            {
                "Damage increases by {damageBoost[0]} if enemy has above {healthThreshold} health",
                "Increases to {damageBoost[1]}",
                "Increases to {damageBoost[2]}"
            };
        }
        
        public override void ApplyEffect(PlayerStats player, int level)
        {
            if (level <= 0 || level > damageBoost.Length) return;
            
            _currentDamageBoost = damageBoost[level - 1];
            
            // Subscribe to damage calculation event
            // This implementation is hypothetical and depends on your damage system
            WeaponManager.OnCalculateDamage += OnCalculateDamage;
        }
        
        public override void RemoveEffect(PlayerStats player)
        {
            WeaponManager.OnCalculateDamage -= OnCalculateDamage;
            
            ResetState();
        }

        public override void ResetState()
        {
            _currentDamageBoost = 0f;
        }
        
        private void OnCalculateDamage(BaseEnemy target, ref float damage)
        {
            // Check if enemy health is above threshold
            if (target.HealthPercentage > healthThreshold)
            {
                // Increase damage
                damage *= (1f + _currentDamageBoost);
            }
        }
    }

    #endregion
}

// Helper stubs to make the code compile - these would be implemented in your game
public static class WeaponSystem
{
    public static void AddDoubleDamageChance(float chance) { }
    public static void RemoveDoubleDamageChance(float chance) { }
}
