using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.EventSystems;
using System;

public class ActionButton : Button
{
    [Flags]
    public enum PlatformType
    {
        None = 0,
        PC = 1 << 0,
        Mobile = 1 << 1,
        Gamepad = 1 << 2,
        All = PC | Mobile | Gamepad
    }

    [Header("Input Action Settings")]
    [SerializeField] private InputActionReference m_InputAction;
    [SerializeField] private GameObject m_InputIndicator;
    [SerializeField] private TextMeshProUGUI m_InputText;
    [SerializeField] private bool m_ShowInputBindingOnHover = true;
    [SerializeField] private bool m_AlwaysShowInputBinding = false;
    [SerializeField] private PlatformType m_SupportedPlatforms = PlatformType.All;

    private bool m_IsListening = false;
    private PlatformType m_CurrentPlatform = PlatformType.PC; // Default

    protected override void OnEnable()
    {
        base.OnEnable();

        if (m_InputAction != null)
        {
            m_InputAction.action.Enable();
            m_InputAction.action.performed += OnInputActionPerformed;
            m_IsListening = true;
        }

        // Subscribe to device change events
        InputSystem.onDeviceChange += OnDeviceChanged;
        
        // Initialize the current platform
        DetectCurrentPlatform();
        
        UpdateInputDisplay();
        UpdateIndicatorVisibility();
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (m_IsListening && m_InputAction != null)
        {
            m_InputAction.action.performed -= OnInputActionPerformed;
            m_InputAction.action.Disable();
            m_IsListening = false;
        }

        // Unsubscribe from device change events
        InputSystem.onDeviceChange -= OnDeviceChanged;
        
        ShowInputIndicator(false);
    }

    private void OnDeviceChanged(InputDevice device, InputDeviceChange change)
    {
        if (change == InputDeviceChange.Added || change == InputDeviceChange.Removed || 
            change == InputDeviceChange.Enabled || change == InputDeviceChange.Disabled)
        {
            DetectCurrentPlatform();
            UpdateIndicatorVisibility();
        }
    }

    private void DetectCurrentPlatform()
    {
        // Detect current input device/platform
        if (Gamepad.current != null && Gamepad.current.enabled)
        {
            m_CurrentPlatform = PlatformType.Gamepad;
        }
        else if (Application.isMobilePlatform || Touchscreen.current != null)
        {
            m_CurrentPlatform = PlatformType.Mobile;
        }
        else
        {
            m_CurrentPlatform = PlatformType.PC;
        }
    }

    private void UpdateIndicatorVisibility()
    {
        bool shouldShowBasedOnPlatform = (m_SupportedPlatforms & m_CurrentPlatform) != 0;
        
        if (!shouldShowBasedOnPlatform)
        {
            ShowInputIndicator(false);
            return;
        }

        if (m_AlwaysShowInputBinding)
        {
            ShowInputIndicator(true);
        }
        else if (m_ShowInputBindingOnHover)
        {
            // Don't change visibility here - it's handled in hover events
        }
        else
        {
            ShowInputIndicator(false);
        }
    }

    private void OnInputActionPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (IsInteractable() && gameObject.activeInHierarchy)
        {
            onClick.Invoke();

            // Visual feedback
            DoStateTransition(SelectionState.Pressed, false);
            StartCoroutine(DelayedNormalState());
        }
    }

    private System.Collections.IEnumerator DelayedNormalState()
    {
        yield return new WaitForSeconds(0.1f);
        DoStateTransition(currentSelectionState, false);
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);

        if (m_ShowInputBindingOnHover && !m_AlwaysShowInputBinding && 
            (m_SupportedPlatforms & m_CurrentPlatform) != 0)
        {
            ShowInputIndicator(true);
        }
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);

        if (m_ShowInputBindingOnHover && !m_AlwaysShowInputBinding)
        {
            ShowInputIndicator(false);
        }
    }

    private void ShowInputIndicator(bool show)
    {
        if (m_InputIndicator != null)
            m_InputIndicator.SetActive(show);
    }

    private void UpdateInputDisplay()
    {
        if (m_InputText != null && m_InputAction != null)
        {
            string bindingDisplayString = m_InputAction.action.GetBindingDisplayString();
            m_InputText.text = bindingDisplayString;
        }
    }

    public void SetInputAction(InputActionReference inputAction)
    {
        // Clean up old binding
        if (m_IsListening && m_InputAction != null)
        {
            m_InputAction.action.performed -= OnInputActionPerformed;
            m_InputAction.action.Disable();
            m_IsListening = false;
        }

        m_InputAction = inputAction;

        // Set up new binding
        if (m_InputAction != null && gameObject.activeInHierarchy)
        {
            m_InputAction.action.Enable();
            m_InputAction.action.performed += OnInputActionPerformed;
            m_IsListening = true;
            UpdateInputDisplay();
        }
    }

    public void SetSupportedPlatforms(PlatformType platforms)
    {
        m_SupportedPlatforms = platforms;
        UpdateIndicatorVisibility();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        UpdateInputDisplay();
    }
#endif
}