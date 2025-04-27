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
            
            // Generate boss stage with the precise depth level at OverallLength - 1
            m_BossStage = new StageNode(StageType.Boss, m_CurrentZoneDefinition.BossStageName, 
                new Vector2(0, m_CurrentZoneDefinition.OverallLength - 1));
            m_StageMap.Add(m_BossStage.ID, m_BossStage);
            
            // Generate interzone stage (for after boss) with the precise depth level at OverallLength
            m_InterzoneStage = new StageNode(StageType.Interzone, m_CurrentZoneDefinition.GetRandomStageScene(StageType.Interzone), 
                new Vector2(0, m_CurrentZoneDefinition.OverallLength));
            m_StageMap.Add(m_InterzoneStage.ID, m_InterzoneStage);
            
            // Connect boss to interzone
            m_BossStage.AddNextStage(m_InterzoneStage);
            
            // Generate stages iteratively, starting at depth 1
            GenerateStagesRecursive(m_RootStage, 1);
            
            // Post-processing: validate depth progression - ensure no stage skips depths
            ValidateDepthProgression();
            
            // Check for shared paths when not allowed
            if (!m_CurrentZoneDefinition.AllowSharedPaths)
            {
                FixSharedPaths();
            }
            
            // Validate that all nodes have a path to the boss
            FixDeadEndNodes();
            
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
            int maxBranches = Mathf.Min(m_CurrentZoneDefinition.MaxBranchingFactor, nodesLeftAtDepth);
            
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
                        // Skip to next depth recursively - using depth+1 ensures we follow proper depth progression
                        GenerateStagesRecursive(parentStage, depth + 1);
                    }
                }
                else
                {
                    // Skip to next depth recursively - using depth+1 ensures we follow proper depth progression
                    GenerateStagesRecursive(parentStage, depth + 1);
                }
                
                return;
            }
            
            // Calculate actual number of branches to create based on weighted probabilities
            int branches = m_CurrentZoneDefinition.GetRandomBranchingFactor();
            // Cap the branches by the maximum allowed
            branches = Mathf.Min(branches, maxBranches);
            
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
                
                // Always continue to the next depth level until we reach the last layer
                // before boss (OverallLength - 1)
                if (depth < m_CurrentZoneDefinition.OverallLength - 1)
                {
                    // Recursively generate next stages but ALWAYS at the next depth level (depth+1)
                    GenerateStagesRecursive(nextStage, depth + 1);
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
        
        /// <summary>
        /// Validates depth progression of the dungeon structure and fixes any stage connections that skip depths
        /// </summary>
        private void ValidateDepthProgression()
        {
            bool foundIssue = false;
            int maxAllowedDepthJump = 1; // Only allow connections to next depth level
            
            // First, count existing nodes at each depth
            Dictionary<int, int> depthNodeCounts = new Dictionary<int, int>();
            foreach (var stage in m_StageMap.Values)
            {
                int depth = Mathf.RoundToInt(stage.GridPosition.y);
                if (!depthNodeCounts.ContainsKey(depth))
                {
                    depthNodeCounts[depth] = 0;
                }
                depthNodeCounts[depth]++;
            }
            
            // Update m_DepthNodeCounters to match the actual counts
            m_DepthNodeCounters = new Dictionary<int, int>(depthNodeCounts);
            
            // Build parent-child relationships for checking shared paths
            Dictionary<StageNode, List<StageNode>> nodeParents = BuildNodeParentsMap();
            
            // Check each node's connections
            foreach (var stage in m_StageMap.Values.ToList()) // ToList to avoid collection modification issues
            {
                // Skip special nodes (boss and interzone)
                if (stage.StageType == StageType.Boss || stage.StageType == StageType.Interzone)
                {
                    continue;
                }
                
                // Get this stage's depth
                int stageDepth = Mathf.RoundToInt(stage.GridPosition.y);
                
                List<StageNode> nextStagesToRemove = new List<StageNode>();
                
                // Check each of its connections
                foreach (var nextStage in stage.NextStages.ToList()) // ToList to avoid collection modification issues
                {
                    // Get next stage's depth
                    int nextStageDepth = Mathf.RoundToInt(nextStage.GridPosition.y);
                    
                    // Check if depth jump is too big (skipping levels)
                    int depthJump = nextStageDepth - stageDepth;
                    
                    if (depthJump > maxAllowedDepthJump)
                    {
                        foundIssue = true;
                        
                        if (m_DebugMode)
                        {
                            Debug.LogWarning($"Found invalid depth jump: {stage.StageType} at depth {stageDepth} " +
                                             $"connects to {nextStage.StageType} at depth {nextStageDepth}");
                        }
                        
                        // Fix: Remove this connection and fix it later
                        nextStagesToRemove.Add(nextStage);
                        
                        // Only try to add intermediate nodes if it's not the boss
                        if (nextStage.StageType != StageType.Boss)
                        {
                            // Create a chain of nodes to fix the gap
                            StageNode lastNode = stage;
                            
                            for (int intermediateDepth = stageDepth + 1; intermediateDepth < nextStageDepth; intermediateDepth++)
                            {
                                // Check if we've reached the max node count at this depth
                                if (!depthNodeCounts.ContainsKey(intermediateDepth))
                                {
                                    depthNodeCounts[intermediateDepth] = 0;
                                }
                                
                                int maxNodesAtDepth = m_CurrentZoneDefinition.MaxNodesPerDepth;
                                bool allowSharedPaths = m_CurrentZoneDefinition.AllowSharedPaths;
                                
                                // If shared paths are allowed, try to find an existing node at this depth
                                if (allowSharedPaths)
                                {
                                    // Try to find an existing node at the intermediate depth
                                    var existingNodesAtDepth = m_StageMap.Values
                                        .Where(s => Mathf.RoundToInt(s.GridPosition.y) == intermediateDepth)
                                        .OrderBy(s => Vector2.Distance(s.GridPosition, new Vector2(lastNode.GridPosition.x, intermediateDepth)))
                                        .Take(1)
                                        .ToList();
                                        
                                    if (existingNodesAtDepth.Count > 0)
                                    {
                                        // Use an existing node at this depth
                                        lastNode.AddNextStage(existingNodesAtDepth[0]);
                                        lastNode = existingNodesAtDepth[0];
                                        continue; // Continue to next depth level
                                    }
                                }
                                
                                // If we're at the max count, try to find an existing node to use only if shared paths are allowed
                                if (depthNodeCounts[intermediateDepth] >= maxNodesAtDepth)
                                {
                                    if (allowSharedPaths)
                                    {
                                        // Try to find an existing node at the intermediate depth
                                        var existingNodesAtDepth = m_StageMap.Values
                                            .Where(s => Mathf.RoundToInt(s.GridPosition.y) == intermediateDepth)
                                            .OrderBy(s => Vector2.Distance(s.GridPosition, new Vector2(lastNode.GridPosition.x, intermediateDepth)))
                                            .Take(1)
                                            .ToList();
                                            
                                            if (existingNodesAtDepth.Count > 0)
                                            {
                                                // Use an existing node at this depth
                                                lastNode.AddNextStage(existingNodesAtDepth[0]);
                                                lastNode = existingNodesAtDepth[0];
                                            }
                                            else if (m_DebugMode)
                                            {
                                                Debug.LogWarning($"Max nodes reached at depth {intermediateDepth}, but no good connection found. " +
                                                              $"Skip connection from {stage.StageType} at {stageDepth} to {nextStage.StageType} at {nextStageDepth}.");
                                            }
                                    }
                                    else if (m_DebugMode)
                                    {
                                        Debug.LogWarning($"Max nodes reached at depth {intermediateDepth} and shared paths not allowed. " +
                                                      $"Skip connection from {stage.StageType} at {stageDepth} to {nextStage.StageType} at {nextStageDepth}.");
                                    }
                                }
                                else
                                {
                                    // Create a new intermediate node
                                    StageType intermediateType = m_CurrentZoneDefinition.GetWeightedRandomStageType();
                                    string sceneName = m_CurrentZoneDefinition.GetRandomStageScene(intermediateType);
                                    Vector2 intermediatePosition = new Vector2(lastNode.GridPosition.x, intermediateDepth);
                                    
                                    StageNode intermediateNode = new StageNode(intermediateType, sceneName, intermediatePosition);
                                    m_StageMap.Add(intermediateNode.ID, intermediateNode);
                                    
                                    // Connect previous node to this one
                                    lastNode.AddNextStage(intermediateNode);
                                    
                                    // Increment counter for this depth
                                    depthNodeCounts[intermediateDepth]++;
                                    m_DepthNodeCounters[intermediateDepth] = depthNodeCounts[intermediateDepth];
                                    
                                    // Update last node for next iteration
                                    lastNode = intermediateNode;
                                }
                            }
                            
                            // Connect last intermediate node to the originally skipped node
                            if (lastNode != stage) // Only connect if we actually created/found intermediate nodes
                            {
                                lastNode.AddNextStage(nextStage);
                            }
                        }
                        else
                        {
                            // Handle boss connection - ensure proper path to boss
                            StageNode lastNode = stage;
                            
                            // Always ensure a path to the boss, even if we exceed max nodes
                            for (int intermediateDepth = stageDepth + 1; intermediateDepth < nextStageDepth; intermediateDepth++)
                            {
                                bool allowSharedPaths = m_CurrentZoneDefinition.AllowSharedPaths;
                                
                                // Try to use existing nodes first to minimize node count, but respect AllowSharedPaths
                                if (allowSharedPaths)
                                {
                                    var existingNodesAtDepth = m_StageMap.Values
                                        .Where(s => Mathf.RoundToInt(s.GridPosition.y) == intermediateDepth)
                                        .OrderBy(s => Vector2.Distance(s.GridPosition, new Vector2(lastNode.GridPosition.x, intermediateDepth)))
                                        .Take(1)
                                        .ToList();
                                        
                                    if (existingNodesAtDepth.Count > 0)
                                    {
                                        // Use existing node if shared paths are allowed
                                        lastNode.AddNextStage(existingNodesAtDepth[0]);
                                        lastNode = existingNodesAtDepth[0];
                                        continue; // Continue to next depth level
                                    }
                                }
                                
                                // If we couldn't use an existing node, create a new one
                                // For boss paths, we'll create new nodes even if over the limit
                                StageType intermediateType = m_CurrentZoneDefinition.GetWeightedRandomStageType();
                                string sceneName = m_CurrentZoneDefinition.GetRandomStageScene(intermediateType);
                                Vector2 intermediatePosition = new Vector2(lastNode.GridPosition.x, intermediateDepth);
                                
                                StageNode intermediateNode = new StageNode(intermediateType, sceneName, intermediatePosition);
                                m_StageMap.Add(intermediateNode.ID, intermediateNode);
                                
                                // Connect previous node to this one
                                lastNode.AddNextStage(intermediateNode);
                                
                                // Update counter
                                if (!depthNodeCounts.ContainsKey(intermediateDepth))
                                {
                                    depthNodeCounts[intermediateDepth] = 0;
                                }
                                depthNodeCounts[intermediateDepth]++;
                                m_DepthNodeCounters[intermediateDepth] = depthNodeCounts[intermediateDepth];
                                
                                // Update last node for next iteration
                                lastNode = intermediateNode;
                            }
                            
                            // Connect last intermediate node to boss
                            if (lastNode != stage) // Only connect if we actually created/found intermediate nodes
                            {
                                lastNode.AddNextStage(nextStage);
                            }
                        }
                    }
                }
                
                // Remove any invalid connections
                foreach (var nodeToRemove in nextStagesToRemove)
                {
                    stage.NextStages.Remove(nodeToRemove);
                }
            }
            
            // Final pass: check for any depth levels with too many nodes and trim if needed
            EnforceMaxNodesPerDepth();
            
            // Check for shared paths when not allowed
            if (!m_CurrentZoneDefinition.AllowSharedPaths)
            {
                FixSharedPaths();
            }
            
            if (foundIssue && m_DebugMode)
            {
                Debug.Log("Fixed depth progression issues in dungeon structure");
            }
        }
        
        /// <summary>
        /// Builds a map of parent nodes for each node
        /// </summary>
        private Dictionary<StageNode, List<StageNode>> BuildNodeParentsMap()
        {
            Dictionary<StageNode, List<StageNode>> nodeParents = new Dictionary<StageNode, List<StageNode>>();
            
            foreach (var node in m_StageMap.Values)
            {
                if (!nodeParents.ContainsKey(node))
                {
                    nodeParents[node] = new List<StageNode>();
                }
                
                foreach (var nextStage in node.NextStages)
                {
                    if (!nodeParents.ContainsKey(nextStage))
                    {
                        nodeParents[nextStage] = new List<StageNode>();
                    }
                    
                    nodeParents[nextStage].Add(node);
                }
            }
            
            return nodeParents;
        }
        
        /// <summary>
        /// Fixes shared paths when they're not allowed in the zone configuration
        /// </summary>
        private void FixSharedPaths()
        {
            if (m_CurrentZoneDefinition.AllowSharedPaths)
                return;
                
            bool foundSharedPath;
            Dictionary<int, int> depthNodeCounts = new Dictionary<int, int>();
            
            // Build the initial parent map
            Dictionary<StageNode, List<StageNode>> parentMap = BuildNodeParentsMap();
            
            // Count nodes at each depth
            foreach (var stage in m_StageMap.Values)
            {
                int depth = Mathf.RoundToInt(stage.GridPosition.y);
                if (!depthNodeCounts.ContainsKey(depth))
                {
                    depthNodeCounts[depth] = 0;
                }
                depthNodeCounts[depth]++;
            }
            
            do
            {
                foundSharedPath = false;
                
                // Check each node for multiple parents
                foreach (var entry in parentMap)
                {
                    StageNode node = entry.Key;
                    List<StageNode> parents = entry.Value;
                    
                    // Skip boss and interzone (they always have shared paths)
                    if (node.StageType == StageType.Boss || node.StageType == StageType.Interzone)
                        continue;
                        
                    // If this node has multiple parents, we need to fix it
                    if (parents.Count > 1)
                    {
                        foundSharedPath = true;
                        
                        if (m_DebugMode)
                        {
                            Debug.LogWarning($"Found shared path to {node.StageType} at depth {Mathf.RoundToInt(node.GridPosition.y)} with {parents.Count} parents");
                        }
                        
                        // Keep the first parent connection, replace others
                        StageNode keepParent = parents[0];
                        
                        // For each additional parent, create a new node
                        for (int i = 1; i < parents.Count; i++)
                        {
                            StageNode parent = parents[i];
                            
                            // Remove this connection
                            parent.NextStages.Remove(node);
                            
                            // Create a replacement node at the same depth
                            int depth = Mathf.RoundToInt(node.GridPosition.y);
                            
                            // Check if we can create a new node at this depth
                            if (depthNodeCounts.ContainsKey(depth) && 
                                depthNodeCounts[depth] >= m_CurrentZoneDefinition.MaxNodesPerDepth)
                            {
                                // We're at the max, just disconnect this path
                                if (m_DebugMode)
                                {
                                    Debug.LogWarning($"Max nodes reached at depth {depth}, removing connection from {parent.StageType}");
                                }
                                continue;
                            }
                            
                            // Create a new node with similar properties but at parent's X-position
                            StageType newType = node.StageType;
                            string sceneName = node.SceneName;
                            
                            // If it's a mystery stage, get a new random type
                            if (newType == StageType.Mystery)
                            {
                                newType = m_CurrentZoneDefinition.GetWeightedRandomStageType();
                                sceneName = m_CurrentZoneDefinition.GetRandomStageScene(newType);
                            }
                            
                            // Create at parent's x-coordinate with node's y-coordinate
                            Vector2 newPosition = new Vector2(parent.GridPosition.x, node.GridPosition.y);
                            
                            // Create the new node
                            StageNode newNode = new StageNode(newType, sceneName, newPosition);
                            
                            // Connect parent to new node
                            parent.AddNextStage(newNode);
                            
                            // Connect new node to all the same destinations as the original node
                            foreach (var nextNode in node.NextStages)
                            {
                                newNode.AddNextStage(nextNode);
                            }
                            
                            // Add to stage map
                            m_StageMap.Add(newNode.ID, newNode);
                            
                            // Increment node count at this depth
                            depthNodeCounts[depth]++;
                            m_DepthNodeCounters[depth] = depthNodeCounts[depth];
                        }
                        
                        // Rebuild parent map for next iteration
                        parentMap = BuildNodeParentsMap();
                        
                        // Break after handling one node to avoid concurrent modification issues
                        break;
                    }
                }
            } while (foundSharedPath);
            
            if (m_DebugMode && foundSharedPath)
            {
                Debug.Log("Fixed shared paths in dungeon structure");
            }
        }
        
        /// <summary>
        /// Enforces the maximum nodes per depth limit by removing excess nodes
        /// </summary>
        private void EnforceMaxNodesPerDepth()
        {
            Dictionary<int, List<StageNode>> nodesByDepth = new Dictionary<int, List<StageNode>>();
            
            // Group nodes by depth
            foreach (var stage in m_StageMap.Values.ToList())
            {
                int depth = Mathf.RoundToInt(stage.GridPosition.y);
                
                // Skip boss and interzone depth levels
                if (stage.StageType == StageType.Boss || stage.StageType == StageType.Interzone)
                {
                    continue;
                }
                
                if (!nodesByDepth.ContainsKey(depth))
                {
                    nodesByDepth[depth] = new List<StageNode>();
                }
                
                nodesByDepth[depth].Add(stage);
            }
            
            // Check each depth for excess nodes
            foreach (var depthPair in nodesByDepth)
            {
                int depth = depthPair.Key;
                List<StageNode> nodesAtDepth = depthPair.Value;
                
                int maxNodes = m_CurrentZoneDefinition.MaxNodesPerDepth;
                
                // If we have more nodes than allowed, trim the excess
                if (nodesAtDepth.Count > maxNodes)
                {
                    if (m_DebugMode)
                    {
                        Debug.LogWarning($"Found {nodesAtDepth.Count} nodes at depth {depth}, max allowed is {maxNodes}. Trimming excess nodes.");
                    }
                    
                    // Sort nodes by number of connections - keep nodes with more connections
                    nodesAtDepth.Sort((a, b) => 
                    {
                        int aConnections = CountConnections(a);
                        int bConnections = CountConnections(b);
                        return bConnections.CompareTo(aConnections); // Descending
                    });
                    
                    // Remove excess nodes
                    int excessCount = nodesAtDepth.Count - maxNodes;
                    for (int i = 0; i < excessCount; i++)
                    {
                        if (nodesAtDepth.Count <= maxNodes) break;
                        
                        // Choose a node to remove (from the end, as we sorted by connections)
                        var nodeToRemove = nodesAtDepth[nodesAtDepth.Count - 1];
                        
                        // First, check if it has any parents
                        var parents = FindParents(nodeToRemove);
                        
                        if (parents.Count == 0)
                        {
                            // No parents, just remove it
                            m_StageMap.Remove(nodeToRemove.ID);
                            nodesAtDepth.RemoveAt(nodesAtDepth.Count - 1);
                            continue;
                        }
                        
                        // Has parents, need to redirect them
                        foreach (var parent in parents)
                        {
                            // Remove link to the node we're removing
                            parent.NextStages.Remove(nodeToRemove);
                            
                            // Check if we can create connections based on AllowSharedPaths
                            bool allowSharedPaths = m_CurrentZoneDefinition.AllowSharedPaths;
                            
                            if (allowSharedPaths)
                            {
                                // Find a suitable replacement at this depth
                                var replacementNode = nodesAtDepth.FirstOrDefault(n => n != nodeToRemove && n.StageType != StageType.Boss);
                                
                                // If no good replacement, try to find a node at the next depth
                                if (replacementNode == null && nodesByDepth.ContainsKey(depth + 1) && nodesByDepth[depth + 1].Count > 0)
                                {
                                    var nextDepthNode = nodesByDepth[depth + 1].OrderBy(_ => Random.value).FirstOrDefault();
                                    if (nextDepthNode != null)
                                    {
                                        parent.AddNextStage(nextDepthNode);
                                    }
                                }
                                else if (replacementNode != null)
                                {
                                    parent.AddNextStage(replacementNode);
                                }
                            }
                            else
                            {
                                // If shared paths aren't allowed, we need to find a unique path
                                // Find a replacement node that isn't already connected to this parent
                                var replacementNode = nodesAtDepth.FirstOrDefault(n => 
                                    n != nodeToRemove && 
                                    n.StageType != StageType.Boss && 
                                    !parent.NextStages.Contains(n) &&
                                    FindParents(n).Count == 0); // No other parents
                                
                                if (replacementNode != null)
                                {
                                    parent.AddNextStage(replacementNode);
                                }
                                else if (nodesByDepth.ContainsKey(depth + 1) && nodesByDepth[depth + 1].Count > 0)
                                {
                                    // Try to find a node in the next depth with no parents
                                    var nextDepthNodes = nodesByDepth[depth + 1]
                                        .Where(n => FindParents(n).Count == 0)
                                        .OrderBy(_ => Random.value)
                                        .ToList();
                                        
                                    if (nextDepthNodes.Count > 0)
                                    {
                                        parent.AddNextStage(nextDepthNodes[0]);
                                    }
                                    else
                                    {
                                        // As a last resort, create a new node at the next depth
                                        if (depth + 1 < m_CurrentZoneDefinition.OverallLength - 1) // Don't create nodes right before boss
                                        {
                                            StageType newType = m_CurrentZoneDefinition.GetWeightedRandomStageType();
                                            string sceneName = m_CurrentZoneDefinition.GetRandomStageScene(newType);
                                            Vector2 newPosition = new Vector2(parent.GridPosition.x, depth + 1);
                                            
                                            StageNode newNode = new StageNode(newType, sceneName, newPosition);
                                            m_StageMap.Add(newNode.ID, newNode);
                                            
                                            parent.AddNextStage(newNode);
                                            
                                            // Add connections to the next depth (like the original node had)
                                            foreach (var nextStage in nodeToRemove.NextStages)
                                            {
                                                newNode.AddNextStage(nextStage);
                                            }
                                            
                                            // Add to depth mapping
                                            if (!nodesByDepth.ContainsKey(depth + 1))
                                            {
                                                nodesByDepth[depth + 1] = new List<StageNode>();
                                            }
                                            nodesByDepth[depth + 1].Add(newNode);
                                        }
                                    }
                                }
                            }
                        }
                        
                        // Remove the node from the map and list
                        m_StageMap.Remove(nodeToRemove.ID);
                        nodesAtDepth.RemoveAt(nodesAtDepth.Count - 1);
                        
                        // Update counter
                        m_DepthNodeCounters[depth] = nodesAtDepth.Count;
                    }
                }
            }
        }
        
        /// <summary>
        /// Counts total connections (in and out) for a node
        /// </summary>
        private int CountConnections(StageNode node)
        {
            int inConnections = FindParents(node).Count;
            int outConnections = node.NextStages.Count;
            return inConnections + outConnections;
        }
        
        /// <summary>
        /// Finds all parent nodes that connect to the specified node
        /// </summary>
        private List<StageNode> FindParents(StageNode node)
        {
            List<StageNode> parents = new List<StageNode>();
            
            foreach (var potentialParent in m_StageMap.Values)
            {
                if (potentialParent.NextStages.Contains(node))
                {
                    parents.Add(potentialParent);
                }
            }
            
            return parents;
        }
        
        /// <summary>
        /// Fixes any nodes that don't have a path to the boss stage (dead ends)
        /// </summary>
        private void FixDeadEndNodes()
        {
            bool fixedSomething = true;
            int iterationCount = 0;
            int maxIterations = 5; // Prevent infinite loops
            
            // Dictionary to keep track of which nodes can reach the boss
            Dictionary<StageNode, bool> canReachBoss = new Dictionary<StageNode, bool>();
            
            // The boss can obviously reach itself
            canReachBoss[m_BossStage] = true;
            canReachBoss[m_InterzoneStage] = true;
            
            // First, mark all nodes that can definitely reach the boss (bottom-up traversal)
            while (fixedSomething && iterationCount < maxIterations)
            {
                fixedSomething = false;
                iterationCount++;
                
                // Check each node
                foreach (var node in m_StageMap.Values.ToList())
                {
                    // Skip if we already know this node can reach the boss
                    if (canReachBoss.TryGetValue(node, out bool reachable) && reachable)
                        continue;
                        
                    // Check if any of this node's next stages can reach the boss
                    foreach (var nextStage in node.NextStages)
                    {
                        if (canReachBoss.TryGetValue(nextStage, out bool nextCanReach) && nextCanReach)
                        {
                            canReachBoss[node] = true;
                            fixedSomething = true;
                            break;
                        }
                    }
                }
            }
            
            // Now identify and fix any nodes that can't reach the boss
            foreach (var node in m_StageMap.Values.ToList())
            {
                // Skip special nodes
                if (node.StageType == StageType.Boss || node.StageType == StageType.Interzone)
                    continue;
                    
                // If we already know this node can reach the boss, skip it
                if (canReachBoss.TryGetValue(node, out bool reachable) && reachable)
                    continue;
                    
                if (m_DebugMode)
                {
                    Debug.LogWarning($"Found dead-end node: {node.StageType} at depth {Mathf.RoundToInt(node.GridPosition.y)}");
                }
                
                // We need to fix this node by connecting it to the boss or to a node that can reach the boss
                int nodeDepth = Mathf.RoundToInt(node.GridPosition.y);
                
                if (nodeDepth == m_CurrentZoneDefinition.OverallLength - 2)
                {
                    // If this node is right before the boss, connect directly
                    node.AddNextStage(m_BossStage);
                    if (m_DebugMode)
                    {
                        Debug.Log($"Connected dead-end node at depth {nodeDepth} directly to boss");
                    }
                    continue;
                }
                
                // Otherwise, try to find a nearby node that can reach the boss
                List<StageNode> reachableNodes = new List<StageNode>();
                foreach (var otherNode in m_StageMap.Values)
                {
                    int otherDepth = Mathf.RoundToInt(otherNode.GridPosition.y);
                    
                    // Only consider nodes at the next depth level
                    if (otherDepth != nodeDepth + 1)
                        continue;
                        
                    // And only if they can reach the boss
                    if (canReachBoss.TryGetValue(otherNode, out bool otherReachable) && otherReachable)
                    {
                        reachableNodes.Add(otherNode);
                    }
                }
                
                if (reachableNodes.Count > 0)
                {
                    // Sort by horizontal distance to make connections look better
                    reachableNodes.Sort((a, b) => 
                        Vector2.Distance(node.GridPosition, a.GridPosition).CompareTo(
                        Vector2.Distance(node.GridPosition, b.GridPosition)));
                        
                    // Connect to the closest node
                    node.AddNextStage(reachableNodes[0]);
                    
                    if (m_DebugMode)
                    {
                        Debug.Log($"Connected dead-end node at depth {nodeDepth} to {reachableNodes[0].StageType} " +
                                 $"at depth {Mathf.RoundToInt(reachableNodes[0].GridPosition.y)}");
                    }
                }
                else
                {
                    // No good existing node found, create a new path to the boss
                    StageNode currentNode = node;
                    
                    // Create a path to the boss through new nodes
                    for (int depth = nodeDepth + 1; depth < m_CurrentZoneDefinition.OverallLength - 1; depth++)
                    {
                        // Create a new intermediate node
                        StageType newType = m_CurrentZoneDefinition.GetWeightedRandomStageType();
                        string sceneName = m_CurrentZoneDefinition.GetRandomStageScene(newType);
                        Vector2 newPosition = new Vector2(currentNode.GridPosition.x, depth);
                        
                        StageNode newNode = new StageNode(newType, sceneName, newPosition);
                        m_StageMap.Add(newNode.ID, newNode);
                        
                        // Connect current node to new node
                        currentNode.AddNextStage(newNode);
                        
                        // Update current node for next iteration
                        currentNode = newNode;
                        
                        // Mark this new node as reachable
                        canReachBoss[newNode] = true;
                    }
                    
                    // Connect the last node to the boss
                    currentNode.AddNextStage(m_BossStage);
                    
                    if (m_DebugMode)
                    {
                        Debug.Log($"Created new path from dead-end node at depth {nodeDepth} to boss");
                    }
                }
                
                // Mark this node as now reachable
                canReachBoss[node] = true;
            }
            
            // Final check for any remaining unreachable nodes
            foreach (var node in m_StageMap.Values.ToList())
            {
                if (!canReachBoss.TryGetValue(node, out bool reachable) || !reachable)
                {
                    if (m_DebugMode)
                    {
                        Debug.LogWarning($"Found unreachable node after fixes: {node.StageType} at {node.GridPosition}");
                    }
                    
                    // As a last resort, directly connect to boss if we couldn't fix it
                    if (node.StageType != StageType.Boss && node.StageType != StageType.Interzone)
                    {
                        int nodeDepth = Mathf.RoundToInt(node.GridPosition.y);
                        
                        if (nodeDepth < m_CurrentZoneDefinition.OverallLength - 1)
                        {
                            // Create a chain to the boss
                            StageNode lastNode = node;
                            
                            for (int depth = nodeDepth + 1; depth < m_CurrentZoneDefinition.OverallLength - 1; depth++)
                            {
                                StageType newType = m_CurrentZoneDefinition.GetWeightedRandomStageType();
                                string sceneName = m_CurrentZoneDefinition.GetRandomStageScene(newType);
                                Vector2 newPosition = new Vector2(lastNode.GridPosition.x, depth);
                                
                                StageNode newNode = new StageNode(newType, sceneName, newPosition);
                                m_StageMap.Add(newNode.ID, newNode);
                                
                                lastNode.AddNextStage(newNode);
                                lastNode = newNode;
                            }
                            
                            // Connect to boss
                            lastNode.AddNextStage(m_BossStage);
                        }
                    }
                }
            }
        }
        #endregion

        #region Stage Transitions
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