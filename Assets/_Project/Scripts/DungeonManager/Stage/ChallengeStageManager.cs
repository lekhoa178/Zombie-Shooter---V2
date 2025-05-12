using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.AI;

namespace DungeonGeneration
{
    /// <summary>
    /// Manages a challenge stage with various types of challenges
    /// </summary>
    public class ChallengeStageManager : StageBaseManager
    {
        #region Serialized Fields
        [Header("Challenge Configuration")]
        [SerializeField] private ChallengeType m_ChallengeType = ChallengeType.TimeTrial;
        [SerializeField] private string m_ChallengeName = "Challenge";
        [SerializeField] private string m_ChallengeDescription = "Complete the challenge to receive a reward.";
        [SerializeField] private bool m_OptionalChallenge = true;
        
        [Header("Time Trial Settings")]
        [SerializeField] private float m_TimeLimit = 60f;
        [SerializeField] private int m_TargetKills = 10;
        [SerializeField] private int m_TargetScore = 1000;
        [SerializeField] private WaveSpawner m_WaveSpawner;
        
        [Header("Puzzle Settings")]
        [SerializeField] private int m_PuzzleSteps = 3;
        [SerializeField] private float m_PuzzleTimeLimit = 120f;
        
        [Header("Obstacle Course Settings")]
        [SerializeField] private Transform m_StartPoint;
        [SerializeField] private Transform m_EndPoint;
        [SerializeField] private float m_CourseTimeLimit = 90f;
        
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI m_TitleText;
        [SerializeField] private TextMeshProUGUI m_DescriptionText;
        [SerializeField] private TextMeshProUGUI m_TimerText;
        [SerializeField] private TextMeshProUGUI m_ProgressText;
        [SerializeField] private GameObject m_ChallengeUI;
        [SerializeField] private GameObject m_CompleteUI;
        [SerializeField] private GameObject m_FailUI;
        
        [Header("Rewards")]
        [SerializeField] private GameObject m_RewardPrefab;
        [SerializeField] private Transform m_RewardSpawnPoint;
        
        [Header("Events")]
        [SerializeField] private UnityEvent m_OnChallengeAccepted;
        [SerializeField] private UnityEvent m_OnChallengeCompleted;
        [SerializeField] private UnityEvent m_OnChallengeFailed;
        [SerializeField] private UnityEvent m_OnChallengeSkipped;
        #endregion
        
        #region Private Fields
        private bool m_IsChallengeActive = false;
        private bool m_IsChallengeComplete = false;
        private float m_RemainingTime = 0f;
        private int m_CurrentProgress = 0;
        private int m_CurrentScore = 0;
        private Coroutine m_ChallengeCoroutine;
        private List<BaseEnemy> m_ActiveEnemies = new List<BaseEnemy>();
        
        private bool shouldUpdateTimer = true;
        #endregion
        
        #region Unity Lifecycle
        protected override void Start()
        {
            base.Start();
            
            // Initialize UI
            if (m_TitleText != null)
            {
                m_TitleText.text = m_ChallengeName;
            }
            
            if (m_DescriptionText != null)
            {
                m_DescriptionText.text = m_ChallengeDescription;
            }

            // Initialize WaveSpawner if null
            if (m_ChallengeType == ChallengeType.TimeTrial && m_WaveSpawner == null)
            {
                m_WaveSpawner = GetComponent<WaveSpawner>();
                if (m_WaveSpawner == null)
                {
                    m_WaveSpawner = gameObject.AddComponent<WaveSpawner>();
                }
            }

            // Initialize enemy tracking for TimeTrial
            if (m_ChallengeType == ChallengeType.TimeTrial && EnemyManager.Instance)
            {
                EnemyManager.Instance.OnEnemyRegistered.AddListener((enemy) =>
                {
                    if (enemy)
                    {
                        m_ActiveEnemies.Add(enemy);
                    }
                });
                
                EnemyManager.Instance.OnEnemyUnregistered.AddListener((enemy) =>
                {
                    if (enemy)
                    {
                        m_ActiveEnemies.Remove(enemy);
                        if (m_IsChallengeActive && !m_IsChallengeComplete)
                        {
                            IncrementProgress(); // Count each defeated enemy
                        }
                    }
                });
            }

            if (m_WaveSpawner != null)
            {
                m_WaveSpawner.OnWaveCompleted += () =>
                {
                    if (m_ChallengeType == ChallengeType.TimeTrial)
                    {
                        onStageCompleted?.Invoke();
                        shouldUpdateTimer = false;
                    }
                };
            }

            // Show accept UI if challenge is optional
            if (m_OptionalChallenge)
            {
                ShowAcceptPrompt();
            }
            else
            {
                AcceptChallenge();
            }
        }
        
