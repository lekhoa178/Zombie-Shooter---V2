# Power Tree System Technical Documentation

## Overview

The Power Tree system is a flexible, extensible framework for implementing character progression through unlockable and upgradable power nodes. It supports multiple branches of abilities (Survival, Attack, Evasion, Finance), with each node providing unique gameplay effects. The system features a user-friendly UI, configurable costs, and dynamic descriptions.

## Core Architecture

### 1. Base Classes

#### PowerNode

The foundational class for all power nodes, implemented as ScriptableObjects for easy configuration in the editor.

```csharp
public class PowerNode : ScriptableObject
{
    // Basic info
    public string nodeID;
    public string displayName;
    public string[] descriptions = new string[3]; // One per level
    public Sprite icon;
    public PowerBranch branch;
    
    // Tree structure
    public PowerNode[] unlocksNodes; // Nodes this unlocks
    public PowerNode[] requiredNodes; // Nodes required to unlock this
    
    // Methods
    public virtual void ApplyEffect(PlayerStats player, int level) { }
    public virtual void RemoveEffect(PlayerStats player) { }
    public virtual string GetCurrentLevelDescription(int level) { }
    protected string ProcessDescription(string description) { }
}
```

#### PlayerStats

Manages player statistics and serves as the interface between power nodes and gameplay effects.

```csharp
public class PlayerStats : MonoBehaviour
{
    // Health, combat stats, economy variables
    
    // Events for power nodes to subscribe to
    public UnityEvent<float> OnRedHeartLost;
    public UnityEvent<float> OnBlueHeartLost;
    public UnityEvent OnEnemyKilled;
    // (and many more events)
    
    // Configuration system for nodes
    private Dictionary<string, object> nodeConfiguration;
    private Dictionary<string, Timer> nodeTimers;
    private Dictionary<string, Counter> nodeCounters;
    
    // Methods to modify player stats
    public void ModifyDamageMultiplier(float amount, float duration = 0);
    public void ModifyAttackSpeedMultiplier(float amount, float duration = 0, bool canStack = false);
    // (and more modifier methods)
}
```

#### PowerTreeManager

Manages node unlocking, upgrading, and the overall progression system.

```csharp
public class PowerTreeManager : MonoBehaviour
{
    // Node database and player stats references
    private PowerNodeDatabase nodeDatabase;
    private PlayerStats playerStats;
    
    // Methods for node management
    public bool CanUnlockNode(string nodeID);
    public bool UnlockNode(string nodeID);
    public bool CanUpgradeNode(string nodeID);
    public bool UpgradeNode(string nodeID);
    
    // Aether shard management
    public void AddAetherShards(int amount);
    public int GetAvailableShards();
}
```

### 2. Helper Classes

#### Timer

Manages time-based effects for power nodes.

```csharp
public class Timer
{
    public float Duration { get; set; }
    public float CurrentTime { get; private set; }
    public bool IsRunning { get; private set; }
    public bool IsCompleted => !IsRunning && CurrentTime <= 0;
    
    public event Action OnTimerCompleted;
    
    public void Start();
    public void Stop();
    public void Reset();
    public void Update(float deltaTime);
}
```

#### Counter

Manages counting-based effects for power nodes.

```csharp
public class Counter
{
    public int Value { get; private set; }
    public int MaxValue { get; set; }
    
    public event Action<int> OnValueChanged;
    public event Action OnMaxValueReached;
    
    public void Increment();
    public void Decrement();
    public void Reset();
    public void SetValue(int value);
}
```

## Power Node Implementation

### Branch Structure

The power tree is divided into four branches, each focusing on different gameplay aspects:

1. **Survival**: Health, hearts, and defensive abilities
2. **Attack**: Damage, attack speed, and offensive capabilities
3. **Evasion**: Movement speed, dodge chance, and mobility
4. **Finance**: Gold drops, shop discounts, and economy benefits

### Node Levels

Each node supports three levels of progression:

- **Level 1**: Basic effect
- **Level 2**: Enhanced effect
- **Level 3**: Maximum effect, often adding additional features

