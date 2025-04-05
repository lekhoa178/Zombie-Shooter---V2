using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;
using Slider = UnityEngine.UI.Slider;

/// <summary>
/// Base class for all visual effects in the prediction zone system.
/// Provides common functionality and interface for visual effects.
/// </summary>
public abstract class PredictionZoneEffect : MonoBehaviour
{
    [Header("Effect Settings")]
    [Tooltip("Whether this effect is currently active")]
    public bool isEnabled = true;

    [Header("Events")]
    [Tooltip("Event triggered when effect state changes")]
    public UnityEvent<PredictionZoneEffect, bool> onStateChanged = new UnityEvent<PredictionZoneEffect, bool>();

    private bool previousEnabledState;

    protected virtual void Awake()
    {
        previousEnabledState = isEnabled;
        ValidateEffect();
    }

    protected virtual void OnValidate()
    {
        // Notify listeners if enabled state changed in inspector
        if (previousEnabledState != isEnabled)
        {
            onStateChanged.Invoke(this, isEnabled);
            previousEnabledState = isEnabled;
        }
    }

    /// <summary>
    /// Validates the effect's required components and settings
    /// </summary>
    public virtual bool ValidateEffect()
    {
        return true;
    }

    /// <summary>
    /// Updates the effect based on the current progress value (0-1)
    /// </summary>
    public abstract void UpdateEffect(float progress);

    /// <summary>
    /// Resets the effect to its initial state
    /// </summary>
    public abstract void ResetEffect();

    /// <summary>
    /// Safely updates the enabled state and notifies listeners
    /// </summary>
    public virtual void SetEnabled(bool enabled)
    {
        if (isEnabled != enabled)
        {
            isEnabled = enabled;
            onStateChanged.Invoke(this, enabled);
        }
    }

    protected virtual void OnDestroy()
    {
        // Clean up any resources
        onStateChanged.RemoveAllListeners();
    }
}

/// <summary>
/// Handles color gradient transitions based on progress
/// </summary>
public class ColorEffect : PredictionZoneEffect
{
    [Header("Color Settings")]
    [Tooltip("Gradient defining how the color changes with progress")]
    public Gradient colorGradient;
    [Tooltip("Renderer component to apply the color to")]
    public Renderer targetRenderer;

    private MaterialPropertyBlock propertyBlock;
    private static readonly int ColorProperty = Shader.PropertyToID("_BaseColor");

    void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
    }

    public override void UpdateEffect(float progress)
    {
        if (!isEnabled || targetRenderer == null) return;

        Color newColor = colorGradient.Evaluate(progress);
        propertyBlock.SetColor(ColorProperty, newColor);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }

    public override void ResetEffect()
    {
        // Color effect doesn't need reset as it's always based on current progress
    }
}

/// <summary>
/// Handles scale animations based on progress
/// </summary>
public class ScaleEffect : PredictionZoneEffect
{
    [Header("Scale Settings")]
    [Tooltip("Controls how the scale changes with progress (0-1). The curve's x-axis represents progress, y-axis represents scale multiplier.")]
    public AnimationCurve scaleCurve = AnimationCurve.Linear(0, 0, 1, 1);
    [Tooltip("Which axes to apply the scale effect to")]
    public Vector3 scaleAxes = Vector3.up;
    [Tooltip("Initial scale of the object")]
    public Vector3 initialScale = Vector3.one;

    void Awake()
    {
        if (initialScale == Vector3.zero)
            initialScale = transform.localScale;
    }

    public override void UpdateEffect(float progress)
    {
        if (!isEnabled) return;

        float scaleMultiplier = scaleCurve.Evaluate(progress);
        Vector3 newScale = initialScale;
        newScale.Scale(Vector3.one + (scaleAxes * (scaleMultiplier - 1)));
        transform.localScale = newScale;
    }

    public override void ResetEffect()
    {
        transform.localScale = initialScale;
    }
}

/// <summary>
/// Handles rotation animations
/// </summary>
public class RotationEffect : PredictionZoneEffect
{
    [Header("Rotation Settings")]
    [Tooltip("Speed of rotation in degrees per second")]
    public float rotationSpeed = 30f;
    [Tooltip("Axis to rotate around")]
    public Vector3 rotationAxis = Vector3.up;

