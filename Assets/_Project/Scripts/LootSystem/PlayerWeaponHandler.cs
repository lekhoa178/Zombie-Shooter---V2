using System.Collections.Generic;
using DG.Tweening;
using Locomotion.LootSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LocomotionSystem
{
    /// <summary>
    /// Handles all weapon-related functionality for the player
    /// </summary>
    [RequireComponent(typeof(Player))]
    public class PlayerWeaponHandler : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Input Bindings")]
        [SerializeField] private InputActionReference m_WeaponSwitch1Action;
        [SerializeField] private InputActionReference m_WeaponSwitch2Action;
        [SerializeField] private InputActionReference m_WeaponSwitch3Action;
        
        [Header("Weapon Collection Behavior")]
        [Tooltip("If true, current weapon will be dropped when picking up a new one. If false, it will be stored in inventory.")]
        [SerializeField] private bool m_DropWeaponOnNewCollection = false;
        #endregion

        #region Private Fields
        private Player m_Player;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            m_Player = GetComponent<Player>();
            
            SetupWeaponSwitchInputs();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Called when a weapon is about to be collected
        /// </summary>
        /// <param name="_newWeapon">The new weapon being collected</param>
        /// <returns>True if collection should proceed, false to abort</returns>
        public bool OnWeaponCollecting(ICollectable _newWeapon)
        {
            var currentWeapon = GetEquippedWeapon();
            
            if (currentWeapon == null)
                return true;
            
            if (m_DropWeaponOnNewCollection)
            {
                DropCurrentWeapon();
            }
            
            return true;
        }
        
        /// <summary>
        /// Drops the currently equipped weapon as a collectable in the world
        /// </summary>
        public void DropCurrentWeapon()
        {
            ICollectable currentWeapon = GetEquippedWeapon();
            if (currentWeapon == null) return;
            
            WeaponCollectableData weaponCollectableData = GetWeaponData(currentWeapon);
            if (weaponCollectableData == null) return;
            
            Vector3 dropPosition = transform.position + transform.forward * 1.5f;
            
            var droppedWeapon = WeaponFactory.Instance.CreateWeaponCollectable(
                dropPosition, 
                weaponCollectableData.Config,
                weaponCollectableData.Rarity
            );
            
            if (droppedWeapon)
            {
                // Start with a small scale
                droppedWeapon.transform.localScale = Vector3.zero;

                // Animate the scale to bounce from small to big
                droppedWeapon.transform.DOScale(Vector3.one, 0.6f)
                    .SetEase(Ease.OutBounce);
            }
            
            WeaponManager.Instance.RemoveWeaponByInstance(m_Player, currentWeapon);
        }
        
        /// <summary>
        /// Switch to a weapon slot
        /// </summary>
        public void SwitchWeapon(int slot)
        {
            if (WeaponManager.Instance != null)
            {
                WeaponManager.Instance.SwitchWeapon(m_Player, slot);
            }
        }
        
        /// <summary>
        /// Get the currently equipped weapon
        /// </summary>
        public ICollectable GetEquippedWeapon()
        {
            if (WeaponManager.Instance)
            {
                return WeaponManager.Instance.GetEquippedWeapon(m_Player);
            }
            
            return null;
        }
        
        /// <summary>
        /// Get the weapon inventory
        /// </summary>
        public List<ICollectable> GetWeaponInventory()
        {
            if (WeaponManager.Instance != null)
            {
                return WeaponManager.Instance.GetWeaponInventory(m_Player);
            }
            
            return new List<ICollectable>();
        }
        
        /// <summary>
        /// Get weapon data for a collectable if it's a weapon
        /// </summary>
        public WeaponCollectableData GetWeaponData(ICollectable collectable)
        {
            if (collectable != null && collectable.Type == CollectableType.Weapon)
            {
                return collectable.GetTypeData<WeaponCollectableData>();
            }
            
            return null;
        }
        #endregion

        #region Private Methods
        private void SetupWeaponSwitchInputs()
        {
            if (m_WeaponSwitch1Action != null)
            {
                m_WeaponSwitch1Action.action.performed += ctx => SwitchWeapon(0);
                m_WeaponSwitch1Action.action.Enable();
            }
            
            if (m_WeaponSwitch2Action != null)
            {
                m_WeaponSwitch2Action.action.performed += ctx => SwitchWeapon(1);
                m_WeaponSwitch2Action.action.Enable();
            }
            
            if (m_WeaponSwitch3Action != null)
            {
                m_WeaponSwitch3Action.action.performed += ctx => SwitchWeapon(2);
                m_WeaponSwitch3Action.action.Enable();
            }
        }
        
        #endregion
    }
} 