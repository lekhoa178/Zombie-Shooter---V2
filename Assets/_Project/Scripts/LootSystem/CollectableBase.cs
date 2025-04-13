using System;
using UnityEngine;
using DG.Tweening;
using LocomotionSystem.Utilities;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace LocomotionSystem
{
    /// <summary>
    /// Base class for all collectable items with common functionality
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public abstract class CollectableBase : MonoBehaviour, ICollectable
    {
        #region Private Fields
        [ReadOnly]
        [SerializeField] private string m_ID;
        [SerializeField] private string m_ItemName;
        [SerializeField] private string m_DisplayName;
        [HideInInspector]
        [SerializeField] protected CollectableType m_Type;
        [SerializeField] private GameObject m_VisualObject;
        [SerializeField] private float m_CollectionRadius = 2f;
        [SerializeField] private float m_AutoCollectionRadius = 5f;
        [SerializeField, Range(0.1f, 5f)] private float m_MoveToPlayerDuration = 0.5f;
        [SerializeField] private float m_RotationSpeed = 50f;
        [SerializeField] private float m_BobHeight = 0.2f;
        [SerializeField] private float m_BobSpeed = 1f;
        [SerializeField] private float m_HoverHeight = 0.5f;
        [SerializeField] private AudioClip m_CollectionSound;
        
        [Header("Selection UI")]
        [SerializeField, Tooltip("Height above the object where the selection text will appear")]
        private float m_SelectionTextHeight = 1.5f;
        [SerializeField, Tooltip("Color of the selection text")]
        private Color m_SelectionTextColor = Color.yellow;
        [SerializeField, Tooltip("Scale multiplier for the selection text size")]
        private float m_SelectionTextScale = 1f;
        [SerializeField, Tooltip("Font asset for the selection text (leave empty to use default)")]
        private TMP_FontAsset m_SelectionTextFont = null;
        [SerializeField, Tooltip("Font size for the selection text")]
        private float m_SelectionTextFontSize = 3f;
        [SerializeField, Tooltip("Enable text outline effect for better visibility")]
        private bool m_EnableTextOutline = true;
        [SerializeField, Tooltip("Color of the text outline")]
        private Color m_TextOutlineColor = Color.black;
        [SerializeField, Tooltip("Width of the text outline effect")]
        private float m_TextOutlineWidth = 0.2f;
        
        private Vector3 m_StartPosition;
        private bool m_IsMovingToPlayer;
        private bool m_IsRegistered;
        private Collider m_Collider;
        private Sequence m_BobSequence;
        private Tween m_RotationTween;
        private string m_LastGameObjectName;
        private TextMeshProUGUI m_SelectionText;
        private Canvas m_TextCanvas;
        private bool m_IsSelected;
        protected Sequence m_ScaleSequence;
        #endregion

        #region Public Properties
        public CollectableType Type => m_Type;
        public string ID => m_ID;
        public string Name => m_ItemName;
        public GameObject VisualObject => m_VisualObject;
        public float AutoCollectionRadius => m_AutoCollectionRadius;
        public float CollectionRadius => m_CollectionRadius;
        public bool IsMovingToPlayer => m_IsMovingToPlayer;
        #endregion

        #region Unity Lifecycle
        /// <summary>
        /// Called when component is added in the editor or Reset is clicked
        /// </summary>
        private void Reset()
        {
            GenerateUniqueID();
        }
        
        /// <summary>
        /// Called when changes are made in the inspector or when GameObject name changes
        /// </summary>
        private void OnValidate()
        {
            // Check if the GameObject name has changed
            if (m_LastGameObjectName != gameObject.name)
            {
                UpdateUniqueIDPrefix();
                m_LastGameObjectName = gameObject.name;
            }
        }
        
        protected virtual void Awake()
        {
            if (string.IsNullOrEmpty(m_ID))
            {
                GenerateUniqueID();
            }
            
            m_LastGameObjectName = gameObject.name;
            
            m_Collider = GetComponent<Collider>();
            
            m_Collider.isTrigger = true;
            
            if (m_VisualObject == null)
            {
                m_VisualObject = gameObject;
            }
            
            m_StartPosition = transform.position;
            
            // Create selection text
            CreateSelectionText();
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
            m_RotationTween?.Kill();
        }

        protected virtual void Update()
        {
            // Make selection text always face the camera if it's active
            if (m_IsSelected && m_TextCanvas != null && m_TextCanvas.gameObject.activeSelf)
            {
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    m_TextCanvas.transform.rotation = mainCamera.transform.rotation;
                }
            }
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
        
        /// <summary>
        /// Implementation of ICollectable.GetTypeData method
        /// Base implementation returns null as it doesn't have specialized data
        /// </summary>
        public virtual T GetTypeData<T>() where T : class
        {
            return null;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Generates a unique ID with the GameObject name as prefix
        /// </summary>
        private void GenerateUniqueID()
        {
            // Format: ObjectName_GUID
            m_ID = $"{gameObject.name}_{Guid.NewGuid().ToString()}";
            m_LastGameObjectName = gameObject.name;
        }
        
        /// <summary>
        /// Updates the prefix part of the ID when GameObject name changes
        /// </summary>
        private void UpdateUniqueIDPrefix()
        {
            if (string.IsNullOrEmpty(m_ID))
            {
                GenerateUniqueID();
                return;
            }
            
            // Extract the GUID part (everything after the last underscore)
            int lastIndex = m_ID.LastIndexOf('_');
            if (lastIndex > 0)
            {
                string guidPart = m_ID.Substring(lastIndex); // This includes the underscore
                m_ID = $"{gameObject.name}{guidPart}";
            }
            else
            {
                // If ID doesn't have the expected format, generate a new one
                GenerateUniqueID();
            }
        }
        
        private void SetupVisuals()
        {
            // Wait until after the scale animation completes before setting up floating effects
            DOVirtual.DelayedCall(0.7f, () => {
                // Floating effect - hover slightly above ground with bobbing motion
                m_VisualObject.transform.position = new Vector3(
                    m_VisualObject.transform.position.x, 
                    m_HoverHeight, 
                    m_VisualObject.transform.position.z
                );
        
                // Create bobbing animation
                m_BobSequence = DOTween.Sequence();
                m_BobSequence.Append(m_VisualObject.transform.DOLocalMoveY(m_HoverHeight + m_BobHeight, m_BobSpeed).SetEase(Ease.InOutSine));
                m_BobSequence.Append(m_VisualObject.transform.DOLocalMoveY(m_HoverHeight, m_BobSpeed).SetEase(Ease.InOutSine));
                m_BobSequence.SetLoops(-1);
        
                // Slow rotation
                m_RotationTween = m_VisualObject.transform.DOLocalRotate(new Vector3(0, 360, 0), m_RotationSpeed, RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear)
                    .SetLoops(-1);
            });
        }
        
        /// <summary>
        /// Creates the selection text mesh that will appear above the object when selected
        /// </summary>
        private void CreateSelectionText()
        {
            // Create a new GameObject for the text
            GameObject textObj = new GameObject($"{gameObject.name}_SelectionText");
            textObj.transform.SetParent(transform);
            textObj.transform.localPosition = new Vector3(0, m_SelectionTextHeight, 0);
            
            // Create Canvas for the TextMeshPro
            m_TextCanvas = textObj.AddComponent<Canvas>();
            m_TextCanvas.renderMode = RenderMode.WorldSpace;
            m_TextCanvas.worldCamera = Camera.main;
            
            // Add canvas scaler
            CanvasScaler canvasScaler = textObj.AddComponent<CanvasScaler>();
            canvasScaler.dynamicPixelsPerUnit = 10f;
            canvasScaler.referencePixelsPerUnit = 100f;
            
            // Create a separate GameObject for the actual text under the canvas
            GameObject textChildObj = new GameObject("Text");
            textChildObj.transform.SetParent(textObj.transform);
            textChildObj.transform.localPosition = Vector3.zero;
            textChildObj.transform.localRotation = Quaternion.identity;
            textChildObj.transform.localScale = Vector3.one;
            
            // Setup the TextMeshProUGUI component
            m_SelectionText = textChildObj.AddComponent<TextMeshProUGUI>();
            m_SelectionText.text = m_DisplayName;
            m_SelectionText.color = m_SelectionTextColor;
            m_SelectionText.fontSize = m_SelectionTextFontSize;
            m_SelectionText.fontStyle = FontStyles.Bold;
            m_SelectionText.alignment = TextAlignmentOptions.Center;
            m_SelectionText.textWrappingMode = TextWrappingModes.NoWrap;
            m_SelectionText.rectTransform.pivot = new Vector2(0.5f, 0f); // Set pivot to bottom center
            m_SelectionText.rectTransform.anchorMin = new Vector2(0, 0);
            m_SelectionText.rectTransform.anchorMax = new Vector2(1, 1);
            
            // Set the canvas size to match the text
            RectTransform canvasRect = m_TextCanvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(5f, 1f);
            canvasRect.localScale = Vector3.one * 0.01f;
            
            // Apply custom font if specified
            if (m_SelectionTextFont != null)
            {
                m_SelectionText.font = m_SelectionTextFont;
            }
            
            // Enable outline if needed
            if (m_EnableTextOutline)
            {
                // Use TextMeshPro's built-in outline
                m_SelectionText.outlineColor = m_TextOutlineColor;
                m_SelectionText.outlineWidth = m_EnableTextOutline ? m_TextOutlineWidth : 0;
            }
            
            // Hide text initially
            textObj.SetActive(false);
        }
        
        /// <summary>
        /// Called when this collectable is selected by the player
        /// </summary>
        public virtual void OnSelected()
        {
            m_IsSelected = true;
            
            // Show selection text
            if (m_SelectionText != null)
            {
                // Update text to ensure it shows the current display name
                m_SelectionText.text = m_DisplayName;
                m_SelectionText.transform.parent.gameObject.SetActive(true);
            }
        }
        
        /// <summary>
        /// Called when this collectable is no longer selected by the player
        /// </summary>
        public virtual void OnUnselected()
        {
            m_IsSelected = false;
            
            // Hide selection text
            if (m_SelectionText != null)
            {
                m_SelectionText.transform.parent.gameObject.SetActive(false);
            }
        }
        #endregion
    }
} 