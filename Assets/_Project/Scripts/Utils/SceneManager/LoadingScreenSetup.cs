#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using System.IO;

namespace SceneManagement
{
    /// <summary>
    /// Editor utility for creating a loading screen prefab with all required components.
    /// </summary>
    public class LoadingScreenSetup : MonoBehaviour
    {
        #region Menu Items
        [MenuItem("Tools/Project A/Create Loading Screen Prefab")]
        public static void CreateLoadingScreenPrefab()
        {
            // Create the root GameObject with SceneTransitionManager
            GameObject managerObj = new GameObject("SceneTransitionManager");
            SceneTransitionManager manager = managerObj.AddComponent<SceneTransitionManager>();

            // Create Canvas
            GameObject canvasObj = new GameObject("LoadingScreenCanvas");
            canvasObj.transform.SetParent(managerObj.transform);
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999; // Ensure it's above other UI
            
            CanvasScaler canvasScaler = canvasObj.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.matchWidthOrHeight = 0.5f;
            
            canvasObj.AddComponent<GraphicRaycaster>();

            // Create Background Panel
            GameObject bgPanel = CreateUIElement("BackgroundPanel", canvasObj.transform);
            Image bgImage = bgPanel.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 1);
            bgPanel.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            bgPanel.GetComponent<RectTransform>().anchorMax = Vector2.one;
            bgPanel.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            
            // Create CanvasGroup for fading
            CanvasGroup fadeGroup = bgPanel.AddComponent<CanvasGroup>();

            // Create Loading Text
            GameObject loadingTextObj = CreateUIElement("LoadingText", bgPanel.transform);
            TextMeshProUGUI loadingText = loadingTextObj.AddComponent<TextMeshProUGUI>();
            loadingText.text = "LOADING";
            loadingText.fontSize = 48;
            loadingText.alignment = TextAlignmentOptions.Center;
            loadingText.color = Color.white;
            RectTransform loadingTextRect = loadingTextObj.GetComponent<RectTransform>();
            loadingTextRect.anchorMin = new Vector2(0.5f, 0.5f);
            loadingTextRect.anchorMax = new Vector2(0.5f, 0.5f);
            loadingTextRect.anchoredPosition = new Vector2(0, 100);
            loadingTextRect.sizeDelta = new Vector2(400, 100);

            // Create Progress Bar Background
            GameObject progressBarBg = CreateUIElement("ProgressBarBackground", bgPanel.transform);
            Image progressBgImage = progressBarBg.AddComponent<Image>();
            progressBgImage.color = new Color(0.2f, 0.2f, 0.2f, 1);
            RectTransform progressBgRect = progressBarBg.GetComponent<RectTransform>();
            progressBgRect.anchorMin = new Vector2(0.5f, 0.5f);
            progressBgRect.anchorMax = new Vector2(0.5f, 0.5f);
            progressBgRect.anchoredPosition = new Vector2(0, 0);
            progressBgRect.sizeDelta = new Vector2(600, 30);

            // Create Progress Bar Fill
            GameObject progressBarFill = CreateUIElement("ProgressBarFill", progressBarBg.transform);
            Image progressFillImage = progressBarFill.AddComponent<Image>();
            progressFillImage.color = new Color(0.8f, 0.8f, 0.8f, 1);
            progressFillImage.type = Image.Type.Filled;
            progressFillImage.fillMethod = Image.FillMethod.Horizontal;
            progressFillImage.fillOrigin = 0;
            progressFillImage.fillAmount = 0.5f;
            RectTransform progressFillRect = progressBarFill.GetComponent<RectTransform>();
            progressFillRect.anchorMin = Vector2.zero;
            progressFillRect.anchorMax = Vector2.one;
            progressFillRect.sizeDelta = Vector2.zero;

            // Create Progress Text
            GameObject progressTextObj = CreateUIElement("ProgressText", bgPanel.transform);
            TextMeshProUGUI progressText = progressTextObj.AddComponent<TextMeshProUGUI>();
            progressText.text = "50%";
            progressText.fontSize = 24;
            progressText.alignment = TextAlignmentOptions.Center;
            progressText.color = Color.white;
            RectTransform progressTextRect = progressTextObj.GetComponent<RectTransform>();
            progressTextRect.anchorMin = new Vector2(0.5f, 0.5f);
            progressTextRect.anchorMax = new Vector2(0.5f, 0.5f);
            progressTextRect.anchoredPosition = new Vector2(0, -50);
            progressTextRect.sizeDelta = new Vector2(200, 50);

            // Create Tip Text
            GameObject tipTextObj = CreateUIElement("TipText", bgPanel.transform);
            TextMeshProUGUI tipText = tipTextObj.AddComponent<TextMeshProUGUI>();
            tipText.text = "TIP: Loading screens can display helpful gameplay tips!";
            tipText.fontSize = 24;
            tipText.alignment = TextAlignmentOptions.Center;
            tipText.color = new Color(0.8f, 0.8f, 0.8f, 1);
            RectTransform tipTextRect = tipTextObj.GetComponent<RectTransform>();
            tipTextRect.anchorMin = new Vector2(0.5f, 0);
            tipTextRect.anchorMax = new Vector2(0.5f, 0);
            tipTextRect.anchoredPosition = new Vector2(0, 100);
            tipTextRect.sizeDelta = new Vector2(800, 100);

            // Set references
            SerializedObject serializedManager = new SerializedObject(manager);
            serializedManager.FindProperty("m_LoadingScreenCanvas").objectReferenceValue = canvas;
            serializedManager.FindProperty("m_ProgressBar").objectReferenceValue = progressFillImage;
            serializedManager.FindProperty("m_ProgressText").objectReferenceValue = progressText;
            serializedManager.FindProperty("m_TipText").objectReferenceValue = tipText;
            serializedManager.FindProperty("m_FadeCanvasGroup").objectReferenceValue = fadeGroup;
            serializedManager.FindProperty("m_LoadingTips").arraySize = 3;
            serializedManager.FindProperty("m_LoadingTips").GetArrayElementAtIndex(0).stringValue = "TIP: Press F to interact with objects in the world.";
            serializedManager.FindProperty("m_LoadingTips").GetArrayElementAtIndex(1).stringValue = "TIP: Upgrade your skills in the Power Tree menu.";
            serializedManager.FindProperty("m_LoadingTips").GetArrayElementAtIndex(2).stringValue = "TIP: Stronger enemies drop better loot!";
            serializedManager.ApplyModifiedProperties();

            // Create directory if it doesn't exist
            string prefabPath = "Assets/_Project/Prefabs/Managers";
            if (!Directory.Exists(prefabPath))
            {
                Directory.CreateDirectory(prefabPath);
            }

            // Save as prefab
            string prefabFilePath = $"{prefabPath}/SceneTransitionManager.prefab";
            PrefabUtility.SaveAsPrefabAsset(managerObj, prefabFilePath);
            Debug.Log($"Created Scene Transition Manager prefab at {prefabFilePath}");

            // Clean up scene instance
            DestroyImmediate(managerObj);

            // Select the created prefab
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabFilePath);
        }
        #endregion

        #region Utility Methods
        private static GameObject CreateUIElement(string _name, Transform _parent)
        {
            GameObject gameObject = new GameObject(_name);
            gameObject.transform.SetParent(_parent);
            gameObject.AddComponent<RectTransform>();
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.localRotation = Quaternion.identity;
            gameObject.transform.localScale = Vector3.one;
            return gameObject;
        }
        #endregion
    }
}
#endif 