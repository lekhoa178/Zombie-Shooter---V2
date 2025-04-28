using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGeneration
{
    /// <summary>
    /// Represents a single stage node in the procedural dungeon structure
    /// </summary>
    [Serializable]
    public class StageNode
    {
        #region Public Properties
        public string ID => m_ID;
        public StageType StageType => m_StageType;
        public string SceneName => m_SceneName;
        public Vector2 GridPosition => m_GridPosition;
        public List<StageNode> NextStages => m_NextStages;
        public bool IsGenerated => m_IsGenerated;
        public bool IsVisited => m_IsVisited;
        public string Name { get; set; }

        #endregion

        #region Private Fields
        [SerializeField] private string m_ID;
        [SerializeField] private StageType m_StageType;
        [SerializeField] private string m_SceneName;
        [SerializeField] private Vector2 m_GridPosition;
        [SerializeField] private List<StageNode> m_NextStages = new List<StageNode>();
        [SerializeField] private bool m_IsGenerated;
        [SerializeField] private bool m_IsVisited;

        #endregion

        /// <summary>
        /// Creates a new stage node
        /// </summary>
        public StageNode(StageType stageType, string sceneName, Vector2 gridPosition)
        {
            m_ID = Guid.NewGuid().ToString();
            m_StageType = stageType;
            m_SceneName = sceneName;
            m_GridPosition = gridPosition;
            m_IsGenerated = false;
            m_IsVisited = false;
        }

        /// <summary>
        /// Adds a next stage to this node's connections
        /// </summary>
        public void AddNextStage(StageNode nextStage)
        {
            if (!m_NextStages.Contains(nextStage))
            {
                m_NextStages.Add(nextStage);
            }
        }
        
        /// <summary>
        /// Removes a next stage from this node's connections
        /// </summary>
        /// <param name="nextStage"></param>
        public void RemoveNextStage(StageNode nextStage)
        {
            if (m_NextStages.Contains(nextStage))
            {
                m_NextStages.Remove(nextStage);
            }
        }

        /// <summary>
        /// Marks this stage as generated
        /// </summary>
        public void MarkAsGenerated()
        {
            m_IsGenerated = true;
        }

        /// <summary>
        /// Marks this stage as visited
        /// </summary>
        public void MarkAsVisited()
        {
            m_IsVisited = true;
        }

        /// <summary>
        /// Marks this stage as unvisited
        /// </summary>
        public void MarkAsUnvisited()
        {
            m_IsVisited = false;
        }

        /// <summary>
        /// Resolves a Mystery stage type to a concrete stage type
        /// </summary>
        public void ResolveMysteryStage(ZoneDefinition zoneDefinition)
        {
            if (m_StageType == StageType.Mystery)
            {
                m_StageType = zoneDefinition.ResolveMysteryStage();
                m_SceneName = zoneDefinition.GetRandomStageScene(m_StageType);
            }
        }
        
        /// <summary>
        /// Sets the grid position of this stage node
        /// </summary>
        /// <param name="gridPosition"></param>
        public void SetGridPosition(Vector2 gridPosition)
        {
            m_GridPosition = gridPosition;
        }
    }
} 