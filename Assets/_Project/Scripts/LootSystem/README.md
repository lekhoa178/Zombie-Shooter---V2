# Loot System Documentation

## Overview

The Loot System provides a complete framework for item collection, management, and weapon switching in the game. It supports both automatic collection (auto-collect when nearby) and manual collection (press button to collect). The system is designed to be flexible, extensible, and performs well with smooth animations.

## Architecture

```
┌─────────────────────────────────────────────────────┐
│                  Single Interface                    │
│                                                     │
│               ┌──────────────────┐                  │
│               │   ICollectable   │                  │
│               └────────┬─────────┘                  │
│                        │                            │
└────────────────────────┼────────────────────────────┘
                         │
                         │ implements
                         ▼
                ┌─────────────────────┐
                │   CollectableBase   │
                │   (Abstract Class)  │
                └─────────┬───────────┘
                          │
                          │ extends
                          ▼
                ┌─────────────────────┐
                │                     │
                │  WeaponCollectable  │◄───────┐
                │                     │        │
                └─────────────────────┘        │
                          │                    │
                          │ registers with     │ creates
                          ▼                    │
                ┌─────────────────┐     ┌──────────────┐
                │                 │     │              │
                │  LootManager    │     │WeaponFactory │
                │                 │     │              │
                └─────────────────┘     └──────────────┘
                   △             △
                   │             │
          collects │             │ manages weapons
                   │             │
                ┌─────────────────┐
                │                 │
                │     Player      │
                │                 │
                └─────────────────┘
```

### Core Components

#### Interfaces and Data

- `ICollectable`: Single unified interface for all collectables with a `GetTypeData<T>()` method
- `WeaponData`: Type-specific data container for weapons

#### Base Classes

- `CollectableBase`: Abstract base implementation with common functionality for collectables
  - Implements `ICollectable` interface
  - Handles registration with managers
  - Provides bezier curve movement to player
  - Manages visual effects and animations

#### Concrete Implementations

- `WeaponCollectable`: Weapon implementation that extends `CollectableBase`
  - Manages weapon-specific data through `WeaponData`
  - Handles rarity VFX and coloring

#### Managers

- `LootManager`: Singleton that manages collectable item registration and collection
  - Handles both automatic and manual collection
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

## Type Discriminator Pattern

The system uses a "Type Discriminator" pattern with these key components:

1. **Single Interface with Type Property**: All collectables implement the `ICollectable` interface, which includes a `Type` property.

2. **GetTypeData<T>() Method**: The interface provides a generic method to retrieve type-specific data.

3. **Type-Specific Data Classes**: Each collectable type has its own data class (e.g., `WeaponData`).

4. **Type-Safe Access**: Managers and consumers can safely access type-specific data by checking the type first:

```csharp
if (collectable.Type == CollectableType.Weapon)
{
    WeaponData weaponData = collectable.GetTypeData<WeaponData>();
    if (weaponData != null)
    {
        // Use weapon data
    }
}
```

## Setup Guide

### 1. Create Required Assets

Create the following assets in your project:

- Weapon Factory asset (`WeaponFactory` ScriptableObject)
- Default combo configurations for weapons
- Weapon visual prefabs
- VFX prefabs for different rarity levels

### 2. Create a Weapon Collectable Prefab

1. Create a new GameObject in your scene
2. Add a Collider component (configure as trigger)
3. Add the `WeaponCollectable` script
4. Configure the basic properties:
   - Set appropriate collection radius values
   - Set hover height and bob animation settings
   - Assign visual references
5. Create a nested GameObject for the visual representation
6. Add effects as needed (particles, glows, etc.)
7. Convert to prefab by dragging to your project

### 3. Setup Scene Components

Add the following to your scene:

1. Create a GameObject and add the `LootManager` component
2. Create a GameObject and add the `WeaponManager` component
3. Configure layer settings for collectables (recommended: create a "Collectable" layer)

### 4. Player Integration

Ensure your Player class has:

- Weapon switching input actions configured
- Collection input action configured

### 5. Enemy Integration

Add `LootSpawner` component to enemies or other objects that should spawn loot:

- Configure drop rates and quantities
- Assign the WeaponFactory reference
- If using death events, implement the `IHealth` interface

## Collection Methods

The system supports two primary methods of item collection:

### Automatic Collection (formerly Passive Collection)

