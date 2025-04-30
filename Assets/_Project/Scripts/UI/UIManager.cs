using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityUtils;

public class UIManager : PersistentSingleton<UIManager>
{
    [SerializeField] private UIPage m_StartingPage;
    [SerializeField] private bool m_EnableStartingPageOnAwake = true;

    [Header("Input Bindings")]
    [SerializeField] private List<InputActionBinding> m_InputBindings = new List<InputActionBinding>();
    
    [Header("Popup System")]
    [SerializeField] private GameObject m_PopupPrefab;
    [SerializeField] private Transform m_PopupContainer;
    [SerializeField] private Sprite m_AcceptIcon;
    [SerializeField] private Sprite m_DenyIcon;
    [SerializeField] private Sprite m_CancelIcon;
    [SerializeField] private Sprite m_ContinueIcon;

    private Dictionary<string, UIPage> m_RegisteredPages = new Dictionary<string, UIPage>();
    private Stack<UIPage> m_PageHistory = new Stack<UIPage>();
    private UIPage m_CurrentPage;
    private UIPopupPage m_CurrentPopup;

    protected override void Awake()
    {
        base.Awake();
        
        RegisterAllPages();

        foreach (var binding in m_InputBindings.Where(binding => binding.InputAction != null))
        {
            // Make sure binding is processing input even when UI is active
            binding.InputAction.action.actionMap.devices = InputSystem.devices; // Ensure all devices are included
            binding.InputAction.action.Enable();
            binding.InputAction.action.performed += ctx => ExecuteAction(binding);
        }


        if (m_EnableStartingPageOnAwake && m_StartingPage != null)
        {
            ShowPage(m_StartingPage.name);
        }
    }

    private void OnDestroy()
    {
        // Clean up input action callbacks
        foreach (var binding in m_InputBindings)
        {
            if (binding.InputAction != null)
            {
                binding.InputAction.action.Disable();
            }
        }
    }

    private void ExecuteAction(InputActionBinding binding)
    {
        switch (binding.ActionType)
        {
            case UIActionType.ShowPage:
                if (!string.IsNullOrEmpty(binding.PageId))
                {
                    ShowPage(binding.PageId);
                }
                break;
            case UIActionType.HideCurrentPage:
                HideCurrentPage();
                break;
            case UIActionType.GoBack:
                GoBack();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void RegisterAllPages()
    {
        var pages = FindObjectsByType<UIPage>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var page in pages)
        {
            RegisterPage(page);
            page.gameObject.SetActive(false);
        }
    }

    public void RegisterPage(UIPage page)
    {
        if (page == null) return;

        string pageId = page.name;
        m_RegisteredPages.TryAdd(pageId, page);
    }

    public void ShowPage(string pageId)
    {
        if (!m_RegisteredPages.TryGetValue(pageId, out UIPage page))
        {
            Debug.LogWarning($"UIManager: Page '{pageId}' not found.");
            return;
        }

        ShowPage(page);
    }

    public void ShowPage(UIPage page)
    {
        if (page == null) return;
        
        if (m_PageHistory.Contains(page) || m_CurrentPage == page)
        {
            Debug.LogWarning($"UIManager: Page '{page.name}' is already active or in history.");
            return;
        }

        // Hide current page if exists
        if (m_CurrentPage != null)
        {
            m_PageHistory.Push(m_CurrentPage);
            m_CurrentPage.Exit();
        }

        // Show new page
        m_CurrentPage = page;
        m_CurrentPage.gameObject.SetActive(true);
        m_CurrentPage.Enter();
    }

    public void GoBack()
    {
        if (m_PageHistory.Count == 0)
        {
            HideCurrentPage();
            return;
        }

        if (m_CurrentPage != null)
        {
            m_CurrentPage.Exit();
        }

        m_CurrentPage = m_PageHistory.Pop();
        m_CurrentPage.gameObject.SetActive(true);
        m_CurrentPage.Enter();
    }

    public void HideCurrentPage()
    {
        if (m_CurrentPage != null)
        {
            m_CurrentPage.Exit();
            m_CurrentPage = null;
        }
    }

    public UIPage GetPage(string pageId)
    {
        if (m_RegisteredPages.TryGetValue(pageId, out UIPage page))
        {
            return page;
        }
        return null;
    }
    
    #region Popup System
    
    public enum PopupType
    {
        Info,
        ContinueCancel,
        AcceptDenyCancel
    }

    public UIPopupPage ShowPopup(string title, string message, PopupType type,
        Action continueAction = null, Action acceptAction = null, Action denyAction = null,
        string continueText = "OK", string acceptText = "Accept",
        string denyText = "Deny", string cancelText = "Cancel",
        Sprite continueIcon = null, Sprite acceptIcon = null, Sprite denyIcon = null, Sprite cancelIcon = null)
    {
        if (m_CurrentPopup != null)
        {
            m_CurrentPopup.Exit();
            Destroy(m_CurrentPopup.gameObject);
        }

        if (m_PopupPrefab == null)
        {
            Debug.LogError("UIManager: Popup prefab not assigned");
            return null;
        }

        if (m_PopupContainer == null)
        {
            m_PopupContainer = transform;
        }

        // Use default icons if not provided
        continueIcon = continueIcon != null ? continueIcon : m_ContinueIcon;
        acceptIcon = acceptIcon != null ? acceptIcon : m_AcceptIcon;
        denyIcon = denyIcon != null ? denyIcon : m_DenyIcon;
        cancelIcon = cancelIcon != null ? cancelIcon : m_CancelIcon;

        var popup = Instantiate(m_PopupPrefab, m_PopupContainer);
        popup.gameObject.SetActive(false);
        popup.name = "Popup_" + System.Guid.NewGuid().ToString().Substring(0, 8);

        // Configure popup
        UIPopupPage popupPage = popup.GetComponent<UIPopupPage>();
        
        popupPage.ConfigurePopup(title, message);

        switch (type)
        {
            // Configure buttons based on popup type
            case PopupType.Info:
                popupPage.SetupButtons(
                    new UIPopupPage.PopupButton(continueText, null, continueIcon)
                );
                break;
            case PopupType.ContinueCancel:
                popupPage.SetupButtons(
                    new UIPopupPage.PopupButton(cancelText, null, cancelIcon),
                    new UIPopupPage.PopupButton(continueText, continueAction, continueIcon)
                );
                break;
            case PopupType.AcceptDenyCancel:
                popupPage.SetupButtons(
                    new UIPopupPage.PopupButton(acceptText, acceptAction, acceptIcon),
                    new UIPopupPage.PopupButton(denyText, denyAction, denyIcon),
                    new UIPopupPage.PopupButton(cancelText, null, cancelIcon)
                );
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        // Show popup
        m_CurrentPopup = popupPage;
        popup.gameObject.SetActive(true);
        popupPage.Enter();

        return popupPage;
    }

    public void ClosePopup()
    {
        if (m_CurrentPopup != null)
        {
            m_CurrentPopup.Exit();
            m_CurrentPopup = null;
        }
    }
    
    #endregion
}

[System.Serializable]
public class InputActionBinding
{
    [SerializeField] private InputActionReference inputAction;
    [SerializeField] private UIActionType actionType = UIActionType.ShowPage;
    [SerializeField] private string pageId; // Only used when ActionType is ShowPage

    public InputActionReference InputAction => inputAction;
    public UIActionType ActionType => actionType;
    public string PageId => pageId;
}

public enum UIActionType
{
    ShowPage,
    HideCurrentPage,
    GoBack
}