using System;
using System.Collections.Generic;
using UnityEngine;

namespace LocomotionSystem
{
    [Serializable]
    public class PlayerAnimationData
    {
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
        
        [SerializeField] private AnimatorStateClipMapping animatorMapping;


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
            
            InitializeOverrideData(player.Animator);
        }

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
    }
}