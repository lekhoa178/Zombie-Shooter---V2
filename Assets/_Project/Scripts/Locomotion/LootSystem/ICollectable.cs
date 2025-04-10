using UnityEngine;

namespace LocomotionSystem.LootSystem
{
    public enum CollectableType
    {
        Weapon,
        Consumable,
        Material,
        Currency,
        Quest
    }

    /// <summary>
    /// Base interface for all collectable items in the game
    /// </summary>
    public interface ICollectable
    {
        /// <summary>
        /// The type of collectable item
        /// </summary>
        CollectableType Type { get; }
        
        /// <summary>
        /// Unique identifier for the collectable
        /// </summary>
        string ID { get; }
        
        /// <summary>
        /// Display name of the collectable
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Visual GameObject representing the collectable in the world
        /// </summary>
        GameObject VisualObject { get; }
        
        /// <summary>
        /// Called when the item is collected by the player
        /// </summary>
        /// <param name="_collector">The GameObject that collected this item</param>
        /// <returns>True if collection was successful</returns>
        bool OnCollected(GameObject _collector);
        
        /// <summary>
        /// Register this collectable with the loot manager
        /// </summary>
        void Register();
        
        /// <summary>
        /// Unregister this collectable from the loot manager
        /// </summary>
        void Unregister();
    }
} 