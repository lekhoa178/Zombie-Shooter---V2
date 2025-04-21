using System.Collections;
using System.Text;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace PowerTree
{
/// <summary>
    /// UI Component for a single power node
    /// </summary>
    public class PowerNodeUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private GameObject[] levelIndicators;
        
        [Header("Connection Points")]
        [SerializeField] private Transform m_ConnectionPointBottom;
        [SerializeField] private Transform m_ConnectionPointTop;
        
        private PowerNode node;
        private PowerTreeManager treeManager;
        private PowerTreeUI powerTreeUI;
        
        public PowerNode Node => node;
        public Transform ConnectionPoint => m_ConnectionPointBottom != null ? m_ConnectionPointBottom : transform;
        public Transform ConnectionPointTop => m_ConnectionPointTop != null ? m_ConnectionPointTop : transform;
        
        private Coroutine currentAnimation;
        
        public void Initialize(PowerNode powerNode, PowerTreeManager manager)
        {
            node = powerNode;
            treeManager = manager;
            powerTreeUI = FindAnyObjectByType<PowerTreeUI>();
            
            // Set initial values
            iconImage.sprite = node.icon;
            nameText.text = node.displayName;
            
            powerNode.onNodeUnlockedOrUpgraded.AddListener((level) => {
                if (currentAnimation != null)
                {
                    StopCoroutine(currentAnimation);
                    transform.localScale = Vector3.one;
                }
    
                currentAnimation = StartCoroutine(PlayBounceAnimation());
                Refresh();
                GetComponent<TooltipTrigger>()?.UpdateContent();
            });
            
            // Add click handler
            GetComponent<Button>().onClick.AddListener(() => {
                if (powerTreeUI != null)
                {
                    powerTreeUI.OnNodeClicked(node.nodeID);
                }
            });
            
            Refresh();
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (powerTreeUI != null)
            {
                powerTreeUI.SetHoveredNode(this);
            }
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            if (powerTreeUI != null)
            {
                powerTreeUI.ClearHoveredNode(this);
            }
        }
        
        public void Refresh()
        {
            bool isUnlocked = treeManager.IsNodeUnlocked(node.nodeID);
            bool canUnlock = treeManager.CanUnlockNode(node.nodeID);
            int level = treeManager.GetNodeLevel(node.nodeID);
            
            // Update visuals
            if (isUnlocked)
            {
                backgroundImage.color = Color.green; // Unlocked
                levelText.text = $"{level}";
                
                // Update level indicators
                for (int i = 0; i < levelIndicators.Length; i++)
                {
                    levelIndicators[i].SetActive(i < level);
                }
            }
            else if (canUnlock)
            {
                backgroundImage.color = Color.yellow; // Can unlock
                levelText.text = "-";
                
                foreach (var indicator in levelIndicators)
                {
                    indicator.SetActive(false);
                }
            }
            else
            {
                backgroundImage.color = Color.gray; // Locked
                levelText.text = "-";
                
                foreach (var indicator in levelIndicators)
                {
                    indicator.SetActive(false);
                }
            }
            
            // Update tooltip content
            GetComponent<TooltipTrigger>()?.SetContent(
                node.displayName,
                GetCurrentDescription(level)
            );
        }
        
        private string GetCurrentDescription(int level)
        {
            // Display all levels of descriptions with labels
            StringBuilder tooltip = new StringBuilder();
            
            // Add title (node name is already shown elsewhere)
            // tooltip.AppendLine(node.displayName);
            
            // Add descriptions for all levels
            for (int i = 0; i < 3; i++)
            {
                int descLevel = i + 1;
                string description = node.GetCurrentLevelDescription(descLevel);
                
                // Skip empty descriptions
                if (string.IsNullOrEmpty(description))
                    continue;
                
                // Mark the current/next level with a highlight
                string prefix = "- Level " + descLevel + ": ";
                
                // If this is the level the player has or the next one they can purchase
                if (level == descLevel || (level == 0 && descLevel == 1) || (level < 3 && descLevel == level + 1))
                {
                    // For locked nodes, level 1 is the next one to get
                    // For unlocked nodes below max, the next level is level+1
                    if ((level == 0 && descLevel == 1) || (level > 0 && level < 3 && descLevel == level + 1))
                    {
                        int cost = node.GetLevelCost(level);
                        prefix = "- <color=#FFD700>Level " + descLevel + "</color>: ";
                        
                        // Add cost information at the end of this level's description
                        if (i == (level == 0 ? 0 : level))
                        {
                            description += $" <color=#FFD700>(Cost: {cost} Aether {(cost == 1 ? "Shard" : "Shards")})</color>";
                        }
                    }
                    else if (level == descLevel)
                    {
                        // Current level
                        prefix = "- <color=#00FF00>Level " + descLevel + "</color>: ";
                    }
                }
                
                tooltip.AppendLine(prefix + description);
            }
            
            return tooltip.ToString();
        }
        
        private IEnumerator PlayBounceAnimation()
        {
            Vector3 originalScale = Vector3.one; // Use consistent starting scale
            transform.localScale = originalScale;
    
            // First bounce (bigger)
            float duration = 0.3f;
            yield return ScaleTo(originalScale * 1.3f, duration);
            yield return ScaleTo(originalScale * 0.9f, duration * 0.7f);
    
            // Second bounce (smaller)
            yield return ScaleTo(originalScale * 1.1f, duration * 0.5f);
            yield return ScaleTo(originalScale, duration * 0.3f);
            
            currentAnimation = null;
        }

        private IEnumerator ScaleTo(Vector3 targetScale, float duration)
        {
            Vector3 startScale = transform.localScale;
            float elapsed = 0;
    
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                // Using smooth step for more natural easing
                t = t * t * (3f - 2f * t); // Smoothstep formula
        
                transform.localScale = Vector3.Lerp(startScale, targetScale, t);
        
                elapsed += Time.deltaTime;
                yield return null;
            }
    
            transform.localScale = targetScale;
        }
    }

}