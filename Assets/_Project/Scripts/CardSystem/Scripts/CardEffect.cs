using System;
using PowerTree;
using UnityEngine;

namespace CardSystem
{
    /// <summary>
    /// Represents a single effect that a card can apply to the player
    /// </summary>
    [Serializable]
    public class CardEffect
    {
        #region Fields
        [SerializeField] private CardEffectType m_EffectType;
        [SerializeField] private float m_Value;
        [SerializeField] private float m_Duration;
        [SerializeField] private bool m_IsTemporary;
        #endregion
        
        #region Properties
        public CardEffectType EffectType => m_EffectType;
        public float Value => m_Value;
        public float Duration => m_Duration;
        public bool IsTemporary => m_IsTemporary;
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Applies the effect to the player stats
        /// </summary>
        public void Apply(PlayerStats playerStats)
        {
            switch (m_EffectType)
            {
                case CardEffectType.DamageMultiplier:
                    playerStats.ModifyDamageMultiplier(m_Value, m_IsTemporary ? m_Duration : 0);
                    break;
                    
                case CardEffectType.AttackSpeedMultiplier:
                    playerStats.ModifyAttackSpeedMultiplier(m_Value, m_IsTemporary ? m_Duration : 0);
                    break;
                    
                case CardEffectType.MoveSpeedMultiplier:
                    playerStats.ModifyMoveSpeedMultiplier(m_Value, m_IsTemporary ? m_Duration : 0);
                    break;
                    
                case CardEffectType.DodgeChance:
                    playerStats.ModifyDodgeChance(m_Value, m_IsTemporary ? m_Duration : 0);
                    break;
                    
                case CardEffectType.AddRedHeart:
                    playerStats.AddHearts(HeartType.Red, m_Value);
                    break;
                    
                case CardEffectType.AddBlueHeart:
                    playerStats.AddHearts(HeartType.Blue, m_Value);
                    break;
                    
                case CardEffectType.AddBlackHeart:
                    playerStats.AddHearts(HeartType.Black, m_Value);
                    break;
                    
                case CardEffectType.GoldMultiplier:
                    playerStats.ModifyGoldDropMultiplier(m_Value);
                    break;
                    
                case CardEffectType.ShopDiscount:
                    playerStats.ModifyShopDiscount(m_Value);
                    break;
            }
        }
        
        /// <summary>
        /// Returns a display-friendly name for this effect type
        /// </summary>
        public string GetDisplayName()
        {
            return m_EffectType.ToString().SplitCamelCase();
        }
        
        /// <summary>
        /// Returns a display-friendly description of this effect
        /// </summary>
        public string GetDescription()
        {
            string value = FormatValue();
            string duration = m_IsTemporary ? $" for {m_Duration:0.0}s" : string.Empty;
            
            switch (m_EffectType)
            {
                case CardEffectType.DamageMultiplier:
                    return $"Increases damage by {value}{duration}";
                    
                case CardEffectType.AttackSpeedMultiplier:
                    return $"Increases attack speed by {value}{duration}";
                    
                case CardEffectType.MoveSpeedMultiplier:
                    return $"Increases movement speed by {value}{duration}";
                    
                case CardEffectType.DodgeChance:
                    return $"Increases dodge chance by {value}{duration}";
                    
                case CardEffectType.AddRedHeart:
                    return $"Adds {value} red hearts";
                    
                case CardEffectType.AddBlueHeart:
                    return $"Adds {value} blue hearts";
                    
                case CardEffectType.AddBlackHeart:
                    return $"Adds {value} black hearts";
                    
                case CardEffectType.GoldMultiplier:
                    return $"Increases gold drops by {value}";
                    
                case CardEffectType.ShopDiscount:
                    return $"Provides {value} discount in shops";
                    
                default:
                    return $"{GetDisplayName()}: {value}{duration}";
            }
        }
        #endregion
        
        #region Private Methods
        /// <summary>
        /// Formats the value based on the effect type
        /// </summary>
        private string FormatValue()
        {
            if (m_EffectType == CardEffectType.DamageMultiplier || 
                m_EffectType == CardEffectType.AttackSpeedMultiplier ||
                m_EffectType == CardEffectType.MoveSpeedMultiplier ||
                m_EffectType == CardEffectType.DodgeChance ||
                m_EffectType == CardEffectType.GoldMultiplier ||
                m_EffectType == CardEffectType.ShopDiscount)
            {
                return $"{m_Value * 100:0}%";
            }
            
            // For heart quantities
            if (m_Value == (int)m_Value)
            {
                return $"{(int)m_Value}";
            }
            
            return $"{m_Value:0.#}";
        }
        #endregion
    }
} 