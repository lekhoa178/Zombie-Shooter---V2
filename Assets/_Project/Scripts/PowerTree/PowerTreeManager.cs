using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PowerTree
{
    /// <summary>
    /// Manages the power tree progression system
    /// </summary>
    public class PowerTreeManager : MonoBehaviour
    {
        [SerializeField] private PowerNodeDatabase nodeDatabase;
        [SerializeField] private PlayerStats playerStats;

        private PowerTreeData playerTreeData;
        private Dictionary<string, PowerNode> nodeCache = new Dictionary<string, PowerNode>();

        private void Awake()
        {
            // Initialize cache
            if (nodeDatabase != null)
            {
                foreach (var node in nodeDatabase.AllNodes)
                {
                    nodeCache[node.nodeID] = node;
                }
            }

            // Load player data
            LoadPlayerData();

            // Apply all unlocked effects
            ApplyAllUnlockedEffects();
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

            // Add to unlocked nodes
            playerTreeData.unlockedNodes.Add(new PowerNodeState
            {
                nodeID = nodeID,
                level = 1
            });

            // Apply effect
            node.ApplyEffect(playerStats, 1);

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

            // Update level
            var nodeState = playerTreeData.unlockedNodes.First(n => n.nodeID == nodeID);

            // Remove old effect
            node.RemoveEffect(playerStats);

            // Update level
            nodeState.level++;

            // Apply new effect
            node.ApplyEffect(playerStats, nodeState.level);

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
    }
}