using UnityEngine;
using UnityEngine.InputSystem;

namespace LocomotionSystem
{
    public class PlayerGroundedState : PlayerMovementState
    {
        public PlayerGroundedState(PlayerMovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
        {
        }

        public override void Enter()
        {
            base.Enter();

            StartAnimation(StateMachine.Player.AnimationData.GroundedParameterHash);

            UpdateShouldSprintState();

            UpdateCameraRecenteringState(StateMachine.ReusableData.MovementInput);
            
            StateMachine.ReusableData.IsGrounded = true;
        }

        public override void Exit()
        {
            base.Exit();

            StopAnimation(StateMachine.Player.AnimationData.GroundedParameterHash);
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();

            Float();
        }

        private void UpdateShouldSprintState()
        {
            if (!StateMachine.ReusableData.ShouldSprint)
            {
                return;
            }

            if (StateMachine.ReusableData.MovementInput != Vector2.zero)
            {
                return;
            }

            StateMachine.ReusableData.ShouldSprint = false;
        }

        private void Float()
        {
            Vector3 capsuleColliderCenterInWorldSpace = StateMachine.Player.ResizableCapsuleCollider.CapsuleColliderData.Collider.bounds.center;

            Ray downwardsRayFromCapsuleCenter = new Ray(capsuleColliderCenterInWorldSpace, Vector3.down);

            if (Physics.Raycast(downwardsRayFromCapsuleCenter, out RaycastHit hit, StateMachine.Player.ResizableCapsuleCollider.SlopeData.FloatRayDistance, StateMachine.Player.LayerData.GroundLayer, QueryTriggerInteraction.Ignore))
            {
                float groundAngle = Vector3.Angle(hit.normal, -downwardsRayFromCapsuleCenter.direction);

                float slopeSpeedModifier = SetSlopeSpeedModifierOnAngle(groundAngle);
                
                if (slopeSpeedModifier == 0f)
                {
                    return;
                }

                float distanceToFloatingPoint = StateMachine.Player.ResizableCapsuleCollider.CapsuleColliderData.ColliderCenterInLocalSpace.y * StateMachine.Player.transform.localScale.y - hit.distance;

                if (distanceToFloatingPoint == 0f)
                {
                    return;
                }

                float amountToLift = distanceToFloatingPoint * StateMachine.Player.ResizableCapsuleCollider.SlopeData.StepReachForce - GetPlayerVerticalVelocity().y;

                Vector3 liftForce = new Vector3(0f, amountToLift, 0f);

                StateMachine.Player.Rigidbody.AddForce(liftForce, ForceMode.VelocityChange);
            }
        }

        private float SetSlopeSpeedModifierOnAngle(float angle)
        {
            float slopeSpeedModifier = GroundedData.SlopeSpeedAngles.Evaluate(angle);

            if (!Mathf.Approximately(StateMachine.ReusableData.MovementOnSlopesSpeedModifier, slopeSpeedModifier))
            {
                StateMachine.ReusableData.MovementOnSlopesSpeedModifier = slopeSpeedModifier;

                UpdateCameraRecenteringState(StateMachine.ReusableData.MovementInput);
            }

            return slopeSpeedModifier;
        }

        protected override void AddInputActionsCallbacks()
        {
            base.AddInputActionsCallbacks();
        }

        protected override void RemoveInputActionsCallbacks()
        {
            base.RemoveInputActionsCallbacks();
        }
        

        protected override void OnContactWithGroundExited(Collider collider)
        {
            if (IsThereGroundUnderneath())
            {
                return;
            }

            Vector3 capsuleColliderCenterInWorldSpace = StateMachine.Player.ResizableCapsuleCollider.CapsuleColliderData.Collider.bounds.center;

            Ray downwardsRayFromCapsuleBottom = new Ray(capsuleColliderCenterInWorldSpace - StateMachine.Player.ResizableCapsuleCollider.CapsuleColliderData.ColliderVerticalExtents, Vector3.down);

            if (!Physics.Raycast(downwardsRayFromCapsuleBottom, out _, GroundedData.GroundToFallRayDistance, StateMachine.Player.LayerData.GroundLayer, QueryTriggerInteraction.Ignore))
            {
                OnFall();
            }
        }

        private bool IsThereGroundUnderneath()
        {
            PlayerTriggerColliderData triggerColliderData = StateMachine.Player.ResizableCapsuleCollider.TriggerColliderData;

            Vector3 groundColliderCenterInWorldSpace = triggerColliderData.GroundCheckCollider.bounds.center;

            Collider[] results = null;
            var size = Physics.OverlapBoxNonAlloc(groundColliderCenterInWorldSpace, triggerColliderData.GroundCheckColliderVerticalExtents, results, triggerColliderData.GroundCheckCollider.transform.rotation, StateMachine.Player.LayerData.GroundLayer, QueryTriggerInteraction.Ignore);

            return size > 0;
        }

        protected virtual void OnFall()
        {
            const float timeToApex = 0.25f;
            const int maxJumpHeight = 1;
            
            StateMachine.ReusableData.Gravity = (-2 * maxJumpHeight) / Mathf.Pow(timeToApex, 2);
            StateMachine.ReusableData.AppliedYVelocity = 0f;
            
            StateMachine.ReusableData.JumpCount = StateMachine.Player.Data.AirborneData.JumpData.MaxJumpCount;
            StateMachine.Player.Animator.SetFloat(StateMachine.Player.AnimationData.JumpIndexHash, 0);
            
            StateMachine.ChangeState(StateMachine.FallingState);
        }

        protected override void OnMovementPerformed(InputAction.CallbackContext context)
        {
            base.OnMovementPerformed(context);

            UpdateTargetRotation(GetMovementInputDirection());
        }
    }
}