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
        [SerializeField] private PlayerStats m_PlayerStats;
        [SerializeField] private PowerNodeFactory m_NodeFactory;
        
        [Tooltip("The PowerNodeDatabase asset to use. If not set, one will be created at runtime.")]
        [SerializeField] private PowerNodeDatabase m_NodeDatabase;
        
        [Tooltip("Whether to create a new database at runtime. Set to false to use the assigned database.")]
        [SerializeField] private bool m_CreateDatabaseAtRuntime = true;
        
        private PowerTreeManager m_TreeManager;
        
        private void Awake()
        {
            InitializePowerTree();
        }
        
        /// <summary>
        /// Creates the power tree structure and initializes the system
        /// </summary>
        private void InitializePowerTree()
        {
            // Check if we need to create a database at runtime or use the existing one
            if (m_CreateDatabaseAtRuntime)
            {
                if (m_NodeFactory == null)
                {
                    Debug.LogError("PowerTreeSetup: NodeFactory is required when creating database at runtime.");
                    return;
                }
                
                // Create node database at runtime
                m_NodeDatabase = ScriptableObject.CreateInstance<PowerNodeDatabase>();
                
                // Create all nodes using the factory
                m_NodeDatabase.AllNodes = m_NodeFactory.CreatePowerTree().ToArray();
            }
            else
            {
                // Use the assigned database
                if (m_NodeDatabase == null)
                {
                    Debug.LogError("PowerTreeSetup: NodeDatabase is required when not creating at runtime. Set either m_CreateDatabaseAtRuntime=true or assign a database asset.");
                    return;
                }
            }
            
            // Initialize the database
            m_NodeDatabase.Initialize();
            
            // Create and set up the tree manager
            m_TreeManager = gameObject.AddComponent<PowerTreeManager>();
            
            // Set references
            var managerField = m_TreeManager.GetType().GetField("nodeDatabase", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            if (managerField != null)
            {
                managerField.SetValue(m_TreeManager, m_NodeDatabase);
            }
            
            var statsField = m_TreeManager.GetType().GetField("playerStats", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            if (statsField != null)
            {
                statsField.SetValue(m_TreeManager, m_PlayerStats);
            }
        }
        
        /// <summary>
        /// Example of how to obtain and upgrade nodes at runtime
        /// </summary>
        public void ExampleNodeUsage()
        {
            // Add some Aether Shards to the player
            m_TreeManager.AddAetherShards(10);
            
            // Unlock a node
            bool unlocked = m_TreeManager.UnlockNode("gentle_rebirth");
            Debug.Log($"Unlocked Gentle Rebirth: {unlocked}");
            
            // Upgrade a node (if unlocked)
            if (m_TreeManager.IsNodeUnlocked("gentle_rebirth"))
            {
                bool upgraded = m_TreeManager.UpgradeNode("gentle_rebirth");
                Debug.Log($"Upgraded Gentle Rebirth to level {m_TreeManager.GetNodeLevel("gentle_rebirth")}: {upgraded}");
            }
            
            // Check available nodes
            var unlockableNodes = m_TreeManager.GetUnlockableNodeIDs();
            Debug.Log($"Available nodes to unlock: {string.Join(", ", unlockableNodes)}");
            
            // Check upgradeable nodes
            var upgradeableNodes = m_TreeManager.GetUpgradeableNodeIDs();
            Debug.Log($"Available nodes to upgrade: {string.Join(", ", upgradeableNodes)}");
        }
        
        /// <summary>
        /// Example of how to handle Aether Shards
        /// </summary>
        public void ExampleAetherShardManagement()
        {
            // Add Aether Shards (could be from gameplay rewards)
            m_TreeManager.AddAetherShards(5);
            
            // Get current Aether Shards
            int currentShards = m_TreeManager.GetAetherShards();
            Debug.Log($"Player has {currentShards} Aether Shards");
            
            // Check if player can afford to unlock/upgrade
            string nodeID = "wild_strength";
            
            if (m_TreeManager.CanUnlockNode(nodeID))
            {
                Debug.Log($"Player can unlock {nodeID}");
                m_TreeManager.UnlockNode(nodeID);
            }
            else if (m_TreeManager.CanUpgradeNode(nodeID))
            {
                Debug.Log($"Player can upgrade {nodeID}");
                m_TreeManager.UpgradeNode(nodeID);
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
                treeMgrField.SetValue(powerTreeUI, m_TreeManager);
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