using Flyweight;
using UnityEngine;
using UnityEngine.UI;

namespace Flyweight
{
    [CreateAssetMenu(fileName = "New HealthBar Settings", menuName = "Flyweight/HealthBar Settings")]
    public class HealthBarFlyweightSettings : FlyweightSettings
    {
        [Header("UI Element Names")]
        [Tooltip("Name of the background Image component in the prefab")]
        public string backgroundImageName = "Background";
        [Tooltip("Name of the fill Image component in the prefab")]
        public string fillImageName = "Fill";
        [Tooltip("Name of the secondary fill Image component in the prefab (optional)")]
        public string secondaryFillImageName = "SecondaryFill";
        [Tooltip("Name of the health text component in the prefab (optional)")]
        public string healthTextName = "HealthText";
        
        [Header("Visual Settings")]
        [Tooltip("Color for healthy state (high health)")]
        public Color healthyColor = Color.green;
        [Tooltip("Color for damaged state (medium health)")]
        public Color damagedColor = Color.yellow;
        [Tooltip("Color for critical state (low health)")]
        public Color criticalColor = Color.red;
        [Tooltip("Threshold for healthy state (percent, 0-1)")]
        public float healthyThreshold = 0.7f;
        [Tooltip("Threshold for critical state (percent, 0-1)")]
        public float criticalThreshold = 0.3f;
        [Tooltip("Whether to hide the health bar when health is full")]
        public bool hideAtFullHealth = false;
        
        [Header("Effect Settings")]
        [Tooltip("Enable flash effect when taking damage")]
        public bool enableDamageFlash = true;
        [Tooltip("Duration of damage flash effect")]
        public float damageFlashDuration = 0.5f;
        
        [Header("Dual Fill Settings")]
        [Tooltip("Enable dual fill effect with primary and secondary bars")]
        public bool enableDualFill = true;
        [Tooltip("Color of the secondary fill (for damage/healing indication)")]
        public Color secondaryFillColor = new Color(0.7f, 0.7f, 0.7f, 0.6f);
        [Tooltip("Duration of secondary fill animation")]
        public float secondaryFillDuration = 0.5f;
        [Tooltip("Delay before secondary fill animation begins")]
        public float secondaryFillDelay = 0.2f;
        
        [Tooltip("Enable anticipation fill effect when health increases")]
        public bool enableAnticipateFill = false;
        [Tooltip("Duration of anticipation fill animation")]
        public float anticipateFillDuration = 0.5f;
        [Tooltip("Color to use for the anticipation effect")]
        public Color anticipateColor = new Color(1f, 1f, 0.5f, 1f); // Yellowish
        
        [Header("Visibility Settings")]
        [Tooltip("Maximum time a health bar can remain visible when enemy is offscreen or too far")]
        public float offscreenHideDelay = 1.5f;
        [Tooltip("Distance at which health bars should hide")]
        public float maxVisibleDistance = 30f;
        
        public override Flyweight Create()
        {
            if (!prefab)
            {
                Debug.LogError($"HealthBar prefab is null for {name}");
                return null;
            }
            
            // Instantiate the UI prefab as a child of the shared canvas
            GameObject instance = Instantiate(prefab);
            
            instance.transform.localScale = Vector3.one * 0.02f; // Scale down for world space
            
            var canvas = instance.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10; // Ensure it renders on top
                    
            // Add a canvas scaler for better control
            CanvasScaler scaler = instance.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);  
            
            // Set up the health bar component
            var healthBarFlyweight = instance.AddComponent<HealthBarFlyweight>();
            healthBarFlyweight.settings = this;
            
            // Find UI components by their names
            Image background = null;
            Image fill = null;
            Image secondaryFill = null;
            Text healthText = null;
            CanvasGroup canvasGroup = instance.GetComponent<CanvasGroup>();
            
            // Find background
            Transform backgroundTransform = instance.transform.Find(backgroundImageName);
            if (backgroundTransform)
            {
                background = backgroundTransform.GetComponent<Image>();
            }
            else
            {
                // Try to get it directly from the root
                background = instance.GetComponent<Image>();
            }
            
            // Find primary fill
            if (backgroundTransform)
            {
                Transform fillTransform = backgroundTransform.Find(fillImageName);
                if (fillTransform)
                {
                    fill = fillTransform.GetComponent<Image>();
                }
            }
            else
            {
                // Try to find fill from root
                Transform fillTransform = instance.transform.Find(fillImageName);
                if (fillTransform)
                {
                    fill = fillTransform.GetComponent<Image>();
                }
            }
            
            // Find secondary fill
            if (backgroundTransform)
            {
                Transform secondaryFillTransform = backgroundTransform.Find(secondaryFillImageName);
                if (secondaryFillTransform)
                {
                    secondaryFill = secondaryFillTransform.GetComponent<Image>();
                }
            }
            else
            {
                // Try to find secondary fill from root
                Transform secondaryFillTransform = instance.transform.Find(secondaryFillImageName);
                if (secondaryFillTransform)
                {
                    secondaryFill = secondaryFillTransform.GetComponent<Image>();
                }
            }
            
            // Find health text
            Transform textTransform = instance.transform.Find(healthTextName);
            if (textTransform)
            {
                healthText = textTransform.GetComponent<Text>();
            }
            
            // Add a canvas group if it doesn't exist
            if (!canvasGroup)
            {
                canvasGroup = instance.AddComponent<CanvasGroup>();
            }
            
            // Set up the references in the HealthBarFlyweight
            healthBarFlyweight.InitializeComponents(background, fill, secondaryFill, healthText, canvasGroup);
            
            // Log warnings for missing components
            if (!background)
                Debug.LogWarning($"Background image not found in health bar prefab. Looked for '{backgroundImageName}'");
                
            if (!fill)
                Debug.LogWarning($"Fill image not found in health bar prefab. Looked for '{fillImageName}'");
            
            // Disable by default
            instance.SetActive(false);

            return healthBarFlyweight;
        }

        public override void OnGet(Flyweight flyweight)
        {
            if (flyweight is HealthBarFlyweight healthBarFlyweight)
            {
                healthBarFlyweight.gameObject.SetActive(true);
                
                // Initialize and reset state if necessary
                healthBarFlyweight.ResetState();
            }
        }

        public override void OnRelease(Flyweight flyweight)
        {
            if (flyweight is HealthBarFlyweight healthBarFlyweight)
            {
                healthBarFlyweight.Cleanup();
                healthBarFlyweight.gameObject.SetActive(false);
            }
        }
    }
} 