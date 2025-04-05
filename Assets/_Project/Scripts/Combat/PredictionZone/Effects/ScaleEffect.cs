using UnityEngine;

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

    protected override void Awake()
    {
        base.Awake();
        
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
        float scaleMultiplier = scaleCurve.Evaluate(0);
        Vector3 resetScale = initialScale;
        resetScale.Scale(Vector3.one + (scaleAxes * (scaleMultiplier - 1)));
        transform.localScale = resetScale;
    }
} 