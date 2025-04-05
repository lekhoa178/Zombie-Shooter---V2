using UnityEngine;

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