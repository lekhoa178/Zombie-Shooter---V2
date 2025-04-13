using UnityEditor;
using UnityEngine;
using LocomotionSystem.Utilities;

namespace LocomotionSystem.Utilities.Editor
{
    /// <summary>
    /// Custom property drawer for ReadOnly attribute that disables GUI for the property
    /// </summary>
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Store original GUI state
            bool previousGUIState = GUI.enabled;
            
            // Disable the field to make it read-only
            GUI.enabled = false;
            
            // Draw the field with disabled state
            EditorGUI.PropertyField(position, property, label);
            
            // Restore original GUI state
            GUI.enabled = previousGUIState;
        }
    }
} 