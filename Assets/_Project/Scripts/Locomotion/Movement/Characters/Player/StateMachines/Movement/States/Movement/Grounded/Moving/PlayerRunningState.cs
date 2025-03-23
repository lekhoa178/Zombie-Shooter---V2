using UnityEngine;
using UnityEngine.InputSystem;

namespace LocomotionSystem
{
    public class PlayerRunningState : PlayerMovingState
    {
        private float startTime;

        public PlayerRunningState(PlayerMovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
        {
        }

        public override void Enter()
        {
            StateMachine.ReusableData.MovementSpeedModifier = GroundedData.RunData.SpeedModifier;

            base.Enter();

            StartAnimation(StateMachine.Player.AnimationData.RunParameterHash);

            StateMachine.ReusableData.CurrentJumpForce = AirborneData.JumpData.MediumForce;

            startTime = Time.time;
        }

        public override void Exit()
        {
            base.Exit();

            StopAnimation(StateMachine.Player.AnimationData.RunParameterHash);
        }

        public override void Update()
        {
            base.Update();

            if (!StateMachine.ReusableData.ShouldWalk)
            {
                return;
            }

            if (Time.time < startTime + GroundedData.SprintData.RunToWalkTime)
            {
                return;
            }

            StopRunning();
        }

        private void StopRunning()
        {
            if (StateMachine.ReusableData.MovementInput == Vector2.zero)
            {
                StateMachine.ChangeState(StateMachine.IdlingState);

                return;
            }

            StateMachine.ChangeState(StateMachine.WalkingState);
        }

        protected override void OnWalkToggleStarted(InputAction.CallbackContext context)
        {
            base.OnWalkToggleStarted(context);

            StateMachine.ChangeState(StateMachine.WalkingState);
        }

        protected override void OnMovementCanceled(InputAction.CallbackContext context)
        {
            StateMachine.ChangeState(StateMachine.MediumStoppingState);

            base.OnMovementCanceled(context);
        }
        
    }
}