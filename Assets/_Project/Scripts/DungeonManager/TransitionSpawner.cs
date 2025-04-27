using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace DungeonGeneration
{
    /// <summary>
    /// Component responsible for spawning transition portals/doors between stages
    /// </summary>
    public class TransitionSpawner : MonoBehaviour
    {
        #region Serialized Fields
        [SerializeField] private GameObject m_PortalPrefab;
        [SerializeField] private float m_MinDistanceFromOtherSpawners = 3f;
        [SerializeField] private bool m_IsUsed = false;
        #endregion

        #region Public Properties
        public bool IsUsed => m_IsUsed;
        #endregion

        /// <summary>
        /// Spawns a portal to the next stage
        /// </summary>
        /// <param name="targetStageID">The ID of the target stage</param>
        /// <param name="targetSceneName">The scene name to load</param>
        /// <param name="displayName">The display name of the target stage</param>
        /// <returns>The spawned portal GameObject</returns>
        public GameObject SpawnPortal(string targetStageID, string targetSceneName, string displayName)
        {
            if (m_IsUsed)
            {
                Debug.LogWarning("Attempting to use an already used transition spawner!");
                return null;
            }

            var portalInstance = Instantiate(m_PortalPrefab, transform.position, transform.rotation);
            
            var portal = portalInstance.GetComponent<StagePortal>();
            if (portal)
            {
                portal.Initialize(targetStageID, targetSceneName, displayName);
            }
            else
            {
                Debug.LogError("Portal prefab does not have a StagePortal component!");
            }

            m_IsUsed = true;
            
            return portalInstance;
        }
    }

    /// <summary>
    /// Static utility class for finding and creating transition spawners
    /// </summary>
    public static class TransitionSpawnerUtility
    {
        private const int CMaxAttempts = 30;

        /// <summary>
        /// Finds an available transition spawner in the scene
        /// </summary>
        public static TransitionSpawner FindAvailableSpawner()
        {
            TransitionSpawner[] spawners = Object.FindObjectsOfType<TransitionSpawner>();
            
            foreach (TransitionSpawner spawner in spawners)
            {
                if (!spawner.IsUsed)
                {
                    return spawner;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Creates a new transition spawner at a valid position on the NavMesh
        /// </summary>
        public static TransitionSpawner CreateNewSpawner(GameObject _spawnerPrefab, Vector3 _playerPosition, float _minDistance, float _maxDistance)
        {
            for (int i = 0; i < CMaxAttempts; i++)
            {
                // Try to find a random position on the NavMesh
                Vector3 randomDirection = Random.insideUnitSphere * _maxDistance;
                randomDirection += _playerPosition;
                
                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomDirection, out hit, _maxDistance, NavMesh.AllAreas))
                {
                    // Check if it's far enough from player
                    if (!(Vector3.Distance(hit.position, _playerPosition) >= _minDistance)) continue;
                    
                    // Check if it's far enough from other spawners
                    var isTooClose = Object.FindObjectsByType<TransitionSpawner>(
                            FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                        .Any(spawner => Vector3.Distance(hit.position, spawner.transform.position) < 5);

                    if (isTooClose) continue;
                    
                    // Create the spawner prefab
                    var spawnerObject = Object.Instantiate(_spawnerPrefab, hit.position, Quaternion.identity);
                    return spawnerObject.GetComponent<TransitionSpawner>();
                }
            }
            
            Debug.LogWarning("Failed to create a new transition spawner after " + CMaxAttempts + " attempts");
            return null;
        }
    }
} 