        private void Update()
        {
            if (!m_IsChallengeActive || m_IsChallengeComplete)
                return;
                
            // Update timer UI
            if (shouldUpdateTimer)
            {
                UpdateTimerUI();
            }
            
            // Update progress UI
            UpdateProgressUI();
            
            // Check for challenge completion based on type
            CheckChallengeCompletion();
        }
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Accept the challenge
        /// </summary>
        public void AcceptChallenge()
        {
            if (m_IsChallengeActive)
                return;
                
            m_IsChallengeActive = true;
            
            // Show challenge UI
            if (m_ChallengeUI != null)
            {
                m_ChallengeUI.SetActive(true);
            }
            
            // Initialize based on challenge type
            InitializeChallenge();
            
            // Start challenge coroutine
            m_ChallengeCoroutine = StartCoroutine(RunChallengeCoroutine());
            
            // Invoke event
            m_OnChallengeAccepted?.Invoke();
        }
        
        /// <summary>
        /// Skip the challenge
        /// </summary>
        public void SkipChallenge()
        {
            if (m_IsChallengeActive || !m_OptionalChallenge)
                return;
                
            // Invoke event
            m_OnChallengeSkipped?.Invoke();
            onStageCompleted?.Invoke();
        }
        
        /// <summary>
        /// Increment progress (for kills, score, etc.)
        /// </summary>
        public void IncrementProgress(int _amount = 1)
        {
            if (!m_IsChallengeActive || m_IsChallengeComplete)
                return;
                
            m_CurrentProgress += _amount;
        }
        
        /// <summary>
        /// Add to score
        /// </summary>
        public void AddScore(int _score)
        {
            if (!m_IsChallengeActive || m_IsChallengeComplete)
                return;
                
            m_CurrentScore += _score;
        }
        
        /// <summary>
        /// Complete a puzzle step
        /// </summary>
        public void CompletePuzzleStep()
        {
            if (!m_IsChallengeActive || m_IsChallengeComplete || m_ChallengeType != ChallengeType.Puzzle)
                return;
                
            IncrementProgress();
            
            // Check if all steps are complete
            if (m_CurrentProgress >= m_PuzzleSteps)
            {
                CompleteChallenge();
            }
        }
        
        /// <summary>
        /// Reach the end of an obstacle course
        /// </summary>
        public void ReachEndPoint()
        {
            if (!m_IsChallengeActive || m_IsChallengeComplete || m_ChallengeType != ChallengeType.ObstacleCourse)
                return;
                
            CompleteChallenge();
        }
        #endregion
        
        #region Private Methods
        /// <summary>
        /// Shows the accept challenge prompt
        /// </summary>
        private void ShowAcceptPrompt()
        {
            UIManager.Instance.ShowPopup("", $"{m_ChallengeDescription}\nAccept Challenge?", UIManager.PopupType.AcceptDeny,
                AcceptChallenge, 
                denyAction: SkipChallenge);
            
            if (m_ChallengeUI != null)
            {
                m_ChallengeUI.SetActive(false);
            }
            
            if (m_CompleteUI != null)
            {
                m_CompleteUI.SetActive(false);
            }
            
            if (m_FailUI != null)
            {
                m_FailUI.SetActive(false);
            }
        }
        
        /// <summary>
        /// Initialize the challenge based on its type
        /// </summary>
        private void InitializeChallenge()
        {
            switch (m_ChallengeType)
            {
                case ChallengeType.TimeTrial:
                    m_RemainingTime = m_TimeLimit;
                    m_CurrentProgress = 0;
                    if (m_WaveSpawner != null)
                    {
                        m_WaveSpawner.StartSpawning();
                    }
                    break;
                    
                case ChallengeType.Puzzle:
                    m_RemainingTime = m_PuzzleTimeLimit;
                    m_CurrentProgress = 0;
                    break;
                    
                case ChallengeType.ObstacleCourse:
                    m_RemainingTime = m_CourseTimeLimit;
                    break;
                    
                case ChallengeType.ScoreChallenge:
                    m_RemainingTime = m_TimeLimit;
                    m_CurrentScore = 0;
                    break;
            }
        }
        
        /// <summary>
        /// Run the challenge coroutine
        /// </summary>
        private IEnumerator RunChallengeCoroutine()
        {
            while (m_IsChallengeActive && !m_IsChallengeComplete && m_RemainingTime > 0)
            {
                // Decrement time
                m_RemainingTime -= Time.deltaTime;
                
                // Check for timeout
                if (m_RemainingTime <= 0)
                {
                    FailChallenge();
                    break;
                }
                
                yield return null;
            }
        }
        
