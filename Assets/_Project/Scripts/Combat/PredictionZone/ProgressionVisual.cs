using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages multiple visual effects based on a progression value (0-1)
/// </summary>
public class ProgressionVisual : MonoBehaviour, IProgressionHandler
{
    [Header("Progression Settings")]
    [Tooltip("Current progression value (0-1)")]
    [Range(0f, 1f)]
    public float progression = 0f;
    [Tooltip("Whether to auto-update effects based on progression value")]
    public bool autoUpdate = true;
    [Tooltip("Whether to update in FixedUpdate instead of Update")]
    public bool useFixedUpdate = false;
    [Tooltip("Whether to clamp progression value between 0 and 1")]
    public bool clampProgression = true;
    [Tooltip("Whether to auto-reset effects when progression is 0")]
    public bool autoResetAtZero = true;
    [Tooltip("Whether to auto-validate effects on start")]
    public bool autoValidateOnStart = true;
    [Tooltip("Whether to disable invalid effects")]
    public bool disableInvalidEffects = true;

    [Header("Animation Settings")]
    [Tooltip("Whether to animate progression changes")]
    public bool animateProgression = false;
    [Tooltip("Animation speed for progression changes")]
    public float animationSpeed = 5f;
    [Tooltip("Animation curve for progression changes")]
    public AnimationCurve animationCurve = AnimationCurve.Linear(0, 0, 1, 1);
    [Tooltip("Whether to use spring animation for smoother transitions")]
    public bool useSpringAnimation = false;
    [Tooltip("Spring animation settings")]
    public SpringSettings springSettings = new SpringSettings(10f, 100f, 1f);

    [Header("Events")]
    [Tooltip("Event triggered when progression reaches 1")]
    public UnityEvent onComplete;
    [Tooltip("Event triggered when progression reaches 0")]
    public UnityEvent onReset;
    [Tooltip("Event triggered when progression changes")]
    public UnityEvent<float> onProgressionChanged;

    [Header("Effects")]
    [Tooltip("List of visual effects to apply")]
    public List<PredictionZoneEffect> effects = new List<PredictionZoneEffect>();

    private float currentProgression = 0f;
    private float targetProgression = 0f;
    private bool wasComplete = false;
    private bool wasReset = true;
    private SpringAnimation springAnimator;

    [System.Serializable]
    public class SpringSettings
    {
        public float damping = 10f;
        public float stiffness = 100f;
        public float mass = 1f;

        public SpringSettings(float damping, float stiffness, float mass)
        {
            this.damping = damping;
            this.stiffness = stiffness;
            this.mass = mass;
        }
    }

    private void Awake()
    {
        if (useSpringAnimation)
        {
            springAnimator = new SpringAnimation(
                springSettings.damping,
                springSettings.stiffness,
                springSettings.mass,
                0f
            );
        }

        if (autoValidateOnStart)
        {
            ValidateAllEffects();
        }
    }

    private void Start()
    {
        // Initialize progression
        SetProgression(progression, true);
    }

    private void Update()
    {
        if (!useFixedUpdate)
        {
            UpdateProgression();
        }
    }

    private void FixedUpdate()
    {
        if (useFixedUpdate)
        {
            UpdateProgression();
        }
    }

    private void UpdateProgression()
    {
        if (!autoUpdate) return;

        // Check if we need to animate
        if (animateProgression && Mathf.Abs(currentProgression - targetProgression) > 0.001f)
        {
            float deltaTime = useFixedUpdate ? Time.fixedDeltaTime : Time.deltaTime;

            // Update progression with animation
            if (useSpringAnimation && springAnimator != null)
            {
                currentProgression = springAnimator.Update(targetProgression, deltaTime);
            }
            else
            {
                currentProgression = Mathf.MoveTowards(currentProgression, targetProgression, deltaTime * animationSpeed);
            }

            // Update all effects with current progression
            UpdateAllEffects(currentProgression);

            // Check for completion events
            CheckProgressionEvents(currentProgression);
        }
        else if (!animateProgression && !Mathf.Approximately(currentProgression, targetProgression))
        {
            // Immediately update to target progression
            currentProgression = targetProgression;
            UpdateAllEffects(currentProgression);
            CheckProgressionEvents(currentProgression);
        }
    }

