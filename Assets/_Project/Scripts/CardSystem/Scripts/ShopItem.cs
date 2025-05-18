using System;
using PowerTree;
using UnityEngine;

namespace CardSystem
{
    /// <summary>
    /// Represents a single item in the shop
    /// </summary>
    [Serializable]
    public class ShopItem
    {
        #region Fields
        [SerializeField] private CardData m_Card;
        [SerializeField] private ShopItemData m_ItemData;
        #endregion
        
        #region Properties
        public CardData Card => m_Card;
        public ShopItemData ItemData => m_ItemData;
        public int OriginalCost 
        { 
            get 
            {
                if (m_Card)
                {
                    return m_Card.GoldCost;
                }
                
                if (m_ItemData)
                {
                    return m_ItemData.GoldCost;
                }
                
                return 0;
            }
        }
        public int FinalCost 
        { 
            get 
            {
                // If we have a card directly, use its cost
                if (m_Card)
                {
                    var discountMultiplier = 1.0f;
                    var playerStats = PlayerStats.Instance;
                    if (playerStats)
                    {
                        discountMultiplier = 1.0f - playerStats.GetShopDiscountPercent();
                    }
                    return m_Card.CalculatePrice(discountMultiplier);
                }
                
                // Otherwise use the shop item data cost
                if (m_ItemData)
                {
                    var discountMultiplier = 1.0f;
                    var playerStats = PlayerStats.Instance;
                    if (playerStats)
                    {
                        discountMultiplier = 1.0f - playerStats.GetShopDiscountPercent();
                    }
                    return Mathf.RoundToInt(m_ItemData.GoldCost * discountMultiplier);
                }
                
                return 0;
            }
        }
        #endregion
        
        #region Constructors
        /// <summary>
        /// Creates a new shop item from a card
        /// </summary>
        public ShopItem(CardData card)
        {
            m_Card = card;
            m_ItemData = null;
        }
        
        /// <summary>
        /// Creates a new shop item from ShopItemData
        /// </summary>
        public ShopItem(ShopItemData itemData)
        {
            m_ItemData = itemData;
            
            // For SingleCard type, extract the card
            if (itemData.ItemType == ShopItemType.SingleCard)
            {
                m_Card = itemData.Card;
            }
            else
            {
                m_Card = null;
            }
        }
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Gets the original price before discounts
        /// </summary>
        public int GetOriginalPrice()
        {
            if (m_Card != null)
            {
                return m_Card.GoldCost;
            }
            
            if (m_ItemData != null)
            {
                return m_ItemData.GoldCost;
            }
            
            return 0;
        }
        
        /// <summary>
        /// Gets the amount saved from the discount
        /// </summary>
        public int GetSavingsAmount()
        {
            return GetOriginalPrice() - FinalCost;
        }

        /// <summary>
        /// Gets the display name for this item
        /// </summary>
        public string GetDisplayName()
        {
            if (m_Card != null)
            {
                return m_Card.DisplayName;
            }
            
            if (m_ItemData != null)
            {
                return m_ItemData.DisplayName;
            }
            
            return "Unknown Item";
        }
        
        /// <summary>
        /// Gets the description for this item
        /// </summary>
        public string GetDescription()
        {
            if (m_Card != null)
            {
                return m_Card.GetProcessedDescription();
            }
            
            if (m_ItemData != null)
            {
                return m_ItemData.Description;
            }
            
            return string.Empty;
        }
        
        /// <summary>
        /// Gets the icon for this item
        /// </summary>
        public Sprite GetIcon()
        {
            if (m_Card != null)
            {
                return m_Card.Icon;
            }
            
            if (m_ItemData != null)
            {
                return m_ItemData.Icon;
            }
            
            return null;
        }
        #endregion

        public void ApplyDiscount(float discountPercent)
        {
            
            
        }
    }
} 