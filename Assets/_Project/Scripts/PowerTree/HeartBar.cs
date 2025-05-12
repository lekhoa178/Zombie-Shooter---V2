using System;
using System.Collections.Generic;
using UnityEngine;

namespace PowerTree
{
    public class HeartBar : MonoBehaviour
    {
        [SerializeField] private Transform heartContainer;

        [SerializeField] private GameObject redHeartPrefab;
        [SerializeField] private GameObject blueHeartPrefab;
        [SerializeField] private GameObject blackHeartPrefab;

        private Dictionary<HeartType, GameObject> _heartPrefabs;
        private Dictionary<HeartType, float> _heartValues;

        private PlayerStats _playerStats;

        private void Awake()
        {
            _playerStats = PlayerStats.Instance;

            if (heartContainer == null)
            {
                heartContainer = transform;
            }

            _heartPrefabs = new Dictionary<HeartType, GameObject>
            {
                { HeartType.Red, redHeartPrefab },
                { HeartType.Blue, blueHeartPrefab },
                { HeartType.Black, blackHeartPrefab }
            };

            _heartValues = new Dictionary<HeartType, float>
            {
                { HeartType.Red, 0 },
                { HeartType.Blue, 0 },
                { HeartType.Black, 0 }
            };

            foreach (Transform child in heartContainer)
            {
                Destroy(child.gameObject);
            }
            
            UpdateHeart(HeartType.Red, PlayerStats.Instance.GetCurrentRedHearts(), PlayerStats.Instance.GetMaxRedHearts());
            UpdateHeart(HeartType.Blue, PlayerStats.Instance.GetCurrentBlueHearts(), -1);
            UpdateHeart(HeartType.Black, PlayerStats.Instance.GetCurrentBlackHearts(), -1);
            
            _playerStats.OnHeartChanged.AddListener(UpdateHeart);
        }

        private void OnDestroy()
        {
            _playerStats.OnHeartChanged.RemoveListener(UpdateHeart);
        }

        private void UpdateHeart(HeartType heartType, float amount, float maxAmount)
        {
            if (!_heartValues.TryGetValue(heartType, out var previousAmount))
                return;

            // Ensure amount doesn't exceed maxAmount if maxAmount is set
            if (maxAmount > 0)
                amount = Mathf.Min(amount, maxAmount);

            _heartValues[heartType] = amount;

            // Calculate how many hearts we need (full hearts + potentially one partial heart)
            int neededHearts = maxAmount > 0 ? Mathf.CeilToInt(maxAmount) : Mathf.CeilToInt(amount);

            // Get all existing hearts of this type, excluding those being destroyed
            List<Heart> existingHearts = new List<Heart>();
            List<Heart> existingIncludeEmptyHearts = new List<Heart>();
            foreach (Transform child in heartContainer)
            {
                Heart heart = child.GetComponent<Heart>();
                // Skip hearts that are being destroyed
                if (heart != null && !heart.IsBeingDestroyed && heart.gameObject.name.StartsWith(heartType.ToString()))
                {
                    if (!heart.IsEmpty)
                    {
                        existingHearts.Add(heart);
                    }
            
                    existingIncludeEmptyHearts.Add(heart);
                }
            }

            // If we need more hearts than we have, create new ones
            while (existingIncludeEmptyHearts.Count < neededHearts)
            {
                GameObject heartPrefab = _heartPrefabs[heartType];
                GameObject heartInstance = Instantiate(heartPrefab, heartContainer);
                heartInstance.gameObject.name = $"{heartType}Heart_{existingHearts.Count}";
                Heart heartComponent = heartInstance.GetComponent<Heart>();
                existingHearts.Add(heartComponent);
                existingIncludeEmptyHearts.Add(heartComponent);
            }

            // If we have too many hearts, handle them based on maxAmount
            while (existingHearts.Count > neededHearts && existingHearts.Count > 0)
            {
                int lastIndex = existingHearts.Count - 1;
                Heart heartToRemove = existingHearts[lastIndex];

                // For hearts with maxAmount, don't destroy but set to zero
                if (maxAmount > 0)
                {
                    heartToRemove.SetHeartAmount(0);
                }
                else
                {
                    heartToRemove.DestroyWithAnimation();
                }
                
                existingHearts.RemoveAt(lastIndex);
            }

            // Set the appropriate fill amount for each heart
            float remainingAmount = amount;
            for (int i = 0; i < existingIncludeEmptyHearts.Count; i++)
            {
                float fillAmount;

                if (remainingAmount >= 1f)
                {
                    fillAmount = 1f;
                    remainingAmount -= 1f;
                }
                else
                {
                    fillAmount = remainingAmount;
                    remainingAmount = 0f;
                }

                existingIncludeEmptyHearts[i].SetHeartAmount(fillAmount);
            }

            // Reorder hearts if any were added or removed
            if (!Mathf.Approximately(previousAmount, amount))
            {
                ReorderHearts();
            }
        }

        private void ReorderHearts()
        {
            List<(Heart heart, HeartType type, float amount)> heartInfos = new List<(Heart, HeartType, float)>();

            foreach (Transform child in heartContainer)
            {
                Heart heart = child.GetComponent<Heart>();
                if (heart != null && !heart.IsBeingDestroyed)
                {
                    HeartType type = DetermineHeartTypeFromName(heart.gameObject.name);
                    float amount = _heartValues.TryGetValue(type, out var value) ? value : 0;
                    heartInfos.Add((heart, type, amount));
                }
            }

            // Sort by heart type (Red → Blue → Black) and then by amount (descending)
            heartInfos.Sort((a, b) =>
            {
                // First sort by type priority: Red (0), Blue (1), Black (2)
                int typePriorityA = GetTypePriority(a.type);
                int typePriorityB = GetTypePriority(b.type);

                if (typePriorityA != typePriorityB)
                    return typePriorityA.CompareTo(typePriorityB);

                // Then sort by amount (descending)
                return b.amount.CompareTo(a.amount);
            });

            // Set sibling indices to achieve proper ordering
            for (int i = 0; i < heartInfos.Count; i++)
            {
                heartInfos[i].heart.transform.SetSiblingIndex(i);
            }
        }

        private HeartType DetermineHeartTypeFromName(string name)
        {
            if (name.StartsWith(HeartType.Red.ToString()))
                return HeartType.Red;
            if (name.StartsWith(HeartType.Blue.ToString()))
                return HeartType.Blue;
            if (name.StartsWith(HeartType.Black.ToString()))
                return HeartType.Black;

            return HeartType.Red; // Default fallback
        }

        private int GetTypePriority(HeartType type)
        {
            switch (type)
            {
                case HeartType.Red: return 0;
                case HeartType.Blue: return 1;
                case HeartType.Black: return 2;
                default: return 3;
            }
        }
    }
}