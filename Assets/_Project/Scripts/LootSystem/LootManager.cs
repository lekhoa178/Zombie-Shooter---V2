using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace LocomotionSystem
{
    /// <summary>
    /// Manager class for all loot-related functionality
    /// </summary>
    public class LootManager : MonoBehaviour
    {
        #region Singleton
        private static LootManager s_Instance;
        
        public static LootManager Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = FindObjectOfType<LootManager>();
                    
                    if (s_Instance == null)
                    {
                        GameObject obj = new GameObject("LootManager");
                        s_Instance = obj.AddComponent<LootManager>();
                    }
                }
                
                return s_Instance;
            }
        }
        #endregion
        
        #region Private Fields
        [Header("Collection Settings")]
        [SerializeField] private bool m_EnableAutoCollection = true;
        [SerializeField] private bool m_EnableManualCollection = true;
        [SerializeField] private float m_AutoCollectionCheckInterval = 0.5f;
        [SerializeField] private float m_MagnetRange = 8f;
        [SerializeField] private LayerMask m_CollectableLayerMask;
        [SerializeField] private InputActionReference m_CollectAction;
        
        [Header("Selection Settings")]
        [SerializeField] private float m_SelectionRange = 10f;
        [SerializeField] private float m_SelectionUpdateInterval = 0.2f;
        [SerializeField] private float m_MaxSelectionAngle = 45f;
        [SerializeField] private GameObject m_SelectionIndicatorPrefab;
        [SerializeField] private Color m_SelectionTextColor = Color.yellow;
        [SerializeField] private float m_SelectionTextOffset = 1.5f;
        
        [Header("Debug")]
        [SerializeField] private bool m_DebugMode = false;
        
        private List<ICollectable> m_RegisteredCollectables = new List<ICollectable>();
        private Dictionary<string, ICollectable> m_CollectablesByID = new Dictionary<string, ICollectable>();
        private float m_AutoCollectionTimer;
        private float m_SelectionUpdateTimer;
        private Transform m_PlayerTransform;
        private Camera m_MainCamera;
        
        // Currently selected collectable
        private ICollectable m_SelectedCollectable;
        private Dictionary<ICollectable, GameObject> m_SelectionIndicators = new Dictionary<ICollectable, GameObject>();
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Ensure singleton pattern
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            s_Instance = this;
            DontDestroyOnLoad(gameObject);
            
            m_MainCamera = Camera.main;
        }

        private void Start()
        {
            // Find player reference
            Player player = FindAnyObjectByType<Player>();
            if (player != null)
            {
                m_PlayerTransform = player.transform;
            }
            
            // Set up input action if available
            if (m_CollectAction != null)
            {
                m_CollectAction.action.performed += OnCollectActionPerformed;
                m_CollectAction.action.Enable();
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from input action
            if (m_CollectAction != null)
            {
                m_CollectAction.action.performed -= OnCollectActionPerformed;
                m_CollectAction.action.Disable();
            }
            
            // Clear selection indicators
            foreach (var indicator in m_SelectionIndicators.Values)
            {
                if (indicator != null)
                {
                    Destroy(indicator);
                }
            }
            
            // Clear references
            m_RegisteredCollectables.Clear();
            m_CollectablesByID.Clear();
            m_SelectionIndicators.Clear();
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
            
            // Handle automatic collection at intervals
            if (m_EnableAutoCollection)
            {
                m_AutoCollectionTimer -= Time.deltaTime;
                
                if (m_AutoCollectionTimer <= 0f)
                {
                    m_AutoCollectionTimer = m_AutoCollectionCheckInterval;
                    CollectNearbyItems(false);
                }
            }
            
            // Update collectable selection
            m_SelectionUpdateTimer -= Time.deltaTime;
            if (m_SelectionUpdateTimer <= 0f)
            {
                m_SelectionUpdateTimer = m_SelectionUpdateInterval;
                UpdateCollectableSelection();
            }
        }
        
        private void Update()
        {
            // Update position of selection indicators
            UpdateSelectionIndicators();
        }
        #endregion

        #region Public Methods
        public void RegisterCollectable(ICollectable collectable)
        {
            if (collectable == null) return;
            
            if (!m_RegisteredCollectables.Contains(collectable))
            {
                m_RegisteredCollectables.Add(collectable);
                m_CollectablesByID[collectable.ID] = collectable;
                
                if (m_DebugMode)
                {
                    Debug.Log($"Registered collectable: {collectable.Name} (ID: {collectable.ID})");
                }
            }
        }

        public void UnregisterCollectable(ICollectable collectable)
        {
            if (collectable == null) return;
            
            if (m_RegisteredCollectables.Contains(collectable))
            {
                // If this was the selected collectable, clear selection
                if (m_SelectedCollectable == collectable)
                {
                    SetSelectedCollectable(null);
                }
                
                m_RegisteredCollectables.Remove(collectable);
                
                if (m_CollectablesByID.ContainsKey(collectable.ID))
                {
                    m_CollectablesByID.Remove(collectable.ID);
                }
                
                // Remove selection indicator if it exists
                if (m_SelectionIndicators.TryGetValue(collectable, out var indicator))
                {
                    if (indicator != null)
                    {
                        Destroy(indicator);
                    }
                    m_SelectionIndicators.Remove(collectable);
                }
                
                if (m_DebugMode)
                {
                    Debug.Log($"Unregistered collectable: {collectable.Name} (ID: {collectable.ID})");
                }
            }
        }

        public ICollectable GetCollectableByID(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            
            if (m_CollectablesByID.TryGetValue(id, out ICollectable collectable))
            {
                return collectable;
            }
            
            return null;
        }

        public List<ICollectable> GetNearbyCollectables(Vector3 position, float _radius)
        {
            return m_RegisteredCollectables
                .Where(c => c != null && c.VisualObject != null && Vector3.Distance(position, c.VisualObject.transform.position) <= _radius)
                .ToList();
        }
        
        /// <summary>
        /// Get the currently selected collectable
        /// </summary>
        public ICollectable GetSelectedCollectable()
        {
            return m_SelectedCollectable;
        }
        
        /// <summary>
        /// Try to collect the currently selected collectable
        /// </summary>
        public void CollectSelectedItem()
        {
            if (m_SelectedCollectable == null) return;
            
            if (m_SelectedCollectable is CollectableBase collectableBase)
            {
                collectableBase.OnCollected(m_PlayerTransform.gameObject);
            }
        }
        #endregion

        #region Private Methods
        private void CollectNearbyItems(bool _isManualCollection)
        {
            if (m_PlayerTransform == null) return;
            
            // For manual collection, collect the selected item or use collider check
            if (_isManualCollection)
            {
                // If we have a selected item, collect it
                if (m_SelectedCollectable != null && m_SelectedCollectable is CollectableBase collectableBase)
                {
                    // Check if it's in collection range
                    if (Vector3.Distance(m_PlayerTransform.position, collectableBase.transform.position) <= collectableBase.CollectionRadius)
                    {
                        collectableBase.OnCollected(m_PlayerTransform.gameObject);
                        return;
                    }
                }
                
                // Otherwise, use the collider-based collection as before
                float collectRadius = 3f; // Default manual collection radius
                
                // Get all colliders in range
                Collider[] colliders = Physics.OverlapSphere(m_PlayerTransform.position, collectRadius, m_CollectableLayerMask);
                
                foreach (Collider collider in colliders)
                {
                    CollectableBase collectableComponent = collider.GetComponent<CollectableBase>();
                    
                    if (collectableComponent != null)
                    {
                        // Use the collectable's own collection radius if available
                        if (Vector3.Distance(m_PlayerTransform.position, collectableComponent.transform.position) <= collectableComponent.CollectionRadius)
                        {
                            collectableComponent.OnCollected(m_PlayerTransform.gameObject);
                        }
                    }
                }
            }
            // For automatic collection, we check registered collectables
            else
            {
                // Get all registered collectables within range
                List<CollectableBase> nearbyCollectables = new List<CollectableBase>();
                
                foreach (ICollectable collectable in m_RegisteredCollectables.ToList()) // Create a copy to avoid modification during iteration
                {
                    if (collectable == null || collectable.VisualObject == null) continue;
                    
                    // Check if the collectable is a CollectableBase component
                    if (collectable is CollectableBase collectableBase)
                    {
                        if (!collectableBase.IsMovingToPlayer && 
                            Vector3.Distance(m_PlayerTransform.position, collectableBase.transform.position) <= collectableBase.AutoCollectionRadius)
                        {
                            nearbyCollectables.Add(collectableBase);
                        }
                    }
                }
                
                // Start moving collectables toward player
                foreach (CollectableBase collectable in nearbyCollectables)
                {
                    collectable.MoveToPlayer(m_PlayerTransform);
                }
            }
        }
        
        /// <summary>
        /// Update which collectable is currently selected based on player position and facing
        /// </summary>
        private void UpdateCollectableSelection()
        {
            if (m_PlayerTransform == null || m_MainCamera == null) 
            {
                if (m_MainCamera == null)
                {
                    m_MainCamera = Camera.main;
                }
                return;
            }
            
            // Get player forward direction - use camera forward for better targeting
            Vector3 playerForward = m_PlayerTransform.transform.forward;
            playerForward.y = 0; // Ignore vertical angle
            playerForward.Normalize();
            
            // Find closest collectable in front of the player within the selection angle
            ICollectable bestCollectable = null;
            float closestDistance = float.MaxValue;
            
            foreach (var collectable in m_RegisteredCollectables)
            {
                if (collectable == null || collectable.VisualObject == null) continue;
                
                Vector3 collectablePosition = collectable.VisualObject.transform.position;
                float distance = Vector3.Distance(m_PlayerTransform.position, collectablePosition);
                
                // Check if within selection range
                if (distance <= m_SelectionRange)
                {
                    // Calculate direction to collectable (ignore Y axis for better selection)
                    Vector3 directionToCollectable = collectablePosition - m_PlayerTransform.position;
                    directionToCollectable.y = 0;
                    directionToCollectable.Normalize();
                    
                    // Calculate angle between player forward and direction to collectable
                    float angle = Vector3.Angle(playerForward, directionToCollectable);
                    
                    // Check if within selection angle
                    if (angle <= m_MaxSelectionAngle)
                    {
                        // If this is the closest collectable so far, update best
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            bestCollectable = collectable;
                        }
                    }
                }
            }
            
            // Update selection
            SetSelectedCollectable(bestCollectable);
        }
        
        /// <summary>
        /// Set the currently selected collectable and create/update/destroy indicators
        /// </summary>
        private void SetSelectedCollectable(ICollectable collectable)
        {
            // If selection hasn't changed, do nothing
            if (m_SelectedCollectable == collectable) return;
            
            // Clear previous selection
            if (m_SelectedCollectable != null)
            {
                if (m_SelectionIndicators.TryGetValue(m_SelectedCollectable, out var oldIndicator))
                {
                    if (oldIndicator != null)
                    {
                        oldIndicator.SetActive(false);
                    }
                }
                
                // Call OnUnselected on the previously selected collectable
                m_SelectedCollectable.OnUnselected();
            }
            
            // Set new selection
            m_SelectedCollectable = collectable;
            
            // Create or activate indicator for new selection
            if (m_SelectedCollectable != null)
            {
                // Create indicator if it doesn't exist
                if (!m_SelectionIndicators.TryGetValue(m_SelectedCollectable, out var indicator) || indicator == null)
                {
                    CreateSelectionIndicator(m_SelectedCollectable);
                }
                else
                {
                    indicator.SetActive(true);
                }
                
                // Call OnSelected on the newly selected collectable
                m_SelectedCollectable.OnSelected();
                
                if (m_DebugMode)
                {
                    Debug.Log($"Selected collectable: {m_SelectedCollectable.Name} (ID: {m_SelectedCollectable.ID})");
                }
            }
        }
        
        /// <summary>
        /// Create a selection indicator for a collectable
        /// </summary>
        private void CreateSelectionIndicator(ICollectable collectable)
        {
            if (collectable == null || collectable.VisualObject == null || m_SelectionIndicatorPrefab == null) return;
            
            // Create indicator from prefab
            var indicator = Instantiate(m_SelectionIndicatorPrefab, Vector3.zero, Quaternion.identity);
            
            // Set up text component if it exists
            var textComponent = indicator.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = collectable.Name;
                textComponent.color = m_SelectionTextColor;
            }
            else
            {
                var legacyText = indicator.GetComponentInChildren<Text>();
                if (legacyText != null)
                {
                    legacyText.text = collectable.Name;
                    legacyText.color = m_SelectionTextColor;
                }
            }
            
            // Add to dictionary
            m_SelectionIndicators[collectable] = indicator;
            
            // Update position
            UpdateIndicatorPosition(collectable, indicator);
        }
        
        /// <summary>
        /// Update positions of all active selection indicators
        /// </summary>
        private void UpdateSelectionIndicators()
        {
            if (m_SelectedCollectable != null && m_SelectionIndicators.TryGetValue(m_SelectedCollectable, out var indicator) && indicator != null)
            {
                UpdateIndicatorPosition(m_SelectedCollectable, indicator);
            }
        }
        
        /// <summary>
        /// Update the position of a specific indicator
        /// </summary>
        private void UpdateIndicatorPosition(ICollectable collectable, GameObject indicator)
        {
            if (collectable == null || collectable.VisualObject == null || indicator == null) return;
            
            // Position above the collectable
            Vector3 position = collectable.VisualObject.transform.position + Vector3.up * m_SelectionTextOffset;
            indicator.transform.position = position;
            
            // Make indicator face the camera
            if (m_MainCamera != null)
            {
                indicator.transform.rotation = m_MainCamera.transform.rotation;
            }
        }
        
        private void OnCollectActionPerformed(InputAction.CallbackContext _context)
        {
            if (m_EnableManualCollection)
            {
                CollectNearbyItems(true);
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!m_DebugMode) return;
            
            // Draw collection range spheres
            if (m_PlayerTransform != null)
            {
                // Draw automatic collection range
                Gizmos.color = new Color(0, 1, 0, 0.2f);
                Gizmos.DrawSphere(m_PlayerTransform.position, m_MagnetRange);
                
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
                
                // Draw line to selected collectable
                if (m_SelectedCollectable != null && m_SelectedCollectable.VisualObject != null)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(m_PlayerTransform.position, m_SelectedCollectable.VisualObject.transform.position);
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