using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CardSystem
{
    /// <summary>
    /// Generic UI for card selection screens (rewards, upgrades, etc.)
    /// </summary>
    public class CardSelectionScreenUI : MonoBehaviour
    {
        #region Serialized Fields
        [Header("UI References")]
        [SerializeField] private UIPage m_UIPage;
        [SerializeField] private Transform m_CardContainer;
        [SerializeField] private GameObject m_CardUIPrefab;
        [SerializeField] private Button m_ConfirmButton;
        [SerializeField] private Button m_CancelButton;
        [SerializeField] private TMP_Text m_TitleText;
        [SerializeField] private TMP_Text m_SelectionCountText;
        [SerializeField] private TMP_Text m_InstructionText;
        [SerializeField] private Image m_HeaderBackground;
        
        [Header("Selection Settings")]
        [SerializeField] private string m_DefaultTitle = "Select Cards";
        [SerializeField] private string m_DefaultInstructionText = "Choose {0} card(s)";
        [SerializeField] private bool m_AutoConfirmWhenMaxReached = true;
        [SerializeField] private bool m_CanCancel = true;
        #endregion
        
        #region Private Fields
        private CardManager m_CardManager;
        private CardCollection m_CardCollection;
        private List<CardUI> m_CardUIs = new List<CardUI>();
        private Action<List<CardData>> m_OnSelectionConfirmed;
        private Action m_OnSelectionCancelled;
        private Color m_DefaultHeaderColor;
        #endregion
        
        #region Unity Lifecycle
        private void Awake()
        {
            m_CardManager = CardManager.Instance;
            if (m_CardManager == null)
            {
                Debug.LogError("CardManager not found. CardSelectionScreenUI requires CardManager to function.");
                this.enabled = false;
                return;
            }
            
            m_CardCollection = CardCollection.Instance;
            if (m_CardCollection == null)
            {
                Debug.LogWarning("CardCollection not found. Some functionality may be limited.");
            }
            
            // Set up button listeners
            if (m_ConfirmButton != null)
            {
                m_ConfirmButton.onClick.AddListener(OnConfirmButtonClicked);
                m_ConfirmButton.interactable = false;
            }
            
            if (m_CancelButton != null)
            {
                m_CancelButton.onClick.AddListener(OnCancelButtonClicked);
                m_CancelButton.gameObject.SetActive(m_CanCancel);
            }
            
            // Store default header color
            if (m_HeaderBackground != null)
            {
                m_DefaultHeaderColor = m_HeaderBackground.color;
            }
        }
        
        private void OnEnable()
        {
            if (m_CardManager != null)
            {
                m_CardManager.OnDeckGenerated += OnDeckGenerated;
                m_CardManager.OnSelectionApproved += OnSelectionApproved;
                m_CardManager.OnCardSelected += OnCardSelected;
            }
        }
        
        private void OnDisable()
        {
            if (m_CardManager != null)
            {
                m_CardManager.OnDeckGenerated -= OnDeckGenerated;
                m_CardManager.OnSelectionApproved -= OnSelectionApproved;
                m_CardManager.OnCardSelected -= OnCardSelected;
            }
        }
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Shows the selection screen with specified options
        /// </summary>
        /// <param name="_numOptions">Number of cards to display</param>
        /// <param name="_numSelections">Number of cards player can select</param>
        /// <param name="_onConfirmed">Callback when selection is confirmed</param>
        /// <param name="_onCancelled">Callback when selection is cancelled</param>
        /// <param name="_title">Custom title for the screen</param>
        /// <param name="_instructionText">Custom instruction text (use {0} for selections count)</param>
        /// <param name="_filterTag">Optional tag to filter card types</param>
        /// <param name="_headerColor">Optional color for the header background</param>
        public void ShowSelectionScreen(
            int _numOptions, 
            int _numSelections,
            List<CardData> _cards = null,
            Action<List<CardData>> _onConfirmed = null,
            Action _onCancelled = null,
            string _title = null, 
            string _instructionText = null,
            string _filterTag = null,
            Color? _headerColor = null)
        {
            // Store callbacks
            m_OnSelectionConfirmed = _onConfirmed;
            m_OnSelectionCancelled = _onCancelled;
            
            // Set title
            if (m_TitleText != null)
            {
                m_TitleText.text = string.IsNullOrEmpty(_title) ? m_DefaultTitle : _title;
            }
            
            // Set instruction text
            if (m_InstructionText != null)
            {
                string instructionFormat = string.IsNullOrEmpty(_instructionText) ? 
                    m_DefaultInstructionText : _instructionText;
                    
                m_InstructionText.text = string.Format(instructionFormat, _numSelections);
            }
            
            // Set header color if provided
            if (m_HeaderBackground != null && _headerColor.HasValue)
            {
                m_HeaderBackground.color = _headerColor.Value;
            }
            else if (m_HeaderBackground != null)
            {
                m_HeaderBackground.color = m_DefaultHeaderColor;
            }
            
            // Clear existing cards
            ClearCardUIs();
            
            UIManager.Instance?.ShowPage(m_UIPage);

            if (_cards == null)
            {
                m_CardManager.GenerateCardReward(_numOptions, _numSelections, _filterTag);
            }
            else
            {
                m_CardManager.GenerateCardRewardFromList(_cards, _numSelections);
            }
        }
        
        /// <summary>
        /// Shows the selection screen with predefined cards
        /// </summary>
        public void ShowWithCards(
            List<CardData> _cards, 
            int _maxSelections,
            Action<List<CardData>> _onConfirmed = null,
            Action _onCancelled = null,
            string _title = null,
            string _instructionText = null,
            Color? _headerColor = null)
        {
            // Store callbacks
            m_OnSelectionConfirmed = _onConfirmed;
            m_OnSelectionCancelled = _onCancelled;
            
            // Set title
            if (m_TitleText != null)
            {
                m_TitleText.text = string.IsNullOrEmpty(_title) ? m_DefaultTitle : _title;
            }
            
            // Set instruction text
            if (m_InstructionText != null)
            {
                string instructionFormat = string.IsNullOrEmpty(_instructionText) ? 
                    m_DefaultInstructionText : _instructionText;
                    
                m_InstructionText.text = string.Format(instructionFormat, _maxSelections);
            }
            
            // Set header color if provided
            if (m_HeaderBackground != null && _headerColor.HasValue)
            {
                m_HeaderBackground.color = _headerColor.Value;
            }
            else if (m_HeaderBackground != null)
            {
                m_HeaderBackground.color = m_DefaultHeaderColor;
            }
            
            // Clear existing cards
            ClearCardUIs();
            
            // Create card UIs
            foreach (CardData card in _cards)
            {
                CreateCardUI(card);
            }
            
            UIManager.Instance?.ShowPage(m_UIPage);
            
            // Set max selections (need to clear first, as this will be used by the CardManager)
            m_CardManager.ClearSelections();
            
            // Set the active deck in the CardManager to these cards
            m_CardManager.SetActiveDeck(_cards, _maxSelections);
        }
        
        /// <summary>
        /// Hides the selection screen
        /// </summary>
        public void Hide()
        {
            UIManager.Instance?.GoBack();
        }
        #endregion
        
        #region Private Methods
        /// <summary>
        /// Called when CardManager generates a new deck of cards
        /// </summary>
        private void OnDeckGenerated(List<CardData> deck)
        {
            // Clear existing card UIs
            ClearCardUIs();
            
            // Create card UIs for each card
            foreach (CardData card in deck)
            {
                CreateCardUI(card);
            }
            
            UpdateSelectionText();
            
            if (m_TitleText != null && string.IsNullOrEmpty(m_TitleText.text))
            {
                m_TitleText.text = m_DefaultTitle;
            }
    
            if (m_InstructionText != null)
            {
                string instructionFormat = m_DefaultInstructionText;
                m_InstructionText.text = string.Format(instructionFormat, m_CardManager.MaxSelections);
            }
        }
        
        /// <summary>
        /// Called when a card's selection state changes
        /// </summary>
        private void OnCardSelected(CardData card, bool isSelected)
        {
            // Update UI text
            UpdateSelectionText();
            
            // Find and update the card UI
            foreach (CardUI cardUI in m_CardUIs)
            {
                if (cardUI.CardData == card)
                {
                    cardUI.SetSelected(isSelected);
                    break;
                }
            }
            
            // Auto-confirm if max selections reached and option enabled
            if (m_AutoConfirmWhenMaxReached && 
                m_CardManager.CurrentlySelectedCards.Count >= m_CardManager.MaxSelections)
            {
                OnConfirmButtonClicked();
            }
        }
        
        /// <summary>
        /// Called when the selection is confirmed
        /// </summary>
        private void OnSelectionApproved(List<CardData> selectedCards)
        {
            // Hide the UI
            Hide();
            
            // Invoke callback if provided
            m_OnSelectionConfirmed?.Invoke(selectedCards);
        }
        
        /// <summary>
        /// Creates a UI for a card
        /// </summary>
        private CardUI CreateCardUI(CardData card)
        {
            if (m_CardContainer == null || m_CardUIPrefab == null)
                return null;
                
            GameObject cardObj = Instantiate(m_CardUIPrefab, m_CardContainer);
            CardUI cardUI = cardObj.GetComponent<CardUI>();
            
            if (cardUI != null)
            {
                cardUI.Initialize(card);
                
                // Handle card click
                cardUI.OnCardClicked += () => OnCardClicked(cardUI, card);
                
                // Set initial selection state
                cardUI.SetSelected(m_CardManager.IsCardSelected(card));
                
                m_CardUIs.Add(cardUI);
            }
            
            return cardUI;
        }
        
        /// <summary>
        /// Handles card click event
        /// </summary>
        private void OnCardClicked(CardUI cardUI, CardData card)
        {
            bool isSelected = m_CardManager.ToggleCardSelection(card);
        }
        
        /// <summary>
        /// Called when confirm button is clicked
        /// </summary>
        private void OnConfirmButtonClicked()
        {
            if (m_CardManager.CurrentlySelectedCards.Count == 0)
                return;
                
            m_CardManager.ConfirmSelection();
            
            
            
         }
        
        /// <summary>
        /// Called when cancel button is clicked
        /// </summary>
        private void OnCancelButtonClicked()
        {
            // Clear selections
            m_CardManager.ClearSelections();
            
            // Hide UI
            Hide();
            
            // Invoke callback if provided
            m_OnSelectionCancelled?.Invoke();
        }
        
        /// <summary>
        /// Clears all card UIs
        /// </summary>
        private void ClearCardUIs()
        {
            foreach (CardUI cardUI in m_CardUIs)
            {
                Destroy(cardUI.gameObject);
            }
            
            foreach (Transform child in m_CardContainer)
            {
                Destroy(child.gameObject);
            }
            
            m_CardUIs.Clear();
        }
        
        /// <summary>
        /// Updates the selection count text
        /// </summary>
        private void UpdateSelectionText()
        {
            if (m_SelectionCountText != null)
            {
                int current = m_CardManager.CurrentlySelectedCards.Count;
                int max = m_CardManager.MaxSelections;
                m_SelectionCountText.text = $"{current}/{max} Selected";
            }
            
            // Update confirm button state
            if (m_ConfirmButton != null)
            {
                m_ConfirmButton.interactable = m_CardManager.CurrentlySelectedCards.Count > 0;
            }
        }
        #endregion
    }
} 