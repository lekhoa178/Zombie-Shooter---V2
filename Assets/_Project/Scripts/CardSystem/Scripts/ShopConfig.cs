using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardSystem
{
    /// <summary>
    /// ScriptableObject for shop configuration settings
    /// </summary>
    [CreateAssetMenu(fileName = "ShopConfig", menuName = "CardSystem/Shop Config")]
    public class ShopConfig : ScriptableObject
    {
        #region Fields
        [Header("Shop Settings")]
        [SerializeField, Tooltip("Number of item slots in the shop")]
        private int m_SlotCount = 6;
        
        [Header("Item Generation")]
        [SerializeField] 
        private RarityWeightSet m_DefaultRarityWeights;
        
        [SerializeField] 
        private List<RarityWeightSet> m_SpecialRarityWeights = new List<RarityWeightSet>();

        [Header("Available Items")]
        [SerializeField] 
        private List<ShopItemData> m_AlwaysAvailableItems = new List<ShopItemData>();
        
        [SerializeField] 
        private List<ShopItemData> m_RotationItems = new List<ShopItemData>();
        
        [SerializeField] 
        private List<ShopItemData> m_SpecialEventItems = new List<ShopItemData>();
        #endregion
        
        #region Properties
        public int SlotCount => m_SlotCount;
        public RarityWeightSet DefaultRarityWeights => m_DefaultRarityWeights;
        public IReadOnlyList<RarityWeightSet> SpecialRarityWeights => m_SpecialRarityWeights;
        public IReadOnlyList<ShopItemData> AlwaysAvailableItems => m_AlwaysAvailableItems;
        public IReadOnlyList<ShopItemData> RotationItems => m_RotationItems;
        public IReadOnlyList<ShopItemData> SpecialEventItems => m_SpecialEventItems;
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Gets a rarity weight set by ID
        /// </summary>
        public RarityWeightSet GetRarityWeightSet(string id)
        {
            if (string.IsNullOrEmpty(id) || id == "default")
                return m_DefaultRarityWeights;
                
            return m_SpecialRarityWeights.Find(s => s.Id == id);
        }
        
        /// <summary>
        /// Gets all items of a specific availability type
        /// </summary>
        public List<ShopItemData> GetItemsByAvailability(AvailabilityType availability)
        {
            switch (availability)
            {
                case AvailabilityType.Always:
                    return new List<ShopItemData>(m_AlwaysAvailableItems);
                    
                case AvailabilityType.Rotation:
                    return new List<ShopItemData>(m_RotationItems);
                    
                case AvailabilityType.SpecialEvent:
                    return new List<ShopItemData>(m_SpecialEventItems);
                    
                default:
                    // Combine all items
                    List<ShopItemData> allItems = new List<ShopItemData>();
                    allItems.AddRange(m_AlwaysAvailableItems);
                    allItems.AddRange(m_RotationItems);
                    allItems.AddRange(m_SpecialEventItems);
                    return allItems;
            }
        }
        #endregion
        
        #region Unity Methods
        private void OnValidate()
        {
            m_SlotCount = Mathf.Max(1, m_SlotCount);
        }
        #endregion
    }
} 