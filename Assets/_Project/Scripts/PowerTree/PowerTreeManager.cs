using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityUtils;

namespace PowerTree
{
    /// <summary>
    /// Manages the power tree progression system
    /// </summary>
    public class PowerTreeManager : PersistentSingleton<PowerTreeManager>
    {
        [SerializeField] private PowerNodeDatabase nodeDatabase;
        [SerializeField] private PlayerStats playerStats;

        private PowerTreeData playerTreeData;
        private Dictionary<string, PowerNode> nodeCache = new Dictionary<string, PowerNode>();
        
        public UnityEvent OnAetherShardChanged = new UnityEvent(); 

        protected override void Awake()
        {
            base.Awake();
            
            // Initialize cache
            if (nodeDatabase != null)
            {
                foreach (var node in nodeDatabase.AllNodes)
                {
                    nodeCache[node.nodeID] = node;
                }
            }
            
            if (playerStats == null)
            {
                playerStats = FindAnyObjectByType<PlayerStats>();
                if (playerStats == null)
                {
                    Debug.LogError("PlayerStats not found in the scene.");
                }
            }

            // Load player data
            LoadPlayerData();

            // Apply all unlocked effects
            ApplyAllUnlockedEffects();
        }

        private void OnDestroy()
        {
            foreach (var node in nodeDatabase.AllNodes)
            {
                node.ResetState();
            }
        }

        private void ApplyAllUnlockedEffects()
        {
            foreach (var nodeState in playerTreeData.unlockedNodes)
            {
                if (nodeCache.TryGetValue(nodeState.nodeID, out PowerNode node))
                {
                    node.ApplyEffect(playerStats, nodeState.level);
                }
            }
        }

        #region Node Management
        
        public int GetNodeCost(string nodeID, int level = default)
        {
            if (!nodeCache.TryGetValue(nodeID, out PowerNode node))
                return 0;

            if (level == default)
            {
                if (IsNodeUnlocked(nodeID))
                    level = GetNodeLevel(nodeID);
                else
                {
                    level = 0;
                }
            }

            return node.GetLevelCost(level);
        }
        
        public string GetNodeName(string nodeID)
        {
            if (!nodeCache.TryGetValue(nodeID, out PowerNode node))
                return string.Empty;

            return node.displayName;
        }

        public bool CanUnlockNode(string nodeID)
        {
            if (!nodeCache.TryGetValue(nodeID, out PowerNode node))
                return false;

            // Check if already unlocked
            if (IsNodeUnlocked(nodeID))
                return false;

            // Check prerequisites
            foreach (var requiredNode in node.requiredNodes)
            {
                if (!IsNodeUnlocked(requiredNode.nodeID))
                    return false;
            }

            // Check if enough shards
            return playerTreeData.aetherShards >= node.GetLevelCost(0);
        }

        public bool CanUpgradeNode(string nodeID)
        {
            if (!IsNodeUnlocked(nodeID))
                return false;

            int currentLevel = GetNodeLevel(nodeID);
            if (currentLevel >= 3) // Max level is 3
                return false;

            if (!nodeCache.TryGetValue(nodeID, out PowerNode node))
                return false;

            return playerTreeData.aetherShards >= node.GetLevelCost(currentLevel);
        }

        public bool IsNodeUnlocked(string nodeID)
        {
            return playerTreeData.unlockedNodes.Any(n => n.nodeID == nodeID);
        }

        public int GetNodeLevel(string nodeID)
        {
            var nodeState = playerTreeData.unlockedNodes.FirstOrDefault(n => n.nodeID == nodeID);
            return nodeState != null ? nodeState.level : 0;
        }

        public bool UnlockNode(string nodeID)
        {
            if (!CanUnlockNode(nodeID))
                return false;

            var node = nodeCache[nodeID];
            int cost = node.GetLevelCost(0);

            // Consume shards
            playerTreeData.aetherShards -= cost;
            OnAetherShardChanged.Invoke();

            // Add to unlocked nodes
            playerTreeData.unlockedNodes.Add(new PowerNodeState
            {
                nodeID = nodeID,
                level = 1
            });

            // Apply effect
            node.ApplyEffect(playerStats, 1);
            node.onNodeUnlockedOrUpgraded?.Invoke(1);

            // Save data
            SavePlayerData();

            return true;
        }

