using UnityEngine;
using UnityEngine.VFX;

namespace LocomotionSystem.LootSystem
{
    /// <summary>
    /// Concrete implementation of a weapon collectable
    /// </summary>
    public class WeaponCollectable : CollectableBase, IWeaponCollectable
    {
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
        #endregion

        #region Public Properties
        public ComboConfiguration WeaponConfig => m_WeaponConfig;
        public GameObject WeaponVisualPrefab => m_WeaponVisualPrefab;
        public WeaponRarity Rarity => m_Rarity;
        public WeaponType WeaponType => m_WeaponType;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            
            // Set default type for weapon collectables
            if (Type == CollectableType.Weapon)
            {
                var serField = typeof(CollectableBase).GetField("m_Type", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (serField != null)
                {
                    serField.SetValue(this, CollectableType.Weapon);
                }
            }
        }

        protected override void Start()
        {
            base.Start();
            
            // Create rarity VFX if needed
            SetupRarityVfx();
        }
        #endregion

        #region Public Methods
        public override bool OnCollected(GameObject _collector)
        {
            // Get the player component
            Player player = _collector.GetComponent<Player>();
            
            if (player != null)
            {
                // Attempt to equip the weapon
                bool equipped = WeaponManager.Instance.EquipWeapon(player, this);
                
                if (equipped)
                {
                    // Spawn collection VFX if available
                    if (m_CollectionVfxPrefab != null)
                    {
                        GameObject vfx = Instantiate(m_CollectionVfxPrefab, transform.position, Quaternion.identity);
                        Destroy(vfx, 2f); // Auto-destroy VFX after 2 seconds
                    }
                    
                    // Return to base class for standard collection behavior
                    return base.OnCollected(_collector);
                }
                else
                {
                    return false; // Collection failed
                }
            }
            
            return false;
        }
        #endregion

        #region Private Methods
        private void SetupRarityVfx()
        {
            // Create rarity-based visual effects based on rarity
            if (m_RarityVfxPrefab != null)
            {
                GameObject vfxObj = Instantiate(m_RarityVfxPrefab, transform);
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
                    
                    // // Set VFX parameters if available
                    // if (m_RarityVfx.HasColor("GlowColor"))
                    // {
                    //     m_RarityVfx.SetVector4("GlowColor", m_RarityGlowColor);
                    // }
                    //
                    // // Set intensity based on rarity
                    // if (m_RarityVfx.HasFloat("Intensity"))
                    // {
                    //     m_RarityVfx.SetFloat("Intensity", (float)m_Rarity + 1);
                    // }
                }
            }
        }
        #endregion
    }
} 