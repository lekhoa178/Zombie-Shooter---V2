using UnityEngine.InputSystem;

namespace LocomotionSystem
{
    public class PlayerWalkingState : PlayerMovingState
    {
        public PlayerWalkingState(PlayerMovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
        {
        }

        public override void Enter()
        {
            StateMachine.ReusableData.MovementSpeedModifier = GroundedData.WalkData.SpeedModifier;

            StateMachine.ReusableData.BackwardsCameraRecenteringData = GroundedData.WalkData.BackwardsCameraRecenteringData;

            base.Enter();

            StartAnimation(StateMachine.Player.AnimationData.WalkParameterHash);

            StateMachine.ReusableData.CurrentJumpForce = AirborneData.JumpData.WeakForce;
        }

        public override void Exit()
        {
            base.Exit();

            StopAnimation(StateMachine.Player.AnimationData.WalkParameterHash);

            SetBaseCameraRecenterData();
        }

        protected override void OnWalkToggleStarted(InputAction.CallbackContext context)
        {
            base.OnWalkToggleStarted(context);

            StateMachine.ChangeState(StateMachine.RunningState);
        }

        protected override void OnMovementCanceled(InputAction.CallbackContext context)
        {
            StateMachine.ChangeState(StateMachine.LightStoppingState);

            base.OnMovementCanceled(context);
        }
    }
}