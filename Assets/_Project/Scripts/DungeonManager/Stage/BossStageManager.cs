using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

namespace DungeonGeneration
{
    /// <summary>
    /// Manages a boss stage with phases and custom mechanics
    /// </summary>
    public class BossStageManager : StageBaseManager
    {
        #region Serialized Fields
        [Header("Boss Configuration")]
        [SerializeField] private GameObject m_BossPrefab;
        [SerializeField] private Transform m_BossSpawnPoint;
        [SerializeField] private string m_BossName = "Boss";
        [SerializeField] private float m_BossIntroDelay = 3f;
        [SerializeField] private int m_NumPhases = 3;
        [SerializeField] private bool m_HasPhaseTransitions = true;
        
        [Header("Arena Settings")]
        [SerializeField] private GameObject[] m_ArenaElements;
        [SerializeField] private GameObject[] m_ArenaPhaseTwoElements;
        [SerializeField] private GameObject[] m_ArenaPhaseThreeElements;
        [SerializeField] private Light m_ArenaLight;
        [SerializeField] private Color m_PhaseOneColor = Color.white;
        [SerializeField] private Color m_PhaseTwoColor = new Color(1f, 0.5f, 0f);
        [SerializeField] private Color m_PhaseThreeColor = Color.red;
        
        [Header("UI")]
        [SerializeField] private GameObject m_BossUI;
        [SerializeField] private TextMeshProUGUI m_BossNameText;
        [SerializeField] private GameObject m_HealthBarObject;
        [SerializeField] private RectTransform m_HealthBarFill;
        [SerializeField] private TextMeshProUGUI m_PhaseText;
        [SerializeField] private GameObject m_IntroUI;
        [SerializeField] private GameObject m_DefeatedUI;
        
        [Header("Audio")]
        [SerializeField] private AudioClip m_BossIntroMusic;
        [SerializeField] private AudioClip m_BossBattleMusic;
        [SerializeField] private AudioClip m_BossDefeatedMusic;
        [SerializeField] private AudioClip m_PhaseTransitionSound;
        
        [Header("Events")]
        [SerializeField] private UnityEvent m_OnBossIntro;
        [SerializeField] private UnityEvent m_OnBossBattleStart;
        [SerializeField] private UnityEvent m_OnPhaseChange;
        [SerializeField] private UnityEvent m_OnBossDefeated;
        #endregion
        
        #region Private Fields
        private GameObject m_BossInstance;
        private BaseEnemy m_BossEnemy;
        private int m_CurrentPhase = 0;
        private bool m_IsBattleActive = false;
        private bool m_IsBossDefeated = false;
        private AudioSource m_MusicSource;
        private AudioSource m_SfxSource;
        private float[] m_PhaseThresholds;
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
            
            // Calculate phase thresholds
            m_PhaseThresholds = new float[m_NumPhases];
            for (int i = 0; i < m_NumPhases; i++)
            {
                m_PhaseThresholds[i] = 1.0f - ((float)(i + 1) / m_NumPhases);
            }
        }
        
        private void Start()
        {
            // Hide boss UI initially
            if (m_BossUI != null)
            {
                m_BossUI.SetActive(false);
            }
            
            if (m_DefeatedUI != null)
            {
                m_DefeatedUI.SetActive(false);
            }
            
            // Start the boss intro sequence
            StartCoroutine(BossIntroSequence());
        }
        
        private void Update()
        {
            if (!m_IsBattleActive || m_IsBossDefeated || m_BossInstance == null)
                return;
            
            // Check for phase transition
            CheckPhaseTransition();
        }
        #endregion
        
        #region Boss Sequences
        /// <summary>
        /// Handles the boss introduction sequence
        /// </summary>
        private IEnumerator BossIntroSequence()
        {
            // Play intro music
            if (m_MusicSource != null && m_BossIntroMusic != null)
            {
                m_MusicSource.clip = m_BossIntroMusic;
                m_MusicSource.Play();
            }
            
            // Show intro UI
            if (m_IntroUI != null)
            {
                m_IntroUI.SetActive(true);
            }
            
            // Fire intro event
            m_OnBossIntro?.Invoke();
            
            // Wait for intro delay
            yield return new WaitForSeconds(m_BossIntroDelay);
            
            // Hide intro UI
            if (m_IntroUI != null)
            {
                m_IntroUI.SetActive(false);
            }
            
            // Spawn the boss
            SpawnBoss();
            
            // Wait a moment for dramatic effect
            yield return new WaitForSeconds(1.5f);
            
            // Start the battle
            StartBossBattle();
        }
        
        /// <summary>
        /// Spawns the boss
        /// </summary>
        private void SpawnBoss()
        {
            if (m_BossPrefab == null || m_BossSpawnPoint == null)
            {
                Debug.LogError("Boss prefab or spawn point not set!");
                return;
            }
            
            // Spawn the boss
            m_BossInstance = Instantiate(m_BossPrefab, m_BossSpawnPoint.position, m_BossSpawnPoint.rotation);
            
            // Get components
            m_BossEnemy = m_BossInstance.GetComponent<BaseEnemy>();
            
            // Subscribe to death event
            // m_BossHealth.OnDeath += OnBossDeath;
            
            // Set boss name in UI
            if (m_BossNameText != null)
            {
                m_BossNameText.text = m_BossName;
            }
        }
        