        public bool UpgradeNode(string nodeID)
        {
            if (!CanUpgradeNode(nodeID))
                return false;

            int currentLevel = GetNodeLevel(nodeID);
            var node = nodeCache[nodeID];
            int cost = node.GetLevelCost(currentLevel);

            // Consume shards
            playerTreeData.aetherShards -= cost;
            OnAetherShardChanged.Invoke();

            // Update level
            var nodeState = playerTreeData.unlockedNodes.First(n => n.nodeID == nodeID);

            // Remove old effect
            node.RemoveEffect(playerStats);

            // Update level
            nodeState.level++;

            // Apply new effect
            node.ApplyEffect(playerStats, nodeState.level);
            node.onNodeUnlockedOrUpgraded?.Invoke(nodeState.level);

            // Save data
            SavePlayerData();

            return true;
        }

        public List<string> GetUnlockableNodeIDs()
        {
            return nodeCache.Keys
                .Where(nodeID => CanUnlockNode(nodeID))
                .ToList();
        }

        public List<string> GetUpgradeableNodeIDs()
        {
            return playerTreeData.unlockedNodes
                .Where(node => CanUpgradeNode(node.nodeID))
                .Select(node => node.nodeID)
                .ToList();
        }

        #endregion

        #region Aether Shards

        public void AddAetherShards(int amount)
        {
            playerTreeData.aetherShards += amount;
            OnAetherShardChanged.Invoke();
            SavePlayerData();
        }
        
        public void RemoveAetherShards(int amount)
        {
            playerTreeData.aetherShards -= amount;
            if (playerTreeData.aetherShards < 0)
                playerTreeData.aetherShards = 0;
            OnAetherShardChanged.Invoke();
            SavePlayerData();
        }

        public int GetAetherShards()
        {
            return playerTreeData.aetherShards;
        }

        #endregion

        #region Save/Load

        private void SavePlayerData()
        {
            string json = JsonUtility.ToJson(playerTreeData);
            PlayerPrefs.SetString("PowerTreeData", json);
            PlayerPrefs.Save();
        }

        private void LoadPlayerData()
        {
            if (PlayerPrefs.HasKey("PowerTreeData"))
            {
                string json = PlayerPrefs.GetString("PowerTreeData");
                playerTreeData = JsonUtility.FromJson<PowerTreeData>(json);
            }
            else
            {
                // Initialize new data
                playerTreeData = new PowerTreeData
                {
                    aetherShards = 0,
                    unlockedNodes = new List<PowerNodeState>()
                };
            }
        }

        #endregion

        #region Cheat Methods
        /// <summary>
        /// Force unlocks a node without cost or prerequisite checks
        /// </summary>
        public bool ForceUnlockNode(string nodeID)
        {
            if (IsNodeUnlocked(nodeID))
                return false;

            if (!nodeCache.TryGetValue(nodeID, out PowerNode node))
                return false;

            // Add to unlocked nodes
            playerTreeData.unlockedNodes.Add(new PowerNodeState
            {
                nodeID = nodeID,
                level = 1
            });

            // Apply effect
            node.ApplyEffect(playerStats, 1);
            node.onNodeUnlockedOrUpgraded?.Invoke(1);

            // Save data
            SavePlayerData();

            return true;
        }

        /// <summary>
        /// Force upgrades a node without cost checks
        /// </summary>
        public bool ForceUpgradeNode(string nodeID)
        {
            if (!IsNodeUnlocked(nodeID))
                return false;

            int currentLevel = GetNodeLevel(nodeID);
            if (currentLevel >= 3) // Max level is 3
                return false;

            if (!nodeCache.TryGetValue(nodeID, out PowerNode node))
                return false;

            // Update level
            var nodeState = playerTreeData.unlockedNodes.First(n => n.nodeID == nodeID);

            // Remove old effect
            node.RemoveEffect(playerStats);

            // Update level
            nodeState.level++;

            // Apply new effect
            node.ApplyEffect(playerStats, nodeState.level);
            node.onNodeUnlockedOrUpgraded?.Invoke(nodeState.level);

            // Save data
            SavePlayerData();

            return true;
        }

        /// <summary>
        /// Force downgrades a node or removes it if at level 1
        /// </summary>
        public bool ForceDowngradeNode(string nodeID)
        {
            if (!IsNodeUnlocked(nodeID))
                return false;

            if (!nodeCache.TryGetValue(nodeID, out PowerNode node))
                return false;

            var nodeState = playerTreeData.unlockedNodes.First(n => n.nodeID == nodeID);
            
            // Remove current effect
            node.RemoveEffect(playerStats);

            if (nodeState.level > 1)
            {
                // Downgrade level
                nodeState.level--;
                
                // Apply new effect at lower level
                node.ApplyEffect(playerStats, nodeState.level);
            }
            else
            {
                // Remove node completely
                playerTreeData.unlockedNodes.Remove(nodeState);
            }

            // Save data
            SavePlayerData();

            return true;
        }
        #endregion
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
}