    private float currentRotation;

    public override void UpdateEffect(float progress)
    {
        if (!isEnabled) return;

        currentRotation += rotationSpeed * Time.deltaTime;
        transform.rotation = Quaternion.Euler(rotationAxis * currentRotation);
    }

    public override void ResetEffect()
    {
        currentRotation = 0f;
        transform.rotation = Quaternion.identity;
    }
}

/// <summary>
/// Handles pulse animations
/// </summary>
public class PulseEffect : PredictionZoneEffect
{
    [Header("Pulse Settings")]
    [Tooltip("Speed of the pulse animation")]
    public float pulseSpeed = 2f;
    [Tooltip("Maximum scale multiplier for the pulse effect")]
    public float pulseScale = 0.1f;
    [Tooltip("Initial scale of the object")]
    public Vector3 initialScale = Vector3.one;

    private float currentPulseTime;

    void Awake()
    {
        if (initialScale == Vector3.zero)
            initialScale = transform.localScale;
    }

    public override void UpdateEffect(float progress)
    {
        if (!isEnabled) return;

        currentPulseTime += Time.deltaTime * pulseSpeed;
        float pulseValue = Mathf.Sin(currentPulseTime) * pulseScale;
        transform.localScale = initialScale * (1f + pulseValue);
    }

    public override void ResetEffect()
    {
        currentPulseTime = 0f;
        transform.localScale = initialScale;
    }
}

/// <summary>
/// Handles particle system effects based on progress
/// </summary>
public class ParticleEffect : PredictionZoneEffect
{
    [Header("Particle Settings")]
    [Tooltip("Particle system to control")]
    public ParticleSystem particleSystem;
    [Tooltip("Whether to control emission rate")]
    public bool controlEmissionRate = true;
    [Tooltip("Whether to control particle size")]
    public bool controlParticleSize = true;
    [Tooltip("Whether to control particle color")]
    public bool controlParticleColor = true;
    [Tooltip("Maximum emission rate")]
    public float maxEmissionRate = 10f;
    [Tooltip("Maximum particle size")]
    public float maxParticleSize = 1f;
    [Tooltip("Color gradient for particles")]
    public Gradient particleColorGradient;

    private ParticleSystem.EmissionModule emissionModule;
    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.ColorOverLifetimeModule colorModule;

    void Awake()
    {
        if (particleSystem == null)
            particleSystem = GetComponent<ParticleSystem>();

        if (particleSystem != null)
        {
            emissionModule = particleSystem.emission;
            mainModule = particleSystem.main;
            colorModule = particleSystem.colorOverLifetime;
        }
    }

    public override void UpdateEffect(float progress)
    {
        if (!isEnabled || particleSystem == null) return;

        if (controlEmissionRate)
        {
            emissionModule.rateOverTime = maxEmissionRate * progress;
        }

        if (controlParticleSize)
        {
            mainModule.startSize = maxParticleSize * progress;
        }

        if (controlParticleColor)
        {
            var color = particleColorGradient.Evaluate(progress);
            mainModule.startColor = color;
        }
    }

    public override void ResetEffect()
    {
        if (particleSystem == null) return;

        if (controlEmissionRate)
        {
            emissionModule.rateOverTime = 0;
        }

        if (controlParticleSize)
        {
            mainModule.startSize = 0;
        }

        if (controlParticleColor)
        {
            mainModule.startColor = particleColorGradient.Evaluate(0);
        }
    }
}

/// <summary>
/// Handles trail renderer effects based on progress
/// </summary>
public class TrailEffect : PredictionZoneEffect
{
    [Header("Trail Settings")]
    [Tooltip("Trail renderer to control")]
    public TrailRenderer trailRenderer;
    [Tooltip("Whether to control trail width")]
    public bool controlWidth = true;
    [Tooltip("Whether to control trail color")]
    public bool controlColor = true;
    [Tooltip("Maximum trail width")]
    public float maxWidth = 1f;
    [Tooltip("Color gradient for trail")]
    public Gradient trailColorGradient;
    [Tooltip("Time to fade out the trail")]
    public float fadeTime = 1f;
    [Tooltip("Minimum trail width")]
    public float minWidth = 0.1f;

