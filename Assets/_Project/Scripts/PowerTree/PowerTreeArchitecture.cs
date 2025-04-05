using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using System.Text;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PowerTree
{
    #region Core System

    /// <summary>
    /// Heart types in the game
    /// </summary>
    public enum HeartType
    {
        Red,
        Blue,
        Black
    }

    /// <summary>
    /// Power branches in the skill tree
    /// </summary>
    public enum PowerBranch
    {
        Survival,
        Attack,
        Evasion,
        Finance
    }

    /// <summary>
    /// Base ScriptableObject for all power nodes
    /// </summary>
    [CreateAssetMenu(fileName = "PowerNode", menuName = "PowerTree/Base Node")]
    public class PowerNode : ScriptableObject
    {
        [Header("Basic Info")]
        public string nodeID;
        public string displayName;
        public string[] descriptions = new string[3]; // One per level
        public Sprite icon;
        public PowerBranch branch;
        
        [Header("Tree Structure")]
        public PowerNode[] unlocksNodes; // Nodes that this unlocks
        public PowerNode[] requiredNodes; // Nodes required to unlock this
        
        [Header("Costs")]
        [SerializeField] private int[] aetherCosts = new int[] { 1, 2, 3 };
        
        public int GetLevelCost(int level)
        {
            if (level < 0 || level >= aetherCosts.Length) return 0;
            return aetherCosts[level];
        }
        
        public int GetTotalCost()
        {
            return aetherCosts.Sum();
        }
        
        public virtual void ApplyEffect(PlayerStats player, int level) 
        {
            // Override in derived classes
        }
        
        public virtual void RemoveEffect(PlayerStats player) 
        {
            // Override in derived classes
        }
        
        public virtual string GetCurrentLevelDescription(int level)
        {
            if (level <= 0 || level > descriptions.Length) return string.Empty;
            return ProcessDescription(descriptions[level - 1]);
        }
        
        /// <summary>
        /// Processes a description string by replacing placeholders with actual field values.
        /// Placeholders are in the format {fieldName} and will be replaced with the
        /// value of the corresponding field in this node.
        /// </summary>
        /// <param name="description">The description template with placeholders</param>
        /// <returns>The processed description with actual values</returns>
        protected string ProcessDescription(string description)
        {
            if (string.IsNullOrEmpty(description)) return description;
            
            // Find all placeholders in the format {fieldName}
            var placeholderRegex = new System.Text.RegularExpressions.Regex(@"\{([^{}]+)\}");
            var matches = placeholderRegex.Matches(description);
            
            string result = description;
            
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                string placeholder = match.Groups[1].Value;
                object value = GetFieldValue(placeholder);
                
                if (value != null)
                {
                    // Format values based on their type
                    string formattedValue = FormatFieldValue(value, placeholder);
                    result = result.Replace("{" + placeholder + "}", formattedValue);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Gets the value of a field or property by name using reflection.
        /// </summary>
        /// <param name="fieldName">Name of the field or property</param>
        /// <returns>The value of the field/property or null if not found</returns>
        private object GetFieldValue(string fieldName)
        {
            // Check if we're looking for an array element like "attackSpeedBoost[0]"
            var arrayMatch = System.Text.RegularExpressions.Regex.Match(fieldName, @"(\w+)\[(\d+)\]");
            if (arrayMatch.Success)
            {
                string arrayFieldName = arrayMatch.Groups[1].Value;
                int index = int.Parse(arrayMatch.Groups[2].Value);
                
                // Get the array field
                var field = GetType().GetField(arrayFieldName, System.Reflection.BindingFlags.Instance | 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                
                if (field != null)
                {
                    var array = field.GetValue(this) as System.Array;
                    if (array != null && index >= 0 && index < array.Length)
                    {
                        return array.GetValue(index);
                    }
                }
                return null;
            }
            
            // Look for standard fields
            var fieldInfo = GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            
            if (fieldInfo != null)
            {
                return fieldInfo.GetValue(this);
            }
            
            // Look for properties
            var propertyInfo = GetType().GetProperty(fieldName, System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            
            if (propertyInfo != null)
            {
                return propertyInfo.GetValue(this);
            }
            
            return null;
        }
        
        /// <summary>
        /// Formats a field value based on its type and field name.
        /// </summary>
        /// <param name="value">The field value to format</param>
        /// <param name="fieldName">The name of the field (used for custom formatting rules)</param>
        /// <returns>The formatted string representation of the value</returns>
        private string FormatFieldValue(object value, string fieldName)
        {
            if (value == null) return "null";
            
            // Handle specific field naming conventions
            if (fieldName.Contains("Boost") && value is float floatValue)
            {
                // Convert boost values to percentages
                return $"{floatValue * 100:0}";
            }
            else if (fieldName.EndsWith("Duration") && value is float durationValue)
            {
                // Format durations with one decimal place
                return $"{durationValue:0.0}";
            }
            else if (fieldName.Contains("Chance") && value is float chanceValue)
            {
                // Convert chance values to percentages
                return $"{chanceValue * 100:0}";
            }
            else if (value is float floatVal)
            {
                // Default float formatting
                return $"{floatVal:0.##}";
            }
            else if (value is int intVal)
            {
                // Default int formatting
                return intVal.ToString();
            }
            
            // Default formatting for other types
            return value.ToString();
        }
    }

    /// <summary>
    /// Serializable data for saving player's power tree progress
    /// </summary>
    [System.Serializable]
    public class PowerTreeData
    {
        public int aetherShards;
        public List<PowerNodeState> unlockedNodes = new List<PowerNodeState>();
    }

    /// <summary>
    /// State of a specific power node for the player
    /// </summary>
    [System.Serializable]
    public class PowerNodeState
    {
        public string nodeID;
        public int level;
    }

    /// <summary>
    /// Database of all power nodes in the game
    /// </summary>
    [CreateAssetMenu(fileName = "PowerNodeDatabase", menuName = "PowerTree/Node Database")]
    public class PowerNodeDatabase : ScriptableObject
    {
        public PowerNode[] AllNodes;
        
        // Optional: cached nodes by branch
        private Dictionary<PowerBranch, List<PowerNode>> nodesByBranch;
        
        public void Initialize()
        {
            nodesByBranch = new Dictionary<PowerBranch, List<PowerNode>>();
            
            foreach (PowerBranch branch in Enum.GetValues(typeof(PowerBranch)))
            {
                nodesByBranch[branch] = new List<PowerNode>();
            }
            
            foreach (var node in AllNodes)
            {
                nodesByBranch[node.branch].Add(node);
            }
        }
        
        public List<PowerNode> GetNodesByBranch(PowerBranch branch)
        {
            if (nodesByBranch == null)
            {
                Initialize();
            }
            
            return nodesByBranch[branch];
        }
    }

    #endregion    
    
    #region UI System
    
    /// <summary>
    /// UI Manager for the power tree screen
    /// </summary>
    public class PowerTreeUI : MonoBehaviour
    {
        [SerializeField] private PowerTreeManager treeManager;
        [SerializeField] private Transform[] branchContainers; // One per branch
        [SerializeField] private GameObject nodePrefab;
        [SerializeField] private GameObject connectionPrefab;
        
        private Dictionary<string, PowerNodeUI> nodeUIComponents = new Dictionary<string, PowerNodeUI>();
        
        private void Start()
        {
            BuildTreeUI();
        }
        
        public void BuildTreeUI()
        {
            // Get node database from manager
            PowerNodeDatabase database = treeManager.GetComponent<PowerNodeDatabase>();
            if (database == null) return;
            
            // Clear existing UI
            foreach (var container in branchContainers)
            {
                foreach (Transform child in container)
                {
                    Destroy(child.gameObject);
                }
            }
            
            nodeUIComponents.Clear();
            
            // Build nodes for each branch
            for (int i = 0; i < branchContainers.Length; i++)
            {
                PowerBranch branch = (PowerBranch)i;
                List<PowerNode> branchNodes = database.GetNodesByBranch(branch);
                
                foreach (var node in branchNodes)
                {
                    GameObject nodeObj = Instantiate(nodePrefab, branchContainers[i]);
                    PowerNodeUI nodeUI = nodeObj.GetComponent<PowerNodeUI>();
                    
                    if (nodeUI != null)
                    {
                        nodeUI.Initialize(node, treeManager);
                        nodeUIComponents[node.nodeID] = nodeUI;
                    }
                }
            }
            
            // Create connections between nodes
            CreateNodeConnections();
            
            // Update node states
            RefreshAllNodes();
        }
        
        private void CreateNodeConnections()
        {
            foreach (var nodeEntry in nodeUIComponents)
            {
                PowerNode node = nodeEntry.Value.Node;
                
                foreach (var childNode in node.unlocksNodes)
                {
                    if (nodeUIComponents.TryGetValue(childNode.nodeID, out PowerNodeUI childUI))
                    {
                        // Create connection line
                        GameObject connectionObj = Instantiate(connectionPrefab, nodeEntry.Value.transform);
                        LineRenderer line = connectionObj.GetComponent<LineRenderer>();
                        
                        if (line != null)
                        {
                            // Set up line points
                            line.SetPosition(0, nodeEntry.Value.transform.position);
                            line.SetPosition(1, childUI.transform.position);
                        }
                    }
                }
            }
        }
        
        public void RefreshAllNodes()
        {
            foreach (var nodeUI in nodeUIComponents.Values)
            {
                nodeUI.Refresh();
            }
        }
        
        public void OnNodeClicked(string nodeID)
        {
            // Handle node click (unlock/upgrade)
            if (treeManager.IsNodeUnlocked(nodeID))
            {
                if (treeManager.CanUpgradeNode(nodeID))
                {
                    treeManager.UpgradeNode(nodeID);
                    RefreshAllNodes();
                }
            }
            else if (treeManager.CanUnlockNode(nodeID))
            {
                treeManager.UnlockNode(nodeID);
                RefreshAllNodes();
            }
        }
    }
    
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
    
    #endregion

    /// <summary>
    /// Component that triggers a tooltip to appear when hovering over a UI element
    /// </summary>
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private string title;
        [SerializeField] private string content;
        
        private static GameObject tooltipInstance;
        private static TooltipDisplay tooltipDisplay;

        // Initialize the tooltip singleton
        static TooltipTrigger()
        {
            if (tooltipInstance == null)
            {
                // Create tooltip instance when first used
                GameObject tooltipPrefab = Resources.Load<GameObject>("UI/PowerTreeTooltip");
                if (tooltipPrefab != null)
                {
                    tooltipInstance = Instantiate(tooltipPrefab);
                    tooltipDisplay = tooltipInstance.GetComponent<TooltipDisplay>();
                    tooltipInstance.SetActive(false);
                    DontDestroyOnLoad(tooltipInstance);
                }
                else
                {
                    Debug.LogWarning("Tooltip prefab not found in Resources/UI/PowerTreeTooltip. Creating a basic tooltip.");
                    CreateBasicTooltip();
                }
            }
        }

        private static void CreateBasicTooltip()
        {
            tooltipInstance = new GameObject("PowerTreeTooltip");
            tooltipDisplay = tooltipInstance.AddComponent<TooltipDisplay>();
            tooltipInstance.SetActive(false);
            DontDestroyOnLoad(tooltipInstance);
        }

        public void SetContent(string tooltipTitle, string tooltipContent)
        {
            title = tooltipTitle;
            content = tooltipContent;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (tooltipDisplay != null)
            {
                tooltipDisplay.ShowTooltip(title, content, Input.mousePosition);
                tooltipInstance.SetActive(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (tooltipDisplay != null)
            {
                tooltipInstance.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Displays the tooltip content
    /// </summary>
    public class TooltipDisplay : MonoBehaviour
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text contentText;
        [SerializeField] private LayoutElement layoutElement;
        [SerializeField] private int characterWrapLimit = 50;
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Vector2 offset = new Vector2(20, 20);
        
        private Canvas tooltipCanvas;
        private CanvasScaler canvasScaler;

        private void Awake()
        {
            // Create the tooltip UI if it doesn't exist
            if (titleText == null || contentText == null)
            {
                SetupTooltipUI();
            }
            
            rectTransform = GetComponent<RectTransform>();
        }

        public void ShowTooltip(string title, string content, Vector3 position)
        {
            // Set the tooltip content
            titleText.text = title;
            contentText.text = content;
            
            // Calculate tooltip size based on content
            int titleLength = title.Length;
            int contentLength = content.Length;
            
            layoutElement.enabled = (titleLength > characterWrapLimit || contentLength > characterWrapLimit);
            
            // Position the tooltip
            PositionTooltip(position);
        }

        private void PositionTooltip(Vector3 position)
        {
            // Make sure the tooltip stays within screen bounds
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            Vector2 pivotPosition = position + new Vector3(offset.x, offset.y, 0);
            
            // Adjust pivot based on screen edge proximity
            rectTransform.pivot = Vector2.zero; // Default pivot at bottom-left
            
            if (pivotPosition.x + rectTransform.rect.width > screenSize.x)
            {
                pivotPosition.x = position.x - offset.x - rectTransform.rect.width;
            }
            
            if (pivotPosition.y + rectTransform.rect.height > screenSize.y)
            {
                pivotPosition.y = position.y - offset.y - rectTransform.rect.height;
            }
            
            rectTransform.position = pivotPosition;
        }

        private void SetupTooltipUI()
        {
            // Create Canvas
            tooltipCanvas = gameObject.AddComponent<Canvas>();
            tooltipCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            tooltipCanvas.sortingOrder = 100;
            
            canvasScaler = gameObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            
            gameObject.AddComponent<GraphicRaycaster>();
            
            // Create background
            GameObject background = new GameObject("Background");
            background.transform.SetParent(transform, false);
            
            Image backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            
            RectTransform bgRect = backgroundImage.rectTransform;
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            
            // Create content container
            GameObject container = new GameObject("Container");
            container.transform.SetParent(transform, false);
            
            VerticalLayoutGroup layout = container.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 5;
            
            RectTransform containerRect = container.GetComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.sizeDelta = Vector2.zero;
            
            // Create title text
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(container.transform, false);
            
            titleText = titleObj.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.fontSize = 16;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = Color.white;
            
            // Create content text
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(container.transform, false);
            
            contentText = contentObj.AddComponent<Text>();
            contentText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            contentText.fontSize = 14;
            contentText.color = Color.white;
            
            // Add layout element to container
            layoutElement = container.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 300;
            layoutElement.flexibleWidth = 0;
        }
    }

    // Helper class for node timers
    public class Timer
    {
        public float Duration { get; set; }
        public float CurrentTime { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsCompleted => !IsRunning && CurrentTime <= 0;
        
        public event System.Action OnTimerCompleted;
        
        public Timer(float duration)
        {
            Duration = duration;
            CurrentTime = 0;
            IsRunning = false;
        }
        
        public void Start()
        {
            CurrentTime = Duration;
            IsRunning = true;
        }
        
        public void Stop()
        {
            IsRunning = false;
        }
        
        public void Reset()
        {
            CurrentTime = Duration;
        }
        
        public void Update(float deltaTime)
        {
            if (!IsRunning) return;
            
            if (CurrentTime > 0)
            {
                CurrentTime -= deltaTime;
                if (CurrentTime <= 0)
                {
                    IsRunning = false;
                    OnTimerCompleted?.Invoke();
                }
            }
        }
    }
    
    // Helper class for node counters
    public class Counter
    {
        public int Value { get; private set; }
        public int MaxValue { get; set; }
        
        public event System.Action<int> OnValueChanged;
        public event System.Action OnMaxValueReached;
        
        public Counter(int maxValue = int.MaxValue)
        {
            Value = 0;
            MaxValue = maxValue;
        }
        
        public void Increment()
        {
            Value++;
            OnValueChanged?.Invoke(Value);
            
            if (Value >= MaxValue)
            {
                OnMaxValueReached?.Invoke();
            }
        }
        
        public void Decrement()
        {
            if (Value > 0)
            {
                Value--;
                OnValueChanged?.Invoke(Value);
            }
        }
        
        public void Reset()
        {
            Value = 0;
            OnValueChanged?.Invoke(Value);
        }
        
        public void SetValue(int value)
        {
            Value = value;
            OnValueChanged?.Invoke(Value);
            
            if (Value >= MaxValue)
            {
                OnMaxValueReached?.Invoke();
            }
        }
    }
} 