### Implementation Example

```csharp
[CreateAssetMenu(fileName = "BloodthirstyCombat", menuName = "PowerTree/Nodes/Attack/BloodthirstyCombat")]
public class BloodthirstyCombatNode : PowerNode
{
    [SerializeField] private float[] attackSpeedBoost = new float[] { 0.2f, 0.25f, 0.3f }; 
    [SerializeField] private float[] damageBoost = new float[] { 0f, 0.05f, 0.1f };
    [SerializeField] private float buffDuration = 5f;
    [SerializeField] private float killWindowDuration = 5f;
    [SerializeField] private int requiredKills = 3;
    
    private void OnValidate()
    {
        // Auto-populate descriptions with field values
        descriptions = new string[]
        {
            "When defeating {requiredKills} enemies within {killWindowDuration} seconds, gain {attackSpeedBoost[0]} attack speed for {buffDuration} seconds",
            "Buff increases to {attackSpeedBoost[1]} attack speed and {damageBoost[1]} damage",
            "Buff can stack twice, up to {attackSpeedBoost[2]} attack speed and {damageBoost[2]} damage"
        };
    }
    
    public override void ApplyEffect(PlayerStats player, int level)
    {
        // Initialize timers and counters
        killCounter = player.GetCounter(nodeID, "killCounter");
        killTimer = player.GetTimer(nodeID, "killTimer", killWindowDuration);
        
        // Subscribe to events
        player.OnEnemyKilled.AddListener(OnEnemyKilled);
    }
    
    // Additional implementation methods
}
```

## Dynamic Description System

The system includes an automatic description generation feature that replaces placeholders with actual values.

### Placeholder Format

Descriptions use placeholders in the format `{fieldName}` or `{arrayName[index]}` which are replaced with the actual values from the node's fields.

### Example Templates

```csharp
"When defeating {requiredKills} enemies within {killWindowDuration} seconds, gain {attackSpeedBoost[0]} attack speed"
```

When displayed, this might render as:
```
"When defeating 3 enemies within 5.0 seconds, gain 20% attack speed"
```

### Special Formatting Rules

- **Boost fields**: Values multiplied by 100 and displayed as percentages
- **Duration fields**: Displayed with one decimal place
- **Chance fields**: Values multiplied by 100 and displayed as percentages

## UI System

### PowerTreeUI

Manages the overall UI for the power tree, including node placement and connections.

```csharp
public class PowerTreeUI : MonoBehaviour
{
    // References to UI elements
    private Transform[] branchContainers;
    private GameObject nodePrefab;
    private GameObject connectionPrefab;
    
    // Methods
    public void BuildTreeUI();
    public void RefreshAllNodes();
    public void OnNodeClicked(string nodeID);
}
```

### PowerNodeUI

Handles the UI for individual nodes, including appearance and tooltips.

```csharp
public class PowerNodeUI : MonoBehaviour
{
    // UI components
    private Image iconImage;
    private Image backgroundImage;
    private Text nameText;
    private Text levelText;
    private GameObject[] levelIndicators;
    
    // Methods
    public void Initialize(PowerNode powerNode, PowerTreeManager manager);
    public void Refresh();
    private string GetCurrentDescription(int level);
}
```

### Tooltip System

Provides detailed information when hovering over nodes.

```csharp
public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // Content
    private string title;
    private string content;
    
    // Methods
    public void SetContent(string tooltipTitle, string tooltipContent);
    public void OnPointerEnter(PointerEventData eventData);
    public void OnPointerExit(PointerEventData eventData);
}

public class TooltipDisplay : MonoBehaviour
{
    // UI components
    private Text titleText;
    private Text contentText;
    
    // Methods
    public void ShowTooltip(string title, string content, Vector3 position);
    private void PositionTooltip(Vector3 position);
}
```

## Node Configuration System

### Properties

Nodes can register custom properties that persist throughout gameplay:

