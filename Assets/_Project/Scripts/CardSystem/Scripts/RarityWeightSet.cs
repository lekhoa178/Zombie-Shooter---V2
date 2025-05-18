using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardSystem
{
    /// <summary>
    /// Defines weights for rarities in the shop
    /// </summary>
    [CreateAssetMenu(fileName = "Rarity Weight Set", menuName = "CardSystem/Rarity Weight Set")]
    [Serializable]
    public class RarityWeightSet : ScriptableObject
    {
        #region Fields
        [SerializeField] private string m_Id;
        [SerializeField] private string m_DisplayName;
        
        [SerializeField] 
        private List<Rarity> m_Weights = new();
        #endregion
        
        #region Properties
        public string Id => m_Id;
        public string DisplayName => m_DisplayName;
        public IReadOnlyList<Rarity> Weights => m_Weights;
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Gets the weight for a specific rarity
        /// </summary>
        public float GetWeight(string rarityId, float defaultWeight = 1.0f)
        {
            var weight = m_Weights.Find(w => w.Id == rarityId);
            return weight != null ? weight.DropRate : defaultWeight;
        }
        
        /// <summary>
        /// Gets the weight for a specific rarity
        /// </summary>
        public float GetWeight(Rarity rarity)
        {
            if (rarity == null)
                return 0f;
                
            Rarity weight = m_Weights.Find(w => w.Id == rarity.Id);
            return weight != null ? weight.DropRate : rarity.DropRate;
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
        }
        #endregion
    }
}