using System.Collections;
using UnityEngine;
using TMPro;

namespace DungeonGeneration
{
    /// <summary>
    /// Manages a healing stage where the player can restore health
    /// </summary>
    public class HealingStageManager : StageBaseManager
    {
        #region Serialized Fields
        [Header("Healing Settings")]
        [SerializeField] private float m_HealingAmount = 100f;
        [SerializeField] private bool m_RestoreFullHealth = true;
        [SerializeField] private float m_HealingDelay = 1f;
        [SerializeField] private float m_ExitDelay = 3f;
        [SerializeField] private bool m_AutoHeal = true;
        
        [Header("Visual Effects")]
        [SerializeField] private GameObject m_HealingEffect;
        [SerializeField] private Transform m_EffectSpawnPoint;
        [SerializeField] private GameObject m_HealingAura;
        [SerializeField] private Light m_AmbientLight;
        [SerializeField] private Color m_HealingLightColor = new Color(0.1f, 0.9f, 0.3f);
        [SerializeField] private float m_LightIntensityMax = 2f;
        
        [Header("Audio")]
        [SerializeField] private AudioClip m_HealingSound;
        [SerializeField] private AudioClip m_AmbientSound;
        [SerializeField] private float m_AmbientVolume = 0.5f;
        
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI m_PromptText;
        [SerializeField] private GameObject m_HealingUI;
        [SerializeField] private TextMeshProUGUI m_HealingText;
        #endregion
        
        #region Private Fields
        private bool m_HasHealed = false;
        private GameObject m_Player;
        private AudioSource m_AudioSource;
        private AudioSource m_AmbientAudioSource;
        #endregion
        
        #region Unity Lifecycle
        private void Awake()
        {
            m_AudioSource = GetComponent<AudioSource>();
            
            if (m_AudioSource == null)
            {
                m_AudioSource = gameObject.AddComponent<AudioSource>();
            }
            
            // Create a second audio source for ambient sound
            m_AmbientAudioSource = gameObject.AddComponent<AudioSource>();
            m_AmbientAudioSource.loop = true;
            m_AmbientAudioSource.volume = m_AmbientVolume;
            m_AmbientAudioSource.spatialBlend = 0f; // 2D sound
            
            if (m_AmbientSound != null)
            {
                m_AmbientAudioSource.clip = m_AmbientSound;
                m_AmbientAudioSource.Play();
            }
            
            // Set up light
            if (m_AmbientLight != null)
            {
                m_AmbientLight.color = m_HealingLightColor;
                m_AmbientLight.intensity = 0f;
            }
        }
        
        private void Start()
        {
            // Find player
            m_Player = GameObject.FindGameObjectWithTag("Player");
            
            if (m_Player != null)
            {
                // m_PlayerHealth = m_Player.GetComponent<HealthComponent>();
                //
                // if (m_PlayerHealth == null)
                // {
                //     Debug.LogWarning("Player does not have a HealthComponent!");
                // }
            }
            else
            {
                Debug.LogError("Player not found in scene! Make sure the player has the 'Player' tag.");
            }
            
            // Set up UI
            if (m_PromptText != null)
            {
                m_PromptText.text = m_AutoHeal ? "Healing..." : "Press [E] to heal";
            }
            
            // Start healing automatically if enabled
            if (m_AutoHeal)
            {
                StartCoroutine(HealPlayerAfterDelay(m_HealingDelay));
            }
            
            // Start visual effects
            StartCoroutine(FadeInEffects());
        }
        
        private void Update()
        {
            if (!m_AutoHeal && !m_HasHealed && Input.GetKeyDown(KeyCode.E))
            {
                HealPlayer();
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && !m_AutoHeal && !m_HasHealed)
            {
                // Show prompt
                if (m_PromptText != null)
                {
                    m_PromptText.gameObject.SetActive(true);
                }
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                // Hide prompt
                if (m_PromptText != null)
                {
                    m_PromptText.gameObject.SetActive(false);
                }
            }
        }
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Heals the player immediately
        /// </summary>
        public void HealPlayer()
        {
            // if (m_HasHealed || m_PlayerHealth == null)
            //     return;
            //     
            m_HasHealed = true;
            
            // Play healing effect
            if (m_HealingEffect != null && m_EffectSpawnPoint != null)
            {
                Instantiate(m_HealingEffect, m_EffectSpawnPoint.position, Quaternion.identity);
            }
            
            // Play healing sound
            if (m_AudioSource != null && m_HealingSound != null)
            {
                m_AudioSource.PlayOneShot(m_HealingSound);
            }
            
            // Activate healing aura
            if (m_HealingAura != null)
            {
                m_HealingAura.SetActive(true);
            }
            
            // Apply healing
            // if (m_RestoreFullHealth)
            // {
            //     m_PlayerHealth.RestoreFullHealth();
            //     
            //     if (m_HealingText != null)
            //     {
            //         m_HealingText.text = "Full health restored!";
            //     }
            // }
            // else
            // {
            //     m_PlayerHealth.Heal(m_HealingAmount);
            //     
            //     if (m_HealingText != null)
            //     {
            //         m_HealingText.text = $"Healed {m_HealingAmount} HP";
            //     }
            // }
            
            // Show healing UI
            if (m_HealingUI != null)
            {
                m_HealingUI.SetActive(true);
            }
            
            // Hide prompt
            if (m_PromptText != null)
            {
                m_PromptText.gameObject.SetActive(false);
            }
            
            // Create exit portal after delay
            StartCoroutine(CreateExitPortalsDelayed(m_ExitDelay));
        }
        #endregion
        
        #region Private Methods
        /// <summary>
        /// Fades in the visual effects
        /// </summary>
        private IEnumerator FadeInEffects()
        {
            float duration = 2f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                
                // Fade in light
                if (m_AmbientLight != null)
                {
                    m_AmbientLight.intensity = Mathf.Lerp(0f, m_LightIntensityMax, t);
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Ensure final values are set
            if (m_AmbientLight != null)
            {
                m_AmbientLight.intensity = m_LightIntensityMax;
            }
        }
        
        /// <summary>
        /// Heals the player after a delay
        /// </summary>
        private IEnumerator HealPlayerAfterDelay(float _delay)
        {
            yield return new WaitForSeconds(_delay);
            HealPlayer();
        }
        
        /// <summary>
        /// Creates exit portals after a delay
        /// </summary>
        private IEnumerator CreateExitPortalsDelayed(float _delay)
        {
            yield return new WaitForSeconds(_delay);
            CreateExitPortals();
        }
        
        /// <summary>
        /// Creates exit portals to leave the healing stage
        /// </summary>
        private void CreateExitPortals()
        {
            // This would typically notify the DungeonManager to create portals to the next stages
            Debug.Log("Creating exit portals from healing stage");
            
            // In a real implementation, this might look like:
            // DungeonManager.Instance.CreateStageExits();
        }
        #endregion
    }
} 