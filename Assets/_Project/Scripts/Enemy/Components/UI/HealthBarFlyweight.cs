using Flyweight;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Flyweight
{
    public class HealthBarFlyweight : Flyweight
    {
        #region Private Fields
        // References to the UI components
        private Image m_BackgroundImage;
        private Image m_FillImage;
        private Image m_SecondaryFillImage;
        private Text m_HealthText;
        private TextMeshProUGUI m_TMPHealthText;
        private TextMeshProUGUI m_BossNameText;
        private Image m_BossIconImage;
        private CanvasGroup m_CanvasGroup;
        
        // Reference to the associated enemy
        private BaseEnemy m_TargetEnemy;
        private Transform m_MainCamera;
        private float m_LastHealthPercent = 1f;
        private float m_CurrentFillAmount = 1f;
        private Coroutine m_UpdateCoroutine;
        private Coroutine m_DamageFlashCoroutine;
        private Coroutine m_DelayFillCoroutine;
        private Coroutine m_AnticipateFillCoroutine;
        private Coroutine m_PrimaryFillCoroutine;
        private Coroutine m_SecondaryFillCoroutine;
        private Vector3 m_DisplayOffset = Vector3.up * 2f;
        private bool m_IsInitialized = false;
        
        // New fields for tracking health changes
        private float m_ActualHealthPercent = 1f;    // The current real health
        private float m_PrimaryFillPercent = 1f;     // Currently displayed primary fill
        private float m_SecondaryFillPercent = 1f;   // Currently displayed secondary fill
        private bool m_IsAnimatingPrimary = false;
        private bool m_IsAnimatingSecondary = false;
        
        // Boss health bar fields
        private bool m_IsBossHealthBar = false;
        private string m_BossName = "";
        private Color m_BossColor = Color.red;
        private Sprite m_BossIcon;

        // Animation settings
        private float m_PrimaryHealthDropDuration = 0.2f;  // Duration for primary health bar drop animation
        #endregion
        
        #region Properties
        // Get settings from our FlyweightSettings
        private HealthBarFlyweightSettings Settings => settings as HealthBarFlyweightSettings;
        
        /// <summary>
        /// Whether this health bar is for a boss
        /// </summary>
        public bool IsBossHealthBar => m_IsBossHealthBar;
        
        /// <summary>
        /// The target enemy this health bar is tracking
        /// </summary>
        public BaseEnemy TargetEnemy => m_TargetEnemy;
        #endregion
        
        #region Initialization
        /// <summary>
        /// Used by HealthBarFlyweightSettings to set up components from prefab
        /// </summary>
        public void InitializeComponents(Image background, Image fill, Image secondaryFill, Text text, CanvasGroup group)
        {
            m_BackgroundImage = background;
            m_FillImage = fill;
            m_SecondaryFillImage = secondaryFill;
            m_HealthText = text;
            m_CanvasGroup = group;
            
            // Check if we have TextMeshPro texts already assigned
            m_TMPHealthText = GetComponentInChildren<TextMeshProUGUI>(true);
            m_BossNameText = transform.Find("BossName")?.GetComponent<TextMeshProUGUI>();
            m_BossIconImage = transform.Find("BossIcon")?.GetComponent<Image>();
            
            // Set up the secondary fill if available
            if (m_SecondaryFillImage != null)
            {
                m_SecondaryFillImage.color = Settings.secondaryFillColor;
                
                // Ensure the secondary fill is behind the primary fill in render order
                m_SecondaryFillImage.transform.SetAsFirstSibling();
                if (m_FillImage != null)
                {
                    m_FillImage.transform.SetAsLastSibling();
                }
            }
        }
        
        /// <summary>
        /// Initialize for a standard enemy in world space
        /// </summary>
        public void Initialize(BaseEnemy enemy, Vector3 offset)
        {
            m_TargetEnemy = enemy;
            m_MainCamera = Camera.main.transform;
            m_DisplayOffset = offset;
            m_IsInitialized = true;
            m_IsBossHealthBar = false;
            
            // Attach to enemy transform for world-space tracking
            transform.SetParent(m_TargetEnemy.transform);
            transform.localPosition = m_DisplayOffset;
            
            // Setup initial state
            m_ActualHealthPercent = m_TargetEnemy.CurrentHealth / m_TargetEnemy.MaxHealth;
            m_PrimaryFillPercent = m_ActualHealthPercent;
            m_SecondaryFillPercent = m_ActualHealthPercent;
            
            UpdateFillAmount(m_PrimaryFillPercent, m_SecondaryFillPercent);
            
            // Stop any existing update routine
            if (m_UpdateCoroutine != null)
                StopCoroutine(m_UpdateCoroutine);
                
            // Start update routine
            m_UpdateCoroutine = StartCoroutine(UpdateHealthBarRoutine());
        }
        
        /// <summary>
        /// Initialize as a boss health bar for HUD display
        /// </summary>
        public void InitializeAsBossBar(BaseEnemy enemy, Color bossColor, string bossName = "", Sprite bossIcon = null)
        {
            m_TargetEnemy = enemy;
            m_IsBossHealthBar = true;
            m_BossName = string.IsNullOrEmpty(bossName) ? enemy.name : bossName;
            m_BossColor = bossColor;
            m_BossIcon = bossIcon;
            m_IsInitialized = true;
            
            // Set up UI elements for boss display
            SetupBossUI();
            
            // Setup initial state
            m_ActualHealthPercent = m_TargetEnemy.CurrentHealth / m_TargetEnemy.MaxHealth;
            m_PrimaryFillPercent = m_ActualHealthPercent;
            m_SecondaryFillPercent = m_ActualHealthPercent;
            
            UpdateFillAmount(m_PrimaryFillPercent, m_SecondaryFillPercent);
            UpdateHealthText();
            
            // Stop any existing update routine
            if (m_UpdateCoroutine != null)
                StopCoroutine(m_UpdateCoroutine);
                
            // Start update routine
            m_UpdateCoroutine = StartCoroutine(UpdateHealthBarRoutine());
        }
        
        /// <summary>
        /// Setup the UI elements for a boss health bar
        /// </summary>
        private void SetupBossUI()
        {
            // Update boss name text if available
            if (m_BossNameText != null)
            {
                m_BossNameText.text = m_BossName;
                m_BossNameText.gameObject.SetActive(true);
            }
            
            // Update boss icon if available
            if (m_BossIconImage != null)
            {
                if (m_BossIcon != null)
                {
                    m_BossIconImage.sprite = m_BossIcon;
                    m_BossIconImage.gameObject.SetActive(true);
                }
                else
                {
                    m_BossIconImage.gameObject.SetActive(false);
                }
            }
            
            // Apply boss color to background or fill if desired
            if (m_BackgroundImage != null)
            {
                // Keep alpha the same but use the boss color
                m_BackgroundImage.color = new Color(
                    m_BossColor.r,
                    m_BossColor.g,
                    m_BossColor.b,
                    m_BackgroundImage.color.a
                );
            }
            
            // Make sure boss health bars are visible
            if (m_CanvasGroup != null)
            {
                m_CanvasGroup.alpha = 1f;
            }
        }
        #endregion
        
        #region State Management
        /// <summary>
        /// Reset internal state when retrieved from pool
        /// </summary>
        public void ResetState()
        {
            // Reset internal state when retrieved from pool
            m_LastHealthPercent = 1f;
            m_CurrentFillAmount = 1f;
            m_ActualHealthPercent = 1f;
            m_PrimaryFillPercent = 1f;
            m_SecondaryFillPercent = 1f;
            m_IsInitialized = false;
            m_IsAnimatingPrimary = false;
            m_IsAnimatingSecondary = false;
            m_IsBossHealthBar = false;
            
            // Reset visuals
            if (m_FillImage != null)
            {
                m_FillImage.fillAmount = 1f;
                m_FillImage.color = Settings.healthyColor;
            }
            
            if (m_SecondaryFillImage != null)
            {
                m_SecondaryFillImage.fillAmount = 1f;
                m_SecondaryFillImage.color = Settings.secondaryFillColor;
            }
            
            if (m_CanvasGroup != null)
            {
                m_CanvasGroup.alpha = 1f;
            }
            
            // Hide boss elements
            if (m_BossNameText != null)
            {
                m_BossNameText.gameObject.SetActive(false);
            }
            
            if (m_BossIconImage != null)
            {
                m_BossIconImage.gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// Clean up resources when released
        /// </summary>
        public void Cleanup()
        {
            // Stop all coroutines
            if (m_UpdateCoroutine != null)
                StopCoroutine(m_UpdateCoroutine);
                
            if (m_DamageFlashCoroutine != null)
                StopCoroutine(m_DamageFlashCoroutine);
                
            if (m_DelayFillCoroutine != null)
                StopCoroutine(m_DelayFillCoroutine);
                
            if (m_AnticipateFillCoroutine != null)
                StopCoroutine(m_AnticipateFillCoroutine);
                
            if (m_PrimaryFillCoroutine != null)
                StopCoroutine(m_PrimaryFillCoroutine);
                
            if (m_SecondaryFillCoroutine != null)
                StopCoroutine(m_SecondaryFillCoroutine);
                
            m_TargetEnemy = null;
            m_IsInitialized = false;
        }
        #endregion
        
        #region Update Routines
        /// <summary>
        /// Updates the health bar continuously
        /// </summary>
        private IEnumerator UpdateHealthBarRoutine()
        {
            while (m_IsInitialized && m_TargetEnemy != null)
            {
                // Skip updates if the enemy is dead or the game is paused
                if (m_TargetEnemy.IsDead || Time.timeScale <= 0.01f)
                {
                    // Hide this health bar
                    SetBarVisibility(false);
                    
                    // For boss bars, also signal for removal
                    if (m_IsBossHealthBar)
                    {
                        EnemyHealthBarComponent component = m_TargetEnemy.GetComponent<EnemyHealthBarComponent>();
                        if (component != null)
                        {
                            EnemyHealthBarManager.Instance?.UnregisterBossHealthBar(component);
                        }
                    }
                    
                    yield return null;
                    continue;
                }
                
                // For world space bars only: face camera
                if (!m_IsBossHealthBar && m_MainCamera != null)
                {
                    transform.LookAt(transform.position + m_MainCamera.forward); 
                }
                
                // Check if health changed
                float currentHealthPercent = m_TargetEnemy.CurrentHealth / m_TargetEnemy.MaxHealth;
                
                if (Mathf.Abs(currentHealthPercent - m_ActualHealthPercent) > 0.001f)
                {
                    bool healthDecreased = currentHealthPercent < m_ActualHealthPercent;
                    bool healthIncreased = currentHealthPercent > m_ActualHealthPercent;
                    
                    float previousHealth = m_ActualHealthPercent;
                    m_ActualHealthPercent = currentHealthPercent;
                    
                    // Handle health change with dual fill system
                    if (Settings.enableDualFill && m_SecondaryFillImage != null)
                    {
                        if (healthDecreased)
                        {
                            // HEALTH DECREASED:
                            // - Primary fill animates down quickly
                            // - Secondary fill animates down with delay
                            
                            // Stop any existing primary fill animation
                            if (m_PrimaryFillCoroutine != null)
                                StopCoroutine(m_PrimaryFillCoroutine);
                                
                            m_PrimaryFillCoroutine = StartCoroutine(
                                AnimatePrimaryFillDown(previousHealth, currentHealthPercent)
                            );
                            
                            // Stop any existing secondary fill animation
                            if (m_SecondaryFillCoroutine != null)
                                StopCoroutine(m_SecondaryFillCoroutine);
                                
                            m_SecondaryFillCoroutine = StartCoroutine(
                                AnimateSecondaryFill(previousHealth, currentHealthPercent, true)
                            );
                        }
                        else if (healthIncreased)
                        {
                            // HEALTH INCREASED:
                            // - Secondary fill jumps up instantly
                            // - Primary fill animates up with delay
                            m_SecondaryFillPercent = currentHealthPercent;
                            
                            // Stop any existing primary fill animation
                            if (m_PrimaryFillCoroutine != null)
                                StopCoroutine(m_PrimaryFillCoroutine);
                                
                            m_PrimaryFillCoroutine = StartCoroutine(
                                AnimatePrimaryFill(previousHealth, currentHealthPercent)
                            );
                        }
                    }
                    else
                    {
                        // If dual fill is disabled, just animate primary
                        // Stop any existing primary fill animation
                        if (m_PrimaryFillCoroutine != null)
                            StopCoroutine(m_PrimaryFillCoroutine);
                            
                        m_PrimaryFillCoroutine = StartCoroutine(
                            healthDecreased ? 
                            AnimatePrimaryFillDown(previousHealth, currentHealthPercent) : 
                            AnimatePrimaryFill(previousHealth, currentHealthPercent)
                        );
                    }
                    
                    // Apply damage flash if enabled and health decreased
                    if (healthDecreased && Settings.enableDamageFlash)
                    {
                        if (m_DamageFlashCoroutine != null)
                            StopCoroutine(m_DamageFlashCoroutine);
                            
                        m_DamageFlashCoroutine = StartCoroutine(DamageFlashEffect());
                    }
                    
                    m_LastHealthPercent = currentHealthPercent;
                    
                    // Always update text immediately
                    UpdateHealthText();
                }
                
                // Check visibility conditions (for world-space bars only)
                if (!m_IsBossHealthBar)
                {
                    bool shouldBeVisible = ShouldBeVisible();
                    SetBarVisibility(shouldBeVisible);
                }
                
                yield return null;
            }
            
            // Return to pool when target is gone
            if (m_TargetEnemy == null || m_TargetEnemy.IsDead)
            {
                if (m_IsBossHealthBar)
                {
                    EnemyHealthBarComponent component = m_TargetEnemy?.GetComponent<EnemyHealthBarComponent>();
                    if (component != null)
                    {
                        EnemyHealthBarManager.Instance?.UnregisterBossHealthBar(component);
                    }
                }
                else
                {
                    FlyweightFactory.ReturnToPool(this);
                }
            }
        }
        
        /// <summary>
        /// Animate primary fill when health increases (slow animation with delay)
        /// </summary>
        private IEnumerator AnimatePrimaryFill(float fromHealth, float toHealth)
        {
            m_IsAnimatingPrimary = true;
            
            // Wait before starting the animation (delay)
            yield return new WaitForSeconds(Settings.secondaryFillDelay);
            
            float timer = 0f;
            float duration = Settings.secondaryFillDuration;
            
            while (timer < duration)
            {
                float t = timer / duration;
                t = Mathf.SmoothStep(0, 1, t); // Smooth the animation
                
                m_PrimaryFillPercent = Mathf.Lerp(fromHealth, toHealth, t);
                UpdateFillAmount(m_PrimaryFillPercent, m_SecondaryFillPercent);
                
                timer += Time.deltaTime;
                yield return null;
            }
            
            // Ensure we end exactly at the target value
            m_PrimaryFillPercent = toHealth;
            UpdateFillAmount(m_PrimaryFillPercent, m_SecondaryFillPercent);
            
            m_IsAnimatingPrimary = false;
            m_PrimaryFillCoroutine = null;
        }

        /// <summary>
        /// Animate primary fill when health decreases (quick animation)
        /// </summary>
        private IEnumerator AnimatePrimaryFillDown(float fromHealth, float toHealth)
        {
            m_IsAnimatingPrimary = true;
            
            float timer = 0f;
            float duration = m_PrimaryHealthDropDuration;
            
            while (timer < duration)
            {
                float t = timer / duration;
                t = Mathf.SmoothStep(0, 1, t); // Smooth the animation
                
                m_PrimaryFillPercent = Mathf.Lerp(fromHealth, toHealth, t);
                UpdateFillAmount(m_PrimaryFillPercent, m_SecondaryFillPercent);
                
                timer += Time.deltaTime;
                yield return null;
            }
            
            // Ensure we end exactly at the target value
            m_PrimaryFillPercent = toHealth;
            UpdateFillAmount(m_PrimaryFillPercent, m_SecondaryFillPercent);
            
            m_IsAnimatingPrimary = false;
            m_PrimaryFillCoroutine = null;
        }
        
        /// <summary>
        /// Animate secondary fill
        /// </summary>
        private IEnumerator AnimateSecondaryFill(float fromHealth, float toHealth, bool isDecrease)
        {
            m_IsAnimatingSecondary = true;
            
            // For health decrease, add a delay before the secondary bar starts moving
            if (isDecrease)
            {
                yield return new WaitForSeconds(Settings.secondaryFillDelay);
            }
            
            float timer = 0f;
            float duration = Settings.secondaryFillDuration;
            
            while (timer < duration)
            {
                float t = timer / duration;
                
                // Different easing for increase vs decrease
                if (isDecrease)
                {
                    t = Mathf.SmoothStep(0, 1, t); // Smooth step for decrease
                }
                else
                {
                    t = 1 - (1 - t) * (1 - t); // Ease out for increase
                }
                
                m_SecondaryFillPercent = Mathf.Lerp(fromHealth, toHealth, t);
                UpdateFillAmount(m_PrimaryFillPercent, m_SecondaryFillPercent);
                
                timer += Time.deltaTime;
                yield return null;
            }
            
            // Ensure we end exactly at the target value
            m_SecondaryFillPercent = toHealth;
            UpdateFillAmount(m_PrimaryFillPercent, m_SecondaryFillPercent);
            
            m_IsAnimatingSecondary = false;
            m_SecondaryFillCoroutine = null;
        }
        
        /// <summary>
        /// Flash effect when taking damage
        /// </summary>
        private IEnumerator DamageFlashEffect()
        {
            float duration = Settings.damageFlashDuration;
            float timer = duration;
            
            while (timer > 0)
            {
                if (m_FillImage != null)
                {
                    float flashIntensity = Mathf.PingPong(timer * 10f, 1f);
                    Color baseColor = GetHealthColor(m_PrimaryFillPercent);
                    m_FillImage.color = Color.Lerp(baseColor, Color.white, flashIntensity);
                }
                
                timer -= Time.deltaTime;
                yield return null;
            }
            
            // Reset to normal color
            if (m_FillImage != null)
            {
                m_FillImage.color = GetHealthColor(m_PrimaryFillPercent);
            }
            
            m_DamageFlashCoroutine = null;
        }
        #endregion
        
        #region UI Updates
        /// <summary>
        /// Update the fill amounts
        /// </summary>
        private void UpdateFillAmount(float primaryFill, float secondaryFill)
        {
            // Update primary fill
            if (m_FillImage != null)
            {
                m_FillImage.fillAmount = primaryFill;
                
                // Only change color if not flashing
                if (m_DamageFlashCoroutine == null)
                {
                    m_FillImage.color = GetHealthColor(primaryFill);
                }
            }
            
            // Update secondary fill if available
            if (m_SecondaryFillImage != null)
            {
                m_SecondaryFillImage.fillAmount = secondaryFill;
            }
        }
        
        /// <summary>
        /// Update health text
        /// </summary>
        private void UpdateHealthText()
        {
            if (m_TargetEnemy == null) return;
            
            string healthText = $"{Mathf.CeilToInt(m_TargetEnemy.CurrentHealth)}/{m_TargetEnemy.MaxHealth}";
            
            // Update legacy UI Text
            if (m_HealthText != null)
            {
                m_HealthText.text = healthText;
            }
            
            // Update TextMeshPro text if available (for boss bars)
            if (m_TMPHealthText != null)
            {
                m_TMPHealthText.text = healthText;
            }
        }
        
        /// <summary>
        /// Get the appropriate color based on health percentage
        /// </summary>
        private Color GetHealthColor(float healthPercent)
        {
            if (healthPercent > Settings.healthyThreshold)
                return Settings.healthyColor;
            else if (healthPercent > Settings.criticalThreshold)
                return Settings.damagedColor;
            else
                return Settings.criticalColor;
        }
        
        /// <summary>
        /// Set bar visibility
        /// </summary>
        private void SetBarVisibility(bool visible)
        {
            if (m_CanvasGroup != null)
            {
                m_CanvasGroup.alpha = visible ? 1f : 0f;
            }
            else
            {
                gameObject.SetActive(visible);
            }
        }
        
        /// <summary>
        /// Check if the health bar should be visible
        /// </summary>
        private bool ShouldBeVisible()
        {
            // Boss health bars are always visible when enemy is alive
            if (m_IsBossHealthBar)
                return m_TargetEnemy != null && !m_TargetEnemy.IsDead;
                
            // Don't show for dead enemies
            if (m_TargetEnemy == null || m_TargetEnemy.IsDead)
                return false;
                
            // Check full health hide option - only hide if not animating
            if (Settings.hideAtFullHealth && m_ActualHealthPercent >= 0.99f && 
                !m_IsAnimatingPrimary && !m_IsAnimatingSecondary)
                return false;
                
            // Check camera distance for world-space bars
            if (!m_IsBossHealthBar && m_MainCamera != null)
            {
                float maxDistance = Settings.maxVisibleDistance;
                
                float distanceToCamera = Vector3.Distance(m_MainCamera.position, m_TargetEnemy.transform.position);
                if (distanceToCamera > maxDistance)
                    return false;
            }
            
            return true;
        }
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Update health bar manually
        /// </summary>
        public void UpdateHealthBar(float healthPercent)
        {
            m_ActualHealthPercent = healthPercent;
            m_PrimaryFillPercent = healthPercent;
            m_SecondaryFillPercent = healthPercent;
            
            UpdateFillAmount(m_PrimaryFillPercent, m_SecondaryFillPercent);
            UpdateHealthText();
        }
        
        /// <summary>
        /// Update the health bar's position (for world-space bars)
        /// </summary>
        public void UpdatePosition()
        {
            if (m_IsBossHealthBar || m_TargetEnemy == null) return;
            
            transform.SetParent(m_TargetEnemy.transform);
            transform.localPosition = m_DisplayOffset;
        }
        #endregion
    }
} 