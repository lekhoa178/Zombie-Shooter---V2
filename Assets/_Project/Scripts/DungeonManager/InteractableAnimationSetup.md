# Interactable Animation Setup Guide

This guide will walk you through setting up the enhanced selection animation for interactable objects, featuring a compression-burst text animation with arrows that point to it.

## Overview

The new selection animation includes:
1. Text that compresses at the bottom first, then bursts to normal height with a slight overshoot
2. Left and right arrows that gradually move toward the text
3. Continuous subtle animation while the object remains selected

## Prefab Setup

### Step 1: Create the Text Container

1. Create an empty GameObject as a child of your interactable object
2. Name it "NameTextContainer"
3. Position it above your interactable object (e.g., 1-2 units above)
4. This will be the container for the text and arrows

### Step 2: Add Text Component

1. Create a TextMeshPro text object as a child of the NameTextContainer
2. Name it "NameText"
3. Configure the TextMeshPro component:
   - Set font size (recommended: 4-6)
   - Set alignment to center
   - Set color (recommended: white or a bright color that stands out)
   - Enable outline for better visibility (optional)
4. Position it at (0,0,0) relative to the NameTextContainer

### Step 3: Create Arrow GameObjects

1. Create two empty GameObjects as children of the NameTextContainer:
   - Name them "LeftArrow" and "RightArrow"
2. Add sprite renderers to each arrow
   - Use triangle or arrow sprites pointing toward the center
3. Position them on either side of the text:
   - LeftArrow: Position at (-1, 0, 0) relative to the container
   - RightArrow: Position at (1, 0, 0) relative to the container
4. Scale them appropriately (start small, around 0.2-0.3 units)
5. Ensure the arrows are facing toward the text

### Step 4: Set up Animation References

In your interactable object's component inspector:

1. Assign the NameTextContainer to the "Name Text Container" field
2. Assign the TextMeshPro component to the "Name Text Display" field
3. Assign the LeftArrow to the "Left Arrow" field
4. Assign the RightArrow to the "Right Arrow" field

### Step 5: Configure Animation Parameters

Adjust the following parameters to fine-tune the animation:

1. **Text Compression-Burst Animation:**
   - Name Animation Duration: Duration of the entire animation (recommended: 0.5)
   - Initial Compression: How compressed the text starts (recommended: 0.2, smaller values = more compressed)
   - Bounce Overshoot: How much the text overshoots before settling (recommended: 1.2)
   - Bounce Curve: Animation curve for the bounce effect (default is fine)

2. **Arrow Animation:**
   - Arrow Move Duration: How long it takes arrows to move in (recommended: 0.8)
   - Arrow Start Distance: Initial distance from center (recommended: 1.0)
   - Arrow End Distance: Final distance from center (recommended: 0.2)
   - Arrow Move Curve: Animation curve for arrow movement (default is fine)

## Animation Phases

The compression-burst animation has three distinct phases:

1. **Initial Compression**: The text starts compressed at the bottom of the container
2. **Burst Phase**: The text quickly expands and overshoots its normal height
3. **Settle Phase**: The text settles back to its normal height

These phases create a dynamic, attention-grabbing effect as if the text is springing into existence.

## Additional Customization

### Adjusting Animation Intensity

You can adjust the intensity of the animation by modifying these parameters:

- **Initial Compression**: Lower values (e.g., 0.1) create a more dramatic burst
- **Bounce Overshoot**: Higher values (e.g., 1.5) create a more exaggerated bounce
- **Animation Duration**: Shorter durations create a snappier effect

### Adjusting Arrow Visual Style

For better visual appeal, consider these options for arrows:

1. **Using sprites:**
   - Create arrow sprites in your preferred art style
   - Assign them to the sprite renderers

2. **Using 3D objects:**
   - Create simple arrow meshes
   - Replace the sprite renderers with MeshRenderers
   - Adjust rotation to point toward the text

3. **Using TextMeshPro:**
   - Instead of sprites, use TextMeshPro with arrow characters (e.g., "◄" and "►")
   - Adjust font size and color to match your style

### Animation Timing

- For quick interactions, use shorter durations (0.3-0.5 seconds)
- For important objects, use longer durations (0.8-1.2 seconds)
- Adjust the curves for different easing effects:
  - Linear: Constant speed
  - Ease In: Starts slow, ends fast
  - Ease Out: Starts fast, ends slow
  - Ease In Out: Smooth start and end

## Troubleshooting

### Animation Not Working Properly

If the animation doesn't look right:
1. Check that all transforms are correctly positioned
2. Verify that both position and scale of the NameTextContainer are being properly saved
3. Try resetting the transform values to defaults and repositioning
4. Ensure the Initial Compression value isn't too extreme (should be between 0.1 and 0.5)

### Text Appears Clipped or Cut Off

If the text appears cut off during animation:
1. Make sure the TextMeshPro component has sufficient bounds
2. Check that any parent UI elements have enough space for the animation
3. Increase the size of the text container to accommodate the animation

### Text Not Readable

If the text is hard to read:
1. Increase the font size
2. Add an outline or shadow to the TextMeshPro component
3. Adjust the camera angle or position to better view the text

## Example Prefab Hierarchy

```
InteractableObject (Your object with InteractableBase component)
└── NameTextContainer
    ├── NameText (TextMeshPro)
    ├── LeftArrow
    └── RightArrow
``` 