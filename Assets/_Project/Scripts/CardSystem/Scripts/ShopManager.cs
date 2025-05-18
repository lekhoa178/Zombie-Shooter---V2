using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PowerTree;
using Unity.VisualScripting;
using UnityEngine;
using UnityUtils;
using Random = UnityEngine.Random;

namespace CardSystem
{
    /// <summary>
    /// Manages the shop system for purchasing cards
    /// </summary>
    public class ShopManager : PersistentSingleton<ShopManager>
    {
        #region Singleton
        private static ShopManager s_Instance;
        public static ShopManager Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = FindObjectOfType<ShopManager>();
                    if (s_Instance == null)
                    {
                        GameObject obj = new GameObject("ShopManager");
                        s_Instance = obj.AddComponent<ShopManager>();
                    }
                }
                return s_Instance;
            }
        }
        #endregion
        
        #region Inspector Fields
        [Header("Data Sources")]
        [SerializeField] private ShopConfig m_ShopConfig;
        
        [Header("Shop Configuration")]
        [SerializeField] private int m_SlotCount = 6;
        [SerializeField] private bool m_InitOnStart = true;
        
        [Header("Refresh Cost Settings")]
        [SerializeField] private int m_BaseRefreshCost = 50;
        [SerializeField] private int m_MaxRefreshCost = 500;
        [SerializeField] private int m_RefreshCostMultiplier = 2;
        #endregion
        
        #region Private Fields
        private List<ShopItem> m_ShopInventory = new List<ShopItem>();
        private Dictionary<string, float> m_RarityWeights = new Dictionary<string, float>();
        private bool m_IsInitialized = false;
        private int m_CurrentRefreshCost;
        private int m_RefreshCount = 0;
        #endregion
        
        #region Properties
        public IReadOnlyList<ShopItem> ShopInventory => m_ShopInventory;
        public int SlotCount => m_SlotCount;
        public int CurrentRefreshCost => m_CurrentRefreshCost;
        public int RefreshCount => m_RefreshCount;
        #endregion
        
        #region Events

        public event Action OnShopOpened;
        public event Action OnShopRefreshed;
        public event Action<string, int> OnCardPurchased; // (cardId, totalCost)
        public event Action<string> OnPurchaseFailed; // (reason)
        public event Action<int> OnRefreshCostChanged; // (newCost)
        #endregion
        
        #region Unity Methods
        protected override void Awake()
        {
            base.Awake();
            
            // Initialize refresh cost
            m_CurrentRefreshCost = m_BaseRefreshCost;
            
            if (m_ShopConfig != null)
            {
                LoadShopConfig();
            }
        }
        
        private void Start()
        {
            if (m_InitOnStart)
            {
                // Wait for the card manager to initialize
                StartCoroutine(WaitForCardManagerAndInit());
            }
        }
        
        private IEnumerator WaitForCardManagerAndInit()
        {
            while (!CardManager.Instance)
            {
                yield return null;
            }
            
            yield return null;
            
            RefreshShop();
        }
        #endregion
        
        #region Shop Configuration
        /// <summary>
        /// Loads shop configuration from ShopConfig ScriptableObject
        /// </summary>
        public void LoadShopConfig()
        {
            if (m_ShopConfig == null)
            {
                Debug.LogError("Shop config is null");
                return;
            }
            
            m_SlotCount = m_ShopConfig.SlotCount;
            
            m_RarityWeights.Clear();
            var defaultWeights = m_ShopConfig.DefaultRarityWeights;
            if (defaultWeights != null)
            {
                foreach (var weight in defaultWeights.Weights)
                {
                    m_RarityWeights[weight.Id] = weight.DropRate;
                }
            }
            
            Debug.Log($"Loaded shop configuration: {m_SlotCount} slots");
            
            m_IsInitialized = true;
        }
        
        /// <summary>
        /// Sets a new shop config
        /// </summary>
        public void SetShopConfig(ShopConfig config)
        {
            if (config == null)
            {
                Debug.LogError("Cannot set null shop config");
                return;
            }
            
            m_ShopConfig = config;
            LoadShopConfig();
        }
        #endregion
        
        #region Shop Management

        public void OpenShop()
        {
            if (!CardManager.Instance)
            {
                Debug.LogWarning("Cannot open shop: Card manager not initialized");
                return;
            }
            
            if (!m_IsInitialized)
            {
                Debug.LogWarning("Cannot open shop: Shop manager not initialized");
                return;
            }

            RefreshShop();
            
            OnShopOpened?.Invoke();
        }
        
        
        /// <summary>
        /// Refreshes the shop inventory with new items
        /// </summary>
        public void RefreshShop()
        {
            RefreshShop(false);
        }
        
        /// <summary>
        /// Refreshes the shop inventory with new items, with option to pay refresh cost
        /// </summary>
        public void RefreshShop(bool payRefreshCost)
        {
            if (!CardManager.Instance)
            {
                Debug.LogWarning("Cannot refresh shop: Card manager not initialized");
                return;
            }
            
            if (!m_IsInitialized)
            {
                Debug.LogWarning("Cannot refresh shop: Shop manager not initialized");
                return;
            }
            
            // Handle refresh cost if needed
            if (payRefreshCost && m_RefreshCount > 0)
            {
                var playerStats = PlayerStats.Instance;
                if (!playerStats)
                {
                    OnPurchaseFailed?.Invoke("Player stats not found");
                    return;
                }
                
                if (playerStats.GetGold() < m_CurrentRefreshCost)
                {
                    OnPurchaseFailed?.Invoke($"Not enough gold for refresh. Need: {m_CurrentRefreshCost}, Have: {playerStats.GetGold()}");
                    return;
                }
                
                playerStats.SpendGold(m_CurrentRefreshCost);
            }
            
            m_ShopInventory.Clear();
            
            GenerateInventory(m_SlotCount);
            
            // Increment refresh count and update cost if this isn't the first refresh
            if (payRefreshCost || m_RefreshCount > 0)
            {
                m_RefreshCount++;
                UpdateRefreshCost();
            }
            
            OnShopRefreshed?.Invoke();
        }
        
        /// <summary>
        /// Updates the refresh cost based on refresh count
        /// </summary>
        private void UpdateRefreshCost()
        {
            // Calculate new cost (doubling each time)
            int newCost = m_BaseRefreshCost;
            for (int i = 0; i < m_RefreshCount; i++)
            {
                newCost *= m_RefreshCostMultiplier;
            }
            
            // Cap the cost
            m_CurrentRefreshCost = Mathf.Min(newCost, m_MaxRefreshCost);
            
            // Notify listeners
            OnRefreshCostChanged?.Invoke(m_CurrentRefreshCost);
        }
        
        /// <summary>
        /// Gets the current cost to refresh the shop
        /// </summary>
        public int GetRefreshCost()
        {
            return m_CurrentRefreshCost;
        }
        
        /// <summary>
        /// Resets the refresh count and cost (e.g., for a new game day)
        /// </summary>
        public void ResetRefreshCost()
        {
            m_RefreshCount = 0;
            m_CurrentRefreshCost = m_BaseRefreshCost;
            OnRefreshCostChanged?.Invoke(m_CurrentRefreshCost);
        }
        
        /// <summary>
        /// Generates shop inventory items
        /// </summary>
        private void GenerateInventory(int count)
        {
            // Get available cards
            var allCards = CardManager.Instance.AvailableCards.ToList();
            
            if (m_ShopConfig)
            {
                // Add always available items
                AddShopItemsOfType(m_ShopConfig.AlwaysAvailableItems, AvailabilityType.Always);
            }
            
            // Number of regular slots to generate
            int regularSlotsNeeded = count;
            
            if (m_ShopConfig)
            {
                if (regularSlotsNeeded > 0)
                {
                    int added = AddShopItemsOfType(m_ShopConfig.RotationItems, AvailabilityType.Rotation, regularSlotsNeeded);
                    regularSlotsNeeded -= added;
                }
                
                // Then try special event items for any remaining slots
                if (regularSlotsNeeded > 0)
                {
                    int added = AddShopItemsOfType(m_ShopConfig.SpecialEventItems, AvailabilityType.SpecialEvent, regularSlotsNeeded);
                    regularSlotsNeeded -= added;
                }
            }
            
            // Generate remaining random cards
            if (regularSlotsNeeded > 0)
            {
                GenerateRandomItems(regularSlotsNeeded);
            }
            
            Debug.Log($"Generated shop inventory with {m_ShopInventory.Count} items");
        }
        
        /// <summary>
        /// Adds items of a specific type to the shop inventory
        /// </summary>
        private int AddShopItemsOfType(IReadOnlyList<ShopItemData> items, AvailabilityType type, int maxCount = int.MaxValue)
        {
            if (items == null || items.Count == 0 || maxCount <= 0)
                return 0;
                
            var availableItems = new List<ShopItemData>(items);
            
            // Shuffle the available items
            for (var i = 0; i < availableItems.Count; i++)
            {
                var randomIndex = Random.Range(i, availableItems.Count);
                (availableItems[i], availableItems[randomIndex]) = (availableItems[randomIndex], availableItems[i]);
            }
            
            int added = 0;
            
            // Add items up to the max count
            for (int i = 0; i < Mathf.Min(availableItems.Count, maxCount); i++)
            {
                ShopItemData itemData = availableItems[i];
                
                if (itemData.ItemType == ShopItemType.SingleCard && itemData.Card == null)
                    continue;
                
                var shopItem = new ShopItem(itemData);
                
                m_ShopInventory.Add(shopItem);
                added++;
            }
            
            return added;
        }
        
        /// <summary>
        /// Generates random card items for the shop
        /// </summary>
        private void GenerateRandomItems(int count)
        {
            if (count <= 0)
                return;
                
            // Get available cards
            var availableCards = new List<CardData>(CardManager.Instance.AvailableCards);
            
            // Get rarities
            var rarities = new List<Rarity>(CardManager.Instance.AvailableRarities);
            
            // Generate shop items
            int safetyCounter = 0;
            const int MAX_ATTEMPTS = 100; // Prevent infinite loops
            
            for (var i = 0; i < count; i++)
            {
                // Safety check to prevent infinite loops
                if (safetyCounter >= MAX_ATTEMPTS)
                {
                    Debug.LogWarning($"Failed to generate all shop items after {MAX_ATTEMPTS} attempts. Generated {i} of {count} items.");
                    break;
                }
                
                safetyCounter++;
                
                // Check if we have any cards left that aren't in the shop
                var remainingCards = availableCards.Where(c => !m_ShopInventory.Any(si => si.Card && si.Card.Id == c.Id)).ToList();
                if (remainingCards.Count == 0)
                {
                    Debug.LogWarning("No more unique cards available for the shop.");
                    break;
                }
                
                // Select a random rarity based on weights
                var rarity = SelectWeightedRarity(rarities);
                
                if (!rarity)
                {
                    Debug.LogWarning("Failed to select a rarity for a shop item");
                    continue;
                }
                
                // Get all cards of this rarity that aren't already in the shop
                var rarityCards = availableCards.Where(c => c.Rarity && c.Rarity.Id == rarity.Id &&
                                                            !m_ShopInventory.Any(si => si.Card && si.Card.Id == c.Id)).ToList();
                
                // If no cards of this rarity, try a different rarity instead of decrementing i
                if (rarityCards.Count == 0)
                {
                    continue; // Try again with a different rarity
                }
                
                // Select a random card
                var card = rarityCards[Random.Range(0, rarityCards.Count)];
                
                var item = new ShopItem(card);
                
                m_ShopInventory.Add(item);
                
                // Reset safety counter after successful item generation
                safetyCounter = 0;
            }
        }
        
        /// <summary>
        /// Selects a random rarity based on weights
        /// </summary>
        private Rarity SelectWeightedRarity(List<Rarity> rarities)
        {
            if (rarities == null || rarities.Count == 0)
                return null;
                
            float totalWeight = 0;
            var weights = new Dictionary<Rarity, float>();
            
            foreach (var rarity in rarities)
            {
                float weight = 0;
                
                // Use our rarity weights if we have them
                if (m_RarityWeights.TryGetValue(rarity.Id, out float configWeight))
                {
                    weight = configWeight;
                }
                // Otherwise use the rarity's drop rate
                else
                {
                    weight = rarity.DropRate;
                }
                
                weights[rarity] = weight;
                totalWeight += weight;
            }
            
            // If total weight is zero, pick a random rarity
            if (totalWeight <= 0)
            {
                return rarities[Random.Range(0, rarities.Count)];
            }
            
            var random = Random.Range(0, totalWeight);
            
            // Find the rarity at that value
            float currentWeight = 0;
            foreach (var pair in weights)
            {
                currentWeight += pair.Value;
                if (random < currentWeight)
                {
                    return pair.Key;
                }
            }
            
            return rarities[0];
        }
        
        #region Card Purchase
        /// <summary>
        /// Purchases a card from the shop
        /// </summary>
        public bool PurchaseCard(string cardId, Action onPurchaseSuccess = null, Action<string> onPurchaseFailed = null)
        {
            if (string.IsNullOrEmpty(cardId))
            {
                OnPurchaseFailed?.Invoke("Invalid card ID");
                return false;
            }
            
            // Get the card's data
            CardData card = GetCardById(cardId);
            if (card == null)
            {
                OnPurchaseFailed?.Invoke($"Card not found: {cardId}");
                onPurchaseFailed?.Invoke($"Card not found: {cardId}");
                return false;
            }
            
            // Check if it's in the shop
            ShopItem shopItem = m_ShopInventory.FirstOrDefault(i => i.Card != null && i.Card.Id == cardId);
            if (shopItem == null)
            {
                OnPurchaseFailed?.Invoke($"Card not available in the shop: {cardId}");
                onPurchaseFailed?.Invoke($"Card not available in the shop: {cardId}");
                return false;
            }
            
            // Check if player can afford it
            int totalCost = shopItem.FinalCost;
            PlayerStats playerStats = PlayerStats.Instance;
            if (playerStats == null || playerStats.GetGold() < totalCost)
            {
                OnPurchaseFailed?.Invoke($"Not enough gold. Need: {totalCost}, Have: {playerStats?.GetGold() ?? 0}");
                onPurchaseFailed?.Invoke($"Not enough gold. Need: {totalCost}, Have: {playerStats?.GetGold() ?? 0}");
                return false;
            }
            
            // Deduct gold from player
            playerStats.SpendGold(totalCost);
            
            // Add card to player's collection
            bool added = CardManager.Instance.AddCardToCollection(card);
            if (!added)
            {
                // Give back gold if card wasn't added
                playerStats.AddGold(totalCost);
                OnPurchaseFailed?.Invoke("Failed to add card to collection");
                onPurchaseFailed?.Invoke("Failed to add card to collection");
                return false;
            }
            
            // Remove from shop
            m_ShopInventory.Remove(shopItem);
            
            card.ApplyEffects(PlayerStats.Instance);
            
            // Notify
            OnCardPurchased?.Invoke(cardId, totalCost);
            onPurchaseSuccess?.Invoke();
            
            return true;
        }
        
        /// <summary>
        /// Checks if a card can be purchased
        /// </summary>
        public bool CanPurchase(string cardId, int quantity = 1)
        {
            if (string.IsNullOrEmpty(cardId) || quantity <= 0)
                return false;
                
            // Check if the card exists
            CardData card = GetCardById(cardId);
            if (card == null)
                return false;
                
            // Check if it's in the shop
            ShopItem shopItem = m_ShopInventory.FirstOrDefault(i => i.Card != null && i.Card.Id == cardId);
            if (shopItem == null)
                return false;
                
            // Check if player can afford it
            int totalCost = shopItem.FinalCost * quantity;
            PlayerStats playerStats = PlayerStats.Instance;
            if (playerStats == null || playerStats.GetGold() < totalCost)
                return false;
                
            return true;
        }
        
        /// <summary>
        /// Gets the cost of a card
        /// </summary>
        public int GetCardCost(string cardId, int quantity = 1)
        {
            if (string.IsNullOrEmpty(cardId) || quantity <= 0)
                return 0;
                
            // Check if it's in the shop
            ShopItem shopItem = m_ShopInventory.FirstOrDefault(i => i.Card != null && i.Card.Id == cardId);
            if (shopItem == null)
                return 0;
                
            return shopItem.FinalCost * quantity;
        }
        #endregion
        
        /// <summary>
        /// Gets a card by ID
        /// </summary>
        private CardData GetCardById(string cardId)
        {
            if (string.IsNullOrEmpty(cardId))
                return null;
                
            return CardManager.Instance?.GetCardById(cardId);
        }
        
        #endregion
    }
} 