using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityUtils;

namespace DungeonGeneration
{
    /// <summary>
    /// Main manager class for the procedural dungeon generation system
    /// </summary>
    public class DungeonManager : PersistentSingleton<DungeonManager>
    {
        #region Singleton
        protected override void Awake()
        {
            base.Awake();
            
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
        private DungeonMapUI m_DungeonMapUI;
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
        // private void Start()
        // {
        //     m_CurrentZoneDefinition = GetZoneDefinitionAtIndex(m_ZoneIndex);
        //     
        //     if (m_CurrentZoneDefinition == null)
        //     {
        //         Debug.LogError("No Zone Definition assigned to DungeonManager!");
        //         return;
        //     }
        //     
        //     StartCoroutine(GenerateDungeon());
        // }
        
        public void StartDungeonGeneration()
        {
            m_ZoneIndex = 0;
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
            
            // Check for shared paths when not allowed
            // if (!m_CurrentZoneDefinition.AllowSharedPaths)
            // {
            //    FixSharedPaths();
            // }
            //
            // // Validate that all nodes have a path to the boss
            // FixDeadEndNodes();
            
            // Set up portals for the initial stage
            CreateStageExits();
            
            m_IsGenerating = false;
            
            if (m_DebugMode)
            {
                DebugPrintDungeonStructure();
            }
        }

        private void GenerateStagesRecursive(StageNode parentStage, int depth)
        {
            // Only the last layer before boss (exactly OverallLength - 1) should connect to boss
            var isLastLayerBeforeBoss = depth == m_CurrentZoneDefinition.OverallLength - 1;

            if (isLastLayerBeforeBoss)
            {
                parentStage.AddNextStage(m_BossStage);
                return;
            }

            // If we've somehow gone beyond the last layer, return (shouldn't happen)
            if (depth >= m_CurrentZoneDefinition.OverallLength)
            {
                RecursiveRemoveStage(parentStage);
                return;
            }

            // Track nodes at each depth to limit their count
            m_DepthNodeCounters.TryAdd(depth, 0);

            // Determine maximum number of branches based on limits
            var maxNodesAtDepth = m_CurrentZoneDefinition.MaxNodesPerDepth;
            var nodesLeftAtDepth = maxNodesAtDepth - m_DepthNodeCounters[depth];
            var maxBranches = Mathf.Min(m_CurrentZoneDefinition.MaxBranchingFactor, nodesLeftAtDepth);

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
                        .Take(1) // Just take one existing node
                        .ToList();

                    if (existingNodesAtDepth.Count > 0)
                    {
                        parentStage.AddNextStage(existingNodesAtDepth[0]);
                    }
                    else
                    {
                        if (m_CurrentZoneDefinition.AllowSkipDepth)
                        {
                            GenerateStagesRecursive(parentStage, depth + 1);
                        }
                        else
                        {
                            RecursiveRemoveStage(parentStage);
                        }
                    }
                }
                else
                {
                    if (m_CurrentZoneDefinition.AllowSkipDepth)
                    {
                        GenerateStagesRecursive(parentStage, depth + 1);
                    }
                    else
                    {
                        RecursiveRemoveStage(parentStage);
                    }
                }

                return;
            }

            var branches = m_CurrentZoneDefinition.GetRandomBranchingFactor();
            branches = Mathf.Min(branches, maxBranches);

            // Keep track of stages created in this recursion to prevent duplicates
            var createdStages = new HashSet<StageNode>();
            
            // Get initial occupied positions at this depth
            var occupiedPositions = GetOccupiedPositionsAtDepth(depth);
            // Create a local copy to track positions created within this recursion
            var locallyOccupiedPositions = new HashSet<float>(occupiedPositions);
            
            for (var i = 0; i < branches; i++)
            {
                // var stageType = m_CurrentZoneDefinition.GetWeightedRandomStageType();
                var stageType = StageType.Combat;

                // Get scene name based on stage type
                var sceneName = m_CurrentZoneDefinition.GetRandomStageScene(stageType);

                var xOffset = (i - (branches - 1) / 2f) * 2f;
                Vector2 position;
                
                // Check if position is available (considering both existing and locally created positions)
                if (IsPositionAvailable(xOffset, locallyOccupiedPositions))
                {
                    position = new Vector2(xOffset, depth);
                }
                else
                {
                    // Adjust position to avoid overlap with both existing and newly created positions
                    var desiredPositions = new List<float> { xOffset };
                    var adjustedPositions = AdjustPositionsToAvoidOverlap(desiredPositions, locallyOccupiedPositions);
                    position = new Vector2(adjustedPositions[0], depth);
                }
                
                locallyOccupiedPositions.Add(position.x);
                
                StageNode nextStage = null;

                if (m_CurrentZoneDefinition.AllowSharedPaths)
                {
                    var existingNodesAtDepth = m_StageMap.Values
                        .Where(s => Mathf.RoundToInt(s.GridPosition.y) == depth &&
                                    !parentStage.NextStages.Contains(s) &&
                                    !createdStages.Contains(s))
                        .OrderBy(s => Random.value)
                        .Take(1)
                        .ToList();

                    if (existingNodesAtDepth.Count > 0)
                    {
                        nextStage = existingNodesAtDepth[0];
                        createdStages.Add(nextStage);
                    }
                }

                // If no existing stage was found or if shared paths are not allowed, create a new one
                if (nextStage == null)
                {
                    nextStage = new StageNode(stageType, sceneName, position);
                    m_StageMap.Add(nextStage.ID, nextStage);

                    m_DepthNodeCounters[depth]++;

                    createdStages.Add(nextStage);
                }

                parentStage.AddNextStage(nextStage);

                if (depth < m_CurrentZoneDefinition.OverallLength - 1)
                {
                    GenerateStagesRecursive(nextStage, depth + 1);
                }
            }
        }
        
