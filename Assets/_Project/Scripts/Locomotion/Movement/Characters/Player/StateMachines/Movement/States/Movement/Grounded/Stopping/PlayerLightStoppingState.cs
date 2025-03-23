namespace LocomotionSystem
{
    public class PlayerLightStoppingState : PlayerStoppingState
    {
        public PlayerLightStoppingState(PlayerMovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
        {
        }

        public override void Enter()
        {
            base.Enter();

            StateMachine.ReusableData.MovementDecelerationForce = GroundedData.StopData.LightDecelerationForce;

            StateMachine.ReusableData.CurrentJumpForce = AirborneData.JumpData.WeakForce;
        }
    }
}