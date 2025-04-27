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
        [Header("References")]
        [SerializeField] private DungeonManager m_DungeonManager;
        [SerializeField] private Transform m_StagesContainer;
        [SerializeField] private GameObject m_StageNodePrefab;
        [SerializeField] private GameObject m_ConnectionPrefab;
        [SerializeField] private TextMeshProUGUI m_ZoneProgressText;
        [SerializeField] private TextMeshProUGUI m_ZoneNameText;
        [SerializeField] private RectTransform m_ClipArea;
        
        [Header("Drag Settings")]
        [SerializeField] private bool m_EnableDragging = true;
        [SerializeField] private float m_DragSpeed = 1f;
        [SerializeField] private float m_EdgeBuffer = 50f;
        [SerializeField] private bool m_ResetOnRefresh = true;
        
        [Header("Icons")]
        [SerializeField] private Sprite m_CombatStageIcon;
        [SerializeField] private Sprite m_ChallengeStageIcon;
        [SerializeField] private Sprite m_BossStageIcon;
        [SerializeField] private Sprite m_ShopStageIcon;
        [SerializeField] private Sprite m_HealingStageIcon;
        [SerializeField] private Sprite m_MysteryStageIcon;
        [SerializeField] private Sprite m_InterzoneStageIcon;
        
        [Header("Legend")]
        [SerializeField] private Transform m_LegendContainer;
        [SerializeField] private GameObject m_LegendItemPrefab;
        [SerializeField] private GameObject m_LegendDetailsPanel;
        [SerializeField] private TextMeshProUGUI m_LegendDetailsText;
        
        [Header("Connection Settings")]
        [SerializeField] private Material m_ConnectionMaterial;
        [SerializeField] private float m_LineThickness = 2f;
        [SerializeField] private Color m_LineColor = Color.white;
        [SerializeField] private Color m_CurrentPathLineColor = Color.yellow;
        [SerializeField] private Color m_UnvisitedLineColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Semi-transparent gray
        [SerializeField] private bool m_LineCaps = false;
        
        [Header("Layout Settings")]
        [SerializeField] private float m_HorizontalSpacing = 180f;
        [SerializeField] private float m_VerticalSpacing = 120f;
        [SerializeField] private Vector2 m_InitialOffset = new Vector2(0, 0);
        #endregion
        
        #region Private Fields
        private Dictionary<string, DungeonStageUI> m_StageUIComponents = new Dictionary<string, DungeonStageUI>();
        private Dictionary<int, List<StageNode>> m_NodesByDepth = new Dictionary<int, List<StageNode>>();
        private List<GameObject> m_ConnectionObjects = new List<GameObject>();
        private Vector2 m_MapBoundsMin;
        private Vector2 m_MapBoundsMax;
        private bool m_IsDragging;
        private Vector2 m_LastMousePosition;
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
        
        private void Update()
        {
            if (m_EnableDragging)
            {
                HandleMapDragging();
            }
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
                    containerRect.sizeDelta = new Vector2(5000, 5000); // Large size to accommodate content
                }
            }
        }
        
        /// <summary>
        /// Handles dragging of the map container
        /// </summary>
        private void HandleMapDragging()
        {
            if (Input.GetMouseButtonDown(0) && IsMouseOverClipArea())
            {
                m_IsDragging = true;
                m_LastMousePosition = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                m_IsDragging = false;
            }
            
            if (m_IsDragging)
            {
                Vector2 mousePosition = Input.mousePosition;
                Vector2 mouseDelta = mousePosition - m_LastMousePosition;
                m_LastMousePosition = mousePosition;
                
                // Move the container based on mouse movement
                if (mouseDelta.magnitude > 0 && m_StagesContainer != null)
                {
                    RectTransform containerRect = m_StagesContainer as RectTransform;
                    if (containerRect != null)
                    {
                        // Apply drag with speed factor
                        containerRect.anchoredPosition += mouseDelta * m_DragSpeed;
                        
                        // Constrain movement based on content bounds
                        if (m_MapBoundsMin != Vector2.zero || m_MapBoundsMax != Vector2.zero)
                        {
                            ConstrainContainerPosition(containerRect);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Constrains the container position to ensure content is visible
        /// </summary>
        private void ConstrainContainerPosition(RectTransform containerRect)
        {
            if (m_ClipArea == null || containerRect == null)
                return;
                
            // Calculate the total bounds size
            Vector2 boundsSize = m_MapBoundsMax - m_MapBoundsMin;
            
            // Get half size of the clip area
            Vector2 clipHalfSize = m_ClipArea.rect.size * 0.5f;
            
            // Calculate the min/max allowed positions for the container
            Vector2 minContainerPos = clipHalfSize - m_MapBoundsMax - new Vector2(m_EdgeBuffer, m_EdgeBuffer);
            Vector2 maxContainerPos = clipHalfSize - m_MapBoundsMin + new Vector2(m_EdgeBuffer, m_EdgeBuffer);
            
            // If the content is smaller than the clip area in either dimension, center it
            if (boundsSize.x < m_ClipArea.rect.width - (m_EdgeBuffer * 2))
            {
                float centerX = (m_MapBoundsMin.x + m_MapBoundsMax.x) * 0.5f;
                minContainerPos.x = maxContainerPos.x = -centerX;
            }
            
            if (boundsSize.y < m_ClipArea.rect.height - (m_EdgeBuffer * 2))
            {
                float centerY = (m_MapBoundsMin.y + m_MapBoundsMax.y) * 0.5f;
                minContainerPos.y = maxContainerPos.y = -centerY;
            }
            
            // Apply constraints
            Vector2 constrainedPos = containerRect.anchoredPosition;
            constrainedPos.x = Mathf.Clamp(constrainedPos.x, minContainerPos.x, maxContainerPos.x);
            constrainedPos.y = Mathf.Clamp(constrainedPos.y, minContainerPos.y, maxContainerPos.y);
            
            containerRect.anchoredPosition = constrainedPos;
        }
        
        /// <summary>
        /// Checks if the mouse is over the clip area
        /// </summary>
        private bool IsMouseOverClipArea()
        {
            if (m_ClipArea == null)
                return false;
                
            // Convert mouse position to local position in clip area
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                m_ClipArea, Input.mousePosition, null, out localPoint);
                
            // Check if the point is inside the rect
            return m_ClipArea.rect.Contains(localPoint);
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
            CreateLegendItem("Challenge", m_ChallengeStageIcon, "Challenge stages offer special challenges that aren't just combat.");
            CreateLegendItem("Boss", m_BossStageIcon, "Boss stages feature powerful enemies that guard the way to the next zone.");
            CreateLegendItem("Shop", m_ShopStageIcon, "Shop stages allow you to purchase items and upgrades.");
            CreateLegendItem("Healing", m_HealingStageIcon, "Healing stages restore your health and resources.");
            CreateLegendItem("Mystery", m_MysteryStageIcon, "Mystery stages can contain any type of encounter - it's a surprise!");
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
            
            // Update bounds after centering
            m_MapBoundsMin += offset;
            m_MapBoundsMax += offset;
            
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

            // Find the maximum number of nodes at any depth
            int maxNodesInAnyLayer = 0;
            foreach (var nodesAtDepth in m_NodesByDepth.Values)
            {
                maxNodesInAnyLayer = Mathf.Max(maxNodesInAnyLayer, nodesAtDepth.Count);
            }
            
            Debug.Log($"MaxNodesInAnyLayer: {maxNodesInAnyLayer}");

            // Create a grid positions lookup for optimized placement
            Dictionary<int, List<float>> gridPositions = new Dictionary<int, List<float>>();

            // Track node positions for the alignment logic
            Dictionary<string, Vector2> nodePositions = new Dictionary<string, Vector2>();

            // For each depth level
            for (int depth = 0; depth <= maxDepth; depth++)
            {
                if (!m_NodesByDepth.TryGetValue(depth, out var depthNodes))
                    continue;

                // Skip interzone layer (last layer)
                bool isInterzoneLayer = depthNodes.Any(n => n.StageType == StageType.Interzone);
                if (isInterzoneLayer && depth == maxDepth)
                    continue;

                int nodesInThisLayer = depthNodes.Count;
                float totalWidth = (maxNodesInAnyLayer - 1) * m_HorizontalSpacing;
                float startX = -totalWidth / 2;
                
                // Create grid positions for this layer - evenly spaced slots based on max nodes
                List<float> layerGridPositions = new List<float>();
                for (int slot = 0; slot < maxNodesInAnyLayer; slot++)
                {
                    float xPos = startX + (slot * m_HorizontalSpacing);
                    layerGridPositions.Add(xPos);
                }

                gridPositions[depth] = layerGridPositions;

                // Special handling for start node (first layer)
                bool isSpecialLayer = depth == 0;

                // Special handling for boss layer
                bool isBossLayer = depthNodes.Any(n => n.StageType == StageType.Boss);
                if (isBossLayer)
                    isSpecialLayer = true;

                if (isSpecialLayer)
                {
                    // For special layers, center the nodes
                    PlaceNodesForSpecialLayer(depthNodes, depth, layerGridPositions, maxNodesInAnyLayer, nodePositions);
                }
                else
                {
                    // For regular layers, assign grid positions while maintaining order to minimize crossings
                    AssignNodesToGridPositions(depthNodes, depth, layerGridPositions, nodePositions);
                }
            }
        }

        /// <summary>
        /// Places nodes for special layers (start, boss, interzone) by centering them
        /// </summary>
        private void PlaceNodesForSpecialLayer(List<StageNode> nodes, int depth, List<float> gridPositions, int maxSlotsCount, Dictionary<string, Vector2> nodePositions)
        {
            // Center the nodes by using the middle slots
            int nodeCount = nodes.Count;
            int middleSlotIndex = (maxSlotsCount - 1) / 2;  // For centering single nodes
            
            for (int i = 0; i < nodeCount; i++)
            {
                StageNode node = nodes[i];
                float xPos;
                
                if (nodeCount == 1)
                {
                    // Single node, place in center
                    xPos = gridPositions[middleSlotIndex];
                }
                else
                {
                    // Multiple nodes, distribute around center
                    int slotOffset = (nodeCount - 1) / 2;
                    int slotIndex = middleSlotIndex - slotOffset + i;
                    xPos = gridPositions[Mathf.Clamp(slotIndex, 0, gridPositions.Count - 1)];
                }
                
                float yPos = m_InitialOffset.y + (depth * m_VerticalSpacing);
                
                CreateNodeAt(node, xPos, yPos);
                nodePositions[node.ID] = new Vector2(xPos, yPos);
            }
        }
        
        /// <summary>
        /// Assigns regular nodes to grid positions while maintaining order to minimize crossings
        /// </summary>
        private void AssignNodesToGridPositions(List<StageNode> nodes, int depth, List<float> gridPositions, Dictionary<string, Vector2> nodePositions)
        {
            int nodeCount = nodes.Count;
            int gridSize = gridPositions.Count;
            
            // Keep track of which slots are occupied
            HashSet<int> occupiedSlots = new HashSet<int>();
            Dictionary<StageNode, int> assignedSlots = new Dictionary<StageNode, int>();
            
            // Build parent-child relationships
            Dictionary<string, List<StageNode>> parentToChildrenMap = new Dictionary<string, List<StageNode>>();
            Dictionary<StageNode, List<StageNode>> nodeParents = new Dictionary<StageNode, List<StageNode>>();
            
            // First, build parent-child relationships
            foreach (var node in nodes)
            {
                nodeParents[node] = new List<StageNode>();
                
                // Find all parents of this node
                foreach (var parentId in nodePositions.Keys)
                {
                    var potentialParent = GetNodeById(parentId);
                    if (potentialParent != null && potentialParent.NextStages.Contains(node))
                    {
                        if (!parentToChildrenMap.ContainsKey(parentId))
                        {
                            parentToChildrenMap[parentId] = new List<StageNode>();
                        }
                        parentToChildrenMap[parentId].Add(node);
                        nodeParents[node].Add(potentialParent);
                    }
                }
            }
            
            // STEP 1: Calculate the optimal slot for each parent's children
            Dictionary<string, int> parentOptimalSlots = new Dictionary<string, int>();
            
            foreach (var parentId in parentToChildrenMap.Keys)
            {
                if (!nodePositions.ContainsKey(parentId)) continue;
                
                float parentX = nodePositions[parentId].x;
                int bestSlot = 0;
                float minDistance = float.MaxValue;
                
                // Find the grid slot closest to the parent's x position
                for (int i = 0; i < gridPositions.Count; i++)
                {
                    float distance = Mathf.Abs(gridPositions[i] - parentX);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        bestSlot = i;
                    }
                }
                
                parentOptimalSlots[parentId] = bestSlot;
            }
            
            // STEP 2: First ensure that for each parent with multiple children, at least one child is directly below it
            HashSet<string> parentsWithDirectConnection = new HashSet<string>();
            
            foreach (var parentId in parentToChildrenMap.Keys)
            {
                if (!parentOptimalSlots.ContainsKey(parentId)) continue;
                
                var children = parentToChildrenMap[parentId];
                if (children.Count == 0) continue;
                
                int optimalSlot = parentOptimalSlots[parentId];
                
                // If the optimal slot is already occupied, try to find the closest available slot
                if (occupiedSlots.Contains(optimalSlot))
                {
                    // Try to find the nearest available slot to maintain a relatively straight connection
                    int nearestSlot = FindNearestAvailableSlot(optimalSlot, occupiedSlots, gridPositions.Count);
                    if (nearestSlot >= 0)
                    {
                        // Find the best child to place at this position (prefer one with fewer parents)
                        StageNode bestChild = children.OrderBy(c => nodeParents[c].Count).FirstOrDefault();
                        if (bestChild != null)
                        {
                            assignedSlots[bestChild] = nearestSlot;
                            occupiedSlots.Add(nearestSlot);
                            parentsWithDirectConnection.Add(parentId);
                        }
                    }
                }
                else
                {
                    // Find the best child to place at optimal position (prefer one with fewer parents)
                    StageNode bestChild = children.OrderBy(c => nodeParents[c].Count).FirstOrDefault();
                    if (bestChild != null)
                    {
                        assignedSlots[bestChild] = optimalSlot;
                        occupiedSlots.Add(optimalSlot);
                        parentsWithDirectConnection.Add(parentId);
                    }
                }
            }
            
            // STEP 3: Handle remaining nodes with single parents
            List<StageNode> singleParentNodes = nodes.Where(n => nodeParents[n].Count == 1 && !assignedSlots.ContainsKey(n)).ToList();
            List<StageNode> multiParentNodes = nodes.Where(n => nodeParents[n].Count > 1 && !assignedSlots.ContainsKey(n)).ToList();
            List<StageNode> noParentNodes = nodes.Where(n => nodeParents[n].Count == 0).ToList();
            
            // Sort single parent nodes by parent position
            singleParentNodes.Sort((a, b) => {
                string parentA = nodeParents[a][0].ID;
                string parentB = nodeParents[b][0].ID;
                
                int slotA = parentOptimalSlots.ContainsKey(parentA) ? parentOptimalSlots[parentA] : 0;
                int slotB = parentOptimalSlots.ContainsKey(parentB) ? parentOptimalSlots[parentB] : 0;
                
                return slotA.CompareTo(slotB);
            });
            
            // Create a mapping of nodes that share the same parent
            Dictionary<string, List<StageNode>> parentGroups = new Dictionary<string, List<StageNode>>();
            
            foreach (var node in singleParentNodes)
            {
                string parentId = nodeParents[node][0].ID;
                
                if (!parentGroups.ContainsKey(parentId))
                {
                    parentGroups[parentId] = new List<StageNode>();
                }
                
                parentGroups[parentId].Add(node);
            }
            
            // STEP 4: Place single-parent nodes by groups
            foreach (var parentId in parentGroups.Keys)
            {
                if (!parentOptimalSlots.ContainsKey(parentId))
                    continue;
                
                int optimalSlot = parentOptimalSlots[parentId];
                var childGroup = parentGroups[parentId];
                int groupSize = childGroup.Count;
                
                if (groupSize == 0) continue;
                
                // If we already placed a direct child for this parent, we can be more flexible with the rest
                bool hasDirectConnection = parentsWithDirectConnection.Contains(parentId);
                
                // Calculate starting slot - try to center the group under the parent
                int startSlot;
                
                if (hasDirectConnection)
                {
                    // More flexibility as we already have a direct connection
                    startSlot = Mathf.Max(0, optimalSlot - (groupSize - 1) / 2);
                }
                else
                {
                    // Try to ensure one node is directly aligned with parent
                    int centerNodeIndex = groupSize / 2;
                    startSlot = Mathf.Max(0, optimalSlot - centerNodeIndex);
                }
                
                // Make sure we don't go off the grid
                if (startSlot + groupSize > gridPositions.Count)
                {
                    startSlot = gridPositions.Count - groupSize;
                }
                
                // Ensure start slot is valid
                startSlot = Mathf.Max(0, startSlot);
                
                // Check if there's a continuous block of slots available
                bool foundValidBlock = false;
                int attemptsLeft = gridPositions.Count; // Limit search to prevent infinite loop
                
                while (!foundValidBlock && attemptsLeft > 0)
                {
                    bool blockValid = true;
                    
                    // Check if all slots in the block are available
                    for (int i = 0; i < groupSize; i++)
                    {
                        int slot = startSlot + i;
                        if (slot >= gridPositions.Count || occupiedSlots.Contains(slot))
                        {
                            blockValid = false;
                            break;
                        }
                    }
                    
                    if (blockValid)
                    {
                        foundValidBlock = true;
                    }
                    else
                    {
                        // Try next position
                        startSlot++;
                        
                        // Wrap around if needed
                        if (startSlot + groupSize > gridPositions.Count)
                        {
                            startSlot = 0;
                        }
                        
                        attemptsLeft--;
                    }
                }
                
                // If we found a valid block, assign the nodes
                if (foundValidBlock)
                {
                    for (int i = 0; i < groupSize; i++)
                    {
                        int slot = startSlot + i;
                        assignedSlots[childGroup[i]] = slot;
                        occupiedSlots.Add(slot);
                    }
                    
                    // Mark that we've created a direct connection for this parent
                    if (!hasDirectConnection)
                    {
                        // Check if any of these nodes are directly aligned with the parent
                        bool foundDirectConnection = false;
                        for (int i = 0; i < groupSize; i++)
                        {
                            if (startSlot + i == optimalSlot)
                            {
                                foundDirectConnection = true;
                                break;
                            }
                        }
                        
                        if (foundDirectConnection)
                        {
                            parentsWithDirectConnection.Add(parentId);
                        }
                    }
                }
                else
                {
                    // Fallback: Place nodes individually as close as possible to optimal
                    if (!hasDirectConnection)
                    {
                        // Try to ensure at least one child is aligned directly with parent
                        int directSlot = optimalSlot;
                        if (!occupiedSlots.Contains(directSlot))
                        {
                            assignedSlots[childGroup[0]] = directSlot;
                            occupiedSlots.Add(directSlot);
                            parentsWithDirectConnection.Add(parentId);
                            childGroup.RemoveAt(0);
                        }
                        else
                        {
                            // If optimal slot is occupied, try to find the nearest one
                            int nearestSlot = FindNearestAvailableSlot(optimalSlot, occupiedSlots, gridPositions.Count);
                            if (nearestSlot >= 0)
                            {
                                assignedSlots[childGroup[0]] = nearestSlot;
                                occupiedSlots.Add(nearestSlot);
                                parentsWithDirectConnection.Add(parentId);
                                childGroup.RemoveAt(0);
                            }
                        }
                    }
                    
                    // Place remaining nodes
                    foreach (var node in childGroup)
                    {
                        int bestAvailableSlot = FindNearestAvailableSlot(optimalSlot, occupiedSlots, gridPositions.Count);
                        
                        if (bestAvailableSlot >= 0)
                        {
                            assignedSlots[node] = bestAvailableSlot;
                            occupiedSlots.Add(bestAvailableSlot);
                        }
                    }
                }
            }
            
            // STEP 5: Place nodes with multiple parents based on average parent position
            foreach (var node in multiParentNodes)
            {
                float avgX = 0;
                int count = 0;
                int primaryParentSlot = -1;
                
                // First check if any of the parents still don't have a direct connection
                StageNode primaryParent = null;
                foreach (var parent in nodeParents[node])
                {
                    if (!parentsWithDirectConnection.Contains(parent.ID) && parentOptimalSlots.ContainsKey(parent.ID))
                    {
                        primaryParent = parent;
                        primaryParentSlot = parentOptimalSlots[parent.ID];
                        break;
                    }
                    
                    if (nodePositions.ContainsKey(parent.ID))
                    {
                        avgX += nodePositions[parent.ID].x;
                        count++;
                    }
                }
                
                int targetSlot;
                
                // If we found a parent that needs a direct connection, prioritize that
                if (primaryParent != null && primaryParentSlot >= 0)
                {
                    targetSlot = primaryParentSlot;
                    
                    // If this slot is available, use it
                    if (!occupiedSlots.Contains(targetSlot))
                    {
                        assignedSlots[node] = targetSlot;
                        occupiedSlots.Add(targetSlot);
                        parentsWithDirectConnection.Add(primaryParent.ID);
                        continue;
                    }
                    
                    // Try to find nearest available slot
                    int nearestSlot = FindNearestAvailableSlot(targetSlot, occupiedSlots, gridPositions.Count);
                    if (nearestSlot >= 0)
                    {
                        assignedSlots[node] = nearestSlot;
                        occupiedSlots.Add(nearestSlot);
                        parentsWithDirectConnection.Add(primaryParent.ID);
                        continue;
                    }
                }
                
                // If we couldn't prioritize a single parent, use average position
                if (count > 0)
                {
                    avgX /= count;
                    
                    // Find closest grid position
                    int bestSlot = 0;
                    float minDistance = float.MaxValue;
                    
                    for (int i = 0; i < gridPositions.Count; i++)
                    {
                        float distance = Mathf.Abs(gridPositions[i] - avgX);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            bestSlot = i;
                        }
                    }
                    
                    // Find nearest available slot
                    int availableSlot = FindNearestAvailableSlot(bestSlot, occupiedSlots, gridPositions.Count);
                    
                    if (availableSlot >= 0)
                    {
                        assignedSlots[node] = availableSlot;
                        occupiedSlots.Add(availableSlot);
                    }
                }
            }
            
            // STEP 6: Place nodes with no parents (if any)
            foreach (var node in noParentNodes)
            {
                // Just find any available slot
                for (int slot = 0; slot < gridPositions.Count; slot++)
                {
                    if (!occupiedSlots.Contains(slot))
                    {
                        assignedSlots[node] = slot;
                        occupiedSlots.Add(slot);
                        break;
                    }
                }
            }
            
            // STEP 7: Final pass to minimize remaining crossings by swapping adjacent nodes
            bool madeSwap = true;
            int maxPasses = 5; // Limit iterations
            int pass = 0;
            
            while (madeSwap && pass < maxPasses)
            {
                madeSwap = false;
                pass++;
                
                // Look at each adjacent pair of nodes
                List<StageNode> nodesBySlot = new List<StageNode>();
                foreach (var node in nodes)
                {
                    if (assignedSlots.ContainsKey(node))
                    {
                        nodesBySlot.Add(node);
                    }
                }
                
                // Sort by assigned slot
                nodesBySlot.Sort((a, b) => assignedSlots[a].CompareTo(assignedSlots[b]));
                
                for (int i = 0; i < nodesBySlot.Count - 1; i++)
                {
                    var nodeA = nodesBySlot[i];
                    var nodeB = nodesBySlot[i + 1];
                    
                    int slotA = assignedSlots[nodeA];
                    int slotB = assignedSlots[nodeB];
                    
                    // Only consider adjacent slots
                    if (slotB != slotA + 1) continue;
                    
                    // Calculate current edge crossings
                    int currentCrossings = CalculatePairCrossings(nodeA, nodeB, nodeParents, nodePositions);
                    
                    // Calculate crossings if we swap
                    int swappedCrossings = CalculatePairCrossings(nodeB, nodeA, nodeParents, nodePositions);
                    
                    // Check if the swap would break a direct parent-child alignment
                    bool breaksDirect = false;
                    foreach (var parent in nodeParents[nodeA])
                    {
                        if (parentOptimalSlots.ContainsKey(parent.ID) && parentOptimalSlots[parent.ID] == slotA)
                        {
                            breaksDirect = true;
                            break;
                        }
                    }
                    
                    foreach (var parent in nodeParents[nodeB])
                    {
                        if (parentOptimalSlots.ContainsKey(parent.ID) && parentOptimalSlots[parent.ID] == slotB)
                        {
                            breaksDirect = true;
                            break;
                        }
                    }
                    
                    // Only swap if it reduces crossings and doesn't break direct alignments
                    if (swappedCrossings < currentCrossings && !breaksDirect)
                    {
                        assignedSlots[nodeA] = slotB;
                        assignedSlots[nodeB] = slotA;
                        madeSwap = true;
                    }
                }
            }
            
            // STEP 8: Position nodes at their assigned slots
            foreach (var node in nodes)
            {
                int slot = 0;
                
                // Make sure this node has an assigned slot
                if (!assignedSlots.ContainsKey(node))
                {
                    // Fallback - find any available slot
                    for (int i = 0; i < gridPositions.Count; i++)
                    {
                        if (!occupiedSlots.Contains(i))
                        {
                            slot = i;
                            break;
                        }
                    }
                    
                    // If still no available slots, use modulo to distribute
                    if (occupiedSlots.Count >= gridPositions.Count)
                    {
                        slot = nodes.IndexOf(node) % gridPositions.Count;
                    }
                }
                else
                {
                    slot = assignedSlots[node];
                }
                
                // Create node at the assigned position
                slot = Mathf.Clamp(slot, 0, gridPositions.Count - 1);
                float xPos = gridPositions[slot];
                float yPos = m_InitialOffset.y + (depth * m_VerticalSpacing);
                
                CreateNodeAt(node, xPos, yPos);
                nodePositions[node.ID] = new Vector2(xPos, yPos);
            }
        }
        
        /// <summary>
        /// Calculates the number of edge crossings between two adjacent nodes
        /// </summary>
        private int CalculatePairCrossings(StageNode leftNode, StageNode rightNode, 
            Dictionary<StageNode, List<StageNode>> nodeParents, Dictionary<string, Vector2> nodePositions)
        {
            int crossings = 0;
            
            // If either node has no parents, no crossings possible
            if (!nodeParents.ContainsKey(leftNode) || !nodeParents.ContainsKey(rightNode) ||
                nodeParents[leftNode].Count == 0 || nodeParents[rightNode].Count == 0)
                return 0;
                
            // Check each pair of parent connections for crossings
            foreach (var leftParent in nodeParents[leftNode])
            {
                if (!nodePositions.ContainsKey(leftParent.ID)) continue;
                
                float leftParentX = nodePositions[leftParent.ID].x;
                
                foreach (var rightParent in nodeParents[rightNode])
                {
                    if (!nodePositions.ContainsKey(rightParent.ID)) continue;
                    
                    float rightParentX = nodePositions[rightParent.ID].x;
                    
                    // If left parent is to the right of right parent, that's a crossing
                    if (leftParentX > rightParentX)
                    {
                        crossings++;
                    }
                }
            }
            
            return crossings;
        }
        
        /// <summary>
        /// Finds the nearest available slot to the target slot
        /// </summary>
        private int FindNearestAvailableSlot(int targetSlot, HashSet<int> occupiedSlots, int maxSlots)
        {
            // If the target slot is available, use it
            if (!occupiedSlots.Contains(targetSlot))
                return targetSlot;
                
            // Try slots on both sides, increasing distance
            for (int offset = 1; offset < maxSlots; offset++)
            {
                // Try right side
                int rightSlot = targetSlot + offset;
                if (rightSlot < maxSlots && !occupiedSlots.Contains(rightSlot))
                    return rightSlot;
                    
                // Try left side
                int leftSlot = targetSlot - offset;
                if (leftSlot >= 0 && !occupiedSlots.Contains(leftSlot))
                    return leftSlot;
            }
            
            // If all slots are occupied (shouldn't happen), return -1
            return -1;
        }
        
        /// <summary>
        /// Helper to get a node by ID
        /// </summary>
        private StageNode GetNodeById(string id)
        {
            foreach (var depthEntry in m_NodesByDepth)
            {
                foreach (var node in depthEntry.Value)
                {
                    if (node.ID == id)
                        return node;
                }
            }
            return null;
        }
        
        /// <summary>
        /// Creates a node UI element at the specified position
        /// </summary>
        private void CreateNodeAt(StageNode node, float xPos, float yPos)
        {
            // Create stage UI component
            GameObject stageObj = Instantiate(m_StageNodePrefab, m_StagesContainer);
            DungeonStageUI stageUI = stageObj.GetComponent<DungeonStageUI>();
            
            if (stageUI != null)
            {
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