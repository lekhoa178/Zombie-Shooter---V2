using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PowerTree.Editor
{
    public class PowerNodeDatabaseGenerator : EditorWindow
    {
        private string m_OutputPath = "Assets/_Project/ScriptableObjects/PowerNodeDatabase.asset";
        private string m_NodeAssetsFolder = "Assets/_Project/ScriptableObjects/PowerNodes";
        private Vector2 m_ScrollPosition;
        private List<PowerNode> m_AvailableNodes = new List<PowerNode>();
        private List<bool> m_SelectedNodes = new List<bool>();
        private bool m_SelectAll = true;
        private Dictionary<PowerBranch, bool> m_BranchToggles = new Dictionary<PowerBranch, bool>();
        private bool m_GroupByBranch = true;
        private Dictionary<PowerBranch, List<int>> m_NodeIndicesByBranch = new Dictionary<PowerBranch, List<int>>();

        [MenuItem("Tools/PowerTree/Generate Power Node Database")]
        public static void ShowWindow()
        {
            GetWindow<PowerNodeDatabaseGenerator>("Power Node Database Generator");
        }

        private void OnEnable()
        {
            // Initialize branch toggles
            foreach (PowerBranch branch in System.Enum.GetValues(typeof(PowerBranch)))
            {
                m_BranchToggles[branch] = true;
            }
            
            RefreshAvailableNodes();
        }

        private void RefreshAvailableNodes()
        {
            m_AvailableNodes.Clear();
            m_SelectedNodes.Clear();
            m_NodeIndicesByBranch.Clear();
            
            foreach (PowerBranch branch in System.Enum.GetValues(typeof(PowerBranch)))
            {
                m_NodeIndicesByBranch[branch] = new List<int>();
            }
            
            if (!AssetDatabase.IsValidFolder(m_NodeAssetsFolder))
            {
                Debug.LogWarning($"Node assets folder not found or invalid: {m_NodeAssetsFolder}");
                return;
            }
            
            // Add main folder to search paths
            List<string> searchPaths = new List<string> { m_NodeAssetsFolder };
            
            // Check for and add branch subfolders if they exist
            foreach (PowerBranch branch in System.Enum.GetValues(typeof(PowerBranch)))
            {
                string branchFolder = Path.Combine(m_NodeAssetsFolder, branch.ToString());
                if (AssetDatabase.IsValidFolder(branchFolder))
                {
                    searchPaths.Add(branchFolder);
                    Debug.Log($"Including branch subfolder: {branchFolder}");
                }
            }
            
            // Search each path individually to ensure we get ALL nodes
            Dictionary<string, PowerNode> loadedNodeDict = new Dictionary<string, PowerNode>();
            
            foreach (string searchPath in searchPaths)
            {
                string[] guids = AssetDatabase.FindAssets("t:PowerNode", new[] { searchPath });
                Debug.Log($"Found {guids.Length} PowerNode assets in {searchPath}");
                
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    
                    try 
                    {
                        PowerNode node = AssetDatabase.LoadAssetAtPath<PowerNode>(path);
                        
                        if (node != null)
                        {
                            // Use nodeID as key to avoid duplicates
                            if (!loadedNodeDict.ContainsKey(node.nodeID))
                            {
                                loadedNodeDict.Add(node.nodeID, node);
                                Debug.Log($"Loaded node: {node.displayName} ({node.nodeID}) from {path}");
                            }
                            else
                            {
                                Debug.LogWarning($"Duplicate node found: {node.nodeID} at {path}");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"Failed to load PowerNode at {path}");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"Error loading asset at {path}: {ex.Message}");
                    }
                }
                
                // Fallback if no PowerNodes found
                if (guids.Length == 0)
                {
                    // Try searching for all ScriptableObjects and filter by type
                    string[] soGuids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { searchPath });
                    Debug.Log($"No PowerNodes found, checking {soGuids.Length} ScriptableObjects in {searchPath}");
                    
                    foreach (string guid in soGuids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        Object asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                        
                        if (asset != null)
                        {
                            if (asset is PowerNode node)
                            {
                                if (!loadedNodeDict.ContainsKey(node.nodeID))
                                {
                                    loadedNodeDict.Add(node.nodeID, node);
                                    Debug.Log($"Found PowerNode via SO search: {node.displayName} ({node.nodeID})");
                                }
                            }
                            else
                            {
                                // Check if it's a PowerNode by name
                                string typeName = asset.GetType().Name;
                                if (typeName.EndsWith("Node") && !typeName.Equals("PowerNode")) 
                                {
                                    Debug.Log($"Found potential node by name: {typeName} at {path}");
                                }
                            }
                        }
                    }
                }
            }
            
            // Add all unique nodes to our lists
            foreach (PowerNode node in loadedNodeDict.Values)
            {
                m_AvailableNodes.Add(node);
                m_SelectedNodes.Add(true);
                m_NodeIndicesByBranch[node.branch].Add(m_AvailableNodes.Count - 1);
            }
            
            // Output branch statistics
            Debug.Log($"=== Node Statistics ===");
            foreach (PowerBranch branch in System.Enum.GetValues(typeof(PowerBranch)))
            {
                int count = m_NodeIndicesByBranch[branch].Count;
                Debug.Log($"{branch} branch: {count} nodes");
                
                // If only 1 node found, this might indicate a problem
                if (count == 1)
                {
                    int index = m_NodeIndicesByBranch[branch][0];
                    PowerNode node = m_AvailableNodes[index];
                    Debug.Log($"  Only one node found: {node.displayName} ({node.nodeID})");
                }
                else if (count > 0)
                {
                    // List the nodes found
                    Debug.Log($"  Nodes found:");
                    foreach (int index in m_NodeIndicesByBranch[branch])
                    {
                        PowerNode node = m_AvailableNodes[index];
                        Debug.Log($"  - {node.displayName} ({node.nodeID})");
                    }
                }
            }
            
            // Force a refresh of the asset database
            AssetDatabase.Refresh();
        }

        private void OnGUI()
        {
            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

            EditorGUILayout.LabelField("Generate Power Node Database", EditorStyles.boldLabel);

            // Node assets folder
            EditorGUILayout.BeginHorizontal();
            m_NodeAssetsFolder = EditorGUILayout.TextField("Node Assets Folder", m_NodeAssetsFolder);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string folder = EditorUtility.OpenFolderPanel("Select Node Assets Folder", Application.dataPath, "");
                if (!string.IsNullOrEmpty(folder))
                {
                    // Convert to project-relative path
                    if (folder.StartsWith(Application.dataPath))
                    {
                        folder = "Assets" + folder.Substring(Application.dataPath.Length);
                    }
                    m_NodeAssetsFolder = folder;
                    RefreshAvailableNodes();
                }
            }
            if (GUILayout.Button("Refresh", GUILayout.Width(60)))
            {
                RefreshAvailableNodes();
            }
            EditorGUILayout.EndHorizontal();

            // Output path
            EditorGUILayout.BeginHorizontal();
            m_OutputPath = EditorGUILayout.TextField("Output Path", m_OutputPath);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string path = EditorUtility.SaveFilePanelInProject("Save Database Asset", "PowerNodeDatabase", "asset", "Save Power Node Database");
                if (!string.IsNullOrEmpty(path))
                {
                    m_OutputPath = path;
                }
            }
            EditorGUILayout.EndHorizontal();

            // Group by branch toggle
            m_GroupByBranch = EditorGUILayout.Toggle("Group by Branch", m_GroupByBranch);

            EditorGUILayout.Space();

            // Available nodes
            EditorGUILayout.LabelField($"Available Nodes ({m_AvailableNodes.Count}):", EditorStyles.boldLabel);
            
            // Select All toggle
            bool newSelectAll = EditorGUILayout.Toggle("Select All", m_SelectAll);
            if (newSelectAll != m_SelectAll)
            {
                m_SelectAll = newSelectAll;
                for (int i = 0; i < m_SelectedNodes.Count; i++)
                {
                    m_SelectedNodes[i] = m_SelectAll;
                }
                
                foreach (PowerBranch branch in System.Enum.GetValues(typeof(PowerBranch)))
                {
                    m_BranchToggles[branch] = m_SelectAll;
                }
            }
            
            // No nodes found
            if (m_AvailableNodes.Count == 0)
            {
                EditorGUILayout.HelpBox("No Power Node assets found in the specified folder.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                if (m_GroupByBranch)
                {
                    // Show nodes grouped by branch
                    foreach (PowerBranch branch in System.Enum.GetValues(typeof(PowerBranch)))
                    {
                        List<int> nodeIndices = m_NodeIndicesByBranch[branch];
                        if (nodeIndices.Count == 0)
                            continue;
                            
                        EditorGUILayout.Space(5);
                        
                        // Branch header with toggle for all nodes in this branch
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"{branch} ({nodeIndices.Count} nodes)", EditorStyles.boldLabel);
                        
                        bool oldBranchToggle = m_BranchToggles[branch];
                        bool newBranchToggle = EditorGUILayout.Toggle(oldBranchToggle, GUILayout.Width(20));
                        
                        if (newBranchToggle != oldBranchToggle)
                        {
                            m_BranchToggles[branch] = newBranchToggle;
                            foreach (int index in nodeIndices)
                            {
                                m_SelectedNodes[index] = newBranchToggle;
                            }
                        }
                        
                        EditorGUILayout.EndHorizontal();
                        
                        // Draw nodes for this branch
                        foreach (int index in nodeIndices)
                        {
                            PowerNode node = m_AvailableNodes[index];
                            
                            EditorGUILayout.BeginHorizontal();
                            
                            // Indent child nodes
                            GUILayout.Space(20);
                            
                            m_SelectedNodes[index] = EditorGUILayout.Toggle(m_SelectedNodes[index], GUILayout.Width(20));
                            
                            EditorGUI.BeginDisabledGroup(true);
                            EditorGUILayout.ObjectField(node, typeof(PowerNode), false);
                            EditorGUI.EndDisabledGroup();
                            
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }
                else
                {
                    // List all available nodes with checkboxes in a flat list
                    for (int i = 0; i < m_AvailableNodes.Count; i++)
                    {
                        PowerNode node = m_AvailableNodes[i];
                        
                        EditorGUILayout.BeginHorizontal();
                        
                        m_SelectedNodes[i] = EditorGUILayout.Toggle(m_SelectedNodes[i], GUILayout.Width(20));
                        
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.ObjectField(node, typeof(PowerNode), false);
                        EditorGUI.EndDisabledGroup();
                        
                        EditorGUILayout.LabelField(node.branch.ToString(), GUILayout.Width(70));
                        
                        EditorGUILayout.EndHorizontal();
                    }
                }
                
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space();

            // Generate button
            GUI.enabled = m_AvailableNodes.Count > 0;
            if (GUILayout.Button("Generate Database", GUILayout.Height(30)))
            {
                GenerateDatabase();
            }
            GUI.enabled = true;

            EditorGUILayout.EndScrollView();
        }

        private void GenerateDatabase()
        {
            // Create a list of the selected nodes
            List<PowerNode> selectedNodes = new List<PowerNode>();
            
            for (int i = 0; i < m_AvailableNodes.Count; i++)
            {
                if (m_SelectedNodes[i])
                {
                    selectedNodes.Add(m_AvailableNodes[i]);
                }
            }
            
            if (selectedNodes.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "No nodes selected. Please select at least one node.", "OK");
                return;
            }
            
            // Get the directory path
            string directory = Path.GetDirectoryName(m_OutputPath);
            
            // Create the directory if it doesn't exist
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Check if database already exists
            PowerNodeDatabase existingDatabase = AssetDatabase.LoadAssetAtPath<PowerNodeDatabase>(m_OutputPath);
            
            if (existingDatabase != null)
            {
                // Update existing database
                existingDatabase.AllNodes = selectedNodes.ToArray();
                EditorUtility.SetDirty(existingDatabase);
            }
            else
            {
                // Create new database
                PowerNodeDatabase database = ScriptableObject.CreateInstance<PowerNodeDatabase>();
                database.AllNodes = selectedNodes.ToArray();
                
                // Save as asset
                AssetDatabase.CreateAsset(database, m_OutputPath);
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Initialize the database
            PowerNodeDatabase savedDatabase = AssetDatabase.LoadAssetAtPath<PowerNodeDatabase>(m_OutputPath);
            savedDatabase.Initialize();
            EditorUtility.SetDirty(savedDatabase);
            
            // Count nodes by branch for feedback
            Dictionary<PowerBranch, int> branchCounts = new Dictionary<PowerBranch, int>();
            foreach (PowerBranch branch in System.Enum.GetValues(typeof(PowerBranch)))
            {
                branchCounts[branch] = 0;
            }
            
            foreach (PowerNode node in selectedNodes)
            {
                branchCounts[node.branch]++;
            }
            
            string branchInfo = string.Join("\n", branchCounts
                .Where(kvp => kvp.Value > 0)
                .Select(kvp => $"- {kvp.Key}: {kvp.Value} nodes"));
            
            EditorUtility.DisplayDialog("Power Node Database", 
                $"Generated database with {selectedNodes.Count} nodes at {m_OutputPath}\n\nNodes by branch:\n{branchInfo}", 
                "OK");
            
            // Select the database in the project window
            Selection.activeObject = savedDatabase;
            EditorGUIUtility.PingObject(savedDatabase);
        }
    }
} 