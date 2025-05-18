using System;
using System.Collections.Generic;
using PowerTree;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

namespace CardSystem
{
    /// <summary>
    /// UI for shop screen displaying purchasable cards
    /// </summary>
    public class ShopScreenUI : MonoBehaviour
    {
        #region Serialized Fields
        [Header("UI References")]
        [SerializeField] private UIPage m_UIPage;
        [SerializeField] private Transform m_ShopItemsContainer;
        [SerializeField] private GameObject m_CardUIPrefab;
        [SerializeField] private Button m_RefreshButton;
        [SerializeField] private Button m_CloseButton;
        [SerializeField] private TMP_Text m_GoldText;
        [SerializeField] private TMP_Text m_DiscountText;
        
        [Header("Refresh Cost UI")]
        [SerializeField] private TMP_Text m_RefreshCostText;
        [SerializeField] private GameObject m_RefreshCostContainer;
        
        [Header("Feedback")]
        [SerializeField] private GameObject m_PurchaseFeedback;
        [SerializeField] private TMP_Text m_FeedbackText;
        [SerializeField] private float m_FeedbackDuration = 2f;
        
        [Header("Cheats")]
        [SerializeField] private bool m_EnableCheats = true;
        [SerializeField] private int m_GoldCheatAmount = 100;
        [SerializeField] private int m_GoldFillAmount = 9999;
        #endregion
        
        #region Private Fields
        private ShopManager m_ShopManager;
        private PlayerStats m_PlayerStats;
        private List<CardUI> m_ShopCardUIs = new List<CardUI>();
        private Coroutine m_FeedbackCoroutine;
        #endregion
        
        #region Unity Lifecycle
        private void Awake()
        {
            m_ShopManager = ShopManager.Instance;
            if (m_ShopManager == null)
            {
                Debug.LogError("ShopManager not found. ShopScreenUI requires ShopManager to function.");
                this.enabled = false;
                return;
            }
            
            m_PlayerStats = PlayerStats.Instance;
            if (m_PlayerStats == null)
            {
                Debug.LogWarning("PlayerStats not found. ShopScreenUI will show 0 gold.");
            }
            
            // Set up button listeners
            if (m_RefreshButton != null)
            {
                m_RefreshButton.onClick.AddListener(OnRefreshButtonClicked);
            }
            
            if (m_CloseButton != null)
            {
                m_CloseButton.onClick.AddListener(OnCloseButtonClicked);
            }
            
            // Hide feedback panel
            if (m_PurchaseFeedback != null)
            {
                m_PurchaseFeedback.SetActive(false);
            }

            if (m_ShopItemsContainer)
            {
                foreach (Transform child in m_ShopItemsContainer)
                {
                    Destroy(child.gameObject);
                }
            }
        }
        
        private void OnEnable()
        {
            if (m_ShopManager != null)
            {
                m_ShopManager.OnShopOpened += OnShopOpened;
                m_ShopManager.OnShopRefreshed += OnShopRefreshed;
                m_ShopManager.OnCardPurchased += OnCardPurchased;
                m_ShopManager.OnPurchaseFailed += OnPurchaseFailed;
                m_ShopManager.OnRefreshCostChanged += OnRefreshCostChanged;
            }
            
            // Refresh shop UI when becoming active
            RefreshShopUI();
            
            // Update refresh cost display
            UpdateRefreshCostUI();
        }

        private void OnDisable()
        {
            if (m_ShopManager != null)
            {
                m_ShopManager.OnShopOpened -= OnShopOpened;
                m_ShopManager.OnShopRefreshed -= OnShopRefreshed;
                m_ShopManager.OnCardPurchased -= OnCardPurchased;
                m_ShopManager.OnPurchaseFailed -= OnPurchaseFailed;
                m_ShopManager.OnRefreshCostChanged -= OnRefreshCostChanged;
            }
        }
        
