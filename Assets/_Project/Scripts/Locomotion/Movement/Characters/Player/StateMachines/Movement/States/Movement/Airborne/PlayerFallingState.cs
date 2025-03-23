using UnityEngine;
using UnityUtils;

namespace LocomotionSystem
{
    public class PlayerFallingState : PlayerAirborneState
    {
        private Vector3 playerPositionOnEnter;
        
        private CountdownTimer groundCheckTimer;
        
        public PlayerFallingState(PlayerMovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
        {
            groundCheckTimer = new CountdownTimer(0.2f);
        }

        public override void Enter()
        {
            base.Enter();

            StartAnimation(StateMachine.Player.AnimationData.FallParameterHash);

            StateMachine.ReusableData.MovementSpeedModifier = 1.25f;

            playerPositionOnEnter = StateMachine.Player.transform.position;

            ResetVerticalVelocity();

            StateMachine.ReusableData.Gravity *= 2f;
            
            groundCheckTimer.Start();
        }

        public override void Exit()
        {
            base.Exit();
            
            StateMachine.Player.Rigidbody.linearVelocity = new Vector3(StateMachine.Player.Rigidbody.linearVelocity.x, 0f, StateMachine.Player.Rigidbody.linearVelocity.z);
            
            StateMachine.ReusableData.CanJumpAgain = true;

            StopAnimation(StateMachine.Player.AnimationData.FallParameterHash);
        }

        public override void Update()
        {
            base.Update();
            
            groundCheckTimer.Tick(Time.deltaTime);
            if (groundCheckTimer.IsFinished)
            {
                if (CheckGroundStatus())
                {
                    StateMachine.ChangeState(StateMachine.LightLandingState);
                }
                
                groundCheckTimer.Reset();
                // groundCheckTimer.Start();
            }
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();

            LimitVerticalVelocity();
            
            if (StateMachine.Player.LedgeDetector.DetectLedge())
            {
                StateMachine.ChangeState(StateMachine.LedgeGrabbingState);

                return;
            }
        }

        private void LimitVerticalVelocity()
        {
            Vector3 playerVerticalVelocity = GetPlayerVerticalVelocity();

            if (playerVerticalVelocity.y >= -AirborneData.FallData.FallSpeedLimit)
            {
                return;
            }

            Vector3 limitedVelocityForce = new Vector3(0f, -AirborneData.FallData.FallSpeedLimit - playerVerticalVelocity.y, 0f);

            StateMachine.Player.Rigidbody.AddForce(limitedVelocityForce, ForceMode.VelocityChange);
        }
        
        private bool CheckGroundStatus()
        {
            return Physics.Raycast(StateMachine.Player.ResizableCapsuleCollider.CapsuleColliderData.ColliderCenterInLocalSpace
                            + StateMachine.Player.transform.position,
                Vector3.down, out var _, GroundedData.GroundToFallRayDistance, StateMachine.Player.LayerData.GroundLayer);
        }

        protected override void ResetSprintState()
        {
        }

        protected override void OnContactWithGround(Collider collider)
        {
            var fallDistance = playerPositionOnEnter.y - StateMachine.Player.transform.position.y;

            if (fallDistance < AirborneData.FallData.MinimumDistanceToBeConsideredHardFall)
            {
                StateMachine.ChangeState(StateMachine.LightLandingState);

                return;
            }

            if (StateMachine.ReusableData.ShouldWalk && !StateMachine.ReusableData.ShouldSprint || StateMachine.ReusableData.MovementInput == Vector2.zero)
            {
                StateMachine.ChangeState(StateMachine.HardLandingState);

                return;
            }

            StateMachine.ChangeState(StateMachine.RollingState);

        }
    }
}