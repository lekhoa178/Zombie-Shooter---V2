using System.Collections.Generic;
using UnityEngine;

namespace DungeonGeneration
{
    /// <summary>
    /// Container for managing and aligning multiple portal spawners.
    /// Places spawners in a circular or linear formation based on its rotation.
    /// </summary>
    public class PortalSpawnerContainer : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Spawner Settings")]
        [SerializeField] private bool m_UseCircularFormation = true;
        [SerializeField] private float m_CircleRadius = 5f;
        [SerializeField] private float m_AngularOffset = 0f;
        [SerializeField] private float m_LinearSpacing = 2f;
        [SerializeField] private Transform m_SpawnersParent;
        
        [Header("Visualization")]
        [SerializeField] private bool m_ShowGizmos = true;
        [SerializeField] private Color m_GizmoColor = Color.cyan;
        #endregion
        
        #region Private Fields
        private List<TransitionSpawner> m_ActiveSpawners = new List<TransitionSpawner>();
        #endregion
        
        #region Unity Lifecycle
        private void Awake()
        {
            // Create parent transform if not assigned
            if (m_SpawnersParent == null)
            {
                GameObject parent = new GameObject("SpawnersParent");
                parent.transform.SetParent(transform, false);
                parent.transform.localPosition = Vector3.zero;
                parent.transform.localRotation = Quaternion.identity;
                m_SpawnersParent = parent.transform;
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!m_ShowGizmos) return;
            
            Gizmos.color = m_GizmoColor;
            
            // Draw container direction
            Gizmos.DrawRay(transform.position, transform.forward * 2f);
            
            // Draw formation shape
            if (m_UseCircularFormation)
            {
                // Draw circle at container position
                Vector3 lastPos = transform.position + (transform.rotation * Quaternion.Euler(0, 0, 0) * Vector3.forward * m_CircleRadius);
                
                for (int i = 1; i <= 36; i++)
                {
                    float angle = i * 10f;
                    Vector3 nextPos = transform.position + (transform.rotation * Quaternion.Euler(0, angle, 0) * Vector3.forward * m_CircleRadius);
                    Gizmos.DrawLine(lastPos, nextPos);
                    lastPos = nextPos;
                }
            }
            else
            {
                // Draw line for linear formation
                Vector3 right = transform.right;
                float halfLength = m_LinearSpacing * 2.5f; // Just for visualization
                Gizmos.DrawLine(
                    transform.position - right * halfLength,
                    transform.position + right * halfLength);
            }
        }
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Calculates positions for multiple portals based on container's orientation
        /// </summary>
        /// <param name="_portalCount">Number of portals to position</param>
        /// <param name="_minDistance">Minimum distance between portals</param>
        /// <param name="_maxDistance">Maximum distance between portals</param>
        /// <returns>List of world space positions for portals</returns>
        public List<Vector3> CalculatePortalPositions(int _portalCount, float _minDistance, float _maxDistance)
        {
            List<Vector3> positions = new List<Vector3>();
            
            if (_portalCount <= 0)
                return positions;
            
            // Adjust spacings based on min/max constraints
            float effectiveSpacing = Mathf.Clamp(m_LinearSpacing, _minDistance, _maxDistance);
            
            if (m_UseCircularFormation)
            {
                // Calculate angular spacing based on portal count
                float angleStep = 360f / _portalCount;
                
                // Adjust circle radius to maintain minimum distance if needed
                float circumference = 2f * Mathf.PI * m_CircleRadius;
                float arcLength = circumference / _portalCount;
                
                if (arcLength < _minDistance)
                {
                    // Increase radius to ensure minimum distance
                    float newRadius = (_minDistance * _portalCount) / (2f * Mathf.PI);
                    m_CircleRadius = newRadius;
                }
                else if (arcLength > _maxDistance && _portalCount > 1)
                {
                    // Decrease radius to ensure maximum distance
                    float newRadius = (_maxDistance * _portalCount) / (2f * Mathf.PI);
                    m_CircleRadius = newRadius;
                }
                
                // Calculate positions in circle
                for (int i = 0; i < _portalCount; i++)
                {
                    float angle = i * angleStep + m_AngularOffset;
                    Vector3 positionOffset = transform.rotation * Quaternion.Euler(0, angle, 0) * Vector3.forward * m_CircleRadius;
                    positions.Add(transform.position + positionOffset);
                }
            }
            else
            {
                // Linear formation
                // Calculate total length
                float totalLength = (_portalCount - 1) * effectiveSpacing;
                
                // Position portals in a line
                for (int i = 0; i < _portalCount; i++)
                {
                    float offset = (i - (_portalCount - 1) / 2f) * effectiveSpacing;
                    Vector3 positionOffset = transform.right * offset;
                    positions.Add(transform.position + positionOffset);
                }
            }
            
            return positions;
        }
        
        /// <summary>
        /// Spawns a transition spawner at the specified position
        /// </summary>
        /// <param name="_spawnerPrefab">Prefab to instantiate</param>
        /// <param name="_position">World position to spawn at</param>
        /// <returns>The spawned TransitionSpawner component</returns>
        public TransitionSpawner SpawnTransitionSpawner(GameObject _spawnerPrefab, Vector3 _position)
        {
            if (_spawnerPrefab == null)
                return null;
            
            // Instantiate the spawner prefab
            GameObject spawnerObj = Instantiate(_spawnerPrefab, _position, transform.rotation);
            
            // Parent to container
            spawnerObj.transform.SetParent(m_SpawnersParent, true);
            
            // Get the TransitionSpawner component
            TransitionSpawner spawner = spawnerObj.GetComponent<TransitionSpawner>();
            if (spawner != null)
            {
                m_ActiveSpawners.Add(spawner);
            }
            
            return spawner;
        }
        
        /// <summary>
        /// Clears all active spawners
        /// </summary>
        public void ClearAllSpawners()
        {
            foreach (TransitionSpawner spawner in m_ActiveSpawners)
            {
                if (spawner != null)
                {
                    Destroy(spawner.gameObject);
                }
            }
            
            m_ActiveSpawners.Clear();
        }
        #endregion
    }
} 