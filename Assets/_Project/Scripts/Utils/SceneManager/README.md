# Scene Transition Manager

A modular scene management system for Unity that supports smooth scene transitions with loading screens, progress bars, and customizable tips.

## Features

- Smooth transitions between scenes with fade-in/fade-out effects
- Real-time progress tracking during scene loading
- Display random tips during loading
- Minimum loading time option to avoid flashing screens
- Support for callbacks after scene loading completes
- Fallback to direct scene loading if manager isn't available
- Easy-to-use static utility methods

## Setup

### Automatic Setup

1. In Unity Editor, navigate to **Tools > Project A > Create Loading Screen Prefab**
2. The system will automatically create a fully configured SceneTransitionManager prefab in `Assets/_Project/Prefabs/Managers`
3. Drag the prefab into your first scene
4. The prefab will persist across all scenes (using DontDestroyOnLoad)

### Manual Setup

If you prefer to set up the system manually or customize the UI:

1. Create a new GameObject and add the `SceneTransitionManager` component
2. Create child UI elements for the loading screen:
   - Canvas with CanvasGroup (for fading)
   - Progress bar (using Image component with Image.FillAmount)
   - Progress text (TextMeshProUGUI)
   - Tips text (TextMeshProUGUI)
3. Assign the references in the SceneTransitionManager inspector
4. Add loading tips strings in the inspector array

## Usage

### Basic Scene Loading

The simplest way to load a scene is to use the static utility methods:

```csharp
// Load scene by name
SceneUtilities.LoadScene("MainMenu");

// Load scene by build index
SceneUtilities.LoadScene(2);

// Reload the current scene
SceneUtilities.ReloadCurrentScene();
```

### With Callbacks

You can execute code after scene loading completes:

```csharp
SceneUtilities.LoadScene("GameLevel", () => {
    Debug.Log("Scene load completed!");
    // Initialize level-specific systems
    GameManager.Instance.StartLevel();
});
```

### Direct Access

You can also access the manager instance directly:

```csharp
if (SceneTransitionManager.HasInstance)
{
    SceneTransitionManager.Instance.LoadScene("BossLevel", OnBossLevelLoaded);
}

private void OnBossLevelLoaded()
{
    // Setup boss fight
    BossManager.Instance.SpawnBoss();
}
```

### Other Utilities

```csharp
// Get current scene name
string sceneName = SceneUtilities.GetCurrentSceneName();

// Get current scene build index
int buildIndex = SceneUtilities.GetCurrentSceneBuildIndex();

// Check if a scene exists in build settings
bool exists = SceneUtilities.SceneExists("SecretLevel");
```

## Performance Considerations

- The SceneTransitionManager creates a persistent canvas that stays loaded across all scenes
- For extremely memory-constrained platforms, consider creating a simplified version without UI effects
- The system uses coroutines for async loading and smooth transitions

## Customization

You can customize the following aspects through the inspector:

- Loading screen UI elements
- Fade duration
- Minimum loading time
- Loading tips
