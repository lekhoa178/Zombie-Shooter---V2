namespace LocomotionSystem
{
    public class PlayerHardStoppingState : PlayerStoppingState
    {
        public PlayerHardStoppingState(PlayerMovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
        {
        }

        public override void Enter()
        {
            base.Enter();

            StartAnimation(StateMachine.Player.AnimationData.HardStopParameterHash);

            StateMachine.ReusableData.MovementDecelerationForce = GroundedData.StopData.HardDecelerationForce;

            StateMachine.ReusableData.CurrentJumpForce = AirborneData.JumpData.StrongForce;
        }

        public override void Exit()
        {
            base.Exit();

            StopAnimation(StateMachine.Player.AnimationData.HardStopParameterHash);
        }

        protected override void OnMove()
        {
            if (StateMachine.ReusableData.ShouldWalk)
            {
                return;
            }

            StateMachine.ChangeState(StateMachine.RunningState);
        }
    }
}