using System.Collections.Generic;
using UnityEngine;

namespace PowerTree
{
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
}