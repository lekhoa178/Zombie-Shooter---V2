using UnityEngine;

namespace LocomotionSystem
{
    public enum InteractableType
    {
        Weapon,
        Consumable,
        Material,
        Currency,
        Quest
    }

    /// <summary>
    /// Base interface for all interactable items in the game
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// The type of interactable item
        /// </summary>
        InteractableType Type { get; }
        
        /// <summary>
        /// Unique identifier for the interactable
        /// </summary>
        string ID { get; }
        
        /// <summary>
        /// Display name of the interactable
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Visual GameObject representing the interactable in the world
        /// </summary>
        GameObject VisualObject { get; }
        
        /// <summary>
        /// Called when the item is collected by the player
        /// </summary>
        /// <param name="collector">The GameObject that collected this item</param>
        /// <returns>True if collection was successful</returns>
        bool OnInteract(GameObject collector);
        
        /// <summary>
        /// Register this interactable with the loot manager
        /// </summary>
        void Register();
        
        /// <summary>
        /// Unregister this interactable from the loot manager
        /// </summary>
        void Unregister();
        
        /// <summary>
        /// Get type-specific data for this interactable
        /// </summary>
        /// <typeparam name="T">The type of data to retrieve</typeparam>
        /// <returns>Data of the specified type, or null if not applicable</returns>
        T GetTypeData<T>() where T : class;
        
        /// <summary>
        /// Called when this interactable is selected by the player
        /// </summary>
        void OnSelected();
        
        /// <summary>
        /// Called when this interactable is no longer selected by the player
        /// </summary>
        void OnUnselected();
    }
} 