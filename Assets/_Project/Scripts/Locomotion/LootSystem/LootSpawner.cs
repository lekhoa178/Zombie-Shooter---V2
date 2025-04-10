using UnityEngine;

namespace LocomotionSystem.LootSystem
{
    /// <summary>
    /// Component to spawn loot items when triggered, such as when an enemy is defeated
    /// </summary>
    public class LootSpawner : MonoBehaviour
    {
        #region Private Fields
        [Header("Loot Settings")]
        [SerializeField] private bool m_SpawnOnDeath = true;
        [SerializeField, Range(0f, 1f)] private float m_DropChance = 0.5f;
        [SerializeField] private int m_MinDrops = 0;
        [SerializeField] private int m_MaxDrops = 3;
        [SerializeField] private Vector3 m_DropOffset = new Vector3(0, 0.5f, 0);
        [SerializeField] private float m_DropRadius = 1.5f;
        
        [Header("Weapon Loot")]
        [SerializeField] private bool m_EnableWeaponDrops = true;
        [SerializeField, Range(0f, 1f)] private float m_WeaponDropChance = 0.3f;
        [SerializeField] private WeaponRarity m_MinWeaponRarity = WeaponRarity.Common;
        [SerializeField] private WeaponRarity m_MaxWeaponRarity = WeaponRarity.Rare;
        [SerializeField] private WeaponFactory m_WeaponFactory;
        
        [Header("Other Loot Types")]
        // Additional loot types can be added here
        
        private bool m_HasSpawned = false;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            // Subscribe to death event if applicable
            IHealth healthComponent = GetComponent<IHealth>();
            if (healthComponent != null && m_SpawnOnDeath)
            {
                healthComponent.OnDeath += SpawnLoot;
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from death event
            IHealth healthComponent = GetComponent<IHealth>();
            if (healthComponent != null)
            {
                healthComponent.OnDeath -= SpawnLoot;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Manually trigger loot spawning
        /// </summary>
        public void SpawnLoot()
        {
            if (m_HasSpawned) return;
            
            // Determine number of drops
            if (Random.value > m_DropChance) return;
            
            int numDrops = Random.Range(m_MinDrops, m_MaxDrops + 1);
            
            for (int i = 0; i < numDrops; i++)
            {
                SpawnRandomLoot();
            }
            
            m_HasSpawned = true;
        }
        
        /// <summary>
        /// Force spawn a specific weapon
        /// </summary>
        /// <param name="_weaponConfig">Weapon configuration</param>
        /// <param name="_weaponVisual">Weapon visual</param>
        /// <param name="_type">Weapon type</param>
        /// <param name="_rarity">Weapon rarity</param>
        public void SpawnSpecificWeapon(
            ComboConfiguration _weaponConfig,
            GameObject _weaponVisual = null,
            WeaponType _type = WeaponType.Sword,
            WeaponRarity _rarity = WeaponRarity.Common)
        {
            if (m_WeaponFactory == null)
            {
                Debug.LogWarning("No WeaponFactory assigned to LootSpawner!");
                return;
            }
            
            // Calculate spawn position with offset and random variation
            Vector3 spawnPosition = transform.position + m_DropOffset;
            spawnPosition += Random.insideUnitSphere * m_DropRadius;
            spawnPosition.y = transform.position.y + m_DropOffset.y;
            
            // Spawn the weapon
            m_WeaponFactory.CreateWeaponCollectable(spawnPosition, _weaponConfig, _weaponVisual, _type, _rarity);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Spawns a random loot item
        /// </summary>
        private void SpawnRandomLoot()
        {
            // Determine what type of loot to spawn based on chances
            float lootTypeRoll = Random.value;
            
            // Try to spawn a weapon
            if (m_EnableWeaponDrops && lootTypeRoll <= m_WeaponDropChance)
            {
                SpawnRandomWeapon();
                return;
            }
            
            // Add more loot types here as needed
            // else if (other loot types)...
        }
        
        /// <summary>
        /// Spawns a random weapon
        /// </summary>
        private void SpawnRandomWeapon()
        {
            if (m_WeaponFactory == null)
            {
                // Try to find a weapon factory in scene
                m_WeaponFactory = FindObjectOfType<WeaponFactory>();
                
                if (m_WeaponFactory == null)
                {
                    Debug.LogWarning("No WeaponFactory assigned or found in scene!");
                    return;
                }
            }
            
            // Calculate spawn position with offset and random variation
            Vector3 spawnPosition = transform.position + m_DropOffset;
            spawnPosition += Random.insideUnitSphere * m_DropRadius;
            spawnPosition.y = transform.position.y + m_DropOffset.y;
            
            // Spawn a random weapon
            m_WeaponFactory.CreateRandomWeaponCollectable(spawnPosition, m_MinWeaponRarity, m_MaxWeaponRarity);
        }
        #endregion
    }
    
    /// <summary>
    /// Interface for objects with health that can die
    /// </summary>
    public interface IHealth
    {
        event System.Action OnDeath;
    }
} 