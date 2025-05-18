using System;
using System.Collections.Generic;
using System.Linq;
using Locomotion.LootSystem;
using UnityEngine;
using UnityEngine.Events;
using UnityUtils;
using Random = UnityEngine.Random;

namespace LocomotionSystem
{
    /// <summary>
    /// Manager class for weapon handling and switching
    /// </summary>
    public class WeaponManager : PersistentSingleton<WeaponManager>
    {
        #region Events
        // Event triggered when a weapon is equipped
        public event Action<IInteractable> OnWeaponEquipped;
        
        // Event triggered when a weapon is added to inventory
        public event Action<IInteractable> OnWeaponAdded;
        
        // Event triggered when a weapon is removed from inventory
        public event Action<IInteractable> OnWeaponRemoved;
        #endregion
        
        #region Private Fields
        [Header("Weapon Settings")]
        [SerializeField] private int m_MaxWeaponSlots = 3;
        [SerializeField] private bool m_DebugMode = false;
        [SerializeField] private ComboConfiguration m_DefaultComboConfig;
        
        // Dictionary mapping players to their equipped weapons and inventory
        private PlayerWeaponInventory m_PlayerInventory;
        
        // Dictionary mapping players to their active ComboConfiguration
        private ComboConfiguration m_PlayerComboConfig;
        #endregion

        #region Public Methods