        private HashSet<float> GetOccupiedPositionsAtDepth(int depth)
        {
            var occupied = new HashSet<float>();
            foreach (var node in m_StageMap.Values.Where(node => Mathf.RoundToInt(node.GridPosition.y) == depth))
            {
                occupied.Add(Mathf.RoundToInt(node.GridPosition.x));
            }
            return occupied;
        }

        /// <summary>
        /// Checks if a position is available at a given depth
        /// </summary>
        private bool IsPositionAvailable(float x, HashSet<float> occupiedPositions)
        {
            var roundedX = Mathf.RoundToInt(x);
            
            return occupiedPositions.Select(Mathf.RoundToInt).All(roundedPosition => roundedX != roundedPosition);
        }

        /// <summary>
        /// Adjusts positions to avoid overlap with existing nodes
        /// </summary>
        private List<float> AdjustPositionsToAvoidOverlap(List<float> desiredPositions, HashSet<float> occupiedPositions)
        {
            const float minSpacing = 1.0f; // Minimum spacing between nodes
            var result = new List<float>();
            
            foreach (var desiredX in desiredPositions)
            {
                var adjustedX = desiredX;
                bool positionFound = false;
  
                for (float offset = minSpacing; !positionFound && offset <= 15f; offset += minSpacing)
                {
                    // Try right side
                    adjustedX = desiredX + offset;
                    if (IsPositionAvailable(adjustedX, occupiedPositions))
                    {
                        positionFound = true;
                        break;
                    }

                    // Try left side
                    adjustedX = desiredX - offset;
                    if (IsPositionAvailable(adjustedX, occupiedPositions))
                    {
                        positionFound = true;
                        break;
                    }
                }

                result.Add(adjustedX);
                occupiedPositions.Add(adjustedX);
            }
            
            return result;
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
                
                var displayName = GetStageDisplayName(nextStage.StageType);
                nextStage.Name = displayName;
                
                // Spawn the portal
                spawner.SpawnPortal(nextStage.ID, nextStage.SceneName, displayName, nextStage.StageType);
                
                yield return null;
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
        /// Recursively removes a stage and its entire path from the dungeon structure
        /// </summary>
        private void RecursiveRemoveStage(StageNode stage)
        {
            // Don't remove special stages
            if (stage == m_RootStage || stage == m_BossStage || stage == m_InterzoneStage)
            {
                return;
            }

            // Process all children recursively first (bottom-up approach)
            foreach (var childStage in stage.NextStages.ToList())
            {
                // Check if child has other parents
                var childParents = FindParents(childStage);
                if (childParents.Count <= 1)
                {
                    // Child only has this node as parent, recursively remove it
                    RecursiveRemoveStage(childStage);
                }
                else
                {
                    // Child has other parents, just disconnect it
                    stage.RemoveNextStage(childStage);
                }
            }

            // Find all parents of this node
            var parentStages = FindParents(stage);
    
            // Remove this node from all parents
            foreach (var parent in parentStages)
            {
                parent.RemoveNextStage(stage);
            }
    
            // Remove from stage map
            m_StageMap.Remove(stage.ID);
    
            // Decrement node counter for this depth
            int stageDepth = Mathf.RoundToInt(stage.GridPosition.y);
            if (m_DepthNodeCounters.ContainsKey(stageDepth))
            {
                m_DepthNodeCounters[stageDepth]--;
            }
    
            // Now recursively process parents that have no remaining children (going upward)
            foreach (var parent in parentStages)
            {
                if (parent.NextStages.Count == 0 && 
                    parent != m_RootStage && 
                    parent != m_BossStage && 
                    parent != m_InterzoneStage)
                {
                    // Parent has no more children, recursively remove it too
                    RecursiveRemoveStage(parent);
                }
            }
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
                SceneTransitionManager.Instance.LoadScene(targetStage.SceneName);
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

        /// <summary>
        /// Gets the stage icon for a specific stage type
        /// </summary>
        public Sprite GetStageIcon(StageType stageType)
        {
            if (!m_DungeonMapUI)
            {
                m_DungeonMapUI = FindAnyObjectByType<DungeonMapUI>(FindObjectsInactive.Include);
            }
            
            if (m_DungeonMapUI)
            {
                return m_DungeonMapUI.GetStageIcon(stageType);
            }
            
            // If there's no DungeonMapUI instance active, return null
            Debug.LogWarning("DungeonManager: Cannot get stage icon - DungeonMapUI not available");
            return null;
        }
        #endregion
    }
} 