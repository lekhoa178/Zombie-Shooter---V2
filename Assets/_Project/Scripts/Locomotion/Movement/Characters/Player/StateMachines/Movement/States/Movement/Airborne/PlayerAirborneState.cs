using UnityEngine;
using UnityEngine.InputSystem;

namespace LocomotionSystem
{
    public class PlayerAirborneState : PlayerMovementState
    {
        protected PlayerAirborneState(PlayerMovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
        {
        }

        public override void Enter()
        {
            base.Enter();

            ResetSprintState();
            
            StartAnimation(StateMachine.Player.AnimationData.AirborneParameterHash);
            
            StateMachine.ReusableData.IsGrounded = false;
        }

        public override void Exit()
        {
            base.Exit();

            StopAnimation(StateMachine.Player.AnimationData.AirborneParameterHash);
        }


        public override void Update()
        {
            base.Update();

            ApplyGravity();
        }

        protected virtual void ApplyGravity()
        {
            StateMachine.Player.Rigidbody.linearVelocity = new Vector3(
                StateMachine.Player.Rigidbody.linearVelocity.x,
                StateMachine.ReusableData.AppliedYVelocity,
                StateMachine.Player.Rigidbody.linearVelocity.z);
            
            var previousYVelocity = StateMachine.Player.Rigidbody.linearVelocity.y;
            var newYVelocity = StateMachine.Player.Rigidbody.linearVelocity.y + (StateMachine.ReusableData.Gravity * Time.deltaTime);
            StateMachine.ReusableData.AppliedYVelocity = (previousYVelocity + newYVelocity) * 0.5f;
        }

        public override void OnAnimationTransitionEvent()
        {
            StateMachine.ReusableData.CanJumpAgain = true;
        }
        
        protected virtual void ResetSprintState()
        {
            StateMachine.ReusableData.ShouldSprint = false;
        }

        protected override void OnDashStarted(InputAction.CallbackContext context)
        {
        }

        protected override void OnContactWithGround(Collider collider)
        {
            if (StateMachine.CurrentState is not PlayerFallingState)
            {
                return;
            }
            
            StateMachine.ChangeState(StateMachine.LightLandingState);
        }

        protected override void OnNormalAttackPerformed(InputAction.CallbackContext obj)
        {
        }

        protected override void OnJumpStarted(InputAction.CallbackContext context)
        {
            if (StateMachine.ReusableData.JumpCount <= 0)
            {
                return;
            }
            
            StateMachine.Player.Animator.SetFloat(StateMachine.Player.AnimationData.JumpIndexHash, AirborneData.JumpData.MaxJumpCount - StateMachine.ReusableData.JumpCount);
            
            StateMachine.ReusableData.JumpCount--;
            
            StateMachine.ChangeState(StateMachine.JumpingState);
        }
    }
}