using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.AI;

namespace DungeonGeneration
{
    /// <summary>
    /// Manages a boss stage with wave-based boss spawning
    /// </summary>
    public class BossStageManager : StageBaseManager
    {
        #region Serialized Fields
        [Header("Boss Spawner")]
        [SerializeField] private WaveSpawner m_WaveSpawner;
        [SerializeField] private bool m_UseSpecificSpawnPoints = true;
        [SerializeField] private Transform[] m_BossSpawnPoints;
        
        [Header("UI")]
        [SerializeField] private GameObject m_IntroUI;
        [SerializeField] private GameObject m_DefeatedUI;
        
        [Header("Audio")]
        [SerializeField] private AudioClip m_BossIntroMusic;
        [SerializeField] private AudioClip m_BossBattleMusic;
        [SerializeField] private AudioClip m_BossDefeatedMusic;
        
        [Header("Events")]
        [SerializeField] private UnityEvent m_OnBossIntro;
        [SerializeField] private UnityEvent m_OnBossBattleStart;
        [SerializeField] private UnityEvent m_OnBossDefeated;
        
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
        private AudioSource m_MusicSource;
        private AudioSource m_SfxSource;
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
        #endregion
        
        #region Unity Lifecycle
        private void Awake()
        {
            // Create audio sources
            m_MusicSource = gameObject.AddComponent<AudioSource>();
            m_MusicSource.loop = true;
            m_MusicSource.volume = 0.7f;
            m_MusicSource.spatialBlend = 0f; // 2D sound
            
            m_SfxSource = gameObject.AddComponent<AudioSource>();
            m_SfxSource.loop = false;
            m_SfxSource.volume = 1.0f;
            m_SfxSource.spatialBlend = 0f; // 2D sound
        }
        
        protected override void Start()
        {
            base.Start();
            
            if (m_DefeatedUI != null)
            {
                m_DefeatedUI.SetActive(false);
            }
            
            // Initialize wave spawner if null
            if (m_WaveSpawner == null)
            {
                m_WaveSpawner = GetComponent<WaveSpawner>();
                if (m_WaveSpawner == null)
                {
                    m_WaveSpawner = gameObject.AddComponent<WaveSpawner>();
                }
            }
            
            // Override wave spawner's spawn position method if we have specific boss spawn points
            if (m_UseSpecificSpawnPoints && m_BossSpawnPoints != null && m_BossSpawnPoints.Length > 0)
            {
                // Connect our custom spawn position handler to the wave spawner
                m_WaveSpawner.GetCustomSpawnPosition = CustomBossSpawnPosition;
            }
            
            // Subscribe to wave spawner events
            if (m_WaveSpawner != null)
            {
                m_WaveSpawner.OnWaveCompleted += () => 
                {
                    OnWaveCompleted?.Invoke();
                    CheckStageCompletion();
                };
                
                m_WaveSpawner.OnWaveStarted += (waveSetIndex, combinationIndex) => 
                {
                    OnWaveStarted?.Invoke(waveSetIndex, combinationIndex);
                };
            }
            
            // Start with the boss intro
            StartCoroutine(BossIntroSequence());
        }
        
        private void OnEnable()
        {
            // Subscribe to enemy events if needed
            if (EnemyManager.Instance != null)
            {
                EnemyManager.Instance.OnEnemyRegistered.AddListener(OnBossRegistered);
                EnemyManager.Instance.OnEnemyUnregistered.AddListener(OnBossUnregistered);
            }
        }
        
        private void OnDisable()
        {
            // Unsubscribe from events
            if (EnemyManager.Instance != null)
            {
                EnemyManager.Instance.OnEnemyRegistered.RemoveListener(OnBossRegistered);
                EnemyManager.Instance.OnEnemyUnregistered.RemoveListener(OnBossUnregistered);
            }
        }
        #endregion
        