    void Awake()
    {
        if (trailRenderer == null)
            trailRenderer = GetComponent<TrailRenderer>();
    }

    public override void UpdateEffect(float progress)
    {
        if (!isEnabled || trailRenderer == null) return;

        if (controlWidth)
        {
            float width = Mathf.Lerp(minWidth, maxWidth, progress);
            trailRenderer.startWidth = width;
            trailRenderer.endWidth = width * 0.5f;
        }

        if (controlColor)
        {
            trailRenderer.startColor = trailColorGradient.Evaluate(progress);
            trailRenderer.endColor = trailColorGradient.Evaluate(progress);
        }
    }

    public override void ResetEffect()
    {
        if (trailRenderer == null) return;

        if (controlWidth)
        {
            trailRenderer.startWidth = minWidth;
            trailRenderer.endWidth = minWidth * 0.5f;
        }

        if (controlColor)
        {
            trailRenderer.startColor = trailColorGradient.Evaluate(0);
            trailRenderer.endColor = trailColorGradient.Evaluate(0);
        }
    }
}

/// <summary>
/// Handles line renderer effects based on progress
/// </summary>
public class LineEffect : PredictionZoneEffect
{
    [Header("Line Settings")]
    [Tooltip("Line renderer to control")]
    public LineRenderer lineRenderer;
    [Tooltip("Whether to control line width")]
    public bool controlWidth = true;
    [Tooltip("Whether to control line color")]
    public bool controlColor = true;
    [Tooltip("Whether to control line positions")]
    public bool controlPositions = true;
    [Tooltip("Maximum line width")]
    public float maxWidth = 1f;
    [Tooltip("Color gradient for line")]
    public Gradient lineColorGradient;
    [Tooltip("Minimum line width")]
    public float minWidth = 0.1f;
    [Tooltip("Start position of the line")]
    public Vector3 startPosition;
    [Tooltip("End position of the line")]
    public Vector3 endPosition;
    [Tooltip("Number of positions in the line")]
    public int positionCount = 2;

    void Awake()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
    }

    public override void UpdateEffect(float progress)
    {
        if (!isEnabled || lineRenderer == null) return;

        if (controlWidth)
        {
            float width = Mathf.Lerp(minWidth, maxWidth, progress);
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width * 0.5f;
        }

        if (controlColor)
        {
            lineRenderer.startColor = lineColorGradient.Evaluate(progress);
            lineRenderer.endColor = lineColorGradient.Evaluate(progress);
        }

        if (controlPositions)
        {
            lineRenderer.positionCount = positionCount;
            lineRenderer.SetPosition(0, startPosition);
            lineRenderer.SetPosition(1, Vector3.Lerp(startPosition, endPosition, progress));
        }
    }

    public override void ResetEffect()
    {
        if (lineRenderer == null) return;

        if (controlWidth)
        {
            lineRenderer.startWidth = minWidth;
            lineRenderer.endWidth = minWidth * 0.5f;
        }

        if (controlColor)
        {
            lineRenderer.startColor = lineColorGradient.Evaluate(0);
            lineRenderer.endColor = lineColorGradient.Evaluate(0);
        }

        if (controlPositions)
        {
            lineRenderer.positionCount = positionCount;
            lineRenderer.SetPosition(0, startPosition);
            lineRenderer.SetPosition(1, startPosition);
        }
    }
}

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

/// <summary>
/// Helper class for spring animation calculations
/// </summary>
public class SpringAnimation
{
    private float damping;
    private float stiffness;
    private float mass;
    private float velocity;
    private float currentValue;
    private float targetValue;

    public SpringAnimation(float damping, float stiffness, float mass, float initialVelocity)
    {
        this.damping = damping;
        this.stiffness = stiffness;
        this.mass = mass;
        this.velocity = initialVelocity;
        this.currentValue = 0f;
        this.targetValue = 0f;
    }

    public float Update(float target, float deltaTime)
    {
        targetValue = target;
        float force = (targetValue - currentValue) * stiffness;
        force -= velocity * damping;
        float acceleration = force / mass;
        velocity += acceleration * deltaTime;
        currentValue += velocity * deltaTime;
        return currentValue;
    }

