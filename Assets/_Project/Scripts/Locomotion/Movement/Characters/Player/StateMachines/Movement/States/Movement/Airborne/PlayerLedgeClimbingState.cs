using UnityEngine;

namespace LocomotionSystem
{
    // currently the animation not good enough to use
    public class PlayerLedgeClimbingState : PlayerAirborneState
    {
        public PlayerLedgeClimbingState(PlayerMovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
        {
        }
        
        public override void Enter()
        {
            base.Enter();
            
            StartAnimation(StateMachine.Player.AnimationData.LedgeClimbParameterHash);

            StateMachine.Player.Animator.applyRootMotion = true;
            
            ResetVelocity();
            
            StateMachine.ReusableData.MovementSpeedModifier = 1.25f * StateMachine.ReusableData.JumpForceMultiplier;

            
            InitializeJumpParams();
            
            Jump();
        }

        public override void Exit()
        {
            base.Exit();
            
            StateMachine.Player.Animator.applyRootMotion = false;
            
            StopAnimation(StateMachine.Player.AnimationData.LedgeClimbParameterHash);
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnStateUpdate(animator, stateInfo, layerIndex);

            if (stateInfo.normalizedTime > 0.95f)
            {
                StateMachine.ChangeState(StateMachine.IdlingState);
            }
        }

        protected override void ApplyGravity()
        {

        }
        
        private void InitializeJumpParams()
        {
            var timeToApex = AirborneData.JumpData.MaxJumpTime / 2;
            var maxJumpHeight = StateMachine.ReusableData.CurrentJumpForce.y;
            
            StateMachine.ReusableData.Gravity = (-2 * maxJumpHeight) / Mathf.Pow(timeToApex, 2);
            StateMachine.ReusableData.AppliedYVelocity = 2 * maxJumpHeight / timeToApex;
        }

        private void Jump()
        {
            ResetVerticalVelocity();

            StateMachine.Player.Rigidbody.linearVelocity = new Vector3(
                StateMachine.Player.Rigidbody.linearVelocity.x,
                StateMachine.ReusableData.AppliedYVelocity,
                StateMachine.Player.Rigidbody.linearVelocity.z);
            
            // Old version

            // stateMachine.Player.Rigidbody.AddForce(force: initialJumpVelocity * 0.5f * Vector3.up, ForceMode.VelocityChange);
            // Vector3 jumpForce = stateMachine.ReusableData.CurrentJumpForce;
            //
            // Vector3 jumpDirection = stateMachine.Player.transform.forward;
            //
            // if (shouldKeepRotating)
            // {
            //     UpdateTargetRotation(GetMovementInputDirection());
            //
            //     jumpDirection = GetTargetRotationDirection(stateMachine.ReusableData.CurrentTargetRotation.y);
            // }
            //
            // jumpForce.x *= jumpDirection.x;
            // jumpForce.z *= jumpDirection.z;
            //
            // jumpForce = GetJumpForceOnSlope(jumpForce);
            //
            // ResetVelocity();
            //
            // stateMachine.Player.Rigidbody.AddForce(force: jumpForce, ForceMode.VelocityChange);
        }

        private Vector3 GetJumpForceOnSlope(Vector3 jumpForce)
        {
            Vector3 capsuleColliderCenterInWorldSpace = StateMachine.Player.ResizableCapsuleCollider.CapsuleColliderData.Collider.bounds.center;

            Ray downwardsRayFromCapsuleCenter = new Ray(capsuleColliderCenterInWorldSpace, Vector3.down);

            if (Physics.Raycast(downwardsRayFromCapsuleCenter, out RaycastHit hit, AirborneData.JumpData.JumpToGroundRayDistance, StateMachine.Player.LayerData.GroundLayer, QueryTriggerInteraction.Ignore))
            {
                float groundAngle = Vector3.Angle(hit.normal, -downwardsRayFromCapsuleCenter.direction);

                if (IsMovingUp())
                {
                    float forceModifier = AirborneData.JumpData.JumpForceModifierOnSlopeUpwards.Evaluate(groundAngle);

                    jumpForce.x *= forceModifier;
                    jumpForce.z *= forceModifier;
                }

                if (IsMovingDown())
                {
                    float forceModifier = AirborneData.JumpData.JumpForceModifierOnSlopeDownwards.Evaluate(groundAngle);

                    jumpForce.y *= forceModifier;
                }
            }

            return jumpForce;
        }
    }
}