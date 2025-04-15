using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PowerTree
{
    public class Heart : MonoBehaviour
    {
        [SerializeField] private Image _heartImage;

        [SerializeField] private Sprite fullHeartSprite;
        [SerializeField] private Sprite halfHeartSprite;
        [SerializeField] private Sprite emptyHeartSprite;
        [SerializeField] private float heartAmount = 1f;

        [Header("Animation Settings")]
        [SerializeField] private float increaseBounceScale = 1.2f;
        [SerializeField] private float increaseDuration = 0.2f;
        [SerializeField] private float decreaseDuration = 0.3f;
        [SerializeField] private float constructionDuration = 0.3f;
        [SerializeField] private float destructionDuration = 0.4f;

        private float _previousAmount;
        private Coroutine _currentAnimation;
        private Vector3 _originalScale;
        private bool _isBeingDestroyed = false;
        private bool _isBeingConstructed = false;
        private AnimationType _currentAnimationType = AnimationType.None;
        
        private enum AnimationType
        {
            None,
            Construction,
            Bounce,
            ShrinkUpdate,
            Destruction
        }

        public float HeartAmount
        {
            get => heartAmount;
            set
            {
                var oldValue = heartAmount;
                heartAmount = Mathf.Clamp01(value);

                // Don't animate if we're in the construction phase
                if (!_isBeingConstructed)
                {
                    AnimateHeartChange(oldValue, heartAmount);
                }
                else
                {
                    // Just update the sprite without animation
                    UpdateSprite();
                }
            }
        }

        public bool IsFull => Mathf.Approximately(heartAmount, 1f);
        public bool IsHalf => Mathf.Approximately(heartAmount, 0.5f);
        public bool IsEmpty => Mathf.Approximately(heartAmount, 0f);
        public bool IsBeingDestroyed => _isBeingDestroyed;

        private void Awake()
        {
            if (_heartImage == null)
            {
                _heartImage = GetComponent<Image>();
            }
            _previousAmount = heartAmount;
            _originalScale = transform.localScale;
            transform.localScale = Vector3.zero; // Start at zero scale
            UpdateSprite();
        }

        private void OnEnable()
        {
            if (_currentAnimation != null)
            {
                CleanupCurrentAnimation();
                // For construction, we always want to start at zero scale
                transform.localScale = Vector3.zero;
            }

            // Mark as being constructed
            _isBeingConstructed = true;

            // Play construction animation
            _currentAnimationType = AnimationType.Construction;
            _currentAnimation = StartCoroutine(ConstructionAnimation());
        }

        private void UpdateSprite()
        {
            if (_heartImage == null)
            {
                return;
            }

            if (Mathf.Approximately(heartAmount, 0))
            {
                _heartImage.sprite = emptyHeartSprite;
            }
            else if (Mathf.Approximately(heartAmount, 0.5f))
            {
                _heartImage.sprite = halfHeartSprite;
            }
            else if (Mathf.Approximately(heartAmount, 1))
            {
                _heartImage.sprite = fullHeartSprite;
            }
        }

        public void SetHeartAmount(float amount)
        {
            if (_isBeingDestroyed) return;

            var oldValue = heartAmount;
            heartAmount = Mathf.Clamp01(amount);

            // Don't animate if we're in the construction phase
            if (!_isBeingConstructed)
            {
                AnimateHeartChange(oldValue, heartAmount);
            }
            else
            {
                // Just update the sprite without animation
                UpdateSprite();
            }
        }

        private void ResetScale()
        {
            transform.localScale = _originalScale;
        }

        private void CleanupCurrentAnimation()
        {
            if (_currentAnimation != null)
            {
                // Reset appropriate state based on current animation
                switch (_currentAnimationType)
                {
                    case AnimationType.Bounce:
                    case AnimationType.ShrinkUpdate:
                        ResetScale();
                        break;
                    case AnimationType.Construction:
                        // Don't reset scale during construction
                        break;
                    case AnimationType.Destruction:
                        if (!_isBeingDestroyed)
                        {
                            ResetScale();
                        }
                        break;
                }

                StopCoroutine(_currentAnimation);
                _currentAnimation = null;
                _currentAnimationType = AnimationType.None;
            }
        }

        private void AnimateHeartChange(float oldValue, float newValue)
        {
            if (_isBeingDestroyed || _isBeingConstructed || Mathf.Approximately(oldValue, newValue))
                return;

            CleanupCurrentAnimation();

            if (newValue > oldValue)
            {
                // Heart amount is increasing - bounce animation
                UpdateSprite();
                _currentAnimationType = AnimationType.Bounce;
                _currentAnimation = StartCoroutine(BounceAnimation());
            }
            else
            {
                // Heart amount is decreasing - shrink, update, grow animation
                _currentAnimationType = AnimationType.ShrinkUpdate;
                _currentAnimation = StartCoroutine(ShrinkUpdateGrowAnimation());
            }
        }

        public void DestroyWithAnimation()
        {
            if (_isBeingDestroyed) return;
            _isBeingDestroyed = true;

            CleanupCurrentAnimation();

            _currentAnimationType = AnimationType.Destruction;
            _currentAnimation = StartCoroutine(DestructionAnimation());
        }

        private IEnumerator ConstructionAnimation()
        {
            float elapsed = 0f;
            Vector3 targetScale = _originalScale * increaseBounceScale;
            Vector3 startScale = Vector3.zero;

            // Scale up to bounce point
            while (elapsed < constructionDuration * 0.6f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (constructionDuration * 0.6f);
                transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }

            // Scale down to original
            elapsed = 0f;
            while (elapsed < constructionDuration * 0.4f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (constructionDuration * 0.4f);
                transform.localScale = Vector3.Lerp(targetScale, _originalScale, t);
                yield return null;
            }

            ResetScale();
            _currentAnimation = null;
            _currentAnimationType = AnimationType.None;

            // Construction is complete
            _isBeingConstructed = false;
        }

        private IEnumerator BounceAnimation()
        {
            float elapsed = 0f;
            Vector3 targetScale = _originalScale * increaseBounceScale;

            // Scale up
            while (elapsed < increaseDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (increaseDuration * 0.5f);
                transform.localScale = Vector3.Lerp(_originalScale, targetScale, t);
                yield return null;
            }

            // Scale down
            elapsed = 0f;
            while (elapsed < increaseDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (increaseDuration * 0.5f);
                transform.localScale = Vector3.Lerp(targetScale, _originalScale, t);
                yield return null;
            }

            ResetScale(); // Ensure consistent final state
            _currentAnimation = null;
            _currentAnimationType = AnimationType.None;
        }

        private IEnumerator ShrinkUpdateGrowAnimation()
        {
            float elapsed = 0f;
            Vector3 zeroScale = Vector3.zero;

            // Scale down to zero
            while (elapsed < decreaseDuration * 0.4f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (decreaseDuration * 0.4f);
                transform.localScale = Vector3.Lerp(_originalScale, zeroScale, t);
                yield return null;
            }

            // Update the sprite when we're at zero scale
            UpdateSprite();

            // Scale back up to original
            elapsed = 0f;
            while (elapsed < decreaseDuration * 0.6f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (decreaseDuration * 0.6f);
                transform.localScale = Vector3.Lerp(zeroScale, _originalScale, t);
                yield return null;
            }

            ResetScale(); // Ensure consistent final state
            _currentAnimation = null;
            _currentAnimationType = AnimationType.None;
        }

        private IEnumerator DestructionAnimation()
        {
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;
            Vector3 endScale = Vector3.zero;

            while (elapsed < destructionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / destructionDuration;
                transform.localScale = Vector3.Lerp(startScale, endScale, t);
                yield return null;
            }

            // Actually destroy the object after animation completes
            Destroy(gameObject);
        }
    }
}