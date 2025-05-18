using System;
using PowerTree;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace CardSystem
{
    /// <summary>
    /// UI component for displaying a card
    /// </summary>
    public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        #region Events
        public event Action OnCardClicked;
        #endregion
        
        #region Serialized Fields
        [Header("Card Display")]
        [SerializeField] private Image m_CardBackground;
        [SerializeField] private Image m_CardIcon;
        [SerializeField] private Image m_RarityFrame;
        [SerializeField] private Image m_RarityIcon;
        [SerializeField] private TMP_Text m_DisplayNameText;
        [SerializeField] private TMP_Text m_DescriptionText;
        
        [Header("Selection UI")]
        [SerializeField] private GameObject m_SelectedIndicator;
        [SerializeField] private Button m_CardButton;
        [SerializeField] private Image m_HighlightImage;
        [SerializeField] private Image m_SelectionOutline;
        [SerializeField] private float m_SelectedScale = 1.1f;
        
        [Header("Shop Display")]
        [SerializeField] private GameObject m_PriceTag;
        [SerializeField] private TMP_Text m_PriceText;
        [SerializeField] private TMP_Text m_DiscountText;
        [SerializeField] private GameObject m_PurchasedIcon;
        
        [Header("Colors")]
        [SerializeField] private Color m_HighlightColor = new Color(1f, 0.92f, 0.016f, 0.5f);
        [SerializeField] private Color m_SelectedColor = new Color(0f, 1f, 0f, 0.8f);
        [SerializeField] private Color m_DisabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        
        [Header("Tooltip")]
        [SerializeField] private GameObject m_TooltipPrefab;

        
        [Header("Selection Animation")]
        [SerializeField] private float m_PulseSpeed = 2f;
        [SerializeField] private float m_PulseMinScale = 0.9f;
        [SerializeField] private float m_PulseMaxScale = 1.1f;
        [SerializeField] private Color m_SelectionColorAlt = new Color(0.1f, 0.8f, 0.1f, 0.8f);
        [SerializeField] private float m_ColorPulseSpeed = 1.5f;
        
        #endregion
        
        #region Private Fields
        private CardData m_CardData;
        private ShopItem m_ShopItem;
        private bool m_IsSelected = false;
        private bool m_IsInteractable = true;
        private bool m_IsPurchased = false;
        private GameObject m_ActiveTooltip;
        private Canvas m_RootCanvas;
        
        // Tilt animation fields
        private bool m_IsHovered = false;
        private Vector3 m_CurrentRotation;
        private Vector3 m_TargetRotation;
        private Vector3 m_RotationVelocity;
        private Vector2 m_NormalizedMousePos;
        private float m_IdleAnimationTime;
        
        private bool m_AnimatingSelection = false;
        private float m_SelectionAnimTime = 0f;
        #endregion
        
        #region Properties
        public CardData CardData => m_CardData;
        public ShopItem ShopItem => m_ShopItem;
        
        public bool IsSelected
        {
            get => m_IsSelected;
            set => SetSelected(value);
        }
        
        public bool IsInteractable
        {
            get => m_IsInteractable;
            set => SetInteractable(value);
        }
        #endregion
        
        #region Unity Lifecycle
        private void Awake()
        {
            // Find root canvas for proper tooltip positioning
            m_RootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
            
            // Add button listener
            if (m_CardButton != null)
            {
                m_CardButton.onClick.AddListener(OnButtonClicked);
            }
            
            // Hide selection UI by default
            if (m_SelectedIndicator != null)
            {
                m_SelectedIndicator.SetActive(false);
            }
            
            if (m_SelectionOutline != null)
            {
                m_SelectionOutline.gameObject.SetActive(false);
            }
            
            // Hide shop elements by default
            if (m_PriceTag != null)
            {
                m_PriceTag.SetActive(false);
            }
            
            // Hide purchased icon by default
            if (m_PurchasedIcon != null)
            {
                m_PurchasedIcon.SetActive(false);
            }
        }
        
        private void Update()
        {
            UpdateSelectionAnimation();
        }
        
        private void OnDestroy()
        {
            // Clean up tooltip if it exists
            DestroyTooltip();
        }
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Initialize the card UI with card data
        /// </summary>
        public void Initialize(CardData cardData)
        {
            m_CardData = cardData;
            m_ShopItem = null;
            
            if (cardData == null)
            {
                Debug.LogWarning("Trying to initialize CardUI with null CardData");
                return;
            }
            
            // Set card visuals
            UpdateCardVisuals();
            
            // Hide shop elements
            if (m_PriceTag != null)
            {
                m_PriceTag.SetActive(false);
            }
        }
        
        
        /// <summary>
        /// Initialize the card UI as a shop item
        /// </summary>
        public void InitializeAsShopItem(ShopItem shopItem)
        {
            m_ShopItem = shopItem;
            
            if (shopItem == null)
            {
                Debug.LogWarning("Trying to initialize CardUI with null ShopItem");
                return;
            }
            
            if (shopItem.Card)
            {
                m_CardData = shopItem.Card;
                UpdateCardVisuals();
            }
            else if (shopItem.ItemData)
            {
                m_CardData = null;
                
                UpdateItemVisuals();
            }
            
            if (m_PriceTag)
            {
                m_PriceTag.SetActive(true);
            }
            
            if (m_PriceText)
            {
                var discountPercent = (int)(PlayerStats.Instance.GetShopDiscountPercent() * 100);
                
                if (discountPercent > 0)
                {
                    
                    m_PriceText.text = $"<s><color=#AAAAAA>{shopItem.OriginalCost}</color></s> {shopItem.FinalCost}";
                }
                else
                {
                    m_PriceText.text = shopItem.FinalCost.ToString();
                }
            }
            
            // Show discount if any
            if (m_DiscountText)
            {
                var discountPercent = (int)(PlayerStats.Instance.GetShopDiscountPercent() * 100);
                
                if (discountPercent > 0)
                {
                    m_DiscountText.text = $"-{discountPercent}%";
                    m_DiscountText.gameObject.SetActive(true);
                }
                else
                {
                    m_DiscountText.gameObject.SetActive(false);
                }
            }
        }

        public void SetSelected(bool selected)
        {
            m_IsSelected = selected;
            m_AnimatingSelection = selected;

            if (m_SelectedIndicator != null)
            {
                m_SelectedIndicator.SetActive(selected);
                m_SelectionAnimTime = 0f;
            }

            if (m_SelectionOutline != null)
            {
                m_SelectionOutline.gameObject.SetActive(selected);
                m_SelectionOutline.color = m_SelectedColor;
            }

            if (transform != null)
            {
                transform.localScale = selected ? Vector3.one * m_SelectedScale : Vector3.one;
            }

            if (m_HighlightImage != null && m_HighlightImage.gameObject.activeSelf)
            {
                m_HighlightImage.color = selected ? m_SelectedColor : m_HighlightColor;
            }

            if (transform != null)
            {
                Vector3 position = transform.localPosition;
                position.z = selected ? -1f : 0f;
                transform.localPosition = position;
            }
        }

        private void UpdateSelectionAnimation()
        {
            if (!m_AnimatingSelection || !m_SelectionOutline)
                return;

            m_SelectionAnimTime += Time.deltaTime;

            // Animate indicator scale
            float pulseScale = Mathf.Lerp(
                m_PulseMinScale,
                m_PulseMaxScale,
                (Mathf.Sin(m_SelectionAnimTime * m_PulseSpeed) + 1f) * 0.5f
            );
            m_SelectionOutline.transform.localScale = Vector3.one * pulseScale;

            // Animate outline color if present
            if (m_SelectionOutline && m_SelectionOutline.gameObject.activeSelf)
            {
                m_SelectionOutline.color = Color.Lerp(
                    m_SelectedColor,
                    m_SelectionColorAlt,
                    (Mathf.Sin(m_SelectionAnimTime * m_ColorPulseSpeed) + 1f) * 0.5f
                );
            }
        }

        /// <summary>
        /// Set whether the card is interactable
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            m_IsInteractable = interactable;
            
            // Update button state
            if (m_CardButton != null)
            {
                m_CardButton.interactable = interactable;
            }
            
            // Update visuals
            if (m_CardBackground != null)
            {
                m_CardBackground.color = interactable ? Color.white : m_DisabledColor;
            }
        }
        
        /// <summary>
        /// Marks the card as purchased and updates visuals accordingly
        /// </summary>
        public void SetPurchased(bool purchased)
        {
            m_IsPurchased = purchased;
            
            // Make non-interactable when purchased
            if (purchased)
            {
                SetInteractable(false);
            }
            
            // Show purchased icon and hide price tag
            if (m_PurchasedIcon != null)
            {
                m_PurchasedIcon.SetActive(purchased);
            }
            
            if (m_PriceTag != null)
            {
                m_PriceTag.SetActive(!purchased);
            }
        }
        
        /// <summary>
        /// Set the card as highlighted (hover effect)
        /// </summary>
        public void SetHighlighted(bool highlighted)
        {
            // Only activate highlight if card isn't selected (to avoid conflicting visuals)
            if (m_HighlightImage != null)
            {
                if (m_IsSelected)
                {
                    // If selected, don't show highlight overlay but still track hover state
                    m_HighlightImage.gameObject.SetActive(false);
                }
                else
                {
                    // If not selected, show highlight as normal
                    m_HighlightImage.gameObject.SetActive(highlighted);
                    m_HighlightImage.color = m_HighlightColor;
                }
            }
            
            m_IsHovered = highlighted;
        }
        #endregion
        
        #region IPointerHandlers Implementation
        public void OnPointerEnter(PointerEventData eventData)
        {
            m_IsHovered = true;
            SetHighlighted(true);
            ShowTooltip();
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            m_IsHovered = false;
            SetHighlighted(false);
            HideTooltip();
        }
        #endregion
        
        #region Private Methods
        /// <summary>
        /// Updates the card visuals based on the card data
        /// </summary>
        private void UpdateCardVisuals()
        {
            if (!m_CardData)
                return;
                
            if (m_DisplayNameText)
            {
                m_DisplayNameText.text = m_CardData.DisplayName;
            }
            
            if (m_DescriptionText)
            {
                m_DescriptionText.text = m_CardData.GetProcessedDescription();
            }
            
            if (m_CardIcon && m_CardData.Icon)
            {
                m_CardIcon.sprite = m_CardData.Icon;
                m_CardIcon.gameObject.SetActive(true);
            }
            else if (m_CardIcon)
            {
                m_CardIcon.gameObject.SetActive(false);
            }
            
            if (m_RarityFrame && m_CardData.Rarity)
            {
                m_RarityFrame.color = m_CardData.Rarity.Color;
            }
            
            if (m_RarityIcon && m_CardData.Rarity.RarityIcon)
            {
                m_RarityIcon.sprite = m_CardData.Rarity.RarityIcon;
                m_RarityIcon.gameObject.SetActive(true);
            }
            else if (m_RarityIcon)
            {
                m_RarityIcon.gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// Updates the item visuals based on the shop item data
        /// </summary>
        private void UpdateItemVisuals()
        {
            if (m_ShopItem == null || !m_ShopItem.ItemData)
                return;
                
            // Set item name
            if (m_DisplayNameText)
            {
                m_DisplayNameText.text = m_ShopItem.ItemData.DisplayName;
            }
            
            // Set item description
            if (m_DescriptionText)
            {
                m_DescriptionText.text = m_ShopItem.ItemData.Description;
            }
            
            // Set item icon
            if (m_CardIcon && m_ShopItem.ItemData.Icon)
            {
                m_CardIcon.sprite = m_ShopItem.ItemData.Icon;
                m_CardIcon.gameObject.SetActive(true);
            }
            else if (m_CardIcon)
            {
                m_CardIcon.gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// Called when the card button is clicked
        /// </summary>
        private void OnButtonClicked()
        {
            if (!m_IsInteractable)
                return;
                
            // Invoke the card clicked event
            OnCardClicked?.Invoke();
        }
        
        /// <summary>
        /// Shows a tooltip with detailed card information
        /// </summary>
        private void ShowTooltip()
        {
            // Don't show tooltip if card is not interactable
            if (!m_IsInteractable)
                return;
                
            // Don't show tooltip if there's no data to display
            if (m_CardData == null && m_ShopItem?.ItemData == null)
                return;
                
            // Don't show tooltip if prefab is missing
            if (m_TooltipPrefab == null)
                return;
                
            // Create tooltip if it doesn't exist
            if (m_ActiveTooltip == null && m_RootCanvas != null)
            {
                m_ActiveTooltip = Instantiate(m_TooltipPrefab, m_RootCanvas.transform);
                
                // Position tooltip near the card
                RectTransform tooltipRect = m_ActiveTooltip.GetComponent<RectTransform>();
                if (tooltipRect != null)
                {
                    // Position to the right of the card
                    RectTransform cardRect = GetComponent<RectTransform>();
                    tooltipRect.position = cardRect.position + new Vector3(cardRect.rect.width + tooltipRect.rect.width * 0.5f, 0, 0);
                    
                    // Check if it's off-screen to the right, if so move to the left of the card
                    if (tooltipRect.position.x + tooltipRect.rect.width * 0.5f > Screen.width)
                    {
                        tooltipRect.position = cardRect.position - new Vector3(cardRect.rect.width + tooltipRect.rect.width * 0.5f, 0, 0);
                    }
                }
                
                // Initialize tooltip with card or item data
                if (m_CardData != null)
                {
                    CardTooltip cardTooltip = m_ActiveTooltip.GetComponent<CardTooltip>();
                    if (cardTooltip != null)
                    {
                        cardTooltip.Initialize(m_CardData);
                    }
                }
                else if (m_ShopItem?.ItemData != null)
                {
                    CardTooltip cardTooltip = m_ActiveTooltip.GetComponent<CardTooltip>();
                    if (cardTooltip != null)
                    {
                        cardTooltip.Initialize(m_ShopItem.ItemData);
                    }
                }
            }
        }
        
        /// <summary>
        /// Hides the tooltip
        /// </summary>
        private void HideTooltip()
        {
            DestroyTooltip();
        }
        
        /// <summary>
        /// Destroys the tooltip if it exists
        /// </summary>
        private void DestroyTooltip()
        {
            if (m_ActiveTooltip != null)
            {
                Destroy(m_ActiveTooltip);
                m_ActiveTooltip = null;
            }
        }
        #endregion
    }
    
    /// <summary>
    /// Simple tooltip component for displaying card details
    /// </summary>
    public class CardTooltip : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_TitleText;
        [SerializeField] private TMP_Text m_RarityText;
        [SerializeField] private TMP_Text m_StatsText;
        [SerializeField] private TMP_Text m_EffectsText;
        [SerializeField] private TMP_Text m_FlavorText;
        
        /// <summary>
        /// Initialize with card data
        /// </summary>
        public void Initialize(CardData cardData)
        {
            if (cardData == null)
                return;
                
            if (m_TitleText != null)
            {
                m_TitleText.text = cardData.DisplayName;
            }
            
            if (m_RarityText != null && cardData.Rarity != null)
            {
                m_RarityText.text = cardData.Rarity.DisplayName;
                m_RarityText.color = cardData.Rarity.Color;
            }
            
            if (m_StatsText != null)
            {
                m_StatsText.text = $"Value: {cardData.GoldCost} gold";
            }
            
            if (m_EffectsText != null)
            {
                m_EffectsText.text = cardData.GetProcessedDescription();
            }
            
            if (m_FlavorText != null)
            {
                m_FlavorText.text = cardData.FlavorText;
                m_FlavorText.gameObject.SetActive(!string.IsNullOrEmpty(cardData.FlavorText));
            }
        }
        
        /// <summary>
        /// Initialize with shop item data
        /// </summary>
        public void Initialize(ShopItemData itemData)
        {
            if (itemData == null)
                return;
                
            if (m_TitleText != null)
            {
                m_TitleText.text = itemData.DisplayName;
            }
            
            if (m_RarityText != null)
            {
                m_RarityText.text = $"Shop Item - {itemData.ItemType}";
                m_RarityText.color = Color.yellow;
            }
            
            if (m_StatsText != null)
            {
                m_StatsText.text = $"Base Price: {itemData.GoldCost} gold";
            }
            
            if (m_EffectsText != null)
            {
                m_EffectsText.text = itemData.Description;
            }
            
            if (m_FlavorText != null)
            {
                m_FlavorText.gameObject.SetActive(false);
            }
        }
    }
} 