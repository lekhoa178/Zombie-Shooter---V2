using UnityEngine;
using UnityEngine.InputSystem;

namespace LocomotionSystem
{
    public class PlayerDashingState : PlayerGroundedState
    {
        private float startTime;

        private int consecutiveDashesUsed;

        private bool shouldKeepRotating;
        private bool isBackDash;

        public PlayerDashingState(PlayerMovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
        {
        }

        public override void Enter()
        {
            base.Enter();

            StartAnimation(StateMachine.Player.AnimationData.DashParameterHash);

            StateMachine.ReusableData.CurrentJumpForce = AirborneData.JumpData.StrongForce;

            StateMachine.ReusableData.RotationData = GroundedData.DashData.RotationData;

            StateMachine.Player.Animator.applyRootMotion = true;

            // comment because of using animation to move the player
            // Dash();

            shouldKeepRotating = StateMachine.ReusableData.MovementInput != Vector2.zero;
            isBackDash = StateMachine.ReusableData.MovementInput == Vector2.zero;

            if (StateMachine.ReusableData.MovementInput == Vector2.zero)
            {
                StateMachine.Player.Animator.SetFloat("IsBackDash", 1f);
                StateMachine.ReusableData.MovementSpeedModifier = 0f;
            }
            else
            {
                StateMachine.ReusableData.MovementSpeedModifier = GroundedData.DashData.SpeedModifier;
                StateMachine.Player.Animator.SetFloat("IsBackDash", 0f);
            }

            UpdateConsecutiveDashes();

            startTime = Time.time;
        }

        public override void Exit()
        {
            base.Exit();

            StopAnimation(StateMachine.Player.AnimationData.DashParameterHash);

            SetBaseRotationData();
            
            StateMachine.Player.Animator.applyRootMotion = false;
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();

            if (!shouldKeepRotating)
            {
                return;
            }

            RotateTowardsTargetRotation();
        }

        public override void OnAnimationTransitionEvent()
        {
            if (StateMachine.ReusableData.MovementInput == Vector2.zero)
            {
                // stateMachine.ChangeState(stateMachine.HardStoppingState);

                return;
            }

            StateMachine.ChangeState(StateMachine.SprintingState);
        }

        public override void OnAnimationExitEvent()
        {
            StateMachine.ChangeState(StateMachine.IdlingState);
        }

        protected override void AddInputActionsCallbacks()
        {
            base.AddInputActionsCallbacks();

            StateMachine.Player.Input.PlayerActions.Movement.performed += OnMovementPerformed;
            StateMachine.Player.Input.PlayerActions.BoostDash.performed += OnBoostDashStarted;

        }

        protected override void RemoveInputActionsCallbacks()
        {
            base.RemoveInputActionsCallbacks();

            StateMachine.Player.Input.PlayerActions.Movement.performed -= OnMovementPerformed;
            StateMachine.Player.Input.PlayerActions.BoostDash.performed -= OnBoostDashStarted;
        }

        protected override void OnMovementPerformed(InputAction.CallbackContext context)
        {
            base.OnMovementPerformed(context);

            shouldKeepRotating = true;
        }
        
        private void OnBoostDashStarted(InputAction.CallbackContext obj)
        {
            if (isBackDash) return;
            
            StateMachine.ChangeState(StateMachine.BoostDashingState);
        }

        private void Dash()
        {
            Vector3 dashDirection = StateMachine.Player.transform.forward;

            dashDirection.y = 0f;

            UpdateTargetRotation(dashDirection, false);

            if (StateMachine.ReusableData.MovementInput != Vector2.zero)
            {
                UpdateTargetRotation(GetMovementInputDirection());

                dashDirection = GetTargetRotationDirection(StateMachine.ReusableData.CurrentTargetRotation.y);
            }

            StateMachine.Player.Rigidbody.AddForce(dashDirection * GetMovementSpeed(false), ForceMode.VelocityChange);
            // stateMachine.Player.Rigidbody.linearVelocity = dashDirection * GetMovementSpeed(false);
        }

        private void UpdateConsecutiveDashes()
        {
            if (!IsConsecutive())
            {
                consecutiveDashesUsed = 0;
            }

            ++consecutiveDashesUsed;

            if (consecutiveDashesUsed == GroundedData.DashData.ConsecutiveDashesLimitAmount)
            {
                consecutiveDashesUsed = 0;

                StateMachine.Player.Input.DisableActionFor(StateMachine.Player.Input.PlayerActions.Dash, GroundedData.DashData.DashLimitReachedCooldown);
            }
        }

        private bool IsConsecutive()
        {
            return Time.time < startTime + GroundedData.DashData.TimeToBeConsideredConsecutive;
        }

        protected override void OnDashStarted(InputAction.CallbackContext context)
        {
        }
    }
}