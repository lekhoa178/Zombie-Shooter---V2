using UnityEngine;
using UnityEngine.InputSystem;

namespace LocomotionSystem
{
    public class PlayerJumpingState : PlayerAirborneState
    {
        private bool shouldKeepRotating;
        private bool canStartFalling;

        private readonly PlayerJumpData jumpData;

        public PlayerJumpingState(PlayerMovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
        {
            jumpData = AirborneData.JumpData;
        }

        public override void Enter()
        {
            base.Enter();

            StateMachine.ReusableData.CanJumpAgain = false;
            
            StartAnimation(StateMachine.Player.AnimationData.JumpParameterHash);
            
            StateMachine.ReusableData.MovementSpeedModifier = 1.25f;
            
            StateMachine.ReusableData.RotationData = AirborneData.JumpData.RotationData;
            
            shouldKeepRotating = true;
            
            InitializeJumpParams();
            
            Jump();
        }

        public override void Exit()
        {
            base.Exit();
            
            SetBaseRotationData();

            canStartFalling = false;

            StateMachine.ReusableData.JumpForceMultiplier = 1f;

            StopAnimation(StateMachine.Player.AnimationData.JumpParameterHash);
        }

        public override void Update()
        {
            base.Update();

            if (!canStartFalling && IsMovingUp(0f))
            {
                canStartFalling = true;
            }

            if (!canStartFalling || IsMovingUp(0f))
            {
                return;
            }

            StateMachine.ChangeState(StateMachine.FallingState);
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();

            if (shouldKeepRotating)
            {
                RotateTowardsTargetRotation();
            }

            if (IsMovingUp())
            {
                DecelerateVertically();
            }
        }
        
        private void InitializeJumpParams()
        {
            var timeToApex = jumpData.MaxJumpTime / 2;
            var maxJumpHeight = StateMachine.ReusableData.CurrentJumpForce.y * StateMachine.ReusableData.JumpForceMultiplier;
            
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

        protected override void ResetSprintState()
        {
        }

        protected override void OnMovementCanceled(InputAction.CallbackContext context)
        {
        }
    }
}