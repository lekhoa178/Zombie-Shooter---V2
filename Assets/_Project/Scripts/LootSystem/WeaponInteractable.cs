using Locomotion.LootSystem;
using UnityEngine;
using UnityEngine.VFX;

namespace LocomotionSystem
{
    /// <summary>
    /// Concrete implementation of a weapon interactable.
    /// This class automatically sets its InteractableType to Weapon.
    /// </summary>
    [AddComponentMenu("Loot System/Weapon Collectable")]
    public class WeaponInteractable : CollectableBase
    {
        // Note: The m_Type field from the base class will be set to Weapon
        // by the custom editor. This is handled automatically.

        #region Private Fields
        [Header("Weapon Configuration")]
        [SerializeField] private ComboConfiguration m_WeaponConfig;
        [SerializeField] private GameObject m_WeaponVisualPrefab;
        [SerializeField] private WeaponRarity m_Rarity = WeaponRarity.Common;
        [SerializeField] private WeaponType m_WeaponType = WeaponType.Sword;
        
        [Header("VFX")]
        [SerializeField] private Color m_RarityGlowColor;
        [SerializeField] private GameObject m_RarityVfxPrefab;
        [SerializeField] private GameObject m_CollectionVfxPrefab;
        
        private VisualEffect m_RarityVfx;
        private WeaponCollectableData _mWeaponCollectableData;
        #endregion

        #region Public Properties
        // IWeaponCollectable implementation
        public ComboConfiguration WeaponConfig => m_WeaponConfig;
        public GameObject WeaponVisualPrefab => m_WeaponVisualPrefab;
        public WeaponRarity Rarity => m_Rarity;
        public WeaponType WeaponType => m_WeaponType;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            
            // Initialize weapon data
            _mWeaponCollectableData = new WeaponCollectableData
            {
                Config = m_WeaponConfig,
                Type = m_WeaponType,
                Rarity = m_Rarity
            };
            
            // Ensure type is set correctly
            m_Type = InteractableType.Weapon;
        }

        #if UNITY_EDITOR
        /// <summary>
        /// Ensure the InteractableType is always set to Weapon in the inspector
        /// </summary>
        private void OnValidate()
        {
            // Set type directly since it's now protected in the base class
            m_Type = InteractableType.Weapon;
        }
        #endif

        protected override void Start()
        {
            base.Start();
            
            // Create rarity VFX if needed
            SetupRarityVfx();
        }
        #endregion

        #region Public Methods
        public override bool OnInteract(GameObject collector)
        {
            var player = collector.GetComponent<Player>();

            if (!player) return false;
            
            var weaponHandler = player.GetComponent<PlayerWeaponHandler>();
            
            if (weaponHandler && !weaponHandler.OnWeaponCollecting(this))
            {
                return false;
            }
            
            var equipped = WeaponManager.Instance.EquipWeapon(player, this);
            
            if (m_CollectionVfxPrefab)
            {
                GameObject vfx = Instantiate(m_CollectionVfxPrefab, transform.position, Quaternion.identity);
                Destroy(vfx);
            }
                
            // CameraShakeManager.Instance.Shake(1.5f, 0.5f, 0.5f);

            return base.OnInteract(collector);
        }
        
        /// <summary>
        /// Implementation of GetTypeData for weapons
        /// </summary>
        public override T GetTypeData<T>()
        {
            if (typeof(T) == typeof(WeaponCollectableData))
            {
                return _mWeaponCollectableData as T;
            }
            
            return base.GetTypeData<T>();
        }
        #endregion

        #region Private Methods
        private void SetupRarityVfx()
        {
            if (m_RarityVfxPrefab != null)
            {
                var vfxObj = Instantiate(m_RarityVfxPrefab, transform);
                vfxObj.transform.localPosition = Vector3.zero;
                
                m_RarityVfx = vfxObj.GetComponent<VisualEffect>();
                
                if (m_RarityVfx != null)
                {
                    // Set color based on rarity
                    switch (m_Rarity)
                    {
                        case WeaponRarity.Common:
                            m_RarityGlowColor = Color.white;
                            break;
                        case WeaponRarity.Uncommon:
                            m_RarityGlowColor = Color.green;
                            break;
                        case WeaponRarity.Rare:
                            m_RarityGlowColor = Color.blue;
                            break;
                        case WeaponRarity.Epic:
                            m_RarityGlowColor = new Color(0.5f, 0, 0.5f); // Purple
                            break;
                        case WeaponRarity.Legendary:
                            m_RarityGlowColor = Color.yellow;
                            break;
                    }
                    
                    // Set VFX parameters if available
                    // if (m_RarityVfx.HasColor("GlowColor"))
                    // {
                    //     m_RarityVfx.SetVector4("GlowColor", m_RarityGlowColor);
                    // }
                    
                    // Set intensity based on rarity
                    if (m_RarityVfx.HasFloat("Intensity"))
                    {
                        m_RarityVfx.SetFloat("Intensity", (float)m_Rarity + 1);
                    }
                }
            }
        }
        #endregion
    }
} 