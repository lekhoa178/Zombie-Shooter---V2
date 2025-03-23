using System.Collections;
using UnityEngine;
using UnityUtils;
using MoreMountains.Tools;

namespace LocomotionSystem
{
    public class IFrameManager : Singleton<IFrameManager>
    {
        [System.Serializable]
        public class IFrameSettings
        {
            public float duration = 0.5f;
            public float recoveryDuration = 0.1f;
        }

        [Header("IFrame Settings")]
        [SerializeField] private IFrameSettings damageIFrameSettings = new IFrameSettings();
        [SerializeField] private IFrameSettings attackIFrameSettings = new IFrameSettings();
        
        public bool IsInvincible => isInvincible;
        
        [field: SerializeField] public bool Linear { get; private set; }
        
        private float iFrameDuration;
        private float recoveryDuration;
        private bool isInvincible;
        
        // Visual feedback
        [Header("Visual Feedback")]
        [SerializeField] private float blinkRate = 0.1f;
        [SerializeField] private Material defaultMaterial;
        [SerializeField] private Material invincibleMaterial;

        public MMTweenType invincibilityTween = new MMTweenType(new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1)), "Linear");
        
        private Coroutine iFrameCoroutine;
        private Coroutine blinkCoroutine;

        public void StartDamageInvincibility()
        {
            StartInvincibility(damageIFrameSettings.duration, true, damageIFrameSettings.recoveryDuration);
        }

        public void StartAttackInvincibility()
        {
            StartInvincibility(attackIFrameSettings.duration, false, attackIFrameSettings.recoveryDuration);
        }

        public void StartInvincibility(float duration)
        {
            StartInvincibility(duration, true, damageIFrameSettings.recoveryDuration);
        }

        public void StartInvincibility(float duration, bool withVisualFeedback)
        {
            float recoveryTime = withVisualFeedback ? damageIFrameSettings.recoveryDuration : attackIFrameSettings.recoveryDuration;
            StartInvincibility(duration, withVisualFeedback, recoveryTime);
        }

        private void StartInvincibility(float duration, bool withVisualFeedback, float recoveryTime)
        {
            if (isInvincible)
            {
                if (iFrameCoroutine != null)
                    StopCoroutine(iFrameCoroutine);
                if (blinkCoroutine != null)
                    StopCoroutine(blinkCoroutine);
            }
            
            iFrameDuration = duration;
            recoveryDuration = recoveryTime;
            isInvincible = true;
            
            iFrameCoroutine = StartCoroutine(IFrameCoroutine());
            if (withVisualFeedback)
            {
                blinkCoroutine = StartCoroutine(BlinkCoroutine());
            }
        }

        private IEnumerator IFrameCoroutine()
        {
            if (iFrameDuration <= 0)
            {
                yield break;
            }
            
            var iFrameTimer = 0f;

            while (iFrameTimer < iFrameDuration - recoveryDuration)
            {
                iFrameTimer += Time.deltaTime;
                yield return null;
            }

            // Recovery phase
            if (recoveryDuration > 0 && !Linear)
            {
                float recoveryTimer = 0f;
                while (recoveryTimer < 1f)
                {
                    recoveryTimer += Time.deltaTime / recoveryDuration;
                    yield return null;
                }
            }

            isInvincible = false;
        }
        
        private IEnumerator BlinkCoroutine()
        {
            var renderer = FindObjectOfType<Player>()?.TargetAvatarRenderer;
            if (!renderer) yield break;
            
            var originalMaterial = renderer.material;
            var isBlinking = true;
            
            while (isInvincible)
            {
                renderer.material = isBlinking ? invincibleMaterial : originalMaterial;
                isBlinking = !isBlinking;
                yield return new WaitForSeconds(blinkRate);
            }
            
            renderer.material = originalMaterial;
        }
        
        // Call this from the player's damage handling code
        public bool CanTakeDamage()
        {
            return !isInvincible;
        }
    }
} 