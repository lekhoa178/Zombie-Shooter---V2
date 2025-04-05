using UnityEngine;

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