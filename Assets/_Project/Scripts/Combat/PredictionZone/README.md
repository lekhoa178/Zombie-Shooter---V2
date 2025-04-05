# Prediction Zone System Documentation

## Overview
The Prediction Zone system is a flexible and extensible visual effects framework for Unity that provides a variety of visual feedback mechanisms based on progress values. It's designed to be modular, maintainable, and easy to use.

## Core Components

### 1. ProgressionVisual
The main component that manages all visual effects for a prediction zone.

#### Features
- Manages multiple visual effects
- Handles progress updates
- Provides event system for state changes
- Includes validation and error handling
- Supports automatic component detection

#### Events
```csharp
public UnityEvent<float> onProgressChanged;        // Triggered when progress changes
public UnityEvent<PredictionZoneEffect> onEffectEnabled;    // Triggered when an effect is enabled
public UnityEvent<PredictionZoneEffect> onEffectDisabled;   // Triggered when an effect is disabled
public UnityEvent onVisualReset;                   // Triggered when visual is reset
public UnityEvent onEffectsInitialized;            // Triggered when effects are initialized
public UnityEvent<string> onError;                 // Triggered when an error occurs
```

### 2. PredictionZoneEffect (Base Class)
Abstract base class for all visual effects in the system.

#### Features
- Common functionality for all effects
- State management
- Event system
- Validation framework

## Visual Effects

### 1. ColorEffect
Handles color gradient transitions based on progress.

#### Features
- Color gradient support
- Material property block optimization
- Renderer component targeting

### 2. ScaleEffect
Handles scale animations based on progress.

#### Features
- Animation curve support
- Axis-specific scaling
- Initial scale preservation

### 3. RotationEffect
Handles continuous rotation animations.

#### Features
- Configurable rotation speed
- Axis selection
- Continuous rotation

### 4. PulseEffect
Handles pulsing scale animations.

#### Features
- Configurable pulse speed
- Scale multiplier control
- Initial scale preservation

### 5. ParticleEffect
Controls particle system properties based on progress.

#### Features
- Emission rate control
- Particle size control
- Color gradient support
- Multiple particle system module control

### 6. TrailEffect
Manages trail renderer properties based on progress.

#### Features
- Width control
- Color gradient support
- Fade time control
- Min/max width settings

### 7. LineEffect
Controls line renderer properties based on progress.

#### Features
- Width control
- Color gradient support
- Position control
- Multiple position support

### 8. ProgressBarEffect
Manages UI progress bar elements.

#### Features
- Multiple UI component support
- Animation options
- Spring physics support
- Size and position animation
- Color gradient support
- Format string customization

## Usage Examples

### Basic Setup
```csharp
// Add ProgressionVisual to a GameObject
var progressionVisual = gameObject.AddComponent<ProgressionVisual>();

// Add desired effects
var colorEffect = gameObject.AddComponent<ColorEffect>();
var scaleEffect = gameObject.AddComponent<ScaleEffect>();

// Configure effects
colorEffect.colorGradient = new Gradient();
scaleEffect.scaleCurve = AnimationCurve.Linear(0, 0, 1, 1);

// Update progress
progressionVisual.UpdateProgression(0.5f);
```

### Event Handling
```csharp
// Listen for progress changes
progressionVisual.onProgressChanged.AddListener((progress) => {
    Debug.Log($"Progress: {progress}");
});

// Listen for effect state changes
progressionVisual.onEffectEnabled.AddListener((effect) => {
    Debug.Log($"Effect enabled: {effect.GetType().Name}");
});

// Listen for errors
progressionVisual.onError.AddListener((error) => {
    Debug.LogError($"Error: {error}");
});
```

### Progress Bar Setup
```csharp
// Add ProgressBarEffect to a UI GameObject
var progressBar = gameObject.AddComponent<ProgressBarEffect>();
progressBar.barType = ProgressBarType.Image;
progressBar.targetImage = GetComponent<Image>();

// Configure animation
progressBar.useSpringAnimation = true;
progressBar.springSettings.damping = 10f;
progressBar.springSettings.stiffness = 100f;

// Configure visual settings
progressBar.progressColorGradient = new Gradient();
progressBar.showPercentage = true;
progressBar.percentageFormat = "{0}%";
```

## Best Practices

1. **Component Validation**
   - Always check for required components
   - Use the built-in validation system
   - Enable error logging for debugging

2. **Performance**
   - Use MaterialPropertyBlock for renderer effects
   - Enable spring animation for smooth transitions
   - Disable unused effects

3. **Error Handling**
   - Subscribe to error events
   - Use try-catch blocks for critical operations
   - Validate component references

4. **Event Management**
   - Clean up event listeners in OnDestroy
   - Use appropriate event types
   - Handle state changes properly

## Validation and Error Handling

The system includes comprehensive validation and error handling:

1. **Component Validation**
   - Automatic component detection
   - Required component checking
   - Invalid component handling

2. **Error Reporting**
   - Detailed error messages
   - Error event system
   - Logging options

3. **State Management**
   - Effect state tracking
   - Progress value validation
   - Initialization checks

## Performance Considerations

1. **Optimization Features**
   - MaterialPropertyBlock usage
   - Efficient event handling
   - Smart update system

2. **Resource Management**
   - Proper cleanup
   - Event listener management
   - Component reference tracking

## Extensibility

The system is designed to be easily extended:

1. **New Effects**
   - Inherit from PredictionZoneEffect
   - Implement required methods
   - Add custom validation

2. **Custom Behaviors**
   - Override base methods
   - Add custom events
   - Implement custom validation

## Dependencies

- Unity 2019.4 or later
- TextMeshPro (for text effects)
- UnityEngine.UI (for UI effects)

## Notes

- All progress values should be between 0 and 1
- Effects can be enabled/disabled independently
- The system automatically handles cleanup
- Validation errors are logged by default
- Spring animation provides smooth transitions

## Troubleshooting

### Common Issues

1. **Missing Components**
   - Check if required components are attached
   - Enable error logging for detailed messages
   - Use auto-disable option for invalid components

2. **Performance Issues**
   - Disable unused effects
   - Use MaterialPropertyBlock for renderer effects
   - Optimize update frequency

3. **Visual Glitches**
   - Check animation curves
   - Validate progress values
   - Ensure proper cleanup

### Debug Tips

1. **Enable Logging**
   ```csharp
   progressionVisual.logValidationErrors = true;
   ```

2. **Check Component State**
   ```csharp
   if (effect != null && effect.isEnabled)
   {
       // Effect is valid and active
   }
   ```

3. **Monitor Events**
   ```csharp
   progressionVisual.onError.AddListener((error) => {
       Debug.LogError($"[ProgressionVisual] {error}");
   });
   ```

## Performance Optimization Tips

1. **Renderer Effects**
   - Use MaterialPropertyBlock for better performance
   - Batch similar effects together
   - Disable effects when not visible

2. **UI Effects**
   - Use canvas groups for batch updates
   - Optimize update frequency
   - Use spring animation for smooth transitions

3. **Particle Effects**
   - Control particle count
   - Use emission rate curves
   - Optimize particle lifetime

4. **General Tips**
   - Disable unused effects
   - Use object pooling where appropriate
   - Optimize update frequency
   - Cache component references
   - Use proper cleanup methods 