using System.Collections;
using UnityEngine;

namespace LocomotionSystem
{
    /// <summary>
    /// Base class for all movement states with pause/resume functionality
    /// </summary>
    public abstract class BaseMovementState
    {
        protected readonly Player Player;
        protected readonly PlayerMovementStateMachine StateMachine;
        
        // Flags for state management
        private bool _isPaused = false;
        
        protected BaseMovementState(Player player, PlayerMovementStateMachine stateMachine)
        {
            Player = player;
            StateMachine = stateMachine;
        }
        
        // Core state methods - these should be overridden by subclasses
        public virtual void Enter() { }
        public virtual void Exit() { }
        public virtual void HandleInput() { }
        public virtual void Update() 
        {
            // Only update if not paused
            if (_isPaused) return;
        }
        public virtual void PhysicsUpdate() 
        {
            // Only update physics if not paused
            if (_isPaused) return;
        }
        
        // Additional handlers
        public virtual void OnAnimationEnterEvent() { }
        public virtual void OnAnimationExitEvent() { }
        public virtual void OnAnimationTransitionEvent() { }
        public virtual void OnAnimatorIK(int layerIndex) { }
        public virtual void OnTriggerEnter(Collider other) { }
        public virtual void OnTriggerExit(Collider other) { }
        public virtual void OnDrawGizmos() { }
        
        // Pause and resume functionality for stun/knockback
        public virtual void OnPauseState()
        {
            _isPaused = true;
            
            // Child classes can override to do additional cleanup when paused
            HandlePauseState();
        }
        
        public virtual void OnResumeState()
        {
            _isPaused = false;
            
            // Child classes can override to do additional setup when resumed
            HandleResumeState();
        }
        
        // Protected methods for subclasses to implement
        protected virtual void HandlePauseState() { }
        protected virtual void HandleResumeState() { }
        
        // Helper to check if state is currently paused
        public bool IsPaused() => _isPaused;
        
        // Helper method for starting coroutines from a state
        protected Coroutine StartCoroutine(IEnumerator coroutine)
        {
            return Player.StartStateCoroutine(coroutine);
        }
    }
} 