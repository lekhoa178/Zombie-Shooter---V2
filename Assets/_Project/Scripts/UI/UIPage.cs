using System.Collections;
using LocomotionSystem;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class UIPage : MonoBehaviour
{
    [System.Flags]
    public enum AnimationType
    {
        None = 0,
        Fade = 1 << 0,
        SlideHorizontal = 1 << 1,
        SlideVertical = 1 << 2
    }

    [Header("Animation Settings")]
    [SerializeField] protected AnimationType m_AnimationTypes = AnimationType.Fade;
    [SerializeField] protected float m_AnimationDuration = 0.3f;
    [SerializeField] protected float m_SlideDistance = 100f;
    [SerializeField] protected bool m_ShowCursorWhenActive = true;

    [Header("Animation Curves")]
    [SerializeField] protected AnimationCurve m_FadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] protected AnimationCurve m_SlideHorizontalCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] protected AnimationCurve m_SlideVerticalCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] protected AnimationCurve m_BackgroundFadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Background Settings")]
    [SerializeField] protected Image m_Background;
    [SerializeField] protected bool m_FadeBackground = true;
    [SerializeField] protected float m_BackgroundFadeDuration = 0.3f;
    [SerializeField] protected Color m_BackgroundColor = new Color(0, 0, 0, 0.5f);

    [Header("Events")]
    [SerializeField] protected UnityEvent m_OnPageOpened;
    [SerializeField] protected UnityEvent m_OnPageClosed;

    private CanvasGroup m_CanvasGroup;
    private RectTransform m_RectTransform;
    private Vector2 m_StartPosition;
    private Coroutine m_CurrentAnimation;
    private Coroutine m_BackgroundAnimation;

    private CameraCursor m_CameraCursor;
    private GameObject m_BackgroundGameObject;
    
    public bool ShowCursorWhenActive => m_ShowCursorWhenActive;

    private void Awake()
    {
        m_CanvasGroup = GetComponent<CanvasGroup>();
        m_RectTransform = GetComponent<RectTransform>();
        m_StartPosition = m_RectTransform.anchoredPosition;

        m_CameraCursor = FindAnyObjectByType<CameraCursor>();
        
        m_CanvasGroup.interactable = true;
        m_CanvasGroup.blocksRaycasts = true;

        // Initialize background if set
        if (m_Background != null)
        {
            m_BackgroundGameObject = m_Background.gameObject;
            m_Background.color = new Color(m_BackgroundColor.r, m_BackgroundColor.g, m_BackgroundColor.b, 0);
        }
    }

    protected void OnEnable()
    {
        StartCoroutine(DelayedEnable());
    }

    IEnumerator DelayedEnable()
    {
        yield return new WaitForNextFrameUnit();

        if (m_ShowCursorWhenActive && m_CameraCursor && !m_CameraCursor.IsUICursorVisible)
        {
            m_CameraCursor.UIToggleCursor(true);
            m_CameraCursor.enabled = false;
        }
    }

    private void OnDisable()
    {
        if (m_ShowCursorWhenActive && m_CameraCursor != null && !UIManager.Instance.IsLastPageInHistory)
        {
            m_CameraCursor.UIToggleCursor(false);
            m_CameraCursor.enabled = true;
        }
    }

    public virtual void Enter()
    {
        if (m_CurrentAnimation != null)
            StopCoroutine(m_CurrentAnimation);

        if (m_BackgroundAnimation != null)
            StopCoroutine(m_BackgroundAnimation);

        m_CurrentAnimation = StartCoroutine(AnimateEnter());
        
        m_CanvasGroup.interactable = true;
        m_CanvasGroup.blocksRaycasts = true;

        if (m_Background != null && m_FadeBackground)
        {
            m_BackgroundAnimation = StartCoroutine(AnimateBackground(true));
        }
    }

    public virtual void Exit()
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        if (m_CurrentAnimation != null)
            StopCoroutine(m_CurrentAnimation);

        if (m_BackgroundAnimation != null)
            StopCoroutine(m_BackgroundAnimation);

        StartCoroutine(CoordinatedExit());
    }

    private IEnumerator CoordinatedExit()
    {
        bool pageAnimationComplete = false;
        bool backgroundAnimationComplete = false;
        
        m_CurrentAnimation = StartCoroutine(AnimateExit((complete) => pageAnimationComplete = complete));
        
        if (m_Background && m_FadeBackground)
        {
            GameObject backgroundHolder = m_Background.transform.parent.gameObject;
            m_BackgroundAnimation = StartCoroutine(AnimateBackgroundIndependent(
                backgroundHolder, 
                false, 
                (complete) => backgroundAnimationComplete = complete));
        }
        else
        {
            backgroundAnimationComplete = true;
        }
        
        if (!backgroundAnimationComplete && m_Background)
        {
            yield return new WaitUntil(() => backgroundAnimationComplete);
        }
        
        if (!pageAnimationComplete)
        {
            yield return new WaitUntil(() => pageAnimationComplete);
        }
        
        gameObject.SetActive(false);
    }

    private IEnumerator AnimateBackgroundIndependent(GameObject holder, bool fadeIn, System.Action<bool> onComplete)
    {
        var startAlpha = fadeIn ? 0f : m_BackgroundColor.a;
        var endAlpha = fadeIn ? m_BackgroundColor.a : 0f;
        var elapsedTime = 0f;
        
        var backgroundCanvasGroup = m_Background.GetComponent<CanvasGroup>();

        if (fadeIn)
            m_BackgroundGameObject.SetActive(true);

        while (elapsedTime < m_BackgroundFadeDuration)
        {
            var t = elapsedTime / m_BackgroundFadeDuration;
            var curvedT = m_BackgroundFadeCurve.Evaluate(t);
            var currentAlpha = Mathf.Lerp(startAlpha, endAlpha, curvedT);

            if (backgroundCanvasGroup)
            {
                backgroundCanvasGroup.alpha = currentAlpha;
            }
            else
            {
                m_Background.color = new Color(
                    m_BackgroundColor.r,
                    m_BackgroundColor.g,
                    m_BackgroundColor.b,
                    currentAlpha
                );
            }

            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        if (backgroundCanvasGroup)
        {
            backgroundCanvasGroup.alpha = endAlpha;
        }
        else
        {
            m_Background.color = new Color(
                m_BackgroundColor.r,
                m_BackgroundColor.g,
                m_BackgroundColor.b,
                endAlpha
            );
        }

        if (!fadeIn)
            m_BackgroundGameObject.SetActive(false);
            
        onComplete?.Invoke(true);
    }

    private IEnumerator AnimateBackground(bool fadeIn)
    {
        float startAlpha = fadeIn ? 0f : m_BackgroundColor.a;
        float endAlpha = fadeIn ? m_BackgroundColor.a : 0f;
        float elapsedTime = 0f;
        
        CanvasGroup backgroundCanvasGroup = m_Background.GetComponent<CanvasGroup>();

        // Make sure background is visible
        if (fadeIn)
            m_BackgroundGameObject.SetActive(true);

        while (elapsedTime < m_BackgroundFadeDuration)
        {
            var t = elapsedTime / m_BackgroundFadeDuration;
            var curvedT = m_BackgroundFadeCurve.Evaluate(t);
            var currentAlpha = Mathf.Lerp(startAlpha, endAlpha, curvedT);

            if (backgroundCanvasGroup)
            {
                backgroundCanvasGroup.alpha = currentAlpha;
            }
            else
            {
                m_Background.color = new Color(
                    m_BackgroundColor.r,
                    m_BackgroundColor.g,
                    m_BackgroundColor.b,
                    currentAlpha
                );
            }

            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        if (backgroundCanvasGroup)
        {
            backgroundCanvasGroup.alpha = endAlpha;
        }
        else
        {
            m_Background.color = new Color(
                m_BackgroundColor.r,
                m_BackgroundColor.g,
                m_BackgroundColor.b,
                endAlpha
            );
        }

        // Disable background object if fully transparent
        if (!fadeIn)
            m_BackgroundGameObject.SetActive(false);
    }

    private IEnumerator AnimateEnter()
    {
        // Fade animation initial state
        if ((m_AnimationTypes & AnimationType.Fade) != 0)
        {
            m_CanvasGroup.alpha = 0f;
        }

        // Horizontal slide initial state
        if ((m_AnimationTypes & AnimationType.SlideHorizontal) != 0)
        {
            Vector2 currentPos = m_RectTransform.anchoredPosition;
            m_RectTransform.anchoredPosition = new Vector2(m_StartPosition.x - m_SlideDistance, currentPos.y);
        }

        // Vertical slide initial state
        if ((m_AnimationTypes & AnimationType.SlideVertical) != 0)
        {
            Vector2 currentPos = m_RectTransform.anchoredPosition;
            m_RectTransform.anchoredPosition = new Vector2(currentPos.x, m_StartPosition.y - m_SlideDistance);
        }

        gameObject.SetActive(true);

        float elapsedTime = 0;
        while (elapsedTime < m_AnimationDuration)
        {
            float t = elapsedTime / m_AnimationDuration;
            ApplyAnimation(t, false);

            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        // Ensure final state is correct
        ApplyAnimation(1f, false);
        m_CanvasGroup.blocksRaycasts = true;
        m_OnPageOpened?.Invoke();
    }

    private IEnumerator AnimateExit(System.Action<bool> onComplete = null)
    {
        m_CanvasGroup.blocksRaycasts = false;

        // Start from normal position
        float elapsedTime = 0;
        while (elapsedTime < m_AnimationDuration)
        {
            float t = elapsedTime / m_AnimationDuration;
            ApplyAnimation(t, true);

            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        // Ensure final state is correct
        ApplyAnimation(1f, true);
        m_OnPageClosed?.Invoke();
        
        onComplete?.Invoke(true);
    }

    private void ApplyAnimation(float t, bool isExit)
    {
        // Apply each animation independently based on flags

        // Fade animation
        if ((m_AnimationTypes & AnimationType.Fade) != 0)
        {
            float fadeCurvedT = m_FadeCurve.Evaluate(t);
            m_CanvasGroup.alpha = isExit ? 1 - fadeCurvedT : fadeCurvedT;
        }

        // Current position to apply combined animations
        Vector2 currentPos = m_RectTransform.anchoredPosition;
        Vector2 newPos = currentPos;

        // Horizontal slide animation
        if ((m_AnimationTypes & AnimationType.SlideHorizontal) != 0)
        {
            float slideHCurvedT = m_SlideHorizontalCurve.Evaluate(t);
            float xPos;
            if (isExit)
            {
                // Slide out in reverse direction from starting position
                xPos = Mathf.Lerp(m_StartPosition.x, m_StartPosition.x - m_SlideDistance, slideHCurvedT);
            }
            else
            {
                // Slide in from offset to starting position
                xPos = Mathf.Lerp(m_StartPosition.x - m_SlideDistance, m_StartPosition.x, slideHCurvedT);
            }
            newPos.x = xPos;
        }
        else
        {
            newPos.x = m_StartPosition.x;
        }

        // Vertical slide animation
        if ((m_AnimationTypes & AnimationType.SlideVertical) != 0)
        {
            float slideVCurvedT = m_SlideVerticalCurve.Evaluate(t);
            float yPos;
            if (isExit)
            {
                // Slide out in reverse direction from starting position
                yPos = Mathf.Lerp(m_StartPosition.y, m_StartPosition.y - m_SlideDistance, slideVCurvedT);
            }
            else
            {
                // Slide in from offset to starting position
                yPos = Mathf.Lerp(m_StartPosition.y - m_SlideDistance, m_StartPosition.y, slideVCurvedT);
            }
            newPos.y = yPos;
        }
        else
        {
            newPos.y = m_StartPosition.y;
        }

        // Apply the final position with all animation effects
        m_RectTransform.anchoredPosition = newPos;
    }
}