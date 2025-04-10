using System;
using UnityEngine;
using DG.Tweening;

namespace LocomotionSystem.LootSystem
{
    /// <summary>
    /// Base class for all collectable items with common functionality
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public abstract class CollectableBase : MonoBehaviour, ICollectable
    {
        #region Private Fields
        [SerializeField] private string m_ID;
        [SerializeField] private string m_Name;
        [SerializeField] private CollectableType m_Type;
        [SerializeField] private GameObject m_VisualObject;
        [SerializeField] private float m_CollectionRadius = 2f;
        [SerializeField] private float m_AutoCollectionRadius = 5f;
        [SerializeField, Range(0.1f, 5f)] private float m_MoveToPlayerDuration = 0.5f;
        [SerializeField] private float m_RotationSpeed = 50f;
        [SerializeField] private float m_BobHeight = 0.2f;
        [SerializeField] private float m_BobSpeed = 1f;
        [SerializeField] private float m_HoverHeight = 0.5f;
        [SerializeField] private AudioClip m_CollectionSound;
        
        private Vector3 m_StartPosition;
        private bool m_IsMovingToPlayer;
        private bool m_IsRegistered;
        private Collider m_Collider;
        private Sequence m_BobSequence;
        #endregion

        #region Public Properties
        public CollectableType Type => m_Type;
        public string ID => m_ID;
        public string Name => m_Name;
        public GameObject VisualObject => m_VisualObject;
        public float AutoCollectionRadius => m_AutoCollectionRadius;
        public float CollectionRadius => m_CollectionRadius;
        public bool IsMovingToPlayer => m_IsMovingToPlayer;
        #endregion

        #region Unity Lifecycle
        protected virtual void Awake()
        {
            if (string.IsNullOrEmpty(m_ID))
            {
                m_ID = Guid.NewGuid().ToString();
            }
            
            m_Collider = GetComponent<Collider>();
            
            // Ensure the collider is a trigger
            m_Collider.isTrigger = true;
            
            if (m_VisualObject == null)
            {
                m_VisualObject = gameObject;
            }
            
            m_StartPosition = transform.position;
        }

        protected virtual void Start()
        {
            Register();
            SetupVisuals();
        }
        
        protected virtual void OnEnable()
        {
            if (!m_IsRegistered)
            {
                Register();
            }
        }
        
        protected virtual void OnDisable()
        {
            Unregister();
        }
        
        protected virtual void OnDestroy()
        {
            Unregister();
            m_BobSequence?.Kill();
        }
        #endregion

        #region Public Methods
        public virtual bool OnCollected(GameObject _collector)
        {
            // Play collection sound if available
            if (m_CollectionSound != null)
            {
                AudioSource.PlayClipAtPoint(m_CollectionSound, transform.position);
            }
            
            // Unregister from loot manager
            Unregister();
            
            // Destroy the object after collection
            Destroy(gameObject);
            
            return true;
        }

        public virtual void Register()
        {
            if (!m_IsRegistered)
            {
                LootManager.Instance.RegisterCollectable(this);
                m_IsRegistered = true;
            }
        }

        public virtual void Unregister()
        {
            if (m_IsRegistered)
            {
                LootManager.Instance?.UnregisterCollectable(this);
                m_IsRegistered = false;
            }
        }

        public virtual void MoveToPlayer(Transform _playerTransform, Action _onComplete = null)
        {
            if (m_IsMovingToPlayer) return;
            
            m_IsMovingToPlayer = true;
            
            // Disable collider during movement
            m_Collider.enabled = false;
            
            // Stop bobbing animation
            m_BobSequence?.Kill();
            
            // Calculate bezier points
            Vector3 startPos = transform.position;
            Vector3 endPos = _playerTransform.position + Vector3.up * 1f; // Slightly above player
            Vector3 controlPoint = Vector3.Lerp(startPos, endPos, 0.5f) + Vector3.up * 2f; // Control point above midpoint
            
            // Animate along bezier curve
            transform.DOPath(
                new Vector3[] { startPos, controlPoint, endPos },
                m_MoveToPlayerDuration,
                PathType.CubicBezier
            ).SetEase(Ease.InOutQuad)
            .OnComplete(() => {
                m_IsMovingToPlayer = false;
                _onComplete?.Invoke();
                OnCollected(_playerTransform.gameObject);
            });
        }
        #endregion

        #region Private Methods
        private void SetupVisuals()
        {
            // Floating effect - hover slightly above ground with bobbing motion
            transform.position = new Vector3(transform.position.x, m_HoverHeight, transform.position.z);
            
            // Create bobbing animation
            m_BobSequence = DOTween.Sequence();
            m_BobSequence.Append(transform.DOLocalMoveY(m_HoverHeight + m_BobHeight, m_BobSpeed).SetEase(Ease.InOutSine));
            m_BobSequence.Append(transform.DOLocalMoveY(m_HoverHeight, m_BobSpeed).SetEase(Ease.InOutSine));
            m_BobSequence.SetLoops(-1);
            
            // Slow rotation
            transform.DOLocalRotate(new Vector3(0, 360, 0), m_RotationSpeed, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1);
        }
        #endregion
    }
} 