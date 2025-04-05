using System.Collections.Generic;
using UnityEngine;

namespace PowerTree
{
    /// <summary>
    /// Factory class to create and manage power nodes more easily
    /// </summary>
    [CreateAssetMenu(fileName = "PowerNodeFactory", menuName = "PowerTree/PowerNodeFactory")]
    public class PowerNodeFactory : ScriptableObject
    {
        // Dictionary to hold node templates by ID
        private Dictionary<string, PowerNode> nodeTemplates = new Dictionary<string, PowerNode>();
        
        // Dictionary to map node IDs to their implementation types
        private static readonly Dictionary<string, System.Type> nodeImplementationTypes = new Dictionary<string, System.Type>
        {
            // Survival Branch
            { "gentle_rebirth", typeof(GentleRebirthNode) },
            { "strong_life_drop", typeof(StrongLifeDropNode) },
            { "temporary_shield", typeof(TemporaryShieldNode) },
            { "blood_transforms_armor", typeof(BloodTransformsIntoArmorNode) },
            { "dark_power", typeof(DarkPowerNode) },
            { "near_death_revival", typeof(NearDeathRevivalNode) },
            { "last_shield", typeof(LastShieldNode) },
            { "life_force_transformation", typeof(LifeForceTransformationNode) },
            
            // Attack Branch
            { "wild_strength", typeof(WildStrengthNode) },
            { "rage_destruction", typeof(RageDestructionNode) },
            { "lightning_strike", typeof(LightningStrikeNode) },
            { "berserk", typeof(BerserkNode) },
            { "bloodthirsty_combat", typeof(BloodthirstyCombatNode) },
            { "piercing_damage", typeof(PiercingDamageNode) },
            
            // Evasion Branch
            { "ghost_steps", typeof(GhostStepsNode) },
            { "wind_evasion", typeof(WindEvasionNode) },
            { "agility", typeof(AgilityNode) },
            
            // Finance Branch
            { "luck", typeof(LuckNode) },
            { "hell_merchant", typeof(HellMerchantNode) },
            { "infinite_treasure", typeof(InfiniteTreasureNode) },
            { "safe_finance", typeof(SafeFinanceNode) }
        };
        
        /// <summary>
        /// Creates a new PowerNode instance of the specified type
        /// </summary>
        public PowerNode CreateNode(string nodeID, string displayName, PowerBranch branch)
        {
            if (!nodeImplementationTypes.TryGetValue(nodeID, out System.Type nodeType))
            {
                Debug.LogError($"No implementation found for node ID: {nodeID}");
                return null;
            }
            
            PowerNode node = ScriptableObject.CreateInstance(nodeType) as PowerNode;
            if (node != null)
            {
                node.nodeID = nodeID;
                node.displayName = displayName;
                node.branch = branch;
            }
            
            return node;
        }
        
