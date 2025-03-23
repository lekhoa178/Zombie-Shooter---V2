namespace LocomotionSystem
{
    public class PlayerMediumStoppingState : PlayerStoppingState
    {
        public PlayerMediumStoppingState(PlayerMovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
        {
        }

        public override void Enter()
        {
            base.Enter();

            StartAnimation(StateMachine.Player.AnimationData.MediumStopParameterHash);

            StateMachine.ReusableData.MovementDecelerationForce = GroundedData.StopData.MediumDecelerationForce;

            StateMachine.ReusableData.CurrentJumpForce = AirborneData.JumpData.MediumForce;
        }

        public override void Exit()
        {
            base.Exit();

            StopAnimation(StateMachine.Player.AnimationData.MediumStopParameterHash);
        }
    }
}