using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace DungeonGeneration
{
    /// <summary>
    /// Manages a combat stage with wave-based enemy spawning
    /// </summary>
    public class CombatStageManager : StageBaseManager
    {
        #region Serialized Fields
        [Header("Wave Spawner")]
        [SerializeField] private WaveSpawner m_WaveSpawner;
        [Header("Debug")]
        [SerializeField] private bool m_DebugMode = false;
        #endregion
        
        #region Events
        public event Action OnWaveCompleted;
        public event Action<int, int> OnWaveStarted; // WaveSetIndex, CombinationIndex
        #endregion
        
        #region Private Fields
        private bool m_StageCleared = false;
        private bool m_StageActive = false;
        #endregion
        
        #region Properties
        /// <summary>
        /// Returns true if the current wave is complete and waiting for the next wave to be triggered
        /// </summary>
        public bool IsReadyForNextWave => m_StageActive && !m_StageCleared && m_WaveSpawner != null && m_WaveSpawner.IsReadyForNextWave;
        
        /// <summary>
        /// Returns true if stage is completely finished
        /// </summary>
        public bool IsStageCleared => m_StageCleared;
        
        /// <summary>
        /// Current wave progression mode
        /// </summary>
        public WaveProgressionMode ProgressionMode
        {
            get => m_WaveSpawner != null ? m_WaveSpawner.ProgressionMode : WaveProgressionMode.EnemyBased;
            set
            {
                if (m_WaveSpawner != null)
                {
                    m_WaveSpawner.ProgressionMode = value;
                }
            }
        }
        
        /// <summary>
        /// Time remaining until next automatic wave spawn in TimeBased mode
        /// </summary>
        public float TimeUntilNextWave => m_WaveSpawner != null ? m_WaveSpawner.TimeUntilNextWave : 0f;
        #endregion
        
        #region Unity Lifecycle
        protected override void Start()
        {
            base.Start();
            
            // Initialize wave spawner if null
            if (m_WaveSpawner == null)
            {
                m_WaveSpawner = GetComponent<WaveSpawner>();
                if (m_WaveSpawner == null)
                {
                    m_WaveSpawner = gameObject.AddComponent<WaveSpawner>();
                }
            }
            
            // Subscribe to wave spawner events
            if (m_WaveSpawner != null)
            {
                m_WaveSpawner.OnWaveCompleted += () => 
                {
                    OnWaveCompleted?.Invoke();
                };
                
                m_WaveSpawner.OnWaveStarted += (waveSetIndex, combinationIndex) => 
                {
                    OnWaveStarted?.Invoke(waveSetIndex, combinationIndex);
                };
                
                m_WaveSpawner.OnAllEnemyDefeated += CheckStageCompletion;
            }
            
            // Start combat
            m_StageActive = true;
            m_WaveSpawner.StartSpawning();
        }
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Manually trigger the next wave (only works when in Manual mode or when no wave is active)
        /// </summary>
        public void TriggerNextWave()
        {
            if (m_WaveSpawner != null && !m_StageCleared)
            {
                m_WaveSpawner.TriggerNextWave();
                
                if (m_DebugMode)
                {
                    Debug.Log("Manually triggering next wave set.");
                }
            }
        }
        
        /// <summary>
        /// Set wave progression mode
        /// </summary>
        public void SetProgressionMode(WaveProgressionMode mode)
        {
            ProgressionMode = mode;
        }
        
        /// <summary>
        /// Set the time interval for time-based progression mode
        /// </summary>
        public void SetTimeBasedInterval(float seconds)
        {
            if (seconds > 0 && m_WaveSpawner != null)
            {
                m_WaveSpawner.SetTimeBasedInterval(seconds);
            }
        }
        #endregion
        
        #region Private Methods
        /// <summary>
        /// Check if the stage is complete (all waves spawned and all enemies defeated)
        /// </summary>
        private void CheckStageCompletion()
        {
            if (m_WaveSpawner != null && m_WaveSpawner.IsReadyForNextWave && !m_StageCleared)
            {
                // Check if the current wave set index is beyond the available wave sets
                var selectedCombination = m_WaveSpawner.SelectedCombination;
                if (selectedCombination == null || m_WaveSpawner.IsReadyForNextWave)
                {
                    // All wave sets completed and all enemies defeated
                    OnCombatComplete();
                }
            }
        }
        
        /// <summary>
        /// Called when all waves have been completed
        /// </summary>
        private void OnCombatComplete()
        {
            if (m_StageCleared) return;
            
            m_StageCleared = true;
            
            if (m_DebugMode)
            {
                Debug.Log("Combat stage complete!");
            }
            
            // Stop the wave spawner
            if (m_WaveSpawner != null)
            {
                m_WaveSpawner.StopSpawning();
            }
            
            onStageCompleted?.Invoke();
        }
        #endregion
    }
}