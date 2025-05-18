using System;
using System.Collections.Generic;
using System.Linq;
using PowerTree;
using UnityEngine;
using UnityUtils;

namespace CardSystem
{
    /// <summary>
    /// Manages the loading and handling of card definitions and selection in the game
    /// </summary>
    public class CardManager : PersistentSingleton<CardManager>
    {
        #region Events
        // Events for card selection system
        public event Action<List<CardData>> OnDeckGenerated;
        public event Action<CardData, bool> OnCardSelected;
        public event Action<List<CardData>> OnSelectionApproved;
        #endregion
        
        #region Serialized Fields
        [Header("Card Data")]
        [SerializeField] private List<CardData> m_AvailableCards = new List<CardData>();
        [SerializeField] private List<Rarity> m_AvailableRarities = new List<Rarity>();
        
        [Header("References")]
        [SerializeField] private CardCollection m_CardCollection;
        #endregion
        
        #region Private Fields
        private Dictionary<string, CardData> m_CardLookup = new Dictionary<string, CardData>();
        private Dictionary<string, Rarity> m_RarityLookup = new Dictionary<string, Rarity>();
        private List<CardData> m_ActiveDeck = new List<CardData>();  // Cards currently available for selection
        private List<CardData> m_SelectedCards = new List<CardData>(); // Cards currently selected by the player
        private int m_MaxSelections;
        #endregion
        
        #region Public Properties
        /// <summary>
        /// All available cards in the game
        /// </summary>
        public List<CardData> AvailableCards => m_AvailableCards;
        
        /// <summary>
        /// All available rarity definitions
        /// </summary>
        public List<Rarity> AvailableRarities => m_AvailableRarities;
        
        /// <summary>
        /// Maximum number of cards that can be selected in current context
        /// </summary>
        public int MaxSelections => m_MaxSelections;
        
        /// <summary>
        /// Currently selected cards in the active selection context
        /// </summary>
        public List<CardData> CurrentlySelectedCards => new List<CardData>(m_SelectedCards);
        
        /// <summary>
        /// Cards currently available for selection
        /// </summary>
        public List<CardData> ActiveDeck => new List<CardData>(m_ActiveDeck);
        #endregion
        
        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            
            // Find card collection if not assigned
            if (m_CardCollection == null)
            {
                m_CardCollection = CardCollection.Instance;
            }
            
            // Initialize lookups
            InitializeLookups();
        }
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Get a card by its ID
        /// </summary>
        public CardData GetCardById(string id)
        {
            if (string.IsNullOrEmpty(id) || !m_CardLookup.TryGetValue(id, value: out var byId))
                return null;
                
            return byId;
        }
        
        /// <summary>
        /// Get a rarity by its ID
        /// </summary>
        public Rarity GetRarityById(string id)
        {
            if (string.IsNullOrEmpty(id) || !m_RarityLookup.ContainsKey(id))
                return null;
                
            return m_RarityLookup[id];
        }
        
