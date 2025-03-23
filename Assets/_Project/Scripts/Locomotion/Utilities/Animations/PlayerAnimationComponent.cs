using UnityEngine;

namespace LocomotionSystem
{
    public class PlayerAnimationComponent : MonoBehaviour
    {
        public Animator Animator => player.Animator;
        public PlayerAnimationData AnimationData => player.AnimationData;
        
        private Player player;

        private void Awake()
        {
            player = transform.parent ? transform.parent.GetComponent<Player>() : GetComponent<Player>();
        }

        public void TriggerOnMovementStateAnimationEnterEvent()
        {
            // if (IsInAnimationTransition())
            // {
            //     return;
            // }

            player.OnMovementStateAnimationEnterEvent();
        }

        public void TriggerOnMovementStateAnimationExitEvent()
        {
            // if (IsInAnimationTransition())
            // {
            //     return;
            // }

            player.OnMovementStateAnimationExitEvent();
        }

        public void TriggerOnMovementStateAnimationTransitionEvent()
        {
            // if (IsInAnimationTransition())
            // {
            //     return;
            // }

            player.OnMovementStateAnimationTransitionEvent();
        }
        
        public void TriggerOnMovementStateAnimationWithIntParameterEvent(int value)
        {
            player.OnMovementStateAnimationWithIntParameterEvent(value);
        }

        // *** Animation Events - Combat State ***
        public void GetCloseToTarget()
        {
            (player.MovementStateMachine.CurrentState as PlayerCombatState)?.GetCloseToTarget();;
        }
        
        // ** Animation Event - Combat State ** //
        public void FaceTarget()
        {
            (player.MovementStateMachine.CurrentState as PlayerCombatState)?.FaceTarget();;
        }
        
        // ** Animation Event - Normal Attacking State ** //
        public void StartComboFrame()
        {
            (player.MovementStateMachine.CurrentState as PlayerNormalAttackingState)?.StartComboFrame();;
        }

        // ** Animation Event - Normal Attacking State ** //
        public void EndComboFrame()
        {
            (player.MovementStateMachine.CurrentState as PlayerNormalAttackingState)?.EndComboFrame();;
        }

        // ** Animation Event - Normal Attacking State ** //
        public void StartAttackFrame()
        {
            (player.MovementStateMachine.CurrentState as PlayerNormalAttackingState)?.StartAttackFrame();;
        }
        
        // ** Animation Event - Normal Attacking State ** //
        public void EndAttackFrame()
        {
            (player.MovementStateMachine.CurrentState as PlayerNormalAttackingState)?.EndAttackFrame();;
        }

        public void FinishAttack()
        {
            (player.MovementStateMachine.CurrentState as PlayerNormalAttackingState)?.FinishAttack();
        }

        // ** Animation Event - Combat State ** //
        public void MoveForward(float distance)
        {
            // Default duration of 0.2 seconds if only distance is provided
            (player.MovementStateMachine.CurrentState as PlayerCombatState)?.MoveForward(distance, 0.2f);
        }

        // ** Animation Event - Combat State ** //
        public void MoveTowardsTarget(float maxDistance)
        {
            // Default duration of 0.2 seconds if only distance is provided
            (player.MovementStateMachine.CurrentState as PlayerCombatState)?.MoveTowardsTarget(maxDistance, 0.2f);
        }


        // *** Utility Methods ***
        public void SetAnimationFloatParameter(int parameterHash, float value, float dampTime = 0f, float deltaTime = 0f)
        {
            Animator.SetFloat(parameterHash, value, dampTime, deltaTime);
        }
        
        public void SetAnimationBoolParameter(int parameterHash, bool value)
        {
            Animator.SetBool(parameterHash, value);
        }
        
        public void SetAnimationIntParameter(int parameterHash, int value)
        {
            Animator.SetInteger(parameterHash, value);
        }

        private bool IsInAnimationTransition(int layerIndex = 0)
        {
            return player.Animator.IsInTransition(layerIndex);
        }
    }
}