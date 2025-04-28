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
            var stageNodes = new List<StageNode>(stageMap.Values);

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
            var nodeChildren = new Dictionary<StageNode, List<StageNode>>();
            var nodeParents = new Dictionary<StageNode, List<StageNode>>();

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

            // Process depths in order, starting from the bottom (0)
            var depths = m_NodesByDepth.Keys.OrderBy(d => d).ToList();

            // For the root node/level, we don't need special ordering
            if (depths.Count > 0)
            {
                var rootDepth = depths[0];
                if (m_NodesByDepth.ContainsKey(rootDepth))
                {
                    // Sort by original X position
                    m_NodesByDepth[rootDepth] = m_NodesByDepth[rootDepth]
                        .OrderBy(n => n.GridPosition.x)
                        .ToList();
                }
            }

            // For each subsequent depth, order nodes based on their parents' positions
            for (var i = 1; i < depths.Count; i++)
            {
                var depth = depths[i];
                var nodesAtDepth = m_NodesByDepth[depth];

                if (nodesAtDepth.Count <= 1)
                    continue;

                // Group nodes by their parent nodes
                var nodeGroups = new Dictionary<StageNode, List<StageNode>>();

                foreach (var node in nodesAtDepth)
                {
                    // Get all parent nodes
                    if (!nodeParents.TryGetValue(node, out var parents))
                        continue;

                    foreach (var parent in parents)
                    {
                        if (!nodeGroups.ContainsKey(parent))
                            nodeGroups[parent] = new List<StageNode>();

                        nodeGroups[parent].Add(node);
                    }
                }

                // Now order the nodes based on their parents' positions
                var orderedNodes = new List<StageNode>();

                // Get previous depth nodes in their visual order
                var prevDepth = depths[i - 1];
                var prevDepthNodes = m_NodesByDepth[prevDepth];

                // Process children in order of parent nodes
                foreach (var parentNode in prevDepthNodes)
                {
                    if (!nodeGroups.TryGetValue(parentNode, value: out var group)) continue;

                    var childNodes = group
                        .OrderBy(n => n.GridPosition.x)
                        .ToList();

                    foreach (var childNode in childNodes.Where(childNode => !orderedNodes.Contains(childNode)))
                    {
                        orderedNodes.Add(childNode);
                    }
                }

                // Add any remaining nodes that weren't processed
                foreach (var node in nodesAtDepth.Where(node => !orderedNodes.Contains(node)))
                {
                    orderedNodes.Add(node);
                }

                // Update the ordered list
                m_NodesByDepth[depth] = orderedNodes;
            }

            // Apply a force-directed algorithm to reduce edge crossings
            // We'll do multiple passes to improve the result
            const int optimizationPasses = 4;

            for (var pass = 0; pass < optimizationPasses; pass++)
            {
                // Skip the first level since it's already positioned
                for (var i = 1; i < depths.Count; i++)
                {
                    var depth = depths[i];
                    var nodesAtDepth = m_NodesByDepth[depth];

                    if (nodesAtDepth.Count <= 1)
                        continue;

                    // Calculate the ideal position for each node based on its parents
                    var idealPositions = new Dictionary<StageNode, float>();

                    foreach (var node in nodesAtDepth)
                    {
                        if (!nodeParents.ContainsKey(node) || nodeParents[node].Count == 0)
                            continue;

                        var parents = nodeParents[node];

                        // Calculate the average index of parents
                        float avgParentPosition = 0;
                        var validParentCount = 0;

                        foreach (var parent in parents)
                        {
                            // Find the parent's index in its depth level
                            for (var d = i - 1; d >= 0; d--)
                            {
                                var parentDepth = depths[d];
                                var parentIndex = m_NodesByDepth[parentDepth].IndexOf(parent);

                                if (parentIndex < 0) continue;

                                avgParentPosition += parentIndex;
                                validParentCount++;
                                break;
                            }
                        }

                        if (validParentCount <= 0) continue;

                        avgParentPosition /= validParentCount;
                        idealPositions[node] = avgParentPosition;
                    }

                    // Sort nodes based on their ideal positions
                    if (idealPositions.Count <= 0) continue;

                    var newOrder = nodesAtDepth
                        .OrderBy(n => idealPositions.GetValueOrDefault(n, float.MaxValue))
                        .ToList();

                    var hasChanged = nodesAtDepth.Where((t, j) => t != newOrder[j]).Any();

                    if (hasChanged)
                    {
                        m_NodesByDepth[depth] = newOrder;
                    }
                }

                // Now do a second pass to consider children as well (bottom-up adjustment)
                if (pass >= 2)
                {
                    for (var i = depths.Count - 2; i >= 0; i--)
                    {
                        var depth = depths[i];
                        var nodesAtDepth = m_NodesByDepth[depth];

                        if (nodesAtDepth.Count <= 1)
                            continue;

                        var idealPositions = new Dictionary<StageNode, float>();

                        foreach (var node in nodesAtDepth)
                        {
                            if (!nodeChildren.ContainsKey(node) || nodeChildren[node].Count == 0)
                                continue;

                            var children = nodeChildren[node];

                            // Calculate the average index of children
                            var avgChildPosition = 0f;
                            var validChildCount = 0;

                            foreach (var child in children)
                            {
                                // Find the child's index in its depth level
                                for (var d = i + 1; d < depths.Count; d++)
                                {
                                    var childDepth = depths[d];
                                    var childIndex = m_NodesByDepth[childDepth].IndexOf(child);

                                    if (childIndex < 0) continue;

                                    avgChildPosition += childIndex;
                                    validChildCount++;
                                    break;
                                }
                            }

                            if (validChildCount <= 0) continue;

                            avgChildPosition /= validChildCount;
                            idealPositions[node] = avgChildPosition;
                        }

                        // Sort nodes based on their ideal positions from children
                        if (idealPositions.Count <= 0) continue;

                        var newOrder = nodesAtDepth
                            .OrderBy(n => idealPositions.GetValueOrDefault(n, float.MaxValue))
                            .ToList();

                        // Check if the ordering has changed
                        var hasChanged = nodesAtDepth.Where((t, j) => t != newOrder[j]).Any();

                        if (hasChanged)
                        {
                            m_NodesByDepth[depth] = newOrder;
                        }
                    }
                }

                // Final crossings reduction pass - detect and fix obvious crossings
                if (pass == optimizationPasses - 1)
                {
                    for (var i = 1; i < depths.Count; i++)
                    {
                        var depth = depths[i];
                        var nodesAtDepth = m_NodesByDepth[depth];

                        if (nodesAtDepth.Count <= 1)
                            continue;

                        // Find obvious edge crossings and swap nodes to fix them
                        bool madeSwap;
                        const int maxIterations = 100; // Prevent infinite loop
                        var iterations = 0;
                        do
                        {
                            madeSwap = false;

                            for (var j = 0; j < nodesAtDepth.Count - 1; j++)
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

                                var countA = 0;
                                var countB = 0;

                                // Calculate average parent indices
                                for (var d = i - 1; d >= 0; d--)
                                {
                                    var parentDepth = depths[d];
                                    var parentDepthNodes = m_NodesByDepth[parentDepth];

                                    foreach (var idx in parentsA.Select(parent => parentDepthNodes.IndexOf(parent))
                                                 .Where(idx => idx >= 0))
                                    {
                                        avgParentIndexA += idx;
                                        countA++;
                                    }

                                    foreach (var idx in parentsB.Select(parent => parentDepthNodes.IndexOf(parent))
                                                 .Where(idx => idx >= 0))
                                    {
                                        avgParentIndexB += idx;
                                        countB++;
                                    }

                                    if (countA > 0 && countB > 0)
                                        break;
                                }

                                if (countA > 0)
                                    avgParentIndexA /= countA;

                                if (countB > 0)
                                    avgParentIndexB /= countB;

                                // If B's average parent index is less than A's, swap them
                                if (countA <= 0 || countB <= 0 || !(avgParentIndexB < avgParentIndexA)) continue;

                                // Swap positions
                                nodesAtDepth[j] = nodeB;
                                nodesAtDepth[j + 1] = nodeA;
                                madeSwap = true;
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

        private void LayoutStageNodes()
        {
            if (m_NodesByDepth.Count == 0)
                return;

            string debugStr = "=== Debug Str ===\n";
            for (int i = 0; i < m_NodesByDepth.Count; ++i)
            {
                string rowStr = "";
                for (int j = 0; j < m_NodesByDepth[i].Count; ++j)
                {
                    rowStr += $"StageType: {m_NodesByDepth[i][j].StageType} || ";
                }

                debugStr += $"\t{rowStr}\n";
            }
            
            Debug.Log(debugStr);

            var maxDepth = m_NodesByDepth.Keys.Count > 0 ? m_NodesByDepth.Keys.Max() : 0;

            // Find the maximum number of nodes in any layer
            var maxNodesInAnyLayer =
                m_NodesByDepth.Values.Aggregate(0, (current, depthNodes) => Mathf.Max(current, depthNodes.Count));

            // Create a grid with enough slots for all nodes
            var nodePositions = new Dictionary<StageNode, int>();
            var occupiedPositions = new Dictionary<int, bool[]>();
            var nodeParents = new Dictionary<StageNode, List<StageNode>>();
            var nodeChildren = new Dictionary<StageNode, List<StageNode>>();

            // Initialize occupied positions grid
            for (var depth = 0; depth <= maxDepth; depth++)
            {
                occupiedPositions[depth] = new bool[maxNodesInAnyLayer];
            }

            // Build parent-child relationships
            BuildNodeRelationships(nodeParents, nodeChildren);

            // Start with special layers - we want to handle start/end nodes specially
            PlaceSpecialLayers(maxNodesInAnyLayer, nodePositions, occupiedPositions);

            // Process layers from bottom to top
            var orderedDepths = m_NodesByDepth.Keys.OrderByDescending(d => d).ToList().GetRange(2, m_NodesByDepth.Count - 2);
            
            foreach (var depth in orderedDepths)
            {
                var nodesAtDepth = m_NodesByDepth[depth];
                if (nodesAtDepth.Count == 0)
                    continue;

                // Skip nodes that are already positioned (e.g., special nodes)
                var unpositionedNodes = nodesAtDepth.Where(n => !nodePositions.ContainsKey(n)).ToList();
                if (unpositionedNodes.Count == 0)
                    continue;

                // Check if this is a layer with all leaf nodes (or no nodes have positioned children)
                var allLeafNodes = unpositionedNodes.Count == m_NodesByDepth[depth].Count;

                if (allLeafNodes)
                {
                    PositionLeafNodes(unpositionedNodes, depth, maxNodesInAnyLayer, nodePositions, occupiedPositions);
                    continue;
                }

                // First pass: Position nodes based on their children's positions
                foreach (var node in unpositionedNodes)
                {
                    int pos;
                    
                    // Check if node has positioned children
                    if (nodeChildren.TryGetValue(node, out var children) && children.Count > 0)
                    {
                        var positionedChildren = children.Where(c => nodePositions.ContainsKey(c)).ToList();
                        
                        if (positionedChildren.Count > 0)
                        {
                            // Calculate ideal position based on average of children positions
                            int avgChildrenPos = 
                                Mathf.RoundToInt((float)positionedChildren.Average(c => nodePositions[c]));
                            
                            pos = FindNearestAvailablePosition(depth, avgChildrenPos, maxNodesInAnyLayer, occupiedPositions);
                            
                            if (pos >= 0)
                            {
                                nodePositions[node] = pos;
                                occupiedPositions[depth][pos] = true;
                                continue;
                            }
                        }
                    }
                    
                    // If no positioned children or couldn't find position, check siblings
                    if (nodeParents.TryGetValue(node, out var parents) && parents.Count > 0)
                    {
                        var siblings = FindSiblings(node, parents, nodeParents, nodesAtDepth);
                        var positionedSiblings = siblings.Where(s => nodePositions.ContainsKey(s)).ToList();
                        
                        if (positionedSiblings.Count > 0)
                        {
                            int avgSiblingPos = 
                                Mathf.RoundToInt((float)positionedSiblings.Average(s => nodePositions[s]));
                            
                            pos = FindNearestAvailablePosition(depth, avgSiblingPos, maxNodesInAnyLayer, occupiedPositions);
                            
                            if (pos >= 0)
                            {
                                nodePositions[node] = pos;
                                occupiedPositions[depth][pos] = true;
                                continue;
                            }
                        }
                    }
                    
                    // Fallback to center position if no other reference points
                    int centerPos = maxNodesInAnyLayer / 2;
                    pos = FindNearestAvailablePosition(depth, centerPos, maxNodesInAnyLayer, occupiedPositions);
                    
                    if (pos >= 0)
                    {
                        nodePositions[node] = pos;
                        occupiedPositions[depth][pos] = true;
                    }
                }
            }

            // Apply layout justification based on alignment settings
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
            }

            // Create the actual node objects
            CreateNodeObjects(maxNodesInAnyLayer, nodePositions);
        }

        /// <summary>
        /// Positions all leaf nodes in a layer, preserving their order from m_NodesByDepth
        /// </summary>
        private void PositionLeafNodes(
            List<StageNode> nodes, 
            int depth, 
            int maxPositions, 
            Dictionary<StageNode, int> nodePositions, 
            Dictionary<int, bool[]> occupiedPositions)
        {
            int nodeCount = nodes.Count;
            
            // For a single node, position it in the center
            if (nodeCount == 1)
            {
                int centerPos = maxPositions / 2;
                int pos = FindNearestAvailablePosition(depth, centerPos, maxPositions, occupiedPositions);
                
                if (pos >= 0)
                {
                    nodePositions[nodes[0]] = pos;
                    occupiedPositions[depth][pos] = true;
                }
                
                return;
            }
            
            // Calculate spacing for multiple nodes
            float spacing = 0;
            int startPos = 0;
            
            // Determine the layout based on alignment
            LayerAlignment alignment = m_LayerAlignment;
            
            // Override for special layers if needed
            if (depth == 0 && m_UseSpecialAlignmentForFirstLayer)
                alignment = m_FirstLayerAlignment;
            else if (depth == m_NodesByDepth.Keys.Max() && m_UseSpecialAlignmentForLastLayer)
                alignment = m_LastLayerAlignment;
        
            switch (alignment)
            {
                case LayerAlignment.Left:
                    startPos = 0;
                    spacing = 1;
                    break;
            
                case LayerAlignment.Right:
                    startPos = maxPositions - nodeCount;
                    spacing = 1;
                    break;
            
                case LayerAlignment.EvenlySpaced:
                    spacing = (float)(maxPositions - 1) / (nodeCount - 1);
                    startPos = 0;
                    break;
            
                case LayerAlignment.Center:
                default:
                    spacing = 1;
                    startPos = (maxPositions - nodeCount) / 2;
                    break;
            }
            
            // Position each node according to its index in the original order
            for (int i = 0; i < nodeCount; i++)
            {
                int idealPos = startPos;
            
                if (alignment == LayerAlignment.EvenlySpaced && nodeCount > 1)
                    idealPos = Mathf.RoundToInt(i * spacing);
                else
                    idealPos = startPos + i;
            
                // Ensure position is within bounds
                idealPos = Mathf.Clamp(idealPos, 0, maxPositions - 1);
            
                // Find nearest available position
                int actualPos = FindNearestAvailablePosition(depth, idealPos, maxPositions, occupiedPositions);
            
                if (actualPos >= 0)
                {
                    nodePositions[nodes[i]] = actualPos;
                    occupiedPositions[depth][actualPos] = true;
                }
            }
        }

        /// <summary>
        /// Ensures siblings are placed in contiguous positions where possible
        /// </summary>
        private void EnsureSiblingsAreContiguous(
            Dictionary<StageNode, int> nodePositions,
            Dictionary<StageNode, List<StageNode>> nodeParents,
            Dictionary<int, bool[]> occupiedPositions,
            int maxDepth,
            int maxNodesInAnyLayer)
        {
            // Group nodes by their parent
            var siblingGroups = new Dictionary<StageNode, List<StageNode>>();

            foreach (var pair in nodeParents)
            {
                StageNode child = pair.Key;
                List<StageNode> parents = pair.Value;

                if (parents.Count == 0)
                    continue;

                // Use first parent as primary for grouping
                StageNode primaryParent = parents[0];

                if (!siblingGroups.ContainsKey(primaryParent))
                    siblingGroups[primaryParent] = new List<StageNode>();

                siblingGroups[primaryParent].Add(child);
            }

            // Process each depth to ensure siblings are contiguous
            for (int depth = 0; depth <= maxDepth; depth++)
            {
                if (!m_NodesByDepth.TryGetValue(depth, out var depthNodes))
                    continue;

                // For each parent with multiple children at this depth
                foreach (var parent in siblingGroups.Keys)
                {
                    var siblings = siblingGroups[parent].Where(s => depthNodes.Contains(s)).ToList();

                    if (siblings.Count <= 1)
                        continue;

                    // Get siblings' current positions and depth
                    var siblingPositions = siblings.Where(s => nodePositions.ContainsKey(s))
                        .ToDictionary(s => s, s => nodePositions[s]);

                    if (siblingPositions.Count <= 1)
                        continue;

                    // Check if siblings are already contiguous
                    var positions = siblingPositions.Values.OrderBy(p => p).ToList();
                    bool areContiguous = true;

                    for (int i = 1; i < positions.Count; i++)
                    {
                        if (positions[i] != positions[i - 1] + 1)
                        {
                            areContiguous = false;
                            break;
                        }
                    }

                    if (areContiguous)
                        continue;

                    // Calculate ideal starting position (centered around average)
                    int avgPos = Mathf.RoundToInt((float)positions.Average());
                    int idealStart = avgPos - (siblings.Count / 2);

                    // Check if we can place siblings contiguously
                    bool canPlaceContiguously = true;
                    var existingOccupied = new HashSet<int>(siblingPositions.Values);

                    for (int i = 0; i < siblings.Count; i++)
                    {
                        int pos = idealStart + i;

                        if (pos < 0 || pos >= maxNodesInAnyLayer ||
                            (occupiedPositions[depth][pos] && !existingOccupied.Contains(pos)))
                        {
                            canPlaceContiguously = false;
                            break;
                        }
                    }

                    if (!canPlaceContiguously)
                        continue;

                    // Clear current positions
                    foreach (var sibling in siblings)
                    {
                        if (siblingPositions.TryGetValue(sibling, out int oldPos))
                        {
                            occupiedPositions[depth][oldPos] = false;
                        }
                    }

                    // Place siblings contiguously
                    var orderedSiblings = siblings
                        .OrderBy(s => siblingPositions.ContainsKey(s) ? siblingPositions[s] : int.MaxValue).ToList();

                    for (int i = 0; i < orderedSiblings.Count; i++)
                    {
                        int pos = idealStart + i;

                        if (pos >= 0 && pos < maxNodesInAnyLayer)
                        {
                            nodePositions[orderedSiblings[i]] = pos;
                            occupiedPositions[depth][pos] = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Finds the nearest available position to the target position
        /// </summary>
        private int FindNearestAvailablePosition(int depth, int targetPos, int maxPositions,
            Dictionary<int, bool[]> occupiedPositions)
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
        /// Builds parent-child relationships for nodes
        /// </summary>
        private void BuildNodeRelationships(Dictionary<StageNode, List<StageNode>> nodeParents,
            Dictionary<StageNode, List<StageNode>> nodeChildren)
        {
            // Map parent-child relationships for all nodes
            foreach (var depthPair in m_NodesByDepth)
            {
                foreach (var node in depthPair.Value)
                {
                    if (!nodeChildren.ContainsKey(node))
                        nodeChildren[node] = new List<StageNode>();

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
        }

        /// <summary>
        /// Finds siblings of a node at the same depth level
        /// </summary>
        private List<StageNode> FindSiblings(StageNode node, List<StageNode> parents,
            Dictionary<StageNode, List<StageNode>> nodeParents, List<StageNode> nodesAtSameDepth)
        {
            // Find all nodes at the same depth that share at least one parent with this node
            var siblings = new HashSet<StageNode>();

            foreach (var parent in parents)
            {
                // Find all nodes that have this parent
                foreach (var possibleSibling in nodesAtSameDepth)
                {
                    if (possibleSibling == node)
                        continue;

                    if (nodeParents.TryGetValue(possibleSibling, out var possibleSiblingParents) &&
                        possibleSiblingParents.Contains(parent))
                    {
                        siblings.Add(possibleSibling);
                    }
                }
            }

            return siblings.ToList();
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
        private void AdjustNodePositionsForLayer(List<StageNode> nodesInLayer, Dictionary<StageNode, int> nodePositions,
            int maxNodesInAnyLayer, LayerAlignment alignment)
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
                        var orderedNodes = nodesInLayer
                            .OrderBy(n => nodePositions.TryGetValue(n, out int pos) ? pos : 0).ToList();

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
        private void ShiftNodesInLayer(List<StageNode> nodesInLayer, Dictionary<StageNode, int> nodePositions,
            int offset)
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
        /// Places special layers (start, boss, interzone) in centered positions
        /// </summary>
        private void PlaceSpecialLayers(int maxNodesInAnyLayer, Dictionary<StageNode, int> nodePositions,
            Dictionary<int, bool[]> occupiedPositions)
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