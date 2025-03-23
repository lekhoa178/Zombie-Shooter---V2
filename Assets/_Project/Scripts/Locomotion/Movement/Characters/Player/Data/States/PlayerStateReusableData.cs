using System.Collections.Generic;
using UnityEngine;
using UnityUtils;

namespace LocomotionSystem
{
    public class PlayerStateReusableData
    {
        public Vector2 MovementInput { get; set; }

        public float MovementSpeedModifier { get; set; } = 1f;
        public float MovementOnSlopesSpeedModifier { get; set; } = 1f;
        public float MovementDecelerationForce { get; set; } = 1f;

        public float Gravity { get; set; } = -9.81f;
        public float AppliedYVelocity { get; set; }
        public float JumpForceMultiplier { get; set; } = 1f;
        
        public bool IsGrounded { get; set; }

        public List<PlayerCameraRecenteringData> SidewaysCameraRecenteringData { get; set; }
        public List<PlayerCameraRecenteringData> BackwardsCameraRecenteringData { get; set; }

        public bool ShouldWalk { get; set; }
        public bool ShouldSprint { get; set; }
        public bool LeftFootIsForward { get; set; }
        
        public int JumpCount { get; set; }
        public bool CanJumpAgain { get; set; }
        
        public int CurrentComboIndex { get; set; }
        public bool IsComboFrame { get; set; }
        public CountdownTimer ResetComboTimer { get; set; } = new CountdownTimer(1f);
        
        public CountdownTimer AttackCooldownTimer { get; set; }
        public bool IsOnCooldown => AttackCooldownTimer.Time > 0f;


        private Vector3 currentTargetRotation;
        private Vector3 timeToReachTargetRotation;
        private Vector3 dampedTargetRotationCurrentVelocity;
        private Vector3 dampedTargetRotationPassedTime;
        
        public ref Vector3 CurrentTargetRotation => ref currentTargetRotation;
        public ref Vector3 TimeToReachTargetRotation => ref timeToReachTargetRotation;
        public ref Vector3 DampedTargetRotationCurrentVelocity => ref dampedTargetRotationCurrentVelocity;
        public ref Vector3 DampedTargetRotationPassedTime => ref dampedTargetRotationPassedTime;
        public Vector3 CurrentJumpForce { get; set; }

        public PlayerRotationData RotationData { get; set; }
        
        private Vector3 lastTargetRotation;
        public ref Vector3 LastTargetRotation => ref lastTargetRotation;
    }
}