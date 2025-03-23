using UnityEngine;

namespace LocomotionSystem
{
    public class WeaponSocket : MonoBehaviour
    {
        public enum SocketType
        {
            LeftHand,
            RightHand
        }

        [SerializeField] private SocketType socketType;
        
        // The attach point can be the socket itself if not specified
        [SerializeField] private Transform attachPoint;
        
        private GameObject _currentWeapon;

        private void Awake()
        {
            // Use this transform if no specific attach point is set
            if (attachPoint == null)
            {
                attachPoint = transform;
            }
        }

        public void AttachWeapon(GameObject weaponPrefab)
        {
            if (weaponPrefab == null)
            {
                Debug.LogWarning("WeaponSocket: Attempted to attach null weapon prefab");
                return;
            }

            DetachWeapon();

            try
            {
                _currentWeapon = Instantiate(weaponPrefab, attachPoint);
                _currentWeapon.transform.localPosition = Vector3.zero;
                _currentWeapon.transform.localRotation = Quaternion.identity;
                _currentWeapon.transform.localScale = Vector3.one;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"WeaponSocket: Error attaching weapon: {e.Message}");
            }
        }

        public void DetachWeapon()
        {
            if (!_currentWeapon) return;
            
            if (Application.isPlaying)
            {
                Destroy(_currentWeapon);
            }
            else
            {
                DestroyImmediate(_currentWeapon);
            }
            _currentWeapon = null;
        }

        public bool HasWeapon => _currentWeapon != null;
        public SocketType Type => socketType;

        private void OnDisable()
        {
            DetachWeapon();
        }
    }
} 