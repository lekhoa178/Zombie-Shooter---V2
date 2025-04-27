using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

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
        [SerializeField] private GameObject m_AcceptUI;
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
        #endregion
        
        #region Unity Lifecycle
        private void Start()
        {
            // Initialize UI
            if (m_TitleText != null)
            {
                m_TitleText.text = m_ChallengeName;
            }
            
            if (m_DescriptionText != null)
            {
                m_DescriptionText.text = m_ChallengeDescription;
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
            UpdateTimerUI();
            
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
            
            // Hide accept UI
            if (m_AcceptUI != null)
            {
                m_AcceptUI.SetActive(false);
            }
            
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
                
            // Hide accept UI
            if (m_AcceptUI != null)
            {
                m_AcceptUI.SetActive(false);
            }
            
            // Invoke event
            m_OnChallengeSkipped?.Invoke();
            
            // Continue to next stage
            // This would typically notify the DungeonManager or create portals
            CreateExitPortals();
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
            if (m_AcceptUI != null)
            {
                m_AcceptUI.SetActive(true);
            }
            
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
                m_TimerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
                
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
            
            // Create exit portals
            CreateExitPortals();
        }
        
        /// <summary>
        /// Fail the challenge
        /// </summary>
        private void FailChallenge()
        {
            m_IsChallengeActive = false;
            
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
            
            // Create exit portals after a delay
            StartCoroutine(CreateExitPortalsDelayed(3f));
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
        
        /// <summary>
        /// Create exit portals to leave the challenge stage
        /// </summary>
        private void CreateExitPortals()
        {
            // This would typically notify the DungeonManager to create portals to the next stages
            // For now, we'll just log it
            Debug.Log("Creating exit portals from challenge stage");
            
            // In a real implementation, this might look like:
            // DungeonManager.Instance.CreateStageExits();
        }
        
        /// <summary>
        /// Create exit portals after a delay
        /// </summary>
        private IEnumerator CreateExitPortalsDelayed(float _delay)
        {
            yield return new WaitForSeconds(_delay);
            CreateExitPortals();
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