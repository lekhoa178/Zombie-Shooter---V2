using System.Collections.Generic;
using Locomotion;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityUtils;

namespace LocomotionSystem
{
    public class PlayerChargeAttackingState : PlayerCombatState
    {
        private bool isCombo = false;

        public PlayerChargeAttackingState(PlayerMovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
        {
        }

        public override void Enter()
        {
            base.Enter();

            isCombo = false;

            StateMachine.ReusableData.MovementSpeedModifier = 0f;
            
            ResetVelocity();
            
            StateMachine.Player.Animator.applyRootMotion = CheckGroundStatus();

            if (!StateMachine.Player.TargetDetector.GetCurrentTarget())
            {
                if (StateMachine.ReusableData.MovementInput != Vector2.zero)
                    StateMachine.Player.transform.rotation = Quaternion.Euler(0, GetMovementInputRotationAngle(), 0);
            }
            
            IsAttackFrame = false;
            
            StartAnimation(StateMachine.Player.AnimationData.ChargeAttackParameterHash);
        }

        public override void Exit()
        {
            base.Exit();
            
            StateMachine.Player.Animator.applyRootMotion = false;

            StopAnimation(StateMachine.Player.AnimationData.ChargeAttackParameterHash);
        }

        protected override void AddInputActionsCallbacks()
        {
            base.AddInputActionsCallbacks();
            
            StateMachine.Player.Input.PlayerActions.ChargeAttack.canceled += OnChargeAttackCancelled;
        }

        protected override void RemoveInputActionsCallbacks()
        {
            base.RemoveInputActionsCallbacks();
            
            StateMachine.Player.Input.PlayerActions.ChargeAttack.canceled -= OnChargeAttackCancelled;
        }
        
        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
        }

        private void OnChargeAttackCancelled(InputAction.CallbackContext obj)
        {
            StateMachine.ChangeState(StateMachine.IdlingState);
        }

        protected override void OnNormalAttackPerformed(InputAction.CallbackContext obj)
        {
        }
        
    }
}