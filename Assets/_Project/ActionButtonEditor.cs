using UnityEngine;
using UnityEditor;
using UnityEditor.UI;

#if UNITY_EDITOR
[CustomEditor(typeof(ActionButton))]
public class ActionButtonEditor : ButtonEditor
{
    SerializedProperty m_InputActionProp;
    SerializedProperty m_InputIndicatorProp;
    SerializedProperty m_InputTextProp;
    SerializedProperty m_ShowInputBindingOnHoverProp;
    SerializedProperty m_AlwaysShowInputBindingProp;
    SerializedProperty m_SupportedPlatformsProp;

    protected override void OnEnable()
    {
        base.OnEnable();
        
        // Find the custom properties from ActionButton
        m_InputActionProp = serializedObject.FindProperty("m_InputAction");
        m_InputIndicatorProp = serializedObject.FindProperty("m_InputIndicator");
        m_InputTextProp = serializedObject.FindProperty("m_InputText");
        m_ShowInputBindingOnHoverProp = serializedObject.FindProperty("m_ShowInputBindingOnHover");
        m_AlwaysShowInputBindingProp = serializedObject.FindProperty("m_AlwaysShowInputBinding");
        m_SupportedPlatformsProp = serializedObject.FindProperty("m_SupportedPlatforms");
    }

    public override void OnInspectorGUI()
    {
        // Update the serialized object
        serializedObject.Update();
        
        // Draw the default Button inspector
        base.OnInspectorGUI();
        
        // Draw a header for the ActionButton properties
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Input Action Settings", EditorStyles.boldLabel);
        
        // Draw the ActionButton properties
        EditorGUILayout.PropertyField(m_InputActionProp);
        EditorGUILayout.PropertyField(m_InputIndicatorProp);
        EditorGUILayout.PropertyField(m_InputTextProp);
        EditorGUILayout.PropertyField(m_ShowInputBindingOnHoverProp);
        EditorGUILayout.PropertyField(m_AlwaysShowInputBindingProp);
        EditorGUILayout.PropertyField(m_SupportedPlatformsProp);
        
        // Apply the changes
        serializedObject.ApplyModifiedProperties();
    }
}
#endif