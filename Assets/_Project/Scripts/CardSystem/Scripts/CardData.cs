using System;
using System.Collections.Generic;
using PowerTree;
using UnityEngine;

namespace CardSystem
{
    /// <summary>
    /// Defines a card with effects and properties
    /// </summary>
    [CreateAssetMenu(fileName = "Card", menuName = "CardSystem/Card")]
    public class CardData : ScriptableObject
    {
        #region Fields
        [SerializeField] private string m_Id;
        [SerializeField] private string m_DisplayName;
        [SerializeField] private Sprite m_Icon;
        [SerializeField] private Rarity m_Rarity;
        [SerializeField, TextArea(3, 6)] private string m_Description;
        [SerializeField] private string m_LayoutTemplate;
        [SerializeField] private int m_GoldCost;
        [SerializeField] private List<CardEffect> m_Effects = new List<CardEffect>();
        [SerializeField] private string m_FlavorText;
        [SerializeField] private List<string> m_Tags = new List<string>();
        #endregion
        
        #region Properties
        public string Id => m_Id;
        public string DisplayName => m_DisplayName;
        public string Description => m_Description;
        public string FlavorText => m_FlavorText;
        public int GoldCost => m_GoldCost;
        public Sprite Icon => m_Icon;
        public Rarity Rarity => m_Rarity;
        public CardEffect[] Effects => m_Effects.ToArray();
        public List<string> Tags => m_Tags != null ? new List<string>(m_Tags) : new List<string>();
        #endregion

        #region Public Methods
        /// <summary>
        /// Applies all effects of this card to the target player stats
        /// </summary>
        public void ApplyEffects(PlayerStats target)
        {
            foreach (var effect in m_Effects)
            {
                effect.Apply(target);
            }
        }
        
        /// <summary>
        /// Calculates the price of the card with any discount applied
        /// </summary>
        public int CalculatePrice(float discountMultiplier = 1.0f)
        {
            return Mathf.RoundToInt(m_GoldCost * discountMultiplier);
        }
        
        /// <summary>
        /// Processes the description string by replacing placeholders with actual values
        /// </summary>
        public string GetProcessedDescription()
        {
            string processedDesc = m_Description;
            
            // Replace placeholders with effect values
            foreach (var effect in m_Effects)
            {
                // For damage multipliers and chances, convert to percentage
                if (effect.EffectType == CardEffectType.DamageMultiplier || 
                    effect.EffectType == CardEffectType.AttackSpeedMultiplier ||
                    effect.EffectType == CardEffectType.MoveSpeedMultiplier ||
                    effect.EffectType == CardEffectType.DodgeChance)
                {
                    processedDesc = processedDesc.Replace($"{{{effect.EffectType.ToString()}}}", $"{effect.Value * 100:0}%");
                }
                // Handle durations
                else if (effect.IsTemporary)
                {
                    processedDesc = processedDesc.Replace("{duration}", $"{effect.Duration:0.0}s");
                }
                // Handle health amounts
                else if (effect.EffectType == CardEffectType.AddRedHeart ||
                        effect.EffectType == CardEffectType.AddBlueHeart ||
                        effect.EffectType == CardEffectType.AddBlackHeart)
                {
                    processedDesc = processedDesc.Replace("{healthAmount}", $"{effect.Value:0.#}");
                }
                // Generic value replacement
                else
                {
                    processedDesc = processedDesc.Replace($"{{{effect.EffectType.ToString()}}}", $"{effect.Value:0.##}");
                }
            }
            
            return processedDesc;
        }
        #endregion
        
        #region Unity Methods
        private void OnValidate()
        {
            // Auto-generate ID from name if not set
            if (string.IsNullOrEmpty(m_Id))
            {
                m_Id = name.ToLower().Replace(" ", "_");
            }
            
            // Auto-generate display name from name if not set
            if (string.IsNullOrEmpty(m_DisplayName))
            {
                m_DisplayName = name;
            }
            
            // Auto-calculate gold cost from rarity if set but gold cost is 0
            if (m_Rarity != null && m_GoldCost == 0)
            {
                m_GoldCost = m_Rarity.BaseCost;
            }
        }
        #endregion
    }
} 