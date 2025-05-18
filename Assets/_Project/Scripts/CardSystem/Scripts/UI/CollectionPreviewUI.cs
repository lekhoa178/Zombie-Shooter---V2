using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CardSystem
{
    /// <summary>
    /// UI component for displaying a grid of owned cards in a compact preview format
    /// Allows quick access to the full collection UI
    /// </summary>
    public class CollectionPreviewUI : MonoBehaviour
    {
        #region Serialized Fields
        [Header("UI References")]
        [SerializeField] private Transform m_CardContainer;
        [SerializeField] private GameObject m_CardPreviewPrefab;
        [SerializeField] private Button m_ViewAllButton;
        [SerializeField] private TMP_Text m_CardCountText;
        [SerializeField] private CollectionUI m_CollectionUI;
        
        [Header("Highlight Effect")]
        [SerializeField] private Image m_HighlightImage;
        [SerializeField] private Color m_HighlightColor = new Color(1f, 0.92f, 0.016f, 0.5f);
        [SerializeField] private float m_HighlightAnimationSpeed = 2f;
        [SerializeField] private float m_HighlightMinAlpha = 0.2f;
        [SerializeField] private float m_HighlightMaxAlpha = 0.7f;
        
        [Header("Layout")]
        [SerializeField] private int m_MaxPreviewCards = 12;
        [Tooltip("When true, sorts cards by rarity (highest rarity first). When false, uses the default order.")]
        [SerializeField] private bool m_SortByRarity = true;
        [SerializeField] private GameObject m_EmptyStateMessage;
        #endregion
        
        #region Private Fields
        private CardCollection m_CardCollection;
        private List<CardPreviewUI> m_CardPreviews = new List<CardPreviewUI>();
        private float m_HighlightAnimTime = 0f;
        private bool m_IsHighlighting = false;
        #endregion
        
        #region Unity Lifecycle
        private void Awake()
        {
            m_CardCollection = CardCollection.Instance;
            if (m_CardCollection == null)
            {
                Debug.LogError("CardCollection not found. CollectionPreviewUI requires CardCollection to function.");
                this.enabled = false;
                return;
            }
            
            // Set up button listeners
            if (m_ViewAllButton != null)
            {
                m_ViewAllButton.onClick.AddListener(OnViewAllButtonClicked);
            }
            
            // Hide empty state by default
            if (m_EmptyStateMessage != null)
            {
                m_EmptyStateMessage.SetActive(false);
            }
            
            // Set up highlight
            if (m_HighlightImage != null)
            {
                m_HighlightImage.gameObject.SetActive(false);
            }

            if (m_CollectionUI == null)
            {
                m_CollectionUI = FindAnyObjectByType<CollectionUI>();
            }
        }
        
        private void OnEnable()
        {
            if (m_CardCollection != null)
            {
                m_CardCollection.OnCollectionChanged += OnCollectionChanged;
                m_CardCollection.OnCardAcquired += OnCardAcquired;
            }
            
            // Refresh preview when becoming visible
            RefreshPreview();
            
            // Start highlight animation
            StartHighlighting();
        }
        
        private void OnDisable()
        {
            if (m_CardCollection != null)
            {
                m_CardCollection.OnCollectionChanged -= OnCollectionChanged;
                m_CardCollection.OnCardAcquired -= OnCardAcquired;
            }
            
            // Stop highlight animation
            StopHighlighting();
        }
        
        private void Update()
        {
            if (m_IsHighlighting)
            {
                UpdateHighlightAnimation();
            }
        }
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Shows the collection preview
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
            RefreshPreview();
        }
        
        /// <summary>
        /// Hides the collection preview
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Opens the full collection UI
        /// </summary>
        public void OpenFullCollection()
        {
            if (m_CollectionUI != null)
            {
                m_CollectionUI.Show();
            }
        }
        
        /// <summary>
        /// Starts the highlighting effect
        /// </summary>
        public void StartHighlighting()
        {
            m_IsHighlighting = true;
            
            if (m_HighlightImage != null)
            {
                m_HighlightImage.gameObject.SetActive(true);
                m_HighlightAnimTime = 0f;
            }
        }
        
        /// <summary>
        /// Stops the highlighting effect
        /// </summary>
        public void StopHighlighting()
        {
            m_IsHighlighting = false;
            
            if (m_HighlightImage != null)
            {
                m_HighlightImage.gameObject.SetActive(false);
            }
        }
        #endregion
        
        #region Private Methods
        /// <summary>
        /// Refreshes the preview display
        /// </summary>
        private void RefreshPreview()
        {
            // Clear existing previews
            ClearPreviews();
            
            if (m_CardCollection == null)
                return;
                
            List<CardData> cardsToShow = GetCardsToPreview();
            
            // Check if collection is empty
            if (cardsToShow.Count == 0)
            {
                ShowEmptyState(true);
                return;
            }
            
            ShowEmptyState(false);
            
            // Create preview for each card
            foreach (CardData card in cardsToShow)
            {
                CreateCardPreview(card);
            }
            
            // Update card count text
            UpdateCardCountText();
        }
        
        /// <summary>
        /// Gets the list of cards to show in the preview
        /// </summary>
        private List<CardData> GetCardsToPreview()
        {
            List<CardData> collection = m_CardCollection.Collection;
            List<CardData> cardsToShow = new List<CardData>();
            
            if (collection.Count == 0)
                return cardsToShow;
                
            if (m_SortByRarity)
            {
                // Since we don't have AcquisitionDate, we'll sort by rarity instead
                // You can modify this to sort by any other property that makes sense
                List<CardData> sortedCards = new List<CardData>(collection);
                
                // Sort by rarity (higher rarity first)
                sortedCards.Sort((a, b) => {
                    // If either card has no rarity, put it at the end
                    if (a.Rarity == null) return 1;
                    if (b.Rarity == null) return -1;
                    
                    // Sort by rarity drop rate (lower drop rate means rarer)
                    return a.Rarity.DropRate.CompareTo(b.Rarity.DropRate);
                });
                
                // Take the cards up to max preview count
                int count = Mathf.Min(sortedCards.Count, m_MaxPreviewCards);
                for (int i = 0; i < count; i++)
                {
                    cardsToShow.Add(sortedCards[i]);
                }
            }
            else
            {
                // Just take the first N cards up to max preview count
                int count = Mathf.Min(collection.Count, m_MaxPreviewCards);
                for (int i = 0; i < count; i++)
                {
                    cardsToShow.Add(collection[i]);
                }
            }
            
            return cardsToShow;
        }
        
        /// <summary>
        /// Creates a preview for a card
        /// </summary>
        private CardPreviewUI CreateCardPreview(CardData card)
        {
            if (m_CardContainer == null || m_CardPreviewPrefab == null || card == null)
                return null;
                
            GameObject previewObj = Instantiate(m_CardPreviewPrefab, m_CardContainer);
            CardPreviewUI previewUI = previewObj.GetComponent<CardPreviewUI>();
            
            if (previewUI != null)
            {
                previewUI.Initialize(card);
                m_CardPreviews.Add(previewUI);
            }
            
            return previewUI;
        }
        
        /// <summary>
        /// Clears all preview UIs
        /// </summary>
        private void ClearPreviews()
        {
            foreach (Transform child in m_CardContainer)
            {
                Destroy(child.gameObject);
            }
            
            foreach (CardPreviewUI previewUI in m_CardPreviews)
            {
                if (previewUI != null)
                {
                    Destroy(previewUI.gameObject);
                }
            }
            m_CardPreviews.Clear();
        }
        
        /// <summary>
        /// Updates the card count text
        /// </summary>
        private void UpdateCardCountText()
        {
            if (m_CardCountText == null || m_CardCollection == null)
                return;
                
            int totalCards = m_CardCollection.Collection.Count;
            m_CardCountText.text = $"Cards: {totalCards}";
        }
        
        /// <summary>
        /// Shows or hides the empty state message
        /// </summary>
        private void ShowEmptyState(bool show)
        {
            if (m_EmptyStateMessage != null)
            {
                m_EmptyStateMessage.SetActive(show);
            }
            
            if (m_CardCountText != null)
            {
                m_CardCountText.gameObject.SetActive(!show);
            }
        }
        
        /// <summary>
        /// Callback for collection changes
        /// </summary>
        private void OnCollectionChanged()
        {
            RefreshPreview();
        }
        
        /// <summary>
        /// Callback for card acquisition
        /// </summary>
        private void OnCardAcquired(CardData card, bool isAdded)
        {
            RefreshPreview();
        }
        
        /// <summary>
        /// Handles view all button click
        /// </summary>
        private void OnViewAllButtonClicked()
        {
            OpenFullCollection();
        }
        
        /// <summary>
        /// Updates the highlight animation
        /// </summary>
        private void UpdateHighlightAnimation()
        {
            if (m_HighlightImage == null)
                return;
            
            m_HighlightAnimTime += Time.deltaTime;
            
            // Pulsing alpha effect
            float alpha = Mathf.Lerp(
                m_HighlightMinAlpha,
                m_HighlightMaxAlpha,
                (Mathf.Sin(m_HighlightAnimTime * m_HighlightAnimationSpeed) + 1f) * 0.5f
            );
            
            Color color = m_HighlightColor;
            color.a = alpha;
            m_HighlightImage.color = color;
        }
        #endregion
    }
} 