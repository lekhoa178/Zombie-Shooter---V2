using UnityEngine;

namespace LocomotionSystem
{
    public abstract class StateMachine
    {
        public IState DefaultState { get; protected set; }
        
        public IState CurrentState { get; private set; }

        public void ChangeState(IState newState)
        {
            CurrentState?.Exit();

            CurrentState = newState;

            CurrentState.Enter();
        }
        
        public void HandleInput()
        {
            CurrentState?.HandleInput();
        }

        public void Update()
        {
            CurrentState?.Update();
        }

        public void PhysicsUpdate()
        {
            CurrentState?.PhysicsUpdate();
        }

        public void OnTriggerEnter(Collider collider)
        {
            CurrentState?.OnTriggerEnter(collider);
        }

        public void OnTriggerExit(Collider collider)
        {
            CurrentState?.OnTriggerExit(collider);
        }
        
        public void OnDrawGizmos()
        {
            CurrentState?.OnDrawGizmos();
        }
        
        public void OnAnimatorIK(int layerIndex)
        {
            CurrentState?.OnAnimatorIK(layerIndex);
        }

        public void OnAnimationEnterEvent()
        {
            CurrentState?.OnAnimationEnterEvent();
        }

        public void OnAnimationExitEvent()
        {
            CurrentState?.OnAnimationExitEvent();
        }

        public void OnAnimationTransitionEvent()
        {
            CurrentState?.OnAnimationTransitionEvent();
        }

        public void OnAnimationWithIntParameterEvent(int value)
        {
            CurrentState?.OnAnimationWithIntParameterEvent(value);
        }

    }
}