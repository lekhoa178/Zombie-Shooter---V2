using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PowerTree
{
    /// <summary>
    /// Displays the tooltip content
    /// </summary>
    public class TooltipDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI contentText;
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

        public void ShowTooltip(string title, string content, Vector3 position = default)
        {
            // Set the tooltip content
            titleText.text = title;
            contentText.text = content;

            LayoutRebuilder.ForceRebuildLayoutImmediate(contentText.rectTransform);

            float contentHeight = contentText.preferredHeight;
            float titleHeight = (string.IsNullOrEmpty(title) ? 0 : titleText.preferredHeight);

            float totalPadding = 30; 
            float tooltipHeight = contentHeight + titleHeight + totalPadding;

            rectTransform.sizeDelta = new Vector2(400, Mathf.Max(tooltipHeight - 170, 100));

            if (position != default)
            {
                PositionTooltip(position);
            }
        }

        private void PositionTooltip(Vector3 position)
        {
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            Vector2 tooltipSize = rectTransform.sizeDelta;
    
            rectTransform.pivot = Vector2.zero;
    
            Vector2 targetPosition = position + new Vector3(offset.x, offset.y, 0);
    
            if (targetPosition.x + tooltipSize.x > screenSize.x)
            {
                targetPosition.x = position.x - offset.x;
            }
    
            if (targetPosition.y + tooltipSize.y > screenSize.y)
            {
                targetPosition.y = position.y - offset.y;
            }
    
            rectTransform.position = targetPosition;
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
            
            titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.font = TMP_Settings.defaultFontAsset;
            titleText.fontSize = 16;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = Color.white;
            
            // Create content text
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(container.transform, false);
            
            contentText = contentObj.AddComponent<TextMeshProUGUI>();
            contentText.font = TMP_Settings.defaultFontAsset;
            contentText.fontSize = 14;
            contentText.color = Color.white;
        }
    }
}