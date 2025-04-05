using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PowerTree
{
    #region Survival Branch Nodes

    /// <summary>
    /// 1. Gentle Rebirth: Chance to not lose a red heart when it would be lost
    /// </summary>
    [CreateAssetMenu(fileName = "GentleRebirth", menuName = "PowerTree/Nodes/Survival/GentleRebirth")]
    public class GentleRebirthNode : PowerNode
    {
        [SerializeField] private float[] chanceValues = new float[] { 0.1f, 0.2f, 0.3f }; // 10%, 20%, 30%
        private float currentChance = 0f;
        
        public override void ApplyEffect(PlayerStats player, int level)
        {
            if (level <= 0 || level > chanceValues.Length) return;
            
            currentChance = chanceValues[level - 1];
            
            // Subscribe to red heart lost event
            player.OnRedHeartLost.AddListener(OnRedHeartLost);
        }
        
        public override void RemoveEffect(PlayerStats player)
        {
            player.OnRedHeartLost.RemoveListener(OnRedHeartLost);
            currentChance = 0f;
        }
        
        private void OnRedHeartLost(float amount)
        {
            // Check if we save the heart
            if (UnityEngine.Random.value < currentChance)
            {
                // Here we would implement giving back the heart
                // This depends on how the damage system is implemented
                // In a real implementation, you might want to have the event include
                // a bool reference that can be set to prevent the loss
                var player = FindObjectOfType<PlayerStats>();
                if (player != null)
                {
                    player.AddHearts(HeartType.Red, amount);
                }
            }
        }
    }

    /// <summary>
    /// 2. Strong Life Drop: Heal hearts after completing levels
    /// </summary>
    [CreateAssetMenu(fileName = "StrongLifeDrop", menuName = "PowerTree/Nodes/Survival/StrongLifeDrop")]
    public class StrongLifeDropNode : PowerNode
    {
        [SerializeField] private int[] levelsToHealRed = new int[] { 2, 1, 1 };
        [SerializeField] private int[] levelsToHealBlue = new int[] { 0, 0, 3 };
        
        private int levelCounter = 0;
        private int currentRedThreshold = 0;
        private int currentBlueThreshold = 0;
        
        public override void ApplyEffect(PlayerStats player, int level)
        {
            if (level <= 0 || level > levelsToHealRed.Length) return;
            
            currentRedThreshold = levelsToHealRed[level - 1];
            currentBlueThreshold = levelsToHealBlue[level - 1];
            
            // Subscribe to level completion event
            player.OnLevelCompleted.AddListener(() => OnLevelCompleted(player));
        }
        
        public override void RemoveEffect(PlayerStats player)
        {
            player.OnLevelCompleted.RemoveListener(() => OnLevelCompleted(player));
        }
        
        private void OnLevelCompleted(PlayerStats player)
        {
            levelCounter++;
            
            // Red heart healing
            if (currentRedThreshold > 0 && levelCounter % currentRedThreshold == 0)
            {
                player.AddHearts(HeartType.Red, 1f);
            }
            
            // Blue heart healing
            if (currentBlueThreshold > 0 && levelCounter % currentBlueThreshold == 0)
            {
                player.AddHearts(HeartType.Blue, 1f);
            }
        }
    }

    /// <summary>
    /// 3. Temporary Shield: Receive blue hearts at the start of each level
    /// </summary>
    [CreateAssetMenu(fileName = "TemporaryShield", menuName = "PowerTree/Nodes/Survival/TemporaryShield")]
    public class TemporaryShieldNode : PowerNode
    {
        [SerializeField] private float[] blueHeartsValues = new float[] { 2f, 3f, 3f };
        [SerializeField] private float[] nonLossChanceValues = new float[] { 0f, 0f, 0.1f };
        
        private float currentNonLossChance = 0f;
        
        public override void ApplyEffect(PlayerStats player, int level)
        {
            if (level <= 0 || level > blueHeartsValues.Length) return;
            
            // Give blue hearts at level start
            float heartsToAdd = blueHeartsValues[level - 1];
            player.AddHearts(HeartType.Blue, heartsToAdd);
            
            // Set up non-loss chance for level 3
            if (level == 3)
            {
                currentNonLossChance = nonLossChanceValues[level - 1];
                player.OnBlueHeartLost.AddListener(OnBlueHeartLost);
            }
            
            // Subscribe to level completion to add hearts each level
            player.OnLevelCompleted.AddListener(() => OnLevelCompleted(player, heartsToAdd));
        }
        
        public override void RemoveEffect(PlayerStats player)
        {
            player.OnBlueHeartLost.RemoveListener(OnBlueHeartLost);
            player.OnLevelCompleted.RemoveListener(delegate { OnLevelCompleted(player, 0); });
        }
        
        private void OnLevelCompleted(PlayerStats player, float heartsToAdd)
        {
            // Add blue hearts at the start of each new level
            player.AddHearts(HeartType.Blue, heartsToAdd);
        }
        
        private void OnBlueHeartLost(float amount)
        {
            // Check for non-loss chance
            if (UnityEngine.Random.value < currentNonLossChance)
            {
                // Implementation to prevent heart loss
                var player = FindObjectOfType<PlayerStats>();
                if (player != null)
                {
                    player.AddHearts(HeartType.Blue, amount);
                }
            }
        }
    }

    /// <summary>
    /// 4. Blood Transforms into Armor: Recover blue hearts and get damage boost
    /// </summary>
    [CreateAssetMenu(fileName = "BloodTransformsIntoArmor", menuName = "PowerTree/Nodes/Survival/BloodTransformsIntoArmor")]
    public class BloodTransformsIntoArmorNode : PowerNode
    {
        [SerializeField] private float[] blueHeartRecoveryChance = new float[] { 0.2f, 0.35f, 0.5f };
        [SerializeField] private float[] blackHeartDamageBoost = new float[] { 0.1f, 0.15f, 0.2f };
        [SerializeField] private float blackHeartDamageBoostDuration = 5f; // 5 seconds boost
        [SerializeField] private float[] knockbackStrength = new float[] { 1f, 1f, 2f }; // Level 3 has stronger knockback
        [SerializeField] private bool hasAreaDamage = false; // Level 3 has area damage
        
        private float currentRecoveryChance = 0f;
        private float currentDamageBoost = 0f;
        private float currentKnockback = 0f;
        
        public override void ApplyEffect(PlayerStats player, int level)
        {
            if (level <= 0 || level > blueHeartRecoveryChance.Length) return;
            
            currentRecoveryChance = blueHeartRecoveryChance[level - 1];
            currentDamageBoost = blackHeartDamageBoost[level - 1];
            currentKnockback = knockbackStrength[level - 1];
            hasAreaDamage = (level == 3);
            
            // Subscribe to blue heart lost event
            player.OnBlueHeartLost.AddListener(OnBlueHeartLost);
            
            // Subscribe to black heart lost event
            player.OnBlackHeartLost.AddListener(OnBlackHeartLost);
        }
        
        public override void RemoveEffect(PlayerStats player)
        {
            player.OnBlueHeartLost.RemoveListener(OnBlueHeartLost);
            player.OnBlackHeartLost.RemoveListener(OnBlackHeartLost);
        }
        
        private void OnBlueHeartLost(float amount)
        {
            // Check if we recover blue heart
            if (UnityEngine.Random.value < currentRecoveryChance)
            {
                var player = FindObjectOfType<PlayerStats>();
                if (player != null)
                {
                    player.AddHearts(HeartType.Blue, 0.5f);
                }
            }
        }
        
        private void OnBlackHeartLost(float amount, Vector2 position)
        {
            var player = FindObjectOfType<PlayerStats>();
            if (player != null)
            {
                // Apply damage boost
                player.ModifyDamageMultiplier(currentDamageBoost, blackHeartDamageBoostDuration);
                
                // Apply knockback to nearby enemies
                // This would be implemented in enemy controller
                EnemyManager.ApplyKnockback(position, currentKnockback);
                
                // Apply area damage for level 3
                if (hasAreaDamage)
                {
                    EnemyManager.ApplyAreaDamage(position, 3f, 0.5f); // Small radius and damage
                }
            }
        }
    }

    /// <summary>
    /// 5. Dark Power: Special effects when losing black hearts
    /// </summary>
    [CreateAssetMenu(fileName = "DarkPower", menuName = "PowerTree/Nodes/Survival/DarkPower")]
    public class DarkPowerNode : PowerNode
    {
        [SerializeField] private int[] bulletNegationCount = new int[] { 2, 3, 5 };
        [SerializeField] private float[] pushRange = new float[] { 0f, 5f, 5f };
        [SerializeField] private float[] screenDamagePercent = new float[] { 0f, 0f, 0.5f };
        
        private int currentNegationCount = 0;
        private float currentPushRange = 0f;
        private float currentScreenDamage = 0f;
        
        public override void ApplyEffect(PlayerStats player, int level)
        {
            if (level <= 0 || level > bulletNegationCount.Length) return;
            
            currentNegationCount = bulletNegationCount[level - 1];
            currentPushRange = pushRange[level - 1];
            currentScreenDamage = screenDamagePercent[level - 1];
            
            // Subscribe to black heart lost event
            player.OnBlackHeartLost.AddListener(OnBlackHeartLost);
        }
        
        public override void RemoveEffect(PlayerStats player)
        {
            player.OnBlackHeartLost.RemoveListener(OnBlackHeartLost);
        }
        
        private void OnBlackHeartLost(float amount, Vector2 position)
        {
            // Negate bullets
            BulletManager.NegateNearbyBullets(position, currentNegationCount);
            
            // Apply push effect for level 2+
            if (currentPushRange > 0)
            {
                EnemyManager.PushEnemiesAway(position, currentPushRange);
            }
            
            // Apply screen damage for level 3
            if (currentScreenDamage > 0)
            {
                EnemyManager.DamageAllEnemies(currentScreenDamage, excludeBosses: true);
            }
        }
    }

    /// <summary>
    /// 6. Near Death Revival: Chance to revive when dying
    /// </summary>
    [CreateAssetMenu(fileName = "NearDeathRevival", menuName = "PowerTree/Nodes/Survival/NearDeathRevival")]
    public class NearDeathRevivalNode : PowerNode
    {
        [SerializeField] private float[] reviveChance = new float[] { 0.05f, 0.1f, 0.15f };
        [SerializeField] private float[] redHeartsOnRevive = new float[] { 0f, 1f, 1f };
        [SerializeField] private float[] blueHeartsOnRevive = new float[] { 1f, 0f, 1f };
        
        private float currentReviveChance = 0f;
        private float currentRedHearts = 0f;
        private float currentBlueHearts = 0f;
        
        public override void ApplyEffect(PlayerStats player, int level)
        {
            if (level <= 0 || level > reviveChance.Length) return;
            
            currentReviveChance = reviveChance[level - 1];
            currentRedHearts = redHeartsOnRevive[level - 1];
            currentBlueHearts = blueHeartsOnRevive[level - 1];
            
            // Subscribe to player death event
            player.OnPlayerDeath.AddListener(() => OnPlayerDeath(player));
        }
        
        public override void RemoveEffect(PlayerStats player)
        {
            player.OnPlayerDeath.RemoveListener(() => OnPlayerDeath(player));
        }
        
        private void OnPlayerDeath(PlayerStats player)
        {
            // Check if we revive
            if (UnityEngine.Random.value < currentReviveChance)
            {
                // Implement revival logic
                player.ReviveWithHearts(currentRedHearts, currentBlueHearts);
            }
        }
    }

    /// <summary>
    /// 7. Last Shield: Dodge chance when at low health
    /// </summary>
    [CreateAssetMenu(fileName = "LastShield", menuName = "PowerTree/Nodes/Survival/LastShield")]
    public class LastShieldNode : PowerNode
    {
        [SerializeField] private float[] dodgeChance = new float[] { 0.3f, 0.5f, 0.5f };
        [SerializeField] private bool createPushWave = false; // Level 3 feature
        
        private float currentDodgeChance = 0f;
        private bool dodgeApplied = false;
        
        public override void ApplyEffect(PlayerStats player, int level)
        {
            if (level <= 0 || level > dodgeChance.Length) return;
            
            currentDodgeChance = dodgeChance[level - 1];
            createPushWave = (level == 3);
            
            // We need to monitor the player's health and apply dodge when it's low
            // This effect is special because it needs to be checked before every hit
            CheckAndApplyDodgeIfNeeded(player);
            
            // Subscribe to heart changes to update the dodge effect
            player.OnRedHeartLost.AddListener((_) => CheckAndApplyDodgeIfNeeded(player));
            player.OnRedHeartGained.AddListener((_) => CheckAndApplyDodgeIfNeeded(player));
        }
        
        public override void RemoveEffect(PlayerStats player)
        {
            player.OnRedHeartLost.RemoveListener((_) => CheckAndApplyDodgeIfNeeded(player));
            player.OnRedHeartGained.RemoveListener((_) => CheckAndApplyDodgeIfNeeded(player));
            
            // Remove any applied dodge effects
            if (dodgeApplied)
            {
                player.ModifyDodgeChance(-currentDodgeChance);
                dodgeApplied = false;
            }
        }
        
        private void CheckAndApplyDodgeIfNeeded(PlayerStats player)
        {
            bool isLowHealth = player.IsLowHealth(); // Health <= 0.5 hearts
            
            if (isLowHealth && !dodgeApplied)
            {
                // Add dodge chance
                player.ModifyDodgeChance(currentDodgeChance);
                dodgeApplied = true;
                
                // If level 3, set up event for successful dodges
                if (createPushWave)
                {
                    // This would be implemented in the dodge system
                    // When a dodge is successful, it would trigger a push wave
                }
            }
            else if (!isLowHealth && dodgeApplied)
            {
                // Remove dodge chance
                player.ModifyDodgeChance(-currentDodgeChance);
                dodgeApplied = false;
            }
        }
    }

    /// <summary>
    /// 8. Life Force Transformation: Chance to create blue/black hearts
    /// </summary>
    [CreateAssetMenu(fileName = "LifeForceTransformation", menuName = "PowerTree/Nodes/Survival/LifeForceTransformation")]
    public class LifeForceTransformationNode : PowerNode
    {
        [SerializeField] private float[] chanceValues = new float[] { 0.3f, 0.5f, 0.7f };
        [SerializeField] private float blackHeartChance = 0.3f; // For level 3
        
        private float currentChance = 0f;
        private bool canCreateBlackHeart = false;
        
        public override void ApplyEffect(PlayerStats player, int level)
        {
            if (level <= 0 || level > chanceValues.Length) return;
            
            currentChance = chanceValues[level - 1];
            canCreateBlackHeart = (level == 3);
            
            // Subscribe to red heart gained event
            player.OnRedHeartGained.AddListener(OnRedHeartGained);
        }
        
        public override void RemoveEffect(PlayerStats player)
        {
            player.OnRedHeartGained.RemoveListener(OnRedHeartGained);
        }
        
        private void OnRedHeartGained(float amount)
        {
            // Check if we create a blue/black heart
            if (UnityEngine.Random.value < currentChance)
            {
                var player = FindObjectOfType<PlayerStats>();
                if (player != null)
                {
                    HeartType heartType = HeartType.Blue;
                    
                    // For level 3, chance to create black heart instead
                    if (canCreateBlackHeart && UnityEngine.Random.value < blackHeartChance)
                    {
                        heartType = HeartType.Black;
                    }
                    
                    player.AddHearts(heartType, 0.5f);
                }
            }
        }
    }

    #endregion
} 