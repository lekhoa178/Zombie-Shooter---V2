using Flyweight;
using UnityEngine;

[CreateAssetMenu(fileName = "New Combo Config", menuName = "Combat/Combo Configuration")]
public class ComboConfiguration : ScriptableObject
{
    [Header("Combo Timings")] public float ComboWindowDuration = 0.8f;

    [Header("Attack Speed")]
    [Tooltip("Base duration between attacks in seconds")]
    public float BaseAttackInterval = 0.5f;
    [Tooltip("Attack speed multiplier. Higher value = faster attacks")]
    [Range(0.5f, 10f)]
    public float AttackSpeedMultiplier = 1f;
    [Tooltip("Minimum time between attacks (prevents too fast attacks)")]
    public float MinimumAttackInterval = 0.1f;

    [Header("Attack Configurations")] [Tooltip("Attack configurations for each combo index (0-4)")]
    public AttackHitConfiguration[] AttackConfigs = new AttackHitConfiguration[5];

    [Header("Final Attack VFX")]
    public VFXFlyweightSettings LastHitVfxSettings;

    [Header("Target Detection Settings")]
    [Tooltip("Default detection range for targeting enemies")]
    [Range(0f, 15f)]
    public float DefaultDetectionRange = 10f;
    
    [Tooltip("Default dot product threshold for targeting (higher is more strict alignment)")]
    [Range(0f, 1f)]
    public float DefaultDotProductThreshold = 0.15f;

    // Returns the actual attack interval and the animation speed multiplier to use
    public (float interval, float animSpeedMultiplier) GetAttackTimingInfo()
    {
        float calculatedInterval = BaseAttackInterval / AttackSpeedMultiplier;
        float animationSpeedMultiplier = 1f;
        
        // If we hit the minimum cap, scale the animation speed
        if (calculatedInterval < MinimumAttackInterval)
        {
            // Scale animation speed proportionally to how much faster we want to go
            animationSpeedMultiplier = MinimumAttackInterval / calculatedInterval;
            return (MinimumAttackInterval, animationSpeedMultiplier);
        }
        
        return (calculatedInterval, 1f);
    }
    
    // For backward compatibility
    public float GetCurrentAttackInterval()
    {
        return GetAttackTimingInfo().interval;
    }
}