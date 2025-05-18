using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CardSystem
{
    /// <summary>
    /// UI for displaying and managing the player's card collection
    /// </summary>
    public class CollectionUI : MonoBehaviour
    {
        #region Serialized Fields
        [Header("UI References")]
        [SerializeField] private UIPage m_UIPage;
        [SerializeField] private Transform m_CardContainer;
        [SerializeField] private GameObject m_CardUIPrefab;
        [SerializeField] private Button m_CloseButton;
        [SerializeField] private TMP_Text m_CardCountText;
        
        [Header("Sorting")]
        [SerializeField] private TMP_Dropdown m_SortDropdown;
        [SerializeField] private Button m_SortAscButton;
        [SerializeField] private Button m_SortDescButton;
        [SerializeField] private Image m_SortDirectionImage;
        [SerializeField] private Sprite m_AscendingSprite;
        [SerializeField] private Sprite m_DescendingSprite;
        
        [Header("Filtering")]
        [SerializeField] private TMP_Dropdown m_RarityFilterDropdown;
        [SerializeField] private TMP_InputField m_SearchInputField;
        [SerializeField] private Button m_ClearFilterButton;
        [SerializeField] private Button m_ApplyFilterButton;
        #endregion
        
        #region Private Fields
        private CardCollection m_CardCollection;
        private List<CardUI> m_CardUIs = new List<CardUI>();
        private bool m_SortDescending = false;
        private string m_CurrentSearchFilter = "";
        private Rarity m_CurrentRarityFilter = null;
        #endregion
        
        #region Unity Lifecycle
        private void Awake()
        {
            m_CardCollection = CardCollection.Instance;
            if (m_CardCollection == null)
            {
                Debug.LogError("CardCollection not found. CollectionUI requires CardCollection to function.");
                this.enabled = false;
                return;
            }
            
            // Set up button listeners
            if (m_CloseButton != null)
            {
                m_CloseButton.onClick.AddListener(OnCloseButtonClicked);
            }
            
            // Set up sort dropdown
            if (m_SortDropdown != null)
            {
                SetupSortDropdown();
                m_SortDropdown.onValueChanged.AddListener(OnSortDropdownChanged);
            }
            
            // Set up sort direction buttons
            if (m_SortAscButton != null)
            {
                m_SortAscButton.onClick.AddListener(() => SetSortDirection(false));
            }
            
            if (m_SortDescButton != null)
            {
                m_SortDescButton.onClick.AddListener(() => SetSortDirection(true));
            }
            
            // Set up filter controls
            if (m_RarityFilterDropdown != null)
            {
                SetupRarityFilterDropdown();
            }
            
            if (m_ClearFilterButton != null)
            {
                m_ClearFilterButton.onClick.AddListener(ClearFilters);
            }
            
            if (m_ApplyFilterButton != null)
            {
                m_ApplyFilterButton.onClick.AddListener(ApplyFilters);
            }
            
            if (m_SearchInputField != null)
            {
                m_SearchInputField.onEndEdit.AddListener(OnSearchChanged);
            }
        }
        
        private void OnEnable()
        {
            if (m_CardCollection != null)
            {
                m_CardCollection.OnCollectionChanged += OnCollectionChanged;
                m_CardCollection.OnCardAcquired += OnCardAcquired;
            }
            
            // Initial refresh of collection UI
            RefreshCollectionUI();
        }
        
        private void OnDisable()
        {
            if (m_CardCollection != null)
            {
                m_CardCollection.OnCollectionChanged -= OnCollectionChanged;
                m_CardCollection.OnCardAcquired -= OnCardAcquired;
            }
        }
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Shows the collection UI
        /// </summary>
        public void Show()
        {
            if (m_UIPage)
            {
                UIManager.Instance.ShowPage(m_UIPage);
            }
            
            RefreshCollectionUI();
        }
        
        /// <summary>
        /// Hides the collection UI
        /// </summary>
        public void Hide()
        {
            if (m_UIPage)
            {
                UIManager.Instance.GoBack();
            }
        }
        #endregion
        
        #region Private Methods
        /// <summary>
        /// Sets up the sort dropdown with options
        /// </summary>
        private void SetupSortDropdown()
        {
            if (m_SortDropdown == null)
                return;
                
            m_SortDropdown.ClearOptions();
            
            List<string> options = new List<string>
            {
                "Name",
                "Rarity",
                "Cost",
                "Attack",
                "Defense",
                "Recently Added"
            };
            
            m_SortDropdown.AddOptions(options);
            m_SortDropdown.value = 0; // Default to name
        }
        
        /// <summary>
        /// Sets up the rarity filter dropdown
        /// </summary>
        private void SetupRarityFilterDropdown()
        {
            if (m_RarityFilterDropdown == null)
                return;
                
            m_RarityFilterDropdown.ClearOptions();
            
            // Add "All" option first
            List<string> options = new List<string> { "All Rarities" };
            
            // Get all available rarities
            CardManager cardManager = CardManager.Instance;
            if (cardManager != null)
            {
                foreach (Rarity rarity in cardManager.AvailableRarities)
                {
                    options.Add(rarity.DisplayName);
                }
            }
            
            m_RarityFilterDropdown.AddOptions(options);
            m_RarityFilterDropdown.value = 0; // Default to "All"
        }
        
        /// <summary>
        /// Refreshes the collection UI display
        /// </summary>
        private void RefreshCollectionUI()
        {
            // Clear existing UIs
            ClearCardUIs();
            
            if (m_CardCollection == null)
                return;
                
            // Create UIs for collection cards (applying filters)
            foreach (CardData card in FilterCards(m_CardCollection.Collection))
            {
                CreateCardUI(card);
            }
            
            // Update card count text
            UpdateCardCountText();
        }
        
        /// <summary>
        /// Creates a UI for a card
        /// </summary>
        private CardUI CreateCardUI(CardData card)
        {
            if (m_CardContainer == null || m_CardUIPrefab == null || card == null)
                return null;
                
            GameObject cardObj = Instantiate(m_CardUIPrefab, m_CardContainer);
            CardUI cardUI = cardObj.GetComponent<CardUI>();
            
            if (cardUI != null)
            {
                cardUI.Initialize(card);
                
                // Handle card click (view details, etc.)
                cardUI.OnCardClicked += () => OnCardClicked(cardUI, card);
                
                m_CardUIs.Add(cardUI);
            }
            
            return cardUI;
        }
        
        /// <summary>
        /// Handles card click
        /// </summary>
        private void OnCardClicked(CardUI cardUI, CardData card)
        {
            // Here you can implement actions for when a card is clicked in the collection
            // This could be showing a detailed view, playing an animation, etc.
            
            // For now, we'll just highlight the card
            if (cardUI != null)
            {
                cardUI.SetHighlighted(true);
            }
        }
        
        /// <summary>
        /// Clears all card UIs
        /// </summary>
        private void ClearCardUIs()
        {
            foreach (Transform child in m_CardContainer)
            {
                Destroy(child.gameObject);
            }
            
            foreach (CardUI cardUI in m_CardUIs)
            {
                Destroy(cardUI.gameObject);
            }
            m_CardUIs.Clear();
        }
        
        /// <summary>
        /// Updates the card count text
        /// </summary>
        private void UpdateCardCountText()
        {
            if (m_CardCountText == null || m_CardCollection == null)
                return;
                
            int totalCards = m_CardCollection.Collection.Count;
            int filteredCards = m_CardUIs.Count;
            
            if (string.IsNullOrEmpty(m_CurrentSearchFilter) && m_CurrentRarityFilter == null)
            {
                m_CardCountText.text = $"Cards: {totalCards}";
            }
            else
            {
                m_CardCountText.text = $"Cards: {filteredCards}/{totalCards}";
            }
        }
        
        /// <summary>
        /// Callback for collection changes
        /// </summary>
        private void OnCollectionChanged()
        {
            RefreshCollectionUI();
        }
        
        /// <summary>
        /// Callback for card acquisition
        /// </summary>
        private void OnCardAcquired(CardData card, bool isAdded)
        {
            RefreshCollectionUI();
        }
        
        /// <summary>
        /// Handles close button click
        /// </summary>
        private void OnCloseButtonClicked()
        {
            Hide();
        }
        
        /// <summary>
        /// Handles sort dropdown change
        /// </summary>
        private void OnSortDropdownChanged(int index)
        {
            if (m_CardCollection == null)
                return;
                
            SortType sortType = (SortType)index;
            
            m_CardCollection.SortCollection(sortType, m_SortDescending);
            
            RefreshCollectionUI();
        }
        
        /// <summary>
        /// Sets the sort direction
        /// </summary>
        private void SetSortDirection(bool descending)
        {
            if (m_SortDirectionImage != null)
            {
                m_SortDirectionImage.sprite = descending ? m_DescendingSprite : m_AscendingSprite;
            }
            
            m_SortDescending = descending;
            
            // Re-sort using current dropdown value
            if (m_SortDropdown != null)
            {
                OnSortDropdownChanged(m_SortDropdown.value);
            }
        }
        
        /// <summary>
        /// Handles search field change
        /// </summary>
        private void OnSearchChanged(string search)
        {
            m_CurrentSearchFilter = search;
        }
        
        /// <summary>
        /// Applies the current filters
        /// </summary>
        private void ApplyFilters()
        {
            // Get the rarity filter
            if (m_RarityFilterDropdown != null && m_RarityFilterDropdown.value > 0)
            {
                CardManager cardManager = CardManager.Instance;
                if (cardManager != null && m_RarityFilterDropdown.value <= cardManager.AvailableRarities.Count)
                {
                    m_CurrentRarityFilter = cardManager.AvailableRarities[m_RarityFilterDropdown.value - 1];
                }
            }
            else
            {
                m_CurrentRarityFilter = null;
            }
            
            // Refresh the display
            RefreshCollectionUI();
        }
        
        /// <summary>
        /// Clears all filters
        /// </summary>
        private void ClearFilters()
        {
            m_CurrentSearchFilter = "";
            m_CurrentRarityFilter = null;
            
            // Reset input fields
            if (m_SearchInputField != null)
            {
                m_SearchInputField.text = "";
            }
            
            if (m_RarityFilterDropdown != null)
            {
                m_RarityFilterDropdown.value = 0;
            }
            
            // Refresh the display
            RefreshCollectionUI();
        }
        
        /// <summary>
        /// Filters cards based on current criteria
        /// </summary>
        private List<CardData> FilterCards(List<CardData> cards)
        {
            if (string.IsNullOrEmpty(m_CurrentSearchFilter) && m_CurrentRarityFilter == null)
            {
                return cards; // No filtering needed
            }
            
            List<CardData> filteredCards = new List<CardData>();
            
            foreach (CardData card in cards)
            {
                bool matchesSearch = string.IsNullOrEmpty(m_CurrentSearchFilter) ||
                                     card.DisplayName.ToLower().Contains(m_CurrentSearchFilter.ToLower()) ||
                                     card.GetProcessedDescription().ToLower().Contains(m_CurrentSearchFilter.ToLower());
                                     
                bool matchesRarity = m_CurrentRarityFilter == null || card.Rarity == m_CurrentRarityFilter;
                
                if (matchesSearch && matchesRarity)
                {
                    filteredCards.Add(card);
                }
            }
            
            return filteredCards;
        }
        #endregion
    }
} 