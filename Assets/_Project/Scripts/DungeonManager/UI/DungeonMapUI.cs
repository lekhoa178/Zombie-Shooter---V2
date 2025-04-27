using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI.Extensions;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace DungeonGeneration
{
    /// <summary>
    /// UI Manager for the dungeon map visualization
    /// </summary>
    public class DungeonMapUI : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")] [SerializeField]
        private DungeonManager m_DungeonManager;

        [SerializeField] private Transform m_StagesContainer;
        [SerializeField] private GameObject m_StageNodePrefab;
        [SerializeField] private GameObject m_ConnectionPrefab;
        [SerializeField] private TextMeshProUGUI m_ZoneProgressText;
        [SerializeField] private TextMeshProUGUI m_ZoneNameText;
        [SerializeField] private RectTransform m_ClipArea;

        [Header("Drag Settings")] [SerializeField]
        private bool m_EnableDragging = true;

        [SerializeField] private float m_DragSpeed = 1f;
        [SerializeField] private float m_EdgeBuffer = 50f;
        [SerializeField] private bool m_ResetOnRefresh = true;

        [Header("Icons")] [SerializeField] private Sprite m_CombatStageIcon;
        [SerializeField] private Sprite m_ChallengeStageIcon;
        [SerializeField] private Sprite m_BossStageIcon;
        [SerializeField] private Sprite m_ShopStageIcon;
        [SerializeField] private Sprite m_HealingStageIcon;
        [SerializeField] private Sprite m_MysteryStageIcon;
        [SerializeField] private Sprite m_InterzoneStageIcon;

        [Header("Legend")] [SerializeField] private Transform m_LegendContainer;
        [SerializeField] private GameObject m_LegendItemPrefab;
        [SerializeField] private GameObject m_LegendDetailsPanel;
        [SerializeField] private TextMeshProUGUI m_LegendDetailsText;

        [Header("Connection Settings")] [SerializeField]
        private Material m_ConnectionMaterial;

        [SerializeField] private float m_LineThickness = 2f;
        [SerializeField] private Color m_LineColor = Color.white;
        [SerializeField] private Color m_CurrentPathLineColor = Color.yellow;

        [SerializeField]
        private Color m_UnvisitedLineColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Semi-transparent gray

        [SerializeField] private bool m_LineCaps = false;

        [Header("Layout Settings")] [SerializeField]
        private float m_HorizontalSpacing = 180f;

        [SerializeField] private float m_VerticalSpacing = 120f;
        [SerializeField] private Vector2 m_InitialOffset = new Vector2(0, 0);

        [System.Serializable]
        public enum LayerAlignment
        {
            Left,
            Center,
            Right,
            EvenlySpaced
        }

        [SerializeField, Tooltip("How nodes should be aligned horizontally within each layer")]
        private LayerAlignment m_LayerAlignment = LayerAlignment.Center;

        [SerializeField, Tooltip("Apply special alignment for the first layer (start)")]
        private bool m_UseSpecialAlignmentForFirstLayer = true;

        [SerializeField, Tooltip("First layer alignment (usually the start node)")]
        private LayerAlignment m_FirstLayerAlignment = LayerAlignment.Center;

        [SerializeField, Tooltip("Apply special alignment for the last layer (boss)")]
        private bool m_UseSpecialAlignmentForLastLayer = true;

        [SerializeField, Tooltip("Last layer alignment (usually the boss node)")]
        private LayerAlignment m_LastLayerAlignment = LayerAlignment.Center;

        #endregion

        #region Private Fields

        private Dictionary<string, DungeonStageUI> m_StageUIComponents = new Dictionary<string, DungeonStageUI>();
        private Dictionary<int, List<StageNode>> m_NodesByDepth = new Dictionary<int, List<StageNode>>();
        private List<GameObject> m_ConnectionObjects = new List<GameObject>();
        private Vector2 m_MapBoundsMin;
        private Vector2 m_MapBoundsMax;
        private MapDragController m_DragController;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            SetupContainerForDragging();
        }

        private void Start()
        {
            if (m_DungeonManager == null)
            {
                m_DungeonManager = DungeonManager.Instance;
            }

            // Setup legend items
            SetupLegend();

            // Build the dungeon map
            BuildDungeonMap();
        }

        #endregion

        /// <summary>
        /// Sets up the container for dragging and clipping
        /// </summary>
        private void SetupContainerForDragging()
        {
            // Make sure we have a clip area
            if (m_ClipArea == null)
            {
                // Try to get or create one
                m_ClipArea = transform.Find("ClipArea") as RectTransform;

                // If still not found, create a new one
                if (m_ClipArea == null)
                {
                    GameObject clipObject = new GameObject("ClipArea", typeof(RectTransform));
                    clipObject.transform.SetParent(transform, false);

                    m_ClipArea = clipObject.GetComponent<RectTransform>();
                    m_ClipArea.anchorMin = Vector2.zero;
                    m_ClipArea.anchorMax = Vector2.one;
                    m_ClipArea.offsetMin = Vector2.zero;
                    m_ClipArea.offsetMax = Vector2.zero;

                    // Add mask component for clipping
                    Image maskImage = clipObject.AddComponent<Image>();
                    maskImage.color = new Color(0, 0, 0, 0.01f); // Nearly transparent

                    Mask mask = clipObject.AddComponent<Mask>();
                    mask.showMaskGraphic = false;
                }
            }

            // Move the container inside the clip area if needed
            if (m_StagesContainer.parent != m_ClipArea)
            {
                // Create container if it doesn't exist
                if (m_StagesContainer == null)
                {
                    GameObject containerObj = new GameObject("DungeonMapContainer", typeof(RectTransform));
                    m_StagesContainer = containerObj.transform;
                }

                m_StagesContainer.SetParent(m_ClipArea, false);

                // Setup the container as a rect transform
                RectTransform containerRect = m_StagesContainer as RectTransform;
                if (containerRect != null)
                {
                    containerRect.anchorMin = new Vector2(0.5f, 0.5f);
                    containerRect.anchorMax = new Vector2(0.5f, 0.5f);
                    containerRect.pivot = new Vector2(0.5f, 0.5f);
                    containerRect.anchoredPosition = Vector2.zero;
                    containerRect.sizeDelta = new Vector2(3000, 3000); // Large size to accommodate content
                }
            }
            
            // Setup the drag controller
            if (m_EnableDragging)
            {
                // Check if the drag controller already exists
                m_DragController = m_ClipArea.GetComponent<MapDragController>();
                
                if (m_DragController == null)
                {
                    m_DragController = m_ClipArea.gameObject.AddComponent<MapDragController>();
                }
                
                // Configure the drag controller
                m_DragController.Initialize(m_StagesContainer as RectTransform, m_DragSpeed, m_EdgeBuffer);
            }
        }

        #region Public Methods

        /// <summary>
        /// Refreshes the dungeon map UI
        /// </summary>
        public void RefreshDungeonMap()
        {
            ClearMap();

            // Reset position if needed
            if (m_ResetOnRefresh && m_StagesContainer != null)
            {
                RectTransform containerRect = m_StagesContainer as RectTransform;
                if (containerRect != null)
                {
                    containerRect.anchoredPosition = Vector2.zero;
                }
            }

            BuildDungeonMap();
        }

        /// <summary>
        /// Updates the map bounds after layout changes
        /// </summary>
        public void UpdateMapBounds(Vector2 min, Vector2 max)
        {
            m_MapBoundsMin = min;
            m_MapBoundsMax = max;
            
            if (m_DragController != null)
            {
                m_DragController.SetBounds(min, max);
            }
        }

        /// <summary>
        /// Shows detailed information about all stage types
        /// </summary>
        public void ShowLegendDetails(bool show)
        {
            if (m_LegendDetailsPanel != null)
            {
                m_LegendDetailsPanel.SetActive(show);
            }
        }

        /// <summary>
        /// Sets the legend details text
        /// </summary>
        public void SetLegendDetailsText(string text)
        {
            if (m_LegendDetailsText != null)
            {
                m_LegendDetailsText.text = text;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Clears all map elements
        /// </summary>
        private void ClearMap()
        {
            // Clear stage nodes
            foreach (Transform child in m_StagesContainer)
            {
                Destroy(child.gameObject);
            }

            // Clear connections
            foreach (var connection in m_ConnectionObjects)
            {
                Destroy(connection);
            }

            m_ConnectionObjects.Clear();
            m_StageUIComponents.Clear();
            m_NodesByDepth.Clear();
        }

        /// <summary>
        /// Sets up the legend items
        /// </summary>
        private void SetupLegend()
        {
            if (m_LegendContainer == null || m_LegendItemPrefab == null)
                return;

            // Clear existing legend items
            foreach (Transform child in m_LegendContainer)
            {
                Destroy(child.gameObject);
            }

            // Create legend items for each stage type
            CreateLegendItem("Combat", m_CombatStageIcon, "Combat stages test your battle skills against enemies.");
            CreateLegendItem("Challenge", m_ChallengeStageIcon,
                "Challenge stages offer special challenges that aren't just combat.");
            CreateLegendItem("Boss", m_BossStageIcon,
                "Boss stages feature powerful enemies that guard the way to the next zone.");
            CreateLegendItem("Shop", m_ShopStageIcon, "Shop stages allow you to purchase items and upgrades.");
            CreateLegendItem("Healing", m_HealingStageIcon, "Healing stages restore your health and resources.");
            CreateLegendItem("Mystery", m_MysteryStageIcon,
                "Mystery stages can contain any type of encounter - it's a surprise!");
            CreateLegendItem("Next Zone", m_InterzoneStageIcon, "Leads to the next zone after defeating the boss.");
        }

        /// <summary>
        /// Creates a legend item with icon and text
        /// </summary>
        private void CreateLegendItem(string stageName, Sprite icon, string description)
        {
            GameObject legendItem = Instantiate(m_LegendItemPrefab, m_LegendContainer);
            LegendItemUI legendItemUI = legendItem.GetComponent<LegendItemUI>();

            if (legendItemUI != null)
            {
                legendItemUI.Initialize(stageName, icon, description, this);
            }
        }

        /// <summary>
        /// Builds the dungeon map visualization
        /// </summary>
        private void BuildDungeonMap()
        {
            if (m_DungeonManager == null)
            {
                Debug.LogError("DungeonMapUI: DungeonManager reference is null!");
                return;
            }

            // Get stage map from manager
            var stageMap = m_DungeonManager.GetStageMap();
            if (stageMap == null || stageMap.Count == 0)
            {
                Debug.LogWarning("DungeonMapUI: No stages to display!");
                return;
            }

            // Get current zone info
            var currentZone = m_DungeonManager.CurrentZoneDefinition;
            if (currentZone != null)
            {
                // Update zone info
                m_ZoneNameText.text = $"- {currentZone.ZoneName} -";
                m_ZoneProgressText.text = $"Zone {m_DungeonManager.ZoneIndex + 1}/{m_DungeonManager.ZoneCount}";
            }

            // Organize nodes by depth
            OrganizeNodesByDepth(stageMap);

            // Layout nodes
            LayoutStageNodes();

            // Create connections
            CreateStageConnections(stageMap);

            // Recenter the map
            RecenterMapLayout();

            // Refresh all nodes
            RefreshAllNodes();
        }

        /// <summary>
        /// Recalculates the center of all nodes and adjusts their positions to center the map
        /// </summary>
        private void RecenterMapLayout()
        {
            if (m_StageUIComponents.Count == 0)
                return;

            // Calculate bounds of all nodes
            m_MapBoundsMin = new Vector2(float.MaxValue, float.MaxValue);
            m_MapBoundsMax = new Vector2(float.MinValue, float.MinValue);

            // Find the min and max positions of all nodes
            foreach (var stageUI in m_StageUIComponents.Values)
            {
                RectTransform rectTransform = stageUI.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    Vector2 position = rectTransform.anchoredPosition;

                    // Update min/max positions
                    m_MapBoundsMin.x = Mathf.Min(m_MapBoundsMin.x, position.x);
                    m_MapBoundsMin.y = Mathf.Min(m_MapBoundsMin.y, position.y);
                    m_MapBoundsMax.x = Mathf.Max(m_MapBoundsMax.x, position.x);
                    m_MapBoundsMax.y = Mathf.Max(m_MapBoundsMax.y, position.y);
                }
            }

            // Calculate the center of all nodes
            Vector2 center = (m_MapBoundsMin + m_MapBoundsMax) * 0.5f;

            // Calculate the required offset to center everything in the container
            Vector2 offset = -center;

            // Apply offset to all nodes
            foreach (var stageUI in m_StageUIComponents.Values)
            {
                RectTransform rectTransform = stageUI.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition += offset;
                }
            }
            
            // Update drag controller bounds
            UpdateMapBounds(m_MapBoundsMin, m_MapBoundsMax);

            // Recreate all connections with the new positions
            // This is easier than trying to adjust existing connections
            foreach (var connection in m_ConnectionObjects)
            {
                Destroy(connection);
            }

            m_ConnectionObjects.Clear();

            // Recreate connections
            var stageMap = m_DungeonManager.GetStageMap();
            if (stageMap != null)
            {
                CreateStageConnections(stageMap);
            }
        }

        /// <summary>
        /// Organizes the stage nodes by depth for layout purposes
        /// </summary>
        private void OrganizeNodesByDepth(Dictionary<string, StageNode> stageMap)
        {
            m_NodesByDepth.Clear();

            // Create a temporary copy of the values to iterate over
            List<StageNode> stageNodes = new List<StageNode>(stageMap.Values);

            // First, build depth mapping
            foreach (var stage in stageNodes)
            {
                int depth = CalculateDepthFromRoot(stage, stageMap);

                if (!m_NodesByDepth.ContainsKey(depth))
                {
                    m_NodesByDepth[depth] = new List<StageNode>();
                }

                m_NodesByDepth[depth].Add(stage);
            }

            // Create dictionaries to track parent-child relationships for improved layout
            Dictionary<StageNode, List<StageNode>> nodeChildren = new Dictionary<StageNode, List<StageNode>>();
            Dictionary<StageNode, List<StageNode>> nodeParents = new Dictionary<StageNode, List<StageNode>>();

            // Build parent-child relationships
            foreach (var node in stageNodes)
            {
                // Initialize collections
                if (!nodeChildren.ContainsKey(node))
                    nodeChildren[node] = new List<StageNode>();

                if (!nodeParents.ContainsKey(node))
                    nodeParents[node] = new List<StageNode>();

                // For each next stage, add this node as parent
                foreach (var nextStage in node.NextStages)
                {
                    if (nextStage == null) continue;

                    if (!nodeChildren.ContainsKey(node))
                        nodeChildren[node] = new List<StageNode>();

                    if (!nodeParents.ContainsKey(nextStage))
                        nodeParents[nextStage] = new List<StageNode>();

                    nodeChildren[node].Add(nextStage);
                    nodeParents[nextStage].Add(node);
                }
            }

            // Process depths in order, starting from the top (0)
            List<int> depths = m_NodesByDepth.Keys.OrderBy(d => d).ToList();

            // For the root node/level, we don't need special ordering
            if (depths.Count > 0)
            {
                int rootDepth = depths[0];
                if (m_NodesByDepth.ContainsKey(rootDepth))
                {
                    // Sort by original X position
                    m_NodesByDepth[rootDepth] = m_NodesByDepth[rootDepth]
                        .OrderBy(n => n.GridPosition.x)
                        .ToList();
                }
            }

            // For each subsequent depth, order nodes based on their parents' positions
            for (int i = 1; i < depths.Count; i++)
            {
                int depth = depths[i];
                List<StageNode> nodesAtDepth = m_NodesByDepth[depth];

                if (nodesAtDepth.Count <= 1)
                    continue; // No need to sort a single node

                // Dictionary to track node indices at this depth for optimization
                Dictionary<StageNode, int> nodeIndices = new Dictionary<StageNode, int>();
                for (int j = 0; j < nodesAtDepth.Count; j++)
                {
                    nodeIndices[nodesAtDepth[j]] = j;
                }

                // Group nodes by their parent nodes
                var nodeGroups = new Dictionary<StageNode, List<StageNode>>();

                foreach (var node in nodesAtDepth)
                {
                    // Get all parent nodes
                    if (!nodeParents.ContainsKey(node))
                        continue;

                    var parents = nodeParents[node];

                    foreach (var parent in parents)
                    {
                        if (!nodeGroups.ContainsKey(parent))
                            nodeGroups[parent] = new List<StageNode>();

                        nodeGroups[parent].Add(node);
                    }
                }

                // Now order the nodes based on their parents' positions
                List<StageNode> orderedNodes = new List<StageNode>();

                // Get previous depth nodes in their visual order
                var prevDepth = depths[i - 1];
                var prevDepthNodes = m_NodesByDepth[prevDepth];

                // Process children in order of parent nodes
                foreach (var parentNode in prevDepthNodes)
                {
                    if (nodeGroups.ContainsKey(parentNode))
                    {
                        // Add all children of this parent, preserving their original relative order
                        var childNodes = nodeGroups[parentNode]
                            .OrderBy(n => n.GridPosition.x)
                            .ToList();

                        foreach (var childNode in childNodes)
                        {
                            if (!orderedNodes.Contains(childNode))
                                orderedNodes.Add(childNode);
                        }
                    }
                }

                // Add any remaining nodes that weren't processed
                foreach (var node in nodesAtDepth)
                {
                    if (!orderedNodes.Contains(node))
                        orderedNodes.Add(node);
                }

                // Update the ordered list
                m_NodesByDepth[depth] = orderedNodes;
            }

            // Apply a force-directed algorithm to reduce edge crossings
            // We'll do multiple passes to improve the result
            const int optimizationPasses = 4;

            for (int pass = 0; pass < optimizationPasses; pass++)
            {
                // Skip the first level since it's already positioned
                for (int i = 1; i < depths.Count; i++)
                {
                    int depth = depths[i];
                    List<StageNode> nodesAtDepth = m_NodesByDepth[depth];

                    if (nodesAtDepth.Count <= 1)
                        continue; // No need to optimize a single node

                    // Calculate the ideal position for each node based on its parents
                    var idealPositions = new Dictionary<StageNode, float>();

                    foreach (var node in nodesAtDepth)
                    {
                        if (!nodeParents.ContainsKey(node) || nodeParents[node].Count == 0)
                            continue;

                        var parents = nodeParents[node];

                        // Calculate the average index of parents
                        float avgParentPosition = 0;
                        int validParentCount = 0;

                        foreach (var parent in parents)
                        {
                            // Find the parent's index in its depth level
                            for (int d = i - 1; d >= 0; d--)
                            {
                                int parentDepth = depths[d];
                                int parentIndex = m_NodesByDepth[parentDepth].IndexOf(parent);

                                if (parentIndex >= 0)
                                {
                                    avgParentPosition += parentIndex;
                                    validParentCount++;
                                    break;
                                }
                            }
                        }

                        if (validParentCount > 0)
                        {
                            avgParentPosition /= validParentCount;
                            idealPositions[node] = avgParentPosition;
                        }
                    }

                    // Sort nodes based on their ideal positions
                    if (idealPositions.Count > 0)
                    {
                        var newOrder = nodesAtDepth
                            .OrderBy(n => idealPositions.ContainsKey(n) ? idealPositions[n] : float.MaxValue)
                            .ToList();

                        // Check if the ordering has changed
                        bool hasChanged = false;
                        for (int j = 0; j < nodesAtDepth.Count; j++)
                        {
                            if (nodesAtDepth[j] != newOrder[j])
                            {
                                hasChanged = true;
                                break;
                            }
                        }

                        if (hasChanged)
                        {
                            m_NodesByDepth[depth] = newOrder;
                        }
                    }
                }

                // Now do a second pass to consider children as well (bottom-up adjustment)
                if (pass >= 2) // Only do this in later passes
                {
                    for (int i = depths.Count - 2; i >= 0; i--)
                    {
                        int depth = depths[i];
                        List<StageNode> nodesAtDepth = m_NodesByDepth[depth];

                        if (nodesAtDepth.Count <= 1)
                            continue;

                        // Calculate the ideal position for each node based on its children
                        var idealPositions = new Dictionary<StageNode, float>();

                        foreach (var node in nodesAtDepth)
                        {
                            if (!nodeChildren.ContainsKey(node) || nodeChildren[node].Count == 0)
                                continue;

                            var children = nodeChildren[node];

                            // Calculate the average index of children
                            float avgChildPosition = 0;
                            int validChildCount = 0;

                            foreach (var child in children)
                            {
                                // Find the child's index in its depth level
                                for (int d = i + 1; d < depths.Count; d++)
                                {
                                    int childDepth = depths[d];
                                    int childIndex = m_NodesByDepth[childDepth].IndexOf(child);

                                    if (childIndex >= 0)
                                    {
                                        avgChildPosition += childIndex;
                                        validChildCount++;
                                        break;
                                    }
                                }
                            }

                            if (validChildCount > 0)
                            {
                                avgChildPosition /= validChildCount;
                                idealPositions[node] = avgChildPosition;
                            }
                        }

                        // Sort nodes based on their ideal positions from children
                        if (idealPositions.Count > 0)
                        {
                            var newOrder = nodesAtDepth
                                .OrderBy(n => idealPositions.ContainsKey(n) ? idealPositions[n] : float.MaxValue)
                                .ToList();

                            // Check if the ordering has changed
                            bool hasChanged = false;
                            for (int j = 0; j < nodesAtDepth.Count; j++)
                            {
                                if (nodesAtDepth[j] != newOrder[j])
                                {
                                    hasChanged = true;
                                    break;
                                }
                            }

                            if (hasChanged)
                            {
                                m_NodesByDepth[depth] = newOrder;
                            }
                        }
                    }
                }

                // Final crossings reduction pass - detect and fix obvious crossings
                if (pass == optimizationPasses - 1)
                {
                    for (int i = 1; i < depths.Count; i++)
                    {
                        int depth = depths[i];
                        List<StageNode> nodesAtDepth = m_NodesByDepth[depth];

                        if (nodesAtDepth.Count <= 1)
                            continue;

                        // Find obvious edge crossings and swap nodes to fix them
                        bool madeSwap;
                        int maxIterations = 100; // Prevent infinite loop
                        int iterations = 0;
                        do
                        {
                            madeSwap = false;

                            for (int j = 0; j < nodesAtDepth.Count - 1; j++)
                            {
                                var nodeA = nodesAtDepth[j];
                                var nodeB = nodesAtDepth[j + 1];

                                if (!nodeParents.ContainsKey(nodeA) || !nodeParents.ContainsKey(nodeB))
                                    continue;

                                var parentsA = nodeParents[nodeA];
                                var parentsB = nodeParents[nodeB];

                                // Check if there are obvious crossings
                                float avgParentIndexA = 0;
                                float avgParentIndexB = 0;

                                int countA = 0;
                                int countB = 0;

                                // Calculate average parent indices
                                for (int d = i - 1; d >= 0; d--)
                                {
                                    int parentDepth = depths[d];
                                    var parentDepthNodes = m_NodesByDepth[parentDepth];

                                    foreach (var parent in parentsA)
                                    {
                                        int idx = parentDepthNodes.IndexOf(parent);
                                        if (idx >= 0)
                                        {
                                            avgParentIndexA += idx;
                                            countA++;
                                        }
                                    }

                                    foreach (var parent in parentsB)
                                    {
                                        int idx = parentDepthNodes.IndexOf(parent);
                                        if (idx >= 0)
                                        {
                                            avgParentIndexB += idx;
                                            countB++;
                                        }
                                    }

                                    if (countA > 0 && countB > 0)
                                        break;
                                }

                                if (countA > 0)
                                    avgParentIndexA /= countA;

                                if (countB > 0)
                                    avgParentIndexB /= countB;

                                // If B's average parent index is less than A's, swap them
                                if (countA > 0 && countB > 0 && avgParentIndexB < avgParentIndexA)
                                {
                                    // Swap positions
                                    nodesAtDepth[j] = nodeB;
                                    nodesAtDepth[j + 1] = nodeA;
                                    madeSwap = true;
                                }
                            }

                            iterations++;
                        } while (madeSwap && iterations < maxIterations);
                    }
                }
            }
        }

        /// <summary>
        /// Calculates a node's depth from the root
        /// </summary>
        private int CalculateDepthFromRoot(StageNode node, Dictionary<string, StageNode> stageMap)
        {
            // In the DungeonManager, the y position is already set based on depth
            // So we'll just use that directly
            return (int)Mathf.Round(node.GridPosition.y);
        }

        /// <summary>
        /// Layouts all stage nodes in the container
        /// </summary>
        private void LayoutStageNodes()
        {
            if (m_NodesByDepth.Count == 0)
                return;
                
            int maxDepth = m_NodesByDepth.Keys.Count > 0 ? m_NodesByDepth.Keys.Max() : 0;
            
            // Find the maximum number of nodes in any layer to determine grid positions
            int maxNodesInAnyLayer = 0;
            foreach (var depthNodes in m_NodesByDepth.Values)
            {
                maxNodesInAnyLayer = Mathf.Max(maxNodesInAnyLayer, depthNodes.Count);
            }
            
            // Create a grid to track which positions are occupied at each depth
            Dictionary<int, bool[]> occupiedPositions = new Dictionary<int, bool[]>();
            
            // Create position assignments for each node
            Dictionary<StageNode, int> nodePositions = new Dictionary<StageNode, int>();
            
            // Initialize occupied positions grid
            for (int depth = 0; depth <= maxDepth; depth++)
            {
                occupiedPositions[depth] = new bool[maxNodesInAnyLayer];
            }
            
            // First, place the special layers (start, boss, interzone) in centered positions
            PlaceSpecialLayers(maxNodesInAnyLayer, nodePositions, occupiedPositions);
            
            // Next, place remaining nodes to avoid crossed lines
            PlaceRemainingNodes(maxNodesInAnyLayer, nodePositions, occupiedPositions);
            
            // Now create the actual node objects using calculated positions
            CreateNodeObjects(maxNodesInAnyLayer, nodePositions);
        }

        /// <summary>
        /// Places special layers (start, boss, interzone) in centered positions
        /// </summary>
        private void PlaceSpecialLayers(int maxNodesInAnyLayer, Dictionary<StageNode, int> nodePositions, Dictionary<int, bool[]> occupiedPositions)
        {
            int maxDepth = m_NodesByDepth.Keys.Count > 0 ? m_NodesByDepth.Keys.Max() : 0;
            
            // Find first layer (depth 0) - this will be the start node
            if (m_NodesByDepth.TryGetValue(0, out var startNodes) && startNodes.Count > 0)
            {
                // Place start node at center position
                int centerPos = maxNodesInAnyLayer / 2;
                nodePositions[startNodes[0]] = centerPos;
                occupiedPositions[0][centerPos] = true;
            }
            
            // Find boss layer - typically one before the last layer
            // This is a simplification - in reality we'd need to identify boss nodes by type
            int bossDepth = maxDepth - 1;
            if (bossDepth > 0 && m_NodesByDepth.TryGetValue(bossDepth, out var bossNodes))
            {
                foreach (var node in bossNodes)
                {
                    if (node.StageType == StageType.Boss)
                    {
                        // Place boss node at center position
                        int centerPos = maxNodesInAnyLayer / 2;
                        nodePositions[node] = centerPos;
                        occupiedPositions[bossDepth][centerPos] = true;
                        break; // Assuming just one boss node
                    }
                }
            }
            
            // Find interzone layer (last layer)
            if (m_NodesByDepth.TryGetValue(maxDepth, out var interzoneNodes))
            {
                foreach (var node in interzoneNodes)
                {
                    if (node.StageType == StageType.Interzone)
                    {
                        // Place interzone node at center position
                        int centerPos = maxNodesInAnyLayer / 2;
                        nodePositions[node] = centerPos;
                        occupiedPositions[maxDepth][centerPos] = true;
                        break; // Assuming just one interzone node
                    }
                }
            }
        }

        /// <summary>
        /// Places remaining nodes to avoid crossed lines
        /// </summary>
        private void PlaceRemainingNodes(int maxNodesInAnyLayer, Dictionary<StageNode, int> nodePositions, Dictionary<int, bool[]> occupiedPositions)
        {
            int maxDepth = m_NodesByDepth.Keys.Count > 0 ? m_NodesByDepth.Keys.Max() : 0;
            
            // Create dictionaries to track parent-child relationships
            Dictionary<StageNode, List<StageNode>> nodeParents = new Dictionary<StageNode, List<StageNode>>();
            Dictionary<StageNode, List<StageNode>> nodeChildren = new Dictionary<StageNode, List<StageNode>>();
            
            // Build parent-child relationships
            foreach (var depthPair in m_NodesByDepth)
            {
                foreach (var node in depthPair.Value)
                {
                    // Initialize collections if needed
                    if (!nodeChildren.ContainsKey(node))
                        nodeChildren[node] = new List<StageNode>();
                        
                    // For each next stage, establish parent-child relationship
                    foreach (var nextStage in node.NextStages)
                    {
                        if (nextStage == null) continue;
                        
                        if (!nodeParents.ContainsKey(nextStage))
                            nodeParents[nextStage] = new List<StageNode>();
                        
                        nodeParents[nextStage].Add(node);
                        nodeChildren[node].Add(nextStage);
                    }
                }
            }
            
            // Process depths in order (top to bottom)
            for (int depth = 0; depth <= maxDepth; depth++)
            {
                if (!m_NodesByDepth.TryGetValue(depth, out var depthNodes))
                    continue;
                    
                // Skip if there are no nodes at this depth
                if (depthNodes.Count == 0)
                    continue;
                
                // First, handle special nodes (already positioned)
                var alreadyPositionedNodes = depthNodes.Where(n => nodePositions.ContainsKey(n)).ToList();
                
                // Then, create parent-to-children mapping for this depth
                Dictionary<StageNode, List<StageNode>> childrenByParent = new Dictionary<StageNode, List<StageNode>>();
                
                // For each node at this depth that's not positioned yet
                foreach (var node in depthNodes.Where(n => !nodePositions.ContainsKey(n)))
                {
                    // Find all parents
                    if (nodeParents.TryGetValue(node, out var parents) && parents.Count > 0)
                    {
                        // Only consider parents that are already positioned
                        var positionedParents = parents
                            .Where(p => nodePositions.ContainsKey(p))
                            .OrderBy(p => nodePositions[p]) // Sort by horizontal position
                            .ToList();
                        
                        // Select primary parent (prefer single parent or first positioned parent)
                        if (positionedParents.Count > 0)
                        {
                            var primaryParent = positionedParents[0];
                            
                            if (!childrenByParent.ContainsKey(primaryParent))
                                childrenByParent[primaryParent] = new List<StageNode>();
                            
                            childrenByParent[primaryParent].Add(node);
                        }
                    }
                }
                
                // Now process each parent's children as sibling groups
                // Sort parents by their position to process from left to right
                foreach (var parent in childrenByParent.Keys.OrderBy(p => nodePositions[p]))
                {
                    List<StageNode> siblings = childrenByParent[parent];
                    
                    // Skip if no siblings to position
                    if (siblings.Count == 0)
                        continue;
                        
                    int parentPos = nodePositions[parent];
                    
                    // ENHANCE: Sort siblings by their connections to already positioned nodes
                    // This helps maintain a more natural order
                    siblings = OrderSiblingsByConnections(siblings, nodeParents, nodeChildren, nodePositions);
                    
                    // Try to place siblings as a contiguous block centered under their parent
                    int siblingCount = siblings.Count;
                    
                    // Calculate ideal starting position to center under parent
                    int idealStartPos;
                    if (siblingCount % 2 == 0)
                    {
                        // Even number of siblings: center them around parent position
                        idealStartPos = parentPos - (siblingCount / 2);
                    }
                    else
                    {
                        // Odd number of siblings: place middle sibling directly below parent
                        idealStartPos = parentPos - (siblingCount / 2);
                    }
                    
                    // Keep siblings together as a group by finding a contiguous block
                    // First try the ideal position
                    int actualStartPos = TryFindContiguousBlock(depth, idealStartPos, siblingCount, maxNodesInAnyLayer, occupiedPositions, parentPos);
                    
                    // Place all siblings in the contiguous block
                    for (int i = 0; i < siblingCount; i++)
                    {
                        int pos = actualStartPos + i;
                        nodePositions[siblings[i]] = pos;
                        occupiedPositions[depth][pos] = true;
                    }
                }
                
                // Handle remaining unpositioned nodes (orphans or with unpositioned parents)
                foreach (var node in depthNodes)
                {
                    if (nodePositions.ContainsKey(node))
                        continue;
                    
                    // Try to place near the center
                    int centerPos = maxNodesInAnyLayer / 2;
                    int bestPos = FindNearestAvailablePosition(depth, centerPos, maxNodesInAnyLayer, occupiedPositions);
                    
                    if (bestPos >= 0)
                    {
                        nodePositions[node] = bestPos;
                        occupiedPositions[depth][bestPos] = true;
                    }
                }
            }
            
            // Final optimization pass to minimize edge crossings while preserving sibling relationships
            OptimizeNodePositionsToReduceCrossingsPreservingSiblings(nodePositions, nodeParents, nodeChildren, maxDepth);
        }

        /// <summary>
        /// Orders siblings by their connections to already positioned nodes
        /// </summary>
        private List<StageNode> OrderSiblingsByConnections(
            List<StageNode> siblings,
            Dictionary<StageNode, List<StageNode>> nodeParents,
            Dictionary<StageNode, List<StageNode>> nodeChildren,
            Dictionary<StageNode, int> nodePositions)
        {
            // If we only have one sibling, no need to order
            if (siblings.Count <= 1)
                return siblings;
            
            // Create a score for each sibling based on their connections
            Dictionary<StageNode, float> siblingScores = new Dictionary<StageNode, float>();
            
            foreach (var sibling in siblings)
            {
                float score = 0;
                
                // Consider children connections (if any)
                if (nodeChildren.TryGetValue(sibling, out var children))
                {
                    foreach (var child in children)
                    {
                        if (nodePositions.TryGetValue(child, out int childPos))
                        {
                            // The score is the position of the child
                            score += childPos;
                        }
                    }
                    
                    // Average the score if we have children
                    if (children.Count > 0)
                        score /= children.Count;
                }
                
                // Consider other parent connections (if multiple parents)
                if (nodeParents.TryGetValue(sibling, out var parents) && parents.Count > 1)
                {
                    float parentScore = 0;
                    int positionedParentCount = 0;
                    
                    foreach (var parent in parents)
                    {
                        if (nodePositions.TryGetValue(parent, out int parentPos))
                        {
                            parentScore += parentPos;
                            positionedParentCount++;
                        }
                    }
                    
                    // Average parent positions and add to score
                    if (positionedParentCount > 1) // More than one positioned parent
                    {
                        score = (score + (parentScore / positionedParentCount)) / 2; // Average with child score
                    }
                }
                
                siblingScores[sibling] = score;
            }
            
            // Return siblings ordered by their scores
            return siblings.OrderBy(s => siblingScores.ContainsKey(s) ? siblingScores[s] : 0).ToList();
        }

        /// <summary>
        /// Tries to find a contiguous block of positions for a sibling group
        /// </summary>
        private int TryFindContiguousBlock(
            int depth, 
            int idealStartPos, 
            int blockSize, 
            int maxPositions, 
            Dictionary<int, bool[]> occupiedPositions,
            int parentPos)
        {
            // Ensure idealStartPos is within bounds
            idealStartPos = Mathf.Clamp(idealStartPos, 0, maxPositions - blockSize);
            
            // First check if the ideal block is available
            bool idealBlockAvailable = true;
            for (int i = 0; i < blockSize; i++)
            {
                int pos = idealStartPos + i;
                if (pos >= maxPositions || occupiedPositions[depth][pos])
                {
                    idealBlockAvailable = false;
                    break;
                }
            }
            
            if (idealBlockAvailable)
                return idealStartPos;
            
            // Otherwise, search outward from the parent position
            // to find the nearest available contiguous block
            
            int maxOffset = maxPositions - blockSize + 1;
            int bestStartPos = -1;
            int minDistance = int.MaxValue;
            
            // We want to check positions near the parent first
            for (int offset = 0; offset < maxOffset; offset++)
            {
                // Try rightward from parent position
                int rightStartPos = parentPos + offset;
                if (rightStartPos + blockSize <= maxPositions)
                {
                    bool blockAvailable = true;
                    for (int i = 0; i < blockSize; i++)
                    {
                        if (rightStartPos + i >= maxPositions || occupiedPositions[depth][rightStartPos + i])
                        {
                            blockAvailable = false;
                            break;
                        }
                    }
                    
                    if (blockAvailable)
                    {
                        int distance = Mathf.Abs(rightStartPos - idealStartPos);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            bestStartPos = rightStartPos;
                        }
                    }
                }
                
                // Try leftward from parent position
                int leftStartPos = parentPos - offset - blockSize;
                if (leftStartPos >= 0)
                {
                    bool blockAvailable = true;
                    for (int i = 0; i < blockSize; i++)
                    {
                        if (leftStartPos + i < 0 || occupiedPositions[depth][leftStartPos + i])
                        {
                            blockAvailable = false;
                            break;
                        }
                    }
                    
                    if (blockAvailable)
                    {
                        int distance = Mathf.Abs(leftStartPos - idealStartPos);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            bestStartPos = leftStartPos;
                        }
                    }
                }
                
                // Break early if we found a block near the parent
                if (bestStartPos != -1 && offset > blockSize)
                    break;
            }
            
            // If we can't find a contiguous block, we'll need to create one
            // by shifting existing nodes or finding the least disruptive positions
            if (bestStartPos == -1)
            {
                // As a last resort, place siblings individually as close as possible to parent
                // and to each other (adjacent if possible)
                bestStartPos = FindBestStartPositionForSiblings(depth, blockSize, parentPos, maxPositions, occupiedPositions);
                
                // Clear space for the siblings by marking positions as occupied
                // We'll actually place them later in the calling method
                for (int i = 0; i < blockSize; i++)
                {
                    if (bestStartPos + i < maxPositions)
                    {
                        occupiedPositions[depth][bestStartPos + i] = true;
                    }
                }
            }
            
            return bestStartPos;
        }

        /// <summary>
        /// Finds the best starting position for a group of siblings when no contiguous block is available
        /// </summary>
        private int FindBestStartPositionForSiblings(
            int depth,
            int blockSize,
            int parentPos,
            int maxPositions,
            Dictionary<int, bool[]> occupiedPositions)
        {
            // Start at an ideal position centered under the parent
            int idealCenter = parentPos;
            int halfSize = blockSize / 2;
            int idealStart = Mathf.Max(0, idealCenter - halfSize);
            
            // Count how many positions are already occupied in the range we want
            int occupiedCount = 0;
            for (int i = 0; i < blockSize; i++)
            {
                int pos = idealStart + i;
                if (pos < maxPositions && occupiedPositions[depth][pos])
                {
                    occupiedCount++;
                }
            }
            
            // If more than half the positions are occupied, it's better to find a different area
            if (occupiedCount > blockSize / 2)
            {
                // Find a region with the fewest occupied positions
                int bestStart = 0;
                int minOccupied = maxPositions;
                
                for (int start = 0; start <= maxPositions - blockSize; start++)
                {
                    int occupied = 0;
                    for (int i = 0; i < blockSize; i++)
                    {
                        if (occupiedPositions[depth][start + i])
                        {
                            occupied++;
                        }
                    }
                    
                    // If we found a completely unoccupied region, use it
                    if (occupied == 0)
                    {
                        bestStart = start;
                        break;
                    }
                    
                    // Otherwise keep track of the region with fewest occupied positions
                    if (occupied < minOccupied)
                    {
                        minOccupied = occupied;
                        bestStart = start;
                    }
                }
                
                return bestStart;
            }
            
            // Otherwise, use the positions near the parent and mark them for the siblings
            return idealStart;
        }

        /// <summary>
        /// Optimizes node positions to reduce edge crossings while preserving sibling groups
        /// </summary>
        private void OptimizeNodePositionsToReduceCrossingsPreservingSiblings(
            Dictionary<StageNode, int> nodePositions, 
            Dictionary<StageNode, List<StageNode>> nodeParents,
            Dictionary<StageNode, List<StageNode>> nodeChildren,
            int maxDepth)
        {
            // Group nodes by their primary parent to identify sibling groups
            Dictionary<StageNode, List<StageNode>> siblingGroups = new Dictionary<StageNode, List<StageNode>>();
            
            // Build sibling groups
            foreach (var pair in nodeParents)
            {
                StageNode child = pair.Key;
                List<StageNode> parents = pair.Value;
                
                if (parents.Count > 0)
                {
                    // Use first parent as primary for grouping
                    StageNode primaryParent = parents[0];
                    
                    if (!siblingGroups.ContainsKey(primaryParent))
                        siblingGroups[primaryParent] = new List<StageNode>();
                        
                    siblingGroups[primaryParent].Add(child);
                }
            }
            
            // Multiple optimization passes
            const int maxPasses = 3;
            
            for (int pass = 0; pass < maxPasses; pass++)
            {
                bool madeChanges = false;
                
                // Process depths in order
                for (int depth = 1; depth <= maxDepth; depth++)
                {
                    if (!m_NodesByDepth.TryGetValue(depth, out var depthNodes))
                        continue;
                        
                    // Skip special levels (boss, interzone)
                    if (depthNodes.Any(n => n.StageType == StageType.Boss || n.StageType == StageType.Interzone))
                        continue;
                        
                    // First, try to optimize sibling group positions as whole units
                    foreach (var parent in siblingGroups.Keys)
                    {
                        var siblings = siblingGroups[parent].Where(s => m_NodesByDepth[depth].Contains(s)).ToList();
                        
                        // Skip if no siblings at this depth
                        if (siblings.Count <= 1)
                            continue;
                            
                        // Skip if not all siblings have positions
                        if (siblings.Any(s => !nodePositions.ContainsKey(s)))
                            continue;
                            
                        // Check if siblings are contiguous
                        bool areContiguous = true;
                        int minPos = int.MaxValue;
                        int maxPos = int.MinValue;
                        
                        foreach (var sibling in siblings)
                        {
                            int pos = nodePositions[sibling];
                            minPos = Mathf.Min(minPos, pos);
                            maxPos = Mathf.Max(maxPos, pos);
                        }
                        
                        // If siblings span more positions than their count, they're not contiguous
                        if (maxPos - minPos + 1 > siblings.Count)
                        {
                            areContiguous = false;
                        }
                        else
                        {
                            // Check if all positions between min and max are occupied by siblings
                            for (int pos = minPos; pos <= maxPos; pos++)
                            {
                                bool foundSibling = false;
                                foreach (var sibling in siblings)
                                {
                                    if (nodePositions[sibling] == pos)
                                    {
                                        foundSibling = true;
                                        break;
                                    }
                                }
                                
                                if (!foundSibling)
                                {
                                    areContiguous = false;
                                    break;
                                }
                            }
                        }
                        
                        // If siblings are not contiguous, try to make them contiguous
                        if (!areContiguous)
                        {
                            // Find optimal position for parent
                            int parentPos = -1;
                            if (nodePositions.TryGetValue(parent, out int pos))
                            {
                                parentPos = pos;
                            }
                            
                            // Get average position of all siblings
                            float avgPos = 0;
                            foreach (var sibling in siblings)
                            {
                                avgPos += nodePositions[sibling];
                            }
                            avgPos /= siblings.Count;
                            
                            // Determine ideal start position
                            int idealStart;
                            if (parentPos >= 0)
                            {
                                // Center under parent
                                idealStart = parentPos - (siblings.Count / 2);
                            }
                            else
                            {
                                // Center around average position
                                idealStart = Mathf.FloorToInt(avgPos - (siblings.Count / 2));
                            }
                            
                            // Try to move siblings into contiguous positions
                            List<StageNode> orderedSiblings = siblings.OrderBy(s => nodePositions[s]).ToList();
                            Dictionary<StageNode, int> newPositions = new Dictionary<StageNode, int>();
                            
                            // Check if proposed positions are available or occupied by non-siblings
                            bool canMove = true;
                            for (int i = 0; i < siblings.Count; i++)
                            {
                                int newPos = idealStart + i;
                                
                                // Check if this position is already taken by a non-sibling
                                foreach (var node in depthNodes)
                                {
                                    if (!siblings.Contains(node) && 
                                        nodePositions.TryGetValue(node, out int nodePos) && 
                                        nodePos == newPos)
                                    {
                                        canMove = false;
                                        break;
                                    }
                                }
                                
                                if (!canMove)
                                    break;
                                    
                                newPositions[orderedSiblings[i]] = newPos;
                            }
                            
                            // If we can move the siblings, update their positions
                            if (canMove)
                            {
                                foreach (var sibling in siblings)
                                {
                                    nodePositions[sibling] = newPositions[sibling];
                                }
                                
                                madeChanges = true;
                            }
                        }
                    }
                }
                
                // If no changes were made in this pass, stop optimizing
                if (!madeChanges)
                    break;
            }
        }

        /// <summary>
        /// Optimizes node positions to reduce edge crossings
        /// </summary>
        private void OptimizeNodePositionsToReduceCrossings(
            Dictionary<StageNode, int> nodePositions, 
            Dictionary<StageNode, List<StageNode>> nodeParents,
            Dictionary<StageNode, List<StageNode>> nodeChildren,
            int maxDepth)
        {
            // Multiple optimization passes
            const int maxPasses = 3;
            
            for (int pass = 0; pass < maxPasses; pass++)
            {
                bool madeChanges = false;
                
                // Process depths in order
                for (int depth = 1; depth <= maxDepth; depth++)
                {
                    if (!m_NodesByDepth.TryGetValue(depth, out var depthNodes))
                        continue;
                        
                    // Skip special levels (boss, interzone)
                    if (depthNodes.Any(n => n.StageType == StageType.Boss || n.StageType == StageType.Interzone))
                        continue;
                        
                    // Process each node at this depth
                    foreach (var node in depthNodes)
                    {
                        if (!nodePositions.ContainsKey(node))
                            continue;
                            
                        // Check if node has a single parent
                        if (nodeParents.TryGetValue(node, out var parents) && parents.Count == 1)
                        {
                            var parent = parents[0];
                            
                            if (nodePositions.TryGetValue(parent, out int parentPos))
                            {
                                int currentPos = nodePositions[node];
                                
                                // If child is not directly below parent, check if we can move it
                                if (currentPos != parentPos)
                                {
                                    // Try to find positions to swap
                                    foreach (var otherNode in depthNodes)
                                    {
                                        if (otherNode == node || !nodePositions.ContainsKey(otherNode))
                                            continue;
                                            
                                        int otherPos = nodePositions[otherNode];
                                        
                                        // If this node is better positioned for our parent...
                                        if (Mathf.Abs(otherPos - parentPos) < Mathf.Abs(currentPos - parentPos))
                                        {
                                            // Check if the other node would benefit from the swap
                                            bool otherNodeBenefits = false;
                                            
                                            if (nodeParents.TryGetValue(otherNode, out var otherParents) && otherParents.Count > 0)
                                            {
                                                // Calculate average parent position for the other node
                                                int otherAvgParentPos = 0;
                                                int otherParentCount = 0;
                                                
                                                foreach (var otherParent in otherParents)
                                                {
                                                    if (nodePositions.TryGetValue(otherParent, out int otherParentPos))
                                                    {
                                                        otherAvgParentPos += otherParentPos;
                                                        otherParentCount++;
                                                    }
                                                }
                                                
                                                if (otherParentCount > 0)
                                                {
                                                    otherAvgParentPos = Mathf.RoundToInt((float)otherAvgParentPos / otherParentCount);
                                                    
                                                    // Would other node be closer to its parents after swap?
                                                    if (Mathf.Abs(currentPos - otherAvgParentPos) < Mathf.Abs(otherPos - otherAvgParentPos))
                                                    {
                                                        otherNodeBenefits = true;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                // If other node has no parents, it benefits from any swap
                                                otherNodeBenefits = true;
                                            }
                                            
                                            // Swap if beneficial for both nodes
                                            if (otherNodeBenefits)
                                            {
                                                // Swap positions
                                                nodePositions[node] = otherPos;
                                                nodePositions[otherNode] = currentPos;
                                                madeChanges = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                
                // If no changes were made in this pass, stop optimizing
                if (!madeChanges)
                    break;
            }
        }

        /// <summary>
        /// Finds the nearest available position to the target position
        /// </summary>
        private int FindNearestAvailablePosition(int depth, int targetPos, int maxPositions, Dictionary<int, bool[]> occupiedPositions)
        {
            // Check the target position first
            if (targetPos >= 0 && targetPos < maxPositions && !occupiedPositions[depth][targetPos])
            {
                return targetPos;
            }
            
            // Expand search outward from target position
            for (int offset = 1; offset < maxPositions; offset++)
            {
                // Check position to the right
                int rightPos = targetPos + offset;
                if (rightPos < maxPositions && !occupiedPositions[depth][rightPos])
                {
                    return rightPos;
                }
                
                // Check position to the left
                int leftPos = targetPos - offset;
                if (leftPos >= 0 && !occupiedPositions[depth][leftPos])
                {
                    return leftPos;
                }
            }
            
            // If all positions are occupied, return -1
            return -1;
        }

        /// <summary>
        /// Creates the actual node objects using the calculated positions
        /// </summary>
        private void CreateNodeObjects(int maxNodesInAnyLayer, Dictionary<StageNode, int> nodePositions)
        {
            int maxDepth = m_NodesByDepth.Keys.Count > 0 ? m_NodesByDepth.Keys.Max() : 0;
            
            // Calculate total width for the grid
            float totalWidth = (maxNodesInAnyLayer - 1) * m_HorizontalSpacing;
            
            // Layout nodes by depth, starting from the bottom (0) to top (maxDepth)
            for (int depth = 0; depth <= maxDepth; depth++)
            {
                if (!m_NodesByDepth.TryGetValue(depth, out var depthNodes))
                    continue;
                
                // Determine which alignment to use for this layer
                LayerAlignment layerAlignment = m_LayerAlignment;
                
                // Override alignment for special layers if specified
                if (depth == 0 && m_UseSpecialAlignmentForFirstLayer)
                {
                    layerAlignment = m_FirstLayerAlignment;
                }
                else if (depth == maxDepth && m_UseSpecialAlignmentForLastLayer)
                {
                    layerAlignment = m_LastLayerAlignment;
                }
                
                // Apply alignment adjustments
                AdjustNodePositionsForLayer(depthNodes, nodePositions, maxNodesInAnyLayer, layerAlignment);
                
                foreach (var node in depthNodes)
                {
                    if (!nodePositions.TryGetValue(node, out int position))
                        continue;
                        
                    // Create stage UI component
                    GameObject stageObj = Instantiate(m_StageNodePrefab, m_StagesContainer);
                    DungeonStageUI stageUI = stageObj.GetComponent<DungeonStageUI>();
                    
                    if (stageUI != null)
                    {
                        // Calculate position based on grid
                        float startX = -totalWidth / 2; // Center the grid
                        float xPos = startX + (position * m_HorizontalSpacing);
                        float yPos = m_InitialOffset.y + (depth * m_VerticalSpacing);
                        
                        // Set position
                        RectTransform rectTransform = stageObj.GetComponent<RectTransform>();
                        if (rectTransform != null)
                        {
                            rectTransform.anchoredPosition = new Vector2(xPos, yPos);
                        }
                        
                        // Initialize with stage data
                        stageUI.Initialize(node, GetStageIcon(node.StageType));
                        
                        // Store reference
                        m_StageUIComponents[node.ID] = stageUI;
                    }
                }
            }
        }

        /// <summary>
        /// Adjusts node positions based on the selected layer alignment
        /// </summary>
        private void AdjustNodePositionsForLayer(List<StageNode> nodesInLayer, Dictionary<StageNode, int> nodePositions, int maxNodesInAnyLayer, LayerAlignment alignment)
        {
            if (nodesInLayer.Count <= 0)
                return;
            
            // Get the current min/max positions in this layer
            int minPos = int.MaxValue;
            int maxPos = int.MinValue;
            
            foreach (var node in nodesInLayer)
            {
                if (nodePositions.TryGetValue(node, out int position))
                {
                    minPos = Mathf.Min(minPos, position);
                    maxPos = Mathf.Max(maxPos, position);
                }
            }
            
            // If all nodes have the same position, no adjustment needed
            if (minPos == maxPos || minPos == int.MaxValue)
                return;
            
            // Calculate the current width of this layer in grid positions
            int layerWidth = maxPos - minPos;
            
            // Apply alignment
            switch (alignment)
            {
                case LayerAlignment.Left:
                    // Shift all nodes to start at position 0
                    ShiftNodesInLayer(nodesInLayer, nodePositions, -minPos);
                    break;
                    
                case LayerAlignment.Center:
                    // Center nodes in the available space
                    int centerOffset = (maxNodesInAnyLayer - 1 - layerWidth) / 2;
                    ShiftNodesInLayer(nodesInLayer, nodePositions, centerOffset - minPos);
                    break;
                    
                case LayerAlignment.Right:
                    // Align to the right side of the layout
                    ShiftNodesInLayer(nodesInLayer, nodePositions, (maxNodesInAnyLayer - 1 - maxPos));
                    break;
                    
                case LayerAlignment.EvenlySpaced:
                    if (nodesInLayer.Count > 1)
                    {
                        // Create a new mapping with nodes evenly distributed
                        var orderedNodes = nodesInLayer.OrderBy(n => nodePositions.TryGetValue(n, out int pos) ? pos : 0).ToList();
                        
                        // Calculate spacing based on available width
                        float spacing = (float)(maxNodesInAnyLayer - 1) / (orderedNodes.Count - 1);
                        
                        // Reassign positions
                        for (int i = 0; i < orderedNodes.Count; i++)
                        {
                            nodePositions[orderedNodes[i]] = Mathf.RoundToInt(i * spacing);
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Shifts all nodes in a layer by a given offset
        /// </summary>
        private void ShiftNodesInLayer(List<StageNode> nodesInLayer, Dictionary<StageNode, int> nodePositions, int offset)
        {
            if (offset == 0)
                return;
            
            foreach (var node in nodesInLayer)
            {
                if (nodePositions.TryGetValue(node, out int position))
                {
                    nodePositions[node] = position + offset;
                }
            }
        }

        /// <summary>
        /// Creates connections between stage nodes
        /// </summary>
        private void CreateStageConnections(Dictionary<string, StageNode> stageMap)
        {
            foreach (var node in stageMap.Values)
            {
                if (node.NextStages.Count == 0)
                    continue;
                
                if (!m_StageUIComponents.TryGetValue(node.ID, out DungeonStageUI sourceUI))
                    continue;
                
                foreach (var nextNode in node.NextStages)
                {
                    if (!m_StageUIComponents.TryGetValue(nextNode.ID, out DungeonStageUI targetUI))
                        continue;
                    
                    // Create connection line
                    GameObject connectionObj = Instantiate(m_ConnectionPrefab, m_StagesContainer);
                    connectionObj.transform.SetAsFirstSibling(); // Put connections behind nodes
                    
                    UILineRenderer lineRenderer = connectionObj.GetComponent<UILineRenderer>();
                    
                    if (lineRenderer != null)
                    {
                        // Configure line
                        lineRenderer.LineThickness = m_LineThickness;
                        lineRenderer.LineCaps = m_LineCaps;
                        
                        // Determine line color based on path state
                        bool isCurrentPath = nextNode == m_DungeonManager.GetCurrentStage();
                        bool isUnvisitedPath = !nextNode.IsVisited;
                        
                        if (isCurrentPath)
                        {
                            lineRenderer.color = m_CurrentPathLineColor;
                        }
                        else if (isUnvisitedPath)
                        {
                            lineRenderer.color = m_UnvisitedLineColor;
                        }
                        else
                        {
                            lineRenderer.color = m_LineColor;
                        }
                        
                        if (m_ConnectionMaterial != null)
                        {
                            lineRenderer.material = m_ConnectionMaterial;
                        }
                        
                        // Set connection points (using normalized space since RelativeSize is true)
                        var containerRT = m_StagesContainer.GetComponent<RectTransform>();
                        
                        // Convert positions to local container space
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            containerRT,
                            RectTransformUtility.WorldToScreenPoint(null, sourceUI.transform.position),
                            null,
                            out var sourceLocalPos);

                        RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            containerRT,
                            RectTransformUtility.WorldToScreenPoint(null, targetUI.transform.position),
                            null,
                            out var targetLocalPos);
                        
                        // Normalize to 0-1 range
                        Rect containerRect = containerRT.rect;
                        sourceLocalPos.x = (sourceLocalPos.x - containerRect.xMin) / containerRect.width;
                        sourceLocalPos.y = (sourceLocalPos.y - containerRect.yMin) / containerRect.height;
                        targetLocalPos.x = (targetLocalPos.x - containerRect.xMin) / containerRect.width;
                        targetLocalPos.y = (targetLocalPos.y - containerRect.yMin) / containerRect.height;
                        
                        // Set points directly
                        lineRenderer.Points = new Vector2[] { sourceLocalPos, targetLocalPos };
                        lineRenderer.RelativeSize = true;
                        
                        // Store connection
                        m_ConnectionObjects.Add(connectionObj);
                    }
                }
            }
        }
        
        /// <summary>
        /// Refreshes the visual state of all stage nodes
        /// </summary>
        private void RefreshAllNodes()
        {
            var currentStage = m_DungeonManager.GetCurrentStage();
            
            foreach (var stageUI in m_StageUIComponents.Values)
            {
                bool isCurrent = stageUI.StageNode.ID == currentStage?.ID;
                bool isVisited = stageUI.StageNode.IsVisited && !isCurrent;
                
                stageUI.SetState(isCurrent, isVisited);
            }
        }
        
        /// <summary>
        /// Gets the appropriate icon for a stage type
        /// </summary>
        private Sprite GetStageIcon(StageType stageType)
        {
            return stageType switch
            {
                StageType.Combat => m_CombatStageIcon,
                StageType.Challenge => m_ChallengeStageIcon,
                StageType.Boss => m_BossStageIcon,
                StageType.Shop => m_ShopStageIcon,
                StageType.Healing => m_HealingStageIcon,
                StageType.Mystery => m_MysteryStageIcon,
                StageType.Interzone => m_InterzoneStageIcon,
                _ => null
            };
        }
        
        #endregion
    }
}