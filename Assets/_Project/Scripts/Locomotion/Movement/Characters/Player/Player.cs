using System;
using System.Collections;
using System.Collections.Generic;
using Climbing;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace LocomotionSystem
{
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(PlayerResizableCapsuleCollider))]
    [RequireComponent(typeof(TargetDetectionController))]
    public class Player : MonoBehaviour
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
        
        public WeaponManager WeaponManager { get; private set; }

        private void Awake()
        {
            Rigidbody = GetComponent<Rigidbody>();
            Rigidbody.useGravity = false;
            
            Animator = GetComponentInChildren<Animator>();

            Input = GetComponent<PlayerInput>();
            ResizableCapsuleCollider = GetComponent<PlayerResizableCapsuleCollider>();

            TargetDetector = GetComponent<TargetDetectionController>();
            LedgeDetector = GetComponent<LedgeDetectionController>();
            
            WeaponManager = GetComponent<WeaponManager>();

            MainCameraTransform = Camera.main?.transform;
            
            CameraRecenteringUtility.Initialize();
            AnimationData.Initialize(this);

            MovementStateMachine = new PlayerMovementStateMachine(this);

            TargetAvatar = transform.GetChild(0).gameObject;
            TargetAvatarRenderer = TargetAvatar.GetComponentInChildren<SkinnedMeshRenderer>();
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

        // public void OnDamageReceived(float damage)
        // {
        //     // Check if we can take damage
        //     if (IFrameManager.Instance.CanTakeDamage())
        //     {
        //         // Apply damage logic here
        //         Debug.Log($"Player took {damage} damage");
        //         
        //         // Start invincibility frames
        //         IFrameManager.Instance.StartInvincibility(
        //             Data.ComboConfig.AttackConfigs[ReusableData.CurrentComboIndex].IFrameDuration,
        //             Data.ComboConfig.AttackConfigs[ReusableData.CurrentComboIndex].ShowIFrameVisualFeedback
        //         );
        //     }
        // }
    }
}