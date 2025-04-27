    
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace PowerTree
{
    /// <summary>
    /// Manages the player's hearts, stats, and power effects
    /// </summary>
    public class PlayerStats : MonoBehaviour
    {
        [Header("Health")] [SerializeField] private float maxRedHearts = 3f;
        [SerializeField] private float currentRedHearts = 3f;
        [SerializeField] private float currentBlueHearts = 0f;
        [SerializeField] private float currentBlackHearts = 0f;

        [SerializeField] private float damageMultiplier = 1f;
        [SerializeField] private float attackSpeedMultiplier = 1f;
        [SerializeField] private float moveSpeedMultiplier = 1f;
        [SerializeField] private float dodgeChance = 0f;

        [Header("Economy")] [SerializeField] private int gold = 0;
        [SerializeField] private float goldDropMultiplier = 1f;
        [SerializeField] private float shopDiscountPercent = 0f;

        [Header("Temporary Effects")] private List<TimedEffect> activeEffects = new List<TimedEffect>();

        // Events
        public UnityEvent<float> OnRedHeartLost = new UnityEvent<float>();
        public UnityEvent<float> OnBlueHeartLost = new UnityEvent<float>();
        public UnityEvent<float, Vector3> OnBlackHeartLost = new UnityEvent<float, Vector3>();
        public UnityEvent<float> OnRedHeartGained = new UnityEvent<float>();
        public UnityEvent<float> OnBlueHeartGained = new UnityEvent<float>();
        public UnityEvent<float> OnBlackHeartGained = new UnityEvent<float>();
        public UnityEvent<HeartType, float, float> OnHeartChanged = new UnityEvent<HeartType, float, float>();
        public UnityEvent OnPlayerDeath = new UnityEvent();
        public UnityEvent OnLevelCompleted = new UnityEvent();
        public UnityEvent<bool> OnLevelCompletedWithoutDamage = new UnityEvent<bool>();
        public UnityEvent OnDodgeSuccessful = new UnityEvent();
        
        public UnityEvent OnAttributesChanged = new UnityEvent();
        
        public bool IsDied => currentRedHearts <= 0f;

        // Configuration system for PowerNodes
        private Dictionary<string, object> nodeConfiguration = new Dictionary<string, object>();
        private Dictionary<string, Timer> nodeTimers = new Dictionary<string, Timer>();
        private Dictionary<string, Counter> nodeCounters = new Dictionary<string, Counter>();

        private bool damageTakenThisLevel = false;

        private void Start()
        {
            OnHeartChanged.Invoke(HeartType.Red, currentRedHearts, maxRedHearts);
            OnHeartChanged.Invoke(HeartType.Blue, currentBlueHearts, 0);
            OnHeartChanged.Invoke(HeartType.Black, currentBlackHearts, 0);
        }

        private void OnValidate()
        {
            OnHeartChanged.Invoke(HeartType.Red, currentRedHearts, maxRedHearts);
            OnHeartChanged.Invoke(HeartType.Blue, currentBlueHearts, 0);
            OnHeartChanged.Invoke(HeartType.Black, currentBlackHearts, 0);        }

        private void Update()
        {
            // Update timed effects
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                activeEffects[i].remainingTime -= Time.deltaTime;
                if (activeEffects[i].remainingTime <= 0)
                {
                    RemoveTimedEffect(i);
                }
            }

            // Update node timers
            foreach (var timer in nodeTimers.Values)
            {
                timer.Update(Time.deltaTime);
            }
        }

        #region Node Configuration

        // Register a configurable property for a node
        public void SetNodeProperty<T>(string nodeID, string propertyName, T value)
        {
            string key = $"{nodeID}_{propertyName}";
            nodeConfiguration[key] = value;
        }

        // Get a configurable property for a node
        public T GetNodeProperty<T>(string nodeID, string propertyName, T defaultValue)
        {
            string key = $"{nodeID}_{propertyName}";
            if (nodeConfiguration.TryGetValue(key, out object value) && value is T typedValue)
            {
                return typedValue;
            }

            return defaultValue;
        }

        // Get or create a timer for a node
        public Timer GetTimer(string nodeID, string timerName, float duration = 0)
        {
            string key = $"{nodeID}_{timerName}";
            if (!nodeTimers.TryGetValue(key, out Timer timer))
            {
                timer = new Timer(duration);
                nodeTimers[key] = timer;
            }

            return timer;
        }

        // Get or create a counter for a node
        public Counter GetCounter(string nodeID, string counterName)
        {
            string key = $"{nodeID}_{counterName}";
            if (!nodeCounters.TryGetValue(key, out Counter counter))
            {
                counter = new Counter();
                nodeCounters[key] = counter;
            }

            return counter;
        }

        #endregion

        #region Health Management

        public bool TakeDamage(float amount, Vector3 hitPosition)
        {
            if (UnityEngine.Random.value < dodgeChance)
            {
                OnDodgeSuccessful.Invoke();
                return false;
            }

            damageTakenThisLevel = true;

            // Damage calculation (typically 0.5 hearts per hit)
            // Priority: Black > Blue > Red hearts
            if (currentBlackHearts > 0)
            {
                float heartsTaken = Mathf.Min(amount, currentBlackHearts);
                currentBlackHearts -= heartsTaken;
                OnBlackHeartLost.Invoke(heartsTaken, hitPosition);
                OnHeartChanged.Invoke(HeartType.Black, currentBlackHearts, 0);

                if (amount > heartsTaken)
                {
                    // Remaining damage
                    return TakeDamage(amount - heartsTaken, hitPosition);
                }
            }
            else if (currentBlueHearts > 0)
            {
                float heartsTaken = Mathf.Min(amount, currentBlueHearts);
                currentBlueHearts -= heartsTaken;
                OnBlueHeartLost.Invoke(heartsTaken);
                OnHeartChanged.Invoke(HeartType.Blue, currentBlueHearts, 0);

                if (amount > heartsTaken)
                {
                    // Remaining damage
                    return TakeDamage(amount - heartsTaken, hitPosition);
                }
            }
            else
            {
                currentRedHearts -= amount;
                OnRedHeartLost.Invoke(amount);
                OnHeartChanged.Invoke(HeartType.Red, currentRedHearts, maxRedHearts);

                if (currentRedHearts <= 0)
                {
                    Die();
                    return true;
                }
            }

            return true;
        }

        public void AddHearts(HeartType type, float amount)
        {
            switch (type)
            {
                case HeartType.Red:
                    currentRedHearts = Mathf.Min(currentRedHearts + amount, maxRedHearts);
                    OnRedHeartGained.Invoke(amount);
                    OnHeartChanged.Invoke(type, currentRedHearts, maxRedHearts);
                    break;
                case HeartType.Blue:
                    currentBlueHearts += amount;
                    OnBlueHeartGained.Invoke(amount);
                    OnHeartChanged.Invoke(type, currentBlueHearts, 0);
                    break;
                case HeartType.Black:
                    currentBlackHearts += amount;
                    OnBlackHeartGained.Invoke(amount);
                    OnHeartChanged.Invoke(type, currentBlackHearts, 0);
                    break;
            }
        }

        private void Die()
        {
            OnPlayerDeath.Invoke();
        }

        public void ReviveWithHearts(float redHearts, float blueHearts)
        {
            currentRedHearts = Mathf.Min(redHearts, maxRedHearts);
            currentBlueHearts = blueHearts;
            // Implement revival logic
        }

        #endregion

        #region Stat Management
        
        public float GetDamageMultiplier()
        {
            return damageMultiplier;
        }

        public float GetAttackSpeed()
        {
            return attackSpeedMultiplier;
        }

        public float GetMoveSpeed()
        {
            return moveSpeedMultiplier;
        }

        public void ModifyDamageMultiplier(float amount, float duration = 0)
        {
            if (duration > 0)
            {
                AddTimedEffect(new TimedEffect
                {
                    effectType = EffectType.DamageMultiplier,
                    value = amount,
                    remainingTime = duration
                });
            }
            else
            {
                damageMultiplier += amount;
            }
            
            OnAttributesChanged.Invoke();

        }

        public void ModifyAttackSpeedMultiplier(float amount, float duration = 0, bool canStack = false)
        {
            if (duration > 0)
            {
                if (!canStack)
                {
                    // Remove any existing attack speed effects
                    activeEffects.RemoveAll(e => e.effectType == EffectType.AttackSpeedMultiplier);
                }

                AddTimedEffect(new TimedEffect
                {
                    effectType = EffectType.AttackSpeedMultiplier,
                    value = amount,
                    remainingTime = duration
                });
            }
            else
            {
                attackSpeedMultiplier += amount;
            }
            
            OnAttributesChanged.Invoke();
        }

        public void ModifyMoveSpeedMultiplier(float amount, float duration = 0)
        {
            if (duration > 0)
            {
                AddTimedEffect(new TimedEffect
                {
                    effectType = EffectType.MoveSpeedMultiplier,
                    value = amount,
                    remainingTime = duration
                });
            }
            else
            {
                moveSpeedMultiplier += amount;
            }
            
            OnAttributesChanged.Invoke();
        }

        public void ModifyDodgeChance(float amount, float duration = 0)
        {
            if (duration > 0)
            {
                AddTimedEffect(new TimedEffect
                {
                    effectType = EffectType.DodgeChance,
                    value = amount,
                    remainingTime = duration
                });
            }
            else
            {
                dodgeChance += amount;
            }
            
            OnAttributesChanged.Invoke();
        }

        #endregion

        #region Gold & Economy

        public void AddGold(int amount)
        {
            gold += amount;
            
            OnAttributesChanged.Invoke();
        }

        public bool SpendGold(int amount)
        {
            if (gold >= amount)
            {
                gold -= amount;
                return true;
            }
            
            OnAttributesChanged.Invoke();

            return false;
        }

        public int GetDiscountedPrice(int originalPrice)
        {
            return Mathf.RoundToInt(originalPrice * (1 - shopDiscountPercent / 100f));
        }

        public void ModifyShopDiscount(float percentAmount)
        {
            shopDiscountPercent += percentAmount;
            
            OnAttributesChanged.Invoke();
        }

        public void ModifyGoldDropMultiplier(float amount)
        {
            goldDropMultiplier += amount;
            
            OnAttributesChanged.Invoke();
        }

        #endregion

        #region Game Events

        public void OnLevelComplete()
        {
            OnLevelCompleted.Invoke();
            OnLevelCompletedWithoutDamage.Invoke(!damageTakenThisLevel);
            damageTakenThisLevel = false;
        }

        // Configurable health check methods
        public bool IsLowHealth(float threshold = 1f)
        {
            return currentRedHearts <= threshold;
        }

        public bool IsHighHealth(float threshold = 0.5f)
        {
            return currentRedHearts >= maxRedHearts * threshold;
        }

        // Get heart info
        public float GetCurrentRedHearts() => currentRedHearts;
        public float GetMaxRedHearts() => maxRedHearts;
        public float GetCurrentBlueHearts() => currentBlueHearts;
        public float GetCurrentBlackHearts() => currentBlackHearts;
        public float GetDodgeChance() => dodgeChance;
        public int GetGold() => gold;
        public float GetGoldDropMultiplier() => goldDropMultiplier;
        public float GetShopDiscountPercent() => shopDiscountPercent;

        #endregion

        #region Effect System

        private enum EffectType
        {
            DamageMultiplier,
            AttackSpeedMultiplier,
            MoveSpeedMultiplier,
            DodgeChance
        }

        private class TimedEffect
        {
            public EffectType effectType;
            public float value;
            public float remainingTime;
        }

        private void AddTimedEffect(TimedEffect effect)
        {
            activeEffects.Add(effect);

            // Apply the effect
            switch (effect.effectType)
            {
                case EffectType.DamageMultiplier:
                    damageMultiplier += effect.value;
                    break;
                case EffectType.AttackSpeedMultiplier:
                    attackSpeedMultiplier += effect.value;
                    break;
                case EffectType.MoveSpeedMultiplier:
                    moveSpeedMultiplier += effect.value;
                    break;
                case EffectType.DodgeChance:
                    dodgeChance += effect.value;
                    break;
            }
        }

        private void RemoveTimedEffect(int index)
        {
            var effect = activeEffects[index];

            // Remove the effect
            switch (effect.effectType)
            {
                case EffectType.DamageMultiplier:
                    damageMultiplier -= effect.value;
                    break;
                case EffectType.AttackSpeedMultiplier:
                    attackSpeedMultiplier -= effect.value;
                    break;
                case EffectType.MoveSpeedMultiplier:
                    moveSpeedMultiplier -= effect.value;
                    break;
                case EffectType.DodgeChance:
                    dodgeChance -= effect.value;
                    break;
            }

            activeEffects.RemoveAt(index);
            
            OnAttributesChanged.Invoke();
        }

        #endregion
    }
}