    public void SetTarget(float target)
    {
        targetValue = target;
    }

    public void Reset()
    {
        velocity = 0f;
        currentValue = 0f;
        targetValue = 0f;
    }
}

/// <summary>
/// Main component that manages all visual effects for a prediction zone.
/// Implements IProgressionHandler to work with the prediction zone system.
/// </summary>
public class ProgressionVisual : MonoBehaviour, IProgressionHandler
{
    [Header("Effect Components")]
    [Tooltip("Color effect component")]
    public ColorEffect colorEffect;
    [Tooltip("Scale effect component")]
    public ScaleEffect scaleEffect;
    [Tooltip("Rotation effect component")]
    public RotationEffect rotationEffect;
    [Tooltip("Pulse effect component")]
    public PulseEffect pulseEffect;
    [Tooltip("Particle effect component")]
    public ParticleEffect particleEffect;
    [Tooltip("Trail effect component")]
    public TrailEffect trailEffect;
    [Tooltip("Line effect component")]
    public LineEffect lineEffect;
    [Tooltip("Progress bar effect component")]
    public ProgressBarEffect progressBarEffect;

    [Header("Events")]
    [Tooltip("Event triggered when progress value changes")]
    public UnityEvent<float> onProgressChanged = new UnityEvent<float>();
    [Tooltip("Event triggered when any effect is enabled")]
    public UnityEvent<PredictionZoneEffect> onEffectEnabled = new UnityEvent<PredictionZoneEffect>();
    [Tooltip("Event triggered when any effect is disabled")]
    public UnityEvent<PredictionZoneEffect> onEffectDisabled = new UnityEvent<PredictionZoneEffect>();
    [Tooltip("Event triggered when visual is reset")]
    public UnityEvent onVisualReset = new UnityEvent();
    [Tooltip("Event triggered when all effects are initialized")]
    public UnityEvent onEffectsInitialized = new UnityEvent();
    [Tooltip("Event triggered when an error occurs")]
    public UnityEvent<string> onError = new UnityEvent<string>();

    [Header("Validation Settings")]
    [Tooltip("Whether to log validation errors")]
    public bool logValidationErrors = true;
    [Tooltip("Whether to auto-disable invalid effects")]
    public bool autoDisableInvalidEffects = true;
    [Tooltip("Whether to require at least one effect")]
    public bool requireAtLeastOneEffect = true;

    private float currentProgress;
    private bool isInitialized;
    private List<PredictionZoneEffect> activeEffects = new List<PredictionZoneEffect>();

    void Awake()
    {
        InitializeEffects();
    }

    private void InitializeEffects()
    {
        try
        {
            // Auto-assign components if not set
            if (colorEffect == null) colorEffect = GetComponent<ColorEffect>();
            if (scaleEffect == null) scaleEffect = GetComponent<ScaleEffect>();
            if (rotationEffect == null) rotationEffect = GetComponent<RotationEffect>();
            if (pulseEffect == null) pulseEffect = GetComponent<PulseEffect>();
            if (particleEffect == null) particleEffect = GetComponent<ParticleEffect>();
            if (trailEffect == null) trailEffect = GetComponent<TrailEffect>();
            if (lineEffect == null) lineEffect = GetComponent<LineEffect>();
            if (progressBarEffect == null) progressBarEffect = GetComponent<ProgressBarEffect>();

            // Validate all effects
            ValidateEffects();

            // Subscribe to effect state changes
            SubscribeToEffectStateChanges();

            isInitialized = true;
            onEffectsInitialized.Invoke();
        }
        catch (Exception e)
        {
            HandleError($"Failed to initialize effects: {e.Message}");
        }
    }

    private void ValidateEffects()
    {
        activeEffects.Clear();
        bool hasValidEffect = false;

        // Validate each effect
        ValidateEffect(colorEffect, ref hasValidEffect);
        ValidateEffect(scaleEffect, ref hasValidEffect);
        ValidateEffect(rotationEffect, ref hasValidEffect);
        ValidateEffect(pulseEffect, ref hasValidEffect);
        ValidateEffect(particleEffect, ref hasValidEffect);
        ValidateEffect(trailEffect, ref hasValidEffect);
        ValidateEffect(lineEffect, ref hasValidEffect);
        ValidateEffect(progressBarEffect, ref hasValidEffect);

        // Check if we have at least one valid effect
        if (requireAtLeastOneEffect && !hasValidEffect)
        {
            HandleError("No valid effects found. At least one effect is required.");
        }
    }

