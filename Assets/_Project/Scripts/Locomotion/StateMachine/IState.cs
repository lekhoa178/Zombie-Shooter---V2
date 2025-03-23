using UnityEngine;

namespace LocomotionSystem
{
    public interface IState
    {
        public void Enter();
        public void Exit();
        public void HandleInput();
        public void Update();
        public void PhysicsUpdate();
        public void OnTriggerEnter(Collider collider);
        public void OnTriggerExit(Collider collider);
        public void OnAnimationEnterEvent();
        public void OnAnimationExitEvent();
        public void OnAnimationTransitionEvent();
        public void OnAnimationWithIntParameterEvent(int value);
        
        // ** Animation State Machine Events ** //
        public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex);
        public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex);
        public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex);
        public void OnAnimatorIK(int layerIndex);
        public void OnDrawGizmos();
    }
}