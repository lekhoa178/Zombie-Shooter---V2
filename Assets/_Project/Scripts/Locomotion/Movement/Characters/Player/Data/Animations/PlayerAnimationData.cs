using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif

namespace LocomotionSystem
{
    [Serializable]
    public class PlayerAnimationData
    {
        public static class AnimationLayers
        {
            public const int Movement = 0; // state machine architecture-based
            public const int Combat = 1; // state machine architecture-based
            public const int Interaction = 2; // triggering via play / cross-fade
        }

        [Header("State Group Parameter Names")] [SerializeField]
        private string groundedParameterName = "Grounded";

        [SerializeField] private string movingParameterName = "Moving";
        [SerializeField] private string stoppingParameterName = "Stopping";
        [SerializeField] private string landingParameterName = "Landing";
        [SerializeField] private string airborneParameterName = "Airborne";

        [Header("Grounded Parameter Names")] [SerializeField]
        private string idleParameterName = "isIdling";

        [SerializeField] private string dashParameterName = "isDashing";
        [SerializeField] private string walkParameterName = "isWalking";
        [SerializeField] private string runParameterName = "isRunning";
        [SerializeField] private string sprintParameterName = "isSprinting";
        [SerializeField] private string mediumStopParameterName = "isMediumStopping";
        [SerializeField] private string hardStopParameterName = "isHardStopping";
        [SerializeField] private string rollParameterName = "isRolling";
        [SerializeField] private string hardLandParameterName = "isHardLanding";

        [Header("Airborne Parameter Names")] [SerializeField]
        private string jumpParameterName = "isJumping";

        [SerializeField] private string fallParameterName = "isFalling";
        [SerializeField] private string ledgeGrabParameterName = "isLedgeGrabbing";
        [SerializeField] private string ledgeClimbParameterName = "isLedgeClimbing";

        [Header("Attack Parameter Names")] [SerializeField]
        private string normalAttackParameterName = "isNormalAttacking";

        [SerializeField] private string chargeAttackParameterName = "isChargeAttacking";

        [Header("Util Parameter Names")] [SerializeField]
        private string rotationName = "Rotation";

        [SerializeField] private string jumpIndexName = "JumpIndex";

        [Header("Animator Mapping")]
        [SerializeField] private AnimatorStateClipMapping animatorMapping;
        [SerializeField] private bool updateMappingOnInitialize = true;

        [Header("Interaction State Names")]
        [field: SerializeField] public string DamagedStateName { get; private set; } = "Damaged";
        [field: SerializeField] public string CollectStateName { get; private set; } = "Collect";
        [field: SerializeField] public string DieStateName { get; private set; } = "Die";

        public int GroundedParameterHash { get; private set; }
        public int MovingParameterHash { get; private set; }
        public int StoppingParameterHash { get; private set; }
        public int LandingParameterHash { get; private set; }
        public int AirborneParameterHash { get; private set; }

        public int IdleParameterHash { get; private set; }
        public int DashParameterHash { get; private set; }
        public int WalkParameterHash { get; private set; }
        public int RunParameterHash { get; private set; }
        public int SprintParameterHash { get; private set; }
        public int MediumStopParameterHash { get; private set; }
        public int HardStopParameterHash { get; private set; }
        public int RollParameterHash { get; private set; }
        public int HardLandParameterHash { get; private set; }

        public int NormalAttackParameterHash { get; private set; }
        public int ChargeAttackParameterHash { get; private set; }

        public int JumpParameterHash { get; private set; }
        public int FallParameterHash { get; private set; }
        public int LedgeGrabParameterHash { get; private set; }
        public int LedgeClimbParameterHash { get; private set; }

        public int RotationHash { get; private set; }
        public int JumpIndexHash { get; private set; }
        
        // Track currently playing interaction animation
        private string m_CurrentInteractionAnimation;
        private float m_CurrentInteractionNormalizedTime;
        private bool m_IsInteractionActive;

        // Interaction animation event callbacks
        public delegate void InteractionAnimationEvent(string stateName);
        public event InteractionAnimationEvent OnInteractionAnimationStarted;
        public event InteractionAnimationEvent OnInteractionAnimationCompleted;

