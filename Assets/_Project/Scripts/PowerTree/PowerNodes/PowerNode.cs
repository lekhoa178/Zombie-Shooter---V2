using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using System.Text;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Text.RegularExpressions;

namespace PowerTree
{
    /// <summary>
    /// Heart types in the game
    /// </summary>
    public enum HeartType
    {
        Red,
        Blue,
        Black
    }

    /// <summary>
    /// Power branches in the skill tree
    /// </summary>
    public enum PowerBranch
    {
        Survival,
        Attack,
        Evasion,
        Finance
    }

    /// <summary>
    /// Base ScriptableObject for all power nodes
    /// </summary>
    [CreateAssetMenu(fileName = "PowerNode", menuName = "PowerTree/Base Node")]
    public class PowerNode : ScriptableObject
    {
        [Header("Basic Info")]
        [Tooltip("Unique ID for this node")]
        [SerializeField] private string m_UniqueID;
        
        [Tooltip("Node identifier used in game logic")]
        public string nodeID;
        
        [Tooltip("Display name shown in the UI")]
        public string displayName;
        
        [Tooltip("Description for each level (0-2)")]
        public string[] descriptions = new string[3]; // One per level
        public Sprite icon;
        public PowerBranch branch;
        
        [Header("Tree Structure")]
        public PowerNode[] unlocksNodes; // Nodes that this unlocks
        public PowerNode[] requiredNodes; // Nodes required to unlock this
        
        [Header("Costs")]
        [SerializeField] private int[] aetherCosts = new int[] { 1, 2, 3 };
        
        // Track the asset name to detect changes
        private string m_LastAssetName;
        
        /// <summary>
        /// Gets the unique ID of this node
        /// </summary>
        public string UniqueID => m_UniqueID;
        
        private void OnEnable()
        {
            // Generate unique ID if not already set
            if (string.IsNullOrEmpty(m_UniqueID))
            {
                GenerateUniqueID();
            }
            
            m_LastAssetName = name;
        }
        
        private void OnValidate()
        {
            // Check if the asset name has changed
            if (m_LastAssetName != name)
            {
                UpdateUniqueIDPrefix();
                m_LastAssetName = name;
            }
        }
        
        /// <summary>
        /// Generates a unique ID based on the asset name
        /// </summary>
        private void GenerateUniqueID()
        {
            // Format: AssetName_GUID
            m_UniqueID = $"{name}_{Guid.NewGuid().ToString()}";
            m_LastAssetName = name;
        }
        
        /// <summary>
        /// Updates the ID prefix when asset name changes
        /// </summary>
        private void UpdateUniqueIDPrefix()
        {
            if (string.IsNullOrEmpty(m_UniqueID))
            {
                GenerateUniqueID();
                return;
            }
            
            // Extract the GUID part (everything after the last underscore)
            int lastIndex = m_UniqueID.LastIndexOf('_');
            if (lastIndex > 0)
            {
                string guidPart = m_UniqueID.Substring(lastIndex); // Includes the underscore
                m_UniqueID = $"{name}{guidPart}";
            }
            else
            {
                // If ID doesn't have the expected format, generate a new one
                GenerateUniqueID();
            }
        }
        
        public int GetLevelCost(int level)
        {
            if (level < 0 || level >= aetherCosts.Length) return 0;
            return aetherCosts[level];
        }
        
        public int GetTotalCost()
        {
            return aetherCosts.Sum();
        }
        
        public virtual void ApplyEffect(PlayerStats player, int level) 
        {
            // Override in derived classes
        }
        
        public virtual void RemoveEffect(PlayerStats player) 
        {
            // Override in derived classes
        }
        
        public virtual string GetCurrentLevelDescription(int level)
        {
            if (level <= 0 || level > descriptions.Length) return string.Empty;
            return ProcessDescription(descriptions[level - 1]);
        }
        
        /// <summary>
        /// Processes a description string by replacing placeholders with actual field values.
        /// Placeholders are in the format {fieldName} and will be replaced with the
        /// value of the corresponding field in this node.
        /// </summary>
        /// <param name="description">The description template with placeholders</param>
        /// <returns>The processed description with actual values</returns>
        protected string ProcessDescription(string description)
        {
            if (string.IsNullOrEmpty(description)) return description;
            
            // Match placeholders like {fieldName} or {arrayName[index]}
            return Regex.Replace(description, @"\{([^}\[]+)(?:\[(\d+)\])?\}", match => {
                string fieldName = match.Groups[1].Value;
                string indexStr = match.Groups[2].Value;
                
                object value;
                
                if (!string.IsNullOrEmpty(indexStr) && int.TryParse(indexStr, out int index))
                {
                    // Array field access
                    value = GetFieldValue($"{fieldName}[{index}]");
                }
                else
                {
                    // Regular field access
                    value = GetFieldValue(fieldName);
                }
                
                if (value == null)
                    return match.Value; // Keep original if field not found
                
                // Format based on field name and type
                return FormatFieldValue(value, fieldName);
            });
        }
        
        /// <summary>
        /// Gets the value of a field or property by name using reflection.
        /// </summary>
        /// <param name="fieldName">Name of the field or property</param>
        /// <returns>The value of the field/property or null if not found</returns>
        private object GetFieldValue(string fieldName)
        {
            // Check if we're looking for an array element like "attackSpeedBoost[0]"
            var arrayMatch = System.Text.RegularExpressions.Regex.Match(fieldName, @"(\w+)\[(\d+)\]");
            if (arrayMatch.Success)
            {
                string arrayFieldName = arrayMatch.Groups[1].Value;
                int index = int.Parse(arrayMatch.Groups[2].Value);
                
                // Get the array field
                var field = GetType().GetField(arrayFieldName, System.Reflection.BindingFlags.Instance | 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                
                if (field != null)
                {
                    var array = field.GetValue(this) as System.Array;
                    if (array != null && index >= 0 && index < array.Length)
                    {
                        return array.GetValue(index);
                    }
                }
                return null;
            }
            
            // Look for standard fields
            var fieldInfo = GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            
            if (fieldInfo != null)
            {
                return fieldInfo.GetValue(this);
            }
            
            // Look for properties
            var propertyInfo = GetType().GetProperty(fieldName, System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            
            if (propertyInfo != null)
            {
                return propertyInfo.GetValue(this);
            }
            
            return null;
        }
        
        /// <summary>
        /// Formats a field value based on its type and field name.
        /// </summary>
        /// <param name="value">The field value to format</param>
        /// <param name="fieldName">The name of the field (used for custom formatting rules)</param>
        /// <returns>The formatted string representation of the value</returns>
        private string FormatFieldValue(object value, string fieldName)
        {
            if (value == null) return "null";
            
            // Handle specific field naming conventions
            if (fieldName.Contains("Boost") && value is float floatValue)
            {
                // Convert boost values to percentages
                return $"{floatValue * 100:0}";
            }
            else if (fieldName.EndsWith("Duration") && value is float durationValue)
            {
                // Format durations with one decimal place
                return $"{durationValue:0.0}";
            }
            else if (fieldName.Contains("Chance") && value is float chanceValue)
            {
                // Convert chance values to percentages
                return $"{chanceValue * 100:0}";
            }
            else if (value is float floatVal)
            {
                // Default float formatting
                return $"{floatVal:0.##}";
            }
            else if (value is int intVal)
            {
                // Default int formatting
                return intVal.ToString();
            }
            
            // Default formatting for other types
            return value.ToString();
        }
    }
    
} 