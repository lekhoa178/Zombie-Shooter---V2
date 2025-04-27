using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityUtils;

namespace SceneManagement
{
    /// <summary>
    /// Manages scene transitions with loading screens and progress tracking.
    /// Attach to a GameObject that persists across scenes.
    /// </summary>
    public class SceneTransitionManager : PersistentSingleton<SceneTransitionManager>
    {
        #region Serialized Fields
        [Header("Loading Screen References")]
        [SerializeField] private Canvas m_LoadingScreenCanvas;
        [SerializeField] private Image m_ProgressBar;
        [SerializeField] private TextMeshProUGUI m_ProgressText;
        [SerializeField] private TextMeshProUGUI m_TipText;
        [SerializeField] private CanvasGroup m_FadeCanvasGroup;
        
        [Header("Configuration")]
        [SerializeField] private string[] m_LoadingTips;
        [SerializeField, Range(0.1f, 3f)] private float m_FadeDuration = 0.5f;
        [SerializeField, Range(0f, 1f)] private float m_MinimumLoadTime = 0.5f;
        [SerializeField] private bool m_ShowRandomTips = true;
        #endregion

        #region Private Fields
        private bool m_IsLoading = false;
        private Dictionary<string, int> m_SceneIndices = new Dictionary<string, int>();
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            InitializeSceneIndices();
            HideLoadingScreen();
        }
        #endregion

        #region Private Methods
        private void InitializeSceneIndices()
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                m_SceneIndices[sceneName] = i;
            }
        }

        private void ShowLoadingScreen()
        {
            m_LoadingScreenCanvas.gameObject.SetActive(true);
            m_IsLoading = true;
            
            if (m_ShowRandomTips && m_LoadingTips != null && m_LoadingTips.Length > 0)
            {
                m_TipText.text = m_LoadingTips[UnityEngine.Random.Range(0, m_LoadingTips.Length)];
            }
        }

        private void HideLoadingScreen()
        {
            m_LoadingScreenCanvas.gameObject.SetActive(false);
            m_IsLoading = false;
        }

        private IEnumerator LoadSceneAsync(string _sceneName, Action _onComplete = null)
        {
            ShowLoadingScreen();
            
            // Fade in
            yield return StartCoroutine(FadeCanvasGroup(m_FadeCanvasGroup, 0f, 1f, m_FadeDuration));
            
            // Start scene loading
            AsyncOperation asyncOperation;
            
            if (m_SceneIndices.TryGetValue(_sceneName, out int sceneIndex))
            {
                asyncOperation = SceneManager.LoadSceneAsync(sceneIndex);
            }
            else
            {
                Debug.LogWarning($"Scene '{_sceneName}' not found in build settings. Attempting to load by name.");
                asyncOperation = SceneManager.LoadSceneAsync(_sceneName);
            }
            
            asyncOperation.allowSceneActivation = false;
            
            float startTime = Time.time;
            float progress = 0f;
            
            // Update progress while scene loads
            while (progress < 0.9f)
            {
                progress = Mathf.Clamp01(asyncOperation.progress / 0.9f);
                m_ProgressBar.fillAmount = progress;
                m_ProgressText.text = $"{Mathf.Round(progress * 100)}%";
                yield return null;
            }
            
            // Ensure minimum loading time for better UX
            float elapsedTime = Time.time - startTime;
            if (elapsedTime < m_MinimumLoadTime)
            {
                yield return new WaitForSeconds(m_MinimumLoadTime - elapsedTime);
            }
            
            // Complete loading
            m_ProgressBar.fillAmount = 1f;
            m_ProgressText.text = "100%";
            
            // Activate the scene
            asyncOperation.allowSceneActivation = true;
            
            // Wait for scene activation
            while (!asyncOperation.isDone)
            {
                yield return null;
            }
            
            // Fade out
            yield return StartCoroutine(FadeCanvasGroup(m_FadeCanvasGroup, 1f, 0f, m_FadeDuration));
            
            HideLoadingScreen();
            
            _onComplete?.Invoke();
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup _canvasGroup, float _startAlpha, float _targetAlpha, float _duration)
        {
            float elapsedTime = 0f;
            
            while (elapsedTime < _duration)
            {
                _canvasGroup.alpha = Mathf.Lerp(_startAlpha, _targetAlpha, elapsedTime / _duration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            _canvasGroup.alpha = _targetAlpha;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Loads a scene by name with optional callback when complete.
        /// </summary>
        /// <param name="_sceneName">Scene name to load</param>
        /// <param name="_onComplete">Optional callback when scene load completes</param>
        public void LoadScene(string _sceneName, Action _onComplete = null)
        {
            if (m_IsLoading)
            {
                Debug.LogWarning("Scene transition already in progress. Ignoring request.");
                return;
            }
            
            StartCoroutine(LoadSceneAsync(_sceneName, _onComplete));
        }

        /// <summary>
        /// Loads a scene by build index with optional callback when complete.
        /// </summary>
        /// <param name="_buildIndex">Scene build index to load</param>
        /// <param name="_onComplete">Optional callback when scene load completes</param>
        public void LoadScene(int _buildIndex, Action _onComplete = null)
        {
            if (m_IsLoading)
            {
                Debug.LogWarning("Scene transition already in progress. Ignoring request.");
                return;
            }
            
            string scenePath = SceneUtility.GetScenePathByBuildIndex(_buildIndex);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            
            StartCoroutine(LoadSceneAsync(sceneName, _onComplete));
        }

        /// <summary>
        /// Reloads the current active scene.
        /// </summary>
        /// <param name="_onComplete">Optional callback when scene reload completes</param>
        public void ReloadCurrentScene(Action _onComplete = null)
        {
            if (m_IsLoading)
            {
                Debug.LogWarning("Scene transition already in progress. Ignoring request.");
                return;
            }
            
            string currentSceneName = SceneManager.GetActiveScene().name;
            StartCoroutine(LoadSceneAsync(currentSceneName, _onComplete));
        }
        #endregion
    }
} 