using System;
using UnityEngine;

namespace CardSystem
{
    /// <summary>
    /// Defines a rarity tier for cards
    /// </summary>
    [CreateAssetMenu(fileName = "Rarity", menuName = "CardSystem/Rarity")]
    public class Rarity : ScriptableObject
    {
        #region Fields
        [SerializeField] private string m_Id;
        [SerializeField] private string m_DisplayName;
        [SerializeField, Range(0f, 1f)] private float m_DropRate = 0.25f;
        [SerializeField] private Color m_Color = Color.white;
        [SerializeField] private Texture2D m_Background;
        [SerializeField] private Sprite m_RarityIcon;
        [SerializeField] private int m_BaseCost = 50;
        #endregion
        
        #region Properties
        public string Id => m_Id;
        public string DisplayName => m_DisplayName;
        public float DropRate => m_DropRate;
        public Color Color => m_Color;
        public Texture2D Background => m_Background;
        public Sprite RarityIcon => m_RarityIcon;
        public int BaseCost => m_BaseCost;
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Gets the color as a hex string
        /// </summary>
        public string GetColorHex()
        {
            return ColorUtility.ToHtmlStringRGB(m_Color);
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