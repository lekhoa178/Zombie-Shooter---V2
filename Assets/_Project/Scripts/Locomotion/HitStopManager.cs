using MoreMountains.Tools;
using UnityEngine;
using UnityUtils;
using System.Collections;
using UnityEngine.Serialization;

namespace LocomotionSystem
{
    public class HitStopManager : Singleton<HitStopManager>
    {
        public bool IsTimeStopped => isTimeStopped;
        
        [field: SerializeField] public bool Linear { get; private set; }
        [field: SerializeField] public float recoveryDuration = 0.1f;
        
        private float hitStopDuration;
        private bool isTimeStopped;

        public MMTweenType animatePositionTween = new MMTweenType(new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1)), "Linear");

        private Coroutine hitStopCoroutine;

        public void HitStop(float duration)
        {
            HitStop(duration, 0.01f); // Default timeScale value for backward compatibility
        }
        
        public void HitStop(float duration, float timeScale)
        {
            if (isTimeStopped)
            {
                StopCoroutine(hitStopCoroutine);
            }
            
            hitStopDuration = duration;
            isTimeStopped = true;
            
            // Store original time scale and set to specified value
            float originalTimeScale = Time.timeScale;
            Time.timeScale = timeScale;
            
            hitStopCoroutine = StartCoroutine(HitStopCoroutine(originalTimeScale));
        }
        
        public void HitStopPlayer(float duration, float timeScale)
        {
            // For now, this just uses the global time scale method
            // In a more advanced implementation, you could freeze just the player's animations/movement
            HitStop(duration, timeScale);
        }
        
        public void HitStopTarget(GameObject target, float duration, float timeScale)
        {
            if (target == null) return;
            
            // Start a coroutine specific to this target
            StartCoroutine(HitStopTargetCoroutine(target, duration, timeScale));
        }
        
        private IEnumerator HitStopTargetCoroutine(GameObject target, float duration, float timeScale)
        {
            if (duration <= 0 || target == null)
            {
                yield break;
            }
            
            // Get animator and rigidbody if available
            var targetAnimator = target.GetComponentInChildren<Animator>();
            var targetRigidbody = target.GetComponent<Rigidbody>();
            
            // Store original values
            float originalAnimatorSpeed = targetAnimator ? targetAnimator.speed : 1f;
            Vector3 originalVelocity = targetRigidbody ? targetRigidbody.linearVelocity : Vector3.zero;
            
            // Apply hit stop effects
            if (targetAnimator)
                targetAnimator.speed = timeScale;
                
            if (targetRigidbody)
                targetRigidbody.linearVelocity = Vector3.zero;
            
            // Wait for duration
            yield return new WaitForSecondsRealtime(duration);
            
            // Recovery phase
            if (recoveryDuration > 0 && !Linear)
            {
                float recoveryTimer = 0f;
                while (recoveryTimer < 1f)
                {
                    recoveryTimer += Time.unscaledDeltaTime / recoveryDuration;
                    var curveValue = MMTween.Tween(recoveryTimer, 0f, 1f, timeScale, originalAnimatorSpeed, animatePositionTween);
                    
                    if (targetAnimator)
                        targetAnimator.speed = curveValue;
                        
                    yield return null;
                }
            }
            
            // Restore original values
            if (targetAnimator)
                targetAnimator.speed = originalAnimatorSpeed;
                
            if (targetRigidbody)
                targetRigidbody.linearVelocity = originalVelocity;
        }

        private IEnumerator HitStopCoroutine()
        {
            return HitStopCoroutine(1f); // Default to restoring to normal time scale
        }
        
        private IEnumerator HitStopCoroutine(float originalTimeScale)
        {
            if (hitStopDuration <= 0)
            {
                yield break;
            }
            
            var hitStopTimer = 0f;

            while (hitStopTimer < hitStopDuration - recoveryDuration)
            {
                hitStopTimer += Time.unscaledDeltaTime;
                yield return null;
            }

            var recoveryTimer = 0f;
            while (recoveryTimer < 1f && !Linear)
            {
                recoveryTimer += Time.unscaledDeltaTime / recoveryDuration;
                var curveValue = MMTween.Tween(recoveryTimer, 0f, 1f, 0f, originalTimeScale, animatePositionTween);
                Time.timeScale = curveValue;
                yield return null;
            }

            Time.timeScale = originalTimeScale;
            isTimeStopped = false;
        }
    }
}