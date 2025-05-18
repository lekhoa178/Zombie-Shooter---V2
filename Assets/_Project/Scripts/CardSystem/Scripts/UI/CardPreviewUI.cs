using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CardSystem
{
    /// <summary>
    /// Lightweight UI component for displaying a card preview with minimal information
    /// Used for previewing owned cards from collection
    /// </summary>
    public class CardPreviewUI : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Card Display")]
        [SerializeField] private Image m_CardIcon;
        [SerializeField] private Image m_RarityFrame;
        [SerializeField] private Image m_RarityIcon;
        #endregion
        
        #region Private Fields
        private CardData m_CardData;
        #endregion
        
        #region Properties
        public CardData CardData => m_CardData;
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Initialize the card preview with card data
        /// </summary>
        public void Initialize(CardData cardData)
        {
            m_CardData = cardData;
            
            if (cardData == null)
            {
                Debug.LogWarning("Trying to initialize CardPreviewUI with null CardData");
                return;
            }
            
            // Set card visuals (only icon and rarity)
            UpdateCardVisuals();
        }
        
        #endregion
        
        #region Private Methods
        /// <summary>
        /// Updates the card visuals based on the card data
        /// Only updates icon and rarity frame
        /// </summary>
        private void UpdateCardVisuals()
        {
            if (m_CardData == null)
                return;
                
            // Set card icon
            if (m_CardIcon && m_CardData.Icon)
            {
                m_CardIcon.sprite = m_CardData.Icon;
                m_CardIcon.gameObject.SetActive(true);
            }
            else if (m_CardIcon)
            {
                m_CardIcon.gameObject.SetActive(false);
            }
            
            // Set rarity frame color
            if (m_RarityFrame && m_CardData.Rarity)
            {
                m_RarityFrame.color = m_CardData.Rarity.Color;
            }
            
            // Set rarity icon
            if (m_RarityIcon && m_CardData.Rarity && m_CardData.Rarity.RarityIcon)
            {
                m_RarityIcon.sprite = m_CardData.Rarity.RarityIcon;
                m_RarityIcon.gameObject.SetActive(true);
            }
            else if (m_RarityIcon)
            {
                m_RarityIcon.gameObject.SetActive(false);
            }
        }
        #endregion
    }
} 