        public void Initialize(Player player)
        {
            // *** State Group Parameter Names ***
            GroundedParameterHash = Animator.StringToHash(groundedParameterName);
            MovingParameterHash = Animator.StringToHash(movingParameterName);
            StoppingParameterHash = Animator.StringToHash(stoppingParameterName);
            LandingParameterHash = Animator.StringToHash(landingParameterName);
            AirborneParameterHash = Animator.StringToHash(airborneParameterName);

            // *** Grounded Parameter Names ***
            IdleParameterHash = Animator.StringToHash(idleParameterName);
            DashParameterHash = Animator.StringToHash(dashParameterName);
            WalkParameterHash = Animator.StringToHash(walkParameterName);
            RunParameterHash = Animator.StringToHash(runParameterName);
            SprintParameterHash = Animator.StringToHash(sprintParameterName);
            MediumStopParameterHash = Animator.StringToHash(mediumStopParameterName);
            HardStopParameterHash = Animator.StringToHash(hardStopParameterName);
            RollParameterHash = Animator.StringToHash(rollParameterName);
            HardLandParameterHash = Animator.StringToHash(hardLandParameterName);

            // *** Attack Parameter Names ***
            NormalAttackParameterHash = Animator.StringToHash(normalAttackParameterName);
            ChargeAttackParameterHash = Animator.StringToHash(chargeAttackParameterName);

            // *** Airborne Parameter Names ***
            JumpParameterHash = Animator.StringToHash(jumpParameterName);
            FallParameterHash = Animator.StringToHash(fallParameterName);
            LedgeGrabParameterHash = Animator.StringToHash(ledgeGrabParameterName);
            LedgeClimbParameterHash = Animator.StringToHash(ledgeClimbParameterName);

            // *** Util Parameter Names ***
            RotationHash = Animator.StringToHash(rotationName);
            JumpIndexHash = Animator.StringToHash(jumpIndexName);
            
            if (player == null || player.Animator == null) return;

            // Update animator mapping from controller if needed
            if (updateMappingOnInitialize && animatorMapping != null)
            {
                try
                {
                    #if UNITY_EDITOR
                    // Only try to update mapping in editor mode
                    if (Application.isEditor && !Application.isPlaying)
                    {
                        UpdateAnimatorMapping(player.Animator);
                    }
                    #endif
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to update animator mapping: {e.Message}");
                }
            }

            InitializeOverrideData(player.Animator);
        }

        #if UNITY_EDITOR
        /// <summary>
        /// Updates the animator mapping from the animator controller.
        /// </summary>
        /// <param name="animator">The animator with the controller to map.</param>
        public void UpdateAnimatorMapping(Animator animator)
        {
            if (!animator || !animatorMapping) return;
            
            try
            {
                var controller = animator.runtimeAnimatorController as AnimatorController;
                if (controller == null)
                {
                    Debug.LogWarning("Animator does not have an AnimatorController.");
                    return;
                }
                
                AnimatorStateMapper.UpdateMappingFromController(animatorMapping, controller);
                Debug.Log("Updated animator mapping on initialize.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error updating animator mapping: {e.Message}");
            }
        }
        #endif

        private void InitializeOverrideData(Animator animator)
        {
            _animatorOverrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
            animator.runtimeAnimatorController = _animatorOverrideController;

            // Cache animation clips
            _animClips = _animatorOverrideController.animationClips;
            foreach (var clip in _animClips)
            {
                _clipOverrides[clip.name] = clip;
            }
        }

        private AnimatorOverrideController _animatorOverrideController;
        private Dictionary<string, AnimationClip> _clipOverrides = new Dictionary<string, AnimationClip>();
        private AnimationClip[] _animClips;

        public void OverrideAnimation(string stateName, AnimationClip newClip)
        {
            if (string.IsNullOrEmpty(stateName) || newClip == null) return;

            // Get the runtime animator controller
            var controller = _animatorOverrideController;
            if (controller == null) return;

            // Try to find clip name from state name using the mapping
            string clipName = animatorMapping.GetClipName(stateName);
            if (!string.IsNullOrEmpty(clipName))
            {
                // Find and override the clip
                foreach (var clip in _animClips)
                {
                    if (clip.name == clipName)
                    {
                        controller[clip] = newClip;
                        Debug.Log($"Overrode animation for state {stateName} (clip: {clipName})");
                        return;
                    }
                }
            }

            // Fallback to the original approach if mapping doesn't work
            foreach (var clip in _animClips)
            {
                if (clip.name.Contains(stateName))
                {
                    controller[clip] = newClip;
                    Debug.Log($"Overrode animation using fallback method for {stateName}");
                    return;
                }
            }
        }

