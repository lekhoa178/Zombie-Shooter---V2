# UITabSwitcher Component

A flexible, reusable component for creating tabbed UI interfaces in Unity with smooth fade animations.

## Features

- Tab-based content switching with fade in/out animations
- Automatic setup of necessary components
- Customizable animation timing and easing
- Visual tab button state feedback
- Custom editor with validation and utilities
- Extensible architecture for custom implementations

## Dependencies

This component requires the DOTween package for animations. Make sure it's imported in your project.

## Basic Setup

1. Create your UI hierarchy as follows:

```
Switcher (GameObject with UITabSwitcher component)
    ├── Tab Container (Transform)
    │   ├── Button 1
    │   ├── Button 2
    │   └── ... Button N
    └── Content Container (Transform)
        ├── Content Panel 1
        ├── Content Panel 2
        └── ... Content Panel N
```

2. Add the `UITabSwitcher` component to the Switcher GameObject.
3. Assign the Tab Container and Content Container references in the inspector.
4. Make sure the number of tabs and content panels match.
5. Adjust animation settings as needed.

## Important Notes

- Each tab button must have a Button component attached.
- Content panels will automatically have CanvasGroup components added if missing.
- The number of buttons should match the number of content panels.
- Make sure the order of buttons matches the desired order of content panels.

## Advanced Usage

### Extending UITabSwitcher

You can create your own tab switcher variations by extending the `UITabSwitcher` class. See `UITabSwitcherExample.cs` for an example implementation with:

- Custom tab titles
- Audio feedback
- Button scale animations
- Tab title display

### Editor Utilities

The custom editor provides:

- Validation of the tab and content panel counts
- Clear indication of setup issues
- One-click addition of missing CanvasGroup components

## Usage Tips

1. For improved performance, disable gameobjects that are not currently visible in addition to using the fade animations.
2. Consider using a content pooling approach for complex tab content.
3. For touch interfaces, add swipe detection to change tabs.

## Example Code

```csharp
// Get a reference to the tab switcher
UITabSwitcher tabSwitcher = GetComponent<UITabSwitcher>();

// Switch to a specific tab
tabSwitcher.SwitchToTab(2); // Switch to the third tab (zero-indexed)
```
