using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace PowerTree
{
    /// <summary>
    /// UI Manager for the power tree screen
    /// </summary>
    public class PowerTreeUI : MonoBehaviour
    {
        [SerializeField] private PowerTreeManager m_TreeManager;
        [SerializeField] private Transform[] m_BranchContainers; // One per branch
        [SerializeField] private GameObject m_NodePrefab;
        [SerializeField] private GameObject m_ConnectionPrefab;
        [SerializeField] private Canvas m_RootCanvas;
        
        [Header("Connection Settings")]
        [SerializeField] private Material m_ConnectionMaterial;
        [SerializeField] private float m_LineThickness = 2f;
        [SerializeField] private Color m_LineColor = Color.white;
        [SerializeField] private bool m_LineCaps = false;

        [Header("Layout Settings")]
        [SerializeField] private float m_HorizontalSpacing = 180f;
        [SerializeField] private float m_VerticalSpacing = 120f;
        [SerializeField] private float m_BranchVerticalOffset = 300f;
        [SerializeField] private Vector2 m_InitialOffset = new Vector2(0, 0);
        
        [Header("Cheat Settings")]
        [SerializeField] private KeyCode m_LevelUpKey = KeyCode.UpArrow;
        [SerializeField] private KeyCode m_LevelDownKey = KeyCode.DownArrow;
        [SerializeField] private KeyCode m_AddShardsKey = KeyCode.S;
        [SerializeField] private int m_ShardsToAdd = 100;
        [SerializeField] private bool m_EnableCheats = false;

        private Dictionary<string, PowerNodeUI> m_NodeUIComponents = new Dictionary<string, PowerNodeUI>();
        private Dictionary<PowerBranch, Dictionary<int, List<PowerNode>>> m_NodesByDepth = new Dictionary<PowerBranch, Dictionary<int, List<PowerNode>>>();

        private Camera m_MainCamera;
        private PowerNodeUI m_CurrentlyHoveredNode = null;

        private void Awake()
        {
            m_MainCamera = Camera.main;
        }

        private void Start()
        {
            BuildTreeUI();
        }
        
        private void Update()
        {
            if (m_EnableCheats)
            {
                HandleCheatInput();
            }
        }
        
        #region Cheat System
        /// <summary>
        /// Handles cheat keyboard inputs for leveling nodes and adding Aether shards
        /// </summary>
        private void HandleCheatInput()
        {
            // Add Aether shards
            if (Input.GetKeyDown(m_AddShardsKey))
            {
                if (m_TreeManager != null)
                {
                    m_TreeManager.AddAetherShards(m_ShardsToAdd);
                    Debug.Log($"<color=yellow>[CHEAT]</color> Added {m_ShardsToAdd} Aether shards. New total: {m_TreeManager.GetAetherShards()}");
                    RefreshAllNodes();
                }
            }

            // Handle node level manipulation (only if a node is hovered)
            if (m_CurrentlyHoveredNode != null && m_CurrentlyHoveredNode.Node != null)
            {
                string nodeID = m_CurrentlyHoveredNode.Node.nodeID;

                // Level up node
                if (Input.GetKeyDown(m_LevelUpKey))
                {
                    if (m_TreeManager.IsNodeUnlocked(nodeID))
                    {
                        // Force level up
                        bool success = m_TreeManager.ForceUpgradeNode(nodeID);
                        if (success)
                        {
                            int newLevel = m_TreeManager.GetNodeLevel(nodeID);
                            Debug.Log($"<color=yellow>[CHEAT]</color> Force upgraded node: {nodeID} to level {newLevel}");
                            RefreshAllNodes();
                        }
                    }
                    else
                    {
                        // Force unlock
                        bool success = m_TreeManager.ForceUnlockNode(nodeID);
                        if (success)
                        {
                            Debug.Log($"<color=yellow>[CHEAT]</color> Force unlocked node: {nodeID}");
                            RefreshAllNodes();
                        }
                    }
                }

                // Level down node
                if (Input.GetKeyDown(m_LevelDownKey))
                {
                    if (m_TreeManager.IsNodeUnlocked(nodeID))
                    {
                        bool success = m_TreeManager.ForceDowngradeNode(nodeID);
                        if (success)
                        {
                            if (m_TreeManager.IsNodeUnlocked(nodeID))
                            {
                                int newLevel = m_TreeManager.GetNodeLevel(nodeID);
                                Debug.Log($"<color=yellow>[CHEAT]</color> Force downgraded node: {nodeID} to level {newLevel}");
                            }
                            else
                            {
                                Debug.Log($"<color=yellow>[CHEAT]</color> Node removed: {nodeID}");
                            }
                            RefreshAllNodes();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sets the currently hovered node for cheat functionality
        /// </summary>
        public void SetHoveredNode(PowerNodeUI nodeUI)
        {
            m_CurrentlyHoveredNode = nodeUI;
        }

        /// <summary>
        /// Clears the currently hovered node
        /// </summary>
        public void ClearHoveredNode(PowerNodeUI nodeUI)
        {
            // Only clear if this is the currently hovered node
            if (m_CurrentlyHoveredNode == nodeUI)
            {
                m_CurrentlyHoveredNode = null;
            }
        }
        #endregion

        public void BuildTreeUI()
        {
            if (m_TreeManager == null)
            {
                Debug.LogError("PowerTreeUI: TreeManager reference is null!");
                return;
            }
            
            // Get node database from manager using reflection
            FieldInfo databaseField = m_TreeManager.GetType().GetField("nodeDatabase", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            PowerNodeDatabase database = null;
            if (databaseField != null)
            {
                database = databaseField.GetValue(m_TreeManager) as PowerNodeDatabase;
            }
            
            if (database == null)
            {
                Debug.LogError("PowerTreeUI: Failed to get NodeDatabase from TreeManager!");
                return;
            }

            // Clear existing UI
            foreach (var container in m_BranchContainers)
            {
                if (container == null) continue;
                
                foreach (Transform child in container)
                {
                    Destroy(child.gameObject);
                }
            }

            m_NodeUIComponents.Clear();
            m_NodesByDepth.Clear();

            // Pre-process nodes to organize them by depth for each branch
            OrganizeNodesByDepth(database);

            // Build nodes for each branch with proper positioning
            float branchYOffset = 0;
            
            for (int i = 0; i < m_BranchContainers.Length; i++)
            {
                if (m_BranchContainers[i] == null) continue;
    
                PowerBranch branch = (PowerBranch)i;
    
                // Skip branches with no nodes
                if (!m_NodesByDepth.ContainsKey(branch) || m_NodesByDepth[branch].Count == 0)
                    continue;
    
                LayoutBranchNodes(branch, m_BranchContainers[i], Vector2.zero);
    
                // Create connections for this branch
                CreateNodeConnections(branch, m_BranchContainers[i]);
            }

            // Update node states
            RefreshAllNodes();
        }
        
        private void OrganizeNodesByDepth(PowerNodeDatabase database)
        {
            foreach (PowerBranch branch in System.Enum.GetValues(typeof(PowerBranch)))
            {
                List<PowerNode> branchNodes = database.GetNodesByBranch(branch);
                if (branchNodes.Count == 0) continue;
                
                // Initialize depth dictionary for this branch
                m_NodesByDepth[branch] = new Dictionary<int, List<PowerNode>>();
                
                // Calculate node depths for this branch
                Dictionary<PowerNode, int> nodeDepths = CalculateNodeDepths(branchNodes);
                
                // Group nodes by their depth
                foreach (var kvp in nodeDepths)
                {
                    PowerNode node = kvp.Key;
                    int depth = kvp.Value;
                    
                    if (!m_NodesByDepth[branch].ContainsKey(depth))
                    {
                        m_NodesByDepth[branch][depth] = new List<PowerNode>();
                    }
                    
                    m_NodesByDepth[branch][depth].Add(node);
                }
                
                // Sort nodes at each depth horizontally
                // This ensures a more balanced tree layout
                foreach (int depth in m_NodesByDepth[branch].Keys)
                {
                    SortNodesHorizontally(m_NodesByDepth[branch][depth]);
                }
            }
        }
        
        private Dictionary<PowerNode, int> CalculateNodeDepths(List<PowerNode> nodes)
        {
            Dictionary<PowerNode, int> depths = new Dictionary<PowerNode, int>();
            
            // First pass: identify root nodes (nodes with no requirements)
            foreach (PowerNode node in nodes)
            {
                if (node.requiredNodes == null || node.requiredNodes.Length == 0)
                {
                    depths[node] = 0; // Root level
                }
            }
            
            // Second pass: calculate depths for all other nodes
            bool changed;
            do
            {
                changed = false;
                
                foreach (PowerNode node in nodes)
                {
                    // Skip if already processed
                    if (depths.ContainsKey(node))
                        continue;
                    
                    // Check if all required nodes have depths
                    bool allRequirementsProcessed = true;
                    int maxRequiredDepth = -1;
                    
                    if (node.requiredNodes != null && node.requiredNodes.Length > 0)
                    {
                        foreach (PowerNode requiredNode in node.requiredNodes.Where(r => nodes.Contains(r)))
                        {
                            if (!depths.ContainsKey(requiredNode))
                            {
                                allRequirementsProcessed = false;
                                break;
                            }
                            
                            maxRequiredDepth = Mathf.Max(maxRequiredDepth, depths[requiredNode]);
                        }
                    }
                    
                    // If all requirements are processed, set this node's depth
                    if (allRequirementsProcessed)
                    {
                        depths[node] = maxRequiredDepth + 1;
                        changed = true;
                    }
                }
            } while (changed);
            
            // Handle any remaining nodes (circular dependencies or orphans)
            foreach (PowerNode node in nodes)
            {
                if (!depths.ContainsKey(node))
                {
                    depths[node] = 0; // Fallback for orphaned nodes
                }
            }
            
            return depths;
        }
        
        private void SortNodesHorizontally(List<PowerNode> nodes)
        {
            // This is a simple sort by node ID, but could be enhanced to use parent-child relationships
            // for more intelligent positioning
            nodes.Sort((a, b) => string.Compare(a.nodeID, b.nodeID));
        }

        private void LayoutBranchNodes(PowerBranch branch, Transform container, Vector2 offset)
        {
            var branchNodes = m_NodesByDepth[branch];
            Dictionary<string, Vector2> nodePositions = new Dictionary<string, Vector2>();

            // Track bounds of all nodes
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            List<RectTransform> allNodeRects = new List<RectTransform>();

            // Process nodes by depth, starting from the root
            foreach (int depth in branchNodes.Keys.OrderBy(d => d))
            {
                List<PowerNode> depthNodes = branchNodes[depth];
                List<NodePositionInfo> positionInfos = new List<NodePositionInfo>();

                // First calculate ideal positions for each node based on parent positions
                foreach (PowerNode node in depthNodes)
                {
                    float xPos;

                    if (depth == 0 || node.requiredNodes == null || node.requiredNodes.Length == 0)
                    {
                        // Root nodes - use default spacing
                        xPos = positionInfos.Count * m_HorizontalSpacing;
                    }
                    else
                    {
                        // Child nodes - position based on parent(s)
                        float parentXSum = 0;
                        int parentCount = 0;

                        foreach (PowerNode parent in node.requiredNodes)
                        {
                            // Skip parents that aren't in this branch
                            if (parent.branch != branch) continue;

                            if (nodePositions.TryGetValue(parent.nodeID, out Vector2 parentPos))
                            {
                                parentXSum += parentPos.x;
                                parentCount++;
                            }
                        }

                        // Use average of parent positions or default if no valid parents
                        xPos = parentCount > 0 ? parentXSum / parentCount : positionInfos.Count * m_HorizontalSpacing;
                    }

                    positionInfos.Add(new NodePositionInfo
                    {
                        Node = node,
                        IdealX = xPos
                    });
                }

                // Sort nodes by their ideal X positions
                positionInfos.Sort((a, b) => a.IdealX.CompareTo(b.IdealX));

                // Apply minimum spacing between nodes
                for (int i = 1; i < positionInfos.Count; i++)
                {
                    float minPos = positionInfos[i - 1].IdealX + m_HorizontalSpacing;
                    if (positionInfos[i].IdealX < minPos)
                    {
                        positionInfos[i].IdealX = minPos;
                    }
                }

                // Center this row
                float rowStartX = 0;
                float rowWidth = 0;

                if (positionInfos.Count > 0)
                {
                    rowStartX = positionInfos[0].IdealX;
                    rowWidth = positionInfos[positionInfos.Count - 1].IdealX - rowStartX;
                }

                float centerOffset = offset.x - (rowWidth / 2) - rowStartX;

                // Create and position the nodes
                for (int i = 0; i < positionInfos.Count; i++)
                {
                    NodePositionInfo info = positionInfos[i];
                    PowerNode node = info.Node;

                    // Create node UI component
                    GameObject nodeObj = Instantiate(m_NodePrefab, container);
                    PowerNodeUI nodeUI = nodeObj.GetComponent<PowerNodeUI>();

                    if (nodeUI != null)
                    {
                        // Apply position with RectTransform
                        float xPos = info.IdealX + centerOffset;
                        float yPos = offset.y + (depth * m_VerticalSpacing);

                        RectTransform rectTransform = nodeObj.GetComponent<RectTransform>();
                        if (rectTransform != null)
                        {
                            rectTransform.anchoredPosition =
                                new Vector2(xPos, -yPos); // Negative Y because UI coordinates

                            // Update bounds tracking
                            minX = Mathf.Min(minX, xPos);
                            maxX = Mathf.Max(maxX, xPos);
                            minY = Mathf.Min(minY, -yPos);
                            maxY = Mathf.Max(maxY, -yPos);
                            allNodeRects.Add(rectTransform);
                        }

                        // Store position for child nodes to reference
                        nodePositions[node.nodeID] = new Vector2(xPos, yPos);

                        // Initialize node and store reference
                        nodeUI.Initialize(node, m_TreeManager);
                        m_NodeUIComponents[node.nodeID] = nodeUI;
                    }
                }
            }

            // Recenter all nodes within the container
            if (allNodeRects.Count > 0)
            {
                // Calculate center of all nodes
                float centerX = (minX + maxX) / 2;
                float centerY = (minY + maxY) / 2;

                // Get container size
                RectTransform containerRT = container.GetComponent<RectTransform>();
                if (containerRT != null)
                {
                    // Calculate offset to center nodes in container
                    Vector2 containerCenter = Vector2.zero; // Local space center
                    Vector2 offsetToCenter = containerCenter - new Vector2(centerX, centerY);

                    // Apply offset to all nodes
                    foreach (RectTransform nodeRT in allNodeRects)
                    {
                        nodeRT.anchoredPosition += offsetToCenter;

                        // Update the stored position in nodePositions
                        PowerNodeUI nodeUI = nodeRT.GetComponent<PowerNodeUI>();
                        if (nodeUI != null && nodeUI.Node != null)
                        {
                            nodePositions[nodeUI.Node.nodeID] = new Vector2(
                                nodePositions[nodeUI.Node.nodeID].x + offsetToCenter.x,
                                nodePositions[nodeUI.Node.nodeID].y + offsetToCenter.y
                            );
                        }
                    }
                }
            }
        }

        // Helper class for node positioning
        private class NodePositionInfo
        {
            public PowerNode Node;
            public float IdealX;
        }

        private void CreateNodeConnections(PowerBranch branch, Transform branchContainer)
        {
            foreach (var nodeEntry in m_NodeUIComponents)
            {
                PowerNode node = nodeEntry.Value.Node;
                PowerNodeUI sourceNodeUI = nodeEntry.Value;

                // Skip nodes that aren't in the current branch
                if (node == null || node.branch != branch || node.unlocksNodes == null || m_ConnectionPrefab == null)
                    continue;

                foreach (var childNode in node.unlocksNodes)
                {
                    if (childNode == null || !m_NodeUIComponents.TryGetValue(childNode.nodeID, out PowerNodeUI childUI))
                        continue;

                    // Create UI line connection in the specific branch container
                    GameObject connectionObj = Instantiate(m_ConnectionPrefab, branchContainer);
                    connectionObj.transform.SetAsFirstSibling();
                    UILineRenderer lineRenderer = connectionObj.GetComponent<UILineRenderer>();

                    if (lineRenderer != null)
                    {
                        // Set connection's rect transform to match the canvas
                        RectTransform connectionRT = connectionObj.GetComponent<RectTransform>();
                        connectionRT.anchorMin = Vector2.zero;
                        connectionRT.anchorMax = Vector2.one;
                        connectionRT.offsetMin = Vector2.zero;
                        connectionRT.offsetMax = Vector2.zero;

                        // Get UI positions in screen space
                        // Get the container's RectTransform
                        RectTransform containerRT = branchContainer.GetComponent<RectTransform>();
                        Rect containerRect = containerRT.rect;

                        // Get source and target positions in local container space
                        Vector2 sourceLocalPos;
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            containerRT,
                            RectTransformUtility.WorldToScreenPoint(null, sourceNodeUI.transform.position),
                            null,
                            out sourceLocalPos);

                        Vector2 targetLocalPos;
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            containerRT,
                            RectTransformUtility.WorldToScreenPoint(null, childUI.transform.position),
                            null, 
                            out targetLocalPos);

                        // Normalize to 0-1 range relative to container
                        sourceLocalPos.x = (sourceLocalPos.x - containerRect.xMin) / containerRect.width;
                        sourceLocalPos.y = (sourceLocalPos.y - containerRect.yMin) / containerRect.height;
                        targetLocalPos.x = (targetLocalPos.x - containerRect.xMin) / containerRect.width;
                        targetLocalPos.y = (targetLocalPos.y - containerRect.yMin) / containerRect.height;

                        // Set points directly in local coordinates
                        Vector2[] points = new Vector2[2] { sourceLocalPos, targetLocalPos };

                        // Configure line settings
                        lineRenderer.Points = points;
                        lineRenderer.LineThickness = m_LineThickness;
                        lineRenderer.LineCaps = m_LineCaps;
                        lineRenderer.color = m_LineColor;
                        lineRenderer.RelativeSize = true;

                        if (m_ConnectionMaterial != null)
                        {
                            lineRenderer.material = m_ConnectionMaterial;
                        }
                    }
                }
            }
        }

        public Rect GetScreenCoordinates(RectTransform uiElement)
        {
            var worldCorners = new Vector3[4];
            uiElement.GetWorldCorners(worldCorners);
            var result = new Rect(
                worldCorners[0].x,
                worldCorners[0].y,
                worldCorners[2].x - worldCorners[0].x,
                worldCorners[2].y - worldCorners[0].y);
            
            result .position += result .size * uiElement.pivot;
            
            return result;
        }

        public void RefreshAllNodes()
        {
            foreach (var nodeUI in m_NodeUIComponents.Values)
            {
                nodeUI.Refresh();
            }
        }

        public void OnNodeClicked(string nodeID)
        {
            if (m_TreeManager == null) return;
            
            if (m_TreeManager.IsNodeUnlocked(nodeID))
            {
                if (m_TreeManager.CanUpgradeNode(nodeID))
                {
                    m_TreeManager.UpgradeNode(nodeID);
                    RefreshAllNodes();
                }
            }
            else if (m_TreeManager.CanUnlockNode(nodeID))
            {
                m_TreeManager.UnlockNode(nodeID);
                RefreshAllNodes();
            }
        }
    }
}