using UnityEngine;

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