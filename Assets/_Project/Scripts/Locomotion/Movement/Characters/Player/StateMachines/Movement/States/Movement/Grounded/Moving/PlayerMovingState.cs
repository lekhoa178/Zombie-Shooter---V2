using UnityEngine;

namespace LocomotionSystem
{
    public class PlayerMovingState : PlayerGroundedState
    {
        public PlayerMovingState(PlayerMovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
        {
        }

        public override void Enter()
        {
            base.Enter();

            StartAnimation(StateMachine.Player.AnimationData.MovingParameterHash);
        }

        public override void Exit()
        {
            base.Exit();
            
            StopAnimation(StateMachine.Player.AnimationData.MovingParameterHash);
        }

        public override void OnAnimationWithIntParameterEvent(int isLeftFoot)
        {
            StateMachine.ReusableData.LeftFootIsForward = isLeftFoot == 1;
        } 
    }
}