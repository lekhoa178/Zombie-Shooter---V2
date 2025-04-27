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
            [Tooltip("Whether to apply layer exclusions during iFrames")]
            public bool applyLayerExclusions = false;
            [Tooltip("Layers to exclude from collision during iFrames (only if applyLayerExclusions is true)")]
            public LayerMask excludeLayersDuringIFrame = 0;
        }

        [Header("IFrame Settings")]
        [SerializeField] private IFrameSettings damageIFrameSettings = new IFrameSettings();
        [SerializeField] private IFrameSettings attackIFrameSettings = new IFrameSettings();
        
        public bool IsInvincible => isInvincible;
        
        private float iFrameDuration;
        private bool isInvincible;
        private LayerMask originalExcludeLayers;
        private Collider playerCollider;
        
        private Coroutine iFrameCoroutine;

        protected override void Awake()
        {
            base.Awake();
            
            // Find the player and cache the collider
            var player = FindAnyObjectByType<Player>();
            if (player == null) return;
            playerCollider = player.ResizableCapsuleCollider?.CapsuleColliderData.Collider;
            if (playerCollider != null)
            {
                // Store the original exclude layers
                originalExcludeLayers = playerCollider.excludeLayers;
            }
        }

        public void StartDamageInvincibility()
        {
            StartInvincibility(damageIFrameSettings.duration, damageIFrameSettings.applyLayerExclusions, damageIFrameSettings.excludeLayersDuringIFrame);
        }

        public void StartAttackInvincibility()
        {
            StartInvincibility(attackIFrameSettings.duration, attackIFrameSettings.applyLayerExclusions, attackIFrameSettings.excludeLayersDuringIFrame);
        }

        public void StartInvincibility(float duration)
        {
            StartInvincibility(duration, damageIFrameSettings.applyLayerExclusions, damageIFrameSettings.excludeLayersDuringIFrame);
        }

        public void StartInvincibility(float duration, bool applyLayerExclusions = false, LayerMask excludeLayers = default)
        {
            if (isInvincible)
            {
                if (iFrameCoroutine != null)
                {
                    StopCoroutine(iFrameCoroutine);
                    
                    // Make sure to restore layers if we're interrupting an existing invincibility
                    if (applyLayerExclusions)
                    {
                        RestoreOriginalLayers();
                    }
                }
            }
            
            iFrameDuration = duration;
            isInvincible = true;
            
            // Apply layer exclusions to the player's collider only if enabled
            if (applyLayerExclusions && excludeLayers != 0)
            {
                ApplyLayerExclusions(excludeLayers);
            }
            
            iFrameCoroutine = StartCoroutine(IFrameCoroutine(applyLayerExclusions));
        }

        private void ApplyLayerExclusions(LayerMask excludeLayers)
        {
            if (playerCollider == null) return;
            
            // Store original exclude layers if not already stored
            if (originalExcludeLayers == 0)
            {
                originalExcludeLayers = playerCollider.excludeLayers;
            }
            
            // Apply the new exclude layers, combining with original ones
            playerCollider.excludeLayers = originalExcludeLayers | excludeLayers;
            
            #if UNITY_EDITOR
            Debug.Log($"IFrame applying layer exclusions: {LayerMaskToString(excludeLayers)}");
            #endif
        }
        
        private string LayerMaskToString(LayerMask mask)
        {
            string result = "";
            for (int i = 0; i < 32; i++)
            {
                if ((mask & (1 << i)) != 0)
                {
                    if (result != "") result += ", ";
                    result += LayerMask.LayerToName(i);
                }
            }
            return result != "" ? result : "None";
        }

        private IEnumerator IFrameCoroutine(bool applyLayerExclusions)
        {
            if (iFrameDuration <= 0)
            {
                yield break;
            }
            
            var iFrameTimer = 0f;

            while (iFrameTimer < iFrameDuration)
            {
                iFrameTimer += Time.deltaTime;
                yield return null;
            }

            isInvincible = false;
            
            // Only restore layers if we applied them
            if (applyLayerExclusions)
            {
                RestoreOriginalLayers();
            }
        }
        
        // Call to ensure layers are reset, useful for cleanup or when interrupted
        public void ResetExcludeLayers()
        {
            RestoreOriginalLayers();
        }
        
        private void RestoreOriginalLayers()
        {
            if (playerCollider == null) return;
            
            // Restore original exclude layers
            playerCollider.excludeLayers = originalExcludeLayers;
            
            // #if UNITY_EDITOR
            // Debug.Log("IFrame restored original layer exclusions");
            // #endif
        }
        
        // Ensure layers are reset on disable/destroy
        private void OnDisable()
        {
            // Make sure we restore layers if component is disabled
            RestoreOriginalLayers();
        }
        
        private void OnDestroy() 
        {
            // Make sure we restore layers if component is destroyed
            RestoreOriginalLayers();
        }
        
        // Call this from the player's damage handling code
        public bool CanTakeDamage()
        {
            return !isInvincible;
        }

        // Force end any active invincibility
        public void EndInvincibility()
        {
            if (!isInvincible) return;
            
            if (iFrameCoroutine != null)
            {
                StopCoroutine(iFrameCoroutine);
                iFrameCoroutine = null;
            }
            
            isInvincible = false;
            RestoreOriginalLayers();
        }
        
        // Handle application quit
        private void OnApplicationQuit()
        {
            // Make sure we restore layers when the application quits
            if (playerCollider != null)
            {
                RestoreOriginalLayers();
            }
        }
    }
} 