    /// <summary>
    /// Sets the current progression value and updates all effects
    /// </summary>
    /// <param name="value">Progression value (0-1)</param>
    /// <param name="immediate">Whether to skip animation</param>
    public void SetProgression(float value, bool immediate = false)
    {
        // Clamp value if needed
        if (clampProgression)
        {
            value = Mathf.Clamp01(value);
        }

        // Store target value
        targetProgression = value;

        // Update immediately if not animating
        if (immediate || !animateProgression)
        {
            currentProgression = value;
            UpdateAllEffects(currentProgression);
            CheckProgressionEvents(currentProgression);
        }
        else if (useSpringAnimation && springAnimator != null)
        {
            // Set target for spring animation
            springAnimator.SetTarget(value);
        }

        // Fire progression changed event
        onProgressionChanged?.Invoke(value);

        // Auto reset at zero if enabled
        if (autoResetAtZero && Mathf.Approximately(value, 0f))
        {
            ResetAllEffects();
        }
    }

    /// <summary>
    /// Gets the current progression value
    /// </summary>
    public float GetProgression()
    {
        return currentProgression;
    }

    /// <summary>
    /// Updates all effects with the given progression value
    /// </summary>
    /// <param name="progress">Progression value (0-1)</param>
    public void UpdateAllEffects(float progress)
    {
        // Apply animation curve if specified
        float evaluatedProgress = animationCurve.Evaluate(progress);

        // Update each effect
        foreach (var effect in effects)
        {
            if (effect != null && effect.isEnabled)
            {
                effect.UpdateEffect(evaluatedProgress);
            }
        }
    }

    /// <summary>
    /// Resets all effects to their initial state
    /// </summary>
    public void ResetAllEffects()
    {
        foreach (var effect in effects)
        {
            if (effect != null)
            {
                effect.ResetEffect();
            }
        }
    }

    /// <summary>
    /// Validates all effects to ensure they are properly set up
    /// </summary>
    /// <returns>True if all effects are valid</returns>
    public bool ValidateAllEffects()
    {
        bool allValid = true;

        foreach (var effect in effects)
        {
            if (effect != null)
            {
                bool isValid = effect.ValidateEffect();
                if (!isValid)
                {
                    allValid = false;
                    if (disableInvalidEffects)
                    {
                        effect.SetEnabled(false);
                    }
                }
            }
        }

        return allValid;
    }

    /// <summary>
    /// Adds a new effect to the list
    /// </summary>
    /// <param name="effect">Effect to add</param>
    public void AddEffect(PredictionZoneEffect effect)
    {
        if (effect != null && !effects.Contains(effect))
        {
            effects.Add(effect);
            effect.ValidateEffect();
            effect.UpdateEffect(currentProgression);
        }
    }

    /// <summary>
    /// Removes an effect from the list
    /// </summary>
    /// <param name="effect">Effect to remove</param>
    public void RemoveEffect(PredictionZoneEffect effect)
    {
        if (effect != null && effects.Contains(effect))
        {
            effects.Remove(effect);
        }
    }

    /// <summary>
    /// Finds effects of a specific type
    /// </summary>
    /// <typeparam name="T">Effect type to find</typeparam>
    /// <returns>List of matching effects</returns>
    public List<T> GetEffectsOfType<T>() where T : PredictionZoneEffect
    {
        List<T> result = new List<T>();
        foreach (var effect in effects)
        {
            if (effect is T typedEffect)
            {
                result.Add(typedEffect);
            }
        }
        return result;
    }

    /// <summary>
    /// Checks for progression-related events
    /// </summary>
    private void CheckProgressionEvents(float value)
    {
        // Complete event
        if (!wasComplete && Mathf.Approximately(value, 1f))
        {
            wasComplete = true;
            onComplete?.Invoke();
        }
        else if (wasComplete && !Mathf.Approximately(value, 1f))
        {
            wasComplete = false;
        }

        // Reset event
        if (!wasReset && Mathf.Approximately(value, 0f))
        {
            wasReset = true;
            onReset?.Invoke();
        }
        else if (wasReset && !Mathf.Approximately(value, 0f))
        {
            wasReset = false;
        }
    }

    private void OnDestroy()
    {
        // Clean up resources
        springAnimator = null;
        effects.Clear();
    }

    /// <summary>
    /// Implementation of IProgressionHandler.UpdateProgression
    /// </summary>
    /// <param name="value">The progression value to set (0-1)</param>
    public void UpdateProgression(float value)
    {
        SetProgression(value);
    }
} 