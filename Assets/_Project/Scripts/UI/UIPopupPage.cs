using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIPopupPage : UIPage
{
    [Header("Popup Components")]
    [SerializeField] private TextMeshProUGUI m_TitleText;
    [SerializeField] private TextMeshProUGUI m_MessageText;
    [SerializeField] private RectTransform m_ButtonContainer;
    [SerializeField] private Button m_ButtonPrefab;
    
    private List<Button> m_SpawnedButtons = new List<Button>();
    
    public class PopupButton
    {
        public string Text { get; }
        public Action Action { get; }
        public Sprite Icon { get; }
    
        public PopupButton(string text, Action action, Sprite icon = null)
        {
            Text = text;
            Action = action;
            Icon = icon;
        }
    }
    
    public void ConfigurePopup(string title, string message)
    {
        if (m_TitleText != null)
            m_TitleText.text = title;
            
        if (m_MessageText != null)
            m_MessageText.text = message;
    }
    
    public void SetupButtons(params PopupButton[] buttons)
    {
        // Clear any existing buttons
        foreach (var button in m_SpawnedButtons.Where(button => button != null))
        {
            Destroy(button.gameObject);
        }
    
        m_SpawnedButtons.Clear();
        foreach (Transform child in m_ButtonContainer)
        {
            Destroy(child.gameObject);
        }
    
        // Make sure we have a button prefab
        if (m_ButtonPrefab == null)
        {
            Debug.LogError("UIPopupPage: Button prefab is not assigned");
            return;
        }
    
        // Create buttons
        foreach (var buttonConfig in buttons)
        {
            var button = Instantiate(m_ButtonPrefab, m_ButtonContainer);
            var buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            var buttonIcon = button.transform.Find("Image_Icon/Icon")?.GetComponent<Image>();
        
            if (buttonText != null)
                buttonText.text = buttonConfig.Text;
            
            // Set icon if available
            if (buttonIcon != null && buttonConfig.Icon != null)
            {
                buttonIcon.sprite = buttonConfig.Icon;
                buttonIcon.gameObject.SetActive(true);
            }
            else if (buttonIcon != null)
            {
                buttonIcon.gameObject.SetActive(false);
            }
        
            // Setup click action
            button.onClick.RemoveAllListeners();
        
            if (buttonConfig.Action != null)
            {
                button.onClick.AddListener(() => 
                {
                    buttonConfig.Action?.Invoke();
                    Exit();
                });
            }
            else
            {
                // Default action for buttons without a specified action (usually Cancel)
                button.onClick.AddListener(Exit);
            }
        
            m_SpawnedButtons.Add(button);
        }
    }
    
    public override void Exit()
    {
        base.Exit();
        
        // Destroy the popup after it has exited
        StartCoroutine(DestroyAfterExit());
    }
    
    private System.Collections.IEnumerator DestroyAfterExit()
    {
        yield return new WaitForSeconds(m_AnimationDuration);
        Destroy(gameObject);
    }
}