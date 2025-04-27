using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace DungeonGeneration
{
    /// <summary>
    /// Main manager class for the procedural dungeon generation system
    /// </summary>
    public class DungeonManager : MonoBehaviour
    {
        #region Singleton
        private static DungeonManager s_Instance;
        public static DungeonManager Instance
        {
            get
            {
                if (s_Instance != null) return s_Instance;
                
                var go = new GameObject("DungeonManager");
                s_Instance = go.AddComponent<DungeonManager>();
                DontDestroyOnLoad(go);
                
                return s_Instance;
            }
        }
        
        private void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            s_Instance = this;
            DontDestroyOnLoad(gameObject);
            
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        #endregion
        
        #region Serialized Fields
        [Header("Generation Settings")]
        [SerializeField] private List<ZoneDefinition> zoneDefinitions;
        [SerializeField] private GameObject m_TransitionSpawnerPrefab;
        [SerializeField] private float m_MinDistanceFromPlayer = 3f;
        [SerializeField] private float m_MaxDistanceFromPlayer = 8f;
        [SerializeField] private float m_MinDistanceBetweenPortals = 2f;
        [SerializeField] private float m_MaxDistanceBetweenPortals = 5f;
        [SerializeField] private bool m_DebugMode = false;
        
        [Header("Stage Labels")]
        [SerializeField] private string m_CombatStageLabel = "Combat";
        [SerializeField] private string m_ChallengeStageLabel = "Challenge";
        [SerializeField] private string m_BossStageLabel = "Boss";
        [SerializeField] private string m_ShopStageLabel = "Shop";
        [SerializeField] private string m_HealingStageLabel = "Healing";
        [SerializeField] private string m_MysteryStageLabel = "?";
        [SerializeField] private string m_InterzoneStageLabel = "Next Zone";
        #endregion
        
        #region Private Fields
        private ZoneDefinition m_CurrentZoneDefinition;
        private int m_ZoneIndex;
        private Dictionary<string, StageNode> m_StageMap = new();
        private StageNode m_CurrentStage;
        private StageNode m_RootStage;
        private StageNode m_BossStage;
        private StageNode m_InterzoneStage;
        private bool m_IsGenerating = false;
        private int m_CurrentDepth = 0;
        private Dictionary<int, int> m_DepthNodeCounters = new Dictionary<int, int>();
        #endregion

        #region Public Properties
        public ZoneDefinition CurrentZoneDefinition => m_CurrentZoneDefinition;
        public int ZoneCount => zoneDefinitions.Count;
        public int ZoneIndex => m_ZoneIndex;
        #endregion

        #region Public Methods for UI Access
        /// <summary>
        /// Gets the current stage map for UI display
        /// </summary>
        public Dictionary<string, StageNode> GetStageMap()
        {
            return m_StageMap;
        }
        
        /// <summary>
        /// Gets the root stage of the current zone
        /// </summary>
        public StageNode GetRootStage()
        {
            return m_RootStage;
        }
        
        /// <summary>
        /// Gets the current active stage
        /// </summary>
        public StageNode GetCurrentStage()
        {
            return m_CurrentStage;
        }
        
        /// <summary>
        /// Gets the boss stage of the current zone
        /// </summary>
        public StageNode GetBossStage()
        {
            return m_BossStage;
        }
        
        /// <summary>
        /// Gets a stage by its ID
        /// </summary>
        public StageNode GetStageByID(string stageID)
        {
            return m_StageMap.GetValueOrDefault(stageID);
        }
        
        public ZoneDefinition GetZoneByIndex(int index)
        {
            if (index < 0 || index >= zoneDefinitions.Count)
            {
                Debug.LogError("Zone index out of range!");
                return null;
            }
            
            return zoneDefinitions[index];
        }
        
        /// <summary>
        /// Notifies the DungeonManager that portals should be created for the current stage
        /// (Used by stage manager scripts when a stage is completed)
        /// </summary>
        public void CreateStageExits()
        {
            StartCoroutine(SetupStagePortals());
        }
        
        /// <summary>
        /// Transitions to the next zone after defeating a boss
        /// </summary>
        public void TransitionToNextZone()
        {
            if (m_InterzoneStage == null) return;
            
            TransitionToStage(m_InterzoneStage.ID);
                
            m_CurrentZoneDefinition = GetZoneDefinitionAtIndex(++m_ZoneIndex);
                
            StartCoroutine(GenerateDungeon());
        }
        #endregion
        
        #region Unity Lifecycle
        private void Start()
        {
            m_CurrentZoneDefinition = GetZoneDefinitionAtIndex(m_ZoneIndex);
            
            if (m_CurrentZoneDefinition == null)
            {
                Debug.LogError("No Zone Definition assigned to DungeonManager!");
                return;
            }
            
            StartCoroutine(GenerateDungeon());
        }
        #endregion
        
        #region Dungeon Generation
        
        private ZoneDefinition GetZoneDefinitionAtIndex(int index)
        {
            if (zoneDefinitions == null)
                return null;
            
            if (index < 0 || index >= zoneDefinitions.Count)
            {
                return null;
            }
            
            return zoneDefinitions[index];
        }
        
        /// <summary>
        /// Generates the procedural dungeon structure for the current zone
        /// </summary>
        private IEnumerator GenerateDungeon()
        {
            if (m_IsGenerating)
            {
                yield break;
            }
            
            m_IsGenerating = true;
            m_StageMap.Clear();
            m_CurrentDepth = 0;
            m_DepthNodeCounters.Clear(); // Clear the node counters
            
            // Create root stage from current scene
            var currentSceneName = SceneManager.GetActiveScene().name;
            m_RootStage = new StageNode(StageType.Interzone, currentSceneName, Vector2.zero);
            m_RootStage.MarkAsGenerated();
            m_CurrentStage = m_RootStage;
            m_StageMap.Add(m_RootStage.ID, m_RootStage);
            
            // Generate boss stage
            m_BossStage = new StageNode(StageType.Boss, m_CurrentZoneDefinition.BossStageName, 
                new Vector2(0, m_CurrentZoneDefinition.OverallLength - 1));
            m_StageMap.Add(m_BossStage.ID, m_BossStage);
            
            // Generate interzone stage (for after boss)
            m_InterzoneStage = new StageNode(StageType.Interzone, m_CurrentZoneDefinition.GetRandomStageScene(StageType.Interzone), 
                new Vector2(0, m_CurrentZoneDefinition.OverallLength));
            m_StageMap.Add(m_InterzoneStage.ID, m_InterzoneStage);
            
            // Connect boss to interzone
            m_BossStage.AddNextStage(m_InterzoneStage);
            
            // Generate stages iteratively
            GenerateStagesRecursive(m_RootStage, 1);
            
            // Set up portals for the initial stage
            CreateStageExits();
            
            m_IsGenerating = false;
            
            if (m_DebugMode)
            {
                DebugPrintDungeonStructure();
            }
        }
        
        /// <summary>
        /// Recursively generates the stages from a starting node
        /// </summary>
        private void GenerateStagesRecursive(StageNode parentStage, int depth)
        {
            // Only the last layer before boss (exactly OverallLength - 1) should connect to boss
            bool isLastLayerBeforeBoss = depth == m_CurrentZoneDefinition.OverallLength - 1;
            
            // If we've reached exactly the last layer before boss, connect to boss
            if (isLastLayerBeforeBoss)
            {
                parentStage.AddNextStage(m_BossStage);
                return;
            }
            
            // If we've somehow gone beyond the last layer, return (shouldn't happen)
            if (depth >= m_CurrentZoneDefinition.OverallLength)
            {
                return;
            }
            
            // Track nodes at each depth to limit their count
            if (!m_DepthNodeCounters.ContainsKey(depth))
            {
                m_DepthNodeCounters[depth] = 0;
            }
            
            // Determine maximum number of branches based on limits
            int maxNodesAtDepth = m_CurrentZoneDefinition.MaxNodesPerDepth;
            int nodesLeftAtDepth = maxNodesAtDepth - m_DepthNodeCounters[depth];
            int maxBranches = Mathf.Min(m_CurrentZoneDefinition.BranchingFactor, nodesLeftAtDepth);
            
            // If no more nodes can be added at this depth, connect to existing nodes or next depth
            if (maxBranches <= 0)
            {
                // Only allow connecting to existing nodes if shared paths are allowed
                if (m_CurrentZoneDefinition.AllowSharedPaths)
                {
                    // Find existing nodes at this depth to connect to
                    var existingNodesAtDepth = m_StageMap.Values
                        .Where(s => Mathf.RoundToInt(s.GridPosition.y) == depth)
                        .OrderBy(s => Random.value) // Shuffle to pick random nodes
                        .Take(1)  // Just take one existing node
                        .ToList();
                    
                    if (existingNodesAtDepth.Count > 0)
                    {
                        parentStage.AddNextStage(existingNodesAtDepth[0]);
                    }
                    else
                    {
                        // Skip to next depth recursively - this ensures proper layering
                        GenerateStagesRecursive(parentStage, depth + 1);
                    }
                }
                else
                {
                    // Skip to next depth recursively - this ensures proper layering
                    GenerateStagesRecursive(parentStage, depth + 1);
                }
                
                return;
            }
            
            // Calculate actual number of branches to create
            int branches = Random.Range(1, maxBranches + 1);
            
            // Keep track of stages created in this recursion to prevent duplicates
            HashSet<StageNode> createdStages = new HashSet<StageNode>();
            
            for (int i = 0; i < branches; i++)
            {
                StageType stageType = m_CurrentZoneDefinition.GetWeightedRandomStageType();
                
                // Get scene name based on stage type
                string sceneName = m_CurrentZoneDefinition.GetRandomStageScene(stageType);
                
                // Calculate position in the grid (for visualization)
                float xOffset = (i - (branches - 1) / 2f) * 2f;
                Vector2 position = new Vector2(xOffset, depth);
                
                StageNode nextStage;
                
                // If shared paths are not allowed, check if there are existing stages at the next depth
                // that we can reuse from OTHER branches (not current parent's branch)
                if (!m_CurrentZoneDefinition.AllowSharedPaths && depth < m_CurrentZoneDefinition.OverallLength - 1)
                {
                    // Create a new stage node
                    nextStage = new StageNode(stageType, sceneName, position);
                    m_StageMap.Add(nextStage.ID, nextStage);
                    
                    // Increment node counter for this depth
                    m_DepthNodeCounters[depth]++;
                    
                    // Add to our tracking set
                    createdStages.Add(nextStage);
                }
                else
                {
                    // Create the stage node as before - allow shared paths
                    nextStage = new StageNode(stageType, sceneName, position);
                    m_StageMap.Add(nextStage.ID, nextStage);
                    
                    // Increment node counter for this depth
                    m_DepthNodeCounters[depth]++;
                }
                
                // Connect parent to this stage
                parentStage.AddNextStage(nextStage);
                
                // Only recursively generate next stages if we're not at the layer before boss
                if (depth < m_CurrentZoneDefinition.OverallLength - 2)
                {
                    // Recursively generate next stages
                    GenerateStagesRecursive(nextStage, depth + 1);
                }
                else
                {
                    // Connect to boss stage directly as we're at the second-to-last layer
                    nextStage.AddNextStage(m_BossStage);
                }
            }
        }
        
        /// <summary>
        /// Sets up the portals for the current stage
        /// </summary>
        private IEnumerator SetupStagePortals()
        {
            yield return new WaitForSeconds(0.5f); // Wait for scene to fully load
            
            // Find player position
            var player = GameObject.FindGameObjectWithTag("Player");
            if (!player)
            {
                Debug.LogError("No player found in scene! Make sure the player has the 'Player' tag.");
                yield break;
            }
            
            Vector3 playerPosition = player.transform.position;
            
            // Get next stages
            List<StageNode> nextStages = m_CurrentStage.NextStages;
            if (nextStages.Count == 0)
            {
                Debug.LogWarning("Current stage has no next stages!");
                yield break;
            }
            
            // Find PortalSpawnerContainer in the scene
            var portalContainer = FindAnyObjectByType<PortalSpawnerContainer>();
            if (!portalContainer)
            {
                Debug.LogError("No PortalSpawnerContainer found in the scene! Portal spawning failed.");
                yield break;
            }

            // Calculate positions for all portals
            var portalCount = nextStages.Count;
            var portalPositions = portalContainer.CalculatePortalPositions(
                portalCount, 
                m_MinDistanceBetweenPortals, 
                m_MaxDistanceBetweenPortals);

            // Spawn portals for each next stage
            for (var i = 0; i < nextStages.Count; i++)
            {
                var nextStage = nextStages[i];
                
                // Create a new spawner inside the container
                var spawner = portalContainer.SpawnTransitionSpawner(
                    m_TransitionSpawnerPrefab,
                    portalPositions[i]);
                
                if (!spawner)
                {
                    Debug.LogError("Failed to create transition spawner!");
                    continue;
                }
                
                // Get appropriate display name based on stage type
                var displayName = GetStageDisplayName(nextStage.StageType);
                nextStage.Name = displayName;
                
                // Spawn the portal
                spawner.SpawnPortal(nextStage.ID, nextStage.SceneName, displayName);
                
                yield return null; // Wait a frame between spawns
            }
        }
        #endregion
        
        #region Stage Transitions
        /// <summary>
        /// Transitions to the specified stage
        /// </summary>
        public void TransitionToStage(string stageID)
        {
            if (!m_StageMap.TryGetValue(stageID, out var targetStage))
            {
                Debug.LogError("Stage ID not found: " + stageID);
                return;
            }

            if (targetStage.StageType == StageType.Mystery)
            {
                targetStage.ResolveMysteryStage(m_CurrentZoneDefinition);
            }
            
            m_CurrentStage = targetStage;
            
            targetStage.MarkAsGenerated();
            
            // Mark this stage as visited
            targetStage.MarkAsVisited();
            
            // Mark all other stages at the same depth as unvisited
            MarkOtherStagesAtSameDepthAsUnvisited(targetStage);

            if (SceneUtilities.SceneExists(targetStage.SceneName))
            {
                SceneManager.LoadScene(targetStage.SceneName);
            }
            else
            {
                Debug.LogError("Scene not found: " + targetStage.SceneName);
            }
        }
        
        /// <summary>
        /// Marks all stages at the same depth as the target stage as unvisited, except for the target stage
        /// </summary>
        private void MarkOtherStagesAtSameDepthAsUnvisited(StageNode targetStage)
        {
            float targetDepth = targetStage.GridPosition.y;
            
            foreach (var stage in m_StageMap.Values)
            {
                // Check if this stage is at the same depth as the target stage
                if (Mathf.Approximately(stage.GridPosition.y, targetDepth) && stage != targetStage)
                {
                    stage.MarkAsUnvisited();
                }
            }
        }
        
        /// <summary>
        /// Called when a scene is loaded
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var stageManager = FindAnyObjectByType<StageBaseManager>();
            if (stageManager != null)
            {
                stageManager.onStageCompleted.AddListener(CreateStageExits);
            }
        }
        #endregion
        
        #region Utility Methods
        /// <summary>
        /// Gets a display name for a stage type
        /// </summary>
        private string GetStageDisplayName(StageType stageType)
        {
            return stageType switch
            {
                StageType.Combat => m_CombatStageLabel,
                StageType.Challenge => m_ChallengeStageLabel,
                StageType.Boss => m_BossStageLabel,
                StageType.Shop => m_ShopStageLabel,
                StageType.Healing => m_HealingStageLabel,
                StageType.Mystery => m_MysteryStageLabel,
                StageType.Interzone => m_InterzoneStageLabel,
                _ => "Unknown"
            };
        }
        
        /// <summary>
        /// Prints the dungeon structure to the console for debugging
        /// </summary>
        private void DebugPrintDungeonStructure()
        {
            StringBuilder debugText = new StringBuilder();
    
            debugText.AppendLine("=== DUNGEON STRUCTURE ===");
            debugText.AppendLine($"Zone: {m_CurrentZoneDefinition.ZoneName}");
            debugText.AppendLine($"Length: {m_CurrentZoneDefinition.OverallLength} stages + boss");
            debugText.AppendLine($"Total Stages: {m_StageMap.Count}");
            debugText.AppendLine();
    
            // Print the structure
            DebugPrintStageRecursive(m_RootStage, "", debugText);
    
            debugText.AppendLine("========================");
    
            Debug.Log(debugText.ToString());
        }

        /// <summary>
        /// Recursively prints a stage and its connections
        /// </summary>
        private void DebugPrintStageRecursive(StageNode stage, string indent, StringBuilder debugText)
        {
            debugText.AppendLine($"{indent}[{stage.StageType}] {stage.SceneName} - Position: ({stage.GridPosition.x:F1}, {stage.GridPosition.y:F1})");

            foreach (StageNode nextStage in stage.NextStages)
            {
                DebugPrintStageRecursive(nextStage, indent + "  ", debugText);
            }
        }
        #endregion
    }
} 