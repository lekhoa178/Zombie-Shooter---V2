using System.Collections;
using System.Collections.Generic;
using CardSystem;
using DungeonGeneration;
using UnityEngine;
using UnityEngine.Serialization;

namespace PowerTree
{
    #region Finance Branch Nodes

    /// <summary>
    /// 1. Luck: Increase chance for enemies to drop gold
    /// </summary>
    [CreateAssetMenu(fileName = "Luck", menuName = "PowerTree/Nodes/Finance/Luck")]
    public class LuckNode : PowerNode
    {
        [SerializeField] private float[] goldDropChance = new float[] { 0.1f, 0.2f, 0.3f }; // 10%, 20%, 30%
        [SerializeField] private float doubleGoldChance = 0.3f; // 30% chance for x2 gold at level 3
        
        private float currentGoldDropChance = 0f;
        private bool canDropDoubleGold = false;
        
        private void OnValidate()
        {
            // Auto-populate descriptions with field values
            descriptions = new string[]
            {
                "Enemies have a {goldDropChance[0]} chance to drop gold when defeated",
                "Gold drop chance increased to {goldDropChance[1]}",
                "Gold drop chance increased to {goldDropChance[2]} with {doubleGoldChance} chance for double gold"
            };
        }
        
        public override void ApplyEffect(PlayerStats player, int level)
        {
            if (level <= 0 || level > goldDropChance.Length) return;
            
            currentGoldDropChance = goldDropChance[level - 1];
            canDropDoubleGold = (level == 3);
            
            EnemyManager.Instance?.OnEnemyUnregistered.AddListener(OnEnemyUnregistered);
        }
        
        public override void RemoveEffect(PlayerStats player)
        {
            EnemyManager.Instance?.OnEnemyUnregistered.RemoveListener(OnEnemyUnregistered);
            
            ResetState();
        }

        public override void ResetState()
        {
            currentGoldDropChance = 0f;
            canDropDoubleGold = false;
        }
        
        private void OnEnemyUnregistered(BaseEnemy enemy)
        {
            if (!enemy) return;

            if (!(Random.value < currentGoldDropChance)) return;
            
            var goldAmount = Mathf.RoundToInt(enemy.Data.goldDropAmount);
            PlayerStats.Instance?.AddDroppedGold(goldAmount);

            if (!canDropDoubleGold || !(Random.value < doubleGoldChance)) return;
            
            var doubleGoldAmount = Mathf.RoundToInt(goldAmount * 2);
            PlayerStats.Instance?.AddDroppedGold(doubleGoldAmount);
        }
    }

    /// <summary>
    /// 2. Hell Merchant: Reduce card prices in shop
    /// </summary>
    [CreateAssetMenu(fileName = "HellMerchant", menuName = "PowerTree/Nodes/Finance/HellMerchant")]
    public class HellMerchantNode : PowerNode
    {
        [SerializeField] private float[] discountPercent = new float[] { 0.05f, 0.1f, 0.15f }; // 5%, 10%, 15%
        
        private float currentDiscount = 0f;
        
        private void OnValidate()
        {
            // Auto-populate descriptions with field values
            descriptions = new string[]
            {
                "Shop prices are reduced by {discountPercent[0]}%",
                "Shop discount increased to {discountPercent[1]}%",
                "Shop discount increased to {discountPercent[2]}%"
            };
        }
        
        public override void ApplyEffect(PlayerStats player, int level)
        {
            if (level <= 0 || level > discountPercent.Length) return;
            
            // Remove previous effect if any
            if (currentDiscount > 0)
            {
                player.ModifyShopDiscount(-currentDiscount);
            }
            
            // Apply new effect
            currentDiscount = discountPercent[level - 1];
            
            // Apply shop discount (permanent)
            player.ModifyShopDiscount(currentDiscount);
        }
        
        public override void RemoveEffect(PlayerStats player)
        {
            // Remove shop discount
            player.ModifyShopDiscount(-currentDiscount);
            
            ResetState();
        }
        
        public override void ResetState()
        {
            currentDiscount = 0f;
        }
    }