- Items within the auto-collection radius automatically move toward the player
- Items use a smooth bezier curve animation when moving to the player
- No player input required - just walk near items to collect them
- Controlled by `m_EnableAutoCollection` in LootManager

### Manual Collection (formerly Active Collection)

- Player presses a designated button to collect items
- Items within the manual collection radius are instantly collected
- Provides more player control over when to pick up items
- Controlled by `m_EnableManualCollection` in LootManager

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

### Accessing Weapon Data

```csharp
// Check if collectable is a weapon and get its data
if (collectable.Type == CollectableType.Weapon)
{
    WeaponData weaponData = collectable.GetTypeData<WeaponData>();
    if (weaponData != null)
    {
        // Access weapon properties
        ComboConfiguration config = weaponData.Config;
        WeaponType type = weaponData.Type;
        WeaponRarity rarity = weaponData.Rarity;

        // Use the properties
        Debug.Log($"Found {rarity} {type} weapon!");
    }
}
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
ICollectable equippedWeapon = player.GetEquippedWeapon();

// Get weapon inventory
List<ICollectable> inventory = player.GetWeaponInventory();

// Get weapon data for equipped weapon
WeaponData weaponData = player.GetWeaponData(equippedWeapon);
```

## Extending the System

### Adding New Collectable Types

1. Add a new enum value to `CollectableType`
2. Create a new data class for the type (e.g., `ConsumableData`)
3. Create a concrete implementation that extends `CollectableBase`
4. Override `GetTypeData<T>()` to return your new data type
5. Add the new type to the `LootSpawner` and `LootManager` systems

For example, to add a consumable item type:

```csharp
// 1. Add enum value to CollectableType
public enum CollectableType
{
    Weapon,
    Consumable,  // New type
    Material,
    Currency,
    Quest
}

// 2. Create data class
public class ConsumableData
{
    public float HealthAmount { get; set; }
    public float Duration { get; set; }
    public BuffType BuffType { get; set; }
}

// 3. Create implementation
public class ConsumableCollectable : CollectableBase
{
    [SerializeField] private float m_HealthAmount;
    [SerializeField] private float m_Duration;
    [SerializeField] private BuffType m_BuffType;

    private ConsumableData m_ConsumableData;

    protected override void Awake()
    {
        base.Awake();

        // Set type
        var typeField = typeof(CollectableBase).GetField("m_Type",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (typeField != null)
        {
            typeField.SetValue(this, CollectableType.Consumable);
        }

        // Initialize data
        m_ConsumableData = new ConsumableData
        {
            HealthAmount = m_HealthAmount,
            Duration = m_Duration,
            BuffType = m_BuffType
        };
    }

    // 4. Override GetTypeData
    public override T GetTypeData<T>()
    {
        if (typeof(T) == typeof(ConsumableData))
        {
            return m_ConsumableData as T;
        }

        return base.GetTypeData<T>();
    }
}
```

### Customizing Collection Behavior

The system provides several customization points:

- Collection radius for automatic and manual collection
- Collection animation duration and curve
- Visual effects for different rarity tiers

## Troubleshooting

### Weapons Not Being Collected

- Check that the `LootManager` is properly initialized
- Verify collectables are on the correct layer
- Check collection radius settings

### Weapon Visuals Not Appearing

- Ensure weapon visual prefabs are correctly assigned
- Check that the weapon is properly tagged as "EquippedWeapon"
- Verify the weapon's transform hierarchy

### Type Data Not Being Retrieved

- Ensure the correct type check is performed before calling `GetTypeData<T>()`
- Check that the data class type matches exactly what you're requesting
- Verify that the data is properly initialized in the `Awake()` method

## Events and Integration

The system provides events for integration with other systems:

```csharp
// Weapon Manager Events
WeaponManager.Instance.OnWeaponEquipped += HandleWeaponEquipped;
WeaponManager.Instance.OnWeaponAdded += HandleWeaponAdded;
WeaponManager.Instance.OnWeaponRemoved += HandleWeaponRemoved;
```

## Benefits of This Architecture

1. **Simple Interface Hierarchy**: A single, unified interface for all collectables
2. **Type Safety**: Safe type casting through the `GetTypeData<T>()` pattern
3. **Extensibility**: Easy to add new collectable types without changing interfaces
4. **Code Organization**: Clear separation between common functionality and type-specific data
5. **Performance**: Minimal overhead from type checking and retrieval
