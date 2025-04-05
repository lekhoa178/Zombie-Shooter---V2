using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles UI progress bar effects based on progress value
/// </summary>
public class ProgressBarEffect : PredictionZoneEffect
{
    [Header("Progress Bar Settings")]
    [Tooltip("Type of UI element to control")]
    public ProgressBarType barType = ProgressBarType.Image;
    [Tooltip("Target Image component for fill amount")]
    public Image targetImage;
    [Tooltip("Target Slider component for value")]
    public Slider targetSlider;
    [Tooltip("Target Text component for percentage display")]
    public TextMeshProUGUI targetText;
    [Tooltip("Target RawImage component for texture fill")]
    public RawImage targetRawImage;
    [Tooltip("Target TextMeshProUGUI component for percentage display")]
    public TextMeshProUGUI targetPercentageText;
    [Tooltip("Target TextMeshProUGUI component for value display")]
    public TextMeshProUGUI targetValueText;
    [Tooltip("Target RectTransform for size/position animation")]
    public RectTransform targetRectTransform;

    [Header("Validation Settings")]
    [Tooltip("Whether to log validation errors")]
    public bool logValidationErrors = true;
    [Tooltip("Whether to auto-disable invalid components")]
    public bool autoDisableInvalidComponents = true;

    [Header("Visual Settings")]
    [Tooltip("Whether to show percentage text")]
    public bool showPercentage = true;
    [Tooltip("Format string for percentage display (e.g., '{0}%')")]
    public string percentageFormat = "{0}%";
    [Tooltip("Format string for value display (e.g., '{0:F1}')")]
    public string valueFormat = "{0:F1}";
    [Tooltip("Color gradient for the progress bar")]
    public Gradient progressColorGradient;
    [Tooltip("Whether to use color gradient")]
    public bool useColorGradient = true;
    [Tooltip("Whether to use sprite color")]
    public bool useSpriteColor = true;
    [Tooltip("Whether to use text color")]
    public bool useTextColor = true;
    [Tooltip("Whether to use background color")]
    public bool useBackgroundColor = true;
    [Tooltip("Background color gradient")]
    public Gradient backgroundColorGradient;
    [Tooltip("Whether to use background color gradient")]
    public bool useBackgroundColorGradient = false;

    [Header("Animation Settings")]
    [Tooltip("Animation curve for smooth progress updates")]
    public AnimationCurve progressCurve = AnimationCurve.Linear(0, 0, 1, 1);
    [Tooltip("Animation speed for smooth transitions")]
    public float animationSpeed = 5f;
    [Tooltip("Whether to animate size changes")]
    public bool animateSize = false;
    [Tooltip("Animation curve for size changes")]
    public AnimationCurve sizeCurve = AnimationCurve.Linear(0, 0, 1, 1);
    [Tooltip("Animation speed for size changes")]
    public float sizeAnimationSpeed = 5f;
    [Tooltip("Whether to animate position changes")]
    public bool animatePosition = false;
    [Tooltip("Animation curve for position changes")]
    public AnimationCurve positionCurve = AnimationCurve.Linear(0, 0, 1, 1);
    [Tooltip("Animation speed for position changes")]
    public float positionAnimationSpeed = 5f;
    [Tooltip("Whether to use spring animation")]
    public bool useSpringAnimation = false;
    [Tooltip("Spring animation settings")]
    public SpringSettings springSettings = new SpringSettings();

    [Header("Size Animation Settings")]
    [Tooltip("Initial size")]
    public Vector2 initialSize = Vector2.one;
    [Tooltip("Target size")]
    public Vector2 targetSize = Vector2.one;
    [Tooltip("Whether to maintain aspect ratio")]
    public bool maintainAspectRatio = true;

    [Header("Position Animation Settings")]
    [Tooltip("Initial position")]
    public Vector2 initialPosition;
    [Tooltip("Target position")]
    public Vector2 targetPosition;
    [Tooltip("Whether to use local position")]
    public bool useLocalPosition = true;

    private float currentProgress;
    private float targetProgress;
    private Vector2 currentSize;
    private Vector2 targetSizeValue;
    private Vector2 currentPosition;
    private Vector2 targetPositionValue;
    private bool isInitialized;
    private SpringAnimation springAnimation;
    private Component targetComponent;

    public enum ProgressBarType
    {
        Image,
        Slider,
        Text,
        RawImage,
        PercentageText,
        ValueText,
        RectTransform
    }

    [System.Serializable]
    public class SpringSettings
    {
        public float damping = 10f;
        public float stiffness = 100f;
        public float mass = 1f;
        public float initialVelocity = 0f;
    }

