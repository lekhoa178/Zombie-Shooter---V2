using UnityEngine;

namespace LocomotionSystem.LootSystem
{
    /// <summary>
    /// Interface for weapon collectables
    /// </summary>
    public interface IWeaponCollectable : ICollectable
    {
        /// <summary>
        /// Weapon configuration containing attack patterns and properties
        /// </summary>
        ComboConfiguration WeaponConfig { get; }
        
        /// <summary>
        /// Visual prefab to display when the weapon is equipped
        /// </summary>
        GameObject WeaponVisualPrefab { get; }
        
        /// <summary>
        /// Weapon rarity tier
        /// </summary>
        WeaponRarity Rarity { get; }
        
        /// <summary>
        /// Weapon type (sword, bow, etc)
        /// </summary>
        WeaponType WeaponType { get; }
    }
    
    public enum WeaponRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
    
    public enum WeaponType
    {
        Sword,
        Bow,
        Spear,
        Hammer,
        Dagger,
        Staff
    }
} 