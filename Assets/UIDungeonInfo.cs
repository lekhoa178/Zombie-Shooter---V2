using System;
using PowerTree;
using TMPro;
using UnityEngine;

public class UIDungeonInfo : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI aetherShardText;
    
    private PlayerStats playerStats;
    private PowerTreeManager powerTreeManager;

    private void Awake()
    {
        playerStats = FindAnyObjectByType<PlayerStats>();
        
        powerTreeManager = FindAnyObjectByType<PowerTreeManager>();
    }
    
    private void Start()
    {
        playerStats.OnAttributesChanged.AddListener(UpdateGoldText);
        powerTreeManager.OnAetherShardChanged.AddListener(UpdateAetherShardText);
        
        UpdateGoldText();
        UpdateAetherShardText();
    }
    
    private void UpdateGoldText()
    {
        if (playerStats)
        {
            goldText.text = playerStats.GetGold().ToString();
        }
    }
    
    private void UpdateAetherShardText()
    {
        if (powerTreeManager)
        {
            aetherShardText.text = powerTreeManager.GetAetherShards().ToString();
        }
    }
}
