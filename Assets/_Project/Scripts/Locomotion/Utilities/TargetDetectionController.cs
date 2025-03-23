using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LocomotionSystem
{
    public class TargetDetectionController : MonoBehaviour
    {

        [Header("Components")] public PlayerInput Input { get; private set; }

        [Header("Scene")] public List<Transform> allTargetsInScene = new List<Transform>();

        [Space] [Header("Target Detection")] public LayerMask whatIsEnemy;
        public bool canChangeTarget = true;

        [Tooltip("Detection Range: \n Player range for detecting potential targets.")] [Range(0f, 15f)]
        public float detectionRange = 10f;

        [Tooltip(
            "Dot Product Threshold \nHigher Values: More strict alignment required \nLower Values: Allows for broader targeting")]
        [Range(0f, 1f)]
        public float dotProductThreshold = 0.15f;

        [Space] [Header("Debug")] public bool debug;
        public Transform checkPos;

        private GameObject targetEnemy;

        private Camera mainCamera;

        private void Awake()
        {
            mainCamera = Camera.main;
            
            Input = FindAnyObjectByType<PlayerInput>();
        }

        // Start is called before the first frame update
        private void Start()
        {
            PopulateTargetInScene();
            StartCoroutine(RunEveryXms());
        }

        private void PopulateTargetInScene()
        {
            // Find all active GameObjects in the scene
            GameObject[] allGameObjects = GameObject.FindGameObjectsWithTag("Enemy");

            // Convert the array to a list
            List<GameObject> gameObjectList = new List<GameObject>(allGameObjects);

            // Output the number of GameObjects found
            if (debug)
                Debug.Log("Number of targets found: " + gameObjectList.Count);

            // Optionally, iterate over the list and do something with each GameObject
            foreach (var obj in gameObjectList)
            {
                allTargetsInScene.Add(obj.transform);
            }
        }

        private IEnumerator RunEveryXms()
        {
            while (true)
            {
                yield return new WaitForSeconds(.1f); // Wait for 'x' milliseconds
                GetEnemyInInputDirection();
            }
        }
        
        public GameObject GetTargetEnemy()
        {
            if (!targetEnemy) return null;
            
            if (Vector3.Distance(targetEnemy.transform.position, transform.position) < detectionRange)
            {
                return targetEnemy;
            }
            
            return null;
        }

        #region Get Enemy In Input Direction

        private void GetEnemyInInputDirection()
        {
            if (!canChangeTarget) return;

            var moveDirection = Input.PlayerActions.Movement.ReadValue<Vector2>();
            var inputDirection = new Vector3(moveDirection.x, 0, moveDirection.y);

            if (inputDirection == Vector3.zero) return;
            
            inputDirection = mainCamera.transform.TransformDirection(inputDirection);
            inputDirection.y = 0;
            inputDirection.Normalize();


            var closestEnemy = GetClosestEnemyInDirection(inputDirection);

            if (closestEnemy &&
                (Vector3.Distance(transform.position, closestEnemy.position)) <= detectionRange)
            {
                targetEnemy = closestEnemy.gameObject;
            }
        }

        private Transform GetClosestEnemyInDirection(Vector3 inputDirection)
        {
            Transform closestEnemy = null;
            var minDistance = 99999f;

            foreach (var enemy in allTargetsInScene)
            {
                var enemyDirection = (enemy.position - transform.position).normalized;
                var dotProduct = Vector3.Dot(inputDirection, enemyDirection);
                var distance = Vector3.SqrMagnitude(enemy.position - transform.position);

                if (dotProduct > dotProductThreshold && distance < minDistance)
                {
                    minDistance = distance;
                    closestEnemy = enemy;
                }
            }

            return closestEnemy;
        }

        #endregion
        
        public void SetDetectionParameters(float range, float threshold)
        {
            detectionRange = range;
            dotProductThreshold = threshold;
            
            if (debug)
            {
                Debug.Log($"Target detection parameters updated - Range: {range}, Threshold: {threshold}");
            }
        }
        
        public void ResetToDefaultParameters(float defaultRange = 10f, float defaultThreshold = 0.15f)
        {
            detectionRange = defaultRange;
            dotProductThreshold = defaultThreshold;
        }

        #region Gizmos Debug Visualization

        private void OnDrawGizmosSelected()
        {
            if (!debug) return;
            
            // Draw the detection range as a wire sphere
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // Show the current target with a line
            if (targetEnemy != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, targetEnemy.transform.position);
                
                // Draw sphere on the target
                Gizmos.DrawWireSphere(targetEnemy.transform.position, 0.5f);
            }
            
            // Calculate the angle from the dot product threshold
            // dot = cos(angle) => angle = acos(dot)
            float angleInRadians = Mathf.Acos(dotProductThreshold);
            float angleInDegrees = angleInRadians * Mathf.Rad2Deg;
            
            // Draw the directional cone representing the dot product threshold
            DrawDirectionalCone(angleInDegrees);
            
            // Draw lines to potential targets within detection range
            if (Application.isPlaying)
            {
                Vector3 inputDirection = Vector3.zero;
                
                // Get input direction if available
                if (Input != null)
                {
                    var moveDirection = Input.PlayerActions.Movement.ReadValue<Vector2>();
                    inputDirection = new Vector3(moveDirection.x, 0, moveDirection.y);
                    
                    if (inputDirection != Vector3.zero && mainCamera != null)
                    {
                        inputDirection = mainCamera.transform.TransformDirection(inputDirection);
                        inputDirection.y = 0;
                        inputDirection.Normalize();
                        
                        // Draw the input direction
                        Gizmos.color = Color.blue;
                        Gizmos.DrawRay(transform.position, inputDirection * 3f);
                    }
                }
                
                // Draw lines to all targets and highlight valid ones
                foreach (var target in allTargetsInScene)
                {
                    if (target == null) continue;
                    
                    Vector3 directionToTarget = (target.position - transform.position).normalized;
                    float distance = Vector3.Distance(transform.position, target.position);
                    
                    if (distance <= detectionRange)
                    {
                        float dotProduct = Vector3.Dot(inputDirection, directionToTarget);
                        
                        // Color based on whether it's within the dot product threshold
                        if (dotProduct > dotProductThreshold && inputDirection != Vector3.zero)
                        {
                            Gizmos.color = new Color(0, 1, 0, 0.5f); // Green for valid targets
                        }
                        else
                        {
                            Gizmos.color = new Color(1, 0.5f, 0, 0.3f); // Orange for in-range but wrong angle
                        }
                        
                        Gizmos.DrawLine(transform.position, target.position);
                    }
                    else
                    {
                        // Out of range targets
                        Gizmos.color = new Color(1, 0, 0, 0.1f); // Light red for out-of-range
                        Gizmos.DrawLine(transform.position, target.position);
                    }
                }
            }
        }

        private void DrawDirectionalCone(float angle)
        {
            if (angle > 175f) 
            {
                // Don't draw the cone if angle is too large (almost a full sphere)
                return;
            }
            
            Vector3 inputDirection = Vector3.forward;
            Camera cam = Camera.current ?? Camera.main;
            
            if (Application.isPlaying && Input != null && mainCamera != null)
            {
                var moveDirection = Input.PlayerActions.Movement.ReadValue<Vector2>();
                if (moveDirection != Vector2.zero)
                {
                    inputDirection = new Vector3(moveDirection.x, 0, moveDirection.y);
                    inputDirection = mainCamera.transform.TransformDirection(inputDirection);
                    inputDirection.y = 0;
                    inputDirection.Normalize();
                }
            }
            
            // Transform direction to be relative to the camera in Editor mode
            if (!Application.isPlaying && cam != null)
            {
                inputDirection = cam.transform.forward;
                inputDirection.y = 0;
                inputDirection.Normalize();
            }
            
            Color coneColor = new Color(1, 1, 0, 0.2f); // Transparent yellow
            
            // Draw the cone
            DrawWireCone(transform.position, inputDirection, angle, detectionRange, coneColor);
        }

        private void DrawWireCone(Vector3 position, Vector3 direction, float angle, float distance, Color color)
        {
            Gizmos.color = color;
            
            // Calculate the number of segments based on the angle
            int segments = Mathf.Max(12, Mathf.FloorToInt(angle / 5f));
            
            // Calculate the radius of the cone at the farthest point
            float radius = distance * Mathf.Sin(angle * Mathf.Deg2Rad);
            
            // Draw the cone outline
            Vector3 forwardOffset = direction * distance;
            Vector3 prevPoint = Vector3.zero;
            
            // Draw edges of the cone
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float currentAngle = t * angle * 2f * Mathf.PI / 360f;
                
                // Create rotation for the current segment
                Quaternion rotation = Quaternion.AngleAxis(currentAngle * Mathf.Rad2Deg, Vector3.up);
                Vector3 rotatedDir = rotation * direction;
                
                // Calculate perpendicular direction to create the circle
                Vector3 perpendicularDir = Vector3.Cross(rotatedDir, Vector3.up).normalized;
                if (perpendicularDir.magnitude < 0.001f)
                {
                    perpendicularDir = Vector3.Cross(rotatedDir, Vector3.right).normalized;
                }
                
                // Rotate the perpendicular direction to create the cone circle
                Vector3 circleDir = Quaternion.AngleAxis(90, rotatedDir) * perpendicularDir;
                Vector3 pointOnCone = position + forwardOffset + circleDir * radius;
                
                // Draw the cone edge
                Gizmos.DrawLine(position, pointOnCone);
                
                // Connect the points to form the cone's outer circumference
                if (i > 0)
                {
                    Gizmos.DrawLine(prevPoint, pointOnCone);
                }
                
                prevPoint = pointOnCone;
            }
        }

        #endregion
    }

}