using System;
using System.Collections;
using DG.Tweening;
using LocomotionSystem;
using LocomotionSystem.Utilities;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Sequence = DG.Tweening.Sequence;

namespace LocomotionSystem
{
    /// <summary>
    /// Base class for all interactable objects in the game
    /// Implements common functionality including name display with animation
    /// </summary>
    public abstract class InteractableBase : MonoBehaviour, IInteractable
    {
        #region Serialized Fields
        [ReadOnly]
        [SerializeField] protected string m_ID;
        [SerializeField] protected string m_ItemName;
        [SerializeField] protected string m_DisplayName;
        [HideInInspector]
        [SerializeField] protected InteractableType m_Type;
        [SerializeField] protected GameObject m_VisualObject;
        [SerializeField] protected float m_CollectionRadius = 2f;
        [SerializeField] protected float m_RotationSpeed = 50f;
        [SerializeField] protected float m_BobHeight = 0.2f;
        [SerializeField] protected float m_BobSpeed = 1f;
        [SerializeField] protected float m_HoverHeight = 0.5f;
        [SerializeField] protected AudioClip m_InteractSound;
        
        [Header("Selection UI")]
        [SerializeField, Tooltip("Height above the object where the selection text will appear")]
        protected float m_SelectionTextHeight = 1.5f;
        [SerializeField, Tooltip("Color of the selection text")]
        protected Color m_SelectionTextColor = Color.yellow;
        [SerializeField, Tooltip("Scale multiplier for the selection text size")]
        protected float m_SelectionTextScale = 1f;
        [SerializeField, Tooltip("Font asset for the selection text (leave empty to use default)")]
        protected TMP_FontAsset m_SelectionTextFont = null;
        [SerializeField, Tooltip("Font size for the selection text")]
        protected float m_SelectionTextFontSize = 3f;
        [SerializeField, Tooltip("Enable text outline effect for better visibility")]
        protected bool m_EnableTextOutline = true;
        [SerializeField, Tooltip("Color of the text outline")]
        protected Color m_TextOutlineColor = Color.black;
        [SerializeField, Tooltip("Width of the text outline effect")]
        protected float m_TextOutlineWidth = 0.2f;
        
        [Header("Selection Animation")]
        [SerializeField] private AnimationCurve m_SelectionScaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private float m_SelectionAnimationDuration = 0.5f;
        [SerializeField] private float m_CompressScaleFactor = 0.7f;
        [SerializeField] private float m_ExpandScaleFactor = 1.2f;

        #endregion
        
        #region Protected Fields
        protected bool m_IsSelected;
        protected bool m_IsRegistered;
        protected Sequence m_BobSequence;
        protected Tween m_RotationTween;
        protected string m_LastGameObjectName;
        protected TextMeshProUGUI m_SelectionText;
        protected Canvas m_TextCanvas;
        protected Collider m_Collider;
        protected Camera _mainCamera;

        protected Camera MainCamera
        {
            get
            {
                if (_mainCamera == null)
                {
                    _mainCamera = Camera.main;
                }
                return _mainCamera;
            }
        }
        #endregion
        
        public InteractableType Type => m_Type;
        public string ID => m_ID;
        public string Name => m_ItemName;
        public GameObject VisualObject => m_VisualObject;
        public float CollectionRadius => m_CollectionRadius;

        
        #region Unity Lifecycle
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
            
            _mainCamera = Camera.main;
            
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
            m_BobSequence?.Kill();
            m_RotationTween?.Kill();
        }
        
        protected virtual void FixedUpdate()
        {
            // if (!m_IsSelected || !m_TextCanvas || !m_TextCanvas.gameObject.activeSelf) return;
            // if (_mainCamera)
            // {
            //     m_TextCanvas.transform.rotation = _mainCamera.transform.rotation;
            // }
        }
        
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
        #endregion
        
        #region Public Methods
        public virtual bool OnInteract(GameObject collector)
        {
            return false;
        }

        public virtual T GetTypeData<T>() where T : class
        {
            // Check if thxe type is the same as the current class
            if (this is T data)
            {
                return data;
            }
            
            return null;
        }
        
        public virtual void Register()
        {
            if (m_IsRegistered) return;
            
            InteractableManager.Instance.RegisterCollectable(this);
            m_IsRegistered = true;
        }

        public virtual void Unregister()
        {
            if (!m_IsRegistered) return;
            
            InteractableManager.Instance?.UnregisterCollectable(this);
            m_IsRegistered = false;
        }


        public virtual void OnSelected()
        {
            m_IsSelected = true;
    
            if (!m_SelectionText) return;
    
            m_SelectionText.text = m_DisplayName;
            m_SelectionText.transform.parent.gameObject.SetActive(true);
    
            // Store original scale of the text's parent canvas
            RectTransform textTransform = m_SelectionText.rectTransform;
            Vector3 baseScale = textTransform.localScale;
    
            // Kill any previous animations
            textTransform.DOKill(false);
    
            // Create a sequence for compress and bounce
            Sequence textScaleSequence = DOTween.Sequence();
    
            // First compress in Y direction
            Vector3 compressedScale = new Vector3(
                baseScale.x,
                baseScale.y * m_CompressScaleFactor,
                baseScale.z);
            textScaleSequence.Append(textTransform.DOScale(compressedScale, m_SelectionAnimationDuration * 0.3f)
                .SetEase(Ease.OutQuad));
    
            // Then bounce up with an overshoot
            Vector3 expandedScale = baseScale * m_ExpandScaleFactor;
            textScaleSequence.Append(textTransform.DOScale(expandedScale, m_SelectionAnimationDuration * 0.4f)
                .SetEase(Ease.OutBack));
    
            // Finally, return to original scale with a slight bounce
            textScaleSequence.Append(textTransform.DOScale(baseScale, m_SelectionAnimationDuration * 0.3f)
                .SetEase(Ease.OutBounce));
    
            // Play the sequence
            textScaleSequence.Play();
        }

        /// <summary>
        /// Called when this interactable is no longer selected by the player
        /// </summary>
        public virtual void OnUnselected()
        {
            m_IsSelected = false;

            // Hide selection text
            if (m_SelectionText)
            {
                m_SelectionText.transform.parent.gameObject.SetActive(false);
            }

            // Return to normal scale with a quick animation
            if (m_VisualObject)
            {
                // Kill any selection animations in progress
                m_VisualObject.transform.DOKill(false);

                // Return to original scale with a slight squish effect
                Vector3 currentScale = m_VisualObject.transform.localScale;
                m_VisualObject.transform.DOScale(currentScale * 0.9f, 0.1f)
                    .OnComplete(() => { m_VisualObject.transform.DOScale(currentScale, 0.2f).SetEase(Ease.OutQuad); });
            }
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
        
        private IEnumerator DelaySetActive(bool isActive)
        {
            yield return new WaitForNextFrameUnit();
            m_SelectionText.transform.parent.gameObject.SetActive(isActive);
        }
        
        private void SetupVisuals()
        {
            // Wait until after the scale animation completes before setting up floating effects
            DOVirtual.DelayedCall(1f, () => {
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
            textChildObj.gameObject.layer = LayerMask.NameToLayer("UI");
            textChildObj.transform.localPosition = Vector3.zero;
            textChildObj.transform.localRotation = Quaternion.identity;
            textChildObj.transform.localScale = new Vector3(1, 1, 1);
            
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
        #endregion
    }
} 