        // Add this method to reset to original animations
        public void ResetAnimationOverrides()
        {
            foreach (var pair in _clipOverrides)
            {
                _animatorOverrideController[pair.Key] = pair.Value;
            }
        }

        #region Interaction Animation Handling

        /// <summary>
        /// Plays an interaction animation with full control over all parameters
        /// </summary>
        public void PlayInteractionAnimation(Animator animator, string stateName, float crossFadeDuration = 0.2f, float layerWeight = 1.0f)
        {
            if (animator == null) return;

            // string clipName = animatorMapping.GetClipName(stateName);
            string clipName = stateName;
            if (string.IsNullOrEmpty(clipName))
            {
                Debug.LogWarning($"No clip found for interaction state {stateName}");
                return;
            }

            // Set the layer weight
            animator.SetLayerWeight(AnimationLayers.Interaction, layerWeight);
            
            // Play the animation with crossfade
            if (crossFadeDuration <= 0)
            {
                animator.Play(clipName, AnimationLayers.Interaction, 0);
            }
            else
            {
                animator.CrossFade(clipName, crossFadeDuration, AnimationLayers.Interaction);
            }
            
            // Track current interaction animation
            m_CurrentInteractionAnimation = stateName;
            m_CurrentInteractionNormalizedTime = 0f;
            m_IsInteractionActive = true;
            OnInteractionAnimationStarted?.Invoke(stateName);
        }

        /// <summary>
        /// Stops the currently playing interaction animation by fading out the layer
        /// </summary>
        public void StopInteractionAnimation(Animator animator, float blendOutDuration = 0.2f)
        {
            if (animator == null || !m_IsInteractionActive) return;
            
            // Start the blend out
            StartBlendOutInteractionLayer(animator, blendOutDuration);
            
            // Signal animation completed
            OnInteractionAnimationCompleted?.Invoke(m_CurrentInteractionAnimation);
            m_IsInteractionActive = false;
        }

        /// <summary>
        /// Updates the interaction animation system. Should be called from Update.
        /// </summary>
        public void UpdateInteractionAnimations(Animator animator)
        {
            if (animator == null || !m_IsInteractionActive) return;

            // Get current state info
            var stateInfo = animator.GetCurrentAnimatorStateInfo(AnimationLayers.Interaction);
            
            // Update normalized time
            m_CurrentInteractionNormalizedTime = stateInfo.normalizedTime;
            
            // Check if the animation has completed (normalized time >= 1.0)
            if (m_CurrentInteractionNormalizedTime >= 0.99f && !stateInfo.loop)
            {
                // Auto-stop interaction animations that have completed
                ResetInteractionLayer(animator);
                
                // Signal animation completed
                OnInteractionAnimationCompleted?.Invoke(m_CurrentInteractionAnimation);
                m_IsInteractionActive = false;
            }
        }

        /// <summary>
        /// Returns whether an interaction animation is currently active
        /// </summary>
        public bool IsInteractionAnimationActive()
        {
            return m_IsInteractionActive;
        }

        /// <summary>
        /// Returns the normalized playback time (0-1) of the current interaction animation
        /// </summary>
        public float GetInteractionAnimationNormalizedTime()
        {
            return m_CurrentInteractionNormalizedTime;
        }

        /// <summary>
        /// Returns the name of the currently playing interaction animation
        /// </summary>
        public string GetCurrentInteractionAnimation()
        {
            return m_CurrentInteractionAnimation;
        }

        /// <summary>
        /// Gradually blends out the interaction layer weight
        /// </summary>
        private void StartBlendOutInteractionLayer(Animator animator, float duration)
        {
            if (animator == null) return;
            
            // Since we can't easily create a coroutine in this non-MonoBehaviour class,
            // we'll set the weight to 0 and let the animation system blend it
            animator.SetLayerWeight(AnimationLayers.Interaction, 0f);
        }

        /// <summary>
        /// Resets the interaction layer by setting weight to 0 and playing default animation
        /// </summary>
        private void ResetInteractionLayer(Animator animator)
        {
            if (animator == null) return;
            
            animator.SetLayerWeight(AnimationLayers.Interaction, 0f);
            
            // Play default animation for interaction layer
            animator.Play("Idle", AnimationLayers.Interaction, 0f);
        }

        #endregion
    }
}