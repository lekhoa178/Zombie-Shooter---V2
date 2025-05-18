using System;
using System.Collections.Generic;
using PowerTree;
using TMPro;
using UnityEngine;

public class UIAttributeMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject attributeRowPrefab;
    [SerializeField] private Transform attributeContainer;
    [SerializeField] private PlayerStats playerStats;

    [Header("Settings")]
    [SerializeField] private bool updateEveryFrame = false;
    [SerializeField] private float updateInterval = 0.5f;
    
    private List<AttributeRow> attributeRows = new List<AttributeRow>();
    private float updateTimer = 0f;

    private void Start()
    {
        if (playerStats == null)
        {
            playerStats = FindAnyObjectByType<PlayerStats>();
            if (playerStats == null)
            {
                Debug.LogError("UIAttributeMenu: Could not find PlayerStats component");
                return;
            }
        }
        
        // Subscribe to the attribute changed event
        playerStats.OnAttributesChanged.AddListener(RefreshAttributes);
        
        // Also subscribe to heart changes to update health attributes
        playerStats.OnHeartChanged.AddListener((type, current, max) => RefreshAttributes());
        
        CreateAttributeRows();
        UpdateAttributeValues();
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (playerStats != null)
        {
            playerStats.OnAttributesChanged.RemoveListener(RefreshAttributes);
            playerStats.OnHeartChanged.RemoveListener((type, current, max) => RefreshAttributes());
        }
    }

    private void Update()
    {
        if (updateEveryFrame)
        {
            UpdateAttributeValues();
        }
        else
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= updateInterval)
            {
                UpdateAttributeValues();
                updateTimer = 0f;
            }
        }
    }

    private void CreateAttributeRows()
    {
        // Clear any existing rows
        foreach (Transform child in attributeContainer)
        {
            Destroy(child.gameObject);
        }
        attributeRows.Clear();
        
        // Health attributes
        CreateAttributeRow("Max Red Hearts", () => playerStats.GetMaxRedHearts().ToString("F1"));
        // CreateAttributeRow("Current Red Hearts", () => playerStats.GetCurrentRedHearts().ToString("F1"));
        // CreateAttributeRow("Current Blue Hearts", () => playerStats.GetCurrentBlueHearts().ToString("F1"));
        // CreateAttributeRow("Current Black Hearts", () => playerStats.GetCurrentBlackHearts().ToString("F1"));
        
        // Combat attributes
        CreateAttributeRow("Damage Multiplier", () => playerStats.GetDamageMultiplier().ToString("F2") + "x");
        CreateAttributeRow("Attack Speed", () => playerStats.GetAttackSpeed().ToString("F2") + "x");
        CreateAttributeRow("Move Speed", () => playerStats.GetMoveSpeed().ToString("F2") + "x");
        CreateAttributeRow("Dodge Chance", () => (playerStats.GetDodgeChance() * 100).ToString("F0") + "%");
        
        // Economy attributes
        CreateAttributeRow("Gold Drop", () => playerStats.GetGoldDropMultiplier().ToString("F2") + "x");
        CreateAttributeRow("Shop Discount", () => (playerStats.GetShopDiscountPercent() * 100).ToString("F0") + "%");
    }

    private void CreateAttributeRow(string attributeName, Func<string> valueGetter)
    {
        GameObject row = Instantiate(attributeRowPrefab, attributeContainer);
        
        // Set the attribute name
        TextMeshProUGUI nameText = row.transform.Find("AttributeName")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI valueText = row.transform.Find("AttributeValue")?.GetComponent<TextMeshProUGUI>();
        
        if (nameText != null)
        {
            nameText.text = attributeName;
        }
        
        // Add to our list of attribute rows
        attributeRows.Add(new AttributeRow { 
            ValueText = valueText, 
            ValueGetter = valueGetter 
        });
    }

    private void UpdateAttributeValues()
    {
        if (playerStats == null) return;
        
        foreach (var row in attributeRows)
        {
            if (row.ValueText != null && row.ValueGetter != null)
            {
                row.ValueText.text = row.ValueGetter();
            }
        }
    }

    // This method is called when attributes change
    public void RefreshAttributes()
    {
        UpdateAttributeValues();
    }

    // Helper class to store attribute row data
    private class AttributeRow
    {
        public TextMeshProUGUI ValueText;
        public Func<string> ValueGetter;
    }
}