using System.Collections.Generic;
using UnityEngine;

namespace LocomotionSystem
{
    public class WeaponManager : MonoBehaviour
    {
        [SerializeField] private WeaponSocket leftHandSocket;
        [SerializeField] private WeaponSocket rightHandSocket;

        private Dictionary<WeaponSocket.SocketType, WeaponSocket> _sockets;

        private void Awake()
        {
            // Validate socket assignments
            if (leftHandSocket == null || rightHandSocket == null)
            {
                Debug.LogError("WeaponManager: Hand sockets not properly assigned!");
            }

            _sockets = new Dictionary<WeaponSocket.SocketType, WeaponSocket>
            {
                { WeaponSocket.SocketType.LeftHand, leftHandSocket },
                { WeaponSocket.SocketType.RightHand, rightHandSocket }
            };
        }

        public void HandleWeaponForAttack(AttackHitConfiguration config)
        {
            if (!config) return;
            
            // Clear previous weapons if needed
            if (config.RemoveWeaponAfterAttack)
            {
                ClearAllWeapons();
            }

            if (!config.UseWeapon || !config.WeaponPrefab) return;

            try
            {
                // Attach weapons based on configuration
                switch (config.WeaponSlot)
                {
                    case AttackHitConfiguration.WeaponHand.Left:
                        _sockets[WeaponSocket.SocketType.LeftHand].AttachWeapon(config.WeaponPrefab);
                        break;
                    case AttackHitConfiguration.WeaponHand.Right:
                        _sockets[WeaponSocket.SocketType.RightHand].AttachWeapon(config.WeaponPrefab);
                        break;
                    case AttackHitConfiguration.WeaponHand.Both:
                        _sockets[WeaponSocket.SocketType.LeftHand].AttachWeapon(config.WeaponPrefab);
                        _sockets[WeaponSocket.SocketType.RightHand].AttachWeapon(config.WeaponPrefab);
                        break;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"WeaponManager: Error handling weapon attachment: {e.Message}");
            }
        }

        public void ClearAllWeapons()
        {
            if (_sockets == null) return;
            
            foreach (var socket in _sockets.Values)
            {
                if (socket)
                {
                    socket.DetachWeapon();
                }
            }
        }

        private void OnDisable()
        {
            ClearAllWeapons();
        }
    }
} 