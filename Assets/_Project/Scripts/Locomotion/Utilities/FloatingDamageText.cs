using System.Collections;
using TMPro;
using UnityEngine;

public class FloatingDamageText : MonoBehaviour
{
    [Header("Text Settings")]
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private float lifetime = 1.0f;
    [SerializeField] private Color damageColor = Color.red;
    
    [SerializeField] private Vector3 offset = new Vector3(0, 1, 0);

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 1.0f;
    [SerializeField] private float moveDistance = 1.0f;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Scale Settings")]
    [SerializeField] private Vector3 startScale = new Vector3(1.2f, 1.2f, 1.2f);
    [SerializeField] private Vector3 endScale = new Vector3(0.7f, 0.7f, 0.7f);
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Fade Settings")]
    [SerializeField] private float fadeStartTime = 0.5f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector3 startPosition;
    private Vector3 targetPosition;

    public void Initialize(string text, Transform originTransform, Canvas targetCanvas)
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        // Set up text and color
        damageText.text = $"{text}!!";
        damageText.color = damageColor;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(originTransform.position);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetCanvas.GetComponent<RectTransform>(),
            screenPos,
            null,
            out var localPos);

        rectTransform.localPosition = localPos;
        startPosition = rectTransform.localPosition + offset;
        rectTransform.localPosition = startPosition;

        targetPosition = startPosition + new Vector3(0, moveDistance, 0);

        StartCoroutine(AnimateText());
    }


    private IEnumerator AnimateText()
    {
        float elapsedTime = 0f;
        
        // Initial scale should be the start scale
        rectTransform.localScale = startScale;
        canvasGroup.alpha = 1f;
        
        while (elapsedTime < lifetime)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / lifetime;
            
            float moveProgress = moveCurve.Evaluate(normalizedTime);
            rectTransform.localPosition = Vector3.Lerp(startPosition, targetPosition, moveProgress);
            
            float scaleProgress = scaleCurve.Evaluate(normalizedTime);
            rectTransform.localScale = Vector3.Lerp(startScale, endScale, scaleProgress);
            
            if (elapsedTime > fadeStartTime)
            {
                float fadeProgress = (elapsedTime - fadeStartTime) / (lifetime - fadeStartTime);
                canvasGroup.alpha = fadeCurve.Evaluate(fadeProgress);
            }
            
            yield return null;
        }
        
        Destroy(gameObject);
    }
}