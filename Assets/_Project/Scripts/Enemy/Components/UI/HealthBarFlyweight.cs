using Flyweight;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Flyweight
{
    public class HealthBarFlyweight : Flyweight
    {
        // References to the UI components
        private Image _backgroundImage;
        private Image _fillImage;
        private Image _secondaryFillImage;
        private Text _healthText;
        private CanvasGroup _canvasGroup;
        
        // Reference to the associated enemy
        private BaseEnemy _targetEnemy;
        private Transform _mainCamera;
        private float _lastHealthPercent = 1f;
        private float _currentFillAmount = 1f;
        private Coroutine _updateCoroutine;
        private Coroutine _damageFlashCoroutine;
        private Coroutine _delayFillCoroutine;
        private Coroutine _anticipateFillCoroutine;
        private Coroutine _primaryFillCoroutine;
        private Coroutine _secondaryFillCoroutine;
        private Vector3 _displayOffset = Vector3.up * 2f;
        private bool _isInitialized = false;
        
        // New fields for tracking health changes
        private float _actualHealthPercent = 1f;    // The current real health
        private float _primaryFillPercent = 1f;       // Currently displayed primary fill
        private float _secondaryFillPercent = 1f;     // Currently displayed secondary fill
        private bool _isAnimatingPrimary = false;
        private bool _isAnimatingSecondary = false;
        
        // Get settings from our FlyweightSettings
        private HealthBarFlyweightSettings Settings => settings as HealthBarFlyweightSettings;
        
        // Used by HealthBarFlyweightSettings to set up components from prefab
        public void InitializeComponents(Image background, Image fill, Image secondaryFill, Text text, CanvasGroup group)
        {
            _backgroundImage = background;
            _fillImage = fill;
            _secondaryFillImage = secondaryFill;
            _healthText = text;
            _canvasGroup = group;
            
            // Set up the secondary fill if available
            if (_secondaryFillImage != null)
            {
                _secondaryFillImage.color = Settings.secondaryFillColor;
                
                // Ensure the secondary fill is behind the primary fill in render order
                _secondaryFillImage.transform.SetAsFirstSibling();
                if (_fillImage != null)
                {
                    _fillImage.transform.SetAsLastSibling();
                }
            }
        }
        
        public void Initialize(BaseEnemy enemy, Vector3 offset)
        {
            _targetEnemy = enemy;
            _mainCamera = Camera.main.transform;
            _displayOffset = offset;
            _isInitialized = true;
            
            transform.SetParent(_targetEnemy.transform);
            transform.localPosition = _displayOffset;
            
            // Setup initial state
            _actualHealthPercent = _targetEnemy.CurrentHealth / _targetEnemy.MaxHealth;
            _primaryFillPercent = _actualHealthPercent;
            _secondaryFillPercent = _actualHealthPercent;
            
            UpdateFillAmount(_primaryFillPercent, _secondaryFillPercent);
            
            // Stop any existing update routine
            if (_updateCoroutine != null)
                StopCoroutine(_updateCoroutine);
                
            // Start update routine
            _updateCoroutine = StartCoroutine(UpdateHealthBarRoutine());
        }
        
        public void ResetState()
        {
            // Reset internal state when retrieved from pool
            _lastHealthPercent = 1f;
            _currentFillAmount = 1f;
            _actualHealthPercent = 1f;
            _primaryFillPercent = 1f;
            _secondaryFillPercent = 1f;
            _isInitialized = false;
            _isAnimatingPrimary = false;
            _isAnimatingSecondary = false;
            
            // Reset visuals
            if (_fillImage != null)
            {
                _fillImage.fillAmount = 1f;
                _fillImage.color = Settings.healthyColor;
            }
            
            if (_secondaryFillImage != null)
            {
                _secondaryFillImage.fillAmount = 1f;
                _secondaryFillImage.color = Settings.secondaryFillColor;
            }
            
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }
        }
        
        public void Cleanup()
        {
            // Stop all coroutines
            if (_updateCoroutine != null)
                StopCoroutine(_updateCoroutine);
                
            if (_damageFlashCoroutine != null)
                StopCoroutine(_damageFlashCoroutine);
                
            if (_delayFillCoroutine != null)
                StopCoroutine(_delayFillCoroutine);
                
            if (_anticipateFillCoroutine != null)
                StopCoroutine(_anticipateFillCoroutine);
                
            if (_primaryFillCoroutine != null)
                StopCoroutine(_primaryFillCoroutine);
                
            if (_secondaryFillCoroutine != null)
                StopCoroutine(_secondaryFillCoroutine);
                
            _targetEnemy = null;
            _isInitialized = false;
        }
        
        private IEnumerator UpdateHealthBarRoutine()
        {
            while (_isInitialized && _targetEnemy != null)
            {
                // Skip updates if the enemy is dead or the game is paused
                if (_targetEnemy.IsDead || Time.timeScale <= 0.01f)
                {
                    SetBarVisibility(false);
                    yield return null;
                    continue;
                }
                
                // Always face camera
                if (_mainCamera != null)
                {
                    transform.LookAt(transform.position + _mainCamera.forward); 
                }
                
                // Check if health changed
                float currentHealthPercent = _targetEnemy.CurrentHealth / _targetEnemy.MaxHealth;
                
                if (Mathf.Abs(currentHealthPercent - _actualHealthPercent) > 0.001f)
                {
                    bool healthDecreased = currentHealthPercent < _actualHealthPercent;
                    bool healthIncreased = currentHealthPercent > _actualHealthPercent;
                    
                    float previousHealth = _actualHealthPercent;
                    _actualHealthPercent = currentHealthPercent;
                    
                    // Handle health change with dual fill system
                    if (Settings.enableDualFill && _secondaryFillImage != null)
                    {
                        if (healthDecreased)
                        {
                            // HEALTH DECREASED:
                            // - Primary fill drops instantly
                            // - Secondary fill animates down with delay
                            _primaryFillPercent = currentHealthPercent;
                            
                            // Stop any existing secondary fill animation
                            if (_secondaryFillCoroutine != null)
                                StopCoroutine(_secondaryFillCoroutine);
                                
                            _secondaryFillCoroutine = StartCoroutine(
                                AnimateSecondaryFill(previousHealth, currentHealthPercent, true)
                            );
                        }
                        else if (healthIncreased)
                        {
                            // HEALTH INCREASED:
                            // - Secondary fill jumps up instantly
                            // - Primary fill animates up with delay
                            _secondaryFillPercent = currentHealthPercent;
                            
                            // Stop any existing primary fill animation
                            if (_primaryFillCoroutine != null)
                                StopCoroutine(_primaryFillCoroutine);
                                
                            _primaryFillCoroutine = StartCoroutine(
                                AnimatePrimaryFill(previousHealth, currentHealthPercent)
                            );
                        }
                    }
                    else
                    {
                        // If dual fill is disabled, just update normally
                        _primaryFillPercent = currentHealthPercent;
                        _secondaryFillPercent = currentHealthPercent;
                    }
                    
                    // Apply damage flash if enabled and health decreased
                    if (healthDecreased && Settings.enableDamageFlash)
                    {
                        if (_damageFlashCoroutine != null)
                            StopCoroutine(_damageFlashCoroutine);
                            
                        _damageFlashCoroutine = StartCoroutine(DamageFlashEffect());
                    }
                    
                    // Update fill instantly for simple mode
                    if (!Settings.enableDualFill || _secondaryFillImage == null)
                    {
                        UpdateFillAmount(_primaryFillPercent, _secondaryFillPercent);
                    }
                    else
                    {
                        // Update fills to current values for dual fill mode
                        UpdateFillAmount(_primaryFillPercent, _secondaryFillPercent);
                    }
                    
                    _lastHealthPercent = currentHealthPercent;
                    
                    // Always update text immediately
                    UpdateHealthText();
                }
                
                // Check visibility conditions
                bool shouldBeVisible = ShouldBeVisible();
                SetBarVisibility(shouldBeVisible);
                
                yield return null;
            }
            
            // Return to pool when target is gone
            if (_targetEnemy == null || _targetEnemy.IsDead)
            {
                FlyweightFactory.ReturnToPool(this);
            }
        }
        
        private IEnumerator AnimatePrimaryFill(float fromHealth, float toHealth)
        {
            _isAnimatingPrimary = true;
            
            // Wait before starting the animation (delay)
            yield return new WaitForSeconds(Settings.secondaryFillDelay);
            
            float timer = 0f;
            float duration = Settings.secondaryFillDuration;
            
            while (timer < duration)
            {
                float t = timer / duration;
                t = Mathf.SmoothStep(0, 1, t); // Smooth the animation
                
                _primaryFillPercent = Mathf.Lerp(fromHealth, toHealth, t);
                UpdateFillAmount(_primaryFillPercent, _secondaryFillPercent);
                
                timer += Time.deltaTime;
                yield return null;
            }
            
            // Ensure we end exactly at the target value
            _primaryFillPercent = toHealth;
            UpdateFillAmount(_primaryFillPercent, _secondaryFillPercent);
            
            _isAnimatingPrimary = false;
            _primaryFillCoroutine = null;
        }
        
        private IEnumerator AnimateSecondaryFill(float fromHealth, float toHealth, bool isDecrease)
        {
            _isAnimatingSecondary = true;
            
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
                
                _secondaryFillPercent = Mathf.Lerp(fromHealth, toHealth, t);
                UpdateFillAmount(_primaryFillPercent, _secondaryFillPercent);
                
                timer += Time.deltaTime;
                yield return null;
            }
            
            // Ensure we end exactly at the target value
            _secondaryFillPercent = toHealth;
            UpdateFillAmount(_primaryFillPercent, _secondaryFillPercent);
            
            _isAnimatingSecondary = false;
            _secondaryFillCoroutine = null;
        }
        
        private void UpdateFillAmount(float primaryFill, float secondaryFill)
        {
            // Update primary fill
            if (_fillImage != null)
            {
                _fillImage.fillAmount = primaryFill;
                
                // Only change color if not flashing
                if (_damageFlashCoroutine == null)
                {
                    _fillImage.color = GetHealthColor(primaryFill);
                }
            }
            
            // Update secondary fill if available
            if (_secondaryFillImage != null)
            {
                _secondaryFillImage.fillAmount = secondaryFill;
            }
        }
        
        private void UpdateHealthText()
        {
            if (_healthText != null)
            {
                _healthText.text = $"{Mathf.CeilToInt(_targetEnemy.CurrentHealth)}/{_targetEnemy.MaxHealth}";
            }
        }
        
        private bool ShouldBeVisible()
        {
            // Don't show for dead enemies
            if (_targetEnemy == null || _targetEnemy.IsDead)
                return false;
                
            // Check full health hide option - only hide if not animating
            if (Settings.hideAtFullHealth && _actualHealthPercent >= 0.99f && 
                !_isAnimatingPrimary && !_isAnimatingSecondary)
                return false;
                
            // Check camera distance
            if (_mainCamera != null)
            {
                float maxDistance = Settings.maxVisibleDistance;
                
                float distanceToCamera = Vector3.Distance(_mainCamera.position, _targetEnemy.transform.position);
                if (distanceToCamera > maxDistance)
                    return false;
            }
            
            return true;
        }
        
        private IEnumerator DamageFlashEffect()
        {
            float duration = Settings.damageFlashDuration;
            float timer = duration;
            
            while (timer > 0)
            {
                if (_fillImage != null)
                {
                    float flashIntensity = Mathf.PingPong(timer * 10f, 1f);
                    Color baseColor = GetHealthColor(_primaryFillPercent);
                    _fillImage.color = Color.Lerp(baseColor, Color.white, flashIntensity);
                }
                
                timer -= Time.deltaTime;
                yield return null;
            }
            
            // Reset to normal color
            if (_fillImage != null)
            {
                _fillImage.color = GetHealthColor(_primaryFillPercent);
            }
            
            _damageFlashCoroutine = null;
        }
        
        private Color GetHealthColor(float healthPercent)
        {
            if (healthPercent > Settings.healthyThreshold)
                return Settings.healthyColor;
            else if (healthPercent > Settings.criticalThreshold)
                return Settings.damagedColor;
            else
                return Settings.criticalColor;
        }
        
        private void SetBarVisibility(bool visible)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = visible ? 1f : 0f;
            }
            else
            {
                gameObject.SetActive(visible);
            }
        }
        
        public void UpdateHealthBar(float healthPercent)
        {
            _actualHealthPercent = healthPercent;
            _primaryFillPercent = healthPercent;
            _secondaryFillPercent = healthPercent;
            
            UpdateFillAmount(_primaryFillPercent, _secondaryFillPercent);
            UpdateHealthText();
        }
    }
} 