using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Linq;

namespace PowerTree.Editor
{
    public class PowerNodeAssetGenerator : EditorWindow
    {
        private string m_OutputFolder = "Assets/_Project/ScriptableObjects/PowerNodes";
        private PowerNodeFactory m_NodeFactory;
        private Vector2 m_ScrollPosition;
        private bool m_OverwriteExisting = false;
        private Dictionary<string, bool> m_NodesToGenerate = new Dictionary<string, bool>();
        private bool m_SelectAll = true;
        private bool m_OrganizeByBranch = true;
        private bool m_ApplyDesignData = true;

        // Design document data
        private Dictionary<string, NodeDesignData> m_DesignData = new Dictionary<string, NodeDesignData>();

        [MenuItem("Tools/PowerTree/Generate Power Node Assets")]
        public static void ShowWindow()
        {
            GetWindow<PowerNodeAssetGenerator>("Power Node Asset Generator");
        }

        private void OnEnable()
        {
            // Find existing factory
            string[] guids = AssetDatabase.FindAssets("t:PowerNodeFactory");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                m_NodeFactory = AssetDatabase.LoadAssetAtPath<PowerNodeFactory>(path);
            }

            // Get node implementation types via reflection
            PopulateNodeTypes();
            
            // Load design document data
            LoadDesignDocumentData();
        }

        private void PopulateNodeTypes()
        {
            m_NodesToGenerate.Clear();
            
            // Get the nodeImplementationTypes dictionary through reflection
            FieldInfo field = typeof(PowerNodeFactory).GetField("nodeImplementationTypes", 
                BindingFlags.Static | BindingFlags.NonPublic);
                
            if (field != null)
            {
                var nodeTypes = field.GetValue(null) as Dictionary<string, System.Type>;
                if (nodeTypes != null)
                {
                    foreach (var pair in nodeTypes)
                    {
                        m_NodesToGenerate[pair.Key] = true;
                    }
                }
            }
        }

