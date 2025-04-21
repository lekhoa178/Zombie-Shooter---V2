#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(UITabSwitcher))]
public class UITabSwitcherEditor : Editor
{
    #region Serialized Properties
    // References
    private SerializedProperty m_TabContainerProp;
    private SerializedProperty m_ContentContainerProp;
    
    // Animation
    private SerializedProperty m_FadeInDurationProp;
    private SerializedProperty m_FadeOutDurationProp;
    private SerializedProperty m_FadeInEaseProp;
    private SerializedProperty m_FadeOutEaseProp;
    
    // Button State Colors
    private SerializedProperty m_NormalColorProp;
    private SerializedProperty m_HoverColorProp;
    private SerializedProperty m_PressedColorProp; 
    private SerializedProperty m_SelectedColorProp;
    
    // Button Visual State
    private SerializedProperty m_UseStateSpriteProp;
    private SerializedProperty m_NormalSpriteProp;
    private SerializedProperty m_HoverSpriteProp;
    private SerializedProperty m_PressedSpriteProp;
    private SerializedProperty m_SelectedSpriteProp;
    private SerializedProperty m_ScaleOnHoverProp;
    private SerializedProperty m_HoverScaleFactorProp;
    private SerializedProperty m_ScaleSelectedProp;
    private SerializedProperty m_SelectedScaleFactorProp;
    private SerializedProperty m_UseCustomSelectionIndicatorProp;
    private SerializedProperty m_SelectionIndicatorPrefabProp;
    
    // Text Settings
    private SerializedProperty m_CustomizeTabTextProp;
    private SerializedProperty m_NormalTextColorProp;
    private SerializedProperty m_HoverTextColorProp;
    private SerializedProperty m_PressedTextColorProp;
    private SerializedProperty m_SelectedTextColorProp;
    private SerializedProperty m_NormalTextScaleProp;
    private SerializedProperty m_HoverTextScaleProp;
    private SerializedProperty m_PressedTextScaleProp;
    private SerializedProperty m_SelectedTextScaleProp;
    
    // Transition Settings
    private SerializedProperty m_ButtonTransitionDurationProp;
    private SerializedProperty m_ButtonTransitionEaseProp;
    
    // Sine Wave Animation
    private SerializedProperty m_UseSineWaveAnimationProp;
    private SerializedProperty m_SineWaveAmplitudeProp;
    private SerializedProperty m_SineWaveFrequencyProp;
    private SerializedProperty m_UseSineWaveColorAnimationProp;
    private SerializedProperty m_SineWaveColorMinProp;
    private SerializedProperty m_SineWaveColorMaxProp;
    private SerializedProperty m_ColorFrequencyProp;
    
    // Options
    private SerializedProperty m_DefaultTabIndexProp;
    private SerializedProperty m_DeactivateInactiveContentProp;
    #endregion

    private bool m_ShowButtonStateSettings = true;
    private bool m_ShowButtonColorSettings = true;
    private bool m_ShowButtonSpriteSettings = true;
    private bool m_ShowButtonScaleSettings = true;
    private bool m_ShowIndicatorSettings = true;
    private bool m_ShowAnimationSettings = true;
    private bool m_ShowTextSettings = true;
    private bool m_ShowSineWaveSettings = true;
    private bool m_ShowScaleAnimation = true;
    private bool m_ShowColorAnimation = true;
    
    // Color values directly accessed
    private Color m_ColorMin = Color.red;
    private Color m_ColorMax = Color.yellow;
    private bool m_ColorsInitialized = false;

