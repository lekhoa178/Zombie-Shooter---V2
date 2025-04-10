using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace PowerTree
{
/// <summary>
    /// UI Component for a single power node
    /// </summary>
    public class PowerNodeUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Text nameText;
        [SerializeField] private Text levelText;
        [SerializeField] private GameObject[] levelIndicators;
        
        private PowerNode node;
        private PowerTreeManager treeManager;
        
        public PowerNode Node => node;
        
        public void Initialize(PowerNode powerNode, PowerTreeManager manager)
        {
            node = powerNode;
            treeManager = manager;
            
            // Set initial values
            iconImage.sprite = node.icon;
            nameText.text = node.displayName;
            
            // Add click handler
            GetComponent<Button>().onClick.AddListener(() => {
                var uiManager = FindObjectOfType<PowerTreeUI>();
                if (uiManager != null)
                {
                    uiManager.OnNodeClicked(node.nodeID);
                }
            });
            
            Refresh();
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
                levelText.text = $"Level {level}/3";
                
                // Update level indicators
                for (int i = 0; i < levelIndicators.Length; i++)
                {
                    levelIndicators[i].SetActive(i < level);
                }
            }
            else if (canUnlock)
            {
                backgroundImage.color = Color.yellow; // Can unlock
                levelText.text = "Available";
                
                foreach (var indicator in levelIndicators)
                {
                    indicator.SetActive(false);
                }
            }
            else
            {
                backgroundImage.color = Color.gray; // Locked
                levelText.text = "Locked";
                
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
    }
}