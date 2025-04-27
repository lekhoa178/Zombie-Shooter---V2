using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

namespace DungeonGeneration
{
    /// <summary>
    /// Component to handle dragging of a map container within a clip area
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class MapDragController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        #region Inspector Fields
        [Header("Drag Settings")]
        [SerializeField, Range(0.1f, 3f)] private float m_DragSpeed = 1f;
        
        [Header("Padding")]
        [SerializeField] private Vector4 m_Padding = new Vector4(50f, 50f, 50f, 50f); // Left, Right, Top, Bottom
        
        [Header("Elasticity")]
        [SerializeField, Range(0.1f, 1f)] private float m_ElasticityFactor = 0.35f; // How much overdragging is allowed (0-1)
        [SerializeField, Range(0.1f, 1f)] private float m_SpringBackDuration = 0.3f; // How long the spring back animation takes
        
        [Header("Inertia Settings")]
        [SerializeField, Range(5f, 20f)] private float m_InertiaDecelerationRate = 10f; // How quickly movement slows down after release
        [SerializeField, Range(10f, 200f)] private float m_MinVelocityThreshold = 50f; // Minimum velocity required to trigger inertia
        [SerializeField, Range(1000f, 5000f)] private float m_MaxVelocity = 3000f; // Cap on maximum velocity
        [SerializeField, Range(0.1f, 1f)] private float m_BoundaryDamping = 0.3f; // Velocity damping when hitting boundaries
        #endregion

        #region Private Fields
        private RectTransform m_Container;
        private RectTransform m_ClipRect;
        private Vector2 m_BoundsMin;
        private Vector2 m_BoundsMax;
        private bool m_IsDragging;
        private Vector2 m_LastPointerPosition;
        private Vector2 m_Velocity;
        private Coroutine m_DecelerationCoroutine;
        private bool m_IsOverdragged;
        private Canvas m_ParentCanvas;
        private Camera m_UICamera;
        
        // Cached boundary values
        private Vector2 m_MinPosition;
        private Vector2 m_MaxPosition;
        private bool m_BoundariesCalculated = false;
        #endregion
        
        #region Unity Lifecycle
        private void Awake()
        {
            m_ClipRect = GetComponent<RectTransform>();
            
            // Find parent canvas and its render mode
            m_ParentCanvas = GetComponentInParent<Canvas>();
            if (m_ParentCanvas != null && m_ParentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                m_UICamera = m_ParentCanvas.worldCamera;
            }
        }
        
        private void OnDisable()
        {
            // Ensure coroutines are stopped when disabled
            if (m_DecelerationCoroutine != null)
            {
                StopCoroutine(m_DecelerationCoroutine);
                m_DecelerationCoroutine = null;
            }
        }
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Initialize the drag controller with required references
        /// </summary>
        public void Initialize(RectTransform container, float dragSpeed, float edgeBuffer)
        {
            m_Container = container;
            
            // Only override inspector values if they were explicitly provided
            if (dragSpeed > 0)
                m_DragSpeed = dragSpeed;
                
            if (edgeBuffer > 0)
            {
                // Set uniform padding if edge buffer is specified
                m_Padding = new Vector4(edgeBuffer, edgeBuffer, edgeBuffer, edgeBuffer);
            }
                
            // Ensure we have a valid RectTransform
            if (m_ClipRect == null)
                m_ClipRect = GetComponent<RectTransform>();
                
            m_BoundariesCalculated = false;
        }
        
        /// <summary>
        /// Set custom padding values for each edge
        /// </summary>
        /// <param name="left">Left padding</param>
        /// <param name="right">Right padding</param>
        /// <param name="top">Top padding</param>
        /// <param name="bottom">Bottom padding</param>
        public void SetPadding(float left, float right, float top, float bottom)
        {
            m_Padding = new Vector4(left, right, top, bottom);
            m_BoundariesCalculated = false;
            
            // Recalculate boundaries if we already have bounds
            if (m_Container != null)
            {
                CalculateBoundaries();
                ConstrainContainerPosition(false);
            }
        }
        
        /// <summary>
        /// Set the bounds for constraining the container
        /// </summary>
        public void SetBounds(Vector2 min, Vector2 max)
        {
            m_BoundsMin = min;
            m_BoundsMax = max;
            
            // Calculate boundary positions
            CalculateBoundaries();
            
            // Apply constraints immediately
            ConstrainContainerPosition(false);
        }
        #endregion
        
        #region Event Handlers
        /// <summary>
        /// Called when the user begins dragging
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            // Check that this event is coming from a UI source
            if (eventData.pointerPressRaycast.isValid && 
                eventData.pointerPressRaycast.gameObject &&
                eventData.pointerPressRaycast.gameObject.GetComponentInParent<Canvas>() != null &&
                m_Container != null)
            {
                // Stop any existing movement
                if (m_DecelerationCoroutine != null)
                {
                    StopCoroutine(m_DecelerationCoroutine);
                    m_DecelerationCoroutine = null;
                }
                
                m_IsDragging = true;
                m_LastPointerPosition = eventData.position;
                m_Velocity = Vector2.zero;
                
                // Force set the current game object as the drag handler
                eventData.pointerDrag = gameObject;
                eventData.useDragThreshold = false;
            }
        }
        
        /// <summary>
        /// Called when the user is dragging
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            if (!m_IsDragging || m_Container == null)
                return;
            
            // Convert screen position delta to canvas space if needed
            Vector2 currentPointerPosition = eventData.position;
            Vector2 delta = currentPointerPosition - m_LastPointerPosition;
            
            // If using a Camera canvas, adjust delta to account for canvas scaling
            if (m_ParentCanvas != null && m_ParentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                delta *= m_ParentCanvas.scaleFactor;
            }
            
            // Track velocity for inertia, using a reliable time value
            float deltaTime = Time.unscaledDeltaTime > 0 ? Time.unscaledDeltaTime : 0.01f;
            m_Velocity = Vector2.Lerp(m_Velocity, delta / deltaTime, 0.5f); // Smooth velocity to prevent spikes
            
            m_LastPointerPosition = currentPointerPosition;
            
            // Apply drag with speed factor
            m_Container.anchoredPosition += delta * m_DragSpeed;
            
            // Make sure boundaries are calculated
            if (!m_BoundariesCalculated)
            {
                CalculateBoundaries();
            }
            
            // Check if we're currently overdragging, but don't spring back during active drag
            CheckOverdragStatus();
            
            // Only apply elasticity limits during drag, but don't spring back
            ApplyElasticLimits();
            
            // Prevent this event from being handled by other objects
            eventData.Use();
        }
        
        /// <summary>
        /// Called when the user ends dragging
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            if (!m_IsDragging)
                return;
                
            m_IsDragging = false;
            
            // Update overdrag status
            CheckOverdragStatus();
            
            // If we overdragged, spring back now that drag has ended
            if (m_IsOverdragged)
            {
                StartSpringBackAnimation();
            }
            // Otherwise apply inertia for smooth deceleration
            else if (m_Velocity.magnitude > m_MinVelocityThreshold)
            {
                m_DecelerationCoroutine = StartCoroutine(ApplyInertia());
            }
            
            // Prevent this event from being handled by other objects
            eventData.Use();
        }
        #endregion
        
        #region Private Methods
        /// <summary>
        /// Calculate the boundary positions based on content and clip area
        /// </summary>
        private void CalculateBoundaries()
        {
            if (m_Container == null || m_ClipRect == null)
                return;
                
            // Calculate the total bounds size
            Vector2 boundsSize = m_BoundsMax - m_BoundsMin;
            
            // Get clip area size
            Vector2 clipSize = m_ClipRect.rect.size;
            
            // Calculate padded clip area size (accounting for padding on both sides)
            Vector2 paddedClipSize = new Vector2(
                clipSize.x - (m_Padding.x + m_Padding.y), // Width - (left + right)
                clipSize.y - (m_Padding.z + m_Padding.w)  // Height - (top + bottom)
            );
            
            // Calculate the center point of the clip area in its local space
            Vector2 clipCenter = new Vector2(clipSize.x * 0.5f, clipSize.y * 0.5f);
            
            // Calculate the min/max allowed positions for the container
            // If content is larger than clip area, these positions limit scrolling
            // If content is smaller than clip area, these positions center content
            
            // For content larger than clip area:
            // Min position: clip center + content min boundary + left/bottom padding
            // Max position: clip center + content max boundary - right/top padding - clip size
            
            Vector2 minPos, maxPos;
            
            // X-axis: Check if content width is less than clip width
            if (boundsSize.x < paddedClipSize.x)
            {
                // Content is smaller than clip area, center horizontally
                float contentCenter = (m_BoundsMin.x + m_BoundsMax.x) * 0.5f;
                minPos.x = maxPos.x = clipCenter.x - contentCenter;
            }
            else
            {
                // Content is larger than clip area, allow scrolling
                minPos.x = clipCenter.x - m_BoundsMax.x + m_Padding.y; // Right bound + right padding
                maxPos.x = clipCenter.x - m_BoundsMin.x - m_Padding.x; // Left bound - left padding
            }
            
            // Y-axis: Check if content height is less than clip height
            if (boundsSize.y < paddedClipSize.y)
            {
                // Content is smaller than clip area, center vertically
                float contentCenter = (m_BoundsMin.y + m_BoundsMax.y) * 0.5f;
                minPos.y = maxPos.y = clipCenter.y - contentCenter;
            }
            else
            {
                // Content is larger than clip area, allow scrolling
                minPos.y = clipCenter.y - m_BoundsMax.y + m_Padding.w; // Top bound + bottom padding
                maxPos.y = clipCenter.y - m_BoundsMin.y - m_Padding.z; // Bottom bound - top padding
            }
            
            // Min position is always the minimum allowed value (lower-left of scroll range)
            // Max position is always the maximum allowed value (upper-right of scroll range)
            m_MinPosition = minPos;
            m_MaxPosition = maxPos;
            
            // Ensure min is actually less than max
            m_MinPosition.x = Mathf.Min(m_MinPosition.x, m_MaxPosition.x);
            m_MinPosition.y = Mathf.Min(m_MinPosition.y, m_MaxPosition.y);
            m_MaxPosition.x = Mathf.Max(m_MinPosition.x, m_MaxPosition.x);
            m_MaxPosition.y = Mathf.Max(m_MinPosition.y, m_MaxPosition.y);
            
            m_BoundariesCalculated = true;
            
            // Debug.Log($"Bounds: {m_BoundsMin} to {m_BoundsMax}, Size: {boundsSize}");
            // Debug.Log($"Clip size: {clipSize}, Padded: {paddedClipSize}");
            // Debug.Log($"Min position: {m_MinPosition}, Max position: {m_MaxPosition}");
        }
        
        /// <summary>
        /// Check if the container is currently outside the allowed boundaries
        /// </summary>
        private void CheckOverdragStatus()
        {
            if (m_Container == null)
                return;
                
            // Make sure we have valid boundaries
            if (!m_BoundariesCalculated)
                CalculateBoundaries();
                
            Vector2 currentPos = m_Container.anchoredPosition;
            
            // Check if we're outside the allowed bounds
            bool outsideBoundsX = currentPos.x < m_MinPosition.x || currentPos.x > m_MaxPosition.x;
            bool outsideBoundsY = currentPos.y < m_MinPosition.y || currentPos.y > m_MaxPosition.y;
            m_IsOverdragged = outsideBoundsX || outsideBoundsY;
        }
        
        /// <summary>
        /// Apply elasticity limits to prevent extreme overdragging during active drag
        /// Does not spring back, just limits how far you can overdrag
        /// </summary>
        private void ApplyElasticLimits()
        {
            if (m_Container == null || !m_IsDragging)
                return;
                
            Vector2 currentPos = m_Container.anchoredPosition;
            Vector2 constrainedPos = currentPos;
            
            // Calculate elasticity factors - increases resistance the further you overdrag
            float maxElasticDistance = 200f; // Maximum distance user can overdrag
            
            // Apply elasticity to X axis
            if (currentPos.x < m_MinPosition.x)
            {
                float overDragAmount = m_MinPosition.x - currentPos.x;
                // Increase resistance as we drag further
                float elasticFactor = Mathf.Lerp(m_ElasticityFactor, 0.01f, 
                    Mathf.Clamp01(overDragAmount / maxElasticDistance));
                constrainedPos.x = m_MinPosition.x - (overDragAmount * elasticFactor);
            }
            else if (currentPos.x > m_MaxPosition.x)
            {
                float overDragAmount = currentPos.x - m_MaxPosition.x;
                float elasticFactor = Mathf.Lerp(m_ElasticityFactor, 0.01f, 
                    Mathf.Clamp01(overDragAmount / maxElasticDistance));
                constrainedPos.x = m_MaxPosition.x + (overDragAmount * elasticFactor);
            }
            
            // Apply elasticity to Y axis
            if (currentPos.y < m_MinPosition.y)
            {
                float overDragAmount = m_MinPosition.y - currentPos.y;
                float elasticFactor = Mathf.Lerp(m_ElasticityFactor, 0.01f, 
                    Mathf.Clamp01(overDragAmount / maxElasticDistance));
                constrainedPos.y = m_MinPosition.y - (overDragAmount * elasticFactor);
            }
            else if (currentPos.y > m_MaxPosition.y)
            {
                float overDragAmount = currentPos.y - m_MaxPosition.y;
                float elasticFactor = Mathf.Lerp(m_ElasticityFactor, 0.01f, 
                    Mathf.Clamp01(overDragAmount / maxElasticDistance));
                constrainedPos.y = m_MaxPosition.y + (overDragAmount * elasticFactor);
            }
            
            // Apply constraints with increasing resistance
            m_Container.anchoredPosition = constrainedPos;
        }
        
        /// <summary>
        /// Constrains the container position to ensure content is visible
        /// </summary>
        private void ConstrainContainerPosition(bool allowOverdrag)
        {
            if (m_Container == null || m_ClipRect == null)
                return;
                
            // Make sure boundaries are calculated
            if (!m_BoundariesCalculated)
            {
                CalculateBoundaries();
            }
            
            // Get current position
            Vector2 currentPos = m_Container.anchoredPosition;
            
            // Check if we're outside the allowed bounds
            bool outsideBoundsX = currentPos.x < m_MinPosition.x || currentPos.x > m_MaxPosition.x;
            bool outsideBoundsY = currentPos.y < m_MinPosition.y || currentPos.y > m_MaxPosition.y;
            m_IsOverdragged = outsideBoundsX || outsideBoundsY;
            
            // Apply constraints
            Vector2 constrainedPos = currentPos;
            
            if (allowOverdrag)
            {
                // Allow overdragging with elasticity (reducing movement as we go beyond bounds)
                if (currentPos.x < m_MinPosition.x)
                {
                    float overDragAmount = m_MinPosition.x - currentPos.x;
                    constrainedPos.x = m_MinPosition.x - (overDragAmount * m_ElasticityFactor);
                }
                else if (currentPos.x > m_MaxPosition.x)
                {
                    float overDragAmount = currentPos.x - m_MaxPosition.x;
                    constrainedPos.x = m_MaxPosition.x + (overDragAmount * m_ElasticityFactor);
                }
                
                if (currentPos.y < m_MinPosition.y)
                {
                    float overDragAmount = m_MinPosition.y - currentPos.y;
                    constrainedPos.y = m_MinPosition.y - (overDragAmount * m_ElasticityFactor);
                }
                else if (currentPos.y > m_MaxPosition.y)
                {
                    float overDragAmount = currentPos.y - m_MaxPosition.y;
                    constrainedPos.y = m_MaxPosition.y + (overDragAmount * m_ElasticityFactor);
                }
            }
            else
            {
                // Hard constrain without overdrag
                constrainedPos.x = Mathf.Clamp(constrainedPos.x, m_MinPosition.x, m_MaxPosition.x);
                constrainedPos.y = Mathf.Clamp(constrainedPos.y, m_MinPosition.y, m_MaxPosition.y);
            }
            
            m_Container.anchoredPosition = constrainedPos;
        }
        
        /// <summary>
        /// Applies inertia effect after releasing drag
        /// </summary>
        private IEnumerator ApplyInertia()
        {
            // Cap maximum velocity
            if (m_Velocity.magnitude > m_MaxVelocity)
            {
                m_Velocity = m_Velocity.normalized * m_MaxVelocity;
            }
            
            float velocityThreshold = 10f;
            while (m_Velocity.magnitude > velocityThreshold)
            {
                if (m_Container == null)
                    yield break;
                
                // Apply movement using unscaled time for consistent behavior
                float deltaTime = Time.unscaledDeltaTime;
                m_Container.anchoredPosition += m_Velocity * deltaTime;
                
                // Apply deceleration using exponential decay for smoother feel
                m_Velocity = Vector2.Lerp(m_Velocity, Vector2.zero, m_InertiaDecelerationRate * deltaTime);
                
                // Check if we've hit the boundaries
                CheckOverdragStatus();
                bool wasOverdragged = m_IsOverdragged;
                ApplyElasticLimits(); // Apply elastic limits but don't spring back yet
                
                // If we hit the boundary, reduce velocity more quickly
                if (!wasOverdragged && m_IsOverdragged)
                {
                    m_Velocity *= m_BoundaryDamping;
                    
                    // Reflect velocity if we hit a boundary (bounce effect)
                    Vector2 currentPos = m_Container.anchoredPosition;
                    if (currentPos.x <= m_MinPosition.x || currentPos.x >= m_MaxPosition.x)
                    {
                        m_Velocity.x = -m_Velocity.x * 0.2f; // Bounce with reduced energy
                    }
                    
                    if (currentPos.y <= m_MinPosition.y || currentPos.y >= m_MaxPosition.y)
                    {
                        m_Velocity.y = -m_Velocity.y * 0.2f; // Bounce with reduced energy
                    }
                }
                
                yield return null;
            }
            
            // Spring back if we ended overdragged, now that inertia has finished
            if (m_IsOverdragged)
            {
                StartSpringBackAnimation();
            }
            else
            {
                m_DecelerationCoroutine = null;
            }
        }
        
        /// <summary>
        /// Animate springing back to valid bounds when overdragged
        /// </summary>
        private void StartSpringBackAnimation()
        {
            if (m_DecelerationCoroutine != null)
            {
                StopCoroutine(m_DecelerationCoroutine);
            }
            
            m_DecelerationCoroutine = StartCoroutine(SpringBackCoroutine());
        }
        
        /// <summary>
        /// Coroutine to animate the spring back effect
        /// </summary>
        private IEnumerator SpringBackCoroutine()
        {
            if (m_Container == null)
                yield break;
                
            Vector2 startPosition = m_Container.anchoredPosition;
            
            // Make sure boundaries are calculated
            if (!m_BoundariesCalculated)
            {
                CalculateBoundaries();
            }
            
            // Calculate target position (clamped to valid bounds)
            Vector2 targetPosition = startPosition;
            targetPosition.x = Mathf.Clamp(targetPosition.x, m_MinPosition.x, m_MaxPosition.x);
            targetPosition.y = Mathf.Clamp(targetPosition.y, m_MinPosition.y, m_MaxPosition.y);
            
            float elapsedTime = 0f;
            
            while (elapsedTime < m_SpringBackDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsedTime / m_SpringBackDuration);
                
                m_Container.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
                
                yield return null;
            }
            
            // Ensure we end exactly at the target position
            m_Container.anchoredPosition = targetPosition;
            m_IsOverdragged = false;
            m_DecelerationCoroutine = null;
        }
        #endregion
    }
} 