    private void ValidateEffect(PredictionZoneEffect effect, ref bool hasValidEffect)
    {
        if (effect == null) return;

        try
        {
            if (effect.ValidateEffect())
            {
                activeEffects.Add(effect);
                hasValidEffect = true;
            }
            else
            {
                if (autoDisableInvalidEffects)
                {
                    effect.SetEnabled(false);
                }
                if (logValidationErrors)
                {
                    Debug.LogWarning($"Effect validation failed for {effect.GetType().Name} on {gameObject.name}");
                }
            }
        }
        catch (Exception e)
        {
            HandleError($"Error validating effect {effect.GetType().Name}: {e.Message}");
        }
    }

    private void SubscribeToEffectStateChanges()
    {
        foreach (var effect in activeEffects)
        {
            if (effect != null)
            {
                effect.onStateChanged.AddListener(HandleEffectStateChanged);
            }
        }
    }

    private void HandleEffectStateChanged(PredictionZoneEffect effect, bool enabled)
    {
        try
        {
            if (enabled)
            {
                if (!activeEffects.Contains(effect))
                {
                    activeEffects.Add(effect);
                }
                onEffectEnabled.Invoke(effect);
            }
            else
            {
                activeEffects.Remove(effect);
                onEffectDisabled.Invoke(effect);
            }
        }
        catch (Exception e)
        {
            HandleError($"Error handling effect state change: {e.Message}");
        }
    }

    private void HandleError(string message)
    {
        if (logValidationErrors)
        {
            Debug.LogError($"[ProgressionVisual] {message}");
        }
        onError.Invoke(message);
    }

    void Update()
    {
        if (!isInitialized) return;

        try
        {
            // Update continuous effects
            foreach (var effect in activeEffects)
            {
                if (effect != null && effect.isEnabled)
                {
                    if (effect is RotationEffect || effect is PulseEffect)
                    {
                        effect.UpdateEffect(0);
                    }
                }
            }
        }
        catch (Exception e)
        {
            HandleError($"Error updating effects: {e.Message}");
        }
    }

    public void UpdateProgression(float progress)
    {
        if (!isInitialized) return;

        try
        {
            // Clamp progress between 0 and 1
            progress = Mathf.Clamp01(progress);

            // Only trigger event if progress has changed
            if (currentProgress != progress)
            {
                currentProgress = progress;
                onProgressChanged.Invoke(progress);
            }

            // Update progress-based effects
            foreach (var effect in activeEffects)
            {
                if (effect != null && effect.isEnabled)
                {
                    if (!(effect is RotationEffect || effect is PulseEffect))
                    {
                        effect.UpdateEffect(progress);
                    }
                }
            }
        }
        catch (Exception e)
        {
            HandleError($"Error updating progression: {e.Message}");
        }
    }

    public void ResetVisual()
    {
        if (!isInitialized) return;

        try
        {
            // Reset all effects
            foreach (var effect in activeEffects)
            {
                if (effect != null)
                {
                    effect.ResetEffect();
                }
            }

            currentProgress = 0f;
            onVisualReset.Invoke();
        }
        catch (Exception e)
        {
            HandleError($"Error resetting visual: {e.Message}");
        }
    }

    private void OnDestroy()
    {
        try
        {
            // Unsubscribe from events
            foreach (var effect in activeEffects)
            {
                if (effect != null)
                {
                    effect.onStateChanged.RemoveListener(HandleEffectStateChanged);
                }
            }
            activeEffects.Clear();

            // Clear all event listeners
            onProgressChanged.RemoveAllListeners();
            onEffectEnabled.RemoveAllListeners();
            onEffectDisabled.RemoveAllListeners();
            onVisualReset.RemoveAllListeners();
            onEffectsInitialized.RemoveAllListeners();
            onError.RemoveAllListeners();
        }
        catch (Exception e)
        {
            Debug.LogError($"[ProgressionVisual] Error during cleanup: {e.Message}");
        }
    }
}