    public override bool ValidateEffect()
    {
        try
        {
            // Validate based on bar type
            switch (barType)
            {
                case ProgressBarType.Image:
                    targetComponent = targetImage;
                    if (targetImage == null)
                    {
                        targetImage = GetComponent<Image>();
                        targetComponent = targetImage;
                    }
                    break;

                case ProgressBarType.Slider:
                    targetComponent = targetSlider;
                    if (targetSlider == null)
                    {
                        targetSlider = GetComponent<Slider>();
                        targetComponent = targetSlider;
                    }
                    break;

                case ProgressBarType.Text:
                    targetComponent = targetText;
                    if (targetText == null)
                    {
                        targetText = GetComponent<TextMeshProUGUI>();
                        targetComponent = targetText;
                    }
                    break;

                case ProgressBarType.RawImage:
                    targetComponent = targetRawImage;
                    if (targetRawImage == null)
                    {
                        targetRawImage = GetComponent<RawImage>();
                        targetComponent = targetRawImage;
                    }
                    break;

                case ProgressBarType.PercentageText:
                    targetComponent = targetPercentageText;
                    if (targetPercentageText == null)
                    {
                        targetPercentageText = GetComponent<TextMeshProUGUI>();
                        targetComponent = targetPercentageText;
                    }
                    break;

                case ProgressBarType.ValueText:
                    targetComponent = targetValueText;
                    if (targetValueText == null)
                    {
                        targetValueText = GetComponent<TextMeshProUGUI>();
                        targetComponent = targetValueText;
                    }
                    break;

                case ProgressBarType.RectTransform:
                    targetComponent = targetRectTransform;
                    if (targetRectTransform == null)
                    {
                        targetRectTransform = GetComponent<RectTransform>();
                        targetComponent = targetRectTransform;
                    }
                    break;
            }

            // Validate target component
            if (targetComponent == null)
            {
                if (logValidationErrors)
                {
                    Debug.LogError($"[ProgressBarEffect] Missing required component for type {barType} on {gameObject.name}");
                }
                if (autoDisableInvalidComponents)
                {
                    SetEnabled(false);
                }
                return false;
            }

            // Validate RectTransform if needed
            if (animateSize || animatePosition)
            {
                if (targetRectTransform == null)
                {
                    targetRectTransform = GetComponent<RectTransform>();
                    if (targetRectTransform == null)
                    {
                        if (logValidationErrors)
                        {
                            Debug.LogError($"[ProgressBarEffect] Missing RectTransform for size/position animation on {gameObject.name}");
                        }
                        if (autoDisableInvalidComponents)
                        {
                            SetEnabled(false);
                        }
                        return false;
                    }
                }
            }

            // Initialize size and position
            if (targetRectTransform != null)
            {
                currentSize = targetRectTransform.sizeDelta;
                currentPosition = useLocalPosition ? targetRectTransform.localPosition : targetRectTransform.position;
            }

            isInitialized = true;
            return true;
        }
        catch (Exception e)
        {
            if (logValidationErrors)
            {
                Debug.LogError($"[ProgressBarEffect] Validation error on {gameObject.name}: {e.Message}");
            }
            if (autoDisableInvalidComponents)
            {
                SetEnabled(false);
            }
            return false;
        }
    }

    void Update()
    {
        if (!isEnabled || !isInitialized || targetComponent == null) return;

        try
        {
            float deltaTime = Time.deltaTime;

            // Update progress with spring animation if enabled
            if (useSpringAnimation)
            {
                currentProgress = springAnimation.Update(targetProgress, deltaTime);
            }
            else
            {
                currentProgress = Mathf.MoveTowards(currentProgress, targetProgress, deltaTime * animationSpeed);
            }

            // Update size animation
            if (animateSize && targetRectTransform != null)
            {
                if (useSpringAnimation)
                {
                    currentSize = Vector2.Lerp(currentSize, targetSizeValue, deltaTime * sizeAnimationSpeed);
                }
                else
                {
                    currentSize = Vector2.MoveTowards(currentSize, targetSizeValue, deltaTime * sizeAnimationSpeed);
                }
                UpdateSize(currentSize);
            }

            // Update position animation
            if (animatePosition && targetRectTransform != null)
            {
                if (useSpringAnimation)
                {
                    currentPosition = Vector2.Lerp(currentPosition, targetPositionValue, deltaTime * positionAnimationSpeed);
                }
                else
                {
                    currentPosition = Vector2.MoveTowards(currentPosition, targetPositionValue, deltaTime * positionAnimationSpeed);
                }
                UpdatePosition(currentPosition);
            }

            UpdateUIElements(currentProgress);
        }
        catch (Exception e)
        {
            if (logValidationErrors)
            {
                Debug.LogError($"[ProgressBarEffect] Update error on {gameObject.name}: {e.Message}");
            }
        }
    }