        private void Update()
        {
            // Update gold text
            if (m_GoldText && m_PlayerStats)
            {
                m_GoldText.text = m_PlayerStats.GetGold().ToString();
            }
            
            // Update discount text
            if (m_DiscountText && m_PlayerStats && m_PlayerStats.GetShopDiscountPercent() > 0)
            {
                m_DiscountText.text = $"{m_PlayerStats.GetShopDiscountPercent()}% Discount!";
                m_DiscountText.gameObject.SetActive(true);
            }
            else if (m_DiscountText)
            {
                m_DiscountText.gameObject.SetActive(false);
            }
            
            // Handle cheat keys
            if (m_EnableCheats && m_PlayerStats != null)
            {
                // Increase gold with numpad 1 or regular 1 key
                if (Input.GetKeyDown(KeyCode.Keypad1) || Input.GetKeyDown(KeyCode.Alpha1))
                {
                    m_PlayerStats.AddGold(m_GoldCheatAmount);
                    ShowFeedback($"Added {m_GoldCheatAmount} gold (Cheat)");
                }
                
                // Decrease gold with numpad 2 or regular 2 key
                if (Input.GetKeyDown(KeyCode.Keypad2) || Input.GetKeyDown(KeyCode.Alpha2))
                {
                    m_PlayerStats.SpendGold(m_GoldCheatAmount);
                    ShowFeedback($"Removed {m_GoldCheatAmount} gold (Cheat)");
                }
                
                // Fill gold with numpad 0 or regular 0 key
                if (Input.GetKeyDown(KeyCode.Keypad0) || Input.GetKeyDown(KeyCode.Alpha0))
                {
                    int currentGold = m_PlayerStats.GetGold();
                    m_PlayerStats.AddGold(m_GoldFillAmount - currentGold);
                    ShowFeedback($"Gold set to {m_GoldFillAmount} (Cheat)");
                }
            }
        }
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Shows the shop UI
        /// </summary>
        public void Show()
        {
            UIManager.Instance?.ShowPage(m_UIPage);;
            
            if (m_ShopManager != null && m_ShopManager.ShopInventory.Count == 0)
            {
                m_ShopManager.RefreshShop();
            }
            else
            {
                RefreshShopUI();
            }
        }
        
        /// <summary>
        /// Hides the shop UI
        /// </summary>
        public void Hide()
        {
            UIManager.Instance?.GoBack();
            
            if (m_PurchaseFeedback != null)
            {
                m_PurchaseFeedback.SetActive(false);
            }
        }
        
        #endregion
        
        #region Private Methods
        /// <summary>
        /// Refreshes the shop UI
        /// </summary>
        private void RefreshShopUI()
        {
            // Clear existing UIs
            ClearCardUIs();
            
            if (!m_ShopManager)
                return;
                
            // Create UIs for shop items
            foreach (var item in m_ShopManager.ShopInventory)
            {
                // Skip non-card items if any
                if (!item.Card)
                    continue;
                
                CreateShopItemUI(item, m_ShopItemsContainer, m_ShopCardUIs);
            }
        }
        
        /// <summary>
        /// Creates a UI for a shop item
        /// </summary>
        private CardUI CreateShopItemUI(ShopItem item, Transform container, List<CardUI> uiList)
        {
            if (!container || !m_CardUIPrefab)
                return null;
                
            GameObject cardObj = Instantiate(m_CardUIPrefab, container);
            CardUI cardUI = cardObj.GetComponent<CardUI>();
            
            if (cardUI != null)
            {
                cardUI.InitializeAsShopItem(item);
                
                // Handle card click (purchase)
                cardUI.OnCardClicked += () => OnShopItemClicked(cardUI, item);
                
                uiList.Add(cardUI);
            }
            
            return cardUI;
        }
        
        /// <summary>
        /// Handles shop item click
        /// </summary>
        private void OnShopItemClicked(CardUI cardUI, ShopItem item)
        {
            if (m_ShopManager == null || m_PlayerStats == null)
                return;
            
            UIManager.Instance.ShowPopup("", "Are you sure you want to purchase this card?", UIManager.PopupType.ContinueCancel,
            () =>
            {
                if (m_PlayerStats.GetGold() < item.FinalCost)
                {
                    ShowFeedback($"Not enough gold! Need {item.FinalCost} gold.");
                    UIManager.Instance.ShowPopup("", $"Not enough gold! Need {item.FinalCost} gold.", UIManager.PopupType.Info);
                    return;
                }
            
                if (item.Card != null)
                {
                    m_ShopManager.PurchaseCard(item.Card.Id, () =>
                        {
                            UIManager.Instance.ShowPopup("",
                                $"Purchased {item.Card.DisplayName} for {item.FinalCost} gold!",
                                UIManager.PopupType.Info);
                        
                        m_GoldText.text = m_PlayerStats.GetGold().ToString();
                        
                        cardUI.SetPurchased(true);
                    },
                    (message) =>
                    {
                        UIManager.Instance.ShowPopup("",
                            $"Failed to purchase {item.Card.DisplayName}: {message}",
                            UIManager.PopupType.Info);
                    });

                }
            });
            
        }
        
        /// <summary>
        /// Called when a card is purchased
        /// </summary>
        private void OnCardPurchased(string cardId, int totalCost)
        {
            CardUI purchasedCardUI = m_ShopCardUIs.FirstOrDefault(ui => ui.CardData?.Id == cardId);
            if (purchasedCardUI != null)
            {
                purchasedCardUI.SetPurchased(true);
            }
            
            // Show feedback
            ShowFeedback($"Purchased {cardId} for {totalCost} gold!");
            
            // Refresh gold display
            if (m_GoldText && m_PlayerStats)
            {
                m_GoldText.text = m_PlayerStats.GetGold().ToString();
            }
        }
        
        /// <summary>
        /// Called when a purchase fails
        /// </summary>
        private void OnPurchaseFailed(string reason)
        {
            ShowFeedback(reason);
        }
        
        /// <summary>
        /// Called when the shop is opened
        /// </summary>
        private void OnShopOpened()
        {
            Show();
        }
        
        /// <summary>
        /// Called when the shop is refreshed
        /// </summary>
        private void OnShopRefreshed()
        {
            RefreshShopUI();
        }
        
        /// <summary>
        /// Called when the refresh button is clicked
        /// </summary>
        private void OnRefreshButtonClicked()
        {
            if (m_ShopManager != null)
            {
                UIManager.Instance.ShowPopup("", "Shop refreshed!", UIManager.PopupType.ContinueCancel,
                () =>
                {
                    var refreshCost = m_ShopManager.CurrentRefreshCost;
                
                    if (m_PlayerStats != null && m_PlayerStats.GetGold() < refreshCost && m_ShopManager.RefreshCount > 0)
                    {
                        UIManager.Instance.ShowPopup("", $"You need {refreshCost} gold to refresh the shop.", UIManager.PopupType.Info);
                        return;
                    }
                
                    m_ShopManager.RefreshShop(true);             
                });
            }
        }
        
        /// <summary>
        /// Called when the close button is clicked
        /// </summary>
        private void OnCloseButtonClicked()
        {
            Hide();
        }
        
        /// <summary>
        /// Called when the refresh cost changes
        /// </summary>
        private void OnRefreshCostChanged(int newCost)
        {
            UpdateRefreshCostUI();
        }
        
        /// <summary>
        /// Updates the refresh cost UI
        /// </summary>
        private void UpdateRefreshCostUI()
        {
            if (m_RefreshCostText != null && m_ShopManager != null)
            {
                int cost = m_ShopManager.CurrentRefreshCost;
                // First refresh is free
                if (m_ShopManager.RefreshCount == 0)
                {
                    m_RefreshCostText.text = "Free";
                }
                else
                {
                    m_RefreshCostText.text = cost.ToString();
                }
            }
            
            // Show/hide refresh cost container
            if (m_RefreshCostContainer)
            {
                m_RefreshCostContainer.SetActive(true);
            }
        }
        
        /// <summary>
        /// Clears all card UIs
        /// </summary>
        private void ClearCardUIs()
        {
            // Clear shop items
            foreach (var cardUI in m_ShopCardUIs.Where(cardUI => cardUI))
            {
                Destroy(cardUI.gameObject);
            }

            m_ShopCardUIs.Clear();
        }
        
        /// <summary>
        /// Shows feedback text
        /// </summary>
        private void ShowFeedback(string message)
        {
            if (m_PurchaseFeedback == null || m_FeedbackText == null)
                return;
                
            // Set feedback text
            m_FeedbackText.text = message;
            
            // Show feedback panel
            m_PurchaseFeedback.SetActive(true);
            
            // Hide after delay
            if (m_FeedbackCoroutine != null)
            {
                StopCoroutine(m_FeedbackCoroutine);
            }
            
            m_FeedbackCoroutine = StartCoroutine(HideFeedbackAfterDelay());
        }
        
        /// <summary>
        /// Hides feedback after delay
        /// </summary>
        private System.Collections.IEnumerator HideFeedbackAfterDelay()
        {
            yield return new WaitForSeconds(m_FeedbackDuration);
            
            if (m_PurchaseFeedback != null)
            {
                m_PurchaseFeedback.SetActive(false);
            }
            
            m_FeedbackCoroutine = null;
        }
        #endregion
    }
} 