        /// <summary>
        /// Equips a weapon for the specified player
        /// </summary>
        /// <param name="player">The player to equip the weapon to</param>
        /// <param name="interactable">The weapon to equip</param>
        /// <returns>True if the weapon was equipped successfully</returns>
        public bool EquipWeapon(Player player, IInteractable interactable)
        {
            if (!player || interactable == null) return false;
            
            if (interactable.Type != InteractableType.Weapon) return false;
            
            var weaponCollectableData = interactable.GetTypeData<WeaponCollectableData>();
            if (weaponCollectableData == null) return false;
            
            if (!player.GetComponent<PlayerWeaponHandler>().OnWeaponCollecting(interactable))
                return false;
            
            m_PlayerInventory ??= new PlayerWeaponInventory(m_MaxWeaponSlots);
            
            var added = m_PlayerInventory.AddWeapon(interactable);
            
            if (added)
            {
                if (m_PlayerInventory.EquippedWeapon == null)
                {
                    m_PlayerInventory.EquippedWeapon = interactable;
                    
                    ApplyWeaponConfig(interactable, weaponCollectableData);
                    
                    OnWeaponEquipped?.Invoke(interactable);
                    
                    if (m_DebugMode)
                    {
                        Debug.Log($"Equipped weapon: {interactable.Name} for player {player.name}");
                    }
                }
                else
                {
                    OnWeaponAdded?.Invoke(interactable);
                    
                    if (m_DebugMode)
                    {
                        Debug.Log($"Added weapon to inventory: {interactable.Name} for player {player.name}");
                    }
                }
                
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Switches to a specific weapon in the player's inventory
        /// </summary>
        /// <param name="player">The player</param>
        /// <param name="weaponIndex">The index of the weapon to switch to</param>
        /// <returns>True if the switch was successful</returns>
        public bool SwitchWeapon(Player player, int weaponIndex)
        {
            if (player == null) return false;
            
            if (m_PlayerInventory == null)
            {
                Debug.LogWarning("Player weapon inventory is null!");
                return false;
            }
            
            var weapon = m_PlayerInventory.GetWeaponAt(weaponIndex);
            
            if (weapon != null)
            {
                if (m_PlayerInventory.EquippedWeapon == weapon) return true;
                
                WeaponCollectableData weaponCollectableData = weapon.GetTypeData<WeaponCollectableData>();
                if (weaponCollectableData == null) return false;
                
                m_PlayerInventory.EquippedWeapon = weapon;
                
                ApplyWeaponConfig(weapon, weaponCollectableData);
                
                OnWeaponEquipped?.Invoke(weapon);
                
                if (m_DebugMode)
                {
                    Debug.Log($"Switched to weapon: {weapon.Name} for player {player.name}");
                }
                
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Gets the currently equipped weapon for the player
        /// </summary>
        /// <returns>The equipped weapon or null if none is equipped</returns>
        public IInteractable GetEquippedWeapon()
        {
            return m_PlayerInventory?.EquippedWeapon;
        }

        /// <summary>
        /// Gets the list of weapons in the player's inventory
        /// </summary>
        /// <returns>List of weapons in the inventory</returns>
        public List<IInteractable> GetWeaponInventory()
        {
            return m_PlayerInventory == null ? new List<IInteractable>() : m_PlayerInventory.Weapons;
        }

        /// <summary>
        /// Removes a weapon from the player's inventory by index
        /// </summary>
        /// <param name="weaponIndex">The index of the weapon to remove</param>
        /// <returns>True if the weapon was removed successfully</returns>
        public bool RemoveWeapon(int weaponIndex)
        {
            if (m_PlayerInventory == null)
            {
                return false;
            }
            
            var weapon = m_PlayerInventory.GetWeaponAt(weaponIndex);
            
            return weapon != null && RemoveWeaponByInstance(weapon);
        }
        
        /// <summary>
        /// Removes a specific weapon instance from the player's inventory
        /// </summary>
        /// <param name="weapon">The weapon instance to remove</param>
        /// <returns>True if the weapon was removed successfully</returns>
        public bool RemoveWeaponByInstance(IInteractable weapon)
        {
            if (m_PlayerInventory == null)
            {
                return false;
            }

            if (m_PlayerInventory.EquippedWeapon == weapon)
            {
                m_PlayerInventory.EquippedWeapon = null;
                
                // If there are other weapons in inventory, equip the first one
                if (m_PlayerInventory.Weapons.Count > 1)
                {
                    // Find the next weapon to equip (not the one being removed)
                    foreach (var nextWeapon in m_PlayerInventory.Weapons.Where(nextWeapon => nextWeapon != weapon))
                    {
                        m_PlayerInventory.EquippedWeapon = nextWeapon;
                            
                        var weaponCollectableData = nextWeapon.GetTypeData<WeaponCollectableData>();
                            
                        if (weaponCollectableData != null)
                        {
                            ApplyWeaponConfig(nextWeapon, weaponCollectableData);
                                
                            // Trigger equipped event
                            OnWeaponEquipped?.Invoke(nextWeapon);
                                
                            if (m_DebugMode)
                            {
                                Debug.Log($"Auto-equipped next weapon: {nextWeapon.Name}");
                            }
                        }
                            
                        break;
                    }
                }
            }
            
            // Remove from inventory
            bool removed = m_PlayerInventory.RemoveWeapon(weapon);
            
            if (removed)
            {
                // Trigger removed event
                OnWeaponRemoved?.Invoke(weapon);
                
                if (m_DebugMode)
                {
                    Debug.Log($"Removed weapon: {weapon.Name}");
                }
                
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Gets the active ComboConfiguration for a player
        /// </summary>
        /// <param name="player">The player</param>
        /// <returns>The active ComboConfiguration or the default if none is set</returns>
        public ComboConfiguration GetActiveComboConfiguration(Player player)
        {
            if (!player) return m_DefaultComboConfig;

            return m_PlayerComboConfig ? m_PlayerComboConfig : m_DefaultComboConfig;
        }
        
        /// <summary>
        /// Sets the active ComboConfiguration for a player
        /// </summary>
        /// <param name="config">The ComboConfiguration to set</param>
        public void SetActiveComboConfiguration(ComboConfiguration config)
        {
            m_PlayerComboConfig = config;
            
            if (m_DebugMode)
            {
                Debug.Log($"Set active ComboConfiguration");
            }
        }
        
        public static event CalculateDamageHandler OnCalculateDamage;

        public delegate void CalculateDamageHandler(BaseEnemy target, ref float damage);
        
        public float CalculateDamage(AttackHitConfiguration attackConfig, Player player, IHittable target, out bool isCriticalHit)
        {
            if (attackConfig == null || player == null)
            {
                isCriticalHit = false;
                return 0f;
            }
            
            isCriticalHit = attackConfig.EnableCriticalHit && Random.Range(0f, 100f) <= attackConfig.CriticalHitChance;
            var finalDamage = isCriticalHit ? attackConfig.Damage * attackConfig.CriticalHitDamageMultiplier : attackConfig.Damage;
            finalDamage *= player.PlayerStats.GetDamageMultiplier();

            if (target is BaseEnemy enemy)
            {
                OnCalculateDamage?.Invoke(enemy, ref finalDamage);
            }

            return finalDamage;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Applies a weapon configuration to a player
        /// </summary>
        /// <param name="weapon">The weapon interactable</param>
        /// <param name="weaponCollectableData">The weapon data</param>
        private void ApplyWeaponConfig(IInteractable weapon, WeaponCollectableData weaponCollectableData)
        {
            if (weaponCollectableData == null || !weaponCollectableData.Config) return;
            
            SetActiveComboConfiguration(weaponCollectableData.Config);
            
            if (m_DebugMode)
            {
                Debug.Log($"Applied weapon config to player: {weapon.Name}");
            }
        }
        #endregion
        
        #region Helper Classes
        /// <summary>
        /// Class to manage a player's weapon inventory
        /// </summary>
        private class PlayerWeaponInventory
        {
            public List<IInteractable> Weapons { get; private set; }
            public IInteractable EquippedWeapon { get; set; }
            public int MaxSlots { get; private set; }
            
            public PlayerWeaponInventory(int maxSlots)
            {
                MaxSlots = maxSlots;
                Weapons = new List<IInteractable>(maxSlots);
                EquippedWeapon = null;
            }
            
            public bool AddWeapon(IInteractable weapon)
            {
                if (weapon == null) return false;
                
                // Check if inventory is full
                if (Weapons.Count >= MaxSlots)
                {
                    // Could implement inventory full logic here
                    Debug.LogWarning("Weapon inventory is full!");
                    return false;
                }
                
                // Check if weapon is already in inventory
                if (Weapons.Contains(weapon))
                {
                    return false;
                }
                
                // Add weapon to inventory
                Weapons.Add(weapon);
                return true;
            }
            
            public bool RemoveWeapon(IInteractable weapon)
            {
                if (weapon == null) return false;
                
                // Remove weapon from inventory
                return Weapons.Remove(weapon);
            }
            
            public IInteractable GetWeaponAt(int index)
            {
                if (index < 0 || index >= Weapons.Count)
                {
                    return null;
                }
                
                return Weapons[index];
            }
        }
        #endregion
    }
} 