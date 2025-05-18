using System;
using System.Collections.Generic;
using PowerTree;
using UnityEngine;

namespace CardSystem
{
    /// <summary>
    /// ScriptableObject representation of a shop item
    /// </summary>
    [CreateAssetMenu(fileName = "ShopItem", menuName = "CardSystem/Shop Item")]
    public class ShopItemData : ScriptableObject
    {
        #region Fields
        [SerializeField] private string m_Id;
        [SerializeField] private string m_DisplayName;
        [SerializeField, TextArea(2, 4)] private string m_Description;
        [SerializeField] private int m_GoldCost;
        [SerializeField] private Sprite m_Icon;
        
        [Header("Item Type")]
        [SerializeField] private ShopItemType m_ItemType = ShopItemType.SingleCard;
        [SerializeField] private CardData m_Card;
        
        [Header("Consumable Settings")]
        [SerializeField] private ConsumableType m_ConsumableType = ConsumableType.None;
        [SerializeField] private float m_ConsumableValue;
        #endregion
        
        #region Properties
        public string Id => m_Id;
        public string DisplayName => m_DisplayName.Length > 0 ? m_DisplayName : m_Card.DisplayName;
        public string Description => m_Description.Length > 0 ? m_Description : m_Card.Description;
        public int GoldCost => m_GoldCost != 0 ? m_GoldCost : m_Card.GoldCost;
        public Sprite Icon => m_Icon != null ? m_Icon : m_Card.Icon;
        public ShopItemType ItemType => m_ItemType;
        public CardData Card => m_Card;

        public ConsumableType ConsumableType => m_ConsumableType;
        public float ConsumableValue => m_ConsumableValue;
        #endregion
        
        /// <summary>
        /// Creates a ShopItem instance from this data
        /// </summary>
        public ShopItem CreateShopItem()
        {
            if (m_ItemType == ShopItemType.SingleCard)
            {
                return new ShopItem(m_Card);
            }
            
            // For other types, the ShopItem just wraps the data
            return new ShopItem(this);
        }
        
        #region Unity Methods
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(m_Id))
            {
                m_Id = m_Card.Id;
            }
            
            if (string.IsNullOrEmpty(m_DisplayName))
            {
                m_DisplayName = m_Card.DisplayName;
            }

            if (string.IsNullOrEmpty(m_Description))
            {
                m_Description = m_Card.Description;
            }

            if (m_GoldCost == 0)
            {
                m_GoldCost = m_Card.GoldCost;
            }

            if (m_Icon == null)
            {
                m_Icon = m_Card.Icon;
            }
            
            if (m_ItemType == ShopItemType.SingleCard && m_Card == null)
            {
                Debug.LogWarning($"Shop item {name} is of type SingleCard but has no card assigned");
            }
            
            if (m_ItemType == ShopItemType.Consumable && m_ConsumableType == ConsumableType.None)
            {
                Debug.LogWarning($"Shop item {name} is of type Consumable but has no consumable type assigned");
            }
        }
        #endregion
    }
    
    /// <summary>
    /// Types of shop items
    /// </summary>
    public enum ShopItemType
    {
        SingleCard,
        Consumable,
    }
    
    /// <summary>
    /// Types of consumables
    /// </summary>
    public enum ConsumableType
    {
        None,
        CardReroll,
        ShopDiscount,
        GoldBoost,
        HealthRefill
    }
    
    /// <summary>
    /// When the item is available in the shop
    /// </summary>
    public enum AvailabilityType
    {
        Always,
        Rotation,
        SpecialEvent,
        LimitedTime
    }
} 