        /// <summary>
        /// Initializes the complete power tree structure with all 21 nodes
        /// </summary>
        public List<PowerNode> CreatePowerTree()
        {
            List<PowerNode> allNodes = new List<PowerNode>();
            Dictionary<string, PowerNode> nodeMap = new Dictionary<string, PowerNode>();
            
            // Create Survival Branch Nodes
            PowerNode gentleRebirth = CreateNode("gentle_rebirth", "Gentle Rebirth", PowerBranch.Survival);
            PowerNode strongLifeDrop = CreateNode("strong_life_drop", "Strong Life Drop", PowerBranch.Survival);
            PowerNode temporaryShield = CreateNode("temporary_shield", "Temporary Shield", PowerBranch.Survival);
            PowerNode lastShield = CreateNode("last_shield", "Last Shield", PowerBranch.Survival);
            PowerNode bloodTransformsArmor = CreateNode("blood_transforms_armor", "Blood Transforms into Armor", PowerBranch.Survival);
            PowerNode darkPower = CreateNode("dark_power", "Dark Power", PowerBranch.Survival);
            PowerNode nearDeathRevival = CreateNode("near_death_revival", "Near Death Revival", PowerBranch.Survival);
            PowerNode lifeForceTransformation = CreateNode("life_force_transformation", "Life Force Transformation", PowerBranch.Survival);
            
            // Create Attack Branch Nodes
            PowerNode wildStrength = CreateNode("wild_strength", "Wild Strength", PowerBranch.Attack);
            PowerNode rageDestruction = CreateNode("rage_destruction", "Rage Destruction", PowerBranch.Attack);
            PowerNode lightningStrike = CreateNode("lightning_strike", "Lightning Strike", PowerBranch.Attack);
            PowerNode berserk = CreateNode("berserk", "Berserk", PowerBranch.Attack);
            PowerNode bloodthirstyCombat = CreateNode("bloodthirsty_combat", "Bloodthirsty Combat", PowerBranch.Attack);
            PowerNode piercingDamage = CreateNode("piercing_damage", "Piercing Damage", PowerBranch.Attack);
            
            // Create Evasion Branch Nodes
            PowerNode ghostSteps = CreateNode("ghost_steps", "Ghost Steps", PowerBranch.Evasion);
            PowerNode windEvasion = CreateNode("wind_evasion", "Wind Evasion", PowerBranch.Evasion);
            PowerNode agility = CreateNode("agility", "Agility", PowerBranch.Evasion);
            
            // Create Finance Branch Nodes
            PowerNode luck = CreateNode("luck", "Luck", PowerBranch.Finance);
            PowerNode hellMerchant = CreateNode("hell_merchant", "Hell Merchant", PowerBranch.Finance);
            PowerNode infiniteTreasure = CreateNode("infinite_treasure", "Infinite Treasure", PowerBranch.Finance);
            PowerNode safeFinance = CreateNode("safe_finance", "Safe Finance", PowerBranch.Finance);
            
            // Add nodes to map for easy lookup
            AddNodeToMap(nodeMap, gentleRebirth, allNodes);
            AddNodeToMap(nodeMap, strongLifeDrop, allNodes);
            AddNodeToMap(nodeMap, temporaryShield, allNodes);
            AddNodeToMap(nodeMap, lastShield, allNodes);
            AddNodeToMap(nodeMap, bloodTransformsArmor, allNodes);
            AddNodeToMap(nodeMap, darkPower, allNodes);
            AddNodeToMap(nodeMap, nearDeathRevival, allNodes);
            AddNodeToMap(nodeMap, lifeForceTransformation, allNodes);
            AddNodeToMap(nodeMap, wildStrength, allNodes);
            AddNodeToMap(nodeMap, rageDestruction, allNodes);
            AddNodeToMap(nodeMap, lightningStrike, allNodes);
            AddNodeToMap(nodeMap, berserk, allNodes);
            AddNodeToMap(nodeMap, bloodthirstyCombat, allNodes);
            AddNodeToMap(nodeMap, piercingDamage, allNodes);
            AddNodeToMap(nodeMap, ghostSteps, allNodes);
            AddNodeToMap(nodeMap, windEvasion, allNodes);
            AddNodeToMap(nodeMap, agility, allNodes);
            AddNodeToMap(nodeMap, luck, allNodes);
            AddNodeToMap(nodeMap, hellMerchant, allNodes);
            AddNodeToMap(nodeMap, infiniteTreasure, allNodes);
            AddNodeToMap(nodeMap, safeFinance, allNodes);
            
            // Set up node relationships according to the tree structure
            
            // Survival Branch connections
            SetupNodeConnections(gentleRebirth, null, new PowerNode[] { temporaryShield, lastShield });
            SetupNodeConnections(strongLifeDrop, null, new PowerNode[] { bloodTransformsArmor, darkPower });
            SetupNodeConnections(temporaryShield, new PowerNode[] { gentleRebirth }, null);
            SetupNodeConnections(lastShield, new PowerNode[] { gentleRebirth }, new PowerNode[] { nearDeathRevival });
            SetupNodeConnections(bloodTransformsArmor, new PowerNode[] { strongLifeDrop }, new PowerNode[] { lifeForceTransformation });
            SetupNodeConnections(darkPower, new PowerNode[] { strongLifeDrop }, null);
            SetupNodeConnections(nearDeathRevival, new PowerNode[] { lastShield }, null);
            SetupNodeConnections(lifeForceTransformation, new PowerNode[] { bloodTransformsArmor }, null);
            
            // Attack Branch connections
            SetupNodeConnections(wildStrength, null, new PowerNode[] { rageDestruction, piercingDamage });
            SetupNodeConnections(lightningStrike, null, new PowerNode[] { berserk, bloodthirstyCombat });
            SetupNodeConnections(rageDestruction, new PowerNode[] { wildStrength }, null);
            SetupNodeConnections(piercingDamage, new PowerNode[] { wildStrength }, null);
            SetupNodeConnections(berserk, new PowerNode[] { lightningStrike }, null);
            SetupNodeConnections(bloodthirstyCombat, new PowerNode[] { lightningStrike }, null);
            
            // Evasion Branch connections
            SetupNodeConnections(ghostSteps, null, new PowerNode[] { agility });
            SetupNodeConnections(windEvasion, null, new PowerNode[] { agility });
            SetupNodeConnections(agility, new PowerNode[] { ghostSteps, windEvasion }, null);
            
            // Finance Branch connections
            SetupNodeConnections(luck, null, new PowerNode[] { infiniteTreasure });
            SetupNodeConnections(hellMerchant, null, new PowerNode[] { safeFinance });
            SetupNodeConnections(infiniteTreasure, new PowerNode[] { luck }, null);
            SetupNodeConnections(safeFinance, new PowerNode[] { hellMerchant }, null);
            
            return allNodes;
        }
        
        /// <summary>
        /// Helper to add a node to the node map and list
        /// </summary>
        private void AddNodeToMap(Dictionary<string, PowerNode> nodeMap, PowerNode node, List<PowerNode> allNodes)
        {
            nodeMap[node.nodeID] = node;
            allNodes.Add(node);
        }
        
        /// <summary>
        /// Helper to set up node relationships
        /// </summary>
        private void SetupNodeConnections(PowerNode node, PowerNode[] requiredNodes, PowerNode[] unlocksNodes)
        {
            if (requiredNodes != null)
            {
                node.requiredNodes = requiredNodes;
            }
            
            if (unlocksNodes != null)
            {
                node.unlocksNodes = unlocksNodes;
            }
        }
        
        /// <summary>
        /// Utility to create description strings for node levels
        /// </summary>
        public void SetupNodeDescriptions(PowerNode node, string[] descriptions)
        {
            if (descriptions.Length != 3)
            {
                Debug.LogError($"Need exactly 3 descriptions for node {node.nodeID}");
                return;
            }
            
            node.descriptions = descriptions;
        }
    }
} 