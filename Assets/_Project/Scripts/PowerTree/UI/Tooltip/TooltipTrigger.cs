using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
        private static Canvas tooltipCanvas;

        // Initialize the tooltip singleton
        private void Awake()
        {
            if (tooltipInstance == null)
            {
                // Create tooltip canvas if it doesn't exist
                if (tooltipCanvas == null)
                {
                    CreateTooltipCanvas();
                }
                
                // Create tooltip instance when first used
                GameObject tooltipPrefab = Resources.Load<GameObject>("UI/PowerTreeTooltip");
                if (tooltipPrefab != null)
                {
                    tooltipInstance = Instantiate(tooltipPrefab, tooltipCanvas.transform);
                    tooltipDisplay = tooltipInstance.GetComponent<TooltipDisplay>();
                    tooltipInstance.SetActive(false);
                }
                else
                {
                    Debug.LogWarning("Tooltip prefab not found in Resources/UI/PowerTreeTooltip. Creating a basic tooltip.");
                    CreateBasicTooltip();
                }
            }
        }

        private static void CreateTooltipCanvas()
        {
            GameObject canvasObj = new GameObject("TooltipCanvas");
            tooltipCanvas = canvasObj.AddComponent<Canvas>();
            tooltipCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            tooltipCanvas.sortingOrder = 100;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0; // 0 = match width, 1 = match height
            
            canvasObj.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(canvasObj);
        }

        private static void CreateBasicTooltip()
        {
            // Make sure we have a canvas
            if (tooltipCanvas == null)
            {
                CreateTooltipCanvas();
            }
    
            tooltipInstance = new GameObject("PowerTreeTooltip");
            tooltipInstance.transform.SetParent(tooltipCanvas.transform, false);
            tooltipDisplay = tooltipInstance.AddComponent<TooltipDisplay>();
    
            // Set fixed width and height
            RectTransform rectTransform = tooltipInstance.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(400, 200);
    
            tooltipInstance.SetActive(false);
        }

        public void SetContent(string tooltipTitle, string tooltipContent)
        {
            title = tooltipTitle;
            content = tooltipContent;
        }
        
        public void UpdateContent()
        {
            if (tooltipDisplay != null)
            {
                tooltipDisplay.ShowTooltip(title, content);
            }
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