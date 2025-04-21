using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    private static UIManager s_Instance;
    public static UIManager Instance => s_Instance;

    [SerializeField] private UIPage m_StartingPage;
    [SerializeField] private bool m_EnableStartingPageOnAwake = true;

    [Header("Input Bindings")]
    [SerializeField] private List<InputActionBinding> m_InputBindings = new List<InputActionBinding>();

    private Dictionary<string, UIPage> m_RegisteredPages = new Dictionary<string, UIPage>();
    private Stack<UIPage> m_PageHistory = new Stack<UIPage>();
    private UIPage m_CurrentPage;

    private void Awake()
    {
        if (s_Instance != null && s_Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        s_Instance = this;

        RegisterAllPages();

        foreach (var binding in m_InputBindings)
        {
            if (binding.InputAction != null)
            {
                // Make sure binding is processing input even when UI is active
                binding.InputAction.action.actionMap.devices = InputSystem.devices; // Ensure all devices are included
                binding.InputAction.action.Enable();
                binding.InputAction.action.performed += ctx => ExecuteAction(binding);
            }
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
}

[System.Serializable]
public class InputActionBinding
{
    [SerializeField] private InputActionReference m_InputAction;
    [SerializeField] private UIActionType m_ActionType = UIActionType.ShowPage;
    [SerializeField] private string m_PageId; // Only used when ActionType is ShowPage

    public InputActionReference InputAction => m_InputAction;
    public UIActionType ActionType => m_ActionType;
    public string PageId => m_PageId;
}

public enum UIActionType
{
    ShowPage,
    HideCurrentPage,
    GoBack
}