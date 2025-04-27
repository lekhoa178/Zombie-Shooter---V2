using DG.Tweening;
using LocomotionSystem;
using UnityEngine;

namespace DungeonGeneration
{
    /// <summary>
    /// Component that represents a portal/door to another stage
    /// </summary>
    public class StagePortal : InteractableBase
    {
        [SerializeField] private AnimationCurve _scaleAnimationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private float _scaleAnimationDuration = 0.5f;
        [SerializeField] private Vector3 _normalScale = Vector3.one;
        [SerializeField] private Vector3 _expandedScale = new Vector3(1.2f, 1.2f, 1.2f);
        private Vector3 _originalScale;
        
        #region Private Fields
        private string m_TargetStageID;
        private string m_TargetSceneName;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            
            if (m_VisualObject)
            {
                _originalScale = m_VisualObject.transform.localScale;
            }
        }
        #endregion
        
        #region Override Methods

        public override bool OnInteract(GameObject collector)
        {
            return Activate();
        }
        #endregion

        /// <summary>
        /// Initializes the portal with the target stage information
        /// </summary>
        public void Initialize(string targetStageID, string targetSceneName, string displayName)
        {
            m_TargetStageID = targetStageID;
            m_TargetSceneName = targetSceneName;
            
            m_DisplayName = displayName;

            m_SelectionText.transform.localScale = new Vector3(-1, 1, 1);
        }

        /// <summary>
        /// Activates the portal, triggering the transition to the next stage
        /// </summary>
        private bool Activate()
        {
            try 
            {
                DungeonManager.Instance.TransitionToStage(m_TargetStageID);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error activating portal: {e.Message}");
                return false;
            }

            return true;
        }

        protected void Update()
        {
            if (!_mainCamera) return;
            
            var direction = _mainCamera.transform.position - transform.position;
            var rotation = Quaternion.LookRotation(direction);
            transform.rotation = rotation;
        }

        /// <summary>
        /// Legacy trigger handling - can be used alongside InteractManager
        /// by setting the player's interaction trigger to this collider
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && m_VisualObject)
            {
                // Kill any ongoing scale animations
                m_VisualObject.transform.DOKill(true);
        
                // Scale up with animation curve
                m_VisualObject.transform.DOScale(_expandedScale, _scaleAnimationDuration)
                    .SetEase(_scaleAnimationCurve);
            }
        }

        /// <summary>
        /// Legacy trigger handling for exit
        /// </summary>
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") && m_VisualObject)
            {
                // Kill any ongoing scale animations
                m_VisualObject.transform.DOKill(true);
        
                // Scale back to normal with animation curve
                m_VisualObject.transform.DOScale(_normalScale, _scaleAnimationDuration)
                    .SetEase(_scaleAnimationCurve);
            }
        }
    }
} 