```csharp
// Register a property
player.SetNodeProperty(nodeID, "healthThreshold", 0.5f);

// Retrieve a property with fallback
float threshold = player.GetNodeProperty(nodeID, "healthThreshold", 0.5f);
```

### Timers

Manage time-based effects for power nodes:

```csharp
// Create and configure a timer
Timer killTimer = player.GetTimer(nodeID, "killTimer", 5f);
killTimer.OnTimerCompleted += ResetCounter;

// Use the timer
killTimer.Start();
killTimer.Reset();
```

### Counters

Track numeric values with event callbacks:

```csharp
// Create and configure a counter
Counter killCounter = player.GetCounter(nodeID, "killCounter");
killCounter.MaxValue = 3;
killCounter.OnMaxValueReached += ApplyBuff;

// Use the counter
killCounter.Increment();
```

## Editor Tools

### PowerNodeTemplateGenerator

Creates template scripts for new power nodes.

```csharp
[MenuItem("Assets/Create/PowerTree/Node Template Script")]
public static void CreatePowerNodeTemplate()
{
    // Open dialog window
    PowerNodeTemplateWindow.ShowWindow();
}
```

### PowerNodeBatchGenerator

Batch creates multiple node templates for a specific branch.

```csharp
[MenuItem("Tools/PowerTree/Batch Create Node Templates")]
public static void ShowWindow()
{
    // Open batch generator window
    GetWindow<PowerNodeBatchGenerator>("Batch Create Power Nodes");
}
```

## Usage Examples

### Setting Up the Power Tree

```csharp
// Create the base components
var playerStats = gameObject.AddComponent<PlayerStats>();
var treeManager = gameObject.AddComponent<PowerTreeManager>();
var nodeFactory = ScriptableObject.CreateInstance<PowerNodeFactory>();

// Initialize the tree
var tree = nodeFactory.CreatePowerTree();
treeManager.Initialize(tree, playerStats);
```

### Unlocking and Upgrading Nodes

```csharp
// Add currency
treeManager.AddAetherShards(5);

// Unlock a node
if (treeManager.CanUnlockNode("ghost_steps"))
{
    treeManager.UnlockNode("ghost_steps");
}

// Upgrade a node
if (treeManager.CanUpgradeNode("ghost_steps"))
{
    treeManager.UpgradeNode("ghost_steps");
}
```

### Creating a Custom Node

```csharp
[CreateAssetMenu(fileName = "CustomNode", menuName = "PowerTree/Nodes/Attack/CustomNode")]
public class CustomNode : PowerNode
{
    [SerializeField] private float[] effectValue = new float[] { 0.1f, 0.2f, 0.3f };
    
    private void OnValidate()
    {
        descriptions = new string[]
        {
            "Custom effect with {effectValue[0]} bonus",
            "Effect increased to {effectValue[1]} bonus",
            "Effect maxed at {effectValue[2]} bonus"
        };
    }
    
    public override void ApplyEffect(PlayerStats player, int level)
    {
        // Implementation
    }
}
```

## Best Practices

1. **Use OnValidate for descriptions**: Always update descriptions in OnValidate to keep them in sync with actual values.
2. **Clean up event subscriptions**: Always remove event listeners in RemoveEffect to prevent memory leaks.
3. **Use timers and counters**: Leverage the built-in timer and counter systems for time and count-based effects.
4. **Proper error handling**: Check level values and null references to avoid runtime errors.
5. **Follow naming conventions**: Keep node IDs consistent and descriptive.

## Performance Considerations

1. **Event handling**: Use direct references instead of FindObjectOfType in event callbacks when possible.
2. **Tooltip rendering**: The tooltip system uses a singleton pattern to minimize GameObject creation.
3. **Description processing**: Description placeholders are processed only when needed, not on every frame.

## Extensibility

The system is designed for easy extension:

1. **New branches**: Simply add new enum values to PowerBranch.
2. **New node types**: Create new subclasses of PowerNode.
3. **New events**: Add more UnityEvents to PlayerStats for nodes to subscribe to.
4. **Custom UI**: Replace the PowerNodeUI prefab with a custom implementation. 