using UnityEngine;

namespace UniMcp
{
    /// <summary>
    /// Simple localization helper for switching between Chinese and English
    /// Uses McpLocalSettings.CurrentLanguage to determine current language
    /// </summary>
    public static class L
    {
        /// <summary>
        /// Get localized text based on current language setting
        /// </summary>
        /// <param name="en">English text</param>
        /// <param name="zh">Chinese text</param>
        /// <returns>Localized text</returns>
        public static string T(string en, string zh = null)
        {
            if (string.IsNullOrEmpty(zh))
                return en;

            string currentLang = McpLocalSettings.Instance.CurrentLanguage;
            
            // If language is English
            if (!string.IsNullOrEmpty(currentLang) && 
                (currentLang.Contains("English") || currentLang == "English"))
            {
                return en;
            }
            
            // Default to Chinese if empty or contains Chinese
            if (string.IsNullOrEmpty(currentLang) || 
                currentLang.Contains("Chinese") || 
                currentLang.Contains("中文") || 
                currentLang == "中文")
            {
                return zh;
            }
            
            // Fallback to English
            return en;
        }

        /// <summary>
        /// Check if current language is Chinese
        /// </summary>
        public static bool IsChinese()
        {
            string currentLang = McpLocalSettings.Instance.CurrentLanguage;
            if (string.IsNullOrEmpty(currentLang))
                return true; // Default to Chinese
            return currentLang.Contains("Chinese") || currentLang.Contains("中文") || currentLang == "中文";
        }

        /// <summary>
        /// Check if current language is English
        /// </summary>
        public static bool IsEnglish()
        {
            string currentLang = McpLocalSettings.Instance.CurrentLanguage;
            return !string.IsNullOrEmpty(currentLang) && 
                   (currentLang.Contains("English") || currentLang == "English");
        }
    }
}
