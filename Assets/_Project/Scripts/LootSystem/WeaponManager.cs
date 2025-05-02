using System;
using System.Collections.Generic;
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
        public event Action<Player, IInteractable> OnWeaponEquipped;
        
        // Event triggered when a weapon is added to inventory
        public event Action<Player, IInteractable> OnWeaponAdded;
        
        // Event triggered when a weapon is removed from inventory
        public event Action<Player, IInteractable> OnWeaponRemoved;
        #endregion
        
        #region Private Fields
        [Header("Weapon Settings")]
        [SerializeField] private int m_MaxWeaponSlots = 3;
        [SerializeField] private bool m_DebugMode = false;
        [SerializeField] private ComboConfiguration m_DefaultComboConfig;
        
        // Dictionary mapping players to their equipped weapons and inventory
        private Dictionary<Player, PlayerWeaponInventory> m_PlayerWeapons = new Dictionary<Player, PlayerWeaponInventory>();
        
        // Dictionary mapping players to their active ComboConfiguration
        private Dictionary<Player, ComboConfiguration> m_PlayerComboConfigs = new Dictionary<Player, ComboConfiguration>();
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
            
            if (!m_PlayerWeapons.TryGetValue(player, out var inventory))
            {
                inventory = new PlayerWeaponInventory(m_MaxWeaponSlots);
                m_PlayerWeapons[player] = inventory;
            }
            
            bool added = inventory.AddWeapon(interactable);
            
            if (added)
            {
                if (inventory.EquippedWeapon == null)
                {
                    inventory.EquippedWeapon = interactable;
                    
                    ApplyWeaponConfig(player, interactable, weaponCollectableData);
                    
                    OnWeaponEquipped?.Invoke(player, interactable);
                    
                    if (m_DebugMode)
                    {
                        Debug.Log($"Equipped weapon: {interactable.Name} for player {player.name}");
                    }
                }
                else
                {
                    // Just add to inventory if another weapon is already equipped
                    OnWeaponAdded?.Invoke(player, interactable);
                    
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
            
            if (!m_PlayerWeapons.TryGetValue(player, out PlayerWeaponInventory inventory))
            {
                return false;
            }
            
            var weapon = inventory.GetWeaponAt(weaponIndex);
            
            if (weapon != null)
            {
                if (inventory.EquippedWeapon == weapon) return true;
                
                WeaponCollectableData weaponCollectableData = weapon.GetTypeData<WeaponCollectableData>();
                if (weaponCollectableData == null) return false;
                
                inventory.EquippedWeapon = weapon;
                
                ApplyWeaponConfig(player, weapon, weaponCollectableData);
                
                OnWeaponEquipped?.Invoke(player, weapon);
                
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
        /// <param name="_player">The player</param>
        /// <returns>The equipped weapon or null if none is equipped</returns>
        public IInteractable GetEquippedWeapon(Player _player)
        {
            if (!_player) return null;
            
            return !m_PlayerWeapons.TryGetValue(_player, out PlayerWeaponInventory inventory) ? null : inventory.EquippedWeapon;
        }

        /// <summary>
        /// Gets the list of weapons in the player's inventory
        /// </summary>
        /// <param name="_player">The player</param>
        /// <returns>List of weapons in the inventory</returns>
        public List<IInteractable> GetWeaponInventory(Player _player)
        {
            if (_player == null) return new List<IInteractable>();
            
            if (!m_PlayerWeapons.TryGetValue(_player, out PlayerWeaponInventory inventory))
            {
                return new List<IInteractable>();
            }
            
            return inventory.Weapons;
        }

        /// <summary>
        /// Removes a weapon from the player's inventory by index
        /// </summary>
        /// <param name="player">The player</param>
        /// <param name="weaponIndex">The index of the weapon to remove</param>
        /// <returns>True if the weapon was removed successfully</returns>
        public bool RemoveWeapon(Player player, int weaponIndex)
        {
            if (player == null) return false;
            
            if (!m_PlayerWeapons.TryGetValue(player, out PlayerWeaponInventory inventory))
            {
                return false;
            }
            
            IInteractable weapon = inventory.GetWeaponAt(weaponIndex);
            
            if (weapon != null)
            {
                return RemoveWeaponByInstance(player, weapon);
            }
            
            return false;
        }
        
        /// <summary>
        /// Removes a specific weapon instance from the player's inventory
        /// </summary>
        /// <param name="player">The player</param>
        /// <param name="weapon">The weapon instance to remove</param>
        /// <returns>True if the weapon was removed successfully</returns>
        public bool RemoveWeaponByInstance(Player player, IInteractable weapon)
        {
            if (!player || weapon == null) return false;
            
            // Ensure the player has an inventory
            if (!m_PlayerWeapons.TryGetValue(player, out PlayerWeaponInventory inventory))
            {
                return false;
            }
            
            // If removing equipped weapon, clear equipped reference
            if (inventory.EquippedWeapon == weapon)
            {
                inventory.EquippedWeapon = null;
                
                // If there are other weapons in inventory, equip the first one
                if (inventory.Weapons.Count > 1)
                {
                    // Find the next weapon to equip (not the one being removed)
                    foreach (var nextWeapon in inventory.Weapons)
                    {
                        if (nextWeapon != weapon)
                        {
                            // Set as equipped weapon
                            inventory.EquippedWeapon = nextWeapon;
                            
                            // Get weapon data
                            WeaponCollectableData weaponCollectableData = nextWeapon.GetTypeData<WeaponCollectableData>();
                            
                            if (weaponCollectableData != null)
                            {
                                // Apply weapon configuration to player
                                ApplyWeaponConfig(player, nextWeapon, weaponCollectableData);
                                
                                // Trigger equipped event
                                OnWeaponEquipped?.Invoke(player, nextWeapon);
                                
                                if (m_DebugMode)
                                {
                                    Debug.Log($"Auto-equipped next weapon: {nextWeapon.Name} for player {player.name}");
                                }
                            }
                            
                            break;
                        }
                    }
                }
            }
            
            // Remove from inventory
            bool removed = inventory.RemoveWeapon(weapon);
            
            if (removed)
            {
                // Trigger removed event
                OnWeaponRemoved?.Invoke(player, weapon);
                
                if (m_DebugMode)
                {
                    Debug.Log($"Removed weapon: {weapon.Name} from player {player.name}");
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
            
            if (m_PlayerComboConfigs.TryGetValue(player, out ComboConfiguration config))
            {
                return config;
            }
            
            return m_DefaultComboConfig;
        }
        
        /// <summary>
        /// Sets the active ComboConfiguration for a player
        /// </summary>
        /// <param name="player">The player</param>
        /// <param name="config">The ComboConfiguration to set</param>
        public void SetActiveComboConfiguration(Player player, ComboConfiguration config)
        {
            if (!player) return;
            
            m_PlayerComboConfigs[player] = config;
            
            if (m_DebugMode)
            {
                Debug.Log($"Set active ComboConfiguration for player {player.name}");
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
        /// <param name="player">The player to apply the configuration to</param>
        /// <param name="weapon">The weapon interactable</param>
        /// <param name="weaponCollectableData">The weapon data</param>
        private void ApplyWeaponConfig(Player player, IInteractable weapon, WeaponCollectableData weaponCollectableData)
        {
            if (!player || weapon == null || weaponCollectableData == null || !weaponCollectableData.Config) return;
            
            // Store the weapon's ComboConfiguration for this player
            SetActiveComboConfiguration(player, weaponCollectableData.Config);
            
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