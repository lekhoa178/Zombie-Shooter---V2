using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace DungeonGeneration
{
    /// <summary>
    /// ScriptableObject that defines the configuration for a single dungeon zone
    /// </summary>
    [CreateAssetMenu(fileName = "NewZoneDefinition", menuName = "Dungeon/Zone Definition")]
    public class ZoneDefinition : ScriptableObject
    {
        #region Public Properties
        public string ZoneName => m_ZoneName;
        public int MaxBranchingFactor => m_BranchingFactors.Count > 0 ? m_BranchingFactors.Max(bf => bf.branchCount) : 1;
        public int OverallLength => m_OverallLength;
        public string BossStageName => m_BossStageName;
        public int MaxNodesPerDepth => m_MaxNodesPerDepth;
        public bool AllowSharedPaths => m_AllowSharedPaths;
        public bool AllowSkipDepth => m_AllowSkipDepth;
        #endregion

        #region Serialized Fields
        [Header("Zone Settings")]
        [SerializeField, Tooltip("The name of this dungeon zone")]
        private string m_ZoneName;

        [System.Serializable]
        public class BranchingFactorWeight
        {
            public int branchCount = 2;
            [Range(0f, 1f)]
            public float weight = 1f;
        }

        [SerializeField, Tooltip("Probability weights for different branching factors")]
        private List<BranchingFactorWeight> m_BranchingFactors = new List<BranchingFactorWeight>
        {
            new BranchingFactorWeight { branchCount = 2, weight = 1f }
        };

        [SerializeField, Tooltip("Total number of stages before the boss"), Range(3, 20)]
        private int m_OverallLength = 5;
        
        [SerializeField, Tooltip("Maximum number of nodes at any depth level"), Range(1, 8)]
        private int m_MaxNodesPerDepth = 3;
        
        [SerializeField, Tooltip("Allow different branch nodes to lead to the same destination (except for boss)")]
        private bool m_AllowSharedPaths = false;

        [SerializeField, Tooltip("Allow skipping of depth levels (e.g., from 1 to 3)")]
        private bool m_AllowSkipDepth = false;

        [SerializeField, Tooltip("The scene name for the boss stage")]
        private string m_BossStageName;

        [Header("Stage Pools")]
        [SerializeField, Tooltip("List of potential combat stage scene names")]
        private List<string> m_CombatStages = new List<string>();

        [SerializeField, Tooltip("List of potential challenge stage scene names")]
        private List<string> m_ChallengeStages = new List<string>();

        [SerializeField, Tooltip("The scene name for the shop stage")]
        private string m_ShopStageName;

        [SerializeField, Tooltip("The scene name for the healing stage")]
        private string m_HealingStageName;

        [SerializeField, Tooltip("The scene name for the interzone stage")]
        private string m_InterzoneStageName;

        [Header("Weights")]
        [SerializeField, Tooltip("Probability of generating a combat stage"), Range(0f, 1f)]
        private float m_CombatStageWeight = 0.6f;

        [SerializeField, Tooltip("Probability of generating a challenge stage"), Range(0f, 1f)]
        private float m_ChallengeStageWeight = 0.2f;

        [SerializeField, Tooltip("Probability of generating a shop stage"), Range(0f, 1f)]
        private float m_ShopStageWeight = 0.1f;

        [SerializeField, Tooltip("Probability of generating a healing stage"), Range(0f, 1f)]
        private float m_HealingStageWeight = 0.1f;

        [SerializeField, Tooltip("Probability of generating a mystery stage"), Range(0f, 1f)]
        private float m_MysteryStageWeight = 0.2f;
        #endregion

        #region Public Methods
        /// <summary>
        /// Gets a random branching factor based on configured weights
        /// </summary>
        public int GetRandomBranchingFactor()
        {
            // Default to 1 if no branching factors are configured
            if (m_BranchingFactors == null || m_BranchingFactors.Count == 0)
                return 1;
            
            // If only one option, return it directly
            if (m_BranchingFactors.Count == 1)
                return m_BranchingFactors[0].branchCount;
            
            // Calculate total weight
            float totalWeight = m_BranchingFactors.Sum(bf => bf.weight);
            if (totalWeight <= 0)
                return 1;  // Default if all weights are 0
            
            // Select random value based on weights
            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;
            
            foreach (var branchFactor in m_BranchingFactors)
            {
                currentWeight += branchFactor.weight;
                if (randomValue <= currentWeight)
                    return branchFactor.branchCount;
            }
            
            // Fallback to last item if something went wrong
            return m_BranchingFactors[m_BranchingFactors.Count - 1].branchCount;
        }

        /// <summary>
        /// Gets a random stage scene name based on the specified stage type
        /// </summary>
        public string GetRandomStageScene(StageType stageType)
        {
            switch (stageType)
            {
                case StageType.Combat:
                    return m_CombatStages.Count > 0 ? m_CombatStages[Random.Range(0, m_CombatStages.Count)] : string.Empty;
                case StageType.Challenge:
                    return m_ChallengeStages.Count > 0 ? m_ChallengeStages[Random.Range(0, m_ChallengeStages.Count)] : string.Empty;
                case StageType.Boss:
                    return m_BossStageName;
                case StageType.Shop:
                    return m_ShopStageName;
                case StageType.Healing:
                    return m_HealingStageName;
                case StageType.Interzone:
                    return m_InterzoneStageName;
                case StageType.Mystery:
                    // Mystery will be resolved at runtime, so we don't return a scene yet
                    return string.Empty;
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Gets a weighted random stage type based on the configured probabilities
        /// </summary>
        public StageType GetWeightedRandomStageType()
        {
            float totalWeight = m_CombatStageWeight + m_ChallengeStageWeight + m_ShopStageWeight + m_HealingStageWeight + m_MysteryStageWeight;
            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            currentWeight += m_CombatStageWeight;
            if (randomValue <= currentWeight)
                return StageType.Combat;

            currentWeight += m_ChallengeStageWeight;
            if (randomValue <= currentWeight)
                return StageType.Challenge;

            currentWeight += m_ShopStageWeight;
            if (randomValue <= currentWeight)
                return StageType.Shop;

            currentWeight += m_HealingStageWeight;
            if (randomValue <= currentWeight)
                return StageType.Healing;

            return StageType.Mystery;
        }

        /// <summary>
        /// Resolves a mystery stage to a concrete stage type
        /// </summary>
        public StageType ResolveMysteryStage()
        {
            // Mystery can resolve to Combat, Challenge, Shop, or Healing (never Boss)
            float totalWeight = m_CombatStageWeight + m_ChallengeStageWeight + m_ShopStageWeight + m_HealingStageWeight;
            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            currentWeight += m_CombatStageWeight;
            if (randomValue <= currentWeight)
                return StageType.Combat;

            currentWeight += m_ChallengeStageWeight;
            if (randomValue <= currentWeight)
                return StageType.Challenge;

            currentWeight += m_ShopStageWeight;
            if (randomValue <= currentWeight)
                return StageType.Shop;

            return StageType.Healing;
        }
        #endregion
    }
} 