    private void OnEnable()
    {
        // References
        m_TabContainerProp = serializedObject.FindProperty("m_TabContainer");
        m_ContentContainerProp = serializedObject.FindProperty("m_ContentContainer");
        
        // Animation
        m_FadeInDurationProp = serializedObject.FindProperty("m_FadeInDuration");
        m_FadeOutDurationProp = serializedObject.FindProperty("m_FadeOutDuration");
        m_FadeInEaseProp = serializedObject.FindProperty("m_FadeInEase");
        m_FadeOutEaseProp = serializedObject.FindProperty("m_FadeOutEase");
        
        // Button State Colors
        m_NormalColorProp = serializedObject.FindProperty("m_NormalColor");
        m_HoverColorProp = serializedObject.FindProperty("m_HoverColor");
        m_PressedColorProp = serializedObject.FindProperty("m_PressedColor");
        m_SelectedColorProp = serializedObject.FindProperty("m_SelectedColor");
        
        // Button Visual State
        m_UseStateSpriteProp = serializedObject.FindProperty("m_UseStateSprites");
        m_NormalSpriteProp = serializedObject.FindProperty("m_NormalSprite");
        m_HoverSpriteProp = serializedObject.FindProperty("m_HoverSprite");
        m_PressedSpriteProp = serializedObject.FindProperty("m_PressedSprite");
        m_SelectedSpriteProp = serializedObject.FindProperty("m_SelectedSprite");
        m_ScaleOnHoverProp = serializedObject.FindProperty("m_ScaleOnHover");
        m_HoverScaleFactorProp = serializedObject.FindProperty("m_HoverScaleFactor");
        m_ScaleSelectedProp = serializedObject.FindProperty("m_ScaleSelected");
        m_SelectedScaleFactorProp = serializedObject.FindProperty("m_SelectedScaleFactor");
        m_UseCustomSelectionIndicatorProp = serializedObject.FindProperty("m_UseCustomSelectionIndicator");
        m_SelectionIndicatorPrefabProp = serializedObject.FindProperty("m_SelectionIndicatorPrefab");
        
        // Text Settings
        m_CustomizeTabTextProp = serializedObject.FindProperty("m_CustomizeTabText");
        m_NormalTextColorProp = serializedObject.FindProperty("m_NormalTextColor");
        m_HoverTextColorProp = serializedObject.FindProperty("m_HoverTextColor");
        m_PressedTextColorProp = serializedObject.FindProperty("m_PressedTextColor");
        m_SelectedTextColorProp = serializedObject.FindProperty("m_SelectedTextColor");
        m_NormalTextScaleProp = serializedObject.FindProperty("m_NormalTextScale");
        m_HoverTextScaleProp = serializedObject.FindProperty("m_HoverTextScale");
        m_PressedTextScaleProp = serializedObject.FindProperty("m_PressedTextScale");
        m_SelectedTextScaleProp = serializedObject.FindProperty("m_SelectedTextScale");
        
        // Transition Settings
        m_ButtonTransitionDurationProp = serializedObject.FindProperty("m_ButtonTransitionDuration");
        m_ButtonTransitionEaseProp = serializedObject.FindProperty("m_ButtonTransitionEase");
        
        // Sine Wave Animation
        m_UseSineWaveAnimationProp = serializedObject.FindProperty("m_UseSineWaveAnimation");
        m_SineWaveAmplitudeProp = serializedObject.FindProperty("m_SineWaveAmplitude");
        m_SineWaveFrequencyProp = serializedObject.FindProperty("m_SineWaveFrequency");
        m_UseSineWaveColorAnimationProp = serializedObject.FindProperty("m_UseSineWaveColorAnimation");
        m_SineWaveColorMinProp = serializedObject.FindProperty("m_SineWaveColorMin");
        m_SineWaveColorMaxProp = serializedObject.FindProperty("m_SineWaveColorMax");
        m_ColorFrequencyProp = serializedObject.FindProperty("m_ColorFrequency");
        
        // Options
        m_DefaultTabIndexProp = serializedObject.FindProperty("m_DefaultTabIndex");
        m_DeactivateInactiveContentProp = serializedObject.FindProperty("m_DeactivateInactiveContent");
    }

