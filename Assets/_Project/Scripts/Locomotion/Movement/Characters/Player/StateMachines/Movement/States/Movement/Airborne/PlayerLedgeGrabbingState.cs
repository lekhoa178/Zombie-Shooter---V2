using UnityEngine;
using UnityUtils;

namespace LocomotionSystem
{
    public class PlayerLedgeGrabbingState : PlayerAirborneState
    {
        
        private Vector3 leftHandPosition, rightHandPosition, leftFootPosition, rightFootPosition = Vector3.zero;
        
        private readonly GameObject lHand;
        private readonly GameObject rHand;
        
        private CountdownTimer ledgeDelayTimer;

        public PlayerLedgeGrabbingState(PlayerMovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
        {
            lHand = StateMachine.Player.Animator.GetBoneTransform(HumanBodyBones.LeftHand).gameObject;
            rHand = StateMachine.Player.Animator.GetBoneTransform(HumanBodyBones.RightHand).gameObject;
            
            ledgeDelayTimer = new CountdownTimer(0.4f);
        }
        
        public override void Enter()
        {
            base.Enter();
            
            StartAnimation(StateMachine.Player.AnimationData.LedgeGrabParameterHash);

            ResetVelocity();
            
            StateMachine.ReusableData.MovementSpeedModifier = 0f;
            
            StateMachine.ReusableData.JumpCount = 1;
            StateMachine.Player.Animator.SetFloat(StateMachine.Player.AnimationData.JumpIndexHash, 1f);

            // rotate player to the normal of the hit surface
            
            var hitSurface = StateMachine.Player.LedgeDetector.HitSurface;
            StateMachine.Player.transform.rotation = Quaternion.LookRotation(-hitSurface.normal);
            
            // Debug.DrawLine(hitSurface.point, hitSurface.point + hitSurface.normal, Color.green, 5f);

            leftHandPosition = hitSurface.point + StateMachine.Player.transform.forward * 0.25f;
            rightHandPosition = hitSurface.point + StateMachine.Player.transform.forward * 0.25f;
            
            ledgeDelayTimer.Start();
        }

        public override void Exit()
        {
            base.Exit();
            
            StopAnimation(StateMachine.Player.AnimationData.LedgeGrabParameterHash);
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();
            
            ResetVelocity();
        }

        public override void Update()
        {
            base.Update();
            
            ledgeDelayTimer.Tick(Time.deltaTime);
        }

        protected override void Move()
        {
            Vector2 movementInput = StateMachine.ReusableData.MovementInput;

            if (!ledgeDelayTimer.IsFinished) return;
            
            // if (movementInput.y > 0.1f)
            // {
            //     ledgeDelayTimer.Reset();
            //     
            //     StateMachine.ReusableData.JumpForceMultiplier = 0.5f;
            //     StateMachine.ChangeState(StateMachine.JumpingState);
            // }
            
        }

        public override void OnAnimatorIK(int layerIndex)
        {
            base.OnAnimatorIK(layerIndex);

            //IK Position of Feet and Hands
            StateMachine.Player.Animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
            CalculateIKPositions(AvatarIKGoal.LeftHand, ref leftHandPosition);
            StateMachine.Player.Animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
            CalculateIKPositions(AvatarIKGoal.RightHand, ref rightHandPosition);
        }
        
        /// <summary>
        /// Calculates the IK Position to place the limb
        /// </summary>
        void CalculateIKPositions(AvatarIKGoal ikGoal, ref Vector3 ikPosition)
        {
            Vector3 targetIKPosition = StateMachine.Player.Animator.GetIKPosition(ikGoal);

            if (ikPosition != Vector3.zero)
            {
                Vector3 localIkPosition = StateMachine.Player.transform.InverseTransformPoint(ikPosition);
                targetIKPosition = StateMachine.Player.transform.InverseTransformPoint(targetIKPosition);
                targetIKPosition.z = localIkPosition.z;
                targetIKPosition = StateMachine.Player.transform.TransformPoint(targetIKPosition);
            }

            StateMachine.Player.Animator.SetIKPosition(ikGoal, targetIKPosition);
        }

        protected override void ApplyGravity()
        {
        }
    }
}