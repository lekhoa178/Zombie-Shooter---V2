using UnityEngine;

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