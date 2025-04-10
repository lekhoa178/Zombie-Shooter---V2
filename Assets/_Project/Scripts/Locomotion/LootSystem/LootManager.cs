using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LocomotionSystem.LootSystem
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
        [SerializeField] private bool m_EnablePassiveCollection = true;
        [SerializeField] private bool m_EnableActiveCollection = true;
        [SerializeField] private float m_PassiveCollectionCheckInterval = 0.5f;
        [SerializeField] private float m_MagnetRange = 8f;
        [SerializeField] private LayerMask m_CollectableLayerMask;
        [SerializeField] private KeyCode m_CollectKey = KeyCode.F;
        [SerializeField] private InputActionReference m_CollectAction;
        
        [Header("Debug")]
        [SerializeField] private bool m_DebugMode = false;
        
        private List<ICollectable> m_RegisteredCollectables = new List<ICollectable>();
        private Dictionary<string, ICollectable> m_CollectablesByID = new Dictionary<string, ICollectable>();
        private float m_PassiveCollectionTimer;
        private Transform m_PlayerTransform;
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
        }

        private void Start()
        {
            // Find player reference
            Player player = FindObjectOfType<Player>();
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
            
            // Clear references
            m_RegisteredCollectables.Clear();
            m_CollectablesByID.Clear();
        }

        private void Update()
        {
            if (m_PlayerTransform == null)
            {
                // Try to find player reference again
                Player player = FindObjectOfType<Player>();
                if (player != null)
                {
                    m_PlayerTransform = player.transform;
                }
                else
                {
                    return; // No player found
                }
            }
            
            // Check for active collection input
            if (m_EnableActiveCollection && Input.GetKeyDown(m_CollectKey))
            {
                CollectNearbyItems(true);
            }
            
            // Handle passive collection at intervals
            if (m_EnablePassiveCollection)
            {
                m_PassiveCollectionTimer -= Time.deltaTime;
                
                if (m_PassiveCollectionTimer <= 0f)
                {
                    m_PassiveCollectionTimer = m_PassiveCollectionCheckInterval;
                    CollectNearbyItems(false);
                }
            }
        }
        #endregion

        #region Public Methods
        public void RegisterCollectable(ICollectable _collectable)
        {
            if (_collectable == null) return;
            
            if (!m_RegisteredCollectables.Contains(_collectable))
            {
                m_RegisteredCollectables.Add(_collectable);
                m_CollectablesByID[_collectable.ID] = _collectable;
                
                if (m_DebugMode)
                {
                    Debug.Log($"Registered collectable: {_collectable.Name} (ID: {_collectable.ID})");
                }
            }
        }

        public void UnregisterCollectable(ICollectable _collectable)
        {
            if (_collectable == null) return;
            
            if (m_RegisteredCollectables.Contains(_collectable))
            {
                m_RegisteredCollectables.Remove(_collectable);
                
                if (m_CollectablesByID.ContainsKey(_collectable.ID))
                {
                    m_CollectablesByID.Remove(_collectable.ID);
                }
                
                if (m_DebugMode)
                {
                    Debug.Log($"Unregistered collectable: {_collectable.Name} (ID: {_collectable.ID})");
                }
            }
        }

        public ICollectable GetCollectableByID(string _id)
        {
            if (string.IsNullOrEmpty(_id)) return null;
            
            if (m_CollectablesByID.TryGetValue(_id, out ICollectable collectable))
            {
                return collectable;
            }
            
            return null;
        }

        public List<ICollectable> GetNearbyCollectables(Vector3 _position, float _radius)
        {
            return m_RegisteredCollectables
                .Where(c => c != null && c.VisualObject != null && Vector3.Distance(_position, c.VisualObject.transform.position) <= _radius)
                .ToList();
        }
        #endregion

        #region Private Methods
        private void CollectNearbyItems(bool _isActiveCollection)
        {
            if (m_PlayerTransform == null) return;
            
            // For active collection, we use collider check to get more accurate results
            if (_isActiveCollection)
            {
                float collectRadius = 3f; // Default active collection radius
                
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
            // For passive collection, we check registered collectables
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
        
        private void OnCollectActionPerformed(InputAction.CallbackContext _context)
        {
            if (m_EnableActiveCollection)
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
                // Draw passive collection range
                Gizmos.color = new Color(0, 1, 0, 0.2f);
                Gizmos.DrawSphere(m_PlayerTransform.position, m_MagnetRange);
                
                // Draw active collection range
                Gizmos.color = new Color(1, 0, 0, 0.2f);
                Gizmos.DrawSphere(m_PlayerTransform.position, 3f);
            }
        }
        #endregion
    }
} 