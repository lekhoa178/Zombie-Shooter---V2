using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityUtils;

namespace LocomotionSystem
{
    public class PlayerState : IState
    {
        protected readonly PlayerMovementStateMachine StateMachine;

        protected PlayerState(PlayerMovementStateMachine playerMovementStateMachine)
        {
            StateMachine = playerMovementStateMachine;
        }

        public virtual void Enter()
        {
            Debug.Log("Entered " + GetType().Name);

            StateMachine.Player.AnimationData.StopInteractionAnimation(StateMachine.Player.Animator, 0);

            AddInputActionsCallbacks();
        }

        public virtual void Exit()
        {
            RemoveInputActionsCallbacks();
        }


        public virtual void HandleInput()
        {
            ReadMovementInput();
        }

        public virtual void Update()
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                OnNormalAttackPerformed(new InputAction.CallbackContext());
            }

            if (StateMachine.ReusableData.ResetComboTimer.IsRunning)
            {
                StateMachine.ReusableData.ResetComboTimer.Tick(Time.deltaTime);
            }
            
            if (StateMachine.ReusableData.AttackCooldownTimer.IsRunning)
            {
                StateMachine.ReusableData.AttackCooldownTimer.Tick(Time.deltaTime);
            }

            // if (StateMachine.ReusableData.ResetComboTimer.IsFinished)
            // {
            //     Debug.Log("Combo reset");
            //     StateMachine.ReusableData.CurrentComboIndex = 0;
            // }
        }

        public virtual void PhysicsUpdate()
        {
        }

        public virtual void OnTriggerEnter(Collider collider)
        {
            if (!StateMachine.Player.LayerData.IsGroundLayer(collider.gameObject.layer)) return;

            OnContactWithGround(collider);
        }

        public virtual void OnTriggerExit(Collider collider)
        {
            if (!StateMachine.Player.LayerData.IsGroundLayer(collider.gameObject.layer)) return;

            OnContactWithGroundExited(collider);
        }

        public virtual void OnAnimationEnterEvent()
        {
        }

        public virtual void OnAnimationExitEvent()
        {
        }

        public virtual void OnAnimationTransitionEvent()
        {
        }

        public virtual void OnAnimationWithIntParameterEvent(int value)
        {
        }

        public virtual void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
        }

        public virtual void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
        }

        public virtual void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
        }

        public virtual void OnAnimatorIK(int layerIndex)
        {
        }

        public virtual void OnDrawGizmos()
        {
        }

        protected virtual void AddInputActionsCallbacks()
        {
            StateMachine.Player.Input.PlayerActions.NormalAttack.performed += OnNormalAttackPerformed;
            StateMachine.Player.Input.PlayerActions.ChargeAttack.performed += OnChargeAttackPerformed;
            StateMachine.Player.Input.PlayerActions.Dash.started += OnDashStarted;
        }

        protected virtual void RemoveInputActionsCallbacks()
        {
            StateMachine.Player.Input.PlayerActions.NormalAttack.performed -= OnNormalAttackPerformed;
            StateMachine.Player.Input.PlayerActions.ChargeAttack.performed -= OnChargeAttackPerformed;
            StateMachine.Player.Input.PlayerActions.Dash.started -= OnDashStarted;
        }
        
        protected virtual void OnMove()
        {
            if (StateMachine.ReusableData.ShouldSprint)
            {
                if (StateMachine.Player.Data.GroundedData.SprintData.HasTurningState)
                {
                }
                
                StateMachine.ChangeState(StateMachine.SprintingState);

                return;
            }

            if (StateMachine.ReusableData.ShouldWalk)
            {
                if (StateMachine.Player.Data.GroundedData.WalkData.HasTurningState)
                {
                }

                StateMachine.ChangeState(StateMachine.WalkingState);

                return;
            }

            if (StateMachine.Player.Data.GroundedData.RunData.HasTurningState)
            {
            }
            
            StateMachine.ChangeState(StateMachine.RunningState);
        }

        private void ReadMovementInput()
        {
            StateMachine.ReusableData.MovementInput =
                StateMachine.Player.Input.PlayerActions.Movement.ReadValue<Vector2>();
        }
        
        protected void StartAnimation(int animationHash)
        {
            StateMachine.Player.Animator.SetBool(animationHash, true);
        }

        protected void StopAnimation(int animationHash)
        {
            StateMachine.Player.Animator.SetBool(animationHash, false);
        }
        
        protected virtual void OnDashStarted(InputAction.CallbackContext context)
        {
            StateMachine.ChangeState(StateMachine.DashingState);
        }
        
        protected virtual void OnNormalAttackPerformed(InputAction.CallbackContext obj)
        {
            Debug.Log("Normal attack performed");
            
            if (StateMachine.ReusableData.IsOnCooldown)
            {
                Debug.Log($"Attack on cooldown: {StateMachine.ReusableData.AttackCooldownTimer.Time:F2}s remaining");
                return;
            }
            
            // Get the actual number of attack configurations in the ComboConfig
            int attackConfigCount = StateMachine.Player.Data.ComboConfig?.AttackConfigs?.Length ?? 1;
            
            // Make sure we don't divide by zero
            if (attackConfigCount <= 0) attackConfigCount = 1;
            
            StateMachine.ReusableData.CurrentComboIndex = (StateMachine.ReusableData.CurrentComboIndex + 1) % attackConfigCount;
            
             // Reset to first attack if we're at the last attack in the sequence
            // if (StateMachine.ReusableData.CurrentComboIndex >= attackConfigCount - 1)
            // {
            //     StateMachine.ReusableData.CurrentComboIndex = 0;
            // }
            
            // Change to the attacking state
            StateMachine.ChangeState(StateMachine.NormalAttackingState);
        }
        
        private void OnChargeAttackPerformed(InputAction.CallbackContext obj)
        {
            // StateMachine.ChangeState(StateMachine.ChargeAttackingState);
        }

        protected Vector3 GetMovementInputDirection()
        {
            return new Vector3(StateMachine.ReusableData.MovementInput.x, 0f,
                StateMachine.ReusableData.MovementInput.y);
        }

        protected float GetMovementInputRotationAngle(bool shouldConsiderCameraRotation = true)
        {
            var inputRotation =
                Mathf.Atan2(StateMachine.ReusableData.MovementInput.x, StateMachine.ReusableData.MovementInput.y) *
                Mathf.Rad2Deg;

            if (shouldConsiderCameraRotation)
            {
                inputRotation += StateMachine.Player.MainCameraTransform.eulerAngles.y;
            }

            return inputRotation;
        }

        protected float UpdateTargetRotation(Vector3 direction, bool shouldConsiderCameraRotation = true)
        {
            float directionAngle = GetDirectionAngle(direction);

            if (shouldConsiderCameraRotation)
            {
                directionAngle = AddCameraRotationToAngle(directionAngle);
            }

            if (!Mathf.Approximately(directionAngle, StateMachine.ReusableData.CurrentTargetRotation.y))
            {
                UpdateTargetRotationData(directionAngle);
            }

            return directionAngle;
        }

        private float GetDirectionAngle(Vector3 direction)
        {
            float directionAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

            if (directionAngle < 0f)
            {
                directionAngle += 360f;
            }

            return directionAngle;
        }

        private float AddCameraRotationToAngle(float angle)
        {
            angle += StateMachine.Player.MainCameraTransform.eulerAngles.y;

            if (angle > 360f)
            {
                angle -= 360f;
            }

            return angle;
        }

        private void UpdateTargetRotationData(float targetAngle)
        {
            StateMachine.ReusableData.CurrentTargetRotation.y = targetAngle;

            StateMachine.ReusableData.DampedTargetRotationPassedTime.y = 0f;
        }

        protected virtual void RotateTowardsTargetRotation(bool instantRotation = false)
        {
            float currentYAngle = StateMachine.Player.Rigidbody.rotation.eulerAngles.y;
            
            if (Mathf.Approximately(currentYAngle, StateMachine.ReusableData.CurrentTargetRotation.y))
            {
                return;
            }

            float smoothedYAngle;
            
            if (!instantRotation)
            {

                 smoothedYAngle = Mathf.SmoothDampAngle(currentYAngle,
                    StateMachine.ReusableData.CurrentTargetRotation.y,
                    ref StateMachine.ReusableData.DampedTargetRotationCurrentVelocity.y,
                    StateMachine.ReusableData.TimeToReachTargetRotation.y -
                    StateMachine.ReusableData.DampedTargetRotationPassedTime.y);
            }
            else
            {
                smoothedYAngle = StateMachine.ReusableData.CurrentTargetRotation.y;
            }

            StateMachine.ReusableData.DampedTargetRotationPassedTime.y += Time.deltaTime;

            Quaternion targetRotation = Quaternion.Euler(0f, smoothedYAngle, 0f);

            StateMachine.Player.Rigidbody.MoveRotation(targetRotation);
        }

        protected Vector3 GetTargetRotationDirection(float targetRotationAngle)
        {
            return Quaternion.Euler(0f, targetRotationAngle, 0f) * Vector3.forward;
        }

        protected Vector3 GetPlayerHorizontalVelocity()
        {
            Vector3 playerHorizontalVelocity = StateMachine.Player.Rigidbody.linearVelocity;

            playerHorizontalVelocity.y = 0f;

            return playerHorizontalVelocity;
        }

        protected Vector3 GetPlayerVerticalVelocity()
        {
            return new Vector3(0f, StateMachine.Player.Rigidbody.linearVelocity.y, 0f);
        }

        protected virtual void OnContactWithGround(Collider collider)
        {
        }

        protected virtual void OnContactWithGroundExited(Collider collider)
        {
        }

        protected void ResetVelocity()
        {
            StateMachine.Player.Rigidbody.linearVelocity = Vector3.zero;
        }

        protected void ResetVerticalVelocity()
        {
            Vector3 playerHorizontalVelocity = GetPlayerHorizontalVelocity();

            StateMachine.Player.Rigidbody.linearVelocity = playerHorizontalVelocity;
        }

        protected void ResetHorizontalVelocity()
        {
            Vector3 playerVerticalVelocity = GetPlayerVerticalVelocity();

            StateMachine.Player.Rigidbody.linearVelocity = playerVerticalVelocity;
        }

        protected void DecelerateHorizontally()
        {
            Vector3 playerHorizontalVelocity = GetPlayerHorizontalVelocity();

            StateMachine.Player.Rigidbody.AddForce(
                -playerHorizontalVelocity * StateMachine.ReusableData.MovementDecelerationForce,
                ForceMode.Acceleration);
        }

        protected void DecelerateVertically()
        {
            Vector3 playerVerticalVelocity = GetPlayerVerticalVelocity();

            StateMachine.Player.Rigidbody.AddForce(
                -playerVerticalVelocity * StateMachine.ReusableData.MovementDecelerationForce, ForceMode.Acceleration);
        }

        protected bool IsMovingHorizontally(float minimumMagnitude = 0.1f)
        {
            Vector3 playerHorizontaVelocity = GetPlayerHorizontalVelocity();

            Vector2 playerHorizontalMovement = new Vector2(playerHorizontaVelocity.x, playerHorizontaVelocity.z);

            return playerHorizontalMovement.magnitude > minimumMagnitude;
        }

        protected bool IsMovingUp(float minimumVelocity = 0.1f)
        {
            return GetPlayerVerticalVelocity().y > minimumVelocity;
        }

        protected bool IsMovingDown(float minimumVelocity = 0.1f)
        {
            return GetPlayerVerticalVelocity().y < -minimumVelocity;
        }
    }
}