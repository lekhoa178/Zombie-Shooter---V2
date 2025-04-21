using System.Collections;
using LocomotionSystem;
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
    [SerializeField] private AnimationType m_AnimationTypes = AnimationType.Fade;
    [SerializeField] private float m_AnimationDuration = 0.3f;
    [SerializeField] private float m_SlideDistance = 100f;
    [SerializeField] private bool m_ShowCursorWhenActive = true;

    [Header("Animation Curves")]
    [SerializeField] private AnimationCurve m_FadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve m_SlideHorizontalCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve m_SlideVerticalCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve m_BackgroundFadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Background Settings")]
    [SerializeField] private Image m_Background;
    [SerializeField] private bool m_FadeBackground = true;
    [SerializeField] private float m_BackgroundFadeDuration = 0.3f;
    [SerializeField] private Color m_BackgroundColor = new Color(0, 0, 0, 0.5f);

    [Header("Events")]
    [SerializeField] private UnityEvent m_OnPageOpened;
    [SerializeField] private UnityEvent m_OnPageClosed;

    private CanvasGroup m_CanvasGroup;
    private RectTransform m_RectTransform;
    private Vector2 m_StartPosition;
    private Coroutine m_CurrentAnimation;
    private Coroutine m_BackgroundAnimation;

    private CameraCursor m_CameraCursor;
    
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
            m_Background.color = new Color(m_BackgroundColor.r, m_BackgroundColor.g, m_BackgroundColor.b, 0);
        }
    }

    private void OnEnable()
    {
        StartCoroutine(DelayedEnable());
    }

    IEnumerator DelayedEnable()
    {
        yield return new WaitForEndOfFrame();

        if (m_ShowCursorWhenActive && m_CameraCursor)
        {
            m_CameraCursor.UIToggleCursor(true);
            m_CameraCursor.enabled = false;
        }
    }

    private void OnDisable()
    {
        if (m_ShowCursorWhenActive && m_CameraCursor != null)
        {
            m_CameraCursor.UIToggleCursor(false);
            m_CameraCursor.enabled = true;
        }
    }

    public void Enter()
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

    public void Exit()
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        if (m_CurrentAnimation != null)
            StopCoroutine(m_CurrentAnimation);

        if (m_BackgroundAnimation != null)
            StopCoroutine(m_BackgroundAnimation);

        m_CurrentAnimation = StartCoroutine(AnimateExit());

        if (m_Background != null && m_FadeBackground)
        {
            m_BackgroundAnimation = StartCoroutine(AnimateBackground(false));
        }
    }

    private IEnumerator AnimateBackground(bool fadeIn)
    {
        float startAlpha = fadeIn ? 0f : m_BackgroundColor.a;
        float endAlpha = fadeIn ? m_BackgroundColor.a : 0f;
        float elapsedTime = 0f;

        // Make sure background is visible
        if (m_Background.gameObject.activeSelf == false)
            m_Background.gameObject.SetActive(true);

        while (elapsedTime < m_BackgroundFadeDuration)
        {
            float t = elapsedTime / m_BackgroundFadeDuration;
            float curvedT = m_BackgroundFadeCurve.Evaluate(t);
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, curvedT);

            m_Background.color = new Color(
                m_BackgroundColor.r,
                m_BackgroundColor.g,
                m_BackgroundColor.b,
                currentAlpha
            );

            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        // Ensure final state
        m_Background.color = new Color(
            m_BackgroundColor.r,
            m_BackgroundColor.g,
            m_BackgroundColor.b,
            endAlpha
        );

        // Disable background object if fully transparent
        if (!fadeIn && endAlpha <= 0)
            m_Background.gameObject.SetActive(false);
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

    private IEnumerator AnimateExit()
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

        // Hide the page
        gameObject.SetActive(false);
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