        #region Boss Management
        /// <summary>
        /// Handles the boss introduction sequence
        /// </summary>
        private IEnumerator BossIntroSequence()
        {
            // Play intro music
            if (m_MusicSource && m_BossIntroMusic)
            {
                m_MusicSource.clip = m_BossIntroMusic;
                m_MusicSource.Play();
            }
            
            // Show intro UI
            if (m_IntroUI)
            {
                UIManager.Instance?.ShowPage(m_IntroUI.GetComponent<UIPage>());
            }
            
            // Fire intro event
            m_OnBossIntro?.Invoke();
            
            // Wait for intro delay
            yield return new WaitForSeconds(3f);
            
            // Hide intro UI
            if (m_IntroUI)
            {
                UIManager.Instance?.GoBack();
            }
            
            // Start the battle
            StartBossBattle();
        }
        
        /// <summary>
        /// Starts the boss battle
        /// </summary>
        private void StartBossBattle()
        {
            m_StageActive = true;
            
            // Switch to battle music
            if (m_MusicSource != null && m_BossBattleMusic != null)
            {
                m_MusicSource.clip = m_BossBattleMusic;
                m_MusicSource.Play();
            }
            
            // Start spawning bosses
            m_WaveSpawner.StartSpawning();
            
            // Fire event
            m_OnBossBattleStart?.Invoke();
        }
        
        /// <summary>
        /// Called when a boss is registered with the enemy manager
        /// </summary>
        private void OnBossRegistered(BaseEnemy boss)
        {
            if (boss != null && m_StageActive && !m_StageCleared)
            {
                // Set boss name if available
                // if (m_BossNameText != null && boss.name != null)
                // {
                //     m_BossNameText.text = boss.name;
                // }
            }
        }
        
        /// <summary>
        /// Called when a boss is unregistered with the enemy manager
        /// </summary>
        private void OnBossUnregistered(BaseEnemy boss)
        {
            if (boss != null)
            {
                // Check completion in wave spawner event handler
            }
        }
        
        /// <summary>
        /// Called when all bosses are defeated
        /// </summary>
        private void OnBossesDefeated()
        {
            if (m_StageCleared) return;
            
            // Switch to victory music
            if (m_MusicSource != null && m_BossDefeatedMusic != null)
            {
                m_MusicSource.clip = m_BossDefeatedMusic;
                m_MusicSource.Play();
            }
            
            // Show defeated UI
            if (m_DefeatedUI != null)
            {
                m_DefeatedUI.SetActive(true);
            }
            
            // Fire event
            m_OnBossDefeated?.Invoke();
            
            // Mark stage as cleared
            SetStageCleared();
        }
        
        /// <summary>
        /// Mark the stage as cleared and invoke the completion event
        /// </summary>
        private void SetStageCleared()
        {
            m_StageCleared = true;
            
            if (m_DebugMode)
            {
                Debug.Log("Boss stage complete!");
            }
            
            onStageCompleted?.Invoke();
        }
        #endregion
        
        #region Stage Management
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
                    Debug.Log("Manually triggering next boss wave.");
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
        
        /// <summary>
        /// Check if the stage is complete (all bosses defeated)
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
                    OnBossesDefeated();
                }
            }
        }
        
        /// <summary>
        /// Custom spawn position handler for bosses
        /// </summary>
        private Vector3 CustomBossSpawnPosition(int waveSetIndex, int waveIndex, int enemyIndex, GameObject enemyPrefab)
        {
            if (m_UseSpecificSpawnPoints && m_BossSpawnPoints != null && m_BossSpawnPoints.Length > 0)
            {
                // Calculate a unique index based on all three indices
                int bossIndex;
                
                if (m_BossSpawnPoints.Length == 1)
                {
                    // If only one spawn point, use it for everything
                    bossIndex = 0;
                }
                else if (waveSetIndex < m_BossSpawnPoints.Length)
                {
                    // If we have enough spawn points for each phase, use phase-based spawning
                    bossIndex = waveSetIndex;
                }
                else
                {
                    // Otherwise, create a combined index
                    bossIndex = (waveSetIndex * 100 + waveIndex * 10 + enemyIndex) % m_BossSpawnPoints.Length;
                }
                
                if (m_DebugMode)
                {
                    Debug.Log($"Using boss spawn point {bossIndex} for boss in wave set {waveSetIndex}, wave {waveIndex}, enemy {enemyIndex}");
                }
                
                return m_BossSpawnPoints[bossIndex].position;
            }
            
            // Return Vector3.zero to fall back to the default spawning
            return Vector3.zero;
        }
        #endregion
    }
} 