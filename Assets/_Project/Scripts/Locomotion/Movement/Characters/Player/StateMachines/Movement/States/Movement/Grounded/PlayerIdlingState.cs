using UnityEngine;

namespace LocomotionSystem
{
    public class PlayerIdlingState : PlayerGroundedState
    {
        public PlayerIdlingState(PlayerMovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
        {
        }

        public override void Enter()
        {
            StateMachine.ReusableData.MovementSpeedModifier = 0f;

            StateMachine.ReusableData.BackwardsCameraRecenteringData = GroundedData.IdleData.BackwardsCameraRecenteringData;

            base.Enter();

            StartAnimation(StateMachine.Player.AnimationData.IdleParameterHash);

            StateMachine.ReusableData.CurrentJumpForce = AirborneData.JumpData.StationaryForce;

            ResetVelocity();
        }

        public override void Exit()
        {
            base.Exit();

            StopAnimation(StateMachine.Player.AnimationData.IdleParameterHash);
        }

        public override void Update()
        {
            base.Update();

            if (StateMachine.ReusableData.MovementInput == Vector2.zero)
            {
                return;
            }

            OnMove();
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();

            if (!IsMovingHorizontally())
            {
                return;
            }

            ResetVelocity();
        }

        public override void OnAnimatorIK(int layerIndex)
        {
            base.OnAnimatorIK(layerIndex);

            float distanceToGround = GroundedData.IdleData.DistanceToGround;
            
            if (StateMachine.CurrentState is PlayerMovingState)
            {
                return;
            }

            if (!StateMachine.Player.Animator) return;
        
            StateMachine.Player.Animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
            StateMachine.Player.Animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1);
            StateMachine.Player.Animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
            StateMachine.Player.Animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1);

            RaycastHit hit;
            if (Physics.Raycast(StateMachine.Player.Animator.GetIKPosition(AvatarIKGoal.LeftFoot) + Vector3.up * 2f + Vector3.forward * 0.1f, Vector3.down, out hit, distanceToGround + 2f, StateMachine.Player.LayerData.GroundLayer))
            {
                StateMachine.Player.Animator.SetIKPosition(AvatarIKGoal.LeftFoot, hit.point + new Vector3(0, distanceToGround, -0.1f));
                StateMachine.Player.Animator.SetIKRotation(AvatarIKGoal.LeftFoot, Quaternion.LookRotation(StateMachine.Player.transform.forward, hit.normal) * Quaternion.Euler(0, 0, 0));
            }

            if (Physics.Raycast(StateMachine.Player.Animator.GetIKPosition(AvatarIKGoal.RightFoot) + Vector3.up * 2f + Vector3.forward * 0.1f, Vector3.down, out hit, distanceToGround + 2f, StateMachine.Player.LayerData.GroundLayer))
            {
                StateMachine.Player.Animator.SetIKPosition(AvatarIKGoal.RightFoot, hit.point + new Vector3(0, distanceToGround, -0.1f));
                StateMachine.Player.Animator.SetIKRotation(AvatarIKGoal.RightFoot, Quaternion.LookRotation(StateMachine.Player.transform.forward, hit.normal) * Quaternion.Euler(0, 30, 0));
            }
        }
    }
}