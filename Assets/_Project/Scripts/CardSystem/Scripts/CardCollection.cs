using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityUtils;

namespace CardSystem
{
    /// <summary>
    /// Manages the player's card collection - storing, loading, and organizing all owned cards
    /// </summary>
    public class CardCollection : PersistentSingleton<CardCollection>
    {
        
        #region Events
        public event Action OnCollectionChanged;
        public event Action<CardData, bool> OnCardAcquired;
        #endregion
        
        #region Serialized Fields
        [Header("Collection Settings")]
        [SerializeField] private bool m_CreateDefaultCollection = true;
        #endregion
        
        #region Private Fields
        private List<CardData> m_Collection = new List<CardData>();
        private CardManager m_CardManager;
        #endregion
        
        #region Public Properties
        /// <summary>
        /// All cards owned by the player
        /// </summary>
        public List<CardData> Collection => new List<CardData>(m_Collection);
        #endregion
        
        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            // Get references to required components
            m_CardManager = CardManager.Instance;
            
            if (m_CardManager == null)
            {
                Debug.LogError("CardManager not found. CardCollection requires CardManager to function.");
                enabled = false;
                return;
            }
        }
        
        private void Start()
        {
            // Load the player's collection from save
            LoadCollection();
            
            // Create default collection if needed and collection is empty
            if (m_CreateDefaultCollection && m_Collection.Count == 0)
            {
                CreateDefaultCollection();
            }
        }
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Add a card to the player's collection
        /// </summary>
        public void AddCardToCollection(CardData card, bool notifyAcquisition = true)
        {
            if (card == null)
                return;
                
            m_Collection.Add(card);
            
            // Notify listeners that the collection has changed
            OnCollectionChanged?.Invoke();
            
            // Notify about card acquisition if requested
            if (notifyAcquisition)
            {
                OnCardAcquired?.Invoke(card, true);
            }
        }
        
        /// <summary>
        /// Remove a card from the player's collection
        /// </summary>
        public void RemoveCardFromCollection(CardData card)
        {
            if (card == null)
                return;
                
            bool removed = m_Collection.Remove(card);
            
            if (removed)
            {
                // Notify listeners that the collection has changed
                OnCollectionChanged?.Invoke();
                OnCardAcquired?.Invoke(card, false);
            }
        }
        
        /// <summary>
        /// Sort the collection by the specified criteria
        /// </summary>
        public void SortCollection(SortType sortType, bool descending = false)
        {
            // switch (sortType)
            // {
            //     case SortType.ByName:
            //         m_Collection = descending ? 
            //             m_Collection.OrderByDescending(c => c.DisplayName).ToList() : 
            //             m_Collection.OrderBy(c => c.DisplayName).ToList();
            //         break;
            //     case SortType.ByRarity:
            //         m_Collection = descending ? 
            //             m_Collection.OrderByDescending(c => c.Rarity.SortOrder).ToList() : 
            //             m_Collection.OrderBy(c => c.Rarity.SortOrder).ToList();
            //         break;
            //     case SortType.ByCost:
            //         m_Collection = descending ? 
            //             m_Collection.OrderByDescending(c => c.Cost).ToList() : 
            //             m_Collection.OrderBy(c => c.Cost).ToList();
            //         break;
            //     case SortType.ByAttack:
            //         m_Collection = descending ? 
            //             m_Collection.OrderByDescending(c => c.Attack).ToList() : 
            //             m_Collection.OrderBy(c => c.Attack).ToList();
            //         break;
            //     case SortType.ByDefense:
            //         m_Collection = descending ? 
            //             m_Collection.OrderByDescending(c => c.Defense).ToList() : 
            //             m_Collection.OrderBy(c => c.Defense).ToList();
            //         break;
            //     case SortType.ByRecentlyAdded:
            //         // This would require tracking when cards were added
            //         // For now, we'll just leave it as is or you could implement a timestamp system
            //         break;
            // }
            //
            // Notify listeners that the collection has changed (order changed)
            OnCollectionChanged?.Invoke();
        }
        
        /// <summary>
        /// Save the player's collection
        /// </summary>
        public void SaveCollection()
        {
            // Create a list of card IDs to save
            List<string> cardIds = m_Collection.Select(c => c.Id).ToList();
            
            // Convert to JSON
            string json = JsonUtility.ToJson(new StringListWrapper(cardIds));
            
            // Save to player prefs or file
            PlayerPrefs.SetString("CardCollection", json);
            PlayerPrefs.Save();
            
            Debug.Log($"Saved collection with {cardIds.Count} cards");
        }
        
        /// <summary>
        /// Clear the player's collection
        /// </summary>
        public void ClearCollection()
        {
            m_Collection.Clear();
            
            // Notify listeners that the collection has changed
            OnCollectionChanged?.Invoke();
        }
        
        /// <summary>
        /// Check if the player owns a specific card
        /// </summary>
        public bool OwnsCard(string cardId)
        {
            return m_Collection.Any(c => c.Id == cardId);
        }
        
        /// <summary>
        /// Get the count of a specific card in the collection
        /// </summary>
        public int GetCardCount(string cardId)
        {
            return m_Collection.Count(c => c.Id == cardId);
        }
        #endregion
        
        #region Private Methods
        /// <summary>
        /// Load the player's collection from save
        /// </summary>
        private void LoadCollection()
        {
            // Clear current collection
            m_Collection.Clear();
            
            // Check if we have a saved collection
            if (PlayerPrefs.HasKey("CardCollection"))
            {
                string json = PlayerPrefs.GetString("CardCollection");
                
                // Deserialize the card IDs
                StringListWrapper wrapper = JsonUtility.FromJson<StringListWrapper>(json);
                
                if (wrapper != null && wrapper.Items != null)
                {
                    // Load each card by ID
                    foreach (string cardId in wrapper.Items)
                    {
                        CardData card = m_CardManager.GetCardById(cardId);
                        
                        if (card != null)
                        {
                            m_Collection.Add(card);
                        }
                    }
                }
                
                Debug.Log($"Loaded collection with {m_Collection.Count} cards");
            }
            
            // Notify listeners that the collection has changed
            OnCollectionChanged?.Invoke();
        }
        
        /// <summary>
        /// Create a default collection for new players
        /// </summary>
        private void CreateDefaultCollection()
        {
            if (m_CardManager == null)
                return;
                
            // Add some common cards to get started
            List<CardData> commonCards = m_CardManager.GetCardsByRarity("Common", 5);
            
            foreach (CardData card in commonCards)
            {
                AddCardToCollection(card, false);  // Don't notify for initial cards
            }
            
            Debug.Log($"Created default collection with {m_Collection.Count} cards");
            
            // Save the collection
            SaveCollection();
        }
        #endregion
    }
    
    /// <summary>
    /// Enum defining card sorting methods
    /// </summary>
    public enum SortType
    {
        ByName,
        ByRarity,
        ByCost,
        ByAttack,
        ByDefense,
        ByRecentlyAdded
    }
    
    /// <summary>
    /// Helper class for serializing string lists
    /// </summary>
    [System.Serializable]
    public class StringListWrapper
    {
        public List<string> Items;
        
        public StringListWrapper(List<string> items)
        {
            Items = items;
        }
    }
} 