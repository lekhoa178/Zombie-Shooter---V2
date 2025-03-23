using UnityEngine;
using UnityEngine.Animations;

namespace LocomotionSystem
{
    public class PlayerMovementStateMachine : StateMachine
    {
        public Player Player { get; private set; }
        public PlayerStateReusableData ReusableData { get; private set; }

        public PlayerIdlingState IdlingState { get; private set; }
        public PlayerDashingState DashingState { get; private set; }
        public PlayerBoostDashingState BoostDashingState { get; private set; }

        public PlayerWalkingState WalkingState { get; private set; }
        public PlayerRunningState RunningState { get; private set; }
        public PlayerSprintingState SprintingState { get; private set; }

        public PlayerLightStoppingState LightStoppingState { get; private set; }
        public PlayerMediumStoppingState MediumStoppingState { get; private set; }
        public PlayerHardStoppingState HardStoppingState { get; private set; }

        public PlayerLightLandingState LightLandingState { get; private set; }
        public PlayerRollingState RollingState { get; private set; }
        public PlayerHardLandingState HardLandingState { get; private set; }
        
        public PlayerJumpingState JumpingState { get; private set; }
        public PlayerFallingState FallingState { get; private set; }
        public PlayerLedgeGrabbingState LedgeGrabbingState { get; private set; }
        public PlayerLedgeClimbingState LedgeClimbingState { get; private set; }
        
        public PlayerNormalAttackingState NormalAttackingState { get; private set; }
        public PlayerChargeAttackingState ChargeAttackingState { get; private set; }
        
        public PlayerMovementStateMachine(Player player)
        {
            Initialize(player);
        }

        public void Initialize(Player player)
        {
            Player = player;
            ReusableData = new PlayerStateReusableData();

            IdlingState = new PlayerIdlingState(this);
            DashingState = new PlayerDashingState(this);
            BoostDashingState = new PlayerBoostDashingState(this);

            WalkingState = new PlayerWalkingState(this);
            RunningState = new PlayerRunningState(this);
            SprintingState = new PlayerSprintingState(this);

            LightStoppingState = new PlayerLightStoppingState(this);
            MediumStoppingState = new PlayerMediumStoppingState(this);
            HardStoppingState = new PlayerHardStoppingState(this);

            LightLandingState = new PlayerLightLandingState(this);
            RollingState = new PlayerRollingState(this);
            HardLandingState = new PlayerHardLandingState(this);

            JumpingState = new PlayerJumpingState(this);
            FallingState = new PlayerFallingState(this);
            LedgeGrabbingState = new PlayerLedgeGrabbingState(this);
            LedgeClimbingState = new PlayerLedgeClimbingState(this);
            
            NormalAttackingState = new PlayerNormalAttackingState(this);
            ChargeAttackingState = new PlayerChargeAttackingState(this);

            DefaultState = IdlingState;
        }
        
    }
}