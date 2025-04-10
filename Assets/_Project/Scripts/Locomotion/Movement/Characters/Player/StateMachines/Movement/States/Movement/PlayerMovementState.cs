using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LocomotionSystem
{
    public class PlayerMovementState : PlayerState
    {
        protected readonly PlayerGroundedData GroundedData;
        protected readonly PlayerAirborneData AirborneData;

        private float deltaRotation;

        protected PlayerMovementState(PlayerMovementStateMachine playerMovementStateMachine) : base(
            playerMovementStateMachine)
        {
            GroundedData = StateMachine.Player.Data.GroundedData;
            AirborneData = StateMachine.Player.Data.AirborneData;

            InitializeData();
        }

        public override void Enter()
        {
            base.Enter();
            
            StateMachine.Player.SocketManager?.ClearAllWeapons();
        }

        public override void PhysicsUpdate()
        {
            Move();
        }
        
        private void InitializeData()
        {
            SetBaseCameraRecenterData();

            SetBaseRotationData();
        }

        protected void SetBaseCameraRecenterData()
        {
            StateMachine.ReusableData.SidewaysCameraRecenteringData = GroundedData.SidewaysCameraRecenteringData;
            StateMachine.ReusableData.BackwardsCameraRecenteringData = GroundedData.BackwardsCameraRecenteringData;
        }

        protected void SetBaseRotationData()
        {
            StateMachine.ReusableData.RotationData = GroundedData.BaseRotationData;

            StateMachine.ReusableData.TimeToReachTargetRotation =
                StateMachine.ReusableData.RotationData.TargetRotationReachTime;
        }
        

        protected override void AddInputActionsCallbacks()
        {
            base.AddInputActionsCallbacks();
            
            StateMachine.Player.Input.PlayerActions.WalkToggle.started += OnWalkToggleStarted;
            StateMachine.Player.Input.PlayerActions.Jump.started += OnJumpStarted;
            StateMachine.Player.Input.PlayerActions.Look.started += OnMouseMovementStarted;

            StateMachine.Player.Input.PlayerActions.Movement.performed += OnMovementPerformed;
            StateMachine.Player.Input.PlayerActions.Movement.canceled += OnMovementCanceled;
        }

        protected override void RemoveInputActionsCallbacks()
        {
            base.RemoveInputActionsCallbacks();
            
            StateMachine.Player.Input.PlayerActions.WalkToggle.started -= OnWalkToggleStarted;
            StateMachine.Player.Input.PlayerActions.Jump.started -= OnJumpStarted;
            StateMachine.Player.Input.PlayerActions.Look.started -= OnMouseMovementStarted;

            StateMachine.Player.Input.PlayerActions.Movement.performed -= OnMovementPerformed;
            StateMachine.Player.Input.PlayerActions.Movement.canceled -= OnMovementCanceled;
        }

        protected virtual void OnWalkToggleStarted(InputAction.CallbackContext context)
        {
            StateMachine.ReusableData.ShouldWalk = !StateMachine.ReusableData.ShouldWalk;
        }

        private void OnMouseMovementStarted(InputAction.CallbackContext context)
        {
            UpdateCameraRecenteringState(StateMachine.ReusableData.MovementInput);
        }

        protected virtual void OnMovementPerformed(InputAction.CallbackContext context)
        {
            UpdateCameraRecenteringState(context.ReadValue<Vector2>());
        }

        protected virtual void OnMovementCanceled(InputAction.CallbackContext context)
        {
            DisableCameraRecentering();
        }
        
        protected virtual void OnJumpStarted(InputAction.CallbackContext context)
        {
            StateMachine.ReusableData.JumpCount = StateMachine.Player.Data.AirborneData.JumpData.MaxJumpCount - 1;
            StateMachine.Player.Animator.SetFloat(StateMachine.Player.AnimationData.JumpIndexHash, 0);
            
            StateMachine.ChangeState(StateMachine.JumpingState);
        }

        protected virtual void Move()
        {
            if (StateMachine.ReusableData.MovementInput == Vector2.zero || StateMachine.ReusableData.MovementSpeedModifier == 0f)
            {
                return;
            }

            Vector3 movementDirection = GetMovementInputDirection();

            float targetRotationYAngle = Rotate(movementDirection);

            Vector3 targetRotationDirection = GetTargetRotationDirection(targetRotationYAngle);

            float movementSpeed = GetMovementSpeed();

            Vector3 currentPlayerHorizontalVelocity = GetPlayerHorizontalVelocity();

            StateMachine.Player.Rigidbody.AddForce(
                targetRotationDirection * movementSpeed - currentPlayerHorizontalVelocity, ForceMode.VelocityChange);
        }


        private float Rotate(Vector3 direction)
        {
            float directionAngle = UpdateTargetRotation(direction);

            RotateTowardsTargetRotation();

            return directionAngle;
        }

        protected override void RotateTowardsTargetRotation(bool instantRotation = false)
        {
            base.RotateTowardsTargetRotation();
            
            float currentYAngle = StateMachine.Player.Rigidbody.rotation.eulerAngles.y;
            
            deltaRotation = -Mathf.DeltaAngle(StateMachine.ReusableData.CurrentTargetRotation.y, currentYAngle);
            StateMachine.Player.Animator.SetFloat(
                StateMachine.Player.AnimationData.RotationHash,
                deltaRotation,
                0.14f,
                Time.deltaTime);
        }
        
        protected float GetMovementSpeed(bool shouldConsiderSlopes = true)
        {
            float movementSpeed = GroundedData.BaseSpeed * StateMachine.ReusableData.MovementSpeedModifier;

            if (shouldConsiderSlopes)
            {
                movementSpeed *= StateMachine.ReusableData.MovementOnSlopesSpeedModifier;
            }

            return movementSpeed;
        }
        
        protected void UpdateCameraRecenteringState(Vector2 movementInput)
        {
            if (movementInput == Vector2.zero)
            {
                return;
            }

            if (movementInput == Vector2.up)
            {
                DisableCameraRecentering();

                return;
            }

            float cameraVerticalAngle = StateMachine.Player.MainCameraTransform.eulerAngles.x;

            if (cameraVerticalAngle >= 270f)
            {
                cameraVerticalAngle -= 360f;
            }

            cameraVerticalAngle = Mathf.Abs(cameraVerticalAngle);

            if (movementInput == Vector2.down)
            {
                SetCameraRecenteringState(cameraVerticalAngle,
                    StateMachine.ReusableData.BackwardsCameraRecenteringData);

                return;
            }

            SetCameraRecenteringState(cameraVerticalAngle, StateMachine.ReusableData.SidewaysCameraRecenteringData);
        }

        protected void SetCameraRecenteringState(float cameraVerticalAngle,
            List<PlayerCameraRecenteringData> cameraRecenteringData)
        {
            foreach (PlayerCameraRecenteringData recenteringData in cameraRecenteringData)
            {
                if (!recenteringData.IsWithinRange(cameraVerticalAngle))
                {
                    continue;
                }

                EnableCameraRecentering(recenteringData.WaitTime, recenteringData.RecenteringTime);

                return;
            }

            DisableCameraRecentering();
        }

        protected void EnableCameraRecentering(float waitTime = -1f, float recenteringTime = -1f)
        {
            float movementSpeed = GetMovementSpeed();

            if (movementSpeed == 0f)
            {
                movementSpeed = GroundedData.BaseSpeed;
            }

            StateMachine.Player.CameraRecenteringUtility.EnableRecentering(waitTime, recenteringTime,
                GroundedData.BaseSpeed, movementSpeed);
        }

        protected void DisableCameraRecentering()
        {
            StateMachine.Player.CameraRecenteringUtility.DisableRecentering();
        }
    }
}