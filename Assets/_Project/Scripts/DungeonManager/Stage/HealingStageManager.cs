using System.Collections;
using System.Collections.Generic;
using CardSystem;
using DG.Tweening;
using UnityEngine;
using TMPro;

namespace DungeonGeneration
{
    /// <summary>
    /// Manages a healing stage where the player can restore health
    /// </summary>
    public class HealingStageManager : StageBaseManager
    {
        [SerializeField] private List<CardData> m_HealingCards = new List<CardData>();
        
        private CardSelectionScreenUI m_CardSelectionScreen;
        
        protected override void Start()
        {
            base.Start();
            
            m_CardSelectionScreen = FindAnyObjectByType<CardSelectionScreenUI>();
            
            if (m_HealingCards.Count > 0)
            {
                DOVirtual.DelayedCall(2f, () =>
                {
                    m_CardSelectionScreen?.ShowSelectionScreen(m_HealingCards.Count, 1,
                        m_HealingCards, null, null, "Healing Stage", "Choose a healing card to restore health.");
                    
                    onStageCompleted.Invoke();
                });
            }
            else
            {
                Debug.LogWarning("No healing cards available for this stage.");
            }
        }
    }
} 