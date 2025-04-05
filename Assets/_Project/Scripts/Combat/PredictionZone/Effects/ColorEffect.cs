using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Handles color gradient transitions based on progress
/// </summary>
public class ColorEffect : PredictionZoneEffect
{
    [Header("Color Settings")]
    [Tooltip("Gradient defining how the color changes with progress")]
    public Gradient colorGradient;
    [Tooltip("Type of renderer to apply the color to")]
    public RendererType rendererType = RendererType.Renderer;
    [Tooltip("Renderer component to apply the color to")]
    public Renderer targetRenderer;
    [Tooltip("SpriteRenderer component to apply the color to")]
    public SpriteRenderer targetSpriteRenderer;
    [FormerlySerializedAs("PropertyName")] [Tooltip("Property name for the color in the material")]
    public string propertyName = "_BaseColor";
    [Tooltip("Whether to preserve the alpha channel of the renderer")]
    public bool preserveAlpha = false;

    private MaterialPropertyBlock propertyBlock;
    private readonly int ColorProperty;
    private Color originalSpriteColor;
    
    public enum RendererType
    {
        Renderer,
        SpriteRenderer,
        Auto
    }
    
    public ColorEffect()
    {
        ColorProperty = Shader.PropertyToID(propertyName);
    }

    protected override void Awake()
    {
        base.Awake();
        
        // Auto-detect renderer type if set to Auto
        if (rendererType == RendererType.Auto)
        {
            targetSpriteRenderer = GetComponent<SpriteRenderer>();
            if (targetSpriteRenderer != null)
            {
                rendererType = RendererType.SpriteRenderer;
                originalSpriteColor = targetSpriteRenderer.color;
            }
            else
            {
                targetRenderer = GetComponent<Renderer>();
                if (targetRenderer != null)
                {
                    rendererType = RendererType.Renderer;
                }
            }
        }
        else if (rendererType == RendererType.SpriteRenderer)
        {
            if (targetSpriteRenderer == null)
                targetSpriteRenderer = GetComponent<SpriteRenderer>();
                
            if (targetSpriteRenderer != null)
                originalSpriteColor = targetSpriteRenderer.color;
        }
        else // Renderer
        {
            if (targetRenderer == null)
                targetRenderer = GetComponent<Renderer>();
        }
        
        propertyBlock = new MaterialPropertyBlock();
    }

    public override void UpdateEffect(float progress)
    {
        if (!isEnabled) return;
        
        Color newColor = colorGradient.Evaluate(progress);
        
        if (rendererType == RendererType.SpriteRenderer && targetSpriteRenderer != null)
        {
            // Handle SpriteRenderer
            if (preserveAlpha)
            {
                newColor.a = targetSpriteRenderer.color.a;
            }
            targetSpriteRenderer.color = newColor;
        }
        else if (rendererType == RendererType.Renderer && targetRenderer != null)
        {
            // Handle standard Renderer with MaterialPropertyBlock
            propertyBlock.SetColor(ColorProperty, newColor);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    public override void ResetEffect()
    {
        // For SpriteRenderer, restore original color
        if (rendererType == RendererType.SpriteRenderer && targetSpriteRenderer != null)
        {
            targetSpriteRenderer.color = originalSpriteColor;
        }
        
        // For standard Renderer, reset is handled by UpdateEffect(0)
    }
    
    public override bool ValidateEffect()
    {
        bool isValid = true;
        
        if (rendererType == RendererType.SpriteRenderer && targetSpriteRenderer == null)
        {
            targetSpriteRenderer = GetComponent<SpriteRenderer>();
            if (targetSpriteRenderer == null)
            {
                Debug.LogWarning($"ColorEffect on {gameObject.name} is set to use SpriteRenderer but no SpriteRenderer found.");
                isValid = false;
            }
            else
            {
                originalSpriteColor = targetSpriteRenderer.color;
            }
        }
        else if (rendererType == RendererType.Renderer && targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
            if (targetRenderer == null)
            {
                Debug.LogWarning($"ColorEffect on {gameObject.name} is set to use Renderer but no Renderer found.");
                isValid = false;
            }
        }
        
        return isValid && base.ValidateEffect();
    }
} 