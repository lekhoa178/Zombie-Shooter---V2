using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace DungeonGeneration
{
    /// <summary>
    /// UI Component for a single legend item in the dungeon map
    /// </summary>
    public class LegendItemUI : MonoBehaviour, IPointerClickHandler
    {
        #region Serialized Fields
        [SerializeField] private Image m_IconImage;
        [SerializeField] private TextMeshProUGUI m_NameText;
        [SerializeField] private Button m_InfoButton;
        #endregion
        
        #region Private Fields
        private string m_Description;
        private DungeonMapUI m_MapUI;
        #endregion
        
        #region Unity Lifecycle
        private void Start()
        {
            if (m_InfoButton != null)
            {
                m_InfoButton.onClick.AddListener(OnInfoButtonClicked);
            }
        }
        
        private void OnDestroy()
        {
            if (m_InfoButton != null)
            {
                m_InfoButton.onClick.RemoveListener(OnInfoButtonClicked);
            }
        }
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Initializes the legend item with data
        /// </summary>
        public void Initialize(string stageName, Sprite icon, string description, DungeonMapUI mapUI)
        {
            if (m_IconImage != null) m_IconImage.sprite = icon;
            if (m_NameText != null) m_NameText.text = stageName;
            
            m_Description = description;
            m_MapUI = mapUI;
        }
        #endregion
        
        #region Event Handlers
        /// <summary>
        /// Called when the info button is clicked
        /// </summary>
        private void OnInfoButtonClicked()
        {
            if (m_MapUI != null)
            {
                m_MapUI.SetLegendDetailsText(m_Description);
                m_MapUI.ShowLegendDetails(true);
            }
        }
        
        /// <summary>
        /// Called when the legend item is clicked
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            // Can be used to highlight all stages of this type in the map
            // For now, just show the details
            OnInfoButtonClicked();
        }
        #endregion
    }
} 