        /// <summary>
        /// Starts the boss battle
        /// </summary>
        private void StartBossBattle()
        {
            m_IsBattleActive = true;
            m_CurrentPhase = 1;
            
            // Update phase text
            if (m_PhaseText != null)
            {
                m_PhaseText.text = $"Phase {m_CurrentPhase}";
            }
            
            // Show boss UI
            if (m_BossUI != null)
            {
                m_BossUI.SetActive(true);
            }
            
            // Switch to battle music
            if (m_MusicSource != null && m_BossBattleMusic != null)
            {
                m_MusicSource.clip = m_BossBattleMusic;
                m_MusicSource.Play();
            }
            
            // Set arena light to phase one color
            if (m_ArenaLight != null)
            {
                m_ArenaLight.color = m_PhaseOneColor;
            }
            
            // Fire event
            m_OnBossBattleStart?.Invoke();
        }
        
        /// <summary>
        /// Handles boss defeat
        /// </summary>
        private void OnBossDeath()
        {
            if (m_IsBossDefeated)
                return;
                
            m_IsBossDefeated = true;
            m_IsBattleActive = false;
            
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
            
            // Create exit portal after delay
            StartCoroutine(CreateExitPortalDelayed(5f));
        }
        #endregion
        
        #region Phase Management
        /// <summary>
        /// Checks if it's time to transition to the next phase
        /// </summary>
        private void CheckPhaseTransition()
        {
            // if (m_BossHealth == null || !m_HasPhaseTransitions || m_CurrentPhase >= m_NumPhases)
            //     return;
            //     
            // float healthPercentage = m_BossHealth.CurrentHealth / m_BossHealth.MaxHealth;

            if (!m_HasPhaseTransitions || m_CurrentPhase >= m_NumPhases)
                return;
            
            // float healthPercentage = m_BossHealth.CurrentHealth / m_BossHealth.MaxHealth;
            float healthPercentage = 0.4f;
            
            // Check if health has dropped below the next phase threshold
            if (healthPercentage <= m_PhaseThresholds[m_CurrentPhase - 1])
            {
                TransitionToNextPhase();
            }
        }
        
        /// <summary>
        /// Transitions to the next phase
        /// </summary>
        private void TransitionToNextPhase()
        {
            m_CurrentPhase++;
            
            // Play phase transition sound
            if (m_SfxSource != null && m_PhaseTransitionSound != null)
            {
                m_SfxSource.PlayOneShot(m_PhaseTransitionSound);
            }
            
            // Update phase text
            if (m_PhaseText != null)
            {
                m_PhaseText.text = $"Phase {m_CurrentPhase}";
            }
            
            // Update arena elements based on phase
            UpdateArenaForPhase();
            
            // Fire event
            m_OnPhaseChange?.Invoke();
            
            // Optional: Make boss temporarily invulnerable during transition
            // if (m_BossHealth != null)
            // {
            //     StartCoroutine(TemporaryInvulnerability(2f));
            // }
        }
        
        /// <summary>
        /// Updates the arena based on the current phase
        /// </summary>
        private void UpdateArenaForPhase()
        {
            switch (m_CurrentPhase)
            {
                case 2:
                    // Update light color
                    if (m_ArenaLight != null)
                    {
                        m_ArenaLight.color = m_PhaseTwoColor;
                    }
                    
                    // Activate phase two elements
                    foreach (GameObject element in m_ArenaPhaseTwoElements)
                    {
                        if (element != null)
                        {
                            element.SetActive(true);
                        }
                    }
                    break;
                    
                case 3:
                    // Update light color
                    if (m_ArenaLight != null)
                    {
                        m_ArenaLight.color = m_PhaseThreeColor;
                    }
                    
                    // Activate phase three elements
                    foreach (GameObject element in m_ArenaPhaseThreeElements)
                    {
                        if (element != null)
                        {
                            element.SetActive(true);
                        }
                    }
                    break;
            }
        }
        
        /// <summary>
        /// Makes the boss temporarily invulnerable during phase transitions
        /// </summary>
        private IEnumerator TemporaryInvulnerability(float _duration)
        {
            // if (m_BossHealth != null)
            // {
            //     m_BossHealth.SetInvulnerable(true);
            // }
            //
            // yield return new WaitForSeconds(_duration);
            //
            // if (m_BossHealth != null)
            // {
            //     m_BossHealth.SetInvulnerable(false);
            // }
            yield return null;
        }
        #endregion
        
        #region Exit Handling
        /// <summary>
        /// Creates the exit portal after a delay
        /// </summary>
        private IEnumerator CreateExitPortalDelayed(float _delay)
        {
            yield return new WaitForSeconds(_delay);
            CreateExitPortal();
        }
        
        /// <summary>
        /// Creates the exit portal to the interzone/next zone
        /// </summary>
        private void CreateExitPortal()
        {
            // This would typically notify the DungeonManager to create a portal to the interzone
            Debug.Log("Creating exit portal to interzone");
            
            // In a real implementation, this might look like:
            // DungeonManager.Instance.TransitionToNextZone();
        }
        #endregion
    }
} 