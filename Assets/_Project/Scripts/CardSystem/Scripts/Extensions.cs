using System;
using System.Text;
using System.Text.RegularExpressions;

namespace CardSystem
{
    /// <summary>
    /// Utility extension methods for the card system
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Converts camel case string to a formatted, spaced string
        /// Example: "damageMultiplier" becomes "Damage Multiplier"
        /// </summary>
        public static string SplitCamelCase(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;
                
            string pattern = @"(?<=[a-z])([A-Z])";
            string result = Regex.Replace(str, pattern, " $1");
            return char.ToUpper(result[0]) + result.Substring(1);
        }
        
        /// <summary>
        /// Converts PascalCase to camelCase
        /// Example: "DamageMultiplier" becomes "damageMultiplier"
        /// </summary>
        public static string ToCamelCase(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;
                
            return char.ToLower(str[0]) + str.Substring(1);
        }
    }
} 