        private void LoadDesignDocumentData()
        {
            m_DesignData.Clear();
            
            // Populate design data from the design document
            
            // Branch 1: Survival
            
            // Gentle Rebirth
            m_DesignData["gentle_rebirth"] = new NodeDesignData(
                "Gentle Rebirth",
                PowerBranch.Survival,
                new string[] {
                    "10% chance to not lose a red heart when it would be lost",
                    "Increases to 20%",
                    "Increases to 30%"
                }
            );
            
            // Strong Life Drop
            m_DesignData["strong_life_drop"] = new NodeDesignData(
                "Strong Life Drop",
                PowerBranch.Survival,
                new string[] {
                    "Heal 1 red heart after every 2 levels",
                    "Reduces to 1 level",
                    "Heal 1 red heart per level and 1 blue heart after 3 levels"
                }
            );
            
            // Temporary Shield
            m_DesignData["temporary_shield"] = new NodeDesignData(
                "Temporary Shield",
                PowerBranch.Survival,
                new string[] {
                    "Receive 2 blue hearts at the start of each level",
                    "Receive 3 blue hearts",
                    "Receive 3 blue hearts and 10% chance to not lose anything when losing a blue heart"
                }
            );
            
            // Blood Transforms into Armor
            m_DesignData["blood_transforms_armor"] = new NodeDesignData(
                "Blood Transforms into Armor",
                PowerBranch.Survival,
                new string[] {
                    "20% chance to recover 0.5 blue heart when losing a blue heart. When losing a black heart, increase damage by 10% for 5 seconds",
                    "Increases chance to 35%, black heart damage +15%",
                    "Increases chance to 50%, black heart damage +20%, stronger knockback and small area damage"
                }
            );
            
            // Dark Power
            m_DesignData["dark_power"] = new NodeDesignData(
                "Dark Power",
                PowerBranch.Survival,
                new string[] {
                    "When losing a black heart, negate more than 2 bullets instead of just 1",
                    "When losing a black heart, create a shockwave that pushes nearby enemies",
                    "When losing a black heart, deal 50% damage to all enemies on screen (doesn't affect bosses)"
                }
            );
            
            // Near Death Revival
            m_DesignData["near_death_revival"] = new NodeDesignData(
                "Near Death Revival",
                PowerBranch.Survival,
                new string[] {
                    "5% chance to revive with 1 blue heart when dying",
                    "Increases chance to 10%, revive with 1 red heart",
                    "Increases chance to 15%, revive with 1 red heart and 1 blue heart"
                }
            );
            
            // Last Shield
            m_DesignData["last_shield"] = new NodeDesignData(
                "Last Shield",
                PowerBranch.Survival,
                new string[] {
                    "If only 0.5 red heart remains, 30% chance to completely dodge the next attack",
                    "Increases dodge chance to 50%",
                    "Successful dodge creates a wave that pushes back surrounding enemies"
                }
            );
            
            // Life Force Transformation
            m_DesignData["life_force_transformation"] = new NodeDesignData(
                "Life Force Transformation",
                PowerBranch.Survival,
                new string[] {
                    "30% chance to create 0.5 blue heart when receiving a red heart",
                    "Increases chance to 50%",
                    "Increases chance to 70%, created blue heart can be a black heart"
                }
            );
            
            // Branch 2: Attack
            
            // Wild Strength
            m_DesignData["wild_strength"] = new NodeDesignData(
                "Wild Strength",
                PowerBranch.Attack,
                new string[] {
                    "Increase damage by 10% when above 50% health",
                    "Increases to 20%",
                    "Increases to 30%"
                }
            );
            
            // Rage Destruction
            m_DesignData["rage_destruction"] = new NodeDesignData(
                "Rage Destruction",
                PowerBranch.Attack,
                new string[] {
                    "When losing a red heart, increase damage by 15% for 5 seconds",
                    "Increases to 25%",
                    "Increases to 35% and lasts for 7 seconds"
                }
            );
            
            // Lightning Strike
            m_DesignData["lightning_strike"] = new NodeDesignData(
                "Lightning Strike",
                PowerBranch.Attack,
                new string[] {
                    "Attack speed increases by 10%",
                    "Increases to 20%",
                    "Increases to 30% and 5% chance for attacks to deal double damage"
                }
            );
            
            // Berserk
            m_DesignData["berserk"] = new NodeDesignData(
                "Berserk",
                PowerBranch.Attack,
                new string[] {
                    "5% chance to increase attack speed by 20% for 3 seconds when hitting",
                    "Increases to 10% chance, 30% attack speed",
                    "Increases to 15% chance, 40% attack speed, can stack twice"
                }
            );
            
            // Bloodthirsty Combat
            m_DesignData["bloodthirsty_combat"] = new NodeDesignData(
                "Bloodthirsty Combat",
                PowerBranch.Attack,
                new string[] {
                    "When defeating 3 enemies within 5 seconds, gain 20% attack speed for 5 seconds",
                    "Buff increases to 25% attack speed and 5% damage",
                    "Buff can stack twice, up to 30% attack speed and 10% damage"
                }
            );
            
            // Piercing Damage
            m_DesignData["piercing_damage"] = new NodeDesignData(
                "Piercing Damage",
                PowerBranch.Attack,
                new string[] {
                    "Damage increases by 25% if enemy has above 75% health",
                    "Increases to 35%",
                    "Increases to 50%"
                }
            );
            
            // Branch 3: Evasion
            
            // Ghost Steps
            m_DesignData["ghost_steps"] = new NodeDesignData(
                "Ghost Steps",
                PowerBranch.Evasion,
                new string[] {
                    "Increase movement speed by 10%",
                    "Increases to 20%",
                    "When successfully dodging, gain 50% movement speed for 2 seconds"
                }
            );
            
            // Wind Evasion
            m_DesignData["wind_evasion"] = new NodeDesignData(
                "Wind Evasion",
                PowerBranch.Evasion,
                new string[] {
                    "10% chance to completely dodge when attacked",
                    "Increases to 15%",
                    "Increases to 20%, if dodge is successful"
                }
            );
            
            // Agility
            m_DesignData["agility"] = new NodeDesignData(
                "Agility",
                PowerBranch.Evasion,
                new string[] {
                    "When HP below 1 red heart, movement speed +10%",
                    "Increases to +20%",
                    "When HP below 1 red heart, dodge +10%"
                }
            );
            
            // Branch 4: Finance
            
            // Luck
            m_DesignData["luck"] = new NodeDesignData(
                "Luck",
                PowerBranch.Finance,
                new string[] {
                    "10% chance for enemies to drop gold",
                    "Increases to 20%",
                    "Increases to 30%, can drop x2 gold"
                }
            );
            
            // Hell Merchant
            m_DesignData["hell_merchant"] = new NodeDesignData(
                "Hell Merchant",
                PowerBranch.Finance,
                new string[] {
                    "Reduce card price in shop by 5%",
                    "Reduce by 10%",
                    "Reduce by 15%"
                }
            );
            
            // Infinite Treasure
            m_DesignData["infinite_treasure"] = new NodeDesignData(
                "Infinite Treasure",
                PowerBranch.Finance,
                new string[] {
                    "If completing level without losing health, receive 50% more gold",
                    "Increases to 75%",
                    "Increases to 100%"
                }
            );
            
            // Safe Finance
            m_DesignData["safe_finance"] = new NodeDesignData(
                "Safe Finance",
                PowerBranch.Finance,
                new string[] {
                    "When losing a red heart, receive 10 gold back",
                    "Receive 15 gold back",
                    "Receive 20 gold back and 10% chance to not lose heart"
                }
            );
        }