    /// <summary>
    /// 3. Infinite Treasure: Bonus gold for completing level without damage
    /// </summary>
    [CreateAssetMenu(fileName = "InfiniteTreasure", menuName = "PowerTree/Nodes/Finance/InfiniteTreasure")]
    public class InfiniteTreasureNode : PowerNode
    {
        [SerializeField] private float[] goldBoostChance = new float[] { 0.5f, 0.75f, 1.0f }; // 50%, 75%, 100%
        [SerializeField] private float specialCardChance = 0.1f; // 10% chance at level 3
        
        private float currentGoldBoost = 0f;
        private bool canGetSpecialCard = false;
        
        private void OnValidate()
        {
            // Auto-populate descriptions with field values
            descriptions = new string[]
            {
                "Completing a level without taking damage grants {goldBoostChance[0]} extra gold",
                "Bonus gold increased to {goldBoostChance[1]}",
                "Bonus gold increased to {goldBoostChance[2]} and {specialCardChance} chance to get a special card"
            };
        }
        
        public override void ApplyEffect(PlayerStats player, int level)
        {
            ResetState();

            if (level <= 0 || level > goldBoostChance.Length) return;
            
            currentGoldBoost = goldBoostChance[level - 1];
            canGetSpecialCard = (level == 3);
            
            // Subscribe to level completion event
            player.OnLevelCompletedWithoutDamage.AddListener(OnLevelCompletedWithoutDamage);
        }
        
        public override void RemoveEffect(PlayerStats player)
        {
            player.OnLevelCompletedWithoutDamage.RemoveListener(OnLevelCompletedWithoutDamage);
            
            ResetState();
        }
        
        public override void ResetState()
        {
            currentGoldBoost = 0f;
            canGetSpecialCard = false;
        }
        
        private void OnLevelCompletedWithoutDamage(bool withoutDamage)
        {
            if (withoutDamage)
            {
                var player = PlayerStats.Instance;
                if (player)
                {
                    // Calculate bonus gold based on level gold
                    int levelGold = FindAnyObjectByType<StageBaseManager>().CoinEarnAmount;
                    int bonusGold = Mathf.RoundToInt(levelGold * currentGoldBoost);
                    
                    // Award bonus gold
                    player.AddGold(bonusGold);
                    
                    // Check for special card at level 3
                    if (canGetSpecialCard && Random.value < specialCardChance)
                    {
                        FindAnyObjectByType<CardSelectionScreenUI>()?.ShowSelectionScreen(3, 1,
                            null, null, null, $"{displayName}'s Special Card", "Select one special card.");
                    }
                }
            }
        }
    }

    /// <summary>
    /// 4. Safe Finance: Get gold back when losing red hearts
    /// </summary>
    [CreateAssetMenu(fileName = "SafeFinance", menuName = "PowerTree/Nodes/Finance/SafeFinance")]
    public class SafeFinanceNode : PowerNode
    {
        [SerializeField] private int[] goldRefund = new int[] { 10, 15, 20 }; // 10, 15, 20 gold
        [SerializeField] private float heartSaveChance = 0.1f; // 10% chance at level 3
        
        private int currentGoldRefund = 0;
        private bool canSaveHeart = false;
        
        private void OnValidate()
        {
            // Auto-populate descriptions with field values
            descriptions = new string[]
            {
                "When losing a red heart, gain {goldRefund[0]} gold as compensation",
                "Gold refund increased to {goldRefund[1]}",
                "Gold refund increased to {goldRefund[2]} and {heartSaveChance} chance to not lose the heart"
            };
        }
        
        public override void ApplyEffect(PlayerStats player, int level)
        {
            if (level <= 0 || level > goldRefund.Length) return;
            
            currentGoldRefund = goldRefund[level - 1];
            canSaveHeart = (level == 3);
            
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
            currentGoldRefund = 0;
            canSaveHeart = false;
        }
        
        private void OnRedHeartLost(float amount)
        {
            var player = PlayerStats.Instance;
            if (player)
            {
                player.AddGold(currentGoldRefund);
                
                if (canSaveHeart && UnityEngine.Random.value < heartSaveChance)
                {
                    player.AddHearts(HeartType.Red, amount);
                }
            }
        }
    }

    #endregion
}