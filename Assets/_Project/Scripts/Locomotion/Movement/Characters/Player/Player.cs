using System;
using System.Collections;
using System.Collections.Generic;
using Climbing;
using LocomotionSystem.LootSystem;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LocomotionSystem
{
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(PlayerResizableCapsuleCollider))]
    public class Player : MonoBehaviour, IHittable, IStunnable, IKnockbackable
    {
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
        
        [field: Header("Loot & Weapons")]
        [field: SerializeField] public InputActionReference CollectAction { get; private set; }
        [field: SerializeField] public InputActionReference WeaponSwitch1Action { get; private set; }
        [field: SerializeField] public InputActionReference WeaponSwitch2Action { get; private set; }
        [field: SerializeField] public InputActionReference WeaponSwitch3Action { get; private set; }
        
        [field: SerializeField] public MMF_Player DashEndFeedback { get; private set; }
        
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
        
        // Components for stun and knockback
        private PlayerStunHandler _stunHandler;
        private PlayerKnockbackHandler _knockbackHandler;
        
        private URPDamageFlashComponent _damageFlashComponent;
        
        // IStunnable implementation - delegates to PlayerStunHandler
        public bool IsStunned => _stunHandler != null && _stunHandler.IsStunned;
        public bool IsInStunRecovery => _stunHandler != null && _stunHandler.IsInStunRecovery;
        public bool CanBeStunned => _stunHandler != null && _stunHandler.CanBeStunned;
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
            
            // Initialize weapon switch input actions
            SetupWeaponSwitchInputs();
            
            // Initialize collect input action
            SetupCollectInput();
        }

        private void Start()
        {
            MovementStateMachine.ChangeState(MovementStateMachine.DefaultState);
        }

        private void Update()
        {
            MovementStateMachine.HandleInput();

            MovementStateMachine.Update();
        }

        private void FixedUpdate()
        {
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
            
            // Process damage
            Debug.Log($"Player took {hitContext.Damage} damage");
            
            OnDamaged?.Invoke(this, hitContext.Damage, hitContext.HitDirection);
            
            Animator.SetBool(AnimationData.DamagedParameterHash, false);
            Animator.SetBool(AnimationData.DamagedParameterHash, true);
            
            // Face towards hit direction
            Vector3 hitDirection = hitContext.HitDirection;
            hitDirection.y = 0f; // Ignore vertical component
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
        
        #region Weapon System
        private void SetupWeaponSwitchInputs()
        {
            if (WeaponSwitch1Action != null)
            {
                WeaponSwitch1Action.action.performed += ctx => SwitchWeapon(0);
                WeaponSwitch1Action.action.Enable();
            }
            
            if (WeaponSwitch2Action != null)
            {
                WeaponSwitch2Action.action.performed += ctx => SwitchWeapon(1);
                WeaponSwitch2Action.action.Enable();
            }
            
            if (WeaponSwitch3Action != null)
            {
                WeaponSwitch3Action.action.performed += ctx => SwitchWeapon(2);
                WeaponSwitch3Action.action.Enable();
            }
        }
        
        private void SetupCollectInput()
        {
            if (CollectAction != null)
            {
                CollectAction.action.Enable();
            }
        }
        
        /// <summary>
        /// Switch to a weapon slot
        /// </summary>
        public void SwitchWeapon(int _slot)
        {
            if (WeaponManager.Instance != null)
            {
                WeaponManager.Instance.SwitchWeapon(this, _slot);
            }
        }
        
        /// <summary>
        /// Get the currently equipped weapon
        /// </summary>
        public IWeaponCollectable GetEquippedWeapon()
        {
            if (WeaponManager.Instance != null)
            {
                return WeaponManager.Instance.GetEquippedWeapon(this);
            }
            
            return null;
        }
        
        /// <summary>
        /// Get the weapon inventory
        /// </summary>
        public List<IWeaponCollectable> GetWeaponInventory()
        {
            if (WeaponManager.Instance != null)
            {
                return WeaponManager.Instance.GetWeaponInventory(this);
            }
            
            return new List<IWeaponCollectable>();
        }
        #endregion
    }
}