        private void OnGUI()
        {
            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

            EditorGUILayout.LabelField("Generate Power Node ScriptableObjects", EditorStyles.boldLabel);
            
            // Factory selection
            m_NodeFactory = (PowerNodeFactory)EditorGUILayout.ObjectField("Node Factory", m_NodeFactory, typeof(PowerNodeFactory), false);
            
            if (m_NodeFactory == null)
            {
                EditorGUILayout.HelpBox("Please assign a PowerNodeFactory.", MessageType.Warning);
            }
            
            // Output folder
            EditorGUILayout.BeginHorizontal();
            m_OutputFolder = EditorGUILayout.TextField("Output Folder", m_OutputFolder);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string folder = EditorUtility.OpenFolderPanel("Select Output Folder", Application.dataPath, "");
                if (!string.IsNullOrEmpty(folder))
                {
                    // Convert to project-relative path
                    if (folder.StartsWith(Application.dataPath))
                    {
                        folder = "Assets" + folder.Substring(Application.dataPath.Length);
                    }
                    m_OutputFolder = folder;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // Overwrite option
            m_OverwriteExisting = EditorGUILayout.Toggle("Overwrite Existing Assets", m_OverwriteExisting);
            
            // Organize by branch option
            m_OrganizeByBranch = EditorGUILayout.Toggle("Organize by Branch", m_OrganizeByBranch);
            
            // Apply design data option
            m_ApplyDesignData = EditorGUILayout.Toggle("Apply Design Document Data", m_ApplyDesignData);
            
            EditorGUILayout.Space();
            
            // Node selection
            EditorGUILayout.LabelField("Power Nodes to Generate:", EditorStyles.boldLabel);
            
            // Select All toggle
            bool newSelectAll = EditorGUILayout.Toggle("Select All", m_SelectAll);
            if (newSelectAll != m_SelectAll)
            {
                m_SelectAll = newSelectAll;
                foreach (var key in m_NodesToGenerate.Keys.ToList())
                {
                    m_NodesToGenerate[key] = m_SelectAll;
                }
            }
            
            EditorGUILayout.Space();
            
            // List all nodes with checkboxes
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            foreach (var pair in m_NodesToGenerate.ToList())
            {
                // Get nice formatted display from design data if available
                string displayName = pair.Key;
                if (m_DesignData.TryGetValue(pair.Key, out NodeDesignData designData))
                {
                    displayName = $"{designData.DisplayName} ({pair.Key})";
                }
                
                m_NodesToGenerate[pair.Key] = EditorGUILayout.Toggle(displayName, pair.Value);
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            GUI.enabled = m_NodeFactory != null;
            if (GUILayout.Button("Generate Node Assets", GUILayout.Height(30)))
            {
                GenerateNodeAssets();
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndScrollView();
        }

        private void GenerateNodeAssets()
        {
            if (m_NodeFactory == null)
            {
                EditorUtility.DisplayDialog("Error", "PowerNodeFactory is not assigned.", "OK");
                return;
            }
            
            // Create output directory if it doesn't exist
            if (!AssetDatabase.IsValidFolder(m_OutputFolder))
            {
                string parentFolder = Path.GetDirectoryName(m_OutputFolder);
                string folderName = Path.GetFileName(m_OutputFolder);
                
                if (!AssetDatabase.IsValidFolder(parentFolder))
                {
                    EditorUtility.DisplayDialog("Error", $"Parent folder {parentFolder} does not exist.", "OK");
                    return;
                }
                
                AssetDatabase.CreateFolder(parentFolder, folderName);
            }
            
            // Create branch folders if organizing by branch
            Dictionary<PowerBranch, string> branchFolders = new Dictionary<PowerBranch, string>();
            
            if (m_OrganizeByBranch)
            {
                foreach (PowerBranch branch in System.Enum.GetValues(typeof(PowerBranch)))
                {
                    string branchFolderName = branch.ToString();
                    string branchFolderPath = Path.Combine(m_OutputFolder, branchFolderName);
                    
                    if (!AssetDatabase.IsValidFolder(branchFolderPath))
                    {
                        string branchFolderGuid = AssetDatabase.CreateFolder(m_OutputFolder, branchFolderName);
                        branchFolderPath = AssetDatabase.GUIDToAssetPath(branchFolderGuid);
                    }
                    
                    branchFolders[branch] = branchFolderPath;
                }
            }
            
            int created = 0;
            int updated = 0;
            
            // Get the nodeImplementationTypes dictionary through reflection
            FieldInfo field = typeof(PowerNodeFactory).GetField("nodeImplementationTypes", 
                BindingFlags.Static | BindingFlags.NonPublic);
                
            if (field != null)
            {
                var nodeTypes = field.GetValue(null) as Dictionary<string, System.Type>;
                if (nodeTypes != null)
                {
                    // First pass: Create all selected node assets
                    Dictionary<string, PowerNode> createdNodes = new Dictionary<string, PowerNode>();
                    Dictionary<string, string> nodeAssetPaths = new Dictionary<string, string>();
                    
                    foreach (var pair in nodeTypes)
                    {
                        string nodeId = pair.Key;
                        System.Type nodeType = pair.Value;
                        
                        // Skip if not selected
                        if (!m_NodesToGenerate.TryGetValue(nodeId, out bool generate) || !generate)
                            continue;
                        
                        // Get design data if available
                        NodeDesignData designData = null;
                        if (m_ApplyDesignData && m_DesignData.TryGetValue(nodeId, out designData))
                        {
                            // Use design data for branch and display name
                        }
                        else
                        {
                            // If no design data or not applying it, infer from node ID
                            PowerBranch branch = GetBranchFromNodeType(nodeType);
                            string displayName = FormatDisplayName(nodeId);
                            designData = new NodeDesignData(displayName, branch, new string[3]);
                        }
                        
                        // Create the node
                        PowerNode node = m_NodeFactory.CreateNode(nodeId, designData.DisplayName, designData.Branch);
                        
                        if (node == null)
                            continue;
                        
                        // Store for later relationship setup
                        createdNodes[nodeId] = node;
                        
                        // Apply descriptions from design data if available
                        if (m_ApplyDesignData && designData != null && designData.Descriptions.Length > 0)
                        {
                            node.descriptions = designData.Descriptions;
                        }
                        
                        // Determine target folder based on branch
                        string targetFolder = m_OutputFolder;
                        if (m_OrganizeByBranch && branchFolders.TryGetValue(designData.Branch, out string branchFolder))
                        {
                            targetFolder = branchFolder;
                        }
                        
                        string assetPath = Path.Combine(targetFolder, $"{nodeId}.asset");
                        nodeAssetPaths[nodeId] = assetPath;
                    }
                    
                    // Second pass: Get node relationships from factory
                    // Generate the complete power tree to get node relationships
                    List<PowerNode> treeNodes = m_NodeFactory.CreatePowerTree();
                    Dictionary<string, PowerNode> treeNodeMap = new Dictionary<string, PowerNode>();
                    
                    // Build a map of nodes by ID
                    foreach (var node in treeNodes)
                    {
                        treeNodeMap[node.nodeID] = node;
                    }
                    
                    // Third pass: Set up relationships and save assets
                    foreach (var entry in createdNodes)
                    {
                        string nodeId = entry.Key;
                        PowerNode node = entry.Value;
                        string assetPath = nodeAssetPaths[nodeId];
                        
                        // Get the node from the tree to access its relationships
                        if (treeNodeMap.TryGetValue(nodeId, out PowerNode treeNode))
                        {
                            // Copy relationships
                            if (treeNode.requiredNodes != null && treeNode.requiredNodes.Length > 0)
                            {
                                // Convert required nodes to use our created instances
                                List<PowerNode> requiredNodes = new List<PowerNode>();
                                foreach (var required in treeNode.requiredNodes)
                                {
                                    if (createdNodes.TryGetValue(required.nodeID, out PowerNode createdRequired))
                                    {
                                        requiredNodes.Add(createdRequired);
                                    }
                                }
                                node.requiredNodes = requiredNodes.Count > 0 ? requiredNodes.ToArray() : null;
                            }
                            
                            if (treeNode.unlocksNodes != null && treeNode.unlocksNodes.Length > 0)
                            {
                                // Convert unlocks nodes to use our created instances
                                List<PowerNode> unlocksNodes = new List<PowerNode>();
                                foreach (var unlocks in treeNode.unlocksNodes)
                                {
                                    if (createdNodes.TryGetValue(unlocks.nodeID, out PowerNode createdUnlocks))
                                    {
                                        unlocksNodes.Add(createdUnlocks);
                                    }
                                }
                                node.unlocksNodes = unlocksNodes.Count > 0 ? unlocksNodes.ToArray() : null;
                            }
                        }
                        
                        // Check if asset already exists
                        bool exists = AssetDatabase.LoadAssetAtPath<PowerNode>(assetPath) != null;
                        
                        if (!exists || m_OverwriteExisting)
                        {
                            if (exists)
                            {
                                // Update existing asset
                                AssetDatabase.DeleteAsset(assetPath);
                                updated++;
                            }
                            else
                            {
                                created++;
                            }
                            
                            // Save as asset
                            AssetDatabase.CreateAsset(node, assetPath);
                        }
                    }
                }
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            string message = $"Generated {created} new node assets\nUpdated {updated} existing node assets";
            if (m_OrganizeByBranch)
            {
                message += "\nOrganized into branch folders";
            }
            
            EditorUtility.DisplayDialog("Power Node Assets", message, "OK");
        }
        
        private PowerBranch GetBranchFromNodeType(System.Type nodeType)
        {
            // Try to infer the branch from the type name
            string typeName = nodeType.Name.ToLower();
            
            if (typeName.Contains("survival"))
                return PowerBranch.Survival;
            else if (typeName.Contains("attack"))
                return PowerBranch.Attack;
            else if (typeName.Contains("evasion"))
                return PowerBranch.Evasion;
            else if (typeName.Contains("finance"))
                return PowerBranch.Finance;
            
            // Look at what nodes are defined in factory and match by ID to get branch
            MethodInfo createTree = typeof(PowerNodeFactory).GetMethod("CreatePowerTree");
            if (createTree != null)
            {
                List<PowerNode> treeNodes = m_NodeFactory.CreatePowerTree();
                foreach (var node in treeNodes)
                {
                    System.Type nodeImplType = node.GetType();
                    if (nodeImplType == nodeType)
                    {
                        return node.branch;
                    }
                }
            }
            
            // Default to Survival if we can't determine
            return PowerBranch.Survival;
        }
        
        private string FormatDisplayName(string nodeId)
        {
            // Convert from snake_case to Title Case
            string[] words = nodeId.Split('_');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
                }
            }
            
            return string.Join(" ", words);
        }
    }
    
    /// <summary>
    /// Holds design data for a power node
    /// </summary>
    public class NodeDesignData
    {
        public string DisplayName { get; private set; }
        public PowerBranch Branch { get; private set; }
        public string[] Descriptions { get; private set; }
        
        public NodeDesignData(string displayName, PowerBranch branch, string[] descriptions)
        {
            DisplayName = displayName;
            Branch = branch;
            Descriptions = descriptions;
        }
    }
} 