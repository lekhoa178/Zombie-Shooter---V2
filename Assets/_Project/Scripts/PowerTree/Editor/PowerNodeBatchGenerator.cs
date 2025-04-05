using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace PowerTree.Editor
{
    /// <summary>
    /// Editor utility to batch generate multiple PowerNode templates
    /// </summary>
    public class PowerNodeBatchGenerator : EditorWindow
    {
        [MenuItem("Tools/PowerTree/Batch Create Node Templates", false, 100)]
        public static void ShowWindow()
        {
            GetWindow<PowerNodeBatchGenerator>("Batch Create Power Nodes");
        }

        private PowerBranch selectedBranch = PowerBranch.Survival;
        private string basePath = "Custom";
        private Vector2 scrollPosition;
        private List<NodeDefinition> nodeDefinitions = new List<NodeDefinition>();

        private void OnEnable()
        {
            // Initialize with one empty node definition
            if (nodeDefinitions.Count == 0)
            {
                nodeDefinitions.Add(new NodeDefinition());
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("Batch Create Power Node Templates", EditorStyles.boldLabel);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            selectedBranch = (PowerBranch)EditorGUILayout.EnumPopup("Branch", selectedBranch);
            basePath = EditorGUILayout.TextField("Base Menu Path", basePath);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Node Definitions", EditorStyles.boldLabel);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            for (int i = 0; i < nodeDefinitions.Count; i++)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.BeginHorizontal();
                nodeDefinitions[i].ClassName = EditorGUILayout.TextField("Class Name", nodeDefinitions[i].ClassName);
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    nodeDefinitions.RemoveAt(i);
                    GUIUtility.ExitGUI();
                }
                EditorGUILayout.EndHorizontal();
                
                nodeDefinitions[i].DisplayName = EditorGUILayout.TextField("Display Name", nodeDefinitions[i].DisplayName);
                nodeDefinitions[i].Description = EditorGUILayout.TextField("Description", nodeDefinitions[i].Description);
                
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.Space();
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Add Node"))
            {
                nodeDefinitions.Add(new NodeDefinition());
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Generate All Nodes"))
            {
                GenerateAllNodes();
            }
        }

        private void GenerateAllNodes()
        {
            string directory = PowerNodeTemplateGenerator.GetSelectedPathOrFallback();
            
            // Create a subdirectory for the branch if needed
            string branchDir = Path.Combine(directory, selectedBranch.ToString());
            if (!Directory.Exists(branchDir))
            {
                Directory.CreateDirectory(branchDir);
            }
            
            // Generate each node
            foreach (var node in nodeDefinitions)
            {
                if (string.IsNullOrEmpty(node.ClassName)) continue;
                
                string className = node.ClassName;
                if (!className.EndsWith("Node"))
                {
                    className += "Node";
                }
                
                string menuPathCombined = string.IsNullOrEmpty(basePath) ? 
                    node.DisplayName : Path.Combine(basePath, node.DisplayName).Replace("\\", "/");
                
                string content = GenerateNodeScript(className, node.DisplayName, node.Description, menuPathCombined, selectedBranch);
                
                string filePath = Path.Combine(branchDir, $"{className}.cs");
                File.WriteAllText(filePath, content);
            }
            
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Generation Complete", $"Generated {nodeDefinitions.Count} node templates in {branchDir}", "OK");
        }

        private string GenerateNodeScript(string className, string displayName, string description, string menuPath, PowerBranch branch)
        {
            string branchName = branch.ToString();
            
            return $@"using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PowerTree
{{
    /// <summary>
    /// {displayName}: {description}
    /// </summary>
    [CreateAssetMenu(fileName = ""{className}"", menuName = ""PowerTree/Nodes/{branchName}/{menuPath}"")]
    public class {className} : PowerNode
    {{
        [SerializeField] private float[] effectValues = new float[] {{ 0.1f, 0.2f, 0.3f }}; // Values for level 1, 2, 3
        
        private float currentValue = 0f;
        
        private void OnValidate()
        {{
            // Auto-populate descriptions with field values
            descriptions = new string[]
            {{
                ""Level 1: {description} with {{effectValues[0]}} bonus"",
                ""Level 2: {description} with {{effectValues[1]}} bonus"",
                ""Level 3: {description} with {{effectValues[2]}} bonus""
            }};
        }}
        
        public override void ApplyEffect(PlayerStats player, int level)
        {{
            if (level <= 0 || level > effectValues.Length) return;
            
            currentValue = effectValues[level - 1];
            
            // Subscribe to events that this node should respond to
            // Example:
            // player.OnRedHeartLost.AddListener(OnRedHeartLost);
            
            // Apply immediate effects
            // Example:
            // player.ModifyDamageMultiplier(currentValue);
        }}
        
        public override void RemoveEffect(PlayerStats player)
        {{
            // Remove event listeners
            // Example:
            // player.OnRedHeartLost.RemoveListener(OnRedHeartLost);
            
            // Remove any persistent effects
            // Example:
            // player.ModifyDamageMultiplier(-currentValue);
        }}
        
        // Add methods to handle events as needed
        // Example:
        /*
        private void OnRedHeartLost(float amount)
        {{
            var player = FindObjectOfType<PlayerStats>();
            if (player != null)
            {{
                // Implement effect logic here
            }}
        }}
        */
    }}
}}";
        }

        [System.Serializable]
        private class NodeDefinition
        {
            public string ClassName = "";
            public string DisplayName = "";
            public string Description = "";
        }
    }
} 