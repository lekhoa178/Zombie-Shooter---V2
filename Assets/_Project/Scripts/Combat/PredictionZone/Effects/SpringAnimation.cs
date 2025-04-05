using UnityEngine;

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