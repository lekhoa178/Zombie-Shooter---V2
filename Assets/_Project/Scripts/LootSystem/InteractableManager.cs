using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;
using UnityUtils;

namespace LocomotionSystem
{
    /// <summary>
    /// Manager class for all loot-related functionality
    /// </summary>
    public class InteractableManager : PersistentSingleton<InteractableManager>
    {
        #region Private Fields
        [FormerlySerializedAs("_autoCollectionCheckInterval")]
        [FormerlySerializedAs("m_AutoCollectionCheckInterval")]
        [Header("Collection Settings")]
        [SerializeField] private float autoCollectionCheckInterval = 0.5f;
        [SerializeField] private LayerMask collectableLayerMask;
        [SerializeField] private InputActionReference collectAction;
        
        [Header("Selection Settings")]
        [SerializeField] private float m_SelectionRange = 10f;
        [SerializeField] private float m_SelectionUpdateInterval = 0.2f;
        [SerializeField] private float m_MaxSelectionAngle = 45f;
        [SerializeField] private GameObject m_SelectionIndicatorPrefab;
        [SerializeField] private Color m_SelectionTextColor = Color.yellow;
        [SerializeField] private float m_SelectionTextOffset = 1.5f;
        
        [Header("Debug")]
        [SerializeField] private bool m_DebugMode = false;
        
        private List<IInteractable> m_RegisteredCollectables = new();
        private Dictionary<string, IInteractable> m_CollectablesByID = new();
        private float m_AutoCollectionTimer;
        private float m_SelectionUpdateTimer;
        private Transform m_PlayerTransform;
        private Camera m_MainCamera;
        
        // Currently selected interactable
        private IInteractable _selectedInteractable;
        private readonly Dictionary<IInteractable, GameObject> _selectionIndicators = new();
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            
            m_MainCamera = Camera.main;
        }

        private void Start()
        {
            var player = FindAnyObjectByType<Player>();
            if (player != null)
            {
                m_PlayerTransform = player.transform;
            }
            
            if (collectAction == null) return;
            collectAction.action.performed += OnCollectActionPerformed;
            collectAction.action.Enable();
        }

        private void OnDestroy()
        {
            if (this != Instance) return;

            // Unsubscribe from input action
            if (collectAction != null)
            {
                collectAction.action.performed -= OnCollectActionPerformed;
                collectAction.action.Disable();
            }
            
            // Clear selection indicators
            foreach (var indicator in _selectionIndicators.Values)
            {
                if (indicator != null)
                {
                    Destroy(indicator);
                }
            }
            
            // Clear references
            m_RegisteredCollectables.Clear();
            m_CollectablesByID.Clear();
            _selectionIndicators.Clear();
        }

        private void FixedUpdate()
        {
            if (!m_PlayerTransform)
            {
                // Try to find player reference again
                var player = FindAnyObjectByType<Player>();
                if (player)
                {
                    m_PlayerTransform = player.transform;
                }
                else
                {
                    return; // No player found
                }
            }
            
            // Update interactable selection
            m_SelectionUpdateTimer -= Time.deltaTime;
            if (!(m_SelectionUpdateTimer <= 0f)) return;
            
            m_SelectionUpdateTimer = m_SelectionUpdateInterval;
            UpdateCollectableSelection();
        }
        
        private void Update()
        {
            UpdateSelectionIndicators();
        }
        #endregion

        #region Public Methods
        public void RegisterCollectable(IInteractable interactable)
        {
            if (interactable == null) return;
            
            if (!m_RegisteredCollectables.Contains(interactable))
            {
                m_RegisteredCollectables.Add(interactable);
                m_CollectablesByID[interactable.ID] = interactable;
                
                if (m_DebugMode)
                {
                    Debug.Log($"Registered interactable: {interactable.Name} (ID: {interactable.ID})");
                }
            }
        }

        public void UnregisterCollectable(IInteractable interactable)
        {
            if (interactable == null) return;
            
            if (m_RegisteredCollectables.Contains(interactable))
            {
                // If this was the selected interactable, clear selection
                if (_selectedInteractable == interactable)
                {
                    SetSelectedCollectable(null);
                }
                
                m_RegisteredCollectables.Remove(interactable);
                
                if (m_CollectablesByID.ContainsKey(interactable.ID))
                {
                    m_CollectablesByID.Remove(interactable.ID);
                }
                
                // Remove selection indicator if it exists
                if (_selectionIndicators.TryGetValue(interactable, out var indicator))
                {
                    if (indicator != null)
                    {
                        Destroy(indicator);
                    }
                    _selectionIndicators.Remove(interactable);
                }
                
                if (m_DebugMode)
                {
                    Debug.Log($"Unregistered interactable: {interactable.Name} (ID: {interactable.ID})");
                }
            }
        }

