using System.Collections.Generic;
using Locomotion;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityUtils;

namespace LocomotionSystem
{
    public class PlayerNormalAttackingState : PlayerCombatState
    {
        public PlayerNormalAttackingState(PlayerMovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
        {
            StateMachine.ReusableData.AttackCooldownTimer = new CountdownTimer(0f);
        }

        public override void Enter()
        {
            base.Enter();
            
            IsCombo = false;
            StateMachine.ReusableData.IsComboFrame = true;
            StateMachine.ReusableData.MovementSpeedModifier = 0f;
    
            ResetVelocity();
    
            // Get current combo configuration
            var comboIndex = StateMachine.ReusableData.CurrentComboIndex;
            var config = StateMachine.Player.Data.ComboConfig.AttackConfigs[comboIndex];
    
            // Set targeting parameters based on configuration
            SetTargetingParameters(config);
            
            // Get attack timing info including animation speed
            var timingInfo = StateMachine.Player.Data.ComboConfig.GetAttackTimingInfo();
            float attackAnimSpeedMultiplier = timingInfo.animSpeedMultiplier;
            
            // Apply root motion setting from configuration
            StateMachine.Player.Animator.applyRootMotion = !config || config.UseRootMotion;
            
            // Calculate base animation speed from attack speed
            float finalAnimSpeed = attackAnimSpeedMultiplier;
            
            // Apply animation override if available
            if (config != null && config.UseCustomAnimation && config.AnimationClip != null)
            {
                string stateName = !string.IsNullOrEmpty(config.AnimatorStateName) 
                    ? config.AnimatorStateName 
                    : "NormalAttack";
                    
                StateMachine.Player.AnimationData.OverrideAnimation(stateName, config.AnimationClip);
                
                // Combine animation speed override with attack speed scaling
                if (config.OverrideAnimationSpeed)
                {
                    finalAnimSpeed *= config.AnimationSpeedMultiplier;
                }
            }
            
            // Apply the calculated animation speed
            StateMachine.Player.Animator.speed = finalAnimSpeed;
            
            if (finalAnimSpeed > 1.0f)
            {
                Debug.Log($"Attack animation speed scaled to: {finalAnimSpeed:F2}x due to high attack speed");
            }
            
            if (!StateMachine.Player.TargetDetector.GetCurrentTarget())
            {
                if (StateMachine.ReusableData.MovementInput != Vector2.zero)
                {
                    float directionAngle = UpdateTargetRotation(StateMachine.ReusableData.MovementInput);
                    RotateTowardsTargetRotation(true);
                    
                }
            }
            
            // Apply attack speed settings from ComboConfiguration
            var countdown = timingInfo.interval;
            StateMachine.ReusableData.AttackCooldownTimer.SetInitialTime(countdown);
            StateMachine.ReusableData.AttackCooldownTimer.Start();
            
            StateMachine.Player.Animator.SetFloat("ComboAttackIndex", StateMachine.ReusableData.CurrentComboIndex);
            StartAnimation(StateMachine.Player.AnimationData.NormalAttackParameterHash);
    
            IsAttackFrame = false;
            IsFinished = false;
        }

        private void SetTargetingParameters(AttackHitConfiguration config)
        {
            // Get default values from combo config
            float detectionRange = StateMachine.Player.Data.ComboConfig.DefaultDetectionRange;
            float dotProductThreshold = StateMachine.Player.Data.ComboConfig.DefaultDotProductThreshold;
            
            // Override with attack-specific values if configured
            if (config != null && config.OverrideTargetDetectionSettings)
            {
                detectionRange = config.DetectionRange;
                dotProductThreshold = config.DotProductThreshold;
            }
            
            // Set the values directly on the target detector
            StateMachine.Player.TargetDetector.SetDetectionParameters(detectionRange, dotProductThreshold);
        }

        public override void Exit()
        {
            base.Exit();
            
            StateMachine.Player.Animator.applyRootMotion = false;
            StateMachine.Player.Animator.speed = 1f;
            
            StateMachine.Player.AnimationData.ResetAnimationOverrides();
            
            // Reset target detection parameters to defaults when exiting
            StateMachine.Player.TargetDetector.ResetToDefaultParameters(
                StateMachine.Player.Data.ComboConfig.DefaultDetectionRange,
                StateMachine.Player.Data.ComboConfig.DefaultDotProductThreshold
            );

            StopAnimation(StateMachine.Player.AnimationData.NormalAttackParameterHash);
        }
        
        protected override void OnNormalAttackPerformed(InputAction.CallbackContext obj)
        {
            Debug.Log($"Normal attack performed - PlayerNormalAttackingState || IsCombo: {IsCombo} || IsOnCooldown: {StateMachine.ReusableData.IsOnCooldown} || IsFinished: {IsFinished} || IsComboFrame: {StateMachine.ReusableData.IsComboFrame}");
            
            // Only allow combo input during the combo window
            if (StateMachine.ReusableData.IsComboFrame)
            {
                IsCombo = true;
            }
            
            // Check if we're on cooldown
            if (StateMachine.ReusableData.IsOnCooldown)
            {
                Debug.Log($"Attack on cooldown: {StateMachine.ReusableData.AttackCooldownTimer.Time:F2}s remaining");
                return;
            }
    
            if (IsFinished)
            {
                IsCombo = true;
                FinishAttack();
            }
        }

        public override void FinishAttack()
        {
            base.FinishAttack();
            
            Debug.Log($"[FinishAttack] IsCombo: {IsCombo} || IsOnCooldown: {StateMachine.ReusableData.IsOnCooldown}");
            
            if (IsCombo && !StateMachine.ReusableData.IsOnCooldown)
            {
                IsCombo = false;

                // Get the actual number of attack configurations in the ComboConfig
                int attackConfigCount = StateMachine.Player.Data.ComboConfig?.AttackConfigs?.Length ?? 1;
                
                // Make sure we don't divide by zero
                if (attackConfigCount <= 0) attackConfigCount = 1;
                
                // Update the combo index based on the actual number of configured attacks
                StateMachine.ReusableData.CurrentComboIndex = (StateMachine.ReusableData.CurrentComboIndex + 1) % attackConfigCount;
                
                // Get attack timing info including animation speed scaling
                var timingInfo = StateMachine.Player.Data.ComboConfig.GetAttackTimingInfo();
                var countdown = timingInfo.interval;
                
                // Apply attack speed settings from ComboConfiguration
                StateMachine.ReusableData.AttackCooldownTimer.SetInitialTime(countdown);
                StateMachine.ReusableData.AttackCooldownTimer.Start();
                
                if (timingInfo.animSpeedMultiplier > 1.0f)
                {
                    Debug.Log($"Attack cooldown set: {countdown:F2}s with animation speed boost: {timingInfo.animSpeedMultiplier:F2}x");
                }
                else
                {
                    Debug.Log($"Attack cooldown set: {countdown:F2}s (Speed Multiplier: {StateMachine.Player.Data.ComboConfig.AttackSpeedMultiplier:F2}x)");
                }
                
                Debug.Log($"Combo Index: {StateMachine.ReusableData.CurrentComboIndex} of {attackConfigCount}");

                StateMachine.ChangeState(StateMachine.NormalAttackingState);
            }
            else
            {
                if (StateMachine.ReusableData.MovementInput != Vector2.zero)
                {
                    OnMove();
                }
            }
        }

        public override void StartComboFrame()
        {
            base.StartComboFrame();

            IsCombo = false;
        }
    }
}