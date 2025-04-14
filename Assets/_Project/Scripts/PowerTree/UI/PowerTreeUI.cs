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

        private Dictionary<string, PowerNodeUI> m_NodeUIComponents = new Dictionary<string, PowerNodeUI>();
        private Dictionary<PowerBranch, Dictionary<int, List<PowerNode>>> m_NodesByDepth = new Dictionary<PowerBranch, Dictionary<int, List<PowerNode>>>();

        private Camera m_MainCamera;

        private void Awake()
        {
            m_MainCamera = Camera.main;
        }

        private void Start()
        {
            BuildTreeUI();
        }

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
                
                // Layout this branch's nodes
                Vector2 branchOffset = new Vector2(m_InitialOffset.x, m_InitialOffset.y + branchYOffset);
                LayoutBranchNodes(branch, m_BranchContainers[i], branchOffset);
                
                // Calculate next branch Y position based on the height of this branch
                int maxDepth = m_NodesByDepth[branch].Keys.Max();
                branchYOffset += (maxDepth + 1) * m_VerticalSpacing + m_BranchVerticalOffset;
            }

            // Create connections between nodes
            CreateNodeConnections();

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
            
            // For each depth level in the branch
            foreach (int depth in branchNodes.Keys.OrderBy(d => d))
            {
                List<PowerNode> depthNodes = branchNodes[depth];
                
                // Calculate total width of this row
                float totalWidth = depthNodes.Count * m_HorizontalSpacing;
                float startX = offset.x - (totalWidth / 2) + (m_HorizontalSpacing / 2);
                
                // Position each node in this depth level
                for (int i = 0; i < depthNodes.Count; i++)
                {
                    PowerNode node = depthNodes[i];
                    
                    // Create node UI component
                    GameObject nodeObj = Instantiate(m_NodePrefab, container);
                    PowerNodeUI nodeUI = nodeObj.GetComponent<PowerNodeUI>();
                    
                    if (nodeUI != null)
                    {
                        // Position the node
                        float xPos = startX + (i * m_HorizontalSpacing);
                        float yPos = offset.y + (depth * m_VerticalSpacing);
                        
                        // Apply position with RectTransform
                        RectTransform rectTransform = nodeObj.GetComponent<RectTransform>();
                        if (rectTransform != null)
                        {
                            rectTransform.anchoredPosition = new Vector2(xPos, -yPos); // Negative Y because UI coordinates
                        }
                        
                        // Initialize node and store reference
                        nodeUI.Initialize(node, m_TreeManager);
                        m_NodeUIComponents[node.nodeID] = nodeUI;
                    }
                }
            }
        }

        private void CreateNodeConnections()
        {
            RectTransform canvasRect = m_RootCanvas.GetComponent<RectTransform>();
            
            foreach (var nodeEntry in m_NodeUIComponents)
            {
                PowerNode node = nodeEntry.Value.Node;
                PowerNodeUI sourceNodeUI = nodeEntry.Value;
                
                if (node == null || node.unlocksNodes == null || m_ConnectionPrefab == null) 
                    continue;
                
                foreach (var childNode in node.unlocksNodes)
                {
                    if (childNode == null || !m_NodeUIComponents.TryGetValue(childNode.nodeID, out PowerNodeUI childUI))
                        continue;
                    
                    // Create UI line connection
                    GameObject connectionObj = Instantiate(m_ConnectionPrefab, sourceNodeUI.transform);
                    UILineRenderer lineRenderer = connectionObj.GetComponent<UILineRenderer>();
                    
                    if (lineRenderer != null)
                    {
                        // Configure UILineRenderer
                        Vector2[] points = new Vector2[2];
                        
                        // Get positions in world space
                        Vector3 sourcePos = sourceNodeUI.ConnectionPoint.position;
                        Vector3 targetPos = childUI.ConnectionPointTop.position;
                        
                        // Convert to canvas space
                        RectTransform rt = connectionObj.GetComponent<RectTransform>();
                        
                        // Make sure the connection has proper anchoring
                        rt.anchorMin = Vector2.zero;
                        rt.anchorMax = Vector2.one;
                        rt.offsetMin = Vector2.zero;
                        rt.offsetMax = Vector2.zero;
                        
                        // Convert world positions to normalized canvas space (0-1 range)
                        Vector2 screenSourcePos = RectTransformUtility.WorldToScreenPoint(Camera.main, sourcePos);
                        Vector2 screenTargetPos = RectTransformUtility.WorldToScreenPoint(Camera.main, targetPos);

                        Vector2 canvasSourcePos = Vector2.zero;
                        Vector2 canvasTargetPos = Vector2.zero;
                        
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            rt, screenSourcePos, m_MainCamera, out canvasSourcePos);
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            rt, screenTargetPos, m_MainCamera, out canvasTargetPos);
                        
                        // Normalize to 0-1 range
                        Rect rtRect = rt.rect;
                        canvasSourcePos = new Vector2(
                            (canvasSourcePos.x - rtRect.xMin) / rtRect.width,
                            (canvasSourcePos.y - rtRect.yMin) / rtRect.height
                        );
                        
                        canvasTargetPos = new Vector2(
                            (canvasTargetPos.x - rtRect.xMin) / rtRect.width,
                            (canvasTargetPos.y - rtRect.yMin) / rtRect.height
                        );
                        
                        // Set points in 0-1 range
                        points[0] = canvasSourcePos;
                        points[1] = canvasTargetPos;
                        
                        // Configure line settings
                        lineRenderer.Points = points;
                        lineRenderer.LineThickness = m_LineThickness;
                        lineRenderer.LineCaps = m_LineCaps;
                        lineRenderer.color = m_LineColor;
                        lineRenderer.RelativeSize = true; // Must be true for 0-1 coordinates
                        
                        // Apply material if set
                        if (m_ConnectionMaterial != null)
                        {
                            lineRenderer.material = m_ConnectionMaterial;
                        }
                    }
                }
            }
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
            
            // Handle node click (unlock/upgrade)
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