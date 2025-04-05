# World Space Health Bar System

This README provides instructions for setting up the efficient world space health bar system for enemies.

## Prefab Setup

1. Create a new prefab with the following hierarchy:

```
HealthBarCanvas (GameObject)
└── Canvas (Canvas component)
    └── HealthBarContainer (RectTransform)
        ├── Background (Image)
        ├── Fill (Image)
        └── HealthText (Text)
```

2. Configure the components:

### HealthBarCanvas

- Add a `Canvas` component
- Set Render Mode to `World Space`
- Set Sort Order to a higher number (e.g. 10) to ensure it renders on top

### Canvas

- Add a `CanvasScaler` component
  - Set UI Scale Mode to `Scale With Screen Size`
  - Set Reference Resolution to match your project (e.g. 1920x1080)
- Add a `CanvasGroup` component
- Set the Canvas size to something small (e.g. width=1, height=0.25)

### HealthBarContainer

- Position at (0,0,0) relative to Canvas
- Anchors: set to stretch horizontally, but fixed vertically
- Pivot: (0.5, 0.5)

### Background

- Add an `Image` component
- Set color to dark gray with alpha (e.g. RGBA: 30,30,30,200)
- Stretch to fill container

### Fill

- Add an `Image` component
- Set Image Type to `Filled`
- Set Fill Method to `Horizontal`
- Set Fill Origin to `Left`
- Set color to green (e.g. RGB: 50,200,50)
- Stretch to fill container with small margins

### HealthText (optional)

- Add a `Text` component
- Set font, alignment (center), and color (white)
- Set appropriate font size (e.g. 12-14)
- Stretch to fill container

3. Add the `WorldSpaceHealthBar` component to the root object
4. Link the component references:

   - Background Image: assign the Background object's Image component
   - Fill Image: assign the Fill object's Image component
   - Health Text: assign the HealthText object's Text component
   - Canvas Group: assign the Canvas's CanvasGroup component

5. Save as prefab in the UI folder

## Manager Setup

1. Create an empty GameObject in your scene and name it "HealthBarManager"
2. Add the `HealthBarManager` component to it
3. Assign the health bar prefab to the "Health Bar Prefab" field
4. Set the initial pool size (20 is a good default)
5. Check "Expand Pool If Needed" to allow dynamic growth
6. Optional: Create an empty child GameObject named "HealthBars" and assign it to "Health Bar Parent"

## Enemy Setup

1. Add the `HealthBarComponent` to any enemy that should display a health bar
2. Configure the offset if needed (default is 2 units above the enemy)
3. Check "Auto Initialize" to automatically create the health bar when the enemy spawns

## Performance Notes

- This system efficiently pools health bars to reduce instantiation costs
- Health bars only update when health changes
- Health bars automatically disable when enemies are off-screen or too far away
- The system uses world space UI, which performs much better than OnGUI for many enemies

With this setup, your game should be able to handle dozens of enemies with health bars on screen simultaneously with minimal performance impact.
