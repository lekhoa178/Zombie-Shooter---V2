using System;
using System.Collections.Generic;
using UnityEngine;

namespace LocomotionSystem.LootSystem
{
    /// <summary>
    /// Manager class for weapon handling and switching
    /// </summary>
    public class WeaponManager : MonoBehaviour
    {
        #region Singleton
        private static WeaponManager s_Instance;
        
        public static WeaponManager Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = FindObjectOfType<WeaponManager>();
                    
                    if (s_Instance == null)
                    {
                        GameObject obj = new GameObject("WeaponManager");
                        s_Instance = obj.AddComponent<WeaponManager>();
                    }
                }
                
                return s_Instance;
            }
        }
        #endregion
        
        #region Events
        // Event triggered when a weapon is equipped
        public event Action<Player, IWeaponCollectable> OnWeaponEquipped;
        
        // Event triggered when a weapon is added to inventory
        public event Action<Player, IWeaponCollectable> OnWeaponAdded;
        
        // Event triggered when a weapon is removed from inventory
        public event Action<Player, IWeaponCollectable> OnWeaponRemoved;
        #endregion
        
        #region Private Fields
        [Header("Weapon Settings")]
        [SerializeField] private int m_MaxWeaponSlots = 3;
        [SerializeField] private bool m_DebugMode = false;
        
        // Dictionary mapping players to their equipped weapons and inventory
        private Dictionary<Player, PlayerWeaponInventory> m_PlayerWeapons = new Dictionary<Player, PlayerWeaponInventory>();
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Ensure singleton pattern
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            s_Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Equips a weapon for the specified player
        /// </summary>
        /// <param name="_player">The player to equip the weapon to</param>
        /// <param name="_weapon">The weapon to equip</param>
        /// <returns>True if the weapon was equipped successfully</returns>
        public bool EquipWeapon(Player _player, IWeaponCollectable _weapon)
        {
            if (_player == null || _weapon == null) return false;
            
            // Ensure the player has an inventory
            if (!m_PlayerWeapons.TryGetValue(_player, out PlayerWeaponInventory inventory))
            {
                inventory = new PlayerWeaponInventory(m_MaxWeaponSlots);
                m_PlayerWeapons[_player] = inventory;
            }
            
            // Add the weapon to inventory
            bool added = inventory.AddWeapon(_weapon);
            
            if (added)
            {
                // Set as equipped weapon if no weapon is currently equipped
                if (inventory.EquippedWeapon == null)
                {
                    inventory.EquippedWeapon = _weapon;
                    
                    // Apply weapon configuration to player
                    ApplyWeaponConfig(_player, _weapon);
                    
                    // Trigger equipped event
                    OnWeaponEquipped?.Invoke(_player, _weapon);
                    
                    if (m_DebugMode)
                    {
                        Debug.Log($"Equipped weapon: {_weapon.Name} for player {_player.name}");
                    }
                }
                else
                {
                    // Just add to inventory if another weapon is already equipped
                    OnWeaponAdded?.Invoke(_player, _weapon);
                    
                    if (m_DebugMode)
                    {
                        Debug.Log($"Added weapon to inventory: {_weapon.Name} for player {_player.name}");
                    }
                }
                
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Switches to a specific weapon in the player's inventory
        /// </summary>
        /// <param name="_player">The player</param>
        /// <param name="_weaponIndex">The index of the weapon to switch to</param>
        /// <returns>True if the switch was successful</returns>
        public bool SwitchWeapon(Player _player, int _weaponIndex)
        {
            if (_player == null) return false;
            
            // Ensure the player has an inventory
            if (!m_PlayerWeapons.TryGetValue(_player, out PlayerWeaponInventory inventory))
            {
                return false;
            }
            
            // Get the weapon at the specified index
            IWeaponCollectable weapon = inventory.GetWeaponAt(_weaponIndex);
            
            if (weapon != null)
            {
                // Already equipped
                if (inventory.EquippedWeapon == weapon) return true;
                
                // Set as equipped weapon
                inventory.EquippedWeapon = weapon;
                
                // Apply weapon configuration to player
                ApplyWeaponConfig(_player, weapon);
                
                // Trigger equipped event
                OnWeaponEquipped?.Invoke(_player, weapon);
                
                if (m_DebugMode)
                {
                    Debug.Log($"Switched to weapon: {weapon.Name} for player {_player.name}");
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
        public IWeaponCollectable GetEquippedWeapon(Player _player)
        {
            if (_player == null) return null;
            
            // Ensure the player has an inventory
            if (!m_PlayerWeapons.TryGetValue(_player, out PlayerWeaponInventory inventory))
            {
                return null;
            }
            
            return inventory.EquippedWeapon;
        }

        /// <summary>
        /// Gets the list of weapons in the player's inventory
        /// </summary>
        /// <param name="_player">The player</param>
        /// <returns>List of weapons in the inventory</returns>
        public List<IWeaponCollectable> GetWeaponInventory(Player _player)
        {
            if (_player == null) return new List<IWeaponCollectable>();
            
            // Ensure the player has an inventory
            if (!m_PlayerWeapons.TryGetValue(_player, out PlayerWeaponInventory inventory))
            {
                return new List<IWeaponCollectable>();
            }
            
            return inventory.Weapons;
        }

        /// <summary>
        /// Removes a weapon from the player's inventory
        /// </summary>
        /// <param name="_player">The player</param>
        /// <param name="_weaponIndex">The index of the weapon to remove</param>
        /// <returns>True if the weapon was removed successfully</returns>
        public bool RemoveWeapon(Player _player, int _weaponIndex)
        {
            if (_player == null) return false;
            
            // Ensure the player has an inventory
            if (!m_PlayerWeapons.TryGetValue(_player, out PlayerWeaponInventory inventory))
            {
                return false;
            }
            
            // Get the weapon at the specified index
            IWeaponCollectable weapon = inventory.GetWeaponAt(_weaponIndex);
            
            if (weapon != null)
            {
                // If removing equipped weapon, clear equipped reference
                if (inventory.EquippedWeapon == weapon)
                {
                    inventory.EquippedWeapon = null;
                }
                
                // Remove from inventory
                bool removed = inventory.RemoveWeapon(weapon);
                
                if (removed)
                {
                    // Trigger removed event
                    OnWeaponRemoved?.Invoke(_player, weapon);
                    
                    if (m_DebugMode)
                    {
                        Debug.Log($"Removed weapon: {weapon.Name} from player {_player.name}");
                    }
                    
                    return true;
                }
            }
            
            return false;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Applies a weapon configuration to a player
        /// </summary>
        /// <param name="_player">The player to apply the configuration to</param>
        /// <param name="_weapon">The weapon containing the configuration</param>
        private void ApplyWeaponConfig(Player _player, IWeaponCollectable _weapon)
        {
            if (_player == null || _weapon == null || _weapon.WeaponConfig == null) return;
            
            // Update player's combo configuration
            var newComboConfig = _weapon.WeaponConfig;
            
            // Create new PlayerSO data with this weapon's combo configuration
            PlayerSO currentData = _player.Data;
            
            // Create a clone of the PlayerSO to modify it
            PlayerSO newData = ScriptableObject.Instantiate(currentData);
            
            // Set the new combo configuration
            var field = typeof(PlayerSO).GetField("ComboConfig", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (field != null)
            {
                field.SetValue(newData, newComboConfig);
                
                // Update the player's Data field with our modified copy
                var playerDataField = typeof(Player).GetField("Data", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (playerDataField != null)
                {
                    playerDataField.SetValue(_player, newData);
                    
                    if (m_DebugMode)
                    {
                        Debug.Log($"Applied weapon config to player: {_weapon.Name}");
                    }
                }
            }
            
            // Update the weapon model if needed
            if (_weapon.WeaponVisualPrefab != null)
            {
                // Instantiate the weapon visual
                GameObject weaponVisual = UnityEngine.Object.Instantiate(_weapon.WeaponVisualPrefab);
                
                // Find and destroy any previously equipped weapon models
                foreach (Transform child in _player.transform)
                {
                    if (child.CompareTag("EquippedWeapon"))
                    {
                        Destroy(child.gameObject);
                    }
                }
                
                // Parent the weapon visual to the player
                weaponVisual.transform.SetParent(_player.transform);
                weaponVisual.transform.localPosition = Vector3.zero;
                weaponVisual.transform.localRotation = Quaternion.identity;
                weaponVisual.tag = "EquippedWeapon";
            }
        }
        #endregion
        
        #region Helper Classes
        /// <summary>
        /// Class to manage a player's weapon inventory
        /// </summary>
        private class PlayerWeaponInventory
        {
            public List<IWeaponCollectable> Weapons { get; private set; }
            public IWeaponCollectable EquippedWeapon { get; set; }
            public int MaxSlots { get; private set; }
            
            public PlayerWeaponInventory(int _maxSlots)
            {
                MaxSlots = _maxSlots;
                Weapons = new List<IWeaponCollectable>(_maxSlots);
                EquippedWeapon = null;
            }
            
            public bool AddWeapon(IWeaponCollectable _weapon)
            {
                if (_weapon == null) return false;
                
                // Check if inventory is full
                if (Weapons.Count >= MaxSlots)
                {
                    // Could implement inventory full logic here
                    Debug.LogWarning("Weapon inventory is full!");
                    return false;
                }
                
                // Check if weapon is already in inventory
                if (Weapons.Contains(_weapon))
                {
                    return false;
                }
                
                // Add weapon to inventory
                Weapons.Add(_weapon);
                return true;
            }
            
            public bool RemoveWeapon(IWeaponCollectable _weapon)
            {
                if (_weapon == null) return false;
                
                // Remove weapon from inventory
                return Weapons.Remove(_weapon);
            }
            
            public IWeaponCollectable GetWeaponAt(int _index)
            {
                if (_index < 0 || _index >= Weapons.Count)
                {
                    return null;
                }
                
                return Weapons[_index];
            }
        }
        #endregion
    }
} 