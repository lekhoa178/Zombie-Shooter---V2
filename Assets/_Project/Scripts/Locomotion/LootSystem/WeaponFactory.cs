using UnityEngine;

namespace LocomotionSystem.LootSystem
{
    /// <summary>
    /// Factory for creating weapon collectables in the game world
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponFactory", menuName = "Genshin Impact Movement System/Loot/Weapon Factory")]
    public class WeaponFactory : ScriptableObject
    {
        #region Private Fields
        [Header("Prefab Settings")]
        [SerializeField] private GameObject m_WeaponCollectablePrefab;
        
        [Header("Default Settings")]
        [SerializeField] private GameObject m_DefaultWeaponVisualPrefab;
        [SerializeField] private ComboConfiguration m_DefaultComboConfig;
        [SerializeField] private GameObject m_DefaultRarityVfxPrefab;
        [SerializeField] private GameObject m_DefaultCollectionVfxPrefab;
        #endregion

        #region Public Methods
        /// <summary>
        /// Creates a weapon collectable at the specified position
        /// </summary>
        /// <param name="_position">World position to spawn the weapon</param>
        /// <param name="_weaponConfig">Weapon's combo configuration</param>
        /// <param name="_weaponVisual">Visual prefab for the weapon</param>
        /// <param name="_type">Weapon type</param>
        /// <param name="_rarity">Weapon rarity</param>
        /// <returns>The created weapon collectable GameObject</returns>
        public GameObject CreateWeaponCollectable(
            Vector3 _position,
            ComboConfiguration _weaponConfig = null,
            GameObject _weaponVisual = null,
            WeaponType _type = WeaponType.Sword,
            WeaponRarity _rarity = WeaponRarity.Common)
        {
            // Use defaults if not specified
            ComboConfiguration weaponConfig = _weaponConfig ?? m_DefaultComboConfig;
            GameObject weaponVisual = _weaponVisual ?? m_DefaultWeaponVisualPrefab;
            
            // Create the collectable
            GameObject collectableObj = Instantiate(m_WeaponCollectablePrefab, _position, Quaternion.identity);
            
            // Get the weapon collectable component
            WeaponCollectable collectable = collectableObj.GetComponent<WeaponCollectable>();
            
            if (collectable != null)
            {
                // Set weapon properties
                SetWeaponProperties(collectable, weaponConfig, weaponVisual, _type, _rarity);
                
                // Set name
                collectableObj.name = $"{_rarity} {_type}";
            }
            
            return collectableObj;
        }
        
        /// <summary>
        /// Creates a random weapon collectable at the specified position
        /// </summary>
        /// <param name="_position">World position to spawn the weapon</param>
        /// <param name="_minRarity">Minimum rarity tier</param>
        /// <param name="_maxRarity">Maximum rarity tier</param>
        /// <returns>The created weapon collectable GameObject</returns>
        public GameObject CreateRandomWeaponCollectable(
            Vector3 _position,
            WeaponRarity _minRarity = WeaponRarity.Common,
            WeaponRarity _maxRarity = WeaponRarity.Legendary)
        {
            // Randomly determine weapon type and rarity
            WeaponType type = (WeaponType)Random.Range(0, System.Enum.GetValues(typeof(WeaponType)).Length);
            
            // Ensure min/max rarity are valid
            int minRarityIndex = Mathf.Min((int)_minRarity, (int)WeaponRarity.Legendary);
            int maxRarityIndex = Mathf.Min((int)_maxRarity, (int)WeaponRarity.Legendary);
            minRarityIndex = Mathf.Max(minRarityIndex, (int)WeaponRarity.Common);
            maxRarityIndex = Mathf.Max(maxRarityIndex, minRarityIndex);
            
            // Calculate rarity with weighted distribution (rarer weapons are less common)
            float rarityRoll = Mathf.Pow(Random.value, 1.5f); // Power curve to make higher rarities less common
            int rarityIndex = Mathf.FloorToInt(Mathf.Lerp(minRarityIndex, maxRarityIndex + 1, rarityRoll));
            rarityIndex = Mathf.Clamp(rarityIndex, minRarityIndex, maxRarityIndex);
            WeaponRarity rarity = (WeaponRarity)rarityIndex;
            
            // Create and return the weapon
            return CreateWeaponCollectable(_position, null, null, type, rarity);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Sets the properties of a weapon collectable
        /// </summary>
        private void SetWeaponProperties(
            WeaponCollectable _collectable,
            ComboConfiguration _weaponConfig,
            GameObject _weaponVisual,
            WeaponType _type,
            WeaponRarity _rarity)
        {
            // Use reflection to set private fields
            var weaponConfigField = typeof(WeaponCollectable).GetField("m_WeaponConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var weaponVisualField = typeof(WeaponCollectable).GetField("m_WeaponVisualPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var rarityField = typeof(WeaponCollectable).GetField("m_Rarity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var typeField = typeof(WeaponCollectable).GetField("m_Type", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var rarityVfxField = typeof(WeaponCollectable).GetField("m_RarityVfxPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var collectionVfxField = typeof(WeaponCollectable).GetField("m_CollectionVfxPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (weaponConfigField != null)
            {
                weaponConfigField.SetValue(_collectable, _weaponConfig);
            }
            
            if (weaponVisualField != null)
            {
                weaponVisualField.SetValue(_collectable, _weaponVisual);
            }
            
            if (rarityField != null)
            {
                rarityField.SetValue(_collectable, _rarity);
            }
            
            if (typeField != null)
            {
                typeField.SetValue(_collectable, _type);
            }
            
            if (rarityVfxField != null)
            {
                rarityVfxField.SetValue(_collectable, m_DefaultRarityVfxPrefab);
            }
            
            if (collectionVfxField != null)
            {
                collectionVfxField.SetValue(_collectable, m_DefaultCollectionVfxPrefab);
            }
            
            // Set name field as "{Rarity} {Type}"
            var nameField = typeof(CollectableBase).GetField("m_Name", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (nameField != null)
            {
                string weaponName = $"{_rarity} {_type}";
                nameField.SetValue(_collectable, weaponName);
            }
        }
        #endregion
    }
} 