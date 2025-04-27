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
        [SerializeField] private float[] chanceValues = new[] { 0.1f, 0.2f, 0.3f }; // 10%, 20%, 30%
        private float _currentChance;
        
        private void OnValidate()
        {
            descriptions = new[]
            {
                "{chanceValues[0]} chance to prevent red heart loss when taking damage",
                "Chance increased to {chanceValues[1]}",
                "Chance increased to {chanceValues[2]}"
            };
        }
        
        public override void ApplyEffect(PlayerStats player, int level)
        {
            if (level <= 0 || level > chanceValues.Length) return;
            
            _currentChance = chanceValues[level - 1];
            
            player.OnRedHeartLost.AddListener(OnRedHeartLost);
        }
        
        public override void RemoveEffect(PlayerStats player)
        {
            player.OnRedHeartLost.RemoveListener(OnRedHeartLost);
            _currentChance = 0f;

            ResetState();
        }

        public override void ResetState()
        {
            _currentChance = 0f;
        }
        
        private void OnRedHeartLost(float amount)
        {
            if (!(Random.value < _currentChance)) return;
            
            var player = FindAnyObjectByType<PlayerStats>();
            if (player)
            {
                player.AddHearts(HeartType.Red, amount);
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
        
        private void OnValidate()
        {
            descriptions = new[]
            {
                "Heal 1 red heart every {levelsToHealRed[0]} completed levels",
                "Heal 1 red heart after every level",
                "Heal 1 red heart after every level and 1 blue heart every {levelsToHealBlue[2]} completed levels"
            };
        }
        
        private int _levelCounter;
        private int _currentRedThreshold;
        private int _currentBlueThreshold;
        
        public override void ApplyEffect(PlayerStats player, int level)
        {
            if (level <= 0 || level > levelsToHealRed.Length) return;
            
            _currentRedThreshold = levelsToHealRed[level - 1];
            _currentBlueThreshold = levelsToHealBlue[level - 1];
            
            // Subscribe to level completion event
            player.OnLevelCompleted.AddListener(() => OnLevelCompleted(player));
        }
        
        public override void RemoveEffect(PlayerStats player)
        {
            player.OnLevelCompleted.RemoveListener(() => OnLevelCompleted(player));
            
            ResetState();
        }
        
        public override void ResetState()
        {
            _levelCounter = 0;
            _currentRedThreshold = 0;
            _currentBlueThreshold = 0;
        }
        
        private void OnLevelCompleted(PlayerStats player)
        {
            _levelCounter++;
            
            // Red heart healing
            if (_currentRedThreshold > 0 && _levelCounter % _currentRedThreshold == 0)
            {
                player.AddHearts(HeartType.Red, 1f);
            }
            
            // Blue heart healing
            if (_currentBlueThreshold > 0 && _levelCounter % _currentBlueThreshold == 0)
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
        
        private void OnValidate()
        {
            descriptions = new[]
            {
                "Gain {blueHeartsValues[0]} blue hearts at the start of each level",
                "Increased to {blueHeartsValues[1]} blue hearts",
                "Gain {blueHeartsValues[2]} blue hearts and {nonLossChanceValues[2]} chance to not lose blue hearts"
            };
        }
        
        private float _currentNonLossChance;
        
        public override void ApplyEffect(PlayerStats player, int level)
        {
            if (level <= 0 || level > blueHeartsValues.Length) return;
            
            // Give blue hearts at level start
            float heartsToAdd = blueHeartsValues[level - 1];
            player.AddHearts(HeartType.Blue, heartsToAdd);
            
            // Set up non-loss chance for level 3
            if (level == 3)
            {
                _currentNonLossChance = nonLossChanceValues[level - 1];
                player.OnBlueHeartLost.AddListener(OnBlueHeartLost);
            }
            
            // Subscribe to level completion to add hearts each level
            player.OnLevelCompleted.AddListener(() => OnLevelCompleted(player, heartsToAdd));
        }
        
        public override void RemoveEffect(PlayerStats player)
        {
            player.OnBlueHeartLost.RemoveListener(OnBlueHeartLost);
            player.OnLevelCompleted.RemoveListener(delegate { OnLevelCompleted(player, 0); });
            
            ResetState();
        }
        
        public override void ResetState()
        {
            _currentNonLossChance = 0f;
        }
        
        private void OnLevelCompleted(PlayerStats player, float heartsToAdd)
        {
            // Add blue hearts at the start of each new level
            player.AddHearts(HeartType.Blue, heartsToAdd);
        }
        
        private void OnBlueHeartLost(float amount)
        {
            if (!(Random.value < _currentNonLossChance)) return;
            
            var player = FindAnyObjectByType<PlayerStats>();
            if (player)
            {
                player.AddHearts(HeartType.Blue, amount);
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
        [SerializeField] private bool hasAreaDamage; // Level 3 has area damage
        
        private void OnValidate()
        {
            descriptions = new[]
            {
                "{blueHeartRecoveryChance[0]} chance to recover blue hearts when taking damage, black hearts give {blackHeartDamageBoost[0]} damage boost for {blackHeartDamageBoostDuration} seconds when lost",
                "Chances increased to {blueHeartRecoveryChance[1]}, damage boost to {blackHeartDamageBoost[1]}",
                "Chances increased to {blueHeartRecoveryChance[2]}, damage boost to {blackHeartDamageBoost[2]}, stronger knockback and area damage"
            };
        }
        
        private float _currentRecoveryChance;
        private float _currentDamageBoost;
        private float _currentKnockback;
        
        public override void ApplyEffect(PlayerStats player, int level)
        {
            if (level <= 0 || level > blueHeartRecoveryChance.Length) return;
            
            _currentRecoveryChance = blueHeartRecoveryChance[level - 1];
            _currentDamageBoost = blackHeartDamageBoost[level - 1];
            _currentKnockback = knockbackStrength[level - 1];
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
            
            ResetState();
        }
        
        public override void ResetState()
        {
            _currentRecoveryChance = 0f;
            _currentDamageBoost = 0f;
            _currentKnockback = 0f;
        }
        
        private void OnBlueHeartLost(float amount)
        {
            if (!(Random.value < _currentRecoveryChance)) return;
            
            var player = FindAnyObjectByType<PlayerStats>();
            if (player == null)
            {
                player.AddHearts(HeartType.Blue, 0.5f);
            }
        }
        
        private void OnBlackHeartLost(float amount, Vector3 position)
        {
            var player = FindAnyObjectByType<PlayerStats>();
            if (player != null)
            {
                // Apply damage boost
                player.ModifyDamageMultiplier(_currentDamageBoost, blackHeartDamageBoostDuration);
                
                CombatUtilities.PushEnemiesAway(position, _currentKnockback);
                
                // Apply area damage for level 3
                if (hasAreaDamage)
                {
                    CombatUtilities.ApplyAreaDamage(position, 3f, 0.5f); // Small radius and damage
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
        
        private void OnValidate()
        {
            descriptions = new[]
            {
                "Negate {bulletNegationCount[0]} nearby bullets when losing a black heart",
                "Negates {bulletNegationCount[1]} bullets and pushes enemies back {pushRange[1]} units",
                "Negates {bulletNegationCount[2]} bullets, pushes enemies, and deals {screenDamagePercent[2]} damage to all enemies"
            };
        }
        
        private int _currentNegationCount;
        private float _currentPushRange;
        private float _currentScreenDamage;
        
        public override void ApplyEffect(PlayerStats player, int level)
        {
            if (level <= 0 || level > bulletNegationCount.Length) return;
            
            _currentNegationCount = bulletNegationCount[level - 1];
            _currentPushRange = pushRange[level - 1];
            _currentScreenDamage = screenDamagePercent[level - 1];
            
            // Subscribe to black heart lost event
            player.OnBlackHeartLost.AddListener(OnBlackHeartLost);
        }
        
        public override void RemoveEffect(PlayerStats player)
        {
            player.OnBlackHeartLost.RemoveListener(OnBlackHeartLost);
            
            ResetState();
        }
        
        public override void ResetState()
        {
            _currentNegationCount = 0;
            _currentPushRange = 0f;
            _currentScreenDamage = 0f;
        }
        
        private void OnBlackHeartLost(float amount, Vector3 position)
        {
            // Negate bullets
            CombatUtilities.NegateNearbyBullets(position, _currentNegationCount);
            
            // Apply push effect for level 2+
            if (_currentPushRange > 0)
            {
                CombatUtilities.PushEnemiesAway(position, _currentPushRange);
            }
            
            // Apply screen damage for level 3
            if (_currentScreenDamage > 0)
            {
                CombatUtilities.DamageAllEnemies(_currentScreenDamage, excludeBosses: true);
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
        
        private void OnValidate()
        {
            descriptions = new[]
            {
                "{reviveChance[0]} chance to revive with {blueHeartsOnRevive[0]} blue heart when dying",
                "Chance increased to {reviveChance[1]}, revive with {redHeartsOnRevive[1]} red heart instead",
                "Chance increased to {reviveChance[2]}, revive with both {redHeartsOnRevive[2]} red heart and {blueHeartsOnRevive[2]} blue heart"
            };
        }
        
        private float _currentReviveChance;
        private float _currentRedHearts;
        private float _currentBlueHearts;
        
        public override void ApplyEffect(PlayerStats player, int level)
        {
            if (level <= 0 || level > reviveChance.Length) return;
            
            _currentReviveChance = reviveChance[level - 1];
            _currentRedHearts = redHeartsOnRevive[level - 1];
            _currentBlueHearts = blueHeartsOnRevive[level - 1];
            
            // Subscribe to player death event
            player.OnPlayerDeath.AddListener(() => OnPlayerDeath(player));
        }
        
        public override void RemoveEffect(PlayerStats player)
        {
            player.OnPlayerDeath.RemoveListener(() => OnPlayerDeath(player));
            
            ResetState();
        }
        
        public override void ResetState()
        {
            _currentReviveChance = 0f;
            _currentRedHearts = 0f;
            _currentBlueHearts = 0f;
        }
        
        private void OnPlayerDeath(PlayerStats player)
        {
            if (Random.value < _currentReviveChance)
            {
                player.ReviveWithHearts(_currentRedHearts, _currentBlueHearts);
            }
        }
    }

    /// <summary>
    /// 7. Last Shield: Dodge chance when at low health
    /// </summary>
    [CreateAssetMenu(fileName = "LastShield", menuName = "PowerTree/Nodes/Survival/LastShield")]
    public class LastShieldNode : PowerNode
    {
        [SerializeField] private float[] dodgeChance = { 0.3f, 0.5f, 0.5f };
        [SerializeField] private bool createPushWave; // Level 3 feature
        
        private void OnValidate()
        {
            descriptions = new[]
            {
                "Gain {dodgeChance[0]} chance to dodge attacks when at low health",
                "Dodge chance increased to {dodgeChance[1]}",
                "Successful dodges now push enemies away"
            };
        }
        
        private float _currentDodgeChance;
        private bool _dodgeApplied;
        
        public override void ApplyEffect(PlayerStats player, int level)
        {
            if (level <= 0 || level > dodgeChance.Length) return;
            
            _currentDodgeChance = dodgeChance[level - 1];
            createPushWave = (level == 3);
            
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
            if (_dodgeApplied)
            {
                player.ModifyDodgeChance(-_currentDodgeChance);
                _dodgeApplied = false;
            }
            
            ResetState();
        }
        
        public override void ResetState()
        {
            _currentDodgeChance = 0f;
            _dodgeApplied = false;
        }
        
        private void CheckAndApplyDodgeIfNeeded(PlayerStats player)
        {
            bool isLowHealth = player.IsLowHealth();
            
            if (isLowHealth && !_dodgeApplied)
            {
                player.ModifyDodgeChance(_currentDodgeChance);
                _dodgeApplied = true;
                
                // If level 3, set up event for successful dodges
                if (createPushWave)
                {
                    CombatUtilities.PushEnemiesAway(player.transform.position, 5f);
                }
            }
            else if (!isLowHealth && _dodgeApplied)
            {
                player.ModifyDodgeChance(-_currentDodgeChance);
                _dodgeApplied = false;
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
        
        private void OnValidate()
        {
            descriptions = new[]
            {
                "{chanceValues[0]} chance to transform red heart pickups into blue hearts",
                "Chance increased to {chanceValues[1]}",
                "Chance increased to {chanceValues[2]}, with {blackHeartChance} chance to create black hearts instead"
            };
        }
        
        private float _currentChance;
        private bool _canCreateBlackHeart;
        
        public override void ApplyEffect(PlayerStats player, int level)
        {
            if (level <= 0 || level > chanceValues.Length) return;
            
            _currentChance = chanceValues[level - 1];
            _canCreateBlackHeart = (level == 3);
            
            // Subscribe to red heart gained event
            player.OnRedHeartGained.AddListener(OnRedHeartGained);
        }
        
        public override void RemoveEffect(PlayerStats player)
        {
            player.OnRedHeartGained.RemoveListener(OnRedHeartGained);
            
            ResetState();
        }
        
        public override void ResetState()
        {
            _currentChance = 0f;
            _canCreateBlackHeart = false;
        }
        
        private void OnRedHeartGained(float amount)
        {
            if (!(Random.value < _currentChance)) return;
            
            var player = FindAnyObjectByType<PlayerStats>();
            if (!player) return;
            
            var heartType = HeartType.Blue;
                    
            // For level 3, chance to create black heart instead
            if (_canCreateBlackHeart && Random.value < blackHeartChance)
            {
                heartType = HeartType.Black;
            }
                    
            player.AddHearts(heartType, 0.5f);
        }
    }

    #endregion
} 