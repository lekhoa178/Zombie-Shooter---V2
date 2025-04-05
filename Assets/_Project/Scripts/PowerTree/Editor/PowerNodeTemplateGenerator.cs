using System.IO;
using UnityEngine;
using UnityEditor;

namespace PowerTree.Editor
{
    /// <summary>
    /// Editor utility to create PowerNode template scripts
    /// </summary>
    public class PowerNodeTemplateGenerator
    {
        [MenuItem("Assets/Create/PowerTree/Node Template Script", false, 81)]
        public static void CreatePowerNodeTemplate()
        {
            // Create a dialog to get node information
            PowerNodeTemplateWindow.ShowWindow();
        }

        /// <summary>
        /// Generates a PowerNode script from the template
        /// </summary>
        public static void GenerateScript(string className, string displayName, string menuPath, PowerBranch branch)
        {
            // Get the directory where the script should be created
            string directory = GetSelectedPathOrFallback();
            
            // Ensure directory exists
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Create the file path
            string filePath = Path.Combine(directory, $"{className}.cs");
            
            // Generate the script content
            string scriptContent = GenerateNodeScript(className, displayName, menuPath, branch);
            
            // Write the script to disk
            File.WriteAllText(filePath, scriptContent);
            
            // Refresh the asset database
            AssetDatabase.Refresh();
            
            // Open the file in the editor
            AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<MonoScript>(filePath));
        }
        
        /// <summary>
        /// Generate the PowerNode script content
        /// </summary>
        private static string GenerateNodeScript(string className, string displayName, string menuPath, PowerBranch branch)
        {
            string branchName = branch.ToString();
            
            return $@"using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PowerTree
{{
    /// <summary>
    /// {displayName}: Description of what this node does
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
                ""Level 1 effect description with {{effectValues[0]}} placeholder"",
                ""Level 2 effect description with {{effectValues[1]}} placeholder"",
                ""Level 3 effect description with {{effectValues[2]}} placeholder""
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
        
        /// <summary>
        /// Get the selected directory or fallback to Assets
        /// </summary>
        public static string GetSelectedPathOrFallback()
        {
            string path = "Assets";
            
            foreach (Object obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
            {
                path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    path = Path.GetDirectoryName(path);
                    break;
                }
            }
            
            return path;
        }
    }

    /// <summary>
    /// Window to collect PowerNode template information
    /// </summary>
    public class PowerNodeTemplateWindow : EditorWindow
    {
        private string className = "NewPowerNode";
        private string displayName = "New Power Node";
        private string menuPath = "Custom";
        private PowerBranch branch = PowerBranch.Survival;
        private string[] branchOptions = { "Survival", "Attack", "Evasion", "Finance" };
        
        public static void ShowWindow()
        {
            PowerNodeTemplateWindow window = GetWindow<PowerNodeTemplateWindow>("Create Power Node");
            window.minSize = new Vector2(300, 200);
            window.maxSize = new Vector2(300, 200);
            window.ShowUtility();
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Create Power Node Template", EditorStyles.boldLabel);
            
            EditorGUILayout.Space();
            
            className = EditorGUILayout.TextField("Class Name", className);
            displayName = EditorGUILayout.TextField("Display Name", displayName);
            menuPath = EditorGUILayout.TextField("Menu Path", menuPath);
            
            EditorGUILayout.Space();
            
            branch = (PowerBranch)EditorGUILayout.Popup("Branch", (int)branch, branchOptions);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Create"))
            {
                if (string.IsNullOrEmpty(className))
                {
                    EditorUtility.DisplayDialog("Error", "Class name cannot be empty", "OK");
                    return;
                }
                
                // Ensure class name ends with "Node"
                if (!className.EndsWith("Node"))
                {
                    className += "Node";
                }
                
                PowerNodeTemplateGenerator.GenerateScript(className, displayName, menuPath, branch);
                Close();
            }
        }
    }
} 