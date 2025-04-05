using UnityEngine;
using System;

public enum PredictionType { Circle, Square, Line }

public class PredictionZone
{
    public GameObject visual { get; private set; }
    public PredictionType type { get; private set; }
    public Transform target { get; private set; }
    public Vector3 offset { get; private set; }
    public Vector3 scale { get; private set; }
    public bool hasProgression { get; private set; }
    public float progression { get; private set; }

    public Action<float> OnProgressUpdated;
    public Action OnZoneInvalidated;

    private IProgressionHandler progressionHandler;
    private bool isValid = true;

    public PredictionZone(GameObject visual, PredictionType type, Transform target, Vector3 offset, Vector3 scale, bool hasProgression, Action<float> onProgressUpdated)
    {
        if (visual == null)
            throw new ArgumentNullException(nameof(visual), "Visual GameObject cannot be null");
        if (target == null)
            throw new ArgumentNullException(nameof(target), "Target Transform cannot be null");
        if (scale.x <= 0 || scale.y <= 0 || scale.z <= 0)
            throw new ArgumentException("Scale values must be greater than zero", nameof(scale));

        this.visual = visual;
        this.type = type;
        this.target = target;
        this.offset = offset;
        this.scale = scale;
        this.hasProgression = hasProgression;
        this.progression = 0f;
        this.OnProgressUpdated = onProgressUpdated;

        // Cache progression handler
        progressionHandler = visual.GetComponent<IProgressionHandler>();
    }

    public void UpdateZone(Transform target, Vector3 offset, Vector3 scale, bool hasProgression, Action<float> onProgressUpdated)
    {
        if (target == null)
        {
            InvalidateZone();
            return;
        }
        if (scale.x <= 0 || scale.y <= 0 || scale.z <= 0)
        {
            Debug.LogWarning($"Invalid scale values provided: {scale}. Using previous valid scale.");
            scale = this.scale;
        }

        this.target = target;
        this.offset = offset;
        this.scale = scale;
        this.hasProgression = hasProgression;
        this.OnProgressUpdated = onProgressUpdated;
        isValid = true;
    }

    public void SetProgression(float value)
    {
        if (!isValid) return;

        progression = Mathf.Clamp01(value);
        OnProgressUpdated?.Invoke(progression);
        ApplyProgressionVisual();
    }

    public void UpdateVisual()
    {
        if (!isValid || !visual || !target)
        {
            InvalidateZone();
            return;
        }

        try
        {
            visual.transform.position = target.position + offset;
            visual.transform.localScale = scale;
            
            if (!visual.gameObject.activeSelf) 
                visual.SetActive(true);

            if (hasProgression)
                ApplyProgressionVisual();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error updating prediction zone visual: {e.Message}");
            InvalidateZone();
        }
    }

    private void ApplyProgressionVisual()
    {
        if (progressionHandler != null)
            progressionHandler.UpdateProgression(progression);
    }

    private void InvalidateZone()
    {
        if (!isValid) return;

        isValid = false;
        OnZoneInvalidated?.Invoke();
        
        if (visual != null)
        {
            visual.SetActive(false);

            var progressionVisual = visual.GetComponent<ProgressionVisual>();
            if (progressionVisual != null)
            {
                progressionVisual.ResetAllEffects();
            }
        }
    }

    public void ResetZone()
    {
        if (visual != null)
        {
            visual.SetActive(false);
            
            var progressionVisual = visual.GetComponent<ProgressionVisual>();
            if (progressionVisual)
            {
                progressionVisual.ResetAllEffects();
            }
        }
    }

    public bool IsValid() => isValid;
}
