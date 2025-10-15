using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityMcp.Gui;

namespace UnityMcp
{
    /// <summary>
    /// MCPSettings manager，Used for managementMCPRelated configuration and preferences
    /// Save toProjectSettings/McpSettings.asset
    /// </summary>
    [FilePath("ProjectSettings/McpSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class McpSettings : ScriptableSingleton<McpSettings>
    {
        /// <summary>
        /// UISetting
        /// </summary>
        public McpUISettings uiSettings;

        /// <summary>
        /// FigmaSetting
        /// </summary>
        public FigmaSettings figmaSettings;

        /// <summary>
        /// GetMCPSettings instance
        /// </summary>
        public static McpSettings Instance => instance;

        /// <summary>
        /// Save settings
        /// </summary>
        public void SaveSettings()
        {
            EditorUtility.SetDirty(this);
            Save(true);
        }
    }

}