    public override void UpdateEffect(float progress)
    {
        if (!isEnabled || !isInitialized || targetComponent == null) return;

        try
        {
            targetProgress = progressCurve.Evaluate(progress);
            targetSizeValue = Vector2.Lerp(initialSize, targetSize, progress);
            targetPositionValue = Vector2.Lerp(initialPosition, targetPosition, progress);

            if (useSpringAnimation)
            {
                springAnimation.SetTarget(targetProgress);
            }
        }
        catch (Exception e)
        {
            if (logValidationErrors)
            {
                Debug.LogError($"[ProgressBarEffect] UpdateEffect error on {gameObject.name}: {e.Message}");
            }
        }
    }

    private void UpdateUIElements(float progress)
    {
        try
        {
            switch (barType)
            {
                case ProgressBarType.Image:
                    if (targetImage != null)
                    {
                        targetImage.fillAmount = progress;
                        if (useColorGradient)
                            targetImage.color = progressColorGradient.Evaluate(progress);
                        if (useBackgroundColorGradient)
                            targetImage.color = backgroundColorGradient.Evaluate(progress);
                    }
                    break;

                case ProgressBarType.Slider:
                    if (targetSlider != null)
                    {
                        targetSlider.value = progress;
                        if (useColorGradient && targetSlider.fillRect != null)
                        {
                            var image = targetSlider.fillRect.GetComponent<Image>();
                            if (image != null)
                                image.color = progressColorGradient.Evaluate(progress);
                        }
                    }
                    break;

                case ProgressBarType.Text:
                    if (targetText != null)
                    {
                        if (showPercentage)
                            targetText.text = string.Format(percentageFormat, Mathf.Round(progress * 100));
                        if (useTextColor)
                            targetText.color = progressColorGradient.Evaluate(progress);
                    }
                    break;

                case ProgressBarType.RawImage:
                    if (targetRawImage != null)
                    {
                        targetRawImage.uvRect = new Rect(0, 0, progress, 1);
                        if (useColorGradient)
                            targetRawImage.color = progressColorGradient.Evaluate(progress);
                    }
                    break;

                case ProgressBarType.PercentageText:
                    if (targetPercentageText != null)
                    {
                        targetPercentageText.text = string.Format(percentageFormat, Mathf.Round(progress * 100));
                        if (useTextColor)
                            targetPercentageText.color = progressColorGradient.Evaluate(progress);
                    }
                    break;

                case ProgressBarType.ValueText:
                    if (targetValueText != null)
                    {
                        targetValueText.text = string.Format(valueFormat, progress);
                        if (useTextColor)
                            targetValueText.color = progressColorGradient.Evaluate(progress);
                    }
                    break;
            }
        }
        catch (Exception e)
        {
            if (logValidationErrors)
            {
                Debug.LogError($"[ProgressBarEffect] UpdateUIElements error on {gameObject.name}: {e.Message}");
            }
        }
    }

    private void UpdateSize(Vector2 size)
    {
        if (targetRectTransform == null) return;

        try
        {
            if (maintainAspectRatio)
            {
                float aspectRatio = targetRectTransform.sizeDelta.x / targetRectTransform.sizeDelta.y;
                size.y = size.x / aspectRatio;
            }

            targetRectTransform.sizeDelta = size;
        }
        catch (Exception e)
        {
            if (logValidationErrors)
            {
                Debug.LogError($"[ProgressBarEffect] UpdateSize error on {gameObject.name}: {e.Message}");
            }
        }
    }

    private void UpdatePosition(Vector2 position)
    {
        if (targetRectTransform == null) return;

        try
        {
            if (useLocalPosition)
                targetRectTransform.localPosition = position;
            else
                targetRectTransform.position = position;
        }
        catch (Exception e)
        {
            if (logValidationErrors)
            {
                Debug.LogError($"[ProgressBarEffect] UpdatePosition error on {gameObject.name}: {e.Message}");
            }
        }
    }

    public override void ResetEffect()
    {
        if (!isInitialized || targetComponent == null) return;

        try
        {
            targetProgress = 0f;
            currentProgress = 0f;
            targetSizeValue = initialSize;
            targetPositionValue = initialPosition;
            currentSize = initialSize;
            currentPosition = initialPosition;

            if (useSpringAnimation)
            {
                springAnimation.Reset();
            }

            UpdateUIElements(0f);
            UpdateSize(initialSize);
            UpdatePosition(initialPosition);
        }
        catch (Exception e)
        {
            if (logValidationErrors)
            {
                Debug.LogError($"[ProgressBarEffect] ResetEffect error on {gameObject.name}: {e.Message}");
            }
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (springAnimation != null)
        {
            springAnimation = null;
        }
    }
} 