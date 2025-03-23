using UnityEngine.InputSystem;

namespace LocomotionSystem
{
    public class PlayerStoppingState : PlayerGroundedState
    {
        public PlayerStoppingState(PlayerMovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
        {
        }

        public override void Enter()
        {
            StateMachine.ReusableData.MovementSpeedModifier = 0f;

            SetBaseCameraRecenterData();

            base.Enter();

            StartAnimation(StateMachine.Player.AnimationData.StoppingParameterHash);
            
            StateMachine.Player.Animator.SetFloat("IsLeftFootForward", StateMachine.ReusableData.LeftFootIsForward ? 1 : 0);
            StateMachine.Player.Animator.applyRootMotion = true;
        }

        public override void Exit()
        {
            StateMachine.Player.Animator.applyRootMotion = false;

            base.Exit();

            StopAnimation(StateMachine.Player.AnimationData.StoppingParameterHash);
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();

            RotateTowardsTargetRotation();

            if (!IsMovingHorizontally())
            {
                return;
            }

            DecelerateHorizontally();
        }

        public override void OnAnimationTransitionEvent()
        {
            StateMachine.ChangeState(StateMachine.IdlingState);
        }

        protected override void AddInputActionsCallbacks()
        {
            base.AddInputActionsCallbacks();

            StateMachine.Player.Input.PlayerActions.Movement.started += OnMovementStarted;
        }

        protected override void RemoveInputActionsCallbacks()
        {
            base.RemoveInputActionsCallbacks();

            StateMachine.Player.Input.PlayerActions.Movement.started -= OnMovementStarted;
        }

        private void OnMovementStarted(InputAction.CallbackContext context)
        {
            OnMove();
        }
    }
}