        /// <summary>
        /// Get all cards of a specific rarity
        /// </summary>
        public List<CardData> GetCardsByRarity(string rarityId, int maxCount = int.MaxValue)
        {
            List<CardData> result = new List<CardData>();
            
            foreach (CardData card in m_AvailableCards)
            {
                if (card.Rarity != null && card.Rarity.Id == rarityId)
                {
                    result.Add(card);
                    
                    if (result.Count >= maxCount)
                        break;
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Get a list of all available cards
        /// </summary>
        public List<CardData> GetAllCards()
        {
            return new List<CardData>(m_AvailableCards);
        }
        
        /// <summary>
        /// Generate a set of random card rewards for player selection
        /// </summary>
        public void GenerateCardReward(int numOptions, int maxSelections, string filterTag = null)
        {
            List<CardData> availablePool = new List<CardData>(m_AvailableCards);
            
            // Apply tag filter if specified
            if (!string.IsNullOrEmpty(filterTag))
            {
                availablePool = availablePool.Where(c => c.Tags.Contains(filterTag)).ToList();
            }
            
            // Shuffle the available cards
            for (int i = 0; i < availablePool.Count; i++)
            {
                int randomIndex = UnityEngine.Random.Range(i, availablePool.Count);
                (availablePool[i], availablePool[randomIndex]) = (availablePool[randomIndex], availablePool[i]);
            }
            
            // Take the first N cards, or all if we don't have enough
            List<CardData> selectedCards = availablePool.Take(Mathf.Min(numOptions, availablePool.Count)).ToList();
            
            // Set up selection context
            SetSelectionContext(selectedCards, maxSelections);
        }
        
        /// <summary>
        /// Generate card rewards from a specific list of CardData objects
        /// </summary>
        /// <param name="cardOptions">The specific cards to offer as rewards</param>
        /// <param name="maxSelections">Maximum number of cards the player can select from the offered options</param>
        /// <param name="shuffleOptions">Whether to shuffle the provided cards before offering them (default: false)</param>
        public void GenerateCardRewardFromList(List<CardData> cardOptions, int maxSelections, bool shuffleOptions = false)
        {
            if (cardOptions == null || cardOptions.Count == 0)
            {
                Debug.LogWarning("Cannot generate card rewards: No card options provided");
                return;
            }
            
            List<CardData> selectedCards = new List<CardData>(cardOptions);
            
            // Optionally shuffle the cards
            if (shuffleOptions)
            {
                for (int i = 0; i < selectedCards.Count; i++)
                {
                    int randomIndex = UnityEngine.Random.Range(i, selectedCards.Count);
                    (selectedCards[i], selectedCards[randomIndex]) = (selectedCards[randomIndex], selectedCards[i]);
                }
            }
            
            // Set up selection context
            SetSelectionContext(selectedCards, maxSelections);
        }
        
        /// <summary>
        /// Generate card rewards by providing specific card IDs
        /// </summary>
        /// <param name="cardIds">Array of card IDs to offer as rewards</param>
        /// <param name="maxSelections">Maximum number of cards the player can select from the offered options</param>
        public void GenerateCardRewardFromIds(string[] cardIds, int maxSelections)
        {
            if (cardIds == null || cardIds.Length == 0)
            {
                Debug.LogWarning("Cannot generate card rewards: No card IDs provided");
                return;
            }
            
            List<CardData> selectedCards = new List<CardData>();
            
            // Find each card by ID
            foreach (string cardId in cardIds)
            {
                CardData card = GetCardById(cardId);
                if (card != null)
                {
                    selectedCards.Add(card);
                }
                else
                {
                    Debug.LogWarning($"Card with ID '{cardId}' not found");
                }
            }
            
            if (selectedCards.Count == 0)
            {
                Debug.LogWarning("Cannot generate card rewards: None of the provided card IDs were found");
                return;
            }
            
            // Set up selection context
            SetSelectionContext(selectedCards, maxSelections);
        }
        
        /// <summary>
        /// Set up a new selection context with specified cards and max selections
        /// </summary>
        public void SetSelectionContext(List<CardData> availableCards, int maxSelections)
        {
            // Clear any existing selections
            ClearSelections();
            
            // Set active deck
            m_ActiveDeck = new List<CardData>(availableCards);
            m_MaxSelections = Mathf.Max(0, maxSelections);
            
            // Notify listeners about selection context change
            OnDeckGenerated?.Invoke(m_ActiveDeck);
        }
        
        /// <summary>
        /// Set the active deck of cards for selection
        /// </summary>
        public void SetActiveDeck(List<CardData> deck, int maxSelections)
        {
            SetSelectionContext(deck, maxSelections);
        }
        
        #region Card Selection Methods
        /// <summary>
        /// Toggle card selection state
        /// </summary>
        public bool ToggleCardSelection(CardData card)
        {
            if (card == null || !m_ActiveDeck.Contains(card))
                return false;
                
            if (m_SelectedCards.Contains(card))
            {
                return DeselectCard(card);
            }
            else
            {
                return SelectCard(card);
            }
        }
        
        /// <summary>
        /// Select a card
        /// </summary>
        public bool SelectCard(CardData card)
        {
            if (card == null || !m_ActiveDeck.Contains(card))
                return false;
            
            // Already selected
            if (m_SelectedCards.Contains(card))
                return true;
                
            // Check if we've already reached maximum selections
            if (m_SelectedCards.Count >= m_MaxSelections)
                return false;
                
            // Add to selected cards
            m_SelectedCards.Add(card);
            
            // Notify listeners
            OnCardSelected?.Invoke(card, true);
            
            return true;
        }
        
        /// <summary>
        /// Deselect a card
        /// </summary>
        public bool DeselectCard(CardData card)
        {
            if (card == null)
                return false;
            
            // Not selected
            if (!m_SelectedCards.Contains(card))
                return true;
                
            bool removed = m_SelectedCards.Remove(card);
            
            if (removed)
            {
                // Notify listeners
                OnCardSelected?.Invoke(card, false);
            }
            
            return removed;
        }
        
        /// <summary>
        /// Check if a card is selected
        /// </summary>
        public bool IsCardSelected(CardData card)
        {
            return m_SelectedCards.Contains(card);
        }
        
        /// <summary>
        /// Confirm the current selection
        /// </summary>
        public void ConfirmSelection()
        {
            if (m_SelectedCards.Count == 0)
                return;
                
            // Create a copy of the selected cards to operate on
            List<CardData> selectedCards = new List<CardData>(m_SelectedCards);
            
            // Add all selected cards to the collection
            if (m_CardCollection != null)
            {
                foreach (CardData card in selectedCards)
                {
                    m_CardCollection.AddCardToCollection(card);
                    
                    card.ApplyEffects(PlayerStats.Instance);
                }
            }
            
            // Notify listeners about selection approval
            OnSelectionApproved?.Invoke(selectedCards);
            
            // Clear the selection context
            ClearSelections();
            m_ActiveDeck.Clear();
        }
        
        /// <summary>
        /// Clear all current selections
        /// </summary>
        public void ClearSelections()
        {
            foreach (CardData card in m_SelectedCards)
            {
                OnCardSelected?.Invoke(card, false);
            }
            
            m_SelectedCards.Clear();
        }
        #endregion
        
        /// <summary>
        /// Add a card to the player's collection
        /// </summary>
        public bool AddCardToCollection(CardData card)
        {
            if (m_CardCollection == null) return false;
            m_CardCollection.AddCardToCollection(card);

            return true;

        }
        
        /// <summary>
        /// Check if the player owns a specific card
        /// </summary>
        public bool PlayerOwnsCard(string cardId)
        {
            if (m_CardCollection != null)
            {
                return m_CardCollection.OwnsCard(cardId);
            }
            
            return false;
        }
        
        /// <summary>
        /// Get the count of a specific card in the player's collection
        /// </summary>
        public int GetPlayerCardCount(string cardId)
        {
            if (m_CardCollection != null)
            {
                return m_CardCollection.GetCardCount(cardId);
            }
            
            return 0;
        }
        
        /// <summary>
        /// Save the player's card collection
        /// </summary>
        public void SaveCollection()
        {
            if (m_CardCollection != null)
            {
                m_CardCollection.SaveCollection();
            }
        }
        #endregion
        
        #region Private Methods
        /// <summary>
        /// Initialize the lookup dictionaries for quick access
        /// </summary>
        private void InitializeLookups()
        {
            m_CardLookup.Clear();
            m_RarityLookup.Clear();
            
            // Initialize rarity lookup first (cards need these)
            foreach (Rarity rarity in m_AvailableRarities)
            {
                if (!string.IsNullOrEmpty(rarity.Id) && !m_RarityLookup.ContainsKey(rarity.Id))
                {
                    m_RarityLookup.Add(rarity.Id, rarity);
                }
            }
            
            // Initialize card lookup
            foreach (CardData card in m_AvailableCards)
            {
                if (!string.IsNullOrEmpty(card.Id) && !m_CardLookup.ContainsKey(card.Id))
                {
                    m_CardLookup.Add(card.Id, card);
                }
            }
            
            Debug.Log($"CardManager initialized with {m_CardLookup.Count} cards and {m_RarityLookup.Count} rarities");
        }
        #endregion
    }
}