    public override void OnInspectorGUI()
    {
        // Get direct references to the colors if not initialized
        if (!m_ColorsInitialized)
        {
            UITabSwitcher tabSwitcher = (UITabSwitcher)target;
            
            // Use reflection to get the private color values directly
            System.Reflection.FieldInfo colorMinField = typeof(UITabSwitcher).GetField("m_SineWaveColorMin", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            System.Reflection.FieldInfo colorMaxField = typeof(UITabSwitcher).GetField("m_SineWaveColorMax", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            
            if (colorMinField != null && colorMaxField != null)
            {
                m_ColorMin = (Color)colorMinField.GetValue(tabSwitcher);
                m_ColorMax = (Color)colorMaxField.GetValue(tabSwitcher);
            }
            
            m_ColorsInitialized = true;
        }
        
        serializedObject.Update();

        EditorGUILayout.Space();
        
        #region References Section
        EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(m_TabContainerProp);
        EditorGUILayout.PropertyField(m_ContentContainerProp);
        
        // Validate container references
        UITabSwitcher targetSwitcher = (UITabSwitcher)target;
        Transform tabContainer = m_TabContainerProp.objectReferenceValue as Transform;
        Transform contentContainer = m_ContentContainerProp.objectReferenceValue as Transform;
        
        if (tabContainer != null && contentContainer != null)
        {
            // Count tabs and content
            int tabCount = tabContainer.childCount;
            int contentCount = contentContainer.childCount;
            
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Container Status:", EditorStyles.boldLabel);
            
            // Display counts with colors
            bool matchingCounts = tabCount == contentCount;
            Color defaultColor = GUI.color;
            
            GUI.color = matchingCounts ? Color.green : Color.red;
            EditorGUILayout.LabelField($"Tab Count: {tabCount}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Content Count: {contentCount}", EditorStyles.boldLabel);
            GUI.color = defaultColor;
            
            if (!matchingCounts)
            {
                EditorGUILayout.HelpBox("Warning: The number of tabs and content panels do not match!", MessageType.Warning);
            }
            
            // Default tab validation
            if (contentCount > 0 && m_DefaultTabIndexProp.intValue >= contentCount)
            {
                EditorGUILayout.HelpBox($"Default tab index ({m_DefaultTabIndexProp.intValue}) is out of range. Max valid index is {contentCount - 1}.", MessageType.Error);
            }
            
            EditorGUILayout.EndVertical();
        }
        else
        {
            EditorGUILayout.HelpBox("Please assign both Tab Container and Content Container references.", MessageType.Info);
        }
        #endregion
        
        EditorGUILayout.Space();
        
        #region Button State Settings
        m_ShowButtonStateSettings = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowButtonStateSettings, "Button State Settings");
        if (m_ShowButtonStateSettings)
        {
            EditorGUILayout.Space(5);
            
            // Button Colors Section
            m_ShowButtonColorSettings = EditorGUILayout.Foldout(m_ShowButtonColorSettings, "Button Colors", true);
            if (m_ShowButtonColorSettings)
            {
                EditorGUI.indentLevel++;
                
                using (new EditorGUI.DisabledScope(m_UseSineWaveColorAnimationProp.boolValue))
                {
                    if (m_UseSineWaveColorAnimationProp.boolValue)
                    {
                        EditorGUILayout.HelpBox("Selected color is overridden by Sine Wave Color Animation.", MessageType.Info);
                    }
                    
                    EditorGUILayout.PropertyField(m_SelectedColorProp, new GUIContent("Selected Color"));
                }
                
                EditorGUILayout.PropertyField(m_NormalColorProp, new GUIContent("Normal Color"));
                EditorGUILayout.PropertyField(m_HoverColorProp, new GUIContent("Hover Color"));
                EditorGUILayout.PropertyField(m_PressedColorProp, new GUIContent("Pressed Color"));
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space(5);
            
            // Button Sprites Section
            m_ShowButtonSpriteSettings = EditorGUILayout.Foldout(m_ShowButtonSpriteSettings, "Button Sprites", true);
            if (m_ShowButtonSpriteSettings)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(m_UseStateSpriteProp, new GUIContent("Use State Sprites"));
                
                if (m_UseStateSpriteProp.boolValue)
                {
                    EditorGUILayout.PropertyField(m_NormalSpriteProp, new GUIContent("Normal Sprite"));
                    EditorGUILayout.PropertyField(m_HoverSpriteProp, new GUIContent("Hover Sprite"));
                    EditorGUILayout.PropertyField(m_PressedSpriteProp, new GUIContent("Pressed Sprite"));
                    EditorGUILayout.PropertyField(m_SelectedSpriteProp, new GUIContent("Selected Sprite"));
                    
                    if (m_NormalSpriteProp.objectReferenceValue == null ||
                        m_HoverSpriteProp.objectReferenceValue == null ||
                        m_PressedSpriteProp.objectReferenceValue == null ||
                        m_SelectedSpriteProp.objectReferenceValue == null)
                    {
                        EditorGUILayout.HelpBox("At least one sprite is missing. All sprites should be assigned for proper functionality.", MessageType.Warning);
                    }
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space(5);
            
            // Button Scale Section
            m_ShowButtonScaleSettings = EditorGUILayout.Foldout(m_ShowButtonScaleSettings, "Button Scale", true);
            if (m_ShowButtonScaleSettings)
            {
                EditorGUI.indentLevel++;
                
                bool sineWaveEnabled = m_UseSineWaveAnimationProp.boolValue;
                
                // Hover scale
                EditorGUILayout.PropertyField(m_ScaleOnHoverProp, new GUIContent("Scale On Hover"));
                
                if (m_ScaleOnHoverProp.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_HoverScaleFactorProp, new GUIContent("Hover Scale Factor"));
                    EditorGUI.indentLevel--;
                }
                
                EditorGUILayout.Space(5);
                
                // Selected scale
                using (new EditorGUI.DisabledScope(sineWaveEnabled))
                {
                    if (sineWaveEnabled)
                    {
                        EditorGUILayout.HelpBox("Selected button scale is controlled by Sine Wave Animation.", MessageType.Info);
                    }
                    
                    EditorGUILayout.PropertyField(m_ScaleSelectedProp, new GUIContent("Scale Selected Button"));
                    
                    if (m_ScaleSelectedProp.boolValue && !sineWaveEnabled)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(m_SelectedScaleFactorProp, new GUIContent("Selected Scale Factor"));
                        EditorGUI.indentLevel--;
                    }
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space(5);
            
            // Selection Indicator
            m_ShowIndicatorSettings = EditorGUILayout.Foldout(m_ShowIndicatorSettings, "Selection Indicator", true);
            if (m_ShowIndicatorSettings)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(m_UseCustomSelectionIndicatorProp, new GUIContent(
                    "Use Custom Indicator",
                    "When enabled, a custom selection indicator will be used to show the active tab."
                ));
                
                if (m_UseCustomSelectionIndicatorProp.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_SelectionIndicatorPrefabProp, new GUIContent(
                        "Indicator Prefab",
                        "Prefab to use as the selection indicator. Should have a RectTransform component."
                    ));
                    
                    if (m_SelectionIndicatorPrefabProp.objectReferenceValue == null)
                    {
                        EditorGUILayout.HelpBox("Please assign an indicator prefab.", MessageType.Warning);
                    }
                    else
                    {
                        GameObject prefab = m_SelectionIndicatorPrefabProp.objectReferenceValue as GameObject;
                        if (prefab != null && prefab.GetComponent<RectTransform>() == null)
                        {
                            EditorGUILayout.HelpBox("The indicator prefab should have a RectTransform component.", MessageType.Warning);
                        }
                    }
                    EditorGUI.indentLevel--;
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space(5);
            
            // Transition Settings
            EditorGUILayout.LabelField("Transition Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_ButtonTransitionDurationProp, new GUIContent(
                "Transition Duration",
                "Duration in seconds for button state transitions (color, scale, etc)."
            ));
            EditorGUILayout.PropertyField(m_ButtonTransitionEaseProp, new GUIContent(
                "Transition Ease",
                "Easing function for button state transitions."
            ));
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        #endregion
        
        EditorGUILayout.Space();
        
        #region Animation Settings
        m_ShowAnimationSettings = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowAnimationSettings, "Content Animation Settings");
        if (m_ShowAnimationSettings)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(m_FadeInDurationProp, new GUIContent(
                "Fade In Duration",
                "Duration in seconds for content panels to fade in."
            ));
            EditorGUILayout.PropertyField(m_FadeOutDurationProp, new GUIContent(
                "Fade Out Duration",
                "Duration in seconds for content panels to fade out."
            ));
            EditorGUILayout.PropertyField(m_FadeInEaseProp, new GUIContent(
                "Fade In Ease",
                "Easing function for content fade in animation."
            ));
            EditorGUILayout.PropertyField(m_FadeOutEaseProp, new GUIContent(
                "Fade Out Ease",
                "Easing function for content fade out animation."
            ));
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        #endregion
        
        EditorGUILayout.Space();
        
        #region Text Settings
        m_ShowTextSettings = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowTextSettings, "Text Settings");
        if (m_ShowTextSettings)
        {
            EditorGUILayout.Space(5);
            
            EditorGUILayout.PropertyField(m_CustomizeTabTextProp, new GUIContent("Customize Tab Text"));
            
            if (m_CustomizeTabTextProp.boolValue)
            {
                // Text color settings
                EditorGUILayout.LabelField("Text Colors", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(m_NormalTextColorProp, new GUIContent("Normal Text Color"));
                EditorGUILayout.PropertyField(m_HoverTextColorProp, new GUIContent("Hover Text Color"));
                EditorGUILayout.PropertyField(m_PressedTextColorProp, new GUIContent("Pressed Text Color"));
                EditorGUILayout.PropertyField(m_SelectedTextColorProp, new GUIContent("Selected Text Color"));
                
                EditorGUILayout.Space(5);
                
                // Text scale settings
                EditorGUILayout.LabelField("Text Scale", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(m_NormalTextScaleProp, new GUIContent("Normal Text Scale"));
                EditorGUILayout.PropertyField(m_HoverTextScaleProp, new GUIContent("Hover Text Scale"));
                EditorGUILayout.PropertyField(m_PressedTextScaleProp, new GUIContent("Pressed Text Scale"));
                EditorGUILayout.PropertyField(m_SelectedTextScaleProp, new GUIContent("Selected Text Scale"));
                
                // Check if TextMeshPro is found
                if (tabContainer != null)
                {
                    bool textComponentsFound = false;
                    bool missingTextComponents = false;
                    
                    for (int i = 0; i < tabContainer.childCount; i++)
                    {
                        if (tabContainer.GetChild(i).GetComponentInChildren<TMPro.TextMeshProUGUI>() != null)
                        {
                            textComponentsFound = true;
                        }
                        else
                        {
                            missingTextComponents = true;
                        }
                    }
                    
                    if (!textComponentsFound)
                    {
                        EditorGUILayout.HelpBox("No TextMeshProUGUI components found in your tab buttons. Text customization will have no effect.", MessageType.Warning);
                    }
                    else if (missingTextComponents)
                    {
                        EditorGUILayout.HelpBox("Some tab buttons are missing TextMeshProUGUI components. Text settings will only affect buttons with text.", MessageType.Info);
                    }
                }
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        #endregion
        
        EditorGUILayout.Space();
        
        #region Sine Wave Animation
        m_ShowSineWaveSettings = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowSineWaveSettings, "Sine Wave Animation");
        if (m_ShowSineWaveSettings)
        {
            EditorGUILayout.Space(5);
            
            // Scale Animation
            m_ShowScaleAnimation = EditorGUILayout.Foldout(m_ShowScaleAnimation, "Scale Animation", true);
            if (m_ShowScaleAnimation)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(m_UseSineWaveAnimationProp, new GUIContent("Use Scale Animation"));
                
                if (m_UseSineWaveAnimationProp.boolValue)
                {
                    EditorGUILayout.PropertyField(m_SineWaveAmplitudeProp, new GUIContent("Wave Amplitude"));
                    EditorGUILayout.PropertyField(m_SineWaveFrequencyProp, new GUIContent("Wave Frequency"));
                    
                    if (m_ScaleSelectedProp.boolValue)
                    {
                        EditorGUILayout.HelpBox("Sine Wave Animation will override the Selected Button Scale setting.", MessageType.Info);
                    }
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space(5);
            
            // Color Animation
            m_ShowColorAnimation = EditorGUILayout.Foldout(m_ShowColorAnimation, "Color Animation", true);
            if (m_ShowColorAnimation)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(m_UseSineWaveColorAnimationProp, new GUIContent("Use Color Animation"));
                
                if (m_UseSineWaveColorAnimationProp.boolValue)
                {
                    EditorGUILayout.HelpBox("Ensure both colors are different to see the animation effect.", MessageType.Info);
                    
                    // Fix: Use direct color field instead of property field
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel(new GUIContent("Color Min"));
                    Color newColorMin = EditorGUILayout.ColorField(m_SineWaveColorMinProp.colorValue);
                    if (newColorMin != m_SineWaveColorMinProp.colorValue)
                    {
                        m_SineWaveColorMinProp.colorValue = newColorMin;
                        // Store the new value in our local variable too
                        m_ColorMin = newColorMin;
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel(new GUIContent("Color Max"));
                    Color newColorMax = EditorGUILayout.ColorField(m_SineWaveColorMaxProp.colorValue);
                    if (newColorMax != m_SineWaveColorMaxProp.colorValue)
                    {
                        m_SineWaveColorMaxProp.colorValue = newColorMax;
                        // Store the new value in our local variable too
                        m_ColorMax = newColorMax;
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.PropertyField(m_ColorFrequencyProp, new GUIContent("Color Frequency"));
                    
                    // Show alternate color pickers if the standard ones don't work
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Alternative Color Editor:", EditorStyles.boldLabel);
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel(new GUIContent("Alt Min Color"));
                    m_ColorMin = EditorGUILayout.ColorField(m_ColorMin);
                    if (GUILayout.Button("Apply", GUILayout.Width(80)))
                    {
                        m_SineWaveColorMinProp.colorValue = m_ColorMin;
                        GUI.FocusControl(null);
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel(new GUIContent("Alt Max Color"));
                    m_ColorMax = EditorGUILayout.ColorField(m_ColorMax);
                    if (GUILayout.Button("Apply", GUILayout.Width(80)))
                    {
                        m_SineWaveColorMaxProp.colorValue = m_ColorMax;
                        GUI.FocusControl(null);
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    if (GUILayout.Button("Apply Default Colors (Red/Yellow)"))
                    {
                        m_SineWaveColorMinProp.colorValue = new Color(0.8f, 0f, 0f, 1f); // Red
                        m_SineWaveColorMaxProp.colorValue = new Color(1f, 0.8f, 0f, 1f); // Yellow
                        m_ColorMin = m_SineWaveColorMinProp.colorValue;
                        m_ColorMax = m_SineWaveColorMaxProp.colorValue;
                        GUI.FocusControl(null);
                    }
                    
                    if (m_SineWaveColorMinProp.colorValue == m_SineWaveColorMaxProp.colorValue)
                    {
                        EditorGUILayout.HelpBox("Min and Max colors are the same. No visible animation will occur.", MessageType.Warning);
                    }
                    
                    // Preview the color transition
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Color Transition Preview:", EditorStyles.boldLabel);
                    
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    
                    // Draw color boxes
                    Rect previewRect = EditorGUILayout.GetControlRect(false, 20, GUILayout.Width(200));
                    DrawColorTransitionPreview(previewRect, m_SineWaveColorMinProp.colorValue, m_SineWaveColorMaxProp.colorValue);
                    
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUI.indentLevel--;
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        #endregion
        
        EditorGUILayout.Space();
        
        #region Options
        EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(m_DefaultTabIndexProp, new GUIContent(
            "Default Tab Index",
            "The tab that will be active when the component starts."
        ));
        EditorGUILayout.PropertyField(m_DeactivateInactiveContentProp, new GUIContent(
            "Deactivate Inactive Content", 
            "When enabled, inactive content panels will be deactivated to improve performance."
        ));
        
        EditorGUILayout.Space();
        
        if (m_DeactivateInactiveContentProp.boolValue)
        {
            EditorGUILayout.HelpBox("Content deactivation is enabled. This improves performance but may require extra setup for content that needs initialization time.", MessageType.Info);
        }
        #endregion
        
        EditorGUILayout.Space();
        
        #region Utilities
        if (contentContainer != null && tabContainer != null)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Add Missing CanvasGroups"))
            {
                for (int i = 0; i < contentContainer.childCount; i++)
                {
                    Transform contentTransform = contentContainer.GetChild(i);
                    
                    // Add canvas group component if it doesn't exist
                    CanvasGroup canvasGroup = contentTransform.GetComponent<CanvasGroup>();
                    if (canvasGroup == null)
                    {
                        contentTransform.gameObject.AddComponent<CanvasGroup>();
                        EditorUtility.SetDirty(contentTransform.gameObject);
                    }
                }
            }
            
            if (GUILayout.Button("Reset Tab State"))
            {
                if (EditorApplication.isPlaying)
                {
                    targetSwitcher.SendMessage("DebugResetAllTabs", null, SendMessageOptions.DontRequireReceiver);
                }
                else
                {
                    EditorGUILayout.HelpBox("Reset Tab State only works in Play Mode.", MessageType.Info);
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (GUILayout.Button("Test Animations (Play Mode Only)"))
            {
                if (EditorApplication.isPlaying)
                {
                    targetSwitcher.SendMessage("DebugResetAllTabs", null, SendMessageOptions.DontRequireReceiver);
                    
                    if (!m_UseSineWaveAnimationProp.boolValue && !m_UseSineWaveColorAnimationProp.boolValue)
                    {
                        EditorGUILayout.HelpBox("No animations are currently enabled.", MessageType.Warning);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("This function only works in Play Mode.", MessageType.Info);
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        #endregion
        
        // Apply all properties
        serializedObject.ApplyModifiedProperties();
    }
    
    private void DrawColorTransitionPreview(Rect rect, Color colorMin, Color colorMax)
    {
        if (Event.current.type != EventType.Repaint)
            return;
            
        // Create a gradient with the two colors
        Gradient gradient = new Gradient();
        gradient.colorKeys = new GradientColorKey[]
        {
            new GradientColorKey(colorMin, 0),
            new GradientColorKey(colorMax, 1)
        };
        gradient.alphaKeys = new GradientAlphaKey[]
        {
            new GradientAlphaKey(1, 0),
            new GradientAlphaKey(1, 1)
        };
        
        // Draw the gradient
        for (int i = 0; i < rect.width; i++)
        {
            float t = i / rect.width;
            EditorGUI.DrawRect(new Rect(rect.x + i, rect.y, 1, rect.height), gradient.Evaluate(t));
        }
        
        // Add a border
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), Color.black);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y + rect.height - 1, rect.width, 1), Color.black);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1, rect.height), Color.black);
        EditorGUI.DrawRect(new Rect(rect.x + rect.width - 1, rect.y, 1, rect.height), Color.black);
    }
}
#endif 