        /// <summary>
        /// Update the timer UI
        /// </summary>
        private void UpdateTimerUI()
        {
            if (m_TimerText != null)
            {
                int minutes = Mathf.FloorToInt(m_RemainingTime / 60);
                int seconds = Mathf.FloorToInt(m_RemainingTime % 60);
                m_TimerText.text = $"{minutes:00}:{seconds:00}";
                
                // Change color when time is low
                if (m_RemainingTime <= 10f)
                {
                    m_TimerText.color = Color.red;
                }
                else
                {
                    m_TimerText.color = Color.white;
                }
            }
        }
        
        /// <summary>
        /// Update the progress UI
        /// </summary>
        private void UpdateProgressUI()
        {
            if (m_ProgressText != null)
            {
                switch (m_ChallengeType)
                {
                    case ChallengeType.TimeTrial:
                        m_ProgressText.text = $"Kills: {m_CurrentProgress}/{m_TargetKills}";
                        break;
                        
                    case ChallengeType.Puzzle:
                        m_ProgressText.text = $"Steps: {m_CurrentProgress}/{m_PuzzleSteps}";
                        break;
                        
                    case ChallengeType.ScoreChallenge:
                        m_ProgressText.text = $"Score: {m_CurrentScore}/{m_TargetScore}";
                        break;
                        
                    case ChallengeType.ObstacleCourse:
                        // Could implement distance-based progress here
                        m_ProgressText.text = "Reach the end!";
                        break;
                }
            }
        }
        
        /// <summary>
        /// Check if the challenge is complete based on its type
        /// </summary>
        private void CheckChallengeCompletion()
        {
            if (m_IsChallengeComplete)
                return;
                
            bool isComplete = false;
            
            switch (m_ChallengeType)
            {
                case ChallengeType.TimeTrial:
                    isComplete = m_CurrentProgress >= m_TargetKills;
                    break;
                    
                case ChallengeType.Puzzle:
                    isComplete = m_CurrentProgress >= m_PuzzleSteps;
                    break;
                    
                case ChallengeType.ScoreChallenge:
                    isComplete = m_CurrentScore >= m_TargetScore;
                    break;
                    
                // ObstacleCourse is completed by calling ReachEndPoint directly
            }
            
            if (isComplete)
            {
                CompleteChallenge();
            }
        }
        
        /// <summary>
        /// Complete the challenge
        /// </summary>
        private void CompleteChallenge()
        {
            if (m_IsChallengeComplete)
                return;
                
            m_IsChallengeComplete = true;
            
            // Stop coroutine
            if (m_ChallengeCoroutine != null)
            {
                StopCoroutine(m_ChallengeCoroutine);
            }
            
            // Stop wave spawner if TimeTrial
            if (m_ChallengeType == ChallengeType.TimeTrial && m_WaveSpawner != null)
            {
                m_WaveSpawner.StopSpawning();
            }
            
            // Show complete UI
            if (m_CompleteUI != null)
            {
                m_CompleteUI.SetActive(true);
            }
            
            if (m_ChallengeUI != null)
            {
                m_ChallengeUI.SetActive(false);
            }
            
            // Spawn reward
            SpawnReward();
            
            // Invoke event
            m_OnChallengeCompleted?.Invoke();
            onStageCompleted?.Invoke();
        }
        
        /// <summary>
        /// Fail the challenge
        /// </summary>
        private void FailChallenge()
        {
            m_IsChallengeActive = false;
            
            // Stop wave spawner if TimeTrial
            if (m_ChallengeType == ChallengeType.TimeTrial && m_WaveSpawner != null)
            {
                m_WaveSpawner.StopSpawning();
            }
            
            // Show fail UI
            if (m_FailUI != null)
            {
                m_FailUI.SetActive(true);
            }
            
            if (m_ChallengeUI != null)
            {
                m_ChallengeUI.SetActive(false);
            }
            
            // Invoke event
            m_OnChallengeFailed?.Invoke();
            
            onStageCompleted?.Invoke(); 
        }
        
        /// <summary>
        /// Spawn a reward for completing the challenge
        /// </summary>
        private void SpawnReward()
        {
            if (m_RewardPrefab != null && m_RewardSpawnPoint != null)
            {
                Instantiate(m_RewardPrefab, m_RewardSpawnPoint.position, Quaternion.identity);
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Types of challenges that can be implemented
    /// </summary>
    public enum ChallengeType
    {
        TimeTrial,      // Kill X enemies before time runs out
        Puzzle,         // Solve a puzzle before time runs out
        ObstacleCourse, // Navigate an obstacle course
        ScoreChallenge  // Reach a target score before time runs out
    }
} 