using UnityEngine;

namespace Locomotion.LootSystem
{
    /// <summary>
    /// Data container for weapon-specific properties
    /// </summary>
    public class WeaponCollectableData
    {
        /// <summary>
        /// The weapon's combo configuration
        /// </summary>
        public ComboConfiguration Config { get; set; }
        
        /// <summary>
        /// The weapon's type
        /// </summary>
        public WeaponType Type { get; set; }
        
        /// <summary>
        /// The weapon's rarity
        /// </summary>
        public WeaponRarity Rarity { get; set; }
    }
    
    /// <summary>
    /// Types of weapons available in the game
    /// </summary>
    public enum WeaponType
    {
        Sword,
        Bow,
        Spear,
        Hammer,
        Dagger,
        Staff
    }
    
    /// <summary>
    /// Rarity tiers for weapons
    /// </summary>
    public enum WeaponRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
} 