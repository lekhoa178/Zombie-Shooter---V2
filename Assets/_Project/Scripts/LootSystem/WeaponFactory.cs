using System;
using Locomotion.LootSystem;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LocomotionSystem
{
    /// <summary>
    /// Factory for creating weapon collectables in the game world
    /// </summary>
    public class WeaponFactory : MonoBehaviour
    {
        #region Singleton
        public static WeaponFactory Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
        #endregion
        
        #region Private Fields
        [Header("Default Settings")]
        [SerializeField] private GameObject m_DefaultWeaponPrefab;
        [SerializeField] private ComboConfiguration m_DefaultComboConfig;
        [SerializeField] private GameObject m_DefaultRarityVfxPrefab;
        [SerializeField] private GameObject m_DefaultCollectionVfxPrefab;
        
        [Header("Weapon Configuration Mappings")]
        [Tooltip("Map each combo configuration directly to its corresponding prefab")]
        [SerializeField] private WeaponConfigMapping[] m_WeaponMappings;
        
        [System.Serializable]
        private struct WeaponConfigMapping
        {
            public ComboConfiguration ComboConfig;
            public GameObject WeaponPrefab;
            public WeaponType WeaponType;
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Creates a weapon interactable at the specified position using a combo configuration
        /// </summary>
        /// <param name="position">World position to spawn the weapon</param>
        /// <param name="comboConfig">The weapon's combo configuration</param>
        /// <param name="rarity">Weapon rarity</param>
        /// <returns>The created weapon interactable GameObject</returns>
        public GameObject CreateWeaponCollectable(
            Vector3 position,
            ComboConfiguration comboConfig,
            WeaponRarity rarity = WeaponRarity.Common)
        {
            if (comboConfig == null)
            {
                Debug.LogWarning("No combo configuration provided. Using default config.");
                comboConfig = m_DefaultComboConfig;
            }
            
            // Get the mapping for this combo configuration
            var mapping = GetMappingForComboConfig(comboConfig);
            
            // Get the weapon prefab from the mapping
            var prefab = mapping.WeaponPrefab;
            if (prefab == null)
            {
                Debug.LogWarning($"No prefab found for the specified combo configuration. Using default prefab.");
                prefab = m_DefaultWeaponPrefab;
            }
            
            // Get the weapon type from the mapping
            var weaponType = mapping.WeaponType;

            // Instantiate the weapon prefab
            var collectableObj = Instantiate(prefab, position, Quaternion.identity);
            
            // Get the weapon interactable component
            var collectable = collectableObj.GetComponent<WeaponInteractable>();
            
            if (collectable)
            {
                // Set weapon properties
                SetWeaponProperties(collectable, comboConfig, weaponType, rarity);
                
                // Set name
                collectableObj.name = $"{rarity} {weaponType}";
            }
            else
            {
                Debug.LogError($"Prefab for the specified combo configuration does not have a WeaponInteractable component!");
            }
            
            return collectableObj;
        }
        
        /// <summary>
        /// Creates a weapon interactable at the specified position using a weapon type
        /// </summary>
        /// <param name="position">World position to spawn the weapon</param>
        /// <param name="type">Weapon type</param>
        /// <param name="rarity">Weapon rarity</param>
        /// <returns>The created weapon interactable GameObject</returns>
        public GameObject CreateWeaponCollectableByType(
            Vector3 position,
            WeaponType type = WeaponType.Sword,
            WeaponRarity rarity = WeaponRarity.Common)
        {
            // Find a combo configuration for this weapon type
            ComboConfiguration config = GetComboConfigForWeaponType(type);
            
            // Create the weapon using the found combo config
            return CreateWeaponCollectable(position, config, rarity);
        }
        
        /// <summary>
        /// Creates a random weapon interactable at the specified position
        /// </summary>
        /// <param name="position">World position to spawn the weapon</param>
        /// <param name="minRarity">Minimum rarity tier</param>
        /// <param name="maxRarity">Maximum rarity tier</param>
        /// <returns>The created weapon interactable GameObject</returns>
        public GameObject CreateRandomWeaponCollectable(
            Vector3 position,
            WeaponRarity minRarity = WeaponRarity.Common,
            WeaponRarity maxRarity = WeaponRarity.Legendary)
        {
            // Get a random combo configuration from our mappings
            ComboConfiguration randomConfig = GetRandomComboConfig();
            
            // Ensure min/max rarity are valid
            var minRarityIndex = Mathf.Min((int)minRarity, (int)WeaponRarity.Legendary);
            var maxRarityIndex = Mathf.Min((int)maxRarity, (int)WeaponRarity.Legendary);
            minRarityIndex = Mathf.Max(minRarityIndex, (int)WeaponRarity.Common);
            maxRarityIndex = Mathf.Max(maxRarityIndex, minRarityIndex);
            
            // Calculate rarity with weighted distribution (rarer weapons are less common)
            var rarityRoll = Mathf.Pow(Random.value, 1.5f); // Power curve to make higher rarities less common
            var rarityIndex = Mathf.FloorToInt(Mathf.Lerp(minRarityIndex, maxRarityIndex + 1, rarityRoll));
            rarityIndex = Mathf.Clamp(rarityIndex, minRarityIndex, maxRarityIndex);
            WeaponRarity rarity = (WeaponRarity)rarityIndex;
            
            // Create and return the weapon
            return CreateWeaponCollectable(position, randomConfig, rarity);
        }
        
        /// <summary>
        /// Get a combo configuration for a specific weapon type
        /// </summary>
        private ComboConfiguration GetComboConfigForWeaponType(WeaponType type)
        {
            if (m_WeaponMappings != null)
            {
                foreach (var mapping in m_WeaponMappings)
                {
                    if (mapping.WeaponType == type && mapping.ComboConfig != null)
                    {
                        return mapping.ComboConfig;
                    }
                }
            }
            
            // If no configuration found for this type, return default
            return m_DefaultComboConfig;
        }
        
        /// <summary>
        /// Get a random combo configuration from available mappings
        /// </summary>
        private ComboConfiguration GetRandomComboConfig()
        {
            if (m_WeaponMappings != null && m_WeaponMappings.Length > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, m_WeaponMappings.Length);
                return m_WeaponMappings[randomIndex].ComboConfig ?? m_DefaultComboConfig;
            }
            
            return m_DefaultComboConfig;
        }
        
        /// <summary>
        /// Get the mapping for a specific combo configuration
        /// </summary>
        private WeaponConfigMapping GetMappingForComboConfig(ComboConfiguration comboConfig)
        {
            if (m_WeaponMappings != null)
            {
                foreach (var mapping in m_WeaponMappings)
                {
                    if (mapping.ComboConfig == comboConfig)
                    {
                        return mapping;
                    }
                }
            }
            
            // If no mapping found, create a default one
            return new WeaponConfigMapping
            {
                ComboConfig = comboConfig,
                WeaponPrefab = m_DefaultWeaponPrefab,
                WeaponType = WeaponType.Sword // Default type
            };
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Sets the properties of a weapon interactable
        /// </summary>
        private void SetWeaponProperties(
            WeaponInteractable interactable,
            ComboConfiguration weaponConfig,
            WeaponType type,
            WeaponRarity rarity)
        {
            // Use reflection to set private fields
            var weaponConfigField = typeof(WeaponInteractable).GetField("m_WeaponConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var rarityField = typeof(WeaponInteractable).GetField("m_Rarity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var typeField = typeof(WeaponInteractable).GetField("m_WeaponType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var rarityVfxField = typeof(WeaponInteractable).GetField("m_RarityVfxPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var collectionVfxField = typeof(WeaponInteractable).GetField("m_CollectionVfxPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (weaponConfigField != null)
            {
                weaponConfigField.SetValue(interactable, weaponConfig);
            }
            
            if (rarityField != null)
            {
                rarityField.SetValue(interactable, rarity);
            }
            
            if (typeField != null)
            {
                typeField.SetValue(interactable, type);
            }
            
            if (rarityVfxField != null)
            {
                rarityVfxField.SetValue(interactable, m_DefaultRarityVfxPrefab);
            }
            
            if (collectionVfxField != null)
            {
                collectionVfxField.SetValue(interactable, m_DefaultCollectionVfxPrefab);
            }
            
            // Set name field as "{Rarity} {Type}"
            var nameField = typeof(CollectableBase).GetField("m_ItemName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (nameField != null)
            {
                var weaponName = $"{rarity} {type}";
                nameField.SetValue(interactable, weaponName);
            }
            
            // Directly update the weapon data object
            var weaponDataField = typeof(WeaponInteractable).GetField("m_WeaponData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (weaponDataField == null) return;
            
            var weaponData = new WeaponCollectableData
            {
                Config = weaponConfig,
                Type = type,
                Rarity = rarity
            };
                
            weaponDataField.SetValue(interactable, weaponData);
        }
        #endregion
    }
} 