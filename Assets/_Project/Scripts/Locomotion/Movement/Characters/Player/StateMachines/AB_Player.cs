using UnityEngine;

namespace LocomotionSystem.StateMachines
{
    public class AB_Player : StateMachineBehaviour
    {
        private Player playerAnimator;

        // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (playerAnimator == null)
            {
                playerAnimator = animator.GetComponent<Player>();
            }
            
            if (animator.IsInTransition(layerIndex))
            {
                return;
            }
            
            playerAnimator.MovementStateMachine.CurrentState?.OnStateEnter(animator, stateInfo, layerIndex);
        }
        
        // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (playerAnimator == null)
            {
                playerAnimator = animator.GetComponent<Player>();
            }
            
            if (animator.IsInTransition(layerIndex))
            {
                return;
            }
            
            playerAnimator.MovementStateMachine.CurrentState?.OnStateUpdate(animator, stateInfo, layerIndex);
        }

        // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (playerAnimator == null)
            {
                playerAnimator = animator.GetComponent<Player>();
            }
            
            if (animator.IsInTransition(layerIndex))
            {
                return;
            }
            
            playerAnimator.MovementStateMachine.CurrentState?.OnStateExit(animator, stateInfo, layerIndex);
        }
    }
}