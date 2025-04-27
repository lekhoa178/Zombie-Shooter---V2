using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SceneManagement
{
    /// <summary>
    /// Provides static utility methods for scene management.
    /// </summary>
    public static class SceneUtilities
    {
        #region Public Methods
        /// <summary>
        /// Loads a scene by name with optional callback.
        /// </summary>
        /// <param name="_sceneName">Scene name to load</param>
        /// <param name="_onComplete">Optional callback when scene load completes</param>
        public static void LoadScene(string _sceneName, Action _onComplete = null)
        {
            if (SceneTransitionManager.HasInstance)
            {
                SceneTransitionManager.Instance.LoadScene(_sceneName, _onComplete);
            }
            else
            {
                Debug.LogWarning("SceneTransitionManager not found. Using direct scene loading instead.");
                SceneManager.LoadScene(_sceneName);
                _onComplete?.Invoke();
            }
        }

        /// <summary>
        /// Loads a scene by build index with optional callback.
        /// </summary>
        /// <param name="_buildIndex">Scene build index to load</param>
        /// <param name="_onComplete">Optional callback when scene load completes</param>
        public static void LoadScene(int _buildIndex, Action _onComplete = null)
        {
            if (SceneTransitionManager.HasInstance)
            {
                SceneTransitionManager.Instance.LoadScene(_buildIndex, _onComplete);
            }
            else
            {
                Debug.LogWarning("SceneTransitionManager not found. Using direct scene loading instead.");
                SceneManager.LoadScene(_buildIndex);
                _onComplete?.Invoke();
            }
        }

        /// <summary>
        /// Reloads the current active scene.
        /// </summary>
        /// <param name="_onComplete">Optional callback when scene reload completes</param>
        public static void ReloadCurrentScene(Action _onComplete = null)
        {
            if (SceneTransitionManager.HasInstance)
            {
                SceneTransitionManager.Instance.ReloadCurrentScene(_onComplete);
            }
            else
            {
                Debug.LogWarning("SceneTransitionManager not found. Using direct scene loading instead.");
                string currentSceneName = SceneManager.GetActiveScene().name;
                SceneManager.LoadScene(currentSceneName);
                _onComplete?.Invoke();
            }
        }

        /// <summary>
        /// Gets the name of the current active scene.
        /// </summary>
        /// <returns>Current scene name</returns>
        public static string GetCurrentSceneName()
        {
            return SceneManager.GetActiveScene().name;
        }

        /// <summary>
        /// Gets the build index of the current active scene.
        /// </summary>
        /// <returns>Current scene build index</returns>
        public static int GetCurrentSceneBuildIndex()
        {
            return SceneManager.GetActiveScene().buildIndex;
        }

        /// <summary>
        /// Checks if a scene with the given name exists in build settings.
        /// </summary>
        /// <param name="_sceneName">Scene name to check</param>
        /// <returns>True if the scene exists in build settings</returns>
        public static bool SceneExists(string _sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                
                if (sceneName == _sceneName)
                {
                    return true;
                }
            }
            
            return false;
        }
        #endregion
    }
} 