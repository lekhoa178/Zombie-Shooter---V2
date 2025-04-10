# Loot System Documentation

## Overview

The Loot System provides a complete framework for item collection, management, and weapon switching in the game. It supports both passive collection (auto-collect when nearby) and active collection (press button to collect). The system is designed to be flexible, extensible, and performs well with smooth animations.

## Architecture

### Core Components

#### Interfaces

- `ICollectable`: Base interface for all collectables
- `IWeaponCollectable`: Interface for weapon collectables with specialized properties
- `IHealth`: Interface for entities that can die and spawn loot

#### Base Classes

- `CollectableBase`: Abstract base implementation with common functionality for collectables
  - Handles registration with managers
  - Provides bezier curve movement to player
  - Manages visual effects and animations

#### Concrete Implementations

- `WeaponCollectable`: Implementation for weapon collectables
  - Manages weapon-specific properties
  - Handles rarity VFX and coloring

#### Managers

- `LootManager`: Singleton that manages collectable item registration and collection
  - Handles both passive and active collection
  - Tracks all collectables in the scene
  - Provides proximity-based collection
- `WeaponManager`: Manages weapons and player weapon inventory
  - Tracks player weapon inventory
  - Handles weapon switching
  - Applies weapon configurations to player
- `WeaponFactory`: Factory for creating weapon collectables
  - Creates pre-configured weapons
  - Supports random weapon generation with rarity weighting
- `LootSpawner`: Component for spawning loot when triggered
  - Hooks into entity death events
  - Configurable drop rates and quantities
  - Spawns loot with position randomization

### Data Types

- `CollectableType`: Enum of different collectable item types
- `WeaponRarity`: Enum for weapon rarity tiers
- `WeaponType`: Enum for different weapon types

## Setup Guide

### 1. Create Required Assets

Create the following assets in your project:

- Weapon Factory asset (`WeaponFactory` ScriptableObject)
- Default combo configurations for weapons
- Weapon visual prefabs
- VFX prefabs for different rarity levels

### 2. Setup Scene Components

Add the following to your scene:

1. Create a GameObject and add the `LootManager` component
2. Create a GameObject and add the `WeaponManager` component
3. Configure layer settings for collectables (recommended: create a "Collectable" layer)

### 3. Player Integration

Ensure your Player class has:

- Weapon switching input actions configured
- Collection input action configured

### 4. Enemy Integration

Add `LootSpawner` component to enemies or other objects that should spawn loot:

- Configure drop rates and quantities
- Assign the WeaponFactory reference
- If using death events, implement the `IHealth` interface

## Usage Examples

### Creating a Weapon Collectable Manually

```csharp
// Get reference to weapon factory
WeaponFactory factory = Resources.Load<WeaponFactory>("WeaponFactory");

// Create a specific weapon
ComboConfiguration swordConfig = Resources.Load<ComboConfiguration>("SwordComboConfig");
GameObject sword = factory.CreateWeaponCollectable(
    position: new Vector3(0, 1, 0),
    _weaponConfig: swordConfig,
    _type: WeaponType.Sword,
    _rarity: WeaponRarity.Rare
);

// Create a random weapon
GameObject randomWeapon = factory.CreateRandomWeaponCollectable(
    _position: new Vector3(5, 1, 0),
    _minRarity: WeaponRarity.Uncommon,
    _maxRarity: WeaponRarity.Epic
);
```

### Setting Up Enemy Drops

```csharp
// Get the LootSpawner component
LootSpawner lootSpawner = GetComponent<LootSpawner>();

// Configure drop settings
lootSpawner.m_DropChance = 0.75f;
lootSpawner.m_MinDrops = 1;
lootSpawner.m_MaxDrops = 3;

// Manually trigger drops
lootSpawner.SpawnLoot();

// Spawn a specific weapon
lootSpawner.SpawnSpecificWeapon(swordConfig, null, WeaponType.Sword, WeaponRarity.Epic);
```

### Player Weapon Switching

```csharp
// Switch to weapon in slot 0
player.SwitchWeapon(0);

// Get currently equipped weapon
IWeaponCollectable equipped = player.GetEquippedWeapon();

// Get weapon inventory
List<IWeaponCollectable> inventory = player.GetWeaponInventory();
```

## Extending the System

### Adding New Collectable Types

1. Add a new enum value to `CollectableType`
2. Create a new interface that inherits from `ICollectable`
3. Create a concrete implementation that extends `CollectableBase`
4. Add the new type to the `LootSpawner` and `LootManager` systems

### Customizing Collection Behavior

The system provides several customization points:

- Collection radius for passive and active collection
- Collection animation duration and curve
- Visual effects for different rarity tiers

### Adding New Weapon Types

1. Add a new enum value to `WeaponType`
2. Create required visual assets
3. Create a new ComboConfiguration asset

## Troubleshooting

### Weapons Not Being Collected

- Check that the `LootManager` is properly initialized
- Verify collectables are on the correct layer
- Check collection radius settings

### Weapon Visuals Not Appearing

- Ensure weapon visual prefabs are correctly assigned
- Check that the weapon is properly tagged as "EquippedWeapon"
- Verify the weapon's transform hierarchy

### Performance Considerations

- Use object pooling for frequently spawned collectables
- Adjust passive collection check interval based on performance needs
- Consider reducing VFX complexity for lower-end devices

## Events and Integration

The system provides events for integration with other systems:

```csharp
// Weapon Manager Events
WeaponManager.Instance.OnWeaponEquipped += HandleWeaponEquipped;
WeaponManager.Instance.OnWeaponAdded += HandleWeaponAdded;
WeaponManager.Instance.OnWeaponRemoved += HandleWeaponRemoved;
```

---

## Advanced Usage

### Custom Collection Effects

You can customize the collection animation by overriding the `MoveToPlayer` method in derived classes:

```csharp
public override void MoveToPlayer(Transform _playerTransform, Action _onComplete = null)
{
    // Custom collection animation
    // Call base implementation or create entirely new behavior
}
```

### Custom Weapon Effects

For specialized weapon effects, extend the `WeaponCollectable` class:

```csharp
public class ElementalWeaponCollectable : WeaponCollectable
{
    [SerializeField] private ElementType m_ElementType;
    [SerializeField] private GameObject m_ElementalVfx;

    // Override methods to add elemental effects
}
```
