using System;
using UnityEngine;
using UnityEngine.Events;

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