        public IInteractable GetCollectableByID(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            
            return m_CollectablesByID.GetValueOrDefault(id);
        }

        public List<IInteractable> GetNearbyCollectables(Vector3 position, float _radius)
        {
            return m_RegisteredCollectables
                .Where(c => c != null && c.VisualObject != null && Vector3.Distance(position, c.VisualObject.transform.position) <= _radius)
                .ToList();
        }
        
        /// <summary>
        /// Get the currently selected interactable
        /// </summary>
        public IInteractable GetSelectedCollectable()
        {
            return _selectedInteractable;
        }
        
        /// <summary>
        /// Try to collect the currently selected interactable
        /// </summary>
        public void CollectSelectedItem()
        {
            _selectedInteractable.OnInteract(m_PlayerTransform.gameObject);
        }
        #endregion

        #region Private Methods

        private void CollectNearbyItems()
        {
            if (!m_PlayerTransform) return;

            {
                // If we have a selected item, collect it
                if (_selectedInteractable is CollectableBase collectableBase)
                {
                    // Check if it's in collection range
                    if (Vector3.Distance(m_PlayerTransform.position, collectableBase.transform.position) <=
                        collectableBase.CollectionRadius)
                    {
                        collectableBase.OnInteract(m_PlayerTransform.gameObject);
                        return;
                    }
                }

                // Otherwise, use the collider-based collection as before
                const float collectRadius = 3f; // Default manual collection radius

                // Get all colliders in range
                var colliders =
                    Physics.OverlapSphere(m_PlayerTransform.position, collectRadius, collectableLayerMask);

                foreach (Collider col in colliders)
                {
                    CollectableBase collectableComponent = col.GetComponent<CollectableBase>();

                    if (collectableComponent)
                    {
                        // Use the interactable's own collection radius if available
                        if (Vector3.Distance(m_PlayerTransform.position, collectableComponent.transform.position) <=
                            collectableComponent.CollectionRadius)
                        {
                            collectableComponent.OnInteract(m_PlayerTransform.gameObject);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Update which interactable is currently selected based on player position and facing
        /// </summary>
        private void UpdateCollectableSelection()
        {
            if (!m_PlayerTransform || !m_MainCamera) 
            {
                if (!m_MainCamera)
                {
                    m_MainCamera = Camera.main;
                }
                return;
            }
            
            // Get player forward direction - use camera forward for better targeting
            var playerForward = m_PlayerTransform.transform.forward;
            playerForward.y = 0; // Ignore vertical angle
            playerForward.Normalize();
            
            // Find closest interactable in front of the player within the selection angle
            IInteractable bestInteractable = null;
            var closestDistance = float.MaxValue;
            
            foreach (var collectable in m_RegisteredCollectables)
            {
                if (collectable == null || !collectable.VisualObject) continue;
                
                Vector3 collectablePosition = collectable.VisualObject.transform.position;
                float distance = Vector3.Distance(m_PlayerTransform.position, collectablePosition);
                
                // Check if within selection range
                if (distance <= m_SelectionRange)
                {
                    // Calculate direction to interactable (ignore Y axis for better selection)
                    Vector3 directionToCollectable = collectablePosition - m_PlayerTransform.position;
                    directionToCollectable.y = 0;
                    directionToCollectable.Normalize();
                    
                    // Calculate angle between player forward and direction to interactable
                    float angle = Vector3.Angle(playerForward, directionToCollectable);
                    
                    // Check if within selection angle
                    if (angle <= m_MaxSelectionAngle)
                    {
                        // If this is the closest interactable so far, update best
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            bestInteractable = collectable;
                        }
                    }
                }
            }
            
            // Update selection
            SetSelectedCollectable(bestInteractable);
        }
        
        /// <summary>
        /// Set the currently selected interactable and create/update/destroy indicators
        /// </summary>
        private void SetSelectedCollectable(IInteractable interactable)
        {
            // If selection hasn't changed, do nothing
            if (_selectedInteractable == interactable) return;
            
            // Clear previous selection
            if (_selectedInteractable != null)
            {
                if (_selectionIndicators.TryGetValue(_selectedInteractable, out var oldIndicator))
                {
                    if (oldIndicator != null)
                    {
                        oldIndicator.SetActive(false);
                    }
                }
                
                // Call OnUnselected on the previously selected interactable
                _selectedInteractable.OnUnselected();
            }
            
            // Set new selection
            _selectedInteractable = interactable;
            
            // Create or activate indicator for new selection
            if (_selectedInteractable != null)
            {
                // Create indicator if it doesn't exist
                if (!_selectionIndicators.TryGetValue(_selectedInteractable, out var indicator) || indicator == null)
                {
                    CreateSelectionIndicator(_selectedInteractable);
                }
                else
                {
                    indicator.SetActive(true);
                }
                
                // Call OnSelected on the newly selected interactable
                _selectedInteractable.OnSelected();
                
                if (m_DebugMode)
                {
                    Debug.Log($"Selected interactable: {_selectedInteractable.Name} (ID: {_selectedInteractable.ID})");
                }
            }
        }
        
        /// <summary>
        /// Create a selection indicator for a interactable
        /// </summary>
        private void CreateSelectionIndicator(IInteractable interactable)
        {
            if (interactable == null || !interactable.VisualObject || !m_SelectionIndicatorPrefab) return;
            
            // Create indicator from prefab
            var indicator = Instantiate(m_SelectionIndicatorPrefab, Vector3.zero, Quaternion.identity);
            
            // Set up text component if it exists
            var textComponent = indicator.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent)
            {
                textComponent.text = interactable.Name;
                textComponent.color = m_SelectionTextColor;
            }
            else
            {
                var legacyText = indicator.GetComponentInChildren<Text>();
                if (legacyText)
                {
                    legacyText.text = interactable.Name;
                    legacyText.color = m_SelectionTextColor;
                }
            }
            
            // Add to dictionary
            _selectionIndicators[interactable] = indicator;
            
            // Update position
            UpdateIndicatorPosition(interactable, indicator);
        }
        
        /// <summary>
        /// Update positions of all active selection indicators
        /// </summary>
        private void UpdateSelectionIndicators()
        {
            if (_selectedInteractable != null && _selectionIndicators.TryGetValue(_selectedInteractable, out var indicator) && indicator != null)
            {
                UpdateIndicatorPosition(_selectedInteractable, indicator);
            }
        }
        
        /// <summary>
        /// Update the position of a specific indicator
        /// </summary>
        private void UpdateIndicatorPosition(IInteractable interactable, GameObject indicator)
        {
            if (interactable == null || !interactable.VisualObject || !indicator) return;
            
            // Position above the interactable
            var position = interactable.VisualObject.transform.position + Vector3.up * m_SelectionTextOffset;
            indicator.transform.position = position;
            
            // Make indicator face the camera
            if (m_MainCamera != null)
            {
                indicator.transform.rotation = m_MainCamera.transform.rotation;
            }
        }
        
        private void OnCollectActionPerformed(InputAction.CallbackContext context)
        {
            CollectSelectedItem();
        }
        
        private void OnDrawGizmos()
        {
            if (!m_DebugMode) return;
            
            // Draw collection range spheres
            if (m_PlayerTransform != null)
            {
                // Draw manual collection range
                Gizmos.color = new Color(1, 0, 0, 0.2f);
                Gizmos.DrawSphere(m_PlayerTransform.position, 3f);
                
                // Draw selection range
                Gizmos.color = new Color(1, 1, 0, 0.2f);
                Gizmos.DrawSphere(m_PlayerTransform.position, m_SelectionRange);
                
                // Draw selection cone
                if (m_MainCamera != null)
                {
                    Vector3 forward = m_MainCamera.transform.forward;
                    forward.y = 0;
                    forward.Normalize();
                    
                    Gizmos.color = new Color(1, 0.5f, 0, 0.5f);
                    DrawSelectionCone(m_PlayerTransform.position, forward, m_MaxSelectionAngle, m_SelectionRange);
                }
                
                // Draw line to selected interactable
                if (_selectedInteractable != null && _selectedInteractable.VisualObject != null)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(m_PlayerTransform.position, _selectedInteractable.VisualObject.transform.position);
                }
            }
        }
        
        /// <summary>
        /// Draw a selection cone in the editor for visualization
        /// </summary>
        private void DrawSelectionCone(Vector3 origin, Vector3 direction, float maxAngle, float radius)
        {
            const int segments = 12;
            float angleStep = maxAngle / segments;
            
            for (int i = 0; i < segments; i++)
            {
                float angle1 = angleStep * i;
                float angle2 = angleStep * (i + 1);
                
                Vector3 dir1 = Quaternion.AngleAxis(angle1, Vector3.up) * direction;
                Vector3 dir2 = Quaternion.AngleAxis(angle2, Vector3.up) * direction;
                
                Vector3 point1 = origin + dir1 * radius;
                Vector3 point2 = origin + dir2 * radius;
                
                Gizmos.DrawLine(origin, point1);
                Gizmos.DrawLine(point1, point2);
                
                // Mirror to the other side
                dir1 = Quaternion.AngleAxis(-angle1, Vector3.up) * direction;
                dir2 = Quaternion.AngleAxis(-angle2, Vector3.up) * direction;
                
                point1 = origin + dir1 * radius;
                point2 = origin + dir2 * radius;
                
                Gizmos.DrawLine(origin, point1);
                Gizmos.DrawLine(point1, point2);
            }
        }
        #endregion
    }
} 