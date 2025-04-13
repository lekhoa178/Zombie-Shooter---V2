using UnityEngine;

namespace LocomotionSystem
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
        /// <param name="collector">The GameObject that collected this item</param>
        /// <returns>True if collection was successful</returns>
        bool OnCollected(GameObject collector);
        
        /// <summary>
        /// Register this collectable with the loot manager
        /// </summary>
        void Register();
        
        /// <summary>
        /// Unregister this collectable from the loot manager
        /// </summary>
        void Unregister();
        
        /// <summary>
        /// Get type-specific data for this collectable
        /// </summary>
        /// <typeparam name="T">The type of data to retrieve</typeparam>
        /// <returns>Data of the specified type, or null if not applicable</returns>
        T GetTypeData<T>() where T : class;
        
        /// <summary>
        /// Called when this collectable is selected by the player
        /// </summary>
        void OnSelected();
        
        /// <summary>
        /// Called when this collectable is no longer selected by the player
        /// </summary>
        void OnUnselected();
    }
} 