using UnityEngine;
using UnityEngine.EventSystems;

namespace PowerTree
{
/// <summary>
    /// Component that triggers a tooltip to appear when hovering over a UI element
    /// </summary>
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private string title;
        [SerializeField] private string content;
        
        private static GameObject tooltipInstance;
        private static TooltipDisplay tooltipDisplay;

        // Initialize the tooltip singleton
        private void Awake()
        {
            if (tooltipInstance == null)
            {
                // Create tooltip instance when first used
                GameObject tooltipPrefab = Resources.Load<GameObject>("UI/PowerTreeTooltip");
                if (tooltipPrefab != null)
                {
                    tooltipInstance = Instantiate(tooltipPrefab);
                    tooltipDisplay = tooltipInstance.GetComponent<TooltipDisplay>();
                    tooltipInstance.SetActive(false);
                    DontDestroyOnLoad(tooltipInstance);
                }
                else
                {
                    Debug.LogWarning("Tooltip prefab not found in Resources/UI/PowerTreeTooltip. Creating a basic tooltip.");
                    CreateBasicTooltip();
                }
            }
        }

        private static void CreateBasicTooltip()
        {
            tooltipInstance = new GameObject("PowerTreeTooltip");
            tooltipDisplay = tooltipInstance.AddComponent<TooltipDisplay>();
            tooltipInstance.SetActive(false);
            DontDestroyOnLoad(tooltipInstance);
        }

        public void SetContent(string tooltipTitle, string tooltipContent)
        {
            title = tooltipTitle;
            content = tooltipContent;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (tooltipDisplay != null)
            {
                tooltipDisplay.ShowTooltip(title, content, Input.mousePosition);
                tooltipInstance.SetActive(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (tooltipDisplay != null)
            {
                tooltipInstance.SetActive(false);
            }
        }
    }
}