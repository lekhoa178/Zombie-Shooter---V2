using UnityEditor;
using UnityEngine;
using System.Reflection;
using System;

namespace LocomotionSystem.LootSystem.Editor
{
    /// <summary>
    /// Custom editor for CollectableBase that ensures ID updates when GameObject name changes
    /// and proper multi-editing support for derived classes
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(CollectableBase), true)]
    public class CollectableBaseEditor : UnityEditor.Editor
    {
        private string lastKnownName;
        private SerializedProperty m_TypeProperty;
        private SerializedProperty m_IDProperty;
        private SerializedProperty m_DisplayNameProperty;
        
        private void OnEnable()
        {
            if (target != null)
            {
                lastKnownName = (target as MonoBehaviour)?.gameObject.name;
                
                // Find commonly used properties
                m_TypeProperty = serializedObject.FindProperty("m_Type");
                m_IDProperty = serializedObject.FindProperty("m_ID");
                m_DisplayNameProperty = serializedObject.FindProperty("m_DisplayName");
            }
            
            // Subscribe to hierarchy changes
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }
        
        private void OnDisable()
        {
            // Unsubscribe from hierarchy changes
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        }
        
        private void OnHierarchyChanged()
        {
            // Skip if target is null (component was deleted)
            if (targets == null || targets.Length == 0) return;
            
            foreach (var targetObj in targets)
            {
                CollectableBase collectableBase = targetObj as CollectableBase;
                if (collectableBase == null) continue;
                
                GameObject gameObject = collectableBase.gameObject;
                
                // Check if the GameObject name has changed
                if (gameObject != null && lastKnownName != gameObject.name)
                {
                    // Call UpdateUniqueIDPrefix via reflection since it's private
                    MethodInfo updateMethod = typeof(CollectableBase).GetMethod(
                        "UpdateUniqueIDPrefix",
                        BindingFlags.NonPublic | BindingFlags.Instance
                    );
                    
                    if (updateMethod != null)
                    {
                        updateMethod.Invoke(collectableBase, null);
                        EditorUtility.SetDirty(collectableBase); // Mark as dirty to save changes
                    }
                }
            }
            
            // Update last known name using the first target
            CollectableBase firstBase = target as CollectableBase;
            if (firstBase != null && firstBase.gameObject != null)
            {
                lastKnownName = firstBase.gameObject.name;
            }
        }
        
        public override void OnInspectorGUI()
        {
            // Update the serialized object
            serializedObject.Update();
            
            // Show a note about the auto-generated ID
            EditorGUILayout.HelpBox(
                "The ID field is auto-generated with the format: GameObjectName_UniqueGUID\n" +
                "It will automatically update if the GameObject name changes.",
                MessageType.Info
            );
            
            // Handle special case for derived classes
            if (targets.Length > 0)
            {
                HandleDerivedClassTypes();
            }
            
            // Draw the default inspector
            DrawDefaultInspector();
            
            // Apply modifications to the serialized object
            serializedObject.ApplyModifiedProperties();
        }
        
        /// <summary>
        /// Handles special rules for CollectableType field in derived classes
        /// </summary>
        private void HandleDerivedClassTypes()
        {
            if (m_TypeProperty == null) return;
            
            bool allSameType = true;
            Type firstType = targets[0].GetType();
            
            // Check if all selected objects are of the same type
            foreach (var t in targets)
            {
                if (t.GetType() != firstType)
                {
                    allSameType = false;
                    break;
                }
            }
            
            // If all objects are the same derived type, enforce the correct CollectableType
            if (allSameType)
            {
                string typeName = firstType.Name;
                
                // Specific type handling
                if (typeName == "WeaponCollectable" && m_TypeProperty.enumValueIndex != (int)CollectableType.Weapon)
                {
                    m_TypeProperty.enumValueIndex = (int)CollectableType.Weapon;
                    serializedObject.ApplyModifiedProperties();
                }
                else if (typeName == "ConsumableCollectable" && m_TypeProperty.enumValueIndex != (int)CollectableType.Consumable)
                {
                    m_TypeProperty.enumValueIndex = (int)CollectableType.Consumable;
                    serializedObject.ApplyModifiedProperties();
                }
                else if (typeName == "MaterialCollectable" && m_TypeProperty.enumValueIndex != (int)CollectableType.Material)
                {
                    m_TypeProperty.enumValueIndex = (int)CollectableType.Material;
                    serializedObject.ApplyModifiedProperties();
                }
                // Add more derived class handling as needed
            }
        }
    }
} 