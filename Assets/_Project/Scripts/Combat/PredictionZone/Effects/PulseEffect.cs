using UnityEngine;

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