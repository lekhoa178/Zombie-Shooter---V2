using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PowerTree
{
    /// <summary>
    /// Example script to initialize and set up the power tree system
    /// </summary>
    public class PowerTreeSetup : MonoBehaviour
    {
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PowerNodeFactory nodeFactory;
        
        private PowerTreeManager treeManager;
        private PowerNodeDatabase nodeDatabase;
        
        private void Awake()
        {
            InitializePowerTree();
        }
        
        /// <summary>
        /// Creates the power tree structure and initializes the system
        /// </summary>
        private void InitializePowerTree()
        {
            // Create node database
            nodeDatabase = ScriptableObject.CreateInstance<PowerNodeDatabase>();
            
            // Create all nodes using the factory
            nodeDatabase.AllNodes = nodeFactory.CreatePowerTree().ToArray();
            
            // Initialize the database
            nodeDatabase.Initialize();
            
            // Create and set up the tree manager
            treeManager = gameObject.AddComponent<PowerTreeManager>();
            
            // Set references
            var managerField = treeManager.GetType().GetField("nodeDatabase", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            if (managerField != null)
            {
                managerField.SetValue(treeManager, nodeDatabase);
            }
            
            var statsField = treeManager.GetType().GetField("playerStats", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            if (statsField != null)
            {
                statsField.SetValue(treeManager, playerStats);
            }
        }
        
        /// <summary>
        /// Example of how to obtain and upgrade nodes at runtime
        /// </summary>
        public void ExampleNodeUsage()
        {
            // Add some Aether Shards to the player
            treeManager.AddAetherShards(10);
            
            // Unlock a node
            bool unlocked = treeManager.UnlockNode("gentle_rebirth");
            Debug.Log($"Unlocked Gentle Rebirth: {unlocked}");
            
            // Upgrade a node (if unlocked)
            if (treeManager.IsNodeUnlocked("gentle_rebirth"))
            {
                bool upgraded = treeManager.UpgradeNode("gentle_rebirth");
                Debug.Log($"Upgraded Gentle Rebirth to level {treeManager.GetNodeLevel("gentle_rebirth")}: {upgraded}");
            }
            
            // Check available nodes
            var unlockableNodes = treeManager.GetUnlockableNodeIDs();
            Debug.Log($"Available nodes to unlock: {string.Join(", ", unlockableNodes)}");
            
            // Check upgradeable nodes
            var upgradeableNodes = treeManager.GetUpgradeableNodeIDs();
            Debug.Log($"Available nodes to upgrade: {string.Join(", ", upgradeableNodes)}");
        }
        
        /// <summary>
        /// Example of how to handle Aether Shards
        /// </summary>
        public void ExampleAetherShardManagement()
        {
            // Add Aether Shards (could be from gameplay rewards)
            treeManager.AddAetherShards(5);
            
            // Get current Aether Shards
            int currentShards = treeManager.GetAetherShards();
            Debug.Log($"Player has {currentShards} Aether Shards");
            
            // Check if player can afford to unlock/upgrade
            string nodeID = "wild_strength";
            
            if (treeManager.CanUnlockNode(nodeID))
            {
                Debug.Log($"Player can unlock {nodeID}");
                treeManager.UnlockNode(nodeID);
            }
            else if (treeManager.CanUpgradeNode(nodeID))
            {
                Debug.Log($"Player can upgrade {nodeID}");
                treeManager.UpgradeNode(nodeID);
            }
            else
            {
                Debug.Log($"Player cannot unlock or upgrade {nodeID}");
            }
        }
        
        /// <summary>
        /// Example UI methods
        /// </summary>
        public void SetupPowerTreeUI()
        {
            // Create UI manager
            var uiGO = new GameObject("PowerTreeUI");
            var powerTreeUI = uiGO.AddComponent<PowerTreeUI>();
            
            // Setup branch containers
            Transform[] branchContainers = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                var containerGO = new GameObject($"Branch{i}Container");
                containerGO.transform.SetParent(uiGO.transform);
                branchContainers[i] = containerGO.transform;
            }
            
            // Create node prefab (this would be a real prefab in practice)
            var nodePrefab = new GameObject("NodePrefab");
            nodePrefab.AddComponent<PowerNodeUI>();
            
            // Create connection prefab
            var connectionPrefab = new GameObject("ConnectionPrefab");
            connectionPrefab.AddComponent<LineRenderer>();
            
            // Set fields using reflection (for example purposes)
            var treeMgrField = powerTreeUI.GetType().GetField("treeManager", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            if (treeMgrField != null)
            {
                treeMgrField.SetValue(powerTreeUI, treeManager);
            }
            
            var branchField = powerTreeUI.GetType().GetField("branchContainers", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            if (branchField != null)
            {
                branchField.SetValue(powerTreeUI, branchContainers);
            }
            
            var nodePrefabField = powerTreeUI.GetType().GetField("nodePrefab", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            if (nodePrefabField != null)
            {
                nodePrefabField.SetValue(powerTreeUI, nodePrefab);
            }
            
            var connPrefabField = powerTreeUI.GetType().GetField("connectionPrefab", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            if (connPrefabField != null)
            {
                connPrefabField.SetValue(powerTreeUI, connectionPrefab);
            }
            
            // Create a public method to force-refresh the UI
            var buildMethod = powerTreeUI.GetType().GetMethod("BuildTreeUI");
            if (buildMethod != null)
            {
                buildMethod.Invoke(powerTreeUI, null);
            }
        }
    }
} 