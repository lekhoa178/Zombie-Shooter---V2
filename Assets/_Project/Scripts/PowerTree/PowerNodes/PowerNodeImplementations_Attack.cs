using System.Collections;
using System.Collections.Generic;
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
        
        private float currentBoost = 0f;
        private bool boostApplied = false;
        
        private void OnValidate()
        {
            // Auto-populate descriptions with field values
            descriptions = new string[]
            {
                "Increase damage by {damageBoost[0]} when above {healthThreshold} health",
                "Increases to {damageBoost[1]}",
                "Increases to {damageBoost[2]}"
            };
        }
        
        public override void ApplyEffect(PlayerStats player, int level)
        {
            if (level <= 0 || level > damageBoost.Length) return;
            
            currentBoost = damageBoost[level - 1];
            
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
            if (boostApplied)
            {
                player.ModifyDamageMultiplier(-currentBoost);
                boostApplied = false;
            }
            
            ResetState();
        }
        
        private void CheckAndApplyDamageIfNeeded(PlayerStats player)
        {
            // Get the configured threshold for this node
            float threshold = player.GetNodeProperty(nodeID, "healthThreshold", healthThreshold);
            bool isHighHealth = player.IsHighHealth(threshold);
            
            if (isHighHealth && !boostApplied)
            {
                // Apply damage boost
                player.ModifyDamageMultiplier(currentBoost);
                boostApplied = true;
            }
            else if (!isHighHealth && boostApplied)
            {
                // Remove damage boost
                player.ModifyDamageMultiplier(-currentBoost);
                boostApplied = false;
            }
        }

        public override void ResetState()
        {
            currentBoost = 0f;
            boostApplied = false;
        }
    }

    /// <summary>
    /// 2. Rage Destruction: Damage boost when losing red hearts
    /// </summary>
    [CreateAssetMenu(fileName = "RageDestruction", menuName = "PowerTree/Nodes/Attack/RageDestruction")]
    public class RageDestructionNode : PowerNode
    {
        [SerializeField] private float[] damageBoost = new float[] { 0.15f, 0.25f, 0.35f }; // 15%, 25%, 35%
        [SerializeField] private float[] boostDuration = new float[] { 5f, 5f, 7f }; // 5s, 5s, 7s
        
        private float currentBoost = 0f;
        private float currentDuration = 0f;
        
        private void OnValidate()
        {
            // Auto-populate descriptions with field values
            descriptions = new string[]
            {
                "When losing a red heart, increase damage by {damageBoost[0]} for {boostDuration[0]} seconds",
                "Increases to {damageBoost[1]}",
                "Increases to {damageBoost[2]} and lasts for {boostDuration[2]} seconds"
            };
        }
        
        public override void ApplyEffect(PlayerStats player, int level)
        {
            if (level <= 0 || level > damageBoost.Length) return;
            
            currentBoost = damageBoost[level - 1];
            currentDuration = boostDuration[level - 1];
            
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
            currentBoost = 0f;
            currentDuration = 0f;
        }
        
        private void OnRedHeartLost(float amount)
        {
            var player = FindObjectOfType<PlayerStats>();
            if (player != null)
            {
                // Apply temporary damage boost
                player.ModifyDamageMultiplier(currentBoost, currentDuration);
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
        
        private float currentSpeedBoost = 0f;
        private bool hasDoubleDamageChance = false;
        
        private void OnValidate()
        {
            // Auto-populate descriptions with field values
            descriptions = new string[]
            {
                "Attack speed increases by {attackSpeedBoost[0]}",
                "Increases to {attackSpeedBoost[1]}",
                "Increases to {attackSpeedBoost[2]} and {doubleDamageChance} chance for attacks to deal double damage"
            };
        }
        
        public override void ApplyEffect(PlayerStats player, int level)
        {
            if (level <= 0 || level > attackSpeedBoost.Length) return;
            
            // Remove previous effect if any
            if (currentSpeedBoost > 0)
            {
                player.ModifyAttackSpeedMultiplier(-currentSpeedBoost);
            }
            
            // Apply new effect
            currentSpeedBoost = attackSpeedBoost[level - 1];
            hasDoubleDamageChance = (level == 3); // Only level 3 has double damage chance
            
            // Apply attack speed boost (permanent)
            player.ModifyAttackSpeedMultiplier(currentSpeedBoost);
            
            // For level 3, set up double damage chance
            if (hasDoubleDamageChance)
            {
                // This would need to be implemented in the attack/weapon system
                // Typically through a hook in the damage calculation
                WeaponSystem.AddDoubleDamageChance(doubleDamageChance);
            }
        }
        
        public override void RemoveEffect(PlayerStats player)
        {
            // Remove attack speed boost
            player.ModifyAttackSpeedMultiplier(-currentSpeedBoost);
            
            // Remove double damage chance if active
            if (hasDoubleDamageChance)
            {
                WeaponSystem.RemoveDoubleDamageChance(doubleDamageChance);
            }
            
            ResetState();
        }

        public override void ResetState()
        {
            currentSpeedBoost = 0f;
            hasDoubleDamageChance = false;
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
        [SerializeField] private float boostDuration = 3f; // 3 seconds
        [SerializeField] private int maxStacks = 1; // Level 3 can stack twice (2)
        
        private float currentProcChance = 0f;
        private float currentSpeedBoost = 0f;
        private int currentMaxStacks = 1;
        
        private void OnValidate()
        {
            // Auto-populate descriptions with field values
            descriptions = new string[]
            {
                "{procChance[0]} chance to increase attack speed by {speedBoost[0]} for {boostDuration} seconds when hitting",
                "Increases to {procChance[1]} chance, {speedBoost[1]} attack speed",
                "Increases to {procChance[2]} chance, {speedBoost[2]} attack speed, can stack twice"
            };
        }
        
        public override void ApplyEffect(PlayerStats player, int level)
        {
            if (level <= 0 || level > procChance.Length) return;
            
            currentProcChance = procChance[level - 1];
            currentSpeedBoost = speedBoost[level - 1];
            currentMaxStacks = (level == 3) ? 2 : 1; // Level 3 can stack twice
            
            // Subscribe to enemy hit event
            WeaponSystem.OnEnemyHit.AddListener(() => OnEnemyHit(player));
        }
        
        public override void RemoveEffect(PlayerStats player)
        {
            WeaponSystem.OnEnemyHit.RemoveListener(() => OnEnemyHit(player));
            
            ResetState();
        }

        public override void ResetState()
        {
            currentMaxStacks = 1;
            currentProcChance = 0f;
            currentSpeedBoost = 0f;
        }
        
        private void OnEnemyHit(PlayerStats player)
        {
            // Check if we proc the effect
            if (UnityEngine.Random.value < currentProcChance)
            {
                // Apply temporary attack speed boost with stacking
                player.ModifyAttackSpeedMultiplier(currentSpeedBoost, boostDuration, canStack: (currentMaxStacks > 1));
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
        
        private float currentAttackSpeedBoost = 0f;
        private float currentDamageBoost = 0f;
        private int currentMaxStacks = 1;
        private Counter killCounter;
        private Timer killTimer;
        
        private void OnValidate()
        {
            // Auto-populate descriptions with field values
            descriptions = new string[]
            {
                "When defeating {requiredKills} enemies within {killWindowDuration} seconds, gain {attackSpeedBoost[0]} attack speed for {buffDuration} seconds",
                "Buff increases to {attackSpeedBoost[1]} attack speed and {damageBoost[1]} damage",
                "Buff can stack twice, up to {attackSpeedBoost[2]} attack speed and {damageBoost[2]} damage"
            };
        }
        
        public override void ApplyEffect(PlayerStats player, int level)
        {
            if (level <= 0 || level > attackSpeedBoost.Length) return;
            
            currentAttackSpeedBoost = attackSpeedBoost[level - 1];
            currentDamageBoost = damageBoost[level - 1];
            currentMaxStacks = (level == 3) ? 2 : 1; // Level 3 can stack twice
            
            // Initialize counter and timer
            killCounter = player.GetCounter(nodeID, "killCounter");
            killCounter.MaxValue = requiredKills;
            killCounter.OnMaxValueReached += () => ApplyBuffs(player);
            
            killTimer = player.GetTimer(nodeID, "killTimer", killWindowDuration);
            killTimer.OnTimerCompleted += () => killCounter.Reset();
            
            // Subscribe to enemy killed event
            player.OnEnemyKilled.AddListener(OnEnemyKilled);
        }
        
        public override void RemoveEffect(PlayerStats player)
        {
            player.OnEnemyKilled.RemoveListener(OnEnemyKilled);
            
            ResetState();
        }

        public override void ResetState()
        {
            currentMaxStacks = 1;
            currentAttackSpeedBoost = 0f;
            currentDamageBoost = 0f;
            killCounter?.Reset();
            killTimer?.Reset();
        }
        
        private void OnEnemyKilled()
        {
            // Increment kill counter and start/reset timer
            killCounter.Increment();
            
            if (!killTimer.IsRunning)
            {
                killTimer.Start();
            }
            else
            {
                killTimer.Reset();
            }
        }
        
        private void ApplyBuffs(PlayerStats player)
        {
            // Apply temporary attack speed boost
            player.ModifyAttackSpeedMultiplier(currentAttackSpeedBoost, buffDuration, canStack: (currentMaxStacks > 1));
            
            // Apply temporary damage boost if available
            if (currentDamageBoost > 0)
            {
                player.ModifyDamageMultiplier(currentDamageBoost, buffDuration);
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
        
        private float currentDamageBoost = 0f;
        
        private void OnValidate()
        {
            // Auto-populate descriptions with field values
            descriptions = new string[]
            {
                "Damage increases by {damageBoost[0]} if enemy has above {healthThreshold} health",
                "Increases to {damageBoost[1]}",
                "Increases to {damageBoost[2]}"
            };
        }
        
        public override void ApplyEffect(PlayerStats player, int level)
        {
            if (level <= 0 || level > damageBoost.Length) return;
            
            currentDamageBoost = damageBoost[level - 1];
            
            // Subscribe to damage calculation event
            // This implementation is hypothetical and depends on your damage system
            WeaponSystem.OnCalculateDamage += OnCalculateDamage;
        }
        
        public override void RemoveEffect(PlayerStats player)
        {
            WeaponSystem.OnCalculateDamage -= OnCalculateDamage;
            
            ResetState();
        }

        public override void ResetState()
        {
            currentDamageBoost = 0f;
        }
        
        private void OnCalculateDamage(Enemy target, ref float damage)
        {
            // Check if enemy health is above threshold
            if (target.GetHealthPercent() > healthThreshold)
            {
                // Increase damage
                damage *= (1f + currentDamageBoost);
            }
        }
    }

    #endregion
}

// Helper stubs to make the code compile - these would be implemented in your game
public static class WeaponSystem
{
    public static UnityEngine.Events.UnityEvent OnEnemyHit = new UnityEngine.Events.UnityEvent();
    public static event CalculateDamageHandler OnCalculateDamage;

    public delegate void CalculateDamageHandler(Enemy target, ref float damage);
    
    public static void AddDoubleDamageChance(float chance) { }
    public static void RemoveDoubleDamageChance(float chance) { }
}

public static class EnemyManager
{
    public static void ApplyKnockback(Vector2 position, float strength) { }
    public static void ApplyAreaDamage(Vector2 position, float radius, float damage) { }
    public static void PushEnemiesAway(Vector2 position, float range) { }
    public static void DamageAllEnemies(float damagePercent, bool excludeBosses) { }
}

public static class BulletManager
{
    public static void NegateNearbyBullets(Vector2 position, int count) { }
}

public class Enemy
{
    public float GetHealthPercent() { return 1.0f; }
} 