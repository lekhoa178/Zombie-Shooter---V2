using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class URPDamageFlashComponent : MonoBehaviour
{
    [Header("Flash Settings")]
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private float flashDuration = 0.15f;
    [SerializeField] private float flashIntensity = 1.5f;
    
    [Header("Intensity Curve")]
    [SerializeField] private bool useIntensityCurve = false;
    [SerializeField] private AnimationCurve intensityCurve = new AnimationCurve(
        new Keyframe(0, 0),
        new Keyframe(0.1f, 1),
        new Keyframe(0.5f, 0.5f),
        new Keyframe(1, 0)
    );
    
    [Header("Frequency Settings")]
    [SerializeField] private bool useFrequency = false;
    [SerializeField] private float flashFrequency = 10f; // Flashes per second
    [SerializeField] [Range(0, 1)] private float minFlashIntensity = 0.2f; // Intensity during "off" part of oscillation
    [SerializeField] [Range(0, 1)] private float colorBlendIntensityMin = 0.2f; // Base color blend at minimum
    [SerializeField] [Range(0, 1)] private float colorBlendIntensityMax = 0.6f; // Base color blend at maximum
    
    [Header("Components")]
    [SerializeField] private Renderer[] targetRenderers;
    
    private IHittable hittableRef;
    private MaterialPropertyBlock propBlock;
    
    // URP shader property IDs
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    private static readonly int EmissionMap = Shader.PropertyToID("_EmissionMap");
    private static readonly int Surface = Shader.PropertyToID("_Surface");
    
    private Color[] originalColors;
    private Coroutine activeFlashRoutine;
    
    private void Awake()
    {
        hittableRef = GetComponent<IHittable>();
        propBlock = new MaterialPropertyBlock();
        
        // Auto-find renderers if not set
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers = GetComponentsInChildren<Renderer>();
        }
        
        // Store original colors
        originalColors = new Color[targetRenderers.Length];
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            targetRenderers[i].GetPropertyBlock(propBlock);
            if (propBlock.HasColor(BaseColor))
            {
                originalColors[i] = propBlock.GetColor(BaseColor);
            }
            else
            {
                // If no property block yet, get from shared material
                originalColors[i] = targetRenderers[i].sharedMaterial.GetColor(BaseColor);
            }
            
            // Enable emission
            foreach (var material in targetRenderers[i].materials)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor(EmissionColor, Color.black);
            }
        }
    }
    
    private void OnEnable()
    {
        hittableRef.OnDamaged += HandleDamage;
    }
    
    private void OnDisable()
    {
        hittableRef.OnDamaged -= HandleDamage;
        
        if (activeFlashRoutine != null)
        {
            StopCoroutine(activeFlashRoutine);
            ResetMaterials();
        }
    }
    
    private void HandleDamage(IHittable enemy, float damage, Vector3 direction)
    {
        // Scale flash with damage
        float damageRatio = 1;
        float intensity = Mathf.Lerp(flashIntensity * 0.5f, flashIntensity, damageRatio);
        
        // Cancel existing flash
        if (activeFlashRoutine != null)
        {
            StopCoroutine(activeFlashRoutine);
        }
        
        // Start new flash
        activeFlashRoutine = StartCoroutine(useFrequency ? FrequencyFlashRoutine(intensity) : FlashRoutine(intensity));
    }
    
    private IEnumerator FlashRoutine(float intensity)
    {
        float elapsed = 0f;
        
        // Set initial flash color and emission
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            var renderer = targetRenderers[i];
            renderer.GetPropertyBlock(propBlock);
            
            // Tint the base color toward flash color
            Color tintedColor = Color.Lerp(originalColors[i], flashColor, colorBlendIntensityMax);
            propBlock.SetColor(BaseColor, tintedColor);
            
            // Set initial emission for glow effect
            propBlock.SetColor(EmissionColor, flashColor * intensity);
            
            renderer.SetPropertyBlock(propBlock);
        }
        
        // Animate the flash using the curve if enabled
        if (useIntensityCurve)
        {
            while (elapsed < flashDuration)
            {
                float normalizedTime = elapsed / flashDuration;
                float curveValue = intensityCurve.Evaluate(normalizedTime);
                float currentIntensity = intensity * curveValue;
                
                for (int i = 0; i < targetRenderers.Length; i++)
                {
                    var renderer = targetRenderers[i];
                    renderer.GetPropertyBlock(propBlock);
                    
                    // Update emission based on curve
                    propBlock.SetColor(EmissionColor, flashColor * currentIntensity);
                    
                    // Optionally blend base color based on curve
                    float colorBlend = Mathf.Lerp(colorBlendIntensityMin, colorBlendIntensityMax, curveValue);
                    Color tintedColor = Color.Lerp(originalColors[i], flashColor, colorBlend);
                    propBlock.SetColor(BaseColor, tintedColor);
                    
                    renderer.SetPropertyBlock(propBlock);
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            // Original behavior without curve
            yield return new WaitForSeconds(flashDuration);
            
            // Fade out over time
            float fadeDuration = flashDuration * 0.5f;
            
            while (elapsed < flashDuration + fadeDuration)
            {
                float t = (elapsed - flashDuration) / fadeDuration;
                
                for (int i = 0; i < targetRenderers.Length; i++)
                {
                    var renderer = targetRenderers[i];
                    renderer.GetPropertyBlock(propBlock);
                    
                    // Gradually return to original color
                    Color currentColor = Color.Lerp(
                        Color.Lerp(originalColors[i], flashColor, colorBlendIntensityMax), 
                        originalColors[i], 
                        t
                    );
                    propBlock.SetColor(BaseColor, currentColor);
                    
                    // Fade out emission
                    propBlock.SetColor(EmissionColor, flashColor * intensity * (1 - t));
                    
                    renderer.SetPropertyBlock(propBlock);
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        
        // Completely reset
        ResetMaterials();
        activeFlashRoutine = null;
    }
    
    private IEnumerator FrequencyFlashRoutine(float maxIntensity)
    {
        float elapsed = 0f;
        float totalDuration = flashDuration * 1.5f; // Make frequency flash last a bit longer
        
        // Continue until total duration elapsed
        while (elapsed < totalDuration)
        {
            // Calculate oscillation value from 0-1 based on sine wave
            float oscillation = Mathf.Abs(Mathf.Sin(elapsed * flashFrequency * Mathf.PI));
            
            // Calculate current intensity by lerping between min and max
            float currentIntensity = Mathf.Lerp(minFlashIntensity, maxIntensity, oscillation);
            
            // Calculate color blend amount
            float colorBlend = Mathf.Lerp(colorBlendIntensityMin, colorBlendIntensityMax, oscillation);
            
            // Apply to all renderers
            for (int i = 0; i < targetRenderers.Length; i++)
            {
                var renderer = targetRenderers[i];
                renderer.GetPropertyBlock(propBlock);
                
                // Tint the base color toward flash color based on oscillation
                Color tintedColor = Color.Lerp(originalColors[i], flashColor, colorBlend);
                propBlock.SetColor(BaseColor, tintedColor);
                
                // Set emission based on current intensity
                propBlock.SetColor(EmissionColor, flashColor * currentIntensity);
                
                renderer.SetPropertyBlock(propBlock);
            }
            
            // Apply fade out over the last 1/3 of the total duration
            float fadeOutStart = totalDuration * 0.66f;
            if (elapsed > fadeOutStart)
            {
                float fadeT = (elapsed - fadeOutStart) / (totalDuration - fadeOutStart);
                
                // Reduce max intensity as we fade out
                maxIntensity = Mathf.Lerp(maxIntensity, 0f, fadeT);
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Completely reset
        ResetMaterials();
        activeFlashRoutine = null;
    }
    
    private void ResetMaterials()
    {
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            var renderer = targetRenderers[i];
            renderer.GetPropertyBlock(propBlock);
            
            // Reset to original color
            propBlock.SetColor(BaseColor, originalColors[i]);
            
            // Turn off emission
            propBlock.SetColor(EmissionColor, Color.black);
            
            renderer.SetPropertyBlock(propBlock);
        }
    }
}