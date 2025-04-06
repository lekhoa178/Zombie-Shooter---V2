# Enemy Health Bar System

This system provides a flexible and efficient way to display health bars for enemies in your game, using the Flyweight pattern to manage resources efficiently.

## Features

- **World-space health bars** for regular enemies
- **HUD-based health bars** for bosses with responsive layout
- Resource-efficient implementation using the Flyweight pattern
- Customizable appearance and behavior
- Supports multiple boss health bars with responsive layout
- Easy to use with any enemy type
- Both world-space and HUD bars share the same underlying HealthBarFlyweight component

## Setup

### 1. Manager Setup

1. Add the `EnemyHealthBarManager` component to a GameObject in your scene (preferably a manager object).
2. Assign `HealthBarFlyweightSettings` assets:
   - Default settings for regular enemies
   - Boss health bar settings (can be the same or different)
3. **Important**: Assign a UI container for boss health bars:
   - Create a RectTransform in your existing canvas UI
   - Add a VerticalLayoutGroup component for proper layout
   - Assign this RectTransform to the `Boss Health Bar Container` field
   - The system expects you to provide your own canvas

### 2. Creating Health Bar Settings

1. Create HealthBarFlyweightSettings assets for both regular and boss health bars
2. For boss health bars, make sure your prefab includes:
   - A `BossName` TextMeshProUGUI component
   - A `BossIcon` Image component (optional)
   - Additional styling as desired

## Usage

### Regular Enemy Health Bars

To add a health bar to a regular enemy:

```csharp
// Add the component to your enemy
EnemyHealthBarComponent healthBar = enemy.AddComponent<EnemyHealthBarComponent>();

// Customize position if needed
healthBar.PositionOffset = new Vector3(0, 2.5f, 0);

// Initialize (or set autoInitialize = true)
healthBar.InitializeHealthBar();
```

### Boss Health Bars

To use a boss health bar:

```csharp
// Add the component to your boss enemy
EnemyHealthBarComponent healthBar = boss.AddComponent<EnemyHealthBarComponent>();

// Configure it as a boss health bar
healthBar.SetBossHealthBar(true);

// Optional: Set boss-specific properties
healthBar.BossDisplayName = "Mega Boss";
healthBar.BossBarColor = Color.red;
healthBar.BossIcon = bossIconSprite;

// Initialize
healthBar.InitializeHealthBar();
```

You can also toggle between world-space and HUD display at runtime:

```csharp
// Switch to boss bar mode
healthBar.SetBossHealthBar(true);

// Switch back to world-space mode
healthBar.SetBossHealthBar(false);
```

## How It Works

The system uses a single `HealthBarFlyweight` class for both world-space and HUD boss health bars:

1. **World-space mode**: The health bar parented to the enemy transform, follows the enemy, and faces the camera.
2. **Boss HUD mode**: The health bar is placed in a UI container with proper layout, and displays additional boss information.

The `HealthBarFlyweight` adapts its behavior based on which mode it's in, but uses the same core functionality for both modes.

## Canvas Setup for Boss Health Bars

For boss health bars to display properly:

1. Use your existing canvas hierarchy
2. Create a GameObject with a RectTransform in your canvas
3. Add a VerticalLayoutGroup component to this GameObject
4. Configure the layout group with appropriate spacing and padding
5. Assign this GameObject to the "Boss Health Bar Container" field in the EnemyHealthBarManager

## Advanced Options

- **Custom Settings**: You can assign custom `HealthBarFlyweightSettings` to individual health bars.
- **Toggling Visibility**: Use `SetActive(bool)` to show/hide health bars.
- **Position Updates**: Call `UpdatePosition()` when enemies teleport or change position suddenly.
- **Multiple Boss Bars**: The system automatically manages layout for multiple concurrent boss health bars.

## Performance Considerations

- The system automatically pools and reuses health bar objects.
- Health bars are automatically released when enemies are destroyed.
- For large numbers of enemies, consider disabling health bars for distant enemies.
