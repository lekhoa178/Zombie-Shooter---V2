using UnityEngine;
using UnityEngine.UI;

namespace PowerTree
{
    /// <summary>
    /// Displays the tooltip content
    /// </summary>
    public class TooltipDisplay : MonoBehaviour
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text contentText;
        [SerializeField] private LayoutElement layoutElement;
        [SerializeField] private int characterWrapLimit = 50;
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Vector2 offset = new Vector2(20, 20);
        
        private Canvas tooltipCanvas;
        private CanvasScaler canvasScaler;

        private void Awake()
        {
            // Create the tooltip UI if it doesn't exist
            if (titleText == null || contentText == null)
            {
                SetupTooltipUI();
            }
            
            rectTransform = GetComponent<RectTransform>();
        }

        public void ShowTooltip(string title, string content, Vector3 position)
        {
            // Set the tooltip content
            titleText.text = title;
            contentText.text = content;
            
            // Calculate tooltip size based on content
            int titleLength = title.Length;
            int contentLength = content.Length;
            
            layoutElement.enabled = (titleLength > characterWrapLimit || contentLength > characterWrapLimit);
            
            // Position the tooltip
            PositionTooltip(position);
        }

        private void PositionTooltip(Vector3 position)
        {
            // Make sure the tooltip stays within screen bounds
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            Vector2 pivotPosition = position + new Vector3(offset.x, offset.y, 0);
            
            // Adjust pivot based on screen edge proximity
            rectTransform.pivot = Vector2.zero; // Default pivot at bottom-left
            
            if (pivotPosition.x + rectTransform.rect.width > screenSize.x)
            {
                pivotPosition.x = position.x - offset.x - rectTransform.rect.width;
            }
            
            if (pivotPosition.y + rectTransform.rect.height > screenSize.y)
            {
                pivotPosition.y = position.y - offset.y - rectTransform.rect.height;
            }
            
            rectTransform.position = pivotPosition;
        }

        private void SetupTooltipUI()
        {
            // Create Canvas
            tooltipCanvas = gameObject.AddComponent<Canvas>();
            tooltipCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            tooltipCanvas.sortingOrder = 100;
            
            canvasScaler = gameObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            
            gameObject.AddComponent<GraphicRaycaster>();
            
            // Create background
            GameObject background = new GameObject("Background");
            background.transform.SetParent(transform, false);
            
            Image backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            
            RectTransform bgRect = backgroundImage.rectTransform;
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            
            // Create content container
            GameObject container = new GameObject("Container");
            container.transform.SetParent(transform, false);
            
            VerticalLayoutGroup layout = container.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 5;
            
            RectTransform containerRect = container.GetComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.sizeDelta = Vector2.zero;
            
            // Create title text
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(container.transform, false);
            
            titleText = titleObj.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.fontSize = 16;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = Color.white;
            
            // Create content text
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(container.transform, false);
            
            contentText = contentObj.AddComponent<Text>();
            contentText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            contentText.fontSize = 14;
            contentText.color = Color.white;
            
            // Add layout element to container
            layoutElement = container.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 300;
            layoutElement.flexibleWidth = 0;
        }
    }
}