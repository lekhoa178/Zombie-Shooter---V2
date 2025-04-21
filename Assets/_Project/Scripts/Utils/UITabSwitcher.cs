using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using DG.Tweening;
using TMPro;

/// <summary>
/// Handles tab-based UI content switching with fade animations.
/// </summary>
public class UITabSwitcher : MonoBehaviour
{
    #region Enums
    /// <summary>
    /// Represents the visual state of a tab button
    /// </summary>
    private enum TabButtonState
    {
        Normal,
        Hover,
        Pressed,
        Selected
    }
    #endregion
    
    #region Serialized Fields
    [Header("References")]
    [SerializeField] private Transform m_TabContainer;
    [SerializeField] private Transform m_ContentContainer;
    
    [Header("Animation Settings")]
    [SerializeField, Range(0.1f, 1.0f)] private float m_FadeInDuration = 0.3f;
    [SerializeField, Range(0.1f, 1.0f)] private float m_FadeOutDuration = 0.3f;
    [SerializeField] private Ease m_FadeInEase = Ease.OutQuad;
    [SerializeField] private Ease m_FadeOutEase = Ease.InQuad;
    
    [Header("Button State Colors")]
    [SerializeField] private Color m_NormalColor = new Color(0.7f, 0.7f, 0.7f);
    [SerializeField] private Color m_HoverColor = new Color(0.8f, 0.8f, 0.8f);
    [SerializeField] private Color m_PressedColor = new Color(0.6f, 0.6f, 0.6f);
    [SerializeField] private Color m_SelectedColor = Color.white;
    
    [Header("Button Visual State")]
    [SerializeField] private bool m_UseStateSprites = false;
    [SerializeField] private Sprite m_NormalSprite;
    [SerializeField] private Sprite m_HoverSprite;
    [SerializeField] private Sprite m_PressedSprite;
    [SerializeField] private Sprite m_SelectedSprite;
    [SerializeField] private bool m_ScaleOnHover = true;
    [SerializeField, Range(1f, 1.2f)] private float m_HoverScaleFactor = 1.05f;
    [SerializeField] private bool m_ScaleSelected = true;
    [SerializeField, Range(1f, 1.5f)] private float m_SelectedScaleFactor = 1.1f;
    [SerializeField] private bool m_UseCustomSelectionIndicator = false;
    [SerializeField] private GameObject m_SelectionIndicatorPrefab;
    
    [Header("Text Settings")]
    [SerializeField] private bool m_CustomizeTabText = true;
    [SerializeField] private Color m_NormalTextColor = new Color(0.7f, 0.7f, 0.7f, 0.7f);
    [SerializeField] private Color m_HoverTextColor = new Color(0.8f, 0.8f, 0.8f, 0.9f);
    [SerializeField] private Color m_PressedTextColor = new Color(0.6f, 0.6f, 0.6f, 0.8f);
    [SerializeField] private Color m_SelectedTextColor = Color.white;
    [SerializeField, Range(0.5f, 1f)] private float m_NormalTextScale = 0.9f;
    [SerializeField, Range(0.8f, 1.2f)] private float m_HoverTextScale = 0.95f;
    [SerializeField, Range(0.8f, 1.2f)] private float m_PressedTextScale = 0.9f;
    [SerializeField, Range(0.8f, 1.5f)] private float m_SelectedTextScale = 1.1f;
    
    [Header("Transition Settings")]
    [SerializeField, Range(0.01f, 0.5f)] private float m_ButtonTransitionDuration = 0.1f;
    [SerializeField] private Ease m_ButtonTransitionEase = Ease.OutQuad;
    
    [Header("Sine Wave Animation")]
    [SerializeField] private bool m_UseSineWaveAnimation = false;
    [SerializeField, Range(0f, 0.2f)] private float m_SineWaveAmplitude = 0.05f;
    [SerializeField, Range(0.1f, 5f)] private float m_SineWaveFrequency = 2f;
    [SerializeField] private bool m_UseSineWaveColorAnimation = false;
    [SerializeField] private Color m_SineWaveColorMin = new Color(0.8f, 0f, 0f, 1f);  // Default to red
    [SerializeField] private Color m_SineWaveColorMax = new Color(1f, 0.8f, 0f, 1f);  // Default to yellow
    [SerializeField, Range(0.1f, 5f)] private float m_ColorFrequency = 1f;
    
    [Header("Options")]
    [SerializeField] private int m_DefaultTabIndex = 0;
    [SerializeField] private bool m_DeactivateInactiveContent = true;
    #endregion

    #region Private Fields
    private Button[] m_TabButtons;
    private CanvasGroup[] m_ContentCanvasGroups;
    private GameObject[] m_ContentGameObjects;
    private int m_CurrentTabIndex = -1;
    private Tweener m_CurrentFadeOutTween;
    private Tweener m_CurrentFadeInTween;
    private Vector3[] m_ButtonOriginalScales;
    private TextMeshProUGUI[] m_ButtonTexts;
    private Vector3[] m_TextOriginalScales;
    private Coroutine m_SineWaveCoroutine;
    private Coroutine m_SineWaveColorCoroutine;
    private GameObject m_SelectionIndicator;
    private RectTransform m_SelectionIndicatorRect;
    private Image[] m_ButtonImages;
    private TabButtonState[] m_ButtonStates;
    private bool m_IsSwitchingTab = false;
    private EventTrigger[] m_ButtonEventTriggers;
    private bool m_Initialized = false;
    private float m_InitTime;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        m_InitTime = Time.time;
        
        // Ensure colors have full alpha
        EnsureFullAlpha();
        
        // Initialize components
        InitializeComponents();
    }
    
    private void EnsureFullAlpha()
    {
        // Make sure colors have full alpha for visibility
        if (m_SineWaveColorMin.a < 1f)
        {
            m_SineWaveColorMin = new Color(m_SineWaveColorMin.r, m_SineWaveColorMin.g, m_SineWaveColorMin.b, 1f);
        }
        
        if (m_SineWaveColorMax.a < 1f)
        {
            m_SineWaveColorMax = new Color(m_SineWaveColorMax.r, m_SineWaveColorMax.g, m_SineWaveColorMax.b, 1f);
        }
    }

    private void Start()
    {
        // Mark as initialized
        m_Initialized = true;
        
        // Select default tab
        SwitchToTab(m_DefaultTabIndex);
        
        // Force animations to start after a small delay (to ensure UI is fully loaded)
        StartCoroutine(StartAnimationsDelayed(m_DefaultTabIndex));
    }
    
    private IEnumerator StartAnimationsDelayed(int tabIndex)
    {
        // Small delay to ensure UI is ready
        yield return new WaitForFixedUpdate();
        
        // Log animation start
        Debug.Log($"[UITabSwitcher] Starting initial animations for tab {tabIndex} after delay");
        
        // Explicitly start animations for default tab
        StopSineWaveAnimation();
        StopSineWaveColorAnimation();
        
        if (m_UseSineWaveAnimation)
        {
            Debug.Log($"[UITabSwitcher] Starting scale animation on tab {tabIndex} (direct call)");
            // Start sine wave scale animation (direct coroutine start)
            m_SineWaveCoroutine = StartCoroutine(AnimateButtonSineWave(tabIndex)); 
        }
        
        if (m_UseSineWaveColorAnimation && m_ButtonImages != null && tabIndex < m_ButtonImages.Length)
        {
            Debug.Log($"[UITabSwitcher] Starting color animation on tab {tabIndex} (direct call)");
            // Start sine wave color animation (direct coroutine start)
            m_SineWaveColorCoroutine = StartCoroutine(AnimateButtonColorSineWave(tabIndex));
        }
        
        // Make sure the tab is actually in selected state
        if (tabIndex < m_ButtonStates.Length)
        {
            m_ButtonStates[tabIndex] = TabButtonState.Selected;
        }
    }

    private void OnEnable()
    {
        // If we've already been initialized and have a current tab,
        // restart animations when re-enabled
        if (m_Initialized && m_CurrentTabIndex >= 0)
        {
            Debug.Log($"[UITabSwitcher] Re-enabling - restarting animations for tab {m_CurrentTabIndex}");
            StartCoroutine(StartAnimationsDelayed(m_CurrentTabIndex));
        }
    }

    private void OnDestroy()
    {
        // Cleanup tweens and coroutines
        KillCurrentAnimations();
        StopSineWaveAnimation();
        StopSineWaveColorAnimation();
        StopAllCoroutines();
    }
    #endregion

    #region Initialization
    private void InitializeComponents()
    {
        // Cache tab buttons
        m_TabButtons = m_TabContainer.GetComponentsInChildren<Button>();
        
        // Initialize button states array
        m_ButtonStates = new TabButtonState[m_TabButtons.Length];
        for (int i = 0; i < m_ButtonStates.Length; i++)
        {
            m_ButtonStates[i] = TabButtonState.Normal;
        }
        
        // Cache button images
        m_ButtonImages = new Image[m_TabButtons.Length];
        for (int i = 0; i < m_TabButtons.Length; i++)
        {
            m_ButtonImages[i] = m_TabButtons[i].GetComponent<Image>();
        }
        
        // Store original button scales
        m_ButtonOriginalScales = new Vector3[m_TabButtons.Length];
        for (int i = 0; i < m_TabButtons.Length; i++)
        {
            m_ButtonOriginalScales[i] = m_TabButtons[i].transform.localScale;
        }
        
        // Cache button texts
        if (m_CustomizeTabText)
        {
            m_ButtonTexts = new TextMeshProUGUI[m_TabButtons.Length];
            m_TextOriginalScales = new Vector3[m_TabButtons.Length];
            
            for (int i = 0; i < m_TabButtons.Length; i++)
            {
                // Look for TextMeshPro component in children
                m_ButtonTexts[i] = m_TabButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                
                if (m_ButtonTexts[i] != null)
                {
                    m_TextOriginalScales[i] = m_ButtonTexts[i].transform.localScale;
                }
            }
        }
        
        // Setup custom event triggers for button states
        SetupButtonEventTriggers();
        
        // Create selection indicator if enabled
        if (m_UseCustomSelectionIndicator && m_SelectionIndicatorPrefab != null)
        {
            CreateSelectionIndicator();
        }
        
        // Setup content panels
        SetupContentPanels();
        
        // Add click listeners to tab buttons
        for (int i = 0; i < m_TabButtons.Length; i++)
        {
            int tabIndex = i; // Create local variable to capture correct index
            m_TabButtons[i].onClick.AddListener(() => {
                SwitchToTab(tabIndex);
            });
        }
    }

    private void SetupButtonEventTriggers()
    {
        m_ButtonEventTriggers = new EventTrigger[m_TabButtons.Length];
        
        for (int i = 0; i < m_TabButtons.Length; i++)
        {
            // Get existing EventTrigger or add a new one
            EventTrigger trigger = m_TabButtons[i].GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = m_TabButtons[i].gameObject.AddComponent<EventTrigger>();
            }
            
            // Clear existing entries
            trigger.triggers.Clear();
            
            int tabIndex = i; // Local variable to capture correct index
            
            // Add PointerEnter event (hover)
            EventTrigger.Entry enterEntry = new EventTrigger.Entry();
            enterEntry.eventID = EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener((data) => {
                if (m_ButtonStates[tabIndex] != TabButtonState.Selected)
                {
                    UpdateButtonState(tabIndex, TabButtonState.Hover);
                }
            });
            trigger.triggers.Add(enterEntry);
            
            // Add PointerExit event (normal)
            EventTrigger.Entry exitEntry = new EventTrigger.Entry();
            exitEntry.eventID = EventTriggerType.PointerExit;
            exitEntry.callback.AddListener((data) => {
                if (m_ButtonStates[tabIndex] != TabButtonState.Selected)
                {
                    UpdateButtonState(tabIndex, TabButtonState.Normal);
                }
            });
            trigger.triggers.Add(exitEntry);
            
            // Add PointerDown event (pressed)
            EventTrigger.Entry downEntry = new EventTrigger.Entry();
            downEntry.eventID = EventTriggerType.PointerDown;
            downEntry.callback.AddListener((data) => {
                if (m_ButtonStates[tabIndex] != TabButtonState.Selected)
                {
                    UpdateButtonState(tabIndex, TabButtonState.Pressed);
                }
            });
            trigger.triggers.Add(downEntry);
            
            // Add PointerUp event (hover or normal)
            EventTrigger.Entry upEntry = new EventTrigger.Entry();
            upEntry.eventID = EventTriggerType.PointerUp;
            upEntry.callback.AddListener((data) => {
                // Handle in onClick, this is just to reset visual state
                if (m_ButtonStates[tabIndex] != TabButtonState.Selected)
                {
                    // Check if pointer is still over button (hover) or not (normal)
                    PointerEventData pointerData = (PointerEventData)data;
                    RectTransform rt = m_TabButtons[tabIndex].GetComponent<RectTransform>();
                    if (RectTransformUtility.RectangleContainsScreenPoint(rt, pointerData.position))
                    {
                        UpdateButtonState(tabIndex, TabButtonState.Hover);
                    }
                    else
                    {
                        UpdateButtonState(tabIndex, TabButtonState.Normal);
                    }
                }
            });
            trigger.triggers.Add(upEntry);
            
            m_ButtonEventTriggers[i] = trigger;
        }
    }

    private void SetupContentPanels()
    {
        int contentCount = m_ContentContainer.childCount;
        m_ContentCanvasGroups = new CanvasGroup[contentCount];
        m_ContentGameObjects = new GameObject[contentCount];
        
        for (int i = 0; i < contentCount; i++)
        {
            Transform contentTransform = m_ContentContainer.GetChild(i);
            GameObject contentObject = contentTransform.gameObject;
            m_ContentGameObjects[i] = contentObject;
            
            // Add canvas group component if it doesn't exist
            CanvasGroup canvasGroup = contentTransform.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = contentObject.AddComponent<CanvasGroup>();
            }
            
            // Hide all content initially
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            
            // Deactivate if option is enabled
            if (m_DeactivateInactiveContent && i != m_DefaultTabIndex)
            {
                contentObject.SetActive(false);
            }
            
            m_ContentCanvasGroups[i] = canvasGroup;
        }
    }
    
    private void CreateSelectionIndicator()
    {
        // Instantiate the indicator as a sibling to the tab buttons
        m_SelectionIndicator = Instantiate(m_SelectionIndicatorPrefab, m_TabContainer);
        
        // Get the rect transform for positioning
        m_SelectionIndicatorRect = m_SelectionIndicator.GetComponent<RectTransform>();
        
        // Initially hide it
        m_SelectionIndicator.SetActive(false);
    }
    #endregion

    #region Button State Management
    private void UpdateButtonState(int _buttonIndex, TabButtonState _newState)
    {
        if (_buttonIndex < 0 || _buttonIndex >= m_ButtonStates.Length)
            return;
            
        TabButtonState previousState = m_ButtonStates[_buttonIndex];
        
        // Only update if state has changed
        if (previousState == _newState)
            return;
            
        m_ButtonStates[_buttonIndex] = _newState;
        
        // Update visual appearance based on state
        UpdateButtonVisuals(_buttonIndex);
    }
    
    private void UpdateButtonVisuals(int _buttonIndex)
    {
        if (_buttonIndex < 0 || _buttonIndex >= m_ButtonStates.Length)
            return;
        
        TabButtonState state = m_ButtonStates[_buttonIndex];
        Button button = m_TabButtons[_buttonIndex];
        Image buttonImage = m_ButtonImages[_buttonIndex];
        
        // Don't update visuals for selected buttons if using sine wave animation
        bool skipSelectedStateVisuals = state == TabButtonState.Selected && 
                                       (m_UseSineWaveAnimation || m_UseSineWaveColorAnimation);
        
        // Update button color
        if (buttonImage != null && !skipSelectedStateVisuals)
        {
            Color targetColor = GetColorForState(state);
            buttonImage.DOColor(targetColor, m_ButtonTransitionDuration).SetEase(m_ButtonTransitionEase);
            
            // Update sprite if enabled
            if (m_UseStateSprites)
            {
                Sprite targetSprite = GetSpriteForState(state);
                if (targetSprite != null)
                {
                    buttonImage.sprite = targetSprite;
                }
            }
        }
        
        // Update scale based on state
        if (!skipSelectedStateVisuals)
        {
            Vector3 originalScale = m_ButtonOriginalScales[_buttonIndex];
            Vector3 targetScale = originalScale;
            
            // Apply scale based on state
            switch (state)
            {
                case TabButtonState.Hover:
                    if (m_ScaleOnHover)
                    {
                        targetScale = originalScale * m_HoverScaleFactor;
                    }
                    break;
                    
                case TabButtonState.Selected:
                    if (m_ScaleSelected)
                    {
                        targetScale = originalScale * m_SelectedScaleFactor;
                    }
                    break;
                    
                // Keep original scale for pressed and normal
                default:
                    targetScale = originalScale;
                    break;
            }
            
            // Animate scale change
            button.transform.DOScale(targetScale, m_ButtonTransitionDuration).SetEase(m_ButtonTransitionEase);
        }
        
        // Update text appearance if enabled
        if (m_CustomizeTabText && m_ButtonTexts != null && m_ButtonTexts[_buttonIndex] != null)
        {
            TextMeshProUGUI text = m_ButtonTexts[_buttonIndex];
            
            // Update text color
            text.DOColor(GetTextColorForState(state), m_ButtonTransitionDuration).SetEase(m_ButtonTransitionEase);
            
            // Update text scale
            if (m_TextOriginalScales != null)
            {
                Vector3 originalScale = m_TextOriginalScales[_buttonIndex];
                Vector3 targetScale = originalScale * GetTextScaleForState(state);
                text.transform.DOScale(targetScale, m_ButtonTransitionDuration).SetEase(m_ButtonTransitionEase);
            }
        }
    }
    
    private Color GetColorForState(TabButtonState _state)
    {
        switch (_state)
        {
            case TabButtonState.Normal:
                return m_NormalColor;
            case TabButtonState.Hover:
                return m_HoverColor;
            case TabButtonState.Pressed:
                return m_PressedColor;
            case TabButtonState.Selected:
                return m_SelectedColor;
            default:
                return m_NormalColor;
        }
    }
    
    private Sprite GetSpriteForState(TabButtonState _state)
    {
        switch (_state)
        {
            case TabButtonState.Normal:
                return m_NormalSprite;
            case TabButtonState.Hover:
                return m_HoverSprite;
            case TabButtonState.Pressed:
                return m_PressedSprite;
            case TabButtonState.Selected:
                return m_SelectedSprite;
            default:
                return m_NormalSprite;
        }
    }
    
    private Color GetTextColorForState(TabButtonState _state)
    {
        switch (_state)
        {
            case TabButtonState.Normal:
                return m_NormalTextColor;
            case TabButtonState.Hover:
                return m_HoverTextColor;
            case TabButtonState.Pressed:
                return m_PressedTextColor;
            case TabButtonState.Selected:
                return m_SelectedTextColor;
            default:
                return m_NormalTextColor;
        }
    }
    
    private float GetTextScaleForState(TabButtonState _state)
    {
        switch (_state)
        {
            case TabButtonState.Normal:
                return m_NormalTextScale;
            case TabButtonState.Hover:
                return m_HoverTextScale;
            case TabButtonState.Pressed:
                return m_PressedTextScale;
            case TabButtonState.Selected:
                return m_SelectedTextScale;
            default:
                return m_NormalTextScale;
        }
    }
    #endregion

    #region Tab Switching
    /// <summary>
    /// Switches to the specified tab index.
    /// </summary>
    /// <param name="_tabIndex">The index of the tab to switch to.</param>
    public void SwitchToTab(int _tabIndex)
    {
        // Validate index
        if (_tabIndex < 0 || _tabIndex >= m_TabButtons.Length || _tabIndex >= m_ContentCanvasGroups.Length)
        {
            Debug.LogWarning($"[UITabSwitcher] Tab index {_tabIndex} is out of range.");
            return;
        }
        
        // Do nothing if already on this tab
        if (_tabIndex == m_CurrentTabIndex)
        {
            return;
        }
        
        // Set switching flag to prevent multiple rapid tab changes
        m_IsSwitchingTab = true;
        
        // Stop any ongoing animations
        KillCurrentAnimations();
        StopSineWaveAnimation();
        StopSineWaveColorAnimation();
        
        // Update the button states - deselect previous, select new
        if (m_CurrentTabIndex >= 0 && m_CurrentTabIndex < m_ButtonStates.Length)
        {
            UpdateButtonState(m_CurrentTabIndex, TabButtonState.Normal);
        }
        
        UpdateButtonState(_tabIndex, TabButtonState.Selected);
        
        // Start fade out animation for current tab content (if any)
        if (m_CurrentTabIndex >= 0 && m_CurrentTabIndex < m_ContentCanvasGroups.Length)
        {
            // Make sure the current content is active before fading out
            if (!m_ContentGameObjects[m_CurrentTabIndex].activeSelf)
            {
                m_ContentGameObjects[m_CurrentTabIndex].SetActive(true);
            }
            
            int previousTabIndex = m_CurrentTabIndex;
            
            m_CurrentFadeOutTween = m_ContentCanvasGroups[m_CurrentTabIndex]
                .DOFade(0f, m_FadeOutDuration)
                .SetEase(m_FadeOutEase)
                .OnComplete(() => {
                    // Update interactable and raycast states
                    m_ContentCanvasGroups[previousTabIndex].interactable = false;
                    m_ContentCanvasGroups[previousTabIndex].blocksRaycasts = false;
                    
                    // Deactivate previous content if option is enabled
                    if (m_DeactivateInactiveContent)
                    {
                        m_ContentGameObjects[previousTabIndex].SetActive(false);
                    }
                    
                    // Start fade in for new content
                    StartFadeIn(_tabIndex);
                });
        }
        else
        {
            // No current tab, directly fade in the new one
            StartFadeIn(_tabIndex);
        }
        
        // Start animations for the new active tab
        StartTabAnimations(_tabIndex);
        
        // Update current tab index
        m_CurrentTabIndex = _tabIndex;
        
        // Position selection indicator if enabled
        if (m_UseCustomSelectionIndicator && m_SelectionIndicator != null)
        {
            PositionSelectionIndicator(_tabIndex);
        }
        
        // Clear switching flag after a short delay
        StartCoroutine(ClearSwitchingFlagAfterDelay());
    }
    
    private void StartTabAnimations(int _tabIndex)
    {
        // Skip animation if we're too early in initialization
        if (Time.time - m_InitTime < 0.1f)
        {
            Debug.Log($"[UITabSwitcher] Skipping early animation start for tab {_tabIndex}, will start in delayed coroutine");
            return;
        }
        
        // Start sine wave animation if enabled
        if (m_UseSineWaveAnimation)
        {
            StartSineWaveAnimation(_tabIndex);
        }
        
        // Start sine wave color animation if enabled
        if (m_UseSineWaveColorAnimation)
        {
            // Make sure colors are valid
            EnsureFullAlpha();
            StartSineWaveColorAnimation(_tabIndex);
        }
    }
    
    private IEnumerator ClearSwitchingFlagAfterDelay()
    {
        yield return new WaitForSecondsRealtime(0.2f);
        m_IsSwitchingTab = false;
    }
    
    private void PositionSelectionIndicator(int _tabIndex)
    {
        if (m_SelectionIndicatorRect == null) return;
        
        // Make sure the indicator is active
        m_SelectionIndicator.SetActive(true);
        
        // Get the target button's RectTransform
        RectTransform buttonRect = m_TabButtons[_tabIndex].GetComponent<RectTransform>();
        
        // Animate the indicator to the button's position
        m_SelectionIndicatorRect.DOKill();
        m_SelectionIndicatorRect.DOSizeDelta(buttonRect.sizeDelta, 0.3f);
        m_SelectionIndicatorRect.DOAnchorPos(buttonRect.anchoredPosition, 0.3f).SetEase(Ease.OutBack);
    }
    
    private void StartFadeIn(int _tabIndex)
    {
        // Activate the content before fading in
        if (!m_ContentGameObjects[_tabIndex].activeSelf)
        {
            m_ContentGameObjects[_tabIndex].SetActive(true);
        }
        
        // Make content interactable before animating
        m_ContentCanvasGroups[_tabIndex].interactable = true;
        m_ContentCanvasGroups[_tabIndex].blocksRaycasts = true;
        
        // Start fade in animation
        m_CurrentFadeInTween = m_ContentCanvasGroups[_tabIndex]
            .DOFade(1f, m_FadeInDuration)
            .SetEase(m_FadeInEase);
    }
    
    private void KillCurrentAnimations()
    {
        if (m_CurrentFadeOutTween != null && m_CurrentFadeOutTween.IsActive())
        {
            m_CurrentFadeOutTween.Kill();
        }
        
        if (m_CurrentFadeInTween != null && m_CurrentFadeInTween.IsActive())
        {
            m_CurrentFadeInTween.Kill();
        }
    }
    #endregion

    #region Sine Wave Animation
    private void StartSineWaveAnimation(int _buttonIndex)
    {
        StopSineWaveAnimation();
        
        if (_buttonIndex >= 0 && _buttonIndex < m_TabButtons.Length)
        {
            m_SineWaveCoroutine = StartCoroutine(AnimateButtonSineWave(_buttonIndex));
            
            // Log for debugging
            Debug.Log($"[UITabSwitcher] Started sine wave scale animation for tab {_buttonIndex}");
        }
    }
    
    private void StopSineWaveAnimation()
    {
        if (m_SineWaveCoroutine != null)
        {
            StopCoroutine(m_SineWaveCoroutine);
            m_SineWaveCoroutine = null;
        }
    }
    
    private IEnumerator AnimateButtonSineWave(int _buttonIndex)
    {
        if (_buttonIndex < 0 || _buttonIndex >= m_TabButtons.Length || m_TabButtons[_buttonIndex] == null)
        {
            Debug.LogWarning($"[UITabSwitcher] Invalid button index {_buttonIndex} for sine wave animation");
            yield break;
        }
        
        Button button = m_TabButtons[_buttonIndex];
        Vector3 originalScale = m_ButtonOriginalScales[_buttonIndex];
        
        float startTime = Time.time;
        
        // Log start of animation
        Debug.Log($"[UITabSwitcher] Started sine wave scale animation coroutine for tab {_buttonIndex}");
        
        // First frame notification
        bool loggedFirstFrame = false;
        
        while (true)
        {
            // Calculate sine wave factor
            float time = Time.time - startTime;
            float factor = Mathf.Sin(time * m_SineWaveFrequency * Mathf.PI) * m_SineWaveAmplitude + 1f;
            
            // Apply scale
            button.transform.localScale = originalScale * factor;
            
            // Log first frame calculation
            if (!loggedFirstFrame)
            {
                Debug.Log($"[UITabSwitcher] First sine wave scale frame for tab {_buttonIndex}: factor={factor}, scale={originalScale * factor}");
                loggedFirstFrame = true;
            }
            
            yield return null;
        }
    }
    
    private void StartSineWaveColorAnimation(int _buttonIndex)
    {
        StopSineWaveColorAnimation();
        
        if (_buttonIndex >= 0 && _buttonIndex < m_TabButtons.Length && m_ButtonImages[_buttonIndex] != null)
        {
            m_SineWaveColorCoroutine = StartCoroutine(AnimateButtonColorSineWave(_buttonIndex));
            
            // Log for debugging
            Debug.Log($"[UITabSwitcher] Started sine wave color animation for tab {_buttonIndex}");
        }
    }
    
    private void StopSineWaveColorAnimation()
    {
        if (m_SineWaveColorCoroutine != null)
        {
            StopCoroutine(m_SineWaveColorCoroutine);
            m_SineWaveColorCoroutine = null;
        }
    }
    
    private IEnumerator AnimateButtonColorSineWave(int _buttonIndex)
    {
        if (_buttonIndex < 0 || _buttonIndex >= m_ButtonImages.Length || m_ButtonImages[_buttonIndex] == null)
        {
            Debug.LogWarning($"[UITabSwitcher] Invalid button image index {_buttonIndex} for color animation");
            yield break;
        }
        
        Image buttonImage = m_ButtonImages[_buttonIndex];
        if (buttonImage == null) yield break;
        
        float startTime = Time.time;
        
        // Log color values for debugging
        Debug.Log($"[UITabSwitcher] Starting color animation with Min: {m_SineWaveColorMin}, Max: {m_SineWaveColorMax}");
        
        // Ensure full alpha on both colors
        Color colorMin = new Color(m_SineWaveColorMin.r, m_SineWaveColorMin.g, m_SineWaveColorMin.b, 1f);
        Color colorMax = new Color(m_SineWaveColorMax.r, m_SineWaveColorMax.g, m_SineWaveColorMax.b, 1f);
        
        bool loggedFirstFrame = false;
        
        while (true)
        {
            // Calculate sine wave factor (between 0 and 1)
            float time = Time.time - startTime;
            float factor = (Mathf.Sin(time * m_ColorFrequency * Mathf.PI) + 1f) * 0.5f;
            
            // Apply color
            Color newColor = Color.Lerp(colorMin, colorMax, factor);
            buttonImage.color = newColor;
            
            // Log first frame calculation
            if (!loggedFirstFrame)
            {
                Debug.Log($"[UITabSwitcher] First color animation frame for tab {_buttonIndex}: factor={factor}, color={newColor}");
                loggedFirstFrame = true;
            }
            
            yield return null;
        }
    }
    #endregion

    #if UNITY_EDITOR
    [ContextMenu("Reset All Tabs")]
    private void DebugResetAllTabs()
    {
        KillCurrentAnimations();
        StopSineWaveAnimation();
        StopSineWaveColorAnimation();
        
        // Reset all button states
        for (int i = 0; i < m_ButtonStates.Length; i++)
        {
            UpdateButtonState(i, TabButtonState.Normal);
        }
        
        m_CurrentTabIndex = -1;
        
        for (int i = 0; i < m_ContentCanvasGroups.Length; i++)
        {
            m_ContentCanvasGroups[i].alpha = 0f;
            m_ContentCanvasGroups[i].interactable = false;
            m_ContentCanvasGroups[i].blocksRaycasts = false;
            
            if (m_DeactivateInactiveContent)
            {
                m_ContentGameObjects[i].SetActive(false);
            }
        }
        
        SwitchToTab(m_DefaultTabIndex);
        
        // Force animations to run immediately for testing
        StartCoroutine(StartAnimationsDelayed(m_DefaultTabIndex));
        
        Debug.Log("[UITabSwitcher] All tabs reset.");
    }
    
    [ContextMenu("Restart Animations")]
    private void DebugRestartAnimations()
    {
        if (m_CurrentTabIndex >= 0)
        {
            StopSineWaveAnimation();
            StopSineWaveColorAnimation();
            StartCoroutine(StartAnimationsDelayed(m_CurrentTabIndex));
            Debug.Log($"[UITabSwitcher] Restarted animations for tab {m_CurrentTabIndex}.");
        }
    }
    
    [ContextMenu("Log Animation Status")]
    private void LogAnimationStatus()
    {
        string status = "[UITabSwitcher] Animation Status:\n";
        status += $"- Current Tab Index: {m_CurrentTabIndex}\n";
        status += $"- Initialized: {m_Initialized}\n";
        status += $"- Time since init: {Time.time - m_InitTime}\n";
        status += $"- Sine Wave Animation Enabled: {m_UseSineWaveAnimation}\n";
        status += $"- Sine Wave Coroutine Active: {m_SineWaveCoroutine != null}\n";
        status += $"- Color Animation Enabled: {m_UseSineWaveColorAnimation}\n";
        status += $"- Color Coroutine Active: {m_SineWaveColorCoroutine != null}\n";
        
        if (m_CurrentTabIndex >= 0 && m_CurrentTabIndex < m_ButtonStates.Length)
        {
            status += $"- Current Tab State: {m_ButtonStates[m_CurrentTabIndex]}\n";
        }
        
        Debug.Log(status);
    }
    
    [ContextMenu("Log Color Values")]
    private void LogColorValues()
    {
        Debug.Log($"[UITabSwitcher] Current color values - Min: {m_SineWaveColorMin}, Max: {m_SineWaveColorMax}");
    }
    #endif
} 