using System;
using System.Collections;
using System.Collections.Generic;
using Climbing;
using MoreMountains.Feedbacks;
using PowerTree;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LocomotionSystem
{
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(PlayerResizableCapsuleCollider))]
    public class Player : MonoBehaviour, IHittable, IStunnable, IKnockbackable
    {
        #region References
        [field: Header("References")]
        [field: SerializeField] public PlayerSO Data { get; private set; }

        [field: Header("Collisions")]
        [field: SerializeField] public PlayerLayerData LayerData { get; private set; }

        [field: Header("Camera")]
        [field: SerializeField] public PlayerCameraRecenteringUtility CameraRecenteringUtility { get; private set; }

        [field: Header("Animations")]
        [field: SerializeField] public PlayerAnimationData AnimationData { get; private set; }
        
        [field: Header("Hit boxes")]
        [field: SerializeField] public List<GameObject> HitSockets { get; private set; }
        
        [field: SerializeField] public MMF_Player DashEndFeedback { get; private set; }
        #endregion
        
        #region Components
        public Rigidbody Rigidbody { get; private set; }
        public Animator Animator { get; private set; }
        
        public TargetDetectionController TargetDetector { get; private set; }
        public LedgeDetectionController LedgeDetector { get; private set; }

        public PlayerInput Input { get; private set; }
        public PlayerResizableCapsuleCollider ResizableCapsuleCollider { get; private set; }

        public Transform MainCameraTransform { get; private set; }
        
        public PlayerMovementStateMachine MovementStateMachine { get; private set; }
        
        public GameObject TargetAvatar { get; private set; }
        public SkinnedMeshRenderer TargetAvatarRenderer { get; private set; }
        
        public SocketManager SocketManager { get; private set; }
        
        public PlayerStats PlayerStats { get; private set; }
        
        // Components for stun and knockback
        private PlayerStunHandler _stunHandler;
        private PlayerKnockbackHandler _knockbackHandler;
        
        private URPDamageFlashComponent _damageFlashComponent;
        #endregion
        
        #region Interfaces
        // IStunnable implementation - delegates to PlayerStunHandler
        public bool IsStunned => _stunHandler != null && _stunHandler.IsStunned;
        public bool IsInStunRecovery => _stunHandler != null && _stunHandler.IsInStunRecovery;
        public bool CanBeStunned => _stunHandler != null && _stunHandler.CanBeStunned;
        public bool IsDied => PlayerStats.IsDied;
        public event Action<GameObject> OnStunBegin
        {
            add { if (_stunHandler != null) _stunHandler.OnStunBegin += value; }
            remove { if (_stunHandler != null) _stunHandler.OnStunBegin -= value; }
        }
        public event Action<GameObject> OnStunEnd
        {
            add { if (_stunHandler != null) _stunHandler.OnStunEnd += value; }
            remove { if (_stunHandler != null) _stunHandler.OnStunEnd -= value; }
        }
        
        // IKnockbackable implementation - delegates to PlayerKnockbackHandler
        public bool IsPushedBack => _knockbackHandler != null && _knockbackHandler.IsPushedBack;
        public event Action<GameObject> OnKnockbackStart
        {
            add { if (_knockbackHandler != null) _knockbackHandler.OnKnockbackStart += value; }
            remove { if (_knockbackHandler != null) _knockbackHandler.OnKnockbackStart -= value; }
        }
        public event Action<GameObject> OnKnockbackEnd
        {
            add { if (_knockbackHandler != null) _knockbackHandler.OnKnockbackEnd += value; }
            remove { if (_knockbackHandler != null) _knockbackHandler.OnKnockbackEnd -= value; }
        }
        
        // IHittable implementation
        public event Action<IHittable, float, Vector3> OnDamaged;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Rigidbody = GetComponent<Rigidbody>();
            Rigidbody.useGravity = false;
            
            Animator = GetComponentInChildren<Animator>();

            Input = GetComponent<PlayerInput>();
            ResizableCapsuleCollider = GetComponent<PlayerResizableCapsuleCollider>();

            TargetDetector = GetComponent<TargetDetectionController>();
            LedgeDetector = GetComponent<LedgeDetectionController>();
            
            SocketManager = GetComponent<SocketManager>();
            
            // Get or add the stun and knockback handlers
            _stunHandler = GetComponent<PlayerStunHandler>();
            if (_stunHandler == null)
            {
                _stunHandler = gameObject.AddComponent<PlayerStunHandler>();
            }
            
            _knockbackHandler = GetComponent<PlayerKnockbackHandler>();
            if (_knockbackHandler == null)
            {
                _knockbackHandler = gameObject.AddComponent<PlayerKnockbackHandler>();
            }
            
            _damageFlashComponent = GetComponent<URPDamageFlashComponent>();

            MainCameraTransform = Camera.main?.transform;
            
            CameraRecenteringUtility.Initialize();
            AnimationData.Initialize(this);

            MovementStateMachine = new PlayerMovementStateMachine(this);

            TargetAvatar = transform.GetChild(0).gameObject;
            TargetAvatarRenderer = TargetAvatar.GetComponentInChildren<SkinnedMeshRenderer>();
            
            PlayerStats = GetComponent<PlayerStats>();
        }

        private void Start()
        {
            AddInputActionsCallbacks();
            
            MovementStateMachine.ChangeState(MovementStateMachine.DefaultState);
        }

        private void OnDisable()
        {
            RemoveInputActionsCallbacks();
        }

        private void AddInputActionsCallbacks()
        {
            if (PlayerStats != null)
            {
                PlayerStats.OnPlayerDeath.AddListener(OnDie);;
            }
        }
        
        private void RemoveInputActionsCallbacks()
        {
            if (PlayerStats != null)
            {
                PlayerStats.OnPlayerDeath.RemoveListener(OnDie);
            }
        }

        private void Update()
        {
            if (IsDied) return;
            
            MovementStateMachine.HandleInput();

            MovementStateMachine.Update();
            
            AnimationData.UpdateInteractionAnimations(Animator);
        }

        private void FixedUpdate()
        {
            if (IsDied) return;

            MovementStateMachine.PhysicsUpdate();
        }

        private void OnTriggerEnter(Collider inCollider)
        {
            MovementStateMachine.OnTriggerEnter(inCollider);
        }

        private void OnTriggerExit(Collider inCollider)
        {
            MovementStateMachine.OnTriggerExit(inCollider);
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            
            MovementStateMachine.OnDrawGizmos();
        }

        private void OnAnimatorIK(int layerIndex)
        {
            MovementStateMachine.OnAnimatorIK(layerIndex);
        }
        #endregion

        #region Animation Events
        public void OnMovementStateAnimationEnterEvent()
        {
            MovementStateMachine.OnAnimationEnterEvent();
        }

        public void OnMovementStateAnimationExitEvent()
        {
            MovementStateMachine.OnAnimationExitEvent();
        }

        public void OnMovementStateAnimationTransitionEvent()
        {
            MovementStateMachine.OnAnimationTransitionEvent();
        }

        public void OnMovementStateAnimationWithIntParameterEvent(int value)
        {
            MovementStateMachine.OnAnimationWithIntParameterEvent(value);
        }
        
        public Coroutine StartStateCoroutine(IEnumerator enumerator)
        {
            return StartCoroutine(enumerator);
        }
        #endregion

        #region Hit Handling
        public void OnHit(Vector3 direction, float damage)
        {
            // Forward to HitContext version
            HitContext context = new HitContext
            {
                HitDirection = direction,
                Damage = damage,
                EnableStun = true,
                StunDuration = 1.0f,
                StunThresholdDamage = 5f,
                EnablePushBack = true,
                PushBackDistance = 2f,
                PushBackDuration = 0.5f
            };
            
            OnHitWithContext(context);
        }

        public void OnHitWithContext(HitContext hitContext)
        {
            if (IFrameManager.Instance.IsInvincible) return;
            
            var hitDirection = hitContext.HitDirection;
            hitDirection.y = 0f;
            
            // Process damage
            if (PlayerStats && !PlayerStats.TakeDamage(0.5f, hitDirection))
            {
                FloatingTextManager.Instance.ShowDamageText("Dodge", transform);
                IFrameManager.Instance.StartDamageInvincibility();
                return;
            }
            
            if (!IsDied)
            {
                AnimationData.PlayInteractionAnimation(Animator, AnimationData.DamagedStateName);
            }
            
            Debug.Log($"Player took {hitContext.Damage} damage");
            
            OnDamaged?.Invoke(this, hitContext.Damage, hitContext.HitDirection);
            
            // Face towards hit direction
            Quaternion targetRotation = Quaternion.LookRotation(hitDirection);
            transform.rotation = targetRotation;
            
            // CameraShakeManager.Instance.Shake(1f, 0.2f);
            IFrameManager.Instance.StartDamageInvincibility();
            
            // Use the new TriggerOnHit method to handle all event dispatching in one place
            hitContext.TriggerOnHit(gameObject);
        }

        public void OnHitFromDirection(Vector3 hitPosition, Vector3 hitDirection, float damage)
        {
            OnHit(hitDirection, damage);
        }

        public void ReactToHit(HitReactionType reactionType, Vector3 direction)
        {
            // Handle different hit reaction types
            switch (reactionType)
            {
                case HitReactionType.Small:
                    // Small flinch without movement
                    break;
                case HitReactionType.Medium:
                    // Medium pushback
                    if (_knockbackHandler != null)
                    {
                        _knockbackHandler.ApplyKnockback(direction, 1f, 0.2f);
                    }
                    break;
                case HitReactionType.Large:
                    // Large knockback and stun
                    if (_knockbackHandler != null)
                    {
                        _knockbackHandler.ApplyKnockback(direction, 3f, 0.5f);
                    }
                    if (_stunHandler != null)
                    {
                        _stunHandler.ApplyStun(0.5f);
                    }
                    break;
            }
        }

        private void OnDie()
        {
            Input.PlayerActions.Disable();
            
            ResizableCapsuleCollider.CapsuleColliderData.Collider.enabled = false;
            
            AnimationData.PlayInteractionAnimation(Animator, AnimationData.DieStateName, 0);
        }
        #endregion
        
        #region Stun Implementation
        // IStunnable implementation
        public bool ApplyStun(float duration)
        {
            if (_stunHandler != null)
            {
                return _stunHandler.ApplyStun(duration);
            }
            return false;
        }
        
        public void EndStun()
        {
            if (_stunHandler != null)
            {
                _stunHandler.EndStun();
            }
        }
        
        public void HandleStunEvent(CombatEventManager.StunEventContext context)
        {
            if (_stunHandler != null)
            {
                _stunHandler.HandleStunEvent(context);
            }
        }
        #endregion
        
        #region Knockback Implementation
        // IKnockbackable implementation
        public bool ApplyKnockback(Vector3 direction, float distance, float duration, AnimationCurve curve = null)
        {
            if (_knockbackHandler != null)
            {
                return _knockbackHandler.ApplyKnockback(direction, distance, duration, curve);
            }
            return false;
        }
        
        public bool ApplyDeathKnockback(Vector3 direction, float distance, float duration, AnimationCurve curve = null)
        {
            if (_knockbackHandler != null)
            {
                return _knockbackHandler.ApplyDeathKnockback(direction, distance, duration, curve);
            }
            return false;
        }
        
        public void HandleKnockbackEvent(CombatEventManager.KnockbackEventContext context)
        {
            if (_knockbackHandler != null)
            {
                _knockbackHandler.HandleKnockbackEvent(context);
            }
        }
        #endregion
    }
}