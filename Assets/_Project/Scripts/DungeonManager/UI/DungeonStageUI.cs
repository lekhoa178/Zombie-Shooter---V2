using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace DungeonGeneration
{
    /// <summary>
    /// UI Component for a single dungeon stage node
    /// </summary>
    public class DungeonStageUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        #region Serialized Fields
        [SerializeField] private Image m_IconImage;
        [SerializeField] private Image m_BackgroundImage;
        [SerializeField] private Image m_OutlineImage;
        [SerializeField] private TextMeshProUGUI m_StageNameText;
        [SerializeField] private GameObject m_CurrentIndicator;
        
        [Header("States")]
        [SerializeField] private Color m_DefaultColor = Color.gray;
        [SerializeField] private Color m_CompletedColor = Color.green;
        [SerializeField] private Color m_CurrentColor = Color.yellow;
        [SerializeField] private Color m_UnvisitedColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Semi-transparent gray
        [SerializeField] private float m_PulseSpeed = 2f;
        [SerializeField] private float m_PulseScale = 1.2f;
        #endregion
        
        #region Private Fields
        private StageNode m_StageNode;
        private bool m_IsCurrent;
        private bool m_IsUnvisited;
        private Coroutine m_PulseCoroutine;
        #endregion
        
        #region Public Properties
        public StageNode StageNode => m_StageNode;
        #endregion
        
        #region Unity Methods
        private void OnEnable()
        {
            // If this is the current node and should be pulsing, restart the pulse animation
            if (m_IsCurrent && m_PulseCoroutine == null)
            {
                m_PulseCoroutine = StartCoroutine(PulseAnimation());
            }
        }
        
        private void OnDisable()
        {
            // Stop any running coroutines when disabled
            StopPulseCoroutine();
        }
        
        private void OnDestroy()
        {
            StopPulseCoroutine();
        }
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Initializes the stage UI with data
        /// </summary>
        public void Initialize(StageNode stageNode, Sprite icon)
        {
            m_StageNode = stageNode;
            
            // Set icon and name
            if (m_IconImage != null) m_IconImage.sprite = icon;
            if (m_StageNameText != null) m_StageNameText.text = stageNode.Name;
            
            // Set initial state
            SetState(false, false);
        }
        
        /// <summary>
        /// Sets the visual state of the stage node
        /// </summary>
        public void SetState(bool isCurrent, bool isVisited)
        {
            m_IsCurrent = isCurrent;
            m_IsUnvisited = !isVisited;
            
            // Update visual state
            if (m_BackgroundImage != null)
            {
                if (isCurrent)
                {
                    m_BackgroundImage.color = m_CurrentColor;
                }
                else if (!isVisited)
                {
                    m_BackgroundImage.color = m_UnvisitedColor;
                }
                else
                {
                    m_BackgroundImage.color = m_DefaultColor;
                }
            }
            
            // Update icon opacity for unvisited nodes
            // if (m_IconImage != null)
            // {
            //     Color iconColor = m_IconImage.color;
            //     iconColor.a = m_IsUnvisited ? 0.5f : 1.0f;
            //     m_IconImage.color = iconColor;
            // }
            
            // Update text opacity for unvisited nodes
            if (m_StageNameText != null)
            {
                Color textColor = m_StageNameText.color;
                textColor.a = m_IsUnvisited ? 0.5f : 1.0f;
                m_StageNameText.color = textColor;
            }
            
            // Update current indicator
            if (m_CurrentIndicator != null)
            {
                m_CurrentIndicator.SetActive(isCurrent);
            }
            
            // Handle pulse animation for current node
            if (isCurrent)
            {
                // Stop existing coroutine if there is one
                if (m_PulseCoroutine != null)
                {
                    StopPulseCoroutine();
                }
                
                // Only start the coroutine if the GameObject is active
                if (gameObject.activeInHierarchy)
                {
                    m_PulseCoroutine = StartCoroutine(PulseAnimation());
                }
                else
                {
                    // For inactive GameObjects, just set the scale to the target value
                    transform.localScale = new Vector3(m_PulseScale, m_PulseScale, 1f);
                }
            }
            else
            {
                if (m_PulseCoroutine != null)
                {
                    StopPulseCoroutine();
                }
                
                // Reset scale
                transform.localScale = Vector3.one;
            }
            
            // Update outline
            // if (m_OutlineImage != null)
            // {
            //     if (isCurrent)
            //     {
            //         m_OutlineImage.enabled = true;
            //         m_OutlineImage.color = m_CurrentColor;
            //     }
            //     else if (isCompleted)
            //     {
            //         m_OutlineImage.enabled = true;
            //         m_OutlineImage.color = m_CompletedColor;
            //     }
            //     else if (m_IsUnvisited)
            //     {
            //         m_OutlineImage.enabled = true;
            //         m_OutlineImage.color = m_UnvisitedColor;
            //     }
            //     else
            //     {
            //         m_OutlineImage.enabled = false;
            //     }
            // }
        }
        #endregion
        
        #region Private Methods
        /// <summary>
        /// Safely stops the pulse coroutine
        /// </summary>
        private void StopPulseCoroutine()
        {
            if (m_PulseCoroutine != null)
            {
                if (gameObject.activeInHierarchy)
                {
                    StopCoroutine(m_PulseCoroutine);
                }
                m_PulseCoroutine = null;
            }
        }
        
        /// <summary>
        /// Coroutine for pulsing animation
        /// </summary>
        private IEnumerator PulseAnimation()
        {
            while (true)
            {
                // Pulse up and down
                float pulseValue = Mathf.Sin(Time.time * m_PulseSpeed) * 0.5f + 0.5f;
                float scale = Mathf.Lerp(1f, m_PulseScale, pulseValue);
                
                transform.localScale = new Vector3(scale, scale, 1f);
                
                yield return null;
            }
        }
        #endregion
        
        #region Event Handlers
        public void OnPointerEnter(PointerEventData eventData)
        {
            // Highlight on hover
            if (m_OutlineImage != null && !m_OutlineImage.enabled)
            {
                m_OutlineImage.enabled = true;
                
                // Use appropriate color based on state
                if (m_IsCurrent)
                    m_OutlineImage.color = m_CurrentColor;
                else if (m_IsUnvisited)
                    m_OutlineImage.color = m_UnvisitedColor;
                else
                    m_OutlineImage.color = m_DefaultColor;
            }
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            // Remove highlight on exit if not current, completed, or unvisited
            if (m_OutlineImage != null && !m_IsCurrent && !m_IsUnvisited)
            {
                m_OutlineImage.enabled = false;
            }
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            // Optional: Allow clicking on nodes for additional functionality
            // For example, to show